using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using OnePiece.Models;
using OnePiece.Helpers;

namespace OnePiece.Services;

/// <summary>
/// Service for optimizing routes through treasure coordinates.
/// </summary>
public class RouteOptimizationService
{
    private readonly Plugin plugin;
    private readonly TimeBasedPathFinder pathFinder;
    private CancellationTokenSource? currentOptimizationCancellation;

    /// <summary>
    /// Gets the optimized route through the coordinates.
    /// </summary>
    public List<TreasureCoordinate> OptimizedRoute { get; private set; } = new();

    /// <summary>
    /// Gets whether the route is currently optimized.
    /// </summary>
    public bool IsRouteOptimized => OptimizedRoute.Count > 0;

    /// <summary>
    /// Gets whether a route optimization is currently in progress.
    /// </summary>
    public bool IsOptimizationInProgress => currentOptimizationCancellation != null && !currentOptimizationCancellation.Token.IsCancellationRequested;

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
    /// This is the synchronous version for backward compatibility.
    /// </summary>
    /// <param name="coordinates">The coordinates to optimize.</param>
    public List<TreasureCoordinate> OptimizeRoute(List<TreasureCoordinate> coordinates)
    {
        // Get player location on main thread before async operation
        TreasureCoordinate? playerLocation = null;
        if (ThreadSafetyHelper.IsMainThread())
        {
            playerLocation = plugin.PlayerLocationService.GetCurrentLocation();
        }

        // Use the async version and wait for completion
        return OptimizeRouteAsync(coordinates, CancellationToken.None, playerLocation).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously optimizes the route through the coordinates considering player location, teleport costs, and travel distances.
    /// </summary>
    /// <param name="coordinates">The coordinates to optimize.</param>
    /// <param name="cancellationToken">Token to cancel the optimization process.</param>
    /// <param name="playerLocation">Optional pre-fetched player location (must be obtained from main thread).</param>
    /// <returns>A task that represents the asynchronous optimization operation.</returns>
    public async Task<List<TreasureCoordinate>> OptimizeRouteAsync(List<TreasureCoordinate> coordinates, CancellationToken cancellationToken = default, TreasureCoordinate? playerLocation = null)
    {
        // Cancel any existing optimization
        currentOptimizationCancellation?.Cancel();
        currentOptimizationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            if (OriginalOrder.Count == 0)
            {
                OriginalOrder = new List<TreasureCoordinate>(coordinates);
            }

            // Check for cancellation early
            cancellationToken.ThrowIfCancellationRequested();

        // Clear any existing teleport settings from previous optimizations
        foreach (var coord in coordinates)
        {
            coord.AetheryteId = 0;
            coord.Type = CoordinateType.TreasurePoint;
            coord.NavigationInstruction = string.Empty;
        }

        // Remove the early return for single coordinates - they still need teleport optimization
        // Even with 1 coordinate, we need to:
        // 1. Assign the nearest aetheryte for teleportation
        // 2. Calculate optimal route (which may include teleport points)
        // 3. Set proper coordinate types and navigation instructions

        // Get player's current location (use provided location or fetch from main thread)
        TreasureCoordinate currentPlayerLocation;
        if (playerLocation != null)
        {
            // Use pre-fetched player location
            currentPlayerLocation = playerLocation;
        }
        else if (ThreadSafetyHelper.IsMainThread())
        {
            // Fetch from main thread
            currentPlayerLocation = plugin.PlayerLocationService.GetCurrentLocation() ??
                new TreasureCoordinate(0, 0, string.Empty);
        }
        else
        {
            Plugin.Log.Warning($"Route optimization called from non-main thread without player location, using default");
            currentPlayerLocation = new TreasureCoordinate(0, 0, string.Empty);
        }

        // First, translate all map area names to English for proper grouping
        // This ensures coordinates with different language names for the same area are grouped together
        var coordinatesWithEnglishMapAreas = coordinates
            .Where(c => !c.IsCollected)
            .Select(c =>
            {
                var englishMapArea = MapAreaHelper.GetEnglishMapAreaFromCoordinate(c, plugin.MapAreaTranslationService);
                // Create a copy with English map area for grouping, but keep original for display
                var coordCopy = c.DeepCopy();
                coordCopy.MapArea = englishMapArea;
                return new { Original = c, EnglishCopy = coordCopy, EnglishMapArea = englishMapArea };
            })
            .ToList();

        // Group coordinates by English map area names
        var coordinatesByMap = coordinatesWithEnglishMapAreas
            .GroupBy(x => x.EnglishMapArea)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Original).ToList());

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
        var currentLocation = currentPlayerLocation;
        var currentMapArea = currentPlayerLocation.MapArea;

