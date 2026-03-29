using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BoxSearch;

/// <summary>
/// Hosts the Box Search mod lifecycle so players can query known storage contents with minimal interruption.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class BoxSearchPlugin : BaseUnityPlugin
{
    internal const string PluginGuid = "io.github.kk2170.boxsearch";
    internal const string PluginName = "Box Search";
    internal const string PluginVersion = "0.1.0";

    private Harmony? harmony;
    private StorageObservationCoordinator? observationCoordinator;
    private SearchOverlayController? overlayController;
    private ConfigEntry<KeyboardShortcut>? toggleOverlayHotkey;
    private ConfigEntry<bool>? enableDebugSampleHotkey;
    private ConfigEntry<KeyboardShortcut>? seedDebugDataHotkey;

    private void Awake()
    {
        var registry = new StorageRegistry();
        var snapshotService = new StorageSnapshotService();
        var searchService = new StorageSearchService(registry);

        observationCoordinator = new StorageObservationCoordinator(registry, snapshotService, Logger);
        overlayController = new SearchOverlayController(searchService, registry);

        toggleOverlayHotkey = Config.Bind(
            "Input",
            "ToggleOverlayHotkey",
            new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
            "Opens or closes the Box Search overlay.");

        enableDebugSampleHotkey = Config.Bind(
            "Debug",
            "EnableDebugSampleHotkey",
            false,
            "Allows sample data to be injected before a real Core Keeper container hook is wired in.");

        seedDebugDataHotkey = Config.Bind(
            "Debug",
            "SeedDebugDataHotkey",
            new KeyboardShortcut(KeyCode.F8),
            "Seeds sample storage data so the overlay can be exercised end-to-end.");

        harmony = new Harmony(PluginGuid);
        harmony.PatchAll();

        Logger.LogInfo($"{PluginName} loaded. Toggle overlay with {toggleOverlayHotkey.Value}.");
    }

    private void Update()
    {
        if (overlayController is null || toggleOverlayHotkey is null)
        {
            return;
        }

        if (toggleOverlayHotkey.Value.IsDown())
        {
            overlayController.Toggle();
        }

        if (overlayController.IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            overlayController.Hide();
        }

        if (enableDebugSampleHotkey?.Value == true &&
            seedDebugDataHotkey is not null &&
            seedDebugDataHotkey.Value.IsDown())
        {
            SeedDebugData();
        }
    }

    private void OnGUI()
    {
        overlayController?.OnGui();
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }

    private void SeedDebugData()
    {
        if (observationCoordinator is null || overlayController is null)
        {
            return;
        }

        var observedAtUtc = DateTimeOffset.UtcNow;

        observationCoordinator.Observe(new StorageObservation(
            "core-base-west-01",
            "Core Base West",
            new WorldPosition(124.0f, 2.0f, 88.0f),
            observedAtUtc,
            new[]
            {
                new ObservedItemStack("Copper Ore", 142),
                new ObservedItemStack("Tin Ore", 87),
                new ObservedItemStack("Wood", 63),
            }));

        observationCoordinator.Observe(new StorageObservation(
            "core-base-north-02",
            "Workbench Row North",
            new WorldPosition(131.0f, 2.0f, 104.0f),
            observedAtUtc,
            new[]
            {
                new ObservedItemStack("Copper Bar", 26),
                new ObservedItemStack("Mechanical Part", 12),
                new ObservedItemStack("Bomb Pepper", 48),
            }));

        observationCoordinator.Observe(new StorageObservation(
            "farm-shed-01",
            "Farm Shed",
            new WorldPosition(84.0f, 2.0f, 156.0f),
            observedAtUtc,
            new[]
            {
                new ObservedItemStack("Wood", 210),
                new ObservedItemStack("Fiber", 95),
                new ObservedItemStack("Copper Hoe", 1),
            }));

        overlayController.NotifyDataChanged();
        Logger.LogInfo("Seeded sample storage data for Box Search.");
    }
}
