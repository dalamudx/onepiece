using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OnePiece.Helpers;
using OnePiece.Models;

namespace OnePiece.Services
{
    /// <summary>
    /// Time-based path finder, used to optimize treasure map collection routes
    /// </summary>
    public class TimeBasedPathFinder
    {
        private readonly Plugin plugin;
        // Constants definition
        private const float MOUNT_SUMMON_TIME = 1.0f; // Mount summoning time (seconds)
        private const float MOUNT_SPEED = 0.403f; // Mount movement speed (units/second)
        private const float TELEPORT_CAST_TIME = 5.0f; // Teleport casting time (seconds)
        private const float TELEPORT_LOADING_TIME = 3.0f; // Teleport loading time (seconds)
        private const float LONG_DISTANCE_THRESHOLD = 10.0f; // Threshold for long distance travel (units)
        private const float LONG_DISTANCE_PENALTY = 5.0f; // Additional penalty for long distance travel (seconds)

        /// <summary>
        /// Constructor for TimeBasedPathFinder
        /// </summary>
        /// <param name="plugin">Reference to the plugin instance</param>
        public TimeBasedPathFinder(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Calculate time cost between two points (unit: seconds)
        /// </summary>
        /// <param name="start">Starting coordinate</param>
        /// <param name="end">Ending coordinate</param>
        /// <param name="isStartAetheryte">Whether the starting point is an aetheryte</param>
        /// <param name="isEndAetheryte">Whether the ending point is an aetheryte</param>
        /// <returns>Time cost from start to end (seconds)</returns>
        public float CalculateTimeCost(TreasureCoordinate start, TreasureCoordinate end, bool isStartAetheryte, bool isEndAetheryte)
        {
            float distance = start.DistanceTo(end);
            float timeCost = 0;
            
            // Add long distance penalty for direct travel
            float longDistancePenalty = 0;
            if (!isStartAetheryte && !isEndAetheryte && distance > LONG_DISTANCE_THRESHOLD)
            {
                longDistancePenalty = LONG_DISTANCE_PENALTY * (distance / LONG_DISTANCE_THRESHOLD - 1);
            }
            
            if (isStartAetheryte)
            {
                // Aetheryte → Treasure Map: Casting + Loading + Mount Summoning + Movement
                timeCost = TELEPORT_CAST_TIME + TELEPORT_LOADING_TIME + MOUNT_SUMMON_TIME + (distance / MOUNT_SPEED);
            }
            else
            {
                if (isEndAetheryte)
                {
                    // Treasure Map → Aetheryte: Direct movement (already mounted)
                    timeCost = distance / MOUNT_SPEED;
                }
                else
                {
                    // Treasure Map → Treasure Map: Mount Summoning + Movement + Long Distance Penalty
                    timeCost = MOUNT_SUMMON_TIME + (distance / MOUNT_SPEED) + longDistancePenalty;
                }
            }
            
            return timeCost;
        }
        
        /// <summary>
        /// Calculate the best time cost between two points considering all available aetherytes
        /// </summary>
        /// <param name="start">Starting coordinate</param>
        /// <param name="end">Ending coordinate</param>
        /// <param name="isStartAetheryte">Whether the starting point is an aetheryte</param>
        /// <param name="aetherytes">List of all available aetherytes</param>
        /// <param name="bestAetheryte">The best aetheryte to use, or null if direct travel is better</param>
        /// <returns>Best time cost from start to end (seconds)</returns>
        public float CalculateBestTimeCost(TreasureCoordinate start, TreasureCoordinate end, bool isStartAetheryte, List<TreasureCoordinate> aetherytes, out TreasureCoordinate bestAetheryte)
        {
            // First calculate direct travel time
            float directTime = CalculateTimeCost(start, end, isStartAetheryte, false);
            bestAetheryte = null;
            
            // If already at an aetheryte, direct travel is always better
            if (isStartAetheryte)
                return directTime;
            
            // Completely time-based calculation, no longer using fixed distance thresholds
            // Find the optimal solution by comparing time costs of all paths

            // Record current path calculation information for debug logging
            float startToEndDistance = start.DistanceTo(end);
            bool sameMapArea = start.MapArea == end.MapArea;
                
            // Calculate time via each aetheryte
            float bestTime = directTime;
            foreach (var aetheryte in aetherytes)
            {
                // Skip if this is the starting point (already calculated as direct travel)
                if (start.DistanceTo(aetheryte) < 1.0f)
                    continue;
                    
                // Teleporting to aetheryte involves casting time and loading time (no travel time)
                float teleportTime = TELEPORT_CAST_TIME + TELEPORT_LOADING_TIME;
                // Travel from aetheryte to destination
                float fromAetheryteTime = CalculateTimeCost(aetheryte, end, true, false);
                float totalTime = teleportTime + fromAetheryteTime;

                // Choose teleport if it's faster than direct travel (removed 20% threshold)
                if (totalTime < bestTime)
                {
                    bestTime = totalTime;
                    bestAetheryte = aetheryte;
                }
            }
            
            return bestTime;
        }

        /// <summary>
        /// Optimize path within the same map, considering time factors
        /// </summary>
        /// <param name="startLocation">Starting location</param>
        /// <param name="coordinates">Treasure map coordinates to collect</param>
        /// <param name="mapAetherytes">List of all aetherytes in the map</param>
        /// <param name="forceTeleport">If true, forces teleport to the startLocation (used when RouteOptimizationService has already decided to teleport)</param>
        /// <param name="teleportAetheryte">The aetheryte to teleport to (when forceTeleport is true)</param>
        /// <returns>Optimized path</returns>
        public List<TreasureCoordinate> OptimizeRouteByTime(
            TreasureCoordinate startLocation,
            List<TreasureCoordinate> coordinates,
            List<AetheryteInfo> mapAetherytes,
            bool forceTeleport = false,
            AetheryteInfo teleportAetheryte = null)
        {
            if (coordinates.Count <= 1)
                return new List<TreasureCoordinate>(coordinates);



            // Check if teleport is forced by RouteOptimizationService
            if (forceTeleport && teleportAetheryte != null)
            {
                var forcedTeleportRoute = OptimizeGroundRoute(startLocation, coordinates, teleportAetheryte);
                return forcedTeleportRoute;
            }

            // Step 1: Determine the optimal starting point (current location vs teleport to aetheryte)
            var optimalStart = DetermineOptimalStartingPoint(startLocation, coordinates, mapAetherytes);



            // Step 2: Optimize the route from the optimal starting point using ground travel only
            var optimizedRoute = OptimizeGroundRoute(optimalStart.startPoint, coordinates, optimalStart.usedAetheryte);

            return optimizedRoute;
        }

        /// <summary>
        /// Determines the optimal starting point for the route (current location vs teleport to aetheryte)
        /// </summary>
        /// <param name="startLocation">Current player location</param>
        /// <param name="coordinates">Treasure coordinates to visit</param>
        /// <param name="mapAetherytes">Available aetherytes in the map</param>
        /// <returns>Tuple containing the optimal start point and the aetheryte used (if any)</returns>
        private (TreasureCoordinate startPoint, AetheryteInfo? usedAetheryte) DetermineOptimalStartingPoint(
            TreasureCoordinate startLocation,
            List<TreasureCoordinate> coordinates,
            List<AetheryteInfo> mapAetherytes)
        {
            // If no aetherytes available, use current location
            if (mapAetherytes == null || mapAetherytes.Count == 0)
            {
                Plugin.Log.Debug("No aetherytes available, using current location as start point");
                return (startLocation, null);
            }

            // Check if player is in a different map area than the coordinates
            string coordinateMapArea = coordinates.Count > 0 ? coordinates[0].MapArea : "";

            // If player location MapArea is empty or null, assume they need to teleport
            bool playerLocationEmpty = string.IsNullOrEmpty(startLocation.MapArea);
            bool isDifferentMapArea = !string.IsNullOrEmpty(coordinateMapArea) &&
                                     (playerLocationEmpty || startLocation.MapArea != coordinateMapArea);

            Plugin.Log.Information($"Player location: '{startLocation.MapArea}' (empty: {playerLocationEmpty}), Coordinate map area: '{coordinateMapArea}', Different map area: {isDifferentMapArea}");

            // If player is in a different map area, teleportation is mandatory
            if (isDifferentMapArea)
            {
                Plugin.Log.Information($"Player is in different map area ({startLocation.MapArea} vs {coordinateMapArea}), teleportation is mandatory");

                // Find the best aetheryte to teleport to
                AetheryteInfo bestAetheryte = null;
                float bestTeleportTime = float.MaxValue;

                foreach (var aetheryte in mapAetherytes)
                {
                    var aetheryteCoord = new TreasureCoordinate(
                        aetheryte.Position.X,
                        aetheryte.Position.Y,
                        aetheryte.MapArea,
                        CoordinateSystemType.Map,
                        aetheryte.Name);

                    // Calculate time: teleport cost + route time from aetheryte
                    float teleportCost = TELEPORT_CAST_TIME + TELEPORT_LOADING_TIME;
                    float routeTimeFromAetheryte = CalculateRouteTimeFromStart(aetheryteCoord, coordinates);
                    float totalTeleportTime = teleportCost + routeTimeFromAetheryte;

                    Plugin.Log.Debug($"Aetheryte {aetheryte.Name}: teleport cost {teleportCost:F2}s + route time {routeTimeFromAetheryte:F2}s = {totalTeleportTime:F2}s");

                    if (totalTeleportTime < bestTeleportTime)
                    {
                        bestTeleportTime = totalTeleportTime;
                        bestAetheryte = aetheryte;
                    }
                }

                if (bestAetheryte != null)
                {
                    var aetheryteStartPoint = new TreasureCoordinate(
                        bestAetheryte.Position.X,
                        bestAetheryte.Position.Y,
                        bestAetheryte.MapArea,
                        CoordinateSystemType.Map,
                        bestAetheryte.Name);

                    Plugin.Log.Information($"Mandatory teleport to {bestAetheryte.Name} (total time: {bestTeleportTime:F2}s)");
                    Plugin.Log.Information($"About to return: startPoint=({aetheryteStartPoint.X:F1}, {aetheryteStartPoint.Y:F1}), usedAetheryte={bestAetheryte.Name}, AetheryteId={bestAetheryte.AetheryteId}");

                    // Create explicit variables to avoid tuple issues
                    var resultStartPoint = aetheryteStartPoint;
                    var resultUsedAetheryte = bestAetheryte;
                    Plugin.Log.Information($"Created result variables: startPoint=({resultStartPoint.X:F1}, {resultStartPoint.Y:F1}), usedAetheryte={resultUsedAetheryte.Name}, AetheryteId={resultUsedAetheryte.AetheryteId}");

                    return (resultStartPoint, resultUsedAetheryte);
                }
                else
                {
                    Plugin.Log.Warning("No suitable aetheryte found for mandatory teleport, using current location");
                    return (startLocation, null);
                }
            }

            // If in the same map area, compare direct travel vs teleport
            float directTotalTime = CalculateRouteTimeFromStart(startLocation, coordinates);

            // Find the best aetheryte to teleport to
            AetheryteInfo bestAetheryteForComparison = null;
            float bestTeleportTimeForComparison = float.MaxValue;

            foreach (var aetheryte in mapAetherytes)
            {
                var aetheryteCoord = new TreasureCoordinate(
                    aetheryte.Position.X,
                    aetheryte.Position.Y,
                    aetheryte.MapArea,
                    CoordinateSystemType.Map,
                    aetheryte.Name);

                // Calculate time: teleport cost + route time from aetheryte
                float teleportCost = TELEPORT_CAST_TIME + TELEPORT_LOADING_TIME;
                float routeTimeFromAetheryte = CalculateRouteTimeFromStart(aetheryteCoord, coordinates);
                float totalTeleportTime = teleportCost + routeTimeFromAetheryte;

                if (totalTeleportTime < bestTeleportTimeForComparison)
                {
                    bestTeleportTimeForComparison = totalTeleportTime;
                    bestAetheryteForComparison = aetheryte;
                }
            }

            Plugin.Log.Debug($"Same map area comparison - Direct route time: {directTotalTime:F2}s, Best teleport route time: {bestTeleportTimeForComparison:F2}s");

            // Choose teleport if it's faster than direct travel (removed 20% threshold)
            if (bestTeleportTimeForComparison < directTotalTime && bestAetheryteForComparison != null)
            {
                var aetheryteStartPoint = new TreasureCoordinate(
                    bestAetheryteForComparison.Position.X,
                    bestAetheryteForComparison.Position.Y,
                    bestAetheryteForComparison.MapArea,
                    CoordinateSystemType.Map,
                    bestAetheryteForComparison.Name);

                Plugin.Log.Information($"Teleport to {bestAetheryteForComparison.Name} is optimal (saves {directTotalTime - bestTeleportTimeForComparison:F2}s)");
                return (aetheryteStartPoint, bestAetheryteForComparison);
            }
            else
            {
                Plugin.Log.Information("Direct travel from current location is optimal");
                return (startLocation, null);
            }
        }

        /// <summary>
        /// Calculates the total time for a route starting from a given point using ground travel only
        /// </summary>
        /// <param name="startPoint">Starting point</param>
        /// <param name="coordinates">Coordinates to visit</param>
        /// <returns>Total time for the optimal ground route</returns>
        public float CalculateRouteTimeFromStart(TreasureCoordinate startPoint, List<TreasureCoordinate> coordinates)
        {
            if (coordinates.Count == 0)
                return 0;

            if (coordinates.Count == 1)
            {
                return CalculateTimeCost(startPoint, coordinates[0], false, false);
            }

            // For multiple coordinates, find the optimal TSP route
            var optimalRoute = SolveTSP(startPoint, coordinates);
            return CalculateRouteTimeFromStartToCoordinates(startPoint, optimalRoute);
        }

        /// <summary>
        /// Optimizes the route from the starting point, considering teleportation between coordinates
        /// </summary>
        /// <param name="startPoint">Starting point (current location or aetheryte)</param>
        /// <param name="coordinates">Treasure coordinates to visit</param>
        /// <param name="usedAetheryte">The aetheryte used to reach start point (if any)</param>
        /// <returns>Optimized route with proper AetheryteId assignments</returns>
        private List<TreasureCoordinate> OptimizeGroundRoute(TreasureCoordinate startPoint, List<TreasureCoordinate> coordinates, AetheryteInfo? usedAetheryte)
        {
            if (coordinates.Count == 0)
                return new List<TreasureCoordinate>();

            // Solve TSP to get optimal visiting order
            var optimalRoute = SolveTSP(startPoint, coordinates);

            // Create the final route with proper AetheryteId assignments
            var finalRoute = new List<TreasureCoordinate>();

            // Always log for debugging
            Plugin.Log.Debug($"OptimizeGroundRoute: Processing {optimalRoute.Count} coordinates, usedAetheryte: {usedAetheryte?.Name ?? "None"}");

            for (int i = 0; i < optimalRoute.Count; i++)
            {
                var coord = optimalRoute[i];

                // Create a copy of the coordinate to preserve original data
                var coordCopy = TreasureCoordinateBuilder.FromExisting(coord).Build();

                // Debug the condition check
                Plugin.Log.Debug($"Processing coordinate {i}: finalRoute.Count={finalRoute.Count}, usedAetheryte={usedAetheryte?.Name ?? "null"}, usedAetheryte.AetheryteId={usedAetheryte?.AetheryteId ?? 0}");

                // Only assign AetheryteId to the first treasure coordinate if we used teleport
                if (finalRoute.Count == 0 && usedAetheryte != null)
                {
                    coordCopy.AetheryteId = usedAetheryte.AetheryteId;
                    coordCopy.Type = CoordinateType.TeleportPoint; // Mark as teleport point for UI display
                    coordCopy.NavigationInstruction = $"Teleport to {usedAetheryte.Name}, then travel to ({coordCopy.X:F1}, {coordCopy.Y:F1})";

                    Plugin.Log.Information($"SUCCESS: Assigned AetheryteId {usedAetheryte.AetheryteId} and TeleportPoint type to first coordinate ({coordCopy.X:F1}, {coordCopy.Y:F1})");
                }
                else
                {
                    // All other coordinates use ground travel and remain as treasure points
                    coordCopy.Type = CoordinateType.TreasurePoint;
                    var prevCoord = finalRoute.Count > 0 ? finalRoute[finalRoute.Count - 1] : startPoint;
                    coordCopy.NavigationInstruction = $"Ground travel from ({prevCoord.X:F1}, {prevCoord.Y:F1}) to ({coordCopy.X:F1}, {coordCopy.Y:F1})";

                    Plugin.Log.Debug($"Set coordinate ({coordCopy.X:F1}, {coordCopy.Y:F1}) as TreasurePoint with AetheryteId: {coordCopy.AetheryteId}");
                }

                finalRoute.Add(coordCopy);
            }

            Plugin.Log.Debug($"OptimizeGroundRoute: Returning {finalRoute.Count} coordinates");

            // Now optimize each segment of the route to consider teleportation
            var optimizedRouteWithTeleports = OptimizeRouteSegments(finalRoute);

            return optimizedRouteWithTeleports;
        }

        /// <summary>
        /// Optimizes each segment of the route to consider teleportation between coordinates
        /// </summary>
        /// <param name="route">The basic route from TSP optimization</param>
        /// <returns>Route with teleportation optimizations applied</returns>
        private List<TreasureCoordinate> OptimizeRouteSegments(List<TreasureCoordinate> route)
        {
            if (route.Count <= 1)
                return route;

            // Get all available aetherytes for the map areas in the route
            var mapAreas = route.Select(c => c.MapArea).Distinct().ToList();
            var allAetherytes = new List<TreasureCoordinate>();

            foreach (var mapArea in mapAreas)
            {
                // Get English map area name for aetheryte lookup
                var englishMapArea = MapAreaHelper.GetEnglishMapAreaFromCollection(route, mapArea, plugin.MapAreaTranslationService);
                var aetherytesInMap = plugin.AetheryteService.GetAetherytesInMapArea(englishMapArea);
                if (aetherytesInMap != null)
                {
                    foreach (var aetheryte in aetherytesInMap)
                    {
                        allAetherytes.Add(new TreasureCoordinate(
                            aetheryte.Position.X,
                            aetheryte.Position.Y,
                            aetheryte.MapArea,
                            CoordinateSystemType.Map,
                            aetheryte.Name));
                    }
                }
            }

            Plugin.Log.Information($"OptimizeRouteSegments: Processing {route.Count} coordinates with {allAetherytes.Count} available aetherytes");

            var optimizedRoute = new List<TreasureCoordinate>();

            for (int i = 0; i < route.Count; i++)
            {
                var currentCoord = route[i];

                // For the first coordinate, keep its existing teleport settings
                if (i == 0)
                {
                    optimizedRoute.Add(currentCoord);
                    continue;
                }

                var prevCoord = route[i - 1];

                // Check if teleporting to a nearby aetheryte would be faster
                TreasureCoordinate bestAetheryte;
                float bestTime = CalculateBestTimeCost(prevCoord, currentCoord, false, allAetherytes, out bestAetheryte);
                float directTime = CalculateTimeCost(prevCoord, currentCoord, false, false);

                // Create a copy of the coordinate
                var coordCopy = TreasureCoordinateBuilder.FromExisting(currentCoord).Build();

                if (bestAetheryte != null && bestTime < directTime)
                {
                    // Find the corresponding AetheryteInfo using English map area name
                    var englishMapAreaForLookup = MapAreaHelper.GetEnglishMapAreaFromCollection(route, bestAetheryte.MapArea, plugin.MapAreaTranslationService);
                    var aetheryteInfo = plugin.AetheryteService.GetAetherytesInMapArea(englishMapAreaForLookup)
                        ?.FirstOrDefault(a => a.Name == bestAetheryte.Name);

                    if (aetheryteInfo != null)
                    {
                        coordCopy.AetheryteId = aetheryteInfo.AetheryteId;
                        coordCopy.Type = CoordinateType.TeleportPoint;
                        coordCopy.NavigationInstruction = $"Teleport to {bestAetheryte.Name}, then travel to ({coordCopy.X:F1}, {coordCopy.Y:F1})";

                        Plugin.Log.Information($"Optimized segment {i}: Teleport to {bestAetheryte.Name} saves {directTime - bestTime:F2}s for coordinate ({coordCopy.X:F1}, {coordCopy.Y:F1})");
                    }
                    else
                    {
                        // Fallback to direct travel
                        coordCopy.Type = CoordinateType.TreasurePoint;
                        coordCopy.NavigationInstruction = $"Ground travel from ({prevCoord.X:F1}, {prevCoord.Y:F1}) to ({coordCopy.X:F1}, {coordCopy.Y:F1})";
                    }
                }
                else
                {
                    // Direct travel is optimal
                    coordCopy.Type = CoordinateType.TreasurePoint;
                    coordCopy.NavigationInstruction = $"Ground travel from ({prevCoord.X:F1}, {prevCoord.Y:F1}) to ({coordCopy.X:F1}, {coordCopy.Y:F1})";
                }

                optimizedRoute.Add(coordCopy);
            }

            Plugin.Log.Information($"OptimizeRouteSegments: Completed optimization, {optimizedRoute.Count(c => c.Type == CoordinateType.TeleportPoint)} teleport points identified");

            return optimizedRoute;
        }

        /// <summary>
        /// Solves the Traveling Salesman Problem to find the optimal route
        /// </summary>
        /// <param name="startPoint">Starting point</param>
        /// <param name="coordinates">Coordinates to visit</param>
        /// <returns>Optimal route (coordinates only, without start point)</returns>
        private List<TreasureCoordinate> SolveTSP(TreasureCoordinate startPoint, List<TreasureCoordinate> coordinates)
        {
            if (coordinates.Count <= 1)
            {
                return new List<TreasureCoordinate>(coordinates);
            }

            // Get all aetherytes for teleport-aware TSP calculation
            var allAetherytes = new List<TreasureCoordinate>();
            var mapAreas = coordinates.Select(c => c.MapArea).Distinct().ToList();

            foreach (var mapArea in mapAreas)
            {
                var englishMapArea = MapAreaHelper.GetEnglishMapArea(mapArea, plugin.MapAreaTranslationService);
                var aetherytesInMap = plugin.AetheryteService.GetAetherytesInMapArea(englishMapArea);
                if (aetherytesInMap != null)
                {
                    foreach (var aetheryte in aetherytesInMap)
                    {
                        allAetherytes.Add(new TreasureCoordinate(
                            aetheryte.Position.X,
                            aetheryte.Position.Y,
                            aetheryte.MapArea,
                            CoordinateSystemType.Map,
                            aetheryte.Name));
                    }
                }
            }

            // For small number of coordinates, use permutation for exact solution
            if (coordinates.Count <= 7)
            {
                return SolveTSPByPermutation(startPoint, coordinates, allAetherytes);
            }
            // For 8 coordinates, use hybrid approach
            else if (coordinates.Count == 8)
            {
                var permResult = SolveTSPByPermutation(startPoint, coordinates, allAetherytes);
                var nnResult = SolveTSPByNearestNeighbor(startPoint, coordinates, allAetherytes);

                // Calculate time for both routes
                float permTime = CalculateRouteTimeFromStartToCoordinates(startPoint, permResult);
                float nnTime = CalculateRouteTimeFromStartToCoordinates(startPoint, nnResult);

                return permTime <= nnTime ? permResult : nnResult;
            }
            // For larger numbers, use approximation algorithms
            else
            {
                return SolveTSPByNearestNeighbor(startPoint, coordinates, allAetherytes);
            }
        }

        /// <summary>
        /// Calculates total time for a ground route
        /// </summary>
        /// <param name="route">Route coordinates</param>
        /// <returns>Total time in seconds</returns>
        private float CalculateGroundRouteTime(List<TreasureCoordinate> route)
        {
            if (route.Count <= 1)
                return 0;

            float totalTime = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                totalTime += CalculateTimeCost(route[i], route[i + 1], false, false);
            }
            return totalTime;
        }