        // Translate current map area to English for teleport cost calculation
        var currentEnglishMapArea = MapAreaHelper.GetEnglishMapArea(currentMapArea, plugin.MapAreaTranslationService);
        var mapAreaTeleportCosts = await GetAllMapAreaTeleportCostsAsync(currentEnglishMapArea, coordinatesByMap.Keys.ToList(), cancellationToken);

        // Process all map areas until all coordinates are visited
        while (coordinatesByMap.Count > 0)
        {
            // Check for cancellation before processing each map area
            cancellationToken.ThrowIfCancellationRequested();

            string nextMapArea;

            // If player is already in a map area with coordinates, prioritize that
            if (coordinatesByMap.ContainsKey(currentEnglishMapArea))
            {
                nextMapArea = currentEnglishMapArea;
            }
            else
            {
                nextMapArea = FindBestMapAreaToVisit(currentLocation, coordinatesByMap, mapAreaTeleportCosts);
            }

            var mapCoordinates = coordinatesByMap[nextMapArea];

            // nextMapArea is already in English, so use it directly for aetheryte lookup
            var mapAetherytes = plugin.AetheryteService.GetAetherytesInMapArea(nextMapArea).ToList();

            if (mapAetherytes.Count > 0)
            {
                plugin.AetheryteService.UpdateTeleportFees(mapAetherytes);
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
            }

            // Check if player needs to teleport to the map area
            bool needsTeleport = false;

            Plugin.Log.Information($"Teleport decision: currentMapArea='{currentMapArea}', currentEnglishMapArea='{currentEnglishMapArea}', nextMapArea='{nextMapArea}', mapCoordinates.Count={mapCoordinates.Count}");

            if (currentEnglishMapArea != nextMapArea)
            {
                // Different map - definitely need to teleport
                needsTeleport = true;
                Plugin.Log.Information($"Different map areas - teleport required");
            }
            else if (mapCoordinates.Count > 0)
            {
                Plugin.Log.Information($"Same map area - analyzing time costs for teleport decision");

                // Same map - use time-based comparison instead of distance threshold
                // Calculate time for direct route vs teleport route
                float directRouteTime = pathFinder.CalculateRouteTimeFromStart(currentLocation, mapCoordinates);

                float teleportRouteTime = float.MaxValue;
                if (mapAetheryte != null)
                {
                    var aetherytePos = new TreasureCoordinate(
                        mapAetheryte.Position.X,
                        mapAetheryte.Position.Y,
                        mapAetheryte.MapArea,
                        CoordinateSystemType.Map);

                    // Calculate teleport time: cast + loading + route from aetheryte
                    float teleportCost = 3.0f + 2.0f; // TELEPORT_CAST_TIME + TELEPORT_LOADING_TIME
                    float routeFromAetheryteTime = pathFinder.CalculateRouteTimeFromStart(aetherytePos, mapCoordinates);
                    teleportRouteTime = teleportCost + routeFromAetheryteTime;
                }

                Plugin.Log.Information($"Same map route comparison - Direct route time: {directRouteTime:F2}s, Teleport route time: {teleportRouteTime:F2}s");

                // Only teleport if it's significantly faster (saves at least 5 seconds)
                if (teleportRouteTime < directRouteTime - 5.0f)
                {
                    needsTeleport = true;
                    Plugin.Log.Information($"Teleport chosen: saves {directRouteTime - teleportRouteTime:F2}s");
                }
                else
                {
                    Plugin.Log.Information($"Direct route chosen: teleport would only save {directRouteTime - teleportRouteTime:F2}s (minimum 5s required)");
                }
            }
            
            // Get all aetherytes in the current map area for better path optimization
            // nextMapArea is already in English, so use it directly
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



            List<TreasureCoordinate> mapRoute;
            if (needsTeleport && mapAetheryte != null)
            {
                // Create a coordinate for the aetheryte position
                var aetheryteCoord = new TreasureCoordinate(
                    mapAetheryte.Position.X,
                    mapAetheryte.Position.Y,
                    mapAetheryte.MapArea,
                    CoordinateSystemType.Map);

                // Call OptimizeRouteByTime with forced teleport (run in background thread for heavy computation)
                mapRoute = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return pathFinder.OptimizeRouteByTime(aetheryteCoord, mapCoordinates, allMapAetherytes, true, mapAetheryte);
                }, cancellationToken);

