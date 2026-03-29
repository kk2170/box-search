using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BoxSearch;

internal sealed class CoreKeeperStorageHooks
{
    private const float PollIntervalSeconds = 0.5f;

    private static readonly string[] ItemNameMemberNames =
    {
        "itemName",
        "ItemName",
        "objectName",
        "ObjectName",
        "displayName",
        "DisplayName",
        "localizedName",
        "LocalizedName",
        "title",
        "Title",
        "hoverName",
        "HoverName",
        "hoverText",
        "HoverText",
    };

    private static readonly string[] ItemNameMethodNames =
    {
        "GetItemName",
        "GetObjectName",
        "GetDisplayName",
        "GetLocalizedName",
        "GetHoverName",
        "GetHoverText",
    };

    private static readonly HashSet<string> GenericContainerLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Container",
        "Chest",
        "Inventory",
        "Storage",
    };

    private static CoreKeeperStorageHooks? current;

    private readonly StorageObservationCoordinator observationCoordinator;
    private readonly SearchOverlayController overlayController;
    private readonly ManualLogSource logger;

    private object? activeChestInventoryUi;
    private string? activeContainerId;
    private string? activeObservationSignature;
    private float nextPollAtUnscaledTime;
    private int lastShowFrame = -1;

    internal CoreKeeperStorageHooks(
        StorageObservationCoordinator observationCoordinator,
        SearchOverlayController overlayController,
        ManualLogSource logger)
    {
        this.observationCoordinator = observationCoordinator;
        this.overlayController = overlayController;
        this.logger = logger;
    }

    internal void Install(Harmony harmony)
    {
        if (harmony is null)
        {
            throw new ArgumentNullException(nameof(harmony));
        }

        current = this;

        var installedLifecycleHook = false;
        installedLifecycleHook |= PatchContainerUiLifecycle(harmony, "InventoryUI");
        installedLifecycleHook |= PatchContainerUiLifecycle(harmony, "ItemSlotsUIContainer");

        if (installedLifecycleHook)
        {
            logger.LogInfo("Installed Box Search live container hooks.");
            return;
        }

        logger.LogWarning("Box Search could not patch container UI lifecycle methods. Falling back to active UI polling only.");
    }

    internal void Update()
    {
        if (activeChestInventoryUi is not null &&
            TryIsContainerUiShowing(activeChestInventoryUi, out var isShowing) &&
            !isShowing)
        {
            ClearActiveContainerUi();
        }

        if (activeChestInventoryUi is null && !TryGetOpenChestInventoryUi(out activeChestInventoryUi))
        {
            return;
        }

        if (Time.unscaledTime < nextPollAtUnscaledTime)
        {
            return;
        }

        nextPollAtUnscaledTime = Time.unscaledTime + PollIntervalSeconds;

        if (TryObserveInventoryUi(activeChestInventoryUi, forceRefresh: false))
        {
            return;
        }

        if (activeContainerId is not null &&
            TryIsContainerUiShowing(activeChestInventoryUi, out isShowing) &&
            isShowing)
        {
            observationCoordinator.Forget(activeContainerId);
            overlayController.NotifyDataChanged();
            activeContainerId = null;
            activeObservationSignature = null;
        }
    }

    private static void OnContainerUiShown(object __instance)
    {
        current?.HandleContainerUiShown(__instance);
    }

    private static void OnContainerUiHidden(object __instance)
    {
        current?.HandleContainerUiHidden(__instance);
    }

    private bool PatchContainerUiLifecycle(Harmony harmony, string typeName)
    {
        var containerUiType = AccessTools.TypeByName(typeName);

        if (containerUiType is null)
        {
            return false;
        }

        var showMethod = AccessTools.DeclaredMethod(containerUiType, "ShowContainerUI");
        var hideMethod = AccessTools.DeclaredMethod(containerUiType, "HideContainerUI");
        var installed = false;

        if (showMethod is not null)
        {
            harmony.Patch(showMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(CoreKeeperStorageHooks), nameof(OnContainerUiShown))));
            installed = true;
        }

        if (hideMethod is not null)
        {
            harmony.Patch(hideMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(CoreKeeperStorageHooks), nameof(OnContainerUiHidden))));
            installed = true;
        }

        return installed;
    }

    private void HandleContainerUiShown(object inventoryUi)
    {
        if (!IsChestInventoryUi(inventoryUi))
        {
            return;
        }

        if (ReferenceEquals(activeChestInventoryUi, inventoryUi) && lastShowFrame == Time.frameCount)
        {
            return;
        }

        lastShowFrame = Time.frameCount;
        activeChestInventoryUi = inventoryUi;
        nextPollAtUnscaledTime = 0f;

        TryObserveInventoryUi(inventoryUi, forceRefresh: true);
    }

    private void HandleContainerUiHidden(object inventoryUi)
    {
        if (!ReferenceEquals(activeChestInventoryUi, inventoryUi) && !IsChestInventoryUi(inventoryUi))
        {
            return;
        }

        ClearActiveContainerUi();
    }

    private void ClearActiveContainerUi()
    {
        activeChestInventoryUi = null;
        activeObservationSignature = null;
        nextPollAtUnscaledTime = 0f;
    }

    private bool TryObserveInventoryUi(object inventoryUi, bool forceRefresh)
    {
        if (!TryCreateObservation(inventoryUi, out var observation, out var observationSignature))
        {
            return false;
        }

        if (!forceRefresh && string.Equals(activeObservationSignature, observationSignature, StringComparison.Ordinal))
        {
            return true;
        }

        observationCoordinator.Observe(observation);
        overlayController.NotifyDataChanged();
        activeContainerId = observation.ContainerId;
        activeObservationSignature = observationSignature;
        return true;
    }

    private bool TryCreateObservation(
        object inventoryUi,
        out StorageObservation observation,
        out string observationSignature)
    {
        observation = null!;
        observationSignature = string.Empty;

        if (!TryGetInventoryHandler(inventoryUi, out var inventoryHandler))
        {
            return false;
        }

        if (!TryGetObservedItems(inventoryUi, inventoryHandler, out var items))
        {
            return false;
        }

        var hasPosition = TryGetContainerPosition(inventoryHandler, out var position);
        var containerId = CreateContainerId(inventoryHandler, position, hasPosition);
        var locationHint = CreateLocationHint(inventoryHandler, position, hasPosition);

        observation = new StorageObservation(
            containerId,
            locationHint,
            position,
            DateTimeOffset.UtcNow,
            items);

        observationSignature = CreateObservationSignature(containerId, locationHint, position, items);
        return true;
    }

    private bool TryGetObservedItems(
        object inventoryUi,
        object inventoryHandler,
        out IReadOnlyList<ObservedItemStack> items)
    {
        if (TryGetObservedItemsFromUiSlots(inventoryUi, inventoryHandler, out items))
        {
            return true;
        }

        return TryGetObservedItemsFromInventoryHandler(inventoryHandler, out items);
    }

    private bool TryGetObservedItemsFromUiSlots(
        object inventoryUi,
        object inventoryHandler,
        out IReadOnlyList<ObservedItemStack> items)
    {
        items = Array.Empty<ObservedItemStack>();

        if (!TryGetMemberValue(inventoryUi, "itemSlots", out var rawSlots) || rawSlots is not IEnumerable slots)
        {
            return false;
        }

        var slotLimit = TryGetIntMember(inventoryHandler, out var inventorySize, "size", "Size")
            ? inventorySize
            : int.MaxValue;

        var aggregateCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var inspectedSlotCount = 0;

        foreach (var slot in slots)
        {
            if (inspectedSlotCount >= slotLimit)
            {
                break;
            }

            inspectedSlotCount++;

            if (slot is null)
            {
                continue;
            }

            if (!TryReadContainedItem(slot, out var itemName, out var count))
            {
                continue;
            }

            AddObservedItem(aggregateCounts, itemName, count);
        }

        items = aggregateCounts
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => new ObservedItemStack(pair.Key, pair.Value))
            .ToArray();

        return inspectedSlotCount > 0 || slotLimit == 0;
    }

    private bool TryGetObservedItemsFromInventoryHandler(
        object inventoryHandler,
        out IReadOnlyList<ObservedItemStack> items)
    {
        items = Array.Empty<ObservedItemStack>();

        if (!TryGetIntMember(inventoryHandler, out var inventorySize, "size", "Size"))
        {
            return false;
        }

        var aggregateCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var inspectedSlotCount = 0;

        for (var slotIndex = 0; slotIndex < inventorySize; slotIndex++)
        {
            if (!TryInvokeIndexedMethod(
                    inventoryHandler,
                    slotIndex,
                    out var containedObject,
                    "GetContainedObject",
                    "GetObjectData",
                    "GetContainedObjectData",
                    "GetSlotObjectData"))
            {
                continue;
            }

            inspectedSlotCount++;

            if (containedObject is null)
            {
                continue;
            }

            if (!TryReadContainedItem(containedObject, out var itemName, out var count))
            {
                continue;
            }

            AddObservedItem(aggregateCounts, itemName, count);
        }

        items = aggregateCounts
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static pair => new ObservedItemStack(pair.Key, pair.Value))
            .ToArray();

        return inspectedSlotCount > 0 || inventorySize == 0;
    }

    private bool TryReadContainedItem(object source, out string itemName, out int count)
    {
        itemName = string.Empty;
        count = 0;

        var containedObject = TryInvokeNoArgMethod(source, "GetContainedObject") ?? source;

        if (!TryGetContainedObjectIdText(containedObject, out var objectIdText) ||
            string.Equals(objectIdText, "None", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryGetIntMember(containedObject, out count, "amount", "Amount", "count", "Count", "stackSize", "StackSize") ||
            count <= 0)
        {
            count = 1;
        }

        itemName = ResolveItemName(source, containedObject, objectIdText);
        return itemName.Length > 0;
    }

    private static string ResolveItemName(object source, object containedObject, string objectIdText)
    {
        foreach (var memberName in ItemNameMemberNames)
        {
            if (TryGetNonEmptyStringMember(source, memberName, out var itemName) ||
                TryGetNonEmptyStringMember(containedObject, memberName, out itemName))
            {
                return CleanupUiText(itemName);
            }
        }

        foreach (var methodName in ItemNameMethodNames)
        {
            if (TryInvokeNoArgMethod(source, methodName) is string itemName && !string.IsNullOrWhiteSpace(itemName))
            {
                return CleanupUiText(itemName);
            }

            if (TryInvokeNoArgMethod(containedObject, methodName) is string containedObjectName &&
                !string.IsNullOrWhiteSpace(containedObjectName))
            {
                return CleanupUiText(containedObjectName);
            }
        }

        return HumanizeIdentifier(objectIdText);
    }

    private static string CleanupUiText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var firstLine = value
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return firstLine?.Trim() ?? string.Empty;
    }

    private static void AddObservedItem(Dictionary<string, int> aggregateCounts, string itemName, int count)
    {
        if (string.IsNullOrWhiteSpace(itemName) || count <= 0)
        {
            return;
        }

        var trimmedItemName = itemName.Trim();

        if (aggregateCounts.TryGetValue(trimmedItemName, out var existingCount))
        {
            aggregateCounts[trimmedItemName] = existingCount + count;
            return;
        }

        aggregateCounts[trimmedItemName] = count;
    }

    private static string CreateObservationSignature(
        string containerId,
        string locationHint,
        WorldPosition position,
        IReadOnlyList<ObservedItemStack> items)
    {
        var builder = new StringBuilder();
        builder.Append(containerId);
        builder.Append('|');
        builder.Append(locationHint);
        builder.Append('|');
        builder.Append(position.X.ToString("0.###", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(position.Y.ToString("0.###", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(position.Z.ToString("0.###", CultureInfo.InvariantCulture));

        foreach (var item in items.OrderBy(static item => item.ItemName, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append('|');
            builder.Append(item.ItemName);
            builder.Append(':');
            builder.Append(item.Count.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    // Prefer the ECS inventory entity when available because it should survive UI recreation.
    // Fall back to the backing MonoBehaviour instance, then to coordinates as a last resort.
    private static string CreateContainerId(object inventoryHandler, WorldPosition position, bool hasPosition)
    {
        if (TryGetMemberValue(inventoryHandler, "inventoryEntity", out var inventoryEntity) &&
            inventoryEntity is not null &&
            TryFormatEntityId(inventoryEntity, out var entityId))
        {
            return $"inventory:{entityId}";
        }

        if (TryGetMemberValue(inventoryHandler, "entityMonoBehaviour", out var entityMonoBehaviour) &&
            entityMonoBehaviour is UnityEngine.Object unityObject)
        {
            return $"instance:{unityObject.GetInstanceID().ToString(CultureInfo.InvariantCulture)}";
        }

        if (hasPosition)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "position:{0:0.###}:{1:0.###}:{2:0.###}",
                position.X,
                position.Y,
                position.Z);
        }

        return $"inventory-handler:{inventoryHandler.GetHashCode().ToString(CultureInfo.InvariantCulture)}";
    }

    private static string CreateLocationHint(object inventoryHandler, WorldPosition position, bool hasPosition)
    {
        var containerLabel = ResolveContainerLabel(inventoryHandler);

        if (!hasPosition)
        {
            return containerLabel;
        }

        if (IsMeaningfulContainerLabel(containerLabel))
        {
            return containerLabel;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} @ {1:0.#}, {2:0.#}",
            containerLabel,
            position.X,
            position.Z);
    }

    private static string ResolveContainerLabel(object inventoryHandler)
    {
        if (TryGetMemberValue(inventoryHandler, "entityMonoBehaviour", out var entityMonoBehaviour) &&
            entityMonoBehaviour is Component component)
        {
            foreach (var memberName in ItemNameMemberNames)
            {
                if (TryGetNonEmptyStringMember(component, memberName, out var label))
                {
                    return NormalizeContainerLabel(label);
                }
            }

            if (!string.IsNullOrWhiteSpace(component.name))
            {
                return NormalizeContainerLabel(component.name);
            }

            return NormalizeContainerLabel(component.GetType().Name);
        }

        return "Container";
    }

    private static bool IsMeaningfulContainerLabel(string containerLabel)
    {
        return !string.IsNullOrWhiteSpace(containerLabel) && !GenericContainerLabels.Contains(containerLabel.Trim());
    }

    private static string NormalizeContainerLabel(string value)
    {
        var cleanedValue = CleanupUiText(value);

        if (cleanedValue.EndsWith("(Clone)", StringComparison.Ordinal))
        {
            cleanedValue = cleanedValue[..^"(Clone)".Length].TrimEnd();
        }

        var humanizedValue = HumanizeIdentifier(cleanedValue);
        return humanizedValue.Length == 0 ? "Container" : humanizedValue;
    }

    private static bool TryFormatEntityId(object entity, out string entityId)
    {
        entityId = string.Empty;

        if (TryGetIntMember(entity, out var index, "Index", "index") &&
            TryGetIntMember(entity, out var version, "Version", "version") &&
            (index != 0 || version != 0))
        {
            entityId = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", index, version);
            return true;
        }

        var rawText = entity.ToString();

        if (string.IsNullOrWhiteSpace(rawText) ||
            string.Equals(rawText, "Entity.Null", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        entityId = rawText.Trim();
        return true;
    }

    private static bool TryGetContainerPosition(object inventoryHandler, out WorldPosition position)
    {
        position = default;

        if (!TryGetMemberValue(inventoryHandler, "entityMonoBehaviour", out var entityMonoBehaviour) ||
            entityMonoBehaviour is not Component component)
        {
            return false;
        }

        var worldPosition = component.transform.position;
        position = new WorldPosition(worldPosition.x, worldPosition.y, worldPosition.z);
        return true;
    }

    private static bool TryGetContainedObjectIdText(object containedObject, out string objectIdText)
    {
        objectIdText = string.Empty;

        if (!TryGetMemberValue(containedObject, "objectID", out var objectId) &&
            !TryGetMemberValue(containedObject, "objectId", out objectId) &&
            !TryGetMemberValue(containedObject, "ObjectID", out objectId) &&
            !TryGetMemberValue(containedObject, "ObjectId", out objectId))
        {
            return false;
        }

        if (objectId is null)
        {
            return false;
        }

        objectIdText = objectId.ToString()?.Trim() ?? string.Empty;
        return objectIdText.Length > 0;
    }

    private static bool TryGetInventoryHandler(object inventoryUi, out object inventoryHandler)
    {
        inventoryHandler = null!;
        var resolvedInventoryHandler = TryInvokeNoArgMethod(inventoryUi, "GetInventoryHandler");

        if (resolvedInventoryHandler is null)
        {
            return false;
        }

        inventoryHandler = resolvedInventoryHandler;
        return true;
    }

    private bool TryGetOpenChestInventoryUi(out object inventoryUi)
    {
        inventoryUi = null!;

        if (!TryGetChestInventoryUi(out var chestInventoryUi))
        {
            return false;
        }

        if (!TryIsContainerUiShowing(chestInventoryUi, out var isShowing) || !isShowing)
        {
            return false;
        }

        if (!TryGetInventoryHandler(chestInventoryUi, out var chestInventoryHandler))
        {
            return false;
        }

        if (TryGetPlayerActiveInventoryHandler(out var activeInventoryHandler) &&
            !ReferenceEquals(activeInventoryHandler, chestInventoryHandler))
        {
            return false;
        }

        inventoryUi = chestInventoryUi;
        return true;
    }

    private static bool TryGetChestInventoryUi(out object inventoryUi)
    {
        inventoryUi = null!;

        if (!TryGetManagerUi(out var managerUi) ||
            !TryGetMemberValue(managerUi, "chestInventoryUI", out var chestInventoryUi) ||
            chestInventoryUi is null)
        {
            return false;
        }

        inventoryUi = chestInventoryUi;
        return true;
    }

    private static bool TryGetPlayerActiveInventoryHandler(out object inventoryHandler)
    {
        inventoryHandler = null!;

        if (!TryGetManagerMain(out var managerMain) ||
            !TryGetMemberValue(managerMain, "player", out var player) ||
            player is null ||
            !TryGetMemberValue(player, "activeInventoryHandler", out var activeInventoryHandler) ||
            activeInventoryHandler is null)
        {
            return false;
        }

        inventoryHandler = activeInventoryHandler;
        return true;
    }

    private static bool TryGetManagerUi(out object managerUi)
    {
        managerUi = null!;
        var managerType = AccessTools.TypeByName("Manager");

        if (managerType is null || !TryGetStaticMemberValue(managerType, "ui", out var resolvedManagerUi) || resolvedManagerUi is null)
        {
            return false;
        }

        managerUi = resolvedManagerUi;
        return true;
    }

    private static bool TryGetManagerMain(out object managerMain)
    {
        managerMain = null!;
        var managerType = AccessTools.TypeByName("Manager");

        if (managerType is null || !TryGetStaticMemberValue(managerType, "main", out var resolvedManagerMain) || resolvedManagerMain is null)
        {
            return false;
        }

        managerMain = resolvedManagerMain;
        return true;
    }

    private static bool IsChestInventoryUi(object inventoryUi)
    {
        return TryGetChestInventoryUi(out var chestInventoryUi) && ReferenceEquals(chestInventoryUi, inventoryUi);
    }

    private static bool TryIsContainerUiShowing(object inventoryUi, out bool isShowing)
    {
        if (TryGetBoolMember(inventoryUi, out isShowing, "isShowing", "IsShowing", "visible", "Visible"))
        {
            return true;
        }

        if (inventoryUi is Behaviour behaviour)
        {
            isShowing = behaviour.isActiveAndEnabled;
            return true;
        }

        if (inventoryUi is Component component)
        {
            isShowing = component.gameObject.activeInHierarchy;
            return true;
        }

        isShowing = false;
        return false;
    }

    private static object? TryInvokeNoArgMethod(object target, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var method = AccessTools.Method(target.GetType(), methodName, Type.EmptyTypes);

            if (method is null)
            {
                continue;
            }

            try
            {
                return method.Invoke(target, null);
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        return null;
    }

    private static bool TryInvokeIndexedMethod(
        object target,
        int index,
        out object? value,
        params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            var method = AccessTools.Method(target.GetType(), methodName, new[] { typeof(int) });

            if (method is null)
            {
                continue;
            }

            try
            {
                value = method.Invoke(target, new object[] { index });
                return true;
            }
            catch (TargetInvocationException)
            {
                continue;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetStaticMemberValue(Type type, string memberName, out object? value)
    {
        var property = AccessTools.Property(type, memberName);

        if (property is not null)
        {
            try
            {
                value = property.GetValue(null, null);
                return true;
            }
            catch (TargetInvocationException)
            {
            }
        }

        var field = AccessTools.Field(type, memberName);

        if (field is not null)
        {
            value = field.GetValue(null);
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetMemberValue(object target, string memberName, out object? value)
    {
        var property = AccessTools.Property(target.GetType(), memberName);

        if (property is not null)
        {
            try
            {
                value = property.GetValue(target, null);
                return true;
            }
            catch (TargetInvocationException)
            {
            }
        }

        var field = AccessTools.Field(target.GetType(), memberName);

        if (field is not null)
        {
            value = field.GetValue(target);
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetNonEmptyStringMember(object target, string memberName, out string value)
    {
        value = string.Empty;

        if (!TryGetMemberValue(target, memberName, out var rawValue) || rawValue is not string textValue)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(textValue))
        {
            return false;
        }

        value = textValue.Trim();
        return true;
    }

    private static bool TryGetIntMember(object target, out int value, params string[] memberNames)
    {
        foreach (var memberName in memberNames)
        {
            if (!TryGetMemberValue(target, memberName, out var rawValue) || rawValue is null)
            {
                continue;
            }

            if (TryConvertToInt(rawValue, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static bool TryGetBoolMember(object target, out bool value, params string[] memberNames)
    {
        foreach (var memberName in memberNames)
        {
            if (!TryGetMemberValue(target, memberName, out var rawValue) || rawValue is null)
            {
                continue;
            }

            if (rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            if (rawValue is IConvertible convertibleValue)
            {
                try
                {
                    value = convertibleValue.ToBoolean(CultureInfo.InvariantCulture);
                    return true;
                }
                catch (FormatException)
                {
                }
            }
        }

        value = false;
        return false;
    }

    private static bool TryConvertToInt(object value, out int convertedValue)
    {
        if (value is int intValue)
        {
            convertedValue = intValue;
            return true;
        }

        if (value is IConvertible convertibleValue)
        {
            try
            {
                convertedValue = convertibleValue.ToInt32(CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
        }

        convertedValue = 0;
        return false;
    }

    private static string HumanizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length * 2);
        var previousCharacter = '\0';

        foreach (var currentCharacter in value.Trim())
        {
            if (currentCharacter == '_')
            {
                if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                {
                    builder.Append(' ');
                }

                previousCharacter = currentCharacter;
                continue;
            }

            if (builder.Length > 0 &&
                currentCharacter != ' ' &&
                previousCharacter != ' ' &&
                previousCharacter != '_' &&
                ((char.IsUpper(currentCharacter) && !char.IsUpper(previousCharacter)) ||
                 (char.IsDigit(currentCharacter) && !char.IsDigit(previousCharacter)) ||
                 (!char.IsDigit(currentCharacter) && char.IsDigit(previousCharacter))))
            {
                builder.Append(' ');
            }

            builder.Append(currentCharacter);
            previousCharacter = currentCharacter;
        }

        return builder.ToString().Trim();
    }
}