        /// <summary>
        /// Calculates total time for a route starting from a specific point
        /// </summary>
        /// <param name="startPoint">Starting point</param>
        /// <param name="coordinates">Coordinates to visit in order</param>
        /// <returns>Total time in seconds</returns>
        private float CalculateRouteTimeFromStartToCoordinates(TreasureCoordinate startPoint, List<TreasureCoordinate> coordinates)
        {
            if (coordinates.Count == 0)
                return 0;

            float totalTime = 0;
            var currentPos = startPoint;

            foreach (var coord in coordinates)
            {
                totalTime += CalculateTimeCost(currentPos, coord, false, false);
                currentPos = coord;
            }

            return totalTime;
        }

        /// <summary>
        /// Solves TSP using permutation method for exact solution
        /// </summary>
        /// <param name="startPoint">Starting point</param>
        /// <param name="coordinates">Coordinates to visit</param>
        /// <param name="allAetherytes">All available aetherytes for teleport consideration</param>
        /// <returns>Optimal route (coordinates only, without start point)</returns>
        private List<TreasureCoordinate> SolveTSPByPermutation(TreasureCoordinate startPoint, List<TreasureCoordinate> coordinates, List<TreasureCoordinate> allAetherytes)
        {
            var allPermutations = GetAllPermutations(coordinates);
            var bestRoute = new List<TreasureCoordinate>();
            float bestTime = float.MaxValue;

            foreach (var perm in allPermutations)
            {
                // Calculate route time starting from startPoint through all coordinates
                float totalTime = 0;
                var currentPos = startPoint;

                foreach (var coord in perm)
                {
                    // Use CalculateBestTimeCost to consider teleportation
                    TreasureCoordinate bestAetheryte;
                    totalTime += CalculateBestTimeCost(currentPos, coord, false, allAetherytes, out bestAetheryte);
                    currentPos = coord;
                }

                if (totalTime < bestTime)
                {
                    bestTime = totalTime;
                    bestRoute = new List<TreasureCoordinate>(perm);
                }
            }

            return bestRoute;
        }

