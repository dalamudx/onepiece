﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OnePiece.Models;
using OnePiece.Localization;

namespace OnePiece.Services;

/// <summary>
/// Service for handling treasure hunt coordinates and route optimization.
/// </summary>
public class TreasureHuntService
{
    private readonly Plugin plugin;

    /// <summary>
    /// Gets the list of treasure coordinates.
    /// </summary>
    public List<TreasureCoordinate> Coordinates { get; private set; } = new();

    /// <summary>
    /// Gets the list of deleted treasure coordinates (trash bin).
    /// </summary>
    public List<TreasureCoordinate> DeletedCoordinates { get; private set; } = new();

    /// <summary>
    /// Gets the optimized route through the coordinates.
    /// </summary>
    public List<TreasureCoordinate> OptimizedRoute { get; private set; } = new();

    /// <summary>
    /// Gets whether the route is currently optimized.
    /// </summary>
    public bool IsRouteOptimized => OptimizedRoute.Count > 0;

    /// <summary>
    /// Gets the original order of coordinates before optimization.
    /// </summary>
    private List<TreasureCoordinate> OriginalOrder { get; set; } = new();

    /// <summary>
    /// Event raised when coordinates are imported.
    /// </summary>
    public event EventHandler<int>? CoordinatesImported;

    /// <summary>
    /// Event raised when a route is optimized.
    /// </summary>
    public event EventHandler<int>? RouteOptimized;

    /// <summary>
    /// Event raised when a route optimization is reset.
    /// </summary>
    public event EventHandler? RouteOptimizationReset;

    /// <summary>
    /// Event raised when coordinates are cleared.
    /// </summary>
    public event EventHandler? CoordinatesCleared;

    /// <summary>
    /// Event raised when a coordinate is deleted.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? CoordinateDeleted;

    /// <summary>
    /// Event raised when a coordinate is restored from trash.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? CoordinateRestored;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreasureHuntService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public TreasureHuntService(Plugin plugin)
    {
        this.plugin = plugin;
    }

