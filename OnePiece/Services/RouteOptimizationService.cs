using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for optimizing routes through treasure coordinates.
/// </summary>
public class RouteOptimizationService
{
    private readonly Plugin plugin;
    private readonly TimeBasedPathFinder pathFinder;

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
    /// Event raised when a route is optimized.
    /// </summary>
    public event EventHandler<int>? OnRouteOptimized;

    /// <summary>
    /// Event raised when a route optimization is reset.
    /// </summary>
    public event EventHandler? OnRouteOptimizationReset;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteOptimizationService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public RouteOptimizationService(Plugin plugin)
    {
        this.plugin = plugin;
        this.pathFinder = new TimeBasedPathFinder(plugin);
    }

    /// <summary>
    /// Optimizes the route through the coordinates considering player location, teleport costs, and travel distances.
    /// </summary>
    /// <param name="coordinates">The coordinates to optimize.</param>
    public List<TreasureCoordinate> OptimizeRoute(List<TreasureCoordinate> coordinates)
    {
        // Always save the original order before optimization to enable reset functionality
        // This fixes the edge case where reset wasn't possible with 0-1 coordinates
        if (OriginalOrder.Count == 0)
        {
            OriginalOrder = new List<TreasureCoordinate>(coordinates);
            Plugin.Log.Debug($"Saved original order with {OriginalOrder.Count} coordinates.");
        }

        // Clear any existing teleport settings from previous optimizations
        foreach (var coord in coordinates)
        {
            coord.AetheryteId = 0;
            coord.Type = CoordinateType.TreasurePoint;
            coord.NavigationInstruction = string.Empty;
        }
        
        if (coordinates.Count <= 1)
        {
            OptimizedRoute = new List<TreasureCoordinate>(coordinates);
            OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
            return OptimizedRoute;
        }

        // Get player's current location
        var playerLocation = plugin.PlayerLocationService.GetCurrentLocation() ??
            // If player location is not available, create a default location
            new TreasureCoordinate(0, 0, string.Empty);

        Plugin.Log.Information($"Starting route optimization from player location: {playerLocation.MapArea} ({playerLocation.X:F1}, {playerLocation.Y:F1})");

        // Group coordinates by map area, excluding collected ones
        var coordinatesByMap = coordinates
            .Where(c => !c.IsCollected)
            .GroupBy(c => c.MapArea)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (coordinatesByMap.Count == 0)
        {
            // If all coordinates are collected, just use the original list
            OptimizedRoute = new List<TreasureCoordinate>(coordinates);
            OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
            return OptimizedRoute;
        }

        // Create a list to store the optimized route
        var route = new List<TreasureCoordinate>();

        // Keep track of the current location (starting with player's location)
        var currentLocation = playerLocation;
        var currentMapArea = playerLocation.MapArea;

#if DEBUG
        // Log initial state
        Plugin.Log.Debug($"Starting optimization with {coordinatesByMap.Count} map areas and {coordinates.Count} total coordinates");
        foreach (var mapArea in coordinatesByMap.Keys)
        {
            Plugin.Log.Debug($"Map area '{mapArea}' has {coordinatesByMap[mapArea].Count} coordinates");
        }
#endif

        // Get all teleport costs upfront for better decision making
        var mapAreaTeleportCosts = GetAllMapAreaTeleportCosts(currentMapArea, coordinatesByMap.Keys.ToList());
        
#if DEBUG
        Plugin.Log.Debug($"Calculated teleport costs for {mapAreaTeleportCosts.Count} map areas");
        
        // Log teleport costs for better understanding
        foreach (var mapCost in mapAreaTeleportCosts)
        {
            Plugin.Log.Debug($"Teleport cost to {mapCost.Key}: {mapCost.Value} gil");
        }
#endif

        // Process all map areas until all coordinates are visited
        while (coordinatesByMap.Count > 0)
        {
            string nextMapArea;

            // If player is already in a map area with coordinates, prioritize that
            if (coordinatesByMap.ContainsKey(currentMapArea))
            {
                nextMapArea = currentMapArea;
#if DEBUG
                Plugin.Log.Debug($"Prioritizing current map area: {currentMapArea}");
#endif
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
#if DEBUG
                Plugin.Log.Debug($"Selected next map area: {nextMapArea}, teleport cost: {cost} gil");
#endif
            }

            var mapCoordinates = coordinatesByMap[nextMapArea];

            // Get all aetherytes in this map area
            var mapAetherytes = plugin.AetheryteService.GetAetherytesInMapArea(nextMapArea).ToList();
            
            // Update teleport fees for all aetherytes in this map area
            if (mapAetherytes.Count > 0)
            {
                plugin.AetheryteService.UpdateTeleportFees(mapAetherytes);
                
#if DEBUG
                // Log aetherytes in this map area
                foreach (var aetheryte in mapAetherytes)
                {
                    Plugin.Log.Debug($"Aetheryte in {nextMapArea}: {aetheryte.Name} at ({aetheryte.Position.X:F1}, {aetheryte.Position.Y:F1}), fee: {aetheryte.CalculateTeleportFee()} gil");
                }
#endif
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
#if DEBUG
                Plugin.Log.Debug($"Using aetheryte {mapAetheryte.Name} with teleport fee {mapAetheryte.CalculateTeleportFee()} gil");
#endif
            }

            // Check if player needs to teleport to the map area
            bool needsTeleport = false;
            
            if (currentMapArea != nextMapArea)
            {
                // Different map - definitely need to teleport
                needsTeleport = true;
                if (mapAetheryte != null)
                {
                    Plugin.Log.Debug($"Player needs to teleport from {currentMapArea} to {nextMapArea} at aetheryte {mapAetheryte.Name} ({mapAetheryte.Position.X:F1}, {mapAetheryte.Position.Y:F1})");
                }
            }
            else if (mapCoordinates.Count > 0)
            {
                // Same map - check if player is far from all coordinates and closer to an aetheryte
                bool isCloseToAnyTarget = false;
                float closestTargetDistance = float.MaxValue;
                float closestAetheryteDistance = float.MaxValue;
                
                // Find distance to closest target and closest aetheryte
                foreach (var coord in mapCoordinates)
                {
                    // Distance calculation now automatically handles coordinate system differences
                    float distance = currentLocation.DistanceTo(coord);
                    
#if DEBUG
                    Plugin.Log.Debug($"Distance from player {currentLocation} to coordinate {coord}: {distance:F2} units");
#endif
                    
                    if (distance < closestTargetDistance)
                        closestTargetDistance = distance;
                        
                    // If player is within 20 units of any target, consider them close
                    if (distance < 20.0f)
                        isCloseToAnyTarget = true;
                }
                
                if (mapAetheryte != null)
                {
                    var aetherytePos = new TreasureCoordinate(
                        mapAetheryte.Position.X,
                        mapAetheryte.Position.Y,
                        mapAetheryte.MapArea,
                        CoordinateSystemType.Map);
                    
                    // Distance calculation now automatically handles coordinate system differences
                    closestAetheryteDistance = currentLocation.DistanceTo(aetherytePos);
                    
#if DEBUG
                    Plugin.Log.Debug($"Distance from player {currentLocation} to aetheryte {mapAetheryte.Name}: {closestAetheryteDistance:F2} units");
#endif
                }
                
                // Only teleport if player is not close to any target and closer to aetheryte than targets
                if (!isCloseToAnyTarget && closestAetheryteDistance < closestTargetDistance)
                {
                    needsTeleport = true;
#if DEBUG
                    Plugin.Log.Debug($"Player is in same map but far from targets ({closestTargetDistance:F1} units) and closer to aetheryte ({closestAetheryteDistance:F1} units)");
#endif
                }
                else
                {
#if DEBUG
                    Plugin.Log.Debug($"Player is already in same map and {(isCloseToAnyTarget ? "close to a target" : "closer to targets than aetheryte")}. Distance to closest target: {closestTargetDistance:F1} units");
#endif
                }
            }
            
            // Get all aetherytes in the current map area for better path optimization
            var aetherytesInMap = plugin.AetheryteService.GetAetherytesInMapArea(nextMapArea);
            List<AetheryteInfo> allMapAetherytes;

            if (aetherytesInMap == null || !aetherytesInMap.Any())
            {
                // If no aetherytes found in the map area, use the provided one
                allMapAetherytes = new List<AetheryteInfo> { mapAetheryte };
            }
            else
            {
                // Convert IReadOnlyList to List for compatibility
                allMapAetherytes = aetherytesInMap.ToList();
            }

            // Use time-based path optimization algorithm to optimize the route with all available aetherytes
            Plugin.Log.Information($"Calling OptimizeRouteByTime for map '{nextMapArea}' with currentLocation: {currentLocation.MapArea} ({currentLocation.X:F1}, {currentLocation.Y:F1}), needsTeleport: {needsTeleport}");

            List<TreasureCoordinate> mapRoute;
            if (needsTeleport && mapAetheryte != null)
            {
                // Create a coordinate for the aetheryte position
                var aetheryteCoord = new TreasureCoordinate(
                    mapAetheryte.Position.X,
                    mapAetheryte.Position.Y,
                    mapAetheryte.MapArea,
                    CoordinateSystemType.Map);

                // Call OptimizeRouteByTime with forced teleport
                mapRoute = pathFinder.OptimizeRouteByTime(aetheryteCoord, mapCoordinates, allMapAetherytes, true, mapAetheryte);

                // Update current location to the aetheryte after teleporting
                currentLocation = aetheryteCoord;
            }
            else
            {
                // No teleport needed, use current location
                mapRoute = pathFinder.OptimizeRouteByTime(currentLocation, mapCoordinates, allMapAetherytes);
            }

            Plugin.Log.Information($"OptimizeRouteByTime returned {mapRoute.Count} coordinates for map '{nextMapArea}'");
            
            // Estimate the time needed to complete this part of the route
            float estimatedTime = pathFinder.EstimateCompletionTime(mapRoute);
            
            // Count actual treasure points (excluding teleport points)
            int actualTreasurePoints = mapRoute.Count(c => c.Type == CoordinateType.TreasurePoint);
            int teleportPoints = mapRoute.Count(c => c.Type == CoordinateType.TeleportPoint);
            
            Plugin.Log.Information($"Estimated time to complete {actualTreasurePoints} treasure points in '{nextMapArea}' map: {estimatedTime:F2} seconds");

            // Add the optimized route for this map area to the overall route
            if (mapRoute.Count > 0)
            {
                // Check each coordinate in the optimized route to ensure that teleport points have proper AetheryteId
                foreach (var coord in mapRoute)
                {
                    // If this is a teleport point but AetheryteId is not set
                    // This is a safety check to ensure teleport buttons will display properly
                    if (coord.Type == CoordinateType.TeleportPoint && coord.AetheryteId == 0)
                    {
                        // Try to find the aetheryte by name (removing the [Teleport] prefix)
                        string aetheryteName = coord.Name.Replace("[Teleport] ", "");
                        var aetheryteInfo = plugin.AetheryteService.GetAetheryteByName(aetheryteName);
                        
                        if (aetheryteInfo != null)
                        {
                            // Set AetheryteId so teleport button will display
                            coord.AetheryteId = aetheryteInfo.AetheryteId;
                            Plugin.Log.Debug($"Fixed missing AetheryteId for teleport point {aetheryteName}, set to {aetheryteInfo.AetheryteId}");
                        }
                    }
                }
                
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
        var collectedCoordinates = coordinates.Where(c => c.IsCollected).ToList();
        route.AddRange(collectedCoordinates);

        OptimizedRoute = route;

        // Count actual treasure points and teleport points using coordinate types
        int totalTreasurePoints = OptimizedRoute.Count(c => c.Type == CoordinateType.TreasurePoint);
        int totalTeleportPoints = OptimizedRoute.Count(c => c.Type == CoordinateType.TeleportPoint);
        
        // Log the optimized route with clear distinction between treasure and teleport points
        Plugin.Log.Information($"Optimized route contains {totalTreasurePoints} treasure points and {totalTeleportPoints} teleport points across {OptimizedRoute.Select(c => c.MapArea).Distinct().Count()} map areas");
        
        // Raise the event
        OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);
        
        return OptimizedRoute;
    }

    /// <summary>
    /// Resets the route optimization, restoring the original order and resetting all collection states.
    /// </summary>
    /// <returns>The original order of coordinates.</returns>
    public List<TreasureCoordinate> ResetRouteOptimization()
    {
        if (OriginalOrder.Count > 0)
        {
            // Reset collection state and teleport settings for all coordinates
            foreach (var coordinate in OriginalOrder)
            {
                coordinate.IsCollected = false;
                // Clear teleport-related settings that were assigned during optimization
                coordinate.AetheryteId = 0;
                coordinate.Type = CoordinateType.TreasurePoint;
                coordinate.NavigationInstruction = string.Empty;
            }

            // Clear the optimized route to indicate we're no longer in optimized mode
            OptimizedRoute.Clear();

            // Log the reset
            Plugin.Log.Information($"Route optimization reset. Restored {OriginalOrder.Count} coordinates to original order and reset collection states and teleport settings.");

            // Raise the event
            OnRouteOptimizationReset?.Invoke(this, EventArgs.Empty);

            var result = new List<TreasureCoordinate>(OriginalOrder);

            // Clear the original order list to avoid duplicate entries if optimized again
            OriginalOrder.Clear();

            return result;
        }
        else
        {
            Plugin.Log.Warning("Cannot reset route optimization: No original order saved.");
            return new List<TreasureCoordinate>();
        }
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
                    mapArea,
                    CoordinateSystemType.Map);
                
                // Find the nearest coordinate to the aetheryte
                var nearestCoordinate = mapCoordinates.OrderBy(c => aetheryteCoord.DistanceTo(c)).FirstOrDefault();

                if (nearestCoordinate != null)
                {
                    distanceToNearest = aetheryteCoord.DistanceTo(nearestCoordinate);
                    Plugin.Log.Debug($"Distance from aetheryte {bestAetheryte.Name} to nearest coordinate: {distanceToNearest:F1}");
                }
            }

