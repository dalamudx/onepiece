using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OnePiece.Models;

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
    public event EventHandler<int>? OnCoordinatesImported;

    /// <summary>
    /// Event raised when coordinates are exported.
    /// </summary>
    public event EventHandler<string>? OnCoordinatesExported;

    /// <summary>
    /// Event raised when a route is optimized.
    /// </summary>
    public event EventHandler<int>? OnRouteOptimized;

    /// <summary>
    /// Event raised when a route optimization is reset.
    /// </summary>
    public event EventHandler? OnRouteOptimizationReset;

    /// <summary>
    /// Event raised when coordinates are cleared.
    /// </summary>
    public event EventHandler? OnCoordinatesCleared;

    /// <summary>
    /// Event raised when a coordinate is deleted.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateDeleted;

    /// <summary>
    /// Event raised when a coordinate is restored from trash.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateRestored;

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

        var importedCount = 0;

        // Check if the text is a Base64 encoded string
        if (IsBase64String(text))
        {
            try
            {
                // Decode the Base64 string
                var decodedBytes = Convert.FromBase64String(text);
                var decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);

                // Parse the decoded text as JSON
                var coordinates = System.Text.Json.JsonSerializer.Deserialize<List<TreasureCoordinate>>(decodedText);

                if (coordinates != null)
                {
                    foreach (var coordinate in coordinates)
                    {
                        AddCoordinate(coordinate);
                        importedCount++;
                    }

                    Plugin.Log.Debug($"Imported {importedCount} coordinates from Base64 encoded data");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error importing coordinates from Base64: {ex.Message}");

                // If Base64 import fails, try regular text import
                importedCount = ImportCoordinatesFromText(text);
            }
        }
        else
        {
            // Regular text import
            importedCount = ImportCoordinatesFromText(text);
        }

        // If auto-optimize is enabled, optimize the route
        if (plugin.Configuration.AutoOptimizeRoute && Coordinates.Count > 0)
        {
            OptimizeRoute();
        }

        // Raise the event
        OnCoordinatesImported?.Invoke(this, importedCount);

        return importedCount;
    }

    /// <summary>
    /// Imports coordinates from plain text.
    /// </summary>
    /// <param name="text">The text to import coordinates from.</param>
    /// <returns>The number of coordinates imported.</returns>
    private int ImportCoordinatesFromText(string text)
    {
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

        return importedCount;
    }

    /// <summary>
    /// Exports coordinates to a Base64 encoded string.
    /// </summary>
    /// <returns>A Base64 encoded string containing the coordinates.</returns>
    public string ExportCoordinates()
    {
        try
        {
            // Serialize the original coordinates to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(OriginalOrder.Count > 0 ? OriginalOrder : Coordinates);

            // Encode the JSON as Base64
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            // Raise the event
            OnCoordinatesExported?.Invoke(this, base64);

            return base64;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error exporting coordinates: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a string is a valid Base64 encoded string.
    /// </summary>
    /// <param name="s">The string to check.</param>
    /// <returns>True if the string is a valid Base64 encoded string, false otherwise.</returns>
    private bool IsBase64String(string s)
    {
        // Check if the string is a valid Base64 string
        if (string.IsNullOrWhiteSpace(s))
            return false;

        // Remove any whitespace
        s = s.Trim();

        // Check if the length is valid for Base64
        if (s.Length % 4 != 0)
            return false;

        // Check if the string contains only valid Base64 characters
        return s.All(c =>
            (c >= 'A' && c <= 'Z') ||
            (c >= 'a' && c <= 'z') ||
            (c >= '0' && c <= '9') ||
            c == '+' || c == '/' || c == '=');
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
        OnCoordinatesCleared?.Invoke(this, EventArgs.Empty);
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
            OnRouteOptimizationReset?.Invoke(this, EventArgs.Empty);
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
        OnCoordinateDeleted?.Invoke(this, coordinate);

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
        OnCoordinateRestored?.Invoke(this, coordinate);

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
    /// Optimizes the route through the coordinates considering player location, teleport costs, and travel distances.
    /// </summary>
    public void OptimizeRoute()
    {
        if (Coordinates.Count <= 1)
        {
            OptimizedRoute = new List<TreasureCoordinate>(Coordinates);
            OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
            return;
        }

        // Save the original order before optimization, but only if this is the first optimization
        if (OriginalOrder.Count == 0)
        {
            OriginalOrder = new List<TreasureCoordinate>(Coordinates);
            Plugin.Log.Debug($"Saved original order with {OriginalOrder.Count} coordinates.");
        }

        // Get player's current location
        var playerLocation = plugin.PlayerLocationService.GetCurrentLocation() ??
            // If player location is not available, create a default location
            new TreasureCoordinate(0, 0, string.Empty);

        // Group coordinates by map area
        var coordinatesByMap = Coordinates.GroupBy(c => c.MapArea).ToDictionary(g => g.Key, g => g.ToList());

        // Create a list to store the optimized route
        var route = new List<TreasureCoordinate>();

        // Keep track of the current location (starting with player's location)
        var currentLocation = playerLocation;
        var currentMapArea = playerLocation.MapArea;

        // Process all map areas until all coordinates are visited
        while (coordinatesByMap.Count > 0)
        {
            string nextMapArea;

            // If player is already in a map area with coordinates, prioritize that
            if (coordinatesByMap.ContainsKey(currentMapArea))
            {
                nextMapArea = currentMapArea;
                Plugin.Log.Debug($"Prioritizing current map area: {currentMapArea}");
            }
            else
            {
                // Find the best map area to visit next based on teleport costs and distances
                nextMapArea = FindBestMapAreaToVisit(currentLocation, coordinatesByMap);
                Plugin.Log.Debug($"Selected next map area: {nextMapArea}");
            }

            var mapCoordinates = coordinatesByMap[nextMapArea];

            // Get the aetheryte in this map area
            var mapAetheryte = plugin.AetheryteService.GetCheapestAetheryteInMapArea(nextMapArea);

            // Always use the aetheryte-based optimization, even if no aetheryte is found
            // This simplifies the code by removing the fallback path
            if (mapAetheryte == null)
            {
                // Create a default aetheryte at the center of the map
                mapAetheryte = new AetheryteInfo
                {
                    Name = "Default",
                    MapArea = nextMapArea,
                    Position = new System.Numerics.Vector2(50, 50)
                };
                Plugin.Log.Debug($"Created default aetheryte for map area {nextMapArea}");
            }

            // Create a coordinate for the aetheryte position
            var aetheryteCoord = new TreasureCoordinate(
                mapAetheryte.Position.X,
                mapAetheryte.Position.Y,
                mapAetheryte.MapArea);

            // If player is not in this map area, they need to teleport to the aetheryte first
            if (currentMapArea != nextMapArea)
            {
                Plugin.Log.Debug($"Player needs to teleport from {currentMapArea} to {nextMapArea} at aetheryte {mapAetheryte.Name} ({mapAetheryte.Position.X}, {mapAetheryte.Position.Y})");

                // Start from the aetheryte
                currentLocation = aetheryteCoord;
            }

            // Optimize the route within this map area, starting from current location
            var mapRoute = OptimizeRouteFromAetheryte(currentLocation, mapCoordinates, mapAetheryte);

            // Add the optimized route for this map area to the overall route
            route.AddRange(mapRoute);

            // Update the current location to the last coordinate in this map area
            currentLocation = mapRoute.Last();
            currentMapArea = currentLocation.MapArea;

            // Remove this map area from the dictionary
            coordinatesByMap.Remove(nextMapArea);
        }

        OptimizedRoute = route;

        // Raise the event
        OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
    }

    /// <summary>
    /// Finds the best map area to visit next based on teleport costs and distances.
    /// </summary>
    /// <param name="currentLocation">The current location.</param>
    /// <param name="coordinatesByMap">The coordinates grouped by map area.</param>
    /// <returns>The best map area to visit next.</returns>
    private string FindBestMapAreaToVisit(TreasureCoordinate currentLocation, Dictionary<string, List<TreasureCoordinate>> coordinatesByMap)
    {
        // If we're already in a map area with coordinates, prioritize that
        if (coordinatesByMap.ContainsKey(currentLocation.MapArea))
        {
            return currentLocation.MapArea;
        }

        // Calculate the cost (teleport fee + distance) for each map area
        var mapAreaCosts = new Dictionary<string, float>();

        // Calculate the number of coordinates in each map area
        var coordinatesPerMap = coordinatesByMap.ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        // Get the total number of coordinates
        var totalCoordinates = coordinatesPerMap.Values.Sum();

        foreach (var mapArea in coordinatesByMap.Keys)
        {
            // Get the cheapest aetheryte in this map area
            var cheapestAetheryte = plugin.AetheryteService.GetCheapestAetheryteInMapArea(mapArea);

            if (cheapestAetheryte == null)
            {
                // If no aetheryte is available, assign a high cost
                mapAreaCosts[mapArea] = 10000;
                continue;
            }

            // Calculate the teleport cost
            var teleportCost = cheapestAetheryte.CalculateTeleportFee();

            // Get all coordinates in this map area
            var mapCoordinates = coordinatesByMap[mapArea];

            // Find the nearest coordinate to the aetheryte
            var nearestCoordinate = mapCoordinates.OrderBy(c => cheapestAetheryte.DistanceTo(c)).First();

            // Calculate the distance from the aetheryte to the nearest coordinate
            var distanceToNearest = cheapestAetheryte.DistanceTo(nearestCoordinate);

            // Calculate the average distance between coordinates in this map area
            CalculateAverageDistanceBetweenCoordinates(mapCoordinates);

            // Calculate the density factor (more coordinates in an area = lower cost per coordinate)
            var densityFactor = 1.0f - ((float)coordinatesPerMap[mapArea] / totalCoordinates);

            // Combine teleport cost, distance to nearest coordinate, and density factor into a single cost metric
            // We convert gil to a distance equivalent (1 gil = 0.1 distance units)
            // We also consider the density factor to prioritize areas with more coordinates
            var totalCost = (distanceToNearest + (teleportCost * 0.1f)) * (1.0f + densityFactor);

            mapAreaCosts[mapArea] = totalCost;

            Plugin.Log.Debug($"Map area: {mapArea}, Teleport cost: {teleportCost}, Distance: {distanceToNearest}, " +
                           $"Density: {densityFactor}, Total cost: {totalCost}");
        }

        // Return the map area with the lowest cost
        return mapAreaCosts.OrderBy(kv => kv.Value).First().Key;
    }

    /// <summary>
    /// Calculates the average distance between coordinates in a list.
    /// </summary>
    /// <param name="coordinates">The list of coordinates.</param>
    /// <returns>The average distance.</returns>
    private void CalculateAverageDistanceBetweenCoordinates(List<TreasureCoordinate> coordinates)
    {
        if (coordinates.Count <= 1) return;

        // Calculate the distance between each pair of coordinates
        for (var i = 0; i < coordinates.Count; i++)
        {
            for (var j = i + 1; j < coordinates.Count; j++)
            {
                coordinates[i].DistanceTo(coordinates[j]);
            }
        }
    }

    /// <summary>
    /// Optimizes the route within a map area, starting from the aetheryte.
    /// </summary>
    /// <param name="startLocation">The starting location.</param>
    /// <param name="coordinates">The coordinates in the map area.</param>
    /// <param name="aetheryte">The aetheryte in the map area.</param>
    /// <returns>The optimized route within the map area.</returns>
    private List<TreasureCoordinate> OptimizeRouteFromAetheryte(TreasureCoordinate startLocation, List<TreasureCoordinate> coordinates, AetheryteInfo aetheryte)
    {
        if (coordinates.Count == 0)
            return new List<TreasureCoordinate>();

        if (coordinates.Count == 1)
            return new List<TreasureCoordinate>(coordinates);

        // Create a coordinate for the aetheryte position
        var aetheryteCoord = new TreasureCoordinate(
            aetheryte.Position.X,
            aetheryte.Position.Y,
            aetheryte.MapArea);

        // Create a new list to avoid modifying the original list
        var coordsToSort = new List<TreasureCoordinate>(coordinates);

        // Sort all coordinates by distance from the aetheryte or player's location
        List<TreasureCoordinate> sortedCoordinates;

        // If we're already in the same map area, start from the player's location
        if (startLocation.MapArea == coordinates[0].MapArea)
        {
            // Sort coordinates by distance from player's location
            sortedCoordinates = coordsToSort.OrderBy(c => c.DistanceTo(startLocation)).ToList();
        }
        // If we're teleporting to this map area, start from the aetheryte
        else
        {
            // CRITICAL FIX: Sort coordinates by distance from aetheryte
            // Create a dictionary to store distances to avoid recalculating
            var distanceDict = new Dictionary<TreasureCoordinate, float>();
            foreach (var coord in coordsToSort)
            {
                var distance = coord.DistanceTo(aetheryteCoord);
                distanceDict[coord] = distance;
            }

            // Sort using the pre-calculated distances
            sortedCoordinates = coordsToSort.OrderBy(c => distanceDict[c]).ToList();
        }

        // Removed debug logging and calculations to improve performance

        return sortedCoordinates;
    }

    // Removed OptimizeRouteInMapAreaSimple method as it's no longer needed

    // Removed OptimizeRouteSimple method as it's no longer needed

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