                // Update current location to the aetheryte after teleporting
                currentLocation = aetheryteCoord;
            }
            else
            {
                // No teleport needed, use current location (run in background thread for heavy computation)
                mapRoute = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return pathFinder.OptimizeRouteByTime(currentLocation, mapCoordinates, allMapAetherytes);
                }, cancellationToken);
            }

            float estimatedTime = pathFinder.EstimateCompletionTime(mapRoute);
            int actualTreasurePoints = mapRoute.Count(c => c.Type == CoordinateType.TreasurePoint);
            int teleportPoints = mapRoute.Count(c => c.Type == CoordinateType.TeleportPoint);

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
                            coord.AetheryteId = aetheryteInfo.AetheryteId;
                        }
                    }
                }
                
                route.AddRange(mapRoute);
                
                // Update the current location to the last coordinate in this map area
                currentLocation = mapRoute.Last();
                // Translate the current map area to English for next iteration
                currentEnglishMapArea = MapAreaHelper.GetEnglishMapAreaFromCoordinate(currentLocation, plugin.MapAreaTranslationService);

                // Update teleport costs if there are more areas to visit
                if (coordinatesByMap.Count > 1)
                {
                    mapAreaTeleportCosts = GetAllMapAreaTeleportCosts(currentEnglishMapArea, coordinatesByMap.Keys.ToList());
                }
            }

            // Remove this map area from the dictionary
            coordinatesByMap.Remove(nextMapArea);
        }

        // Add any collected coordinates to the end of the route (maintaining their original order)
        var collectedCoordinates = coordinates.Where(c => c.IsCollected).ToList();
        route.AddRange(collectedCoordinates);

        OptimizedRoute = route;

        int totalTreasurePoints = OptimizedRoute.Count(c => c.Type == CoordinateType.TreasurePoint);
        int totalTeleportPoints = OptimizedRoute.Count(c => c.Type == CoordinateType.TeleportPoint);
        
        // Raise the event
        OnRouteOptimized?.Invoke(this, OptimizedRoute.Count);

        return OptimizedRoute;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error during route optimization: {ex.Message}");
            throw;
        }
        finally
        {
            // Clean up cancellation token
            currentOptimizationCancellation?.Dispose();
            currentOptimizationCancellation = null;
        }
    }

    /// <summary>
    /// Cancels any ongoing route optimization.
    /// </summary>
    public void CancelOptimization()
    {
        currentOptimizationCancellation?.Cancel();
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

            OptimizedRoute.Clear();

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
        // Translate current location's map area to English for comparison
        var currentLocationEnglishMapArea = MapAreaHelper.GetEnglishMapAreaFromCoordinate(currentLocation, plugin.MapAreaTranslationService);
        if (!string.IsNullOrEmpty(currentLocationEnglishMapArea) && coordinatesByMap.ContainsKey(currentLocationEnglishMapArea))
        {
            return currentLocationEnglishMapArea;
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

            // mapArea is already in English, so use it directly for aetheryte lookup
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
                }
            }

            // Calculate the final score (lower is better)
            // Primary factor: teleport cost (most important)
            // Secondary factor: distance to nearest coordinate (minor consideration)

            // Use teleport cost directly as primary score, with distance as tiebreaker
            float teleportCostScore = teleportCost; // Use actual cost, not normalized
            float distanceScore = distanceToNearest * 10.0f; // Scale distance to be comparable but secondary

            float score = teleportCostScore + distanceScore;

            // Add debug logging to understand scoring decisions
            Plugin.Log.Information($"Map area scoring - {mapArea}: teleportCost={teleportCost}, distance={distanceToNearest:F1}, teleportScore={teleportCostScore:F0}, distanceScore={distanceScore:F1}, finalScore={score:F1}");

            mapAreaScores[mapArea] = score;
        }
        
        // Return the map area with the lowest score
        var bestMapArea = mapAreaScores.OrderBy(kv => kv.Value).First().Key;
        var bestScore = mapAreaScores[bestMapArea];

        Plugin.Log.Information($"Selected map area: {bestMapArea} with score {bestScore:F1} (lower is better)");

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
    /// Gets teleport costs to all map areas asynchronously.
    /// </summary>
    /// <param name="currentMapArea">The current map area.</param>
    /// <param name="targetMapAreas">The list of target map areas.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping map areas to their teleport costs.</returns>
    private async Task<Dictionary<string, uint>> GetAllMapAreaTeleportCostsAsync(string currentMapArea, List<string> targetMapAreas, CancellationToken cancellationToken)
    {
        return await Task.Run(() => GetAllMapAreaTeleportCosts(currentMapArea, targetMapAreas), cancellationToken);
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
                // If no aetheryte found, don't add teleport cost for this area
                // This will effectively disable teleport optimization for areas without aetherytes
                result[mapArea] = 0;
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
