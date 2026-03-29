using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoxSearch;

internal sealed class SearchOverlayController
{
    private const int WindowId = 0xB0A5EA;
    private const string QueryControlName = "BoxSearchQuery";

    private readonly StorageSearchService searchService;
    private readonly StorageRegistry registry;

    private Rect windowRect = new(80f, 80f, 680f, 460f);
    private Vector2 resultsScrollPosition;
    private IReadOnlyList<SearchResult> currentResults = Array.Empty<SearchResult>();
    private string query = string.Empty;
    private bool focusQuery;

    internal SearchOverlayController(StorageSearchService searchService, StorageRegistry registry)
    {
        this.searchService = searchService;
        this.registry = registry;
    }

    internal bool IsVisible { get; private set; }

    internal void Toggle()
    {
        IsVisible = !IsVisible;

        if (!IsVisible)
        {
            return;
        }

        focusQuery = true;
        RefreshResults();
    }

    internal void Hide()
    {
        IsVisible = false;
    }

    internal void NotifyDataChanged()
    {
        RefreshResults();
    }

    internal void OnGui()
    {
        if (!IsVisible)
        {
            return;
        }

        windowRect = GUILayout.Window(WindowId, windowRect, DrawWindowContents, "Box Search");
    }

    private void DrawWindowContents(int windowId)
    {
        GUILayout.BeginVertical();
        GUILayout.Label($"Known containers: {registry.Count}");
        GUILayout.Label("Search matches use the last observed contents for each known container.");

        GUI.SetNextControlName(QueryControlName);
        var updatedQuery = GUILayout.TextField(query, 128);

        if (focusQuery)
        {
            GUI.FocusControl(QueryControlName);
            focusQuery = false;
        }

        if (!string.Equals(updatedQuery, query, StringComparison.Ordinal))
        {
            query = updatedQuery;
            RefreshResults();
        }

        GUILayout.Space(8f);
        resultsScrollPosition = GUILayout.BeginScrollView(resultsScrollPosition, GUILayout.ExpandHeight(true));

        if (currentResults.Count == 0)
        {
            var emptyState = query.Length == 0
                ? "Enter part of an item name to search known containers."
                : "No known containers match that query yet.";
            GUILayout.Label(emptyState);
        }
        else
        {
            foreach (var result in currentResults)
            {
                DrawResult(result);
            }
        }

        GUILayout.EndScrollView();
        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear", GUILayout.Width(100f)))
        {
            query = string.Empty;
            RefreshResults();
            focusQuery = true;
        }

        if (GUILayout.Button("Close", GUILayout.Width(100f)))
        {
            Hide();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
    }

    private void DrawResult(SearchResult result)
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label(result.LocationHint);
        GUILayout.Label($"Items: {result.TotalMatchingItemCount} | Position: {result.Position}");
        GUILayout.Label($"Observed: {result.LastObservedUtc:yyyy-MM-dd HH:mm:ss} UTC");
        GUILayout.Label(result.SummaryText);
        GUILayout.EndVertical();
    }

    private void RefreshResults()
    {
        currentResults = searchService.Search(query);
    }
}
