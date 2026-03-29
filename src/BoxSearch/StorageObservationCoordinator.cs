using System;
using BepInEx.Logging;

namespace BoxSearch;

internal sealed class StorageObservationCoordinator
{
    private readonly StorageRegistry registry;
    private readonly StorageSnapshotService snapshotService;
    private readonly ManualLogSource logger;

    internal StorageObservationCoordinator(
        StorageRegistry registry,
        StorageSnapshotService snapshotService,
        ManualLogSource logger)
    {
        this.registry = registry;
        this.snapshotService = snapshotService;
        this.logger = logger;
    }

    internal void Observe(StorageObservation observation)
    {
        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        var snapshot = snapshotService.CreateSnapshot(observation);
        registry.Upsert(snapshot);
        logger.LogDebug($"Observed container '{snapshot.ContainerId}' with {snapshot.Items.Count} stacks.");
    }

    internal void Forget(string containerId)
    {
        if (!registry.Remove(containerId))
        {
            return;
        }

        logger.LogDebug($"Removed stale container '{containerId}'.");
    }
}