        /// <summary>
        /// Solves TSP using nearest neighbor heuristic
        /// </summary>
        /// <param name="startPoint">Starting point</param>
        /// <param name="coordinates">Coordinates to visit</param>
        /// <param name="allAetherytes">All available aetherytes for teleport consideration</param>
        /// <returns>Approximate optimal route (coordinates only, without start point)</returns>
        private List<TreasureCoordinate> SolveTSPByNearestNeighbor(TreasureCoordinate startPoint, List<TreasureCoordinate> coordinates, List<TreasureCoordinate> allAetherytes)
        {
            var remaining = new List<TreasureCoordinate>(coordinates);
            var route = new List<TreasureCoordinate>();
            var currentPos = startPoint;

            while (remaining.Count > 0)
            {
                // Find the nearest coordinate considering teleportation
                var nearest = remaining.OrderBy(c =>
                {
                    TreasureCoordinate bestAetheryte;
                    return CalculateBestTimeCost(currentPos, c, false, allAetherytes, out bestAetheryte);
                }).First();

                route.Add(nearest);
                remaining.Remove(nearest);
                currentPos = nearest;
            }

            return route;
        }



        /// <summary>
        /// Generate all possible permutations
        /// </summary>
        private List<List<TreasureCoordinate>> GetAllPermutations(List<TreasureCoordinate> coordinates)
        {
            var result = new List<List<TreasureCoordinate>>();
            PermuteHelper(coordinates, 0, result);
            return result;
        }

