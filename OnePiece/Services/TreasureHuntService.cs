using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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
    /// Resets the route optimization, restoring the original order and resetting all collection states.
    /// </summary>
    public void ResetRouteOptimization()
    {
        if (OriginalOrder.Count > 0)
        {
            // Restore the original order
            Coordinates.Clear();
            Coordinates.AddRange(OriginalOrder);

            // Reset collection state for all coordinates
            foreach (var coordinate in Coordinates)
            {
                coordinate.IsCollected = false;
            }

            // Clear the optimized route to indicate we're no longer in optimized mode
            OptimizedRoute.Clear();

            // Clear the original order list to avoid duplicate entries if optimized again
            OriginalOrder.Clear();

            // Log the reset
            Plugin.Log.Information($"Route optimization reset. Restored {Coordinates.Count} coordinates to original order and reset collection states.");

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

        if (Coordinates.Count == 0)
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

        Plugin.Log.Information($"Starting route optimization from player location: {playerLocation.MapArea} ({playerLocation.X:F1}, {playerLocation.Y:F1})");

        // Group coordinates by map area, excluding collected ones
        var coordinatesByMap = Coordinates
            .Where(c => !c.IsCollected)
            .GroupBy(c => c.MapArea)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (coordinatesByMap.Count == 0)
        {
            // If all coordinates are collected, just use the original list
            OptimizedRoute = new List<TreasureCoordinate>(Coordinates);
            OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
            return;
        }

        // Create a list to store the optimized route
        var route = new List<TreasureCoordinate>();

        // Keep track of the current location (starting with player's location)
        var currentLocation = playerLocation;
        var currentMapArea = playerLocation.MapArea;

        // Log initial state
        Plugin.Log.Debug($"Starting optimization with {coordinatesByMap.Count} map areas and {Coordinates.Count} total coordinates");
        foreach (var mapArea in coordinatesByMap.Keys)
        {
            Plugin.Log.Debug($"Map area '{mapArea}' has {coordinatesByMap[mapArea].Count} coordinates");
        }

        // Get all teleport costs upfront for better decision making
        var mapAreaTeleportCosts = GetAllMapAreaTeleportCosts(currentMapArea, coordinatesByMap.Keys.ToList());
        Plugin.Log.Debug($"Calculated teleport costs for {mapAreaTeleportCosts.Count} map areas");
        
        // Log teleport costs for better understanding
        foreach (var mapCost in mapAreaTeleportCosts)
        {
            Plugin.Log.Debug($"Teleport cost to {mapCost.Key}: {mapCost.Value} gil");
        }

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
                nextMapArea = FindBestMapAreaToVisit(currentLocation, coordinatesByMap, mapAreaTeleportCosts);
                uint cost = 0;
                if (mapAreaTeleportCosts.TryGetValue(nextMapArea, out var mapCost))
                {
                    cost = mapCost;
                }
                Plugin.Log.Debug($"Selected next map area: {nextMapArea}, teleport cost: {cost} gil");
            }

            var mapCoordinates = coordinatesByMap[nextMapArea];

            // Get all aetherytes in this map area
            var mapAetherytes = plugin.AetheryteService.GetAetherytesInMapArea(nextMapArea).ToList();
            
            // Update teleport fees for all aetherytes in this map area
            if (mapAetherytes.Count > 0)
            {
                plugin.AetheryteService.UpdateTeleportFees(mapAetherytes);
                
                // Log aetherytes in this map area
                foreach (var aetheryte in mapAetherytes)
                {
                    Plugin.Log.Debug($"Aetheryte in {nextMapArea}: {aetheryte.Name} at ({aetheryte.Position.X:F1}, {aetheryte.Position.Y:F1}), fee: {aetheryte.CalculateTeleportFee()} gil");
                }
            }
            
            // Get the best aetheryte in this map area, considering distance to treasure coordinates
            var mapAetheryte = FindBestAetheryteForCoordinates(mapAetherytes, mapCoordinates);
            
            // If no aetheryte is found, create a default one for algorithm purposes
            if (mapAetheryte == null)
            {
                mapAetheryte = new AetheryteInfo
                {
                    Name = "Default",
                    MapArea = nextMapArea,
                    Position = new System.Numerics.Vector2(50, 50)
                };
                Plugin.Log.Debug($"Created default aetheryte for map area {nextMapArea}");
            }
            else
            {
                Plugin.Log.Debug($"Using aetheryte {mapAetheryte.Name} with teleport fee {mapAetheryte.CalculateTeleportFee()} gil");
            }

            // If player is not in this map area, they need to teleport to the aetheryte first
            if (currentMapArea != nextMapArea)
            {
                if (mapAetheryte != null)
                {
                    Plugin.Log.Debug($"Player needs to teleport from {currentMapArea} to {nextMapArea} at aetheryte {mapAetheryte.Name} ({mapAetheryte.Position.X:F1}, {mapAetheryte.Position.Y:F1})");
                }
                
                // Create a coordinate for the aetheryte position
                var aetheryteCoord = new TreasureCoordinate(
                    mapAetheryte.Position.X,
                    mapAetheryte.Position.Y,
                    mapAetheryte.MapArea);
                
                // Start from the aetheryte after teleporting
                currentLocation = aetheryteCoord;
            }

            // Optimize the route within this map area, starting from current location
            var mapRoute = OptimizeRouteFromAetheryte(currentLocation, mapCoordinates, mapAetheryte);

            // Add the optimized route for this map area to the overall route
            if (mapRoute.Count > 0)
            {
                // We've changed areas, but we're not adding teleport information to the coordinate name anymore
                // This improves the clarity of the displayed information
                
                route.AddRange(mapRoute);
                
                // Update the current location to the last coordinate in this map area
                currentLocation = mapRoute.Last();
                currentMapArea = currentLocation.MapArea;

                // Update teleport costs if there are more areas to visit
                if (coordinatesByMap.Count > 1)
                {
                    mapAreaTeleportCosts = GetAllMapAreaTeleportCosts(currentMapArea, coordinatesByMap.Keys.ToList());
                    Plugin.Log.Debug($"Updated teleport costs from new location: {currentMapArea}");
                }
            }

            // Remove this map area from the dictionary
            coordinatesByMap.Remove(nextMapArea);
        }

        // Add any collected coordinates to the end of the route (maintaining their original order)
        var collectedCoordinates = Coordinates.Where(c => c.IsCollected).ToList();
        route.AddRange(collectedCoordinates);

        OptimizedRoute = route;

        // Log the optimized route
        Plugin.Log.Information($"Optimized route contains {OptimizedRoute.Count} coordinates across {OptimizedRoute.Select(c => c.MapArea).Distinct().Count()} map areas");
        
        // Raise the event
        OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
    }

    /// <summary>
    /// Finds the best map area to visit next based on teleport costs and distances.
    /// </summary>
    /// <param name="currentLocation">The current location.</param>
    /// <param name="coordinatesByMap">The coordinates grouped by map area.</param>
    /// <param name="mapAreaTeleportCosts">Dictionary of map areas to their teleport costs.</param>
    /// <returns>The best map area to visit next.</returns>
    private string FindBestMapAreaToVisit(
        TreasureCoordinate currentLocation, 
        Dictionary<string, List<TreasureCoordinate>> coordinatesByMap,
        Dictionary<string, uint> mapAreaTeleportCosts)
    {
        // If no map areas, return empty string
        if (coordinatesByMap.Count == 0)
            return string.Empty;
            
        // If we're already in a map area with coordinates, prioritize that
        if (!string.IsNullOrEmpty(currentLocation.MapArea) && coordinatesByMap.ContainsKey(currentLocation.MapArea))
        {
            return currentLocation.MapArea;
        }

        // Calculate the score for each map area (lower is better)
        var mapAreaScores = new Dictionary<string, float>();

        // Calculate the number of coordinates in each map area
        var coordinatesPerMap = coordinatesByMap.ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        // Get the total number of coordinates
        var totalCoordinates = coordinatesPerMap.Values.Sum();

        foreach (var mapArea in coordinatesByMap.Keys)
        {
            // Get the teleport cost from the pre-calculated dictionary
            uint teleportCost = uint.MaxValue;
            if (mapAreaTeleportCosts.TryGetValue(mapArea, out var cost))
            {
                teleportCost = cost;
            }
            
            // Get all coordinates in this map area
            var mapCoordinates = coordinatesByMap[mapArea];

            // Get all aetherytes in this map area
            var aetherytesInMap = plugin.AetheryteService.GetAetherytesInMapArea(mapArea);
            var bestAetheryte = FindBestAetheryteForCoordinates(aetherytesInMap.ToList(), coordinatesByMap[mapArea]);
            float distanceToNearest = 0;

            if (bestAetheryte != null)
            {
                // Calculate distances from aetheryte to each coordinate
                var aetheryteCoord = new TreasureCoordinate(
                    bestAetheryte.Position.X,
                    bestAetheryte.Position.Y,
                    mapArea);
                
                // Find the nearest coordinate to the aetheryte
                var nearestCoordinate = mapCoordinates.OrderBy(c => aetheryteCoord.DistanceTo(c)).FirstOrDefault();

                if (nearestCoordinate != null)
                {
                    distanceToNearest = aetheryteCoord.DistanceTo(nearestCoordinate);
                    Plugin.Log.Debug($"Distance from aetheryte {bestAetheryte.Name} to nearest coordinate: {distanceToNearest:F1}");
                }
            }

            // Normalize teleport cost (higher cost = higher score)
            // Teleport cost is the main factor, so it's given higher weight
            float teleportCostFactor = teleportCost / 200f; // Increase the weight of teleport cost, reduce the normalization range

            // Calculate average distance between coordinates in the map
            float averageDistance = CalculateAverageDistanceBetweenCoordinates(mapCoordinates);

            // Calculate the ratio of coordinates in this map to total coordinates
            float coordinatesRatio = (float)mapCoordinates.Count / totalCoordinates;

            // Calculate final score (lower is better)
            // Significantly increase the weight of teleport cost in scoring, ensuring maps with lower teleport fees get higher priority
            // While still considering coordinate density and distance factors, but with lower weights
            float score = teleportCostFactor * 3 - (coordinatesRatio * 1.5f) + (distanceToNearest / 100);

            mapAreaScores[mapArea] = score;
            Plugin.Log.Debug($"Map area {mapArea} score: {score:F2} (teleport: {teleportCost}, coords: {mapCoordinates.Count}, distance: {distanceToNearest:F1})");
        }

        // Return the map area with the lowest score (best choice)
        if (mapAreaScores.Count > 0)
        {
            var bestMapArea = mapAreaScores.OrderBy(kv => kv.Value).First().Key;
            Plugin.Log.Information($"Selected best map area: {bestMapArea} with score: {mapAreaScores[bestMapArea]}");
            return bestMapArea;
        }

        // Fallback to first map area if scoring fails
        return coordinatesByMap.Keys.First();
    }
    
    /// <summary>
    /// Finds the best aetheryte for a set of coordinates, prioritizing the distance to the coordinates.
    /// </summary>
    /// <param name="aetherytes">List of available aetherytes.</param>
    /// <param name="coordinates">List of treasure coordinates to visit.</param>
    /// <returns>The best aetheryte, or null if no aetherytes are available.</returns>
    private AetheryteInfo FindBestAetheryteForCoordinates(List<AetheryteInfo> aetherytes, List<TreasureCoordinate> coordinates)
    {
        if (aetherytes == null || aetherytes.Count == 0)
        {
            return null;
        }
        
        // If there's only one aetheryte, return it
        if (aetherytes.Count == 1)
        {
            return aetherytes[0];
        }
        
        // If there are no coordinates, return the cheapest aetheryte
        if (coordinates == null || coordinates.Count == 0)
        {
            return aetherytes.OrderBy(a => a.CalculateTeleportFee()).First();
        }
        
        // Dictionary to store the average distance from each aetheryte to all coordinates
        var aetheryteScores = new Dictionary<AetheryteInfo, float>();
        
        foreach (var aetheryte in aetherytes)
        {
            // Create a coordinate object for the aetheryte position
            var aetheryteCoord = new TreasureCoordinate(
                aetheryte.Position.X,
                aetheryte.Position.Y,
                aetheryte.MapArea);
            
            // Calculate the minimum distance to any coordinate
            float minDistance = float.MaxValue;
            float totalDistance = 0;
            float averageDistance = 0;
            
            // Get the distance to the nearest coordinate and average distance to all coordinates
            foreach (var coordinate in coordinates)
            {
                float distance = aetheryteCoord.DistanceTo(coordinate);
                totalDistance += distance;
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
            
            averageDistance = totalDistance / coordinates.Count;
            
            // Calculate a score that prioritizes distance but still considers teleport cost
            // Lower is better
            float teleportCostFactor = aetheryte.CalculateTeleportFee() / 1000f; // Normalize to 0-1 range typically
            float score = minDistance + (averageDistance * 0.5f) + (teleportCostFactor * 20); // Weighted formula
            
            aetheryteScores[aetheryte] = score;
            
            Plugin.Log.Debug($"Aetheryte {aetheryte.Name} score: {score:F2} (min distance: {minDistance:F1}, avg distance: {averageDistance:F1}, teleport: {aetheryte.CalculateTeleportFee()})");
        }
        
        // Return the aetheryte with the lowest score (best combination of distance and cost)
        return aetheryteScores.OrderBy(kvp => kvp.Value).First().Key;
    }

    /// <summary>
    /// Calculates the average distance between coordinates in a list.
    /// </summary>
    /// <param name="coordinates">The list of coordinates.</param>
    /// <returns>The average distance between coordinates.</returns>
    private float CalculateAverageDistanceBetweenCoordinates(List<TreasureCoordinate> coordinates)
    {
        if (coordinates.Count <= 1) return 0;

        float totalDistance = 0;
        int pairCount = 0;

        // Calculate the distance between each pair of coordinates
        for (var i = 0; i < coordinates.Count; i++)
        {
            for (var j = i + 1; j < coordinates.Count; j++)
            {
                totalDistance += coordinates[i].DistanceTo(coordinates[j]);
                pairCount++;
            }
        }

        // Return the average distance
        return pairCount > 0 ? totalDistance / pairCount : 0;
    }

    /// <summary>
    /// Optimizes the route within a map area, considering either starting from the player's location or teleporting to an aetheryte.
    /// Prioritizes coordinates closer to teleport points, and teleports between coordinates as needed.
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

        // Create a new list to avoid modifying the original list
        var coordsToSort = new List<TreasureCoordinate>(coordinates);
        List<TreasureCoordinate> optimizedPath = new List<TreasureCoordinate>();

        // Create a coordinate for the aetheryte position
        var aetheryteCoord = aetheryte != null
            ? new TreasureCoordinate(aetheryte.Position.X, aetheryte.Position.Y, aetheryte.MapArea)
            : new TreasureCoordinate(0, 0, string.Empty);

        // Determine if we're already in the same map area
        bool inSameMapArea = startLocation.MapArea == coordinates[0].MapArea;
        var currentPos = inSameMapArea ? startLocation : aetheryteCoord;
        
        // Track if we need to indicate a teleport to aetheryte before the next coordinate
        bool shouldTeleportToAetheryte = false;

        // Distance threshold factor - if distance between points is greater than this factor * average distance, consider teleporting
        const float distanceThresholdFactor = 2.0f;
        
        // Calculate the average distance between coordinates to determine when teleporting makes sense
        float avgDistance = CalculateAverageDistanceBetweenCoordinates(coordinates);
        float teleportThreshold = avgDistance * distanceThresholdFactor;

        Plugin.Log.Debug($"Average distance between coordinates: {avgDistance}, Teleport threshold: {teleportThreshold}");

        // Pre-calculate distances from all coordinates to the aetheryte for later optimization
        var distancesToAetheryte = new Dictionary<TreasureCoordinate, float>();
        foreach (var coord in coordsToSort)
        {
            distancesToAetheryte[coord] = aetheryteCoord.DistanceTo(coord);
        }
        
        // If the initial position is near the aetheryte, prioritize coordinates closer to the aetheryte
        if (!inSameMapArea || currentPos.DistanceTo(aetheryteCoord) < 1.0f)
        {
            // First sort coordinates based on their distance to the aetheryte
            coordsToSort = coordsToSort.OrderBy(c => distancesToAetheryte[c]).ToList();
            Plugin.Log.Debug("Initializing path, prioritizing coordinates closer to the aetheryte");
        }

        // Build the optimized path
        while (coordsToSort.Count > 0)
        {
            // When selecting the next coordinate, consider both distance from current position and distance to the aetheryte
            var nextCoordCandidates = coordsToSort.Select(c => new
            {
                Coordinate = c,
                DistanceToCurrent = c.DistanceTo(currentPos),
                DistanceToAetheryte = distancesToAetheryte[c],
                // Calculate a combined score: weighted value of distance from current position and distance to aetheryte
                // Weights can be adjusted, here using 0.7:0.3 meaning distance to current position has 70% importance, distance to aetheryte has 30%
                Score = c.DistanceTo(currentPos) * 0.7f + distancesToAetheryte[c] * 0.3f
            }).OrderBy(x => x.Score).ToList();
            
            var nextCoordInfo = nextCoordCandidates.First();
            var nextCoord = nextCoordInfo.Coordinate;
            float directDistance = nextCoordInfo.DistanceToCurrent;
            
            Plugin.Log.Debug($"Selected coordinate: ({nextCoord.X}, {nextCoord.Y}), distance from current: {directDistance:F1}, distance to aetheryte: {nextCoordInfo.DistanceToAetheryte:F1}, score: {nextCoordInfo.Score:F1}");

            // If the distance from current position is too far, consider if teleporting to the aetheryte would be more efficient
            if (aetheryte != null && directDistance > teleportThreshold)
            {
                // Distance to reach the target via the aetheryte
                float aetheryteDistance = distancesToAetheryte[nextCoord];
                
                // If the path via aetheryte is significantly shorter, choose to teleport
                if (aetheryteDistance < directDistance * 0.7f)
                {
                    // Mark that we need to teleport to the aetheryte first
                    shouldTeleportToAetheryte = true;
                    currentPos = aetheryteCoord;
                    
                    // Find the nearest coordinate from the aetheryte
                    nextCoordCandidates = coordsToSort.Select(c => new
                    {
                        Coordinate = c,
                        DistanceToCurrent = aetheryteCoord.DistanceTo(c),
                        DistanceToAetheryte = distancesToAetheryte[c],
                        Score = aetheryteCoord.DistanceTo(c)
                    }).OrderBy(x => x.Score).ToList();
                    
                    nextCoordInfo = nextCoordCandidates.First();
                    nextCoord = nextCoordInfo.Coordinate;

                    Plugin.Log.Debug($"Decided to teleport to the aetheryte first, then proceed to coordinate ({nextCoord.X}, {nextCoord.Y}), distance: {nextCoordInfo.DistanceToCurrent:F1}");
                }
            }

            // Add coordinate to path
            nextCoord.Name = shouldTeleportToAetheryte ? $"[Teleport] {nextCoord.Name}" : nextCoord.Name;
            optimizedPath.Add(nextCoord);
            coordsToSort.Remove(nextCoord);
            currentPos = nextCoord;
            shouldTeleportToAetheryte = false;

            // Check if we should consider teleporting to the aetheryte for remaining coordinates
            if (coordsToSort.Count > 0 && aetheryte != null)
            {
                // Find the nearest remaining coordinate to the current position
                var nearestRemaining = coordsToSort.OrderBy(c => c.DistanceTo(currentPos)).First();
                float directDistanceToNext = currentPos.DistanceTo(nearestRemaining);
                
                // Find the nearest remaining coordinate to the aetheryte
                var nearestToAetheryte = coordsToSort.OrderBy(c => distancesToAetheryte[c]).First();
                float aetheryteDistanceToNearest = distancesToAetheryte[nearestToAetheryte];
                
                // If direct distance is too large and the aetheryte provides a better starting point
                if (directDistanceToNext > teleportThreshold && aetheryteDistanceToNearest < directDistanceToNext * 0.8f)
                {
                    shouldTeleportToAetheryte = true;
                    currentPos = aetheryteCoord;
                    Plugin.Log.Debug($"Will teleport back to the aetheryte to continue finding coordinates. Direct distance: {directDistanceToNext:F1}, via aetheryte: {aetheryteDistanceToNearest:F1}");
                }
            }
        }

        return optimizedPath;
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
    
    /// <summary>
    /// Gets teleport costs for all map areas from the current map area.
    /// </summary>
    /// <param name="currentMapArea">The current map area.</param>
    /// <param name="targetMapAreas">List of target map areas.</param>
    /// <returns>Dictionary of map areas to their teleport costs.</returns>
    private Dictionary<string, uint> GetAllMapAreaTeleportCosts(string currentMapArea, List<string> targetMapAreas)
    {
        var costs = new Dictionary<string, uint>();
        
        foreach (var mapArea in targetMapAreas)
        {
            if (string.IsNullOrEmpty(mapArea))
            {
                costs[mapArea] = uint.MaxValue;
                continue;
            }
            
            if (mapArea == currentMapArea)
            {
                // If it's the same map area, the teleport cost to other aetherytes is 70 gil
                costs[mapArea] = 70;
                continue;
            }
            
            // Get the cheapest aetheryte in this map area
            var cheapestAetheryte = plugin.AetheryteService.GetCheapestAetheryteInMapArea(mapArea);
            
            if (cheapestAetheryte != null)
            {
                // Update the teleport fee using the Telepo API
                plugin.AetheryteService.UpdateTeleportFees(new[] { cheapestAetheryte });
                
                // Get the actual teleport cost - this will use the FFXIVClientStructs.FFXIV.Client.Game.UI.Telepo API
                var teleportCost = (uint)cheapestAetheryte.CalculateTeleportFee();
                
                // Use dynamic teleport cost from Telepo if available, otherwise use the base cost
                unsafe
                {
                    var telepo = Telepo.Instance();
                    if (telepo != null && cheapestAetheryte.AetheryteRowId > 0)
                    {
                        try
                        {
                            // Try to get real-time teleport cost from Telepo API
                            var dynamicCost = GetTeleportCostFromTelepo(cheapestAetheryte.AetheryteRowId);
                            if (dynamicCost > 0)
                            {
                                teleportCost = dynamicCost;
                                Plugin.Log.Debug($"Using dynamic teleport cost for {mapArea}: {teleportCost} gil");
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error($"Error getting teleport cost from Telepo: {ex.Message}");
                        }
                    }
                }
                
                costs[mapArea] = teleportCost;
            }
            else
            {
                // If no aetheryte found, assign a very high cost
                costs[mapArea] = uint.MaxValue;
            }
        }
        
        return costs;
    }
    
    /// <summary>
    /// Gets the teleport cost for a specific aetheryte ID using the Telepo API.
    /// </summary>
    /// <param name="aetheryteId">The aetheryte ID.</param>
    /// <returns>The teleport cost in gil.</returns>
    private unsafe uint GetTeleportCostFromTelepo(uint aetheryteId)
    {
        var telepo = Telepo.Instance();
        if (telepo == null)
            return 0;

        // Update the aetheryte list to ensure we have current data
        telepo->UpdateAetheryteList();

        // Search for the aetheryte in the teleport list
        int count = telepo->TeleportList.Count;
        for (int i = 0; i < count; i++)
        {
            var info = telepo->TeleportList[i];
            if (info.AetheryteId == aetheryteId)
            {
                return info.GilCost;
            }
        }

        return 0;
    }
}