            // Normalize teleport cost (higher cost = higher score)
            float normalizedTeleportCost = teleportCost / 1000.0f; // Normalize to 0-1 range
            
            // Normalize coordinate density (higher density = lower score)
            float coordinateDensity = (float)mapCoordinates.Count / totalCoordinates;
            
            // Calculate the final score (lower is better)
            float score = normalizedTeleportCost * 0.5f + distanceToNearest * 0.3f - coordinateDensity * 0.2f;
            
            mapAreaScores[mapArea] = score;
            
            Plugin.Log.Debug($"Map area {mapArea} score: {score:F3} " +
                            $"(teleport: {normalizedTeleportCost:F3}, " +
                            $"distance: {distanceToNearest:F1}, " +
                            $"density: {coordinateDensity:F3})");
        }
        
        // Return the map area with the lowest score
        var bestMapArea = mapAreaScores.OrderBy(kv => kv.Value).First().Key;
        return bestMapArea;
    }

    /// <summary>
    /// Finds the best aetheryte for a set of coordinates.
    /// </summary>
    /// <param name="aetherytes">The list of aetherytes.</param>
    /// <param name="coordinates">The list of coordinates.</param>
    /// <returns>The best aetheryte, or null if no aetherytes are available.</returns>
    private AetheryteInfo? FindBestAetheryteForCoordinates(List<AetheryteInfo> aetherytes, List<TreasureCoordinate> coordinates)
    {
        if (aetherytes == null || aetherytes.Count == 0 || coordinates == null || coordinates.Count == 0)
            return null;
            
        // Calculate the average position of all coordinates
        float sumX = 0, sumY = 0;
        foreach (var coord in coordinates)
        {
            sumX += coord.X;
            sumY += coord.Y;
        }
        
        float avgX = sumX / coordinates.Count;
        float avgY = sumY / coordinates.Count;
        
        // Create a coordinate at the average position
        var averageCoord = new TreasureCoordinate(avgX, avgY, coordinates[0].MapArea, CoordinateSystemType.Map);
        
        // Find the aetheryte closest to the average position
        var closestAetheryte = aetherytes
            .OrderBy(a => a.DistanceTo(averageCoord))
            .FirstOrDefault();
            
        return closestAetheryte;
    }

    /// <summary>
    /// Gets teleport costs to all map areas.
    /// </summary>
    /// <param name="currentMapArea">The current map area.</param>
    /// <param name="targetMapAreas">The list of target map areas.</param>
    /// <returns>A dictionary mapping map areas to their teleport costs.</returns>
    private Dictionary<string, uint> GetAllMapAreaTeleportCosts(string currentMapArea, List<string> targetMapAreas)
    {
        var result = new Dictionary<string, uint>();
        
        // If we don't know the current map area, assume all teleports cost a default value
        if (string.IsNullOrEmpty(currentMapArea))
        {
            foreach (var mapArea in targetMapAreas)
            {
                // Default teleport cost if we don't know where we are
                result[mapArea] = 999;
            }
            return result;
        }
        
        // We don't directly use Telepo API as it requires unsafe code
        // Instead we'll use our AetheryteService to get teleport costs
        bool telepoAvailable = true; // Assume player is logged in as we're optimizing a route
        
        foreach (var mapArea in targetMapAreas)
        {
            // If it's the current map area, no teleport needed
            if (mapArea == currentMapArea)
            {
                result[mapArea] = 0;
                continue;
            }
            
            // Get the cheapest aetheryte in this map area
            var cheapestAetheryte = plugin.AetheryteService.GetCheapestAetheryteInMapArea(mapArea);
            
            if (cheapestAetheryte != null)
            {
                // Update teleport fees
                plugin.AetheryteService.UpdateTeleportFees(new[] { cheapestAetheryte });
                
                // Try to get teleport cost from Telepo API first, then fallback to estimated cost
                uint teleportCost = uint.MaxValue;
                
                if (telepoAvailable && cheapestAetheryte.AetheryteId > 0)
                {
                    try
                    {
                        // Calculate teleport cost using AetheryteService
                        teleportCost = (uint)cheapestAetheryte.CalculateTeleportFee();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.Error($"Error calculating teleport cost: {ex.Message}");
                    }
                }
                
                // If we couldn't get cost from Telepo, use the calculated cost
                if (teleportCost == uint.MaxValue)
                {
                    teleportCost = (uint)cheapestAetheryte.CalculateTeleportFee();
                }
                
                result[mapArea] = teleportCost;
            }
            else
            {
                // If no aetheryte found, use a default high cost
                result[mapArea] = 999;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Gets teleport cost from an aetheryte.
    /// </summary>
    /// <param name="aetheryteId">The aetheryte ID.</param>
    /// <returns>The teleport cost, or 0 if not available.</returns>
    private uint GetTeleportCostFromTelepo(uint aetheryteId)
    {
        try
        {
            // Get the aetheryte info from the aetheryte service
            var aetheryteInfo = plugin.AetheryteService.GetAetheryteById(aetheryteId);
            if (aetheryteInfo == null)
                return 0;
                
            // Use the aetheryte info to calculate the teleport fee
            return (uint)aetheryteInfo.CalculateTeleportFee();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error calculating teleport cost: {ex.Message}");
            return 0;
        }
    }
}