        private void PermuteHelper(List<TreasureCoordinate> coordinates, int start, List<List<TreasureCoordinate>> result)
        {
            if (start >= coordinates.Count)
            {
                result.Add(new List<TreasureCoordinate>(coordinates));
                return;
            }

            for (int i = start; i < coordinates.Count; i++)
            {
                // Swap
                var temp = coordinates[start];
                coordinates[start] = coordinates[i];
                coordinates[i] = temp;

                // Recurse
                PermuteHelper(coordinates, start + 1, result);

                // Backtrack
                temp = coordinates[start];
                coordinates[start] = coordinates[i];
                coordinates[i] = temp;
            }
        }





        /// <summary>
        /// Estimate the total time needed to complete the entire path
        /// </summary>
        /// <param name="route">Optimized path</param>
        /// <returns>Estimated completion time (seconds)</returns>
        public float EstimateCompletionTime(List<TreasureCoordinate> route)
        {
            if (route.Count <= 1)
                return 0;

            float totalTime = 0;

            // Add teleport time for the first coordinate if it has AetheryteId
            if (route.Count > 0 && route[0].AetheryteId > 0)
            {
                totalTime += TELEPORT_CAST_TIME + TELEPORT_LOADING_TIME;
            }

            // Calculate travel time between consecutive coordinates
            for (int i = 0; i < route.Count - 1; i++)
            {
                // All travel between coordinates is ground travel (no teleporting between them)
                totalTime += CalculateTimeCost(route[i], route[i+1], false, false);
            }

            // Consider collection time for each treasure point (assumed to be 10 seconds)
            // Count all coordinates as treasure points since they all need to be collected
            totalTime += route.Count * 10;

            return totalTime;
        }
    }
}