    /// <summary>
    /// Imports coordinates from text.
    /// </summary>
    /// <param name="text">The text containing coordinates.</param>
    /// <returns>The number of coordinates imported.</returns>
    public int ImportCoordinates(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Regular expression to match coordinates with map area in the format "MapName (x, y)" or just "(x, y)"
        // Group 1: Map area (optional)
        // Group 2: X coordinate
        // Group 3: Y coordinate
        var regex = new Regex(@"(?:([A-Za-z0-9\s']+)?\s*\(?\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)?)", RegexOptions.IgnoreCase);
        var matches = regex.Matches(text);

        var importedCount = 0;
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 4 &&
                float.TryParse(match.Groups[2].Value, out var x) &&
                float.TryParse(match.Groups[3].Value, out var y))
            {
                // Extract map area (if present)
                string mapArea = match.Groups[1].Success ? match.Groups[1].Value.Trim() : string.Empty;

                AddCoordinate(new TreasureCoordinate(x, y, mapArea));
                importedCount++;
            }
        }

        // If auto-optimize is enabled, optimize the route
        if (plugin.Configuration.AutoOptimizeRoute && Coordinates.Count > 0)
        {
            OptimizeRoute();
        }

        // Raise the event
        CoordinatesImported?.Invoke(this, importedCount);

        return importedCount;
    }

    /// <summary>
    /// Adds a coordinate to the list.
    /// </summary>
    /// <param name="coordinate">The coordinate to add.</param>
    public void AddCoordinate(TreasureCoordinate coordinate)
    {
        // Add the coordinate
        Coordinates.Add(coordinate);

        // If auto-optimize is enabled, optimize the route
        if (plugin.Configuration.AutoOptimizeRoute && Coordinates.Count > 0)
        {
            OptimizeRoute();
        }
    }

    /// <summary>
    /// Clears all coordinates.
    /// </summary>
    public void ClearCoordinates()
    {
        Coordinates.Clear();
        OptimizedRoute.Clear();
        OriginalOrder.Clear();

        // Raise the event
        CoordinatesCleared?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resets the route optimization, restoring the original order.
    /// </summary>
    public void ResetRouteOptimization()
    {
        if (OriginalOrder.Count > 0)
        {
            // Restore the original order
            Coordinates.Clear();
            Coordinates.AddRange(OriginalOrder);

            // Clear the optimized route to indicate we're no longer in optimized mode
            OptimizedRoute.Clear();

            // Clear the original order list to avoid duplicate entries if optimized again
            OriginalOrder.Clear();

            // Log the reset
            Plugin.Log.Information($"Route optimization reset. Restored {Coordinates.Count} coordinates to original order.");

            // Raise the event
            RouteOptimizationReset?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Plugin.Log.Warning("Cannot reset route optimization: No original order saved.");
        }
    }

    /// <summary>
    /// Deletes a coordinate and moves it to the trash bin.
    /// </summary>
    /// <param name="index">The index of the coordinate to delete.</param>
    /// <returns>True if the coordinate was deleted, false otherwise.</returns>
    public bool DeleteCoordinate(int index)
    {
        if (index < 0 || index >= Coordinates.Count)
            return false;

        var coordinate = Coordinates[index];
        DeletedCoordinates.Add(coordinate);
        Coordinates.RemoveAt(index);

        // If auto-optimize is enabled, optimize the route
        if (plugin.Configuration.AutoOptimizeRoute && Coordinates.Count > 0)
        {
            OptimizeRoute();
        }
        else if (Coordinates.Count == 0)
        {
            OptimizedRoute.Clear();
        }

        // Raise the event
        CoordinateDeleted?.Invoke(this, coordinate);

        return true;
    }

    /// <summary>
    /// Restores a coordinate from the trash bin.
    /// </summary>
    /// <param name="index">The index of the coordinate in the trash bin.</param>
    /// <returns>True if the coordinate was restored, false otherwise.</returns>
    public bool RestoreCoordinate(int index)
    {
        if (index < 0 || index >= DeletedCoordinates.Count)
            return false;

        var coordinate = DeletedCoordinates[index];
        Coordinates.Add(coordinate);
        DeletedCoordinates.RemoveAt(index);

        // If auto-optimize is enabled, optimize the route
        if (plugin.Configuration.AutoOptimizeRoute && Coordinates.Count > 0)
        {
            OptimizeRoute();
        }

        // Raise the event
        CoordinateRestored?.Invoke(this, coordinate);

        return true;
    }

    /// <summary>
    /// Clears all coordinates in the trash bin.
    /// </summary>
    public void ClearTrash()
    {
        DeletedCoordinates.Clear();
    }

    /// <summary>
    /// Optimizes the route through the coordinates using a nearest neighbor algorithm.
    /// </summary>
    public void OptimizeRoute()
    {
        if (Coordinates.Count <= 1)
        {
            OptimizedRoute = new List<TreasureCoordinate>(Coordinates);
            RouteOptimized?.Invoke(this, OptimizedRoute.Count);
            return;
        }

        // Save the original order before optimization, but only if this is the first optimization
        if (OriginalOrder.Count == 0)
        {
            OriginalOrder = new List<TreasureCoordinate>(Coordinates);
            Plugin.Log.Debug($"Saved original order with {OriginalOrder.Count} coordinates.");
        }

        // Group coordinates by map area
        var coordinatesByMap = Coordinates.GroupBy(c => c.MapArea).ToDictionary(g => g.Key, g => g.ToList());

        var route = new List<TreasureCoordinate>();

        // Process each map area separately
        foreach (var mapGroup in coordinatesByMap)
        {
            var mapArea = mapGroup.Key;
            var mapCoordinates = mapGroup.Value;

            // Skip if there are no coordinates for this map
            if (mapCoordinates.Count == 0)
                continue;

            // If there's only one coordinate, add it directly
            if (mapCoordinates.Count == 1)
            {
                route.Add(mapCoordinates[0]);
                continue;
            }

            // Simple nearest neighbor algorithm for this map area
            var remaining = new List<TreasureCoordinate>(mapCoordinates);

            // Start with the first coordinate
            var current = remaining[0];
            route.Add(current);
            remaining.RemoveAt(0);

            // Find the nearest neighbor until all coordinates are visited
            while (remaining.Count > 0)
            {
                var nearest = remaining.OrderBy(c => c.DistanceTo(current)).First();
                route.Add(nearest);
                remaining.Remove(nearest);
                current = nearest;
            }
        }

        OptimizedRoute = route;

        // Raise the event
        RouteOptimized?.Invoke(this, OptimizedRoute.Count);
    }

    /// <summary>
    /// Marks a coordinate as collected.
    /// </summary>
    /// <param name="index">The index of the coordinate.</param>
    public void MarkAsCollected(int index)
    {
        if (index >= 0 && index < Coordinates.Count)
        {
            Coordinates[index].IsCollected = true;
        }
    }

    /// <summary>
    /// Gets the total distance of the optimized route.
    /// </summary>
    /// <returns>The total distance.</returns>
    public float GetTotalDistance()
    {
        if (OptimizedRoute.Count <= 1)
            return 0;

        var distance = 0f;
        for (var i = 0; i < OptimizedRoute.Count - 1; i++)
        {
            // Only calculate distance if coordinates are in the same map area
            if (string.IsNullOrEmpty(OptimizedRoute[i].MapArea) ||
                string.IsNullOrEmpty(OptimizedRoute[i + 1].MapArea) ||
                OptimizedRoute[i].MapArea == OptimizedRoute[i + 1].MapArea)
            {
                distance += OptimizedRoute[i].DistanceTo(OptimizedRoute[i + 1]);
            }
        }

        return distance;
    }
}
