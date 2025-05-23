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

        // Get all teleport costs upfront for better decision making
        var mapAreaTeleportCosts = GetAllMapAreaTeleportCosts(currentMapArea, coordinatesByMap.Keys.ToList());
        Plugin.Log.Debug($"Calculated teleport costs for {mapAreaTeleportCosts.Count} map areas");

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

            // Get the cheapest aetheryte in this map area
            var mapAetheryte = plugin.AetheryteService.GetCheapestAetheryteInMapArea(nextMapArea);
            
            // Update teleport fee using real-time data from Telepo API
            if (mapAetheryte != null)
            {
                plugin.AetheryteService.UpdateTeleportFees(new[] { mapAetheryte });
                Plugin.Log.Debug($"Using aetheryte {mapAetheryte.Name} with teleport fee {mapAetheryte.CalculateTeleportFee()} gil");
            }
            // If no aetheryte is found, create a default one
            else
            {
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
            if (mapRoute.Count > 0)
            {
                currentLocation = mapRoute.Last();
                currentMapArea = currentLocation.MapArea;

                // Update teleport costs if we've moved to a new map area and there are more areas to visit
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

            // Get the cheapest aetheryte in this map area for distance calculations
            var cheapestAetheryte = plugin.AetheryteService.GetCheapestAetheryteInMapArea(mapArea);
            float distanceToNearest = 0;

            if (cheapestAetheryte != null)
            {
                // Find the nearest coordinate to the aetheryte
                var nearestCoordinate = mapCoordinates.OrderBy(c => cheapestAetheryte.DistanceTo(c)).First();

                // Calculate the distance from the aetheryte to the nearest coordinate
                distanceToNearest = cheapestAetheryte.DistanceTo(nearestCoordinate);
            }

            // Calculate the average distance between coordinates in this map area
            CalculateAverageDistanceBetweenCoordinates(mapCoordinates);

            // Calculate the density factor (more coordinates in an area = lower cost per coordinate)
            var densityFactor = 1.0f - ((float)coordinatesPerMap[mapArea] / totalCoordinates);

            // Calculate the cost per coordinate (prioritize areas with more coordinates and lower teleport costs)
            var costPerCoordinate = teleportCost / (float)mapCoordinates.Count;

            // Combine teleport cost, distance, and density into a single score
            // Lower scores are better
            var score = costPerCoordinate * (1.0f + densityFactor * 0.5f) + (distanceToNearest * 0.1f);
            
            mapAreaScores[mapArea] = score;

            Plugin.Log.Debug($"Map area: {mapArea}, Teleport cost: {teleportCost}, Coordinates: {mapCoordinates.Count}, " +
                           $"Distance: {distanceToNearest}, Density factor: {densityFactor}, Score: {score}");
        }

        // Return the map area with the lowest score
        if (mapAreaScores.Count > 0)
            return mapAreaScores.OrderBy(kv => kv.Value).First().Key;

        // Fallback to first map area if scoring fails
        return coordinatesByMap.Keys.First();
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
    /// Optimizes the route within a map area, considering either starting from the player's location or teleporting to an aetheryte.
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

        // If we're already in the same map area, start from the player's location
        if (startLocation.MapArea == coordinates[0].MapArea)
        {
            var currentPos = startLocation;
            
            // Build the path by repeatedly finding the nearest unvisited coordinate
            while (coordsToSort.Count > 0)
            {
                // Find the nearest coordinate to the current position
                var nearest = coordsToSort.OrderBy(c => c.DistanceTo(currentPos)).First();
                optimizedPath.Add(nearest);
                coordsToSort.Remove(nearest);
                currentPos = nearest;
            }
        }
        // If we're teleporting to this map area, consider both options: 
        // 1. Going directly to each treasure from aetheryte
        // 2. Creating a path between treasures when they're close to each other
        else if (aetheryte != null)
        {
            // Create a coordinate for the aetheryte position
            var aetheryteCoord = new TreasureCoordinate(
                aetheryte.Position.X,
                aetheryte.Position.Y,
                aetheryte.MapArea);
            
            // Start with the nearest coordinate to the aetheryte
            var nearest = coordsToSort.OrderBy(c => c.DistanceTo(aetheryteCoord)).First();
            optimizedPath.Add(nearest);
            coordsToSort.Remove(nearest);
            
            var currentPos = nearest;
            
            // For each remaining coordinate, decide whether to teleport to aetheryte first or go directly
            while (coordsToSort.Count > 0)
            {
                // Find the nearest unvisited coordinate to current position
                var nextNearest = coordsToSort.OrderBy(c => c.DistanceTo(currentPos)).First();
                var distanceDirectly = currentPos.DistanceTo(nextNearest);
                
                // Calculate distance if we teleport to aetheryte first (70 gil cost within same map)
                // Distance from aetheryte to the next coordinate
                var distanceViaAetheryte = aetheryteCoord.DistanceTo(nextNearest);
                
                // If going directly is shorter than going via aetheryte, go directly
                if (distanceDirectly <= distanceViaAetheryte)
                {
                    optimizedPath.Add(nextNearest);
                    currentPos = nextNearest;
                }
                // Otherwise, consider cost of teleporting (70 gil within same map)
                else
                {
                    // Adding a virtual "return to aetheryte" point would be ideal here,
                    // but for simplicity we'll just update the current position and continue
                    // In a full implementation, you might want to mark this as a teleport point
                    currentPos = aetheryteCoord;
                    
                    // Now find the nearest coordinate to the aetheryte
                    nextNearest = coordsToSort.OrderBy(c => c.DistanceTo(currentPos)).First();
                    optimizedPath.Add(nextNearest);
                    currentPos = nextNearest;
                }
                
                coordsToSort.Remove(nextNearest);
            }
        }
        // Fallback if no aetheryte is available
        else
        {
            // Simple nearest neighbor approach starting from an arbitrary point
            var currentPos = coordinates[0];
            optimizedPath.Add(currentPos);
            coordsToSort.Remove(currentPos);
            
            while (coordsToSort.Count > 0)
            {
                var nearest = coordsToSort.OrderBy(c => c.DistanceTo(currentPos)).First();
                optimizedPath.Add(nearest);
                coordsToSort.Remove(nearest);
                currentPos = nearest;
            }
        }

        return optimizedPath;
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
