using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OnePiece.Models;

namespace OnePiece.Services
{
    /// <summary>
    /// Time-based path finder, used to optimize treasure map collection routes
    /// </summary>
    public class TimeBasedPathFinder
    {
        // Constants definition
        private const float MOUNT_SUMMON_TIME = 1.0f; // Mount summoning time (seconds)
        private const float MOUNT_SPEED = 0.403f; // Mount movement speed (units/second)
        private const float TELEPORT_CAST_TIME = 5.0f; // Teleport casting time (seconds)
        private const float TELEPORT_LOADING_TIME = 3.0f; // Teleport loading time (seconds)
        private const float LONG_DISTANCE_THRESHOLD = 10.0f; // Threshold for long distance travel (units)
        private const float LONG_DISTANCE_PENALTY = 5.0f; // Additional penalty for long distance travel (seconds)

        /// <summary>
        /// Initialize a new instance of <see cref="TimeBasedPathFinder"/>
        /// </summary>
        public TimeBasedPathFinder()
        {
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
                // Apply penalty proportional to how much the distance exceeds the threshold
                longDistancePenalty = LONG_DISTANCE_PENALTY * (distance / LONG_DISTANCE_THRESHOLD - 1);
                Plugin.Log.Debug($"Applied long distance penalty of {longDistancePenalty:F2}s for distance {distance:F2}");
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
                Plugin.Log.Debug($"Teleport to {aetheryte.Name}: teleport time {teleportTime:F2}s + travel time {fromAetheryteTime:F2}s = {totalTime:F2}s");
                
                // If this route is faster, update the best time
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
        /// <returns>Optimized path</returns>
        public List<TreasureCoordinate> OptimizeRouteByTime(
            TreasureCoordinate startLocation, 
            List<TreasureCoordinate> coordinates, 
            List<AetheryteInfo> mapAetherytes)
        {
            if (coordinates.Count <= 1)
                return new List<TreasureCoordinate>(coordinates);
            
            // Create aetheryte coordinates
            var aetheryteCoords = new List<TreasureCoordinate>();
            foreach (var aetheryte in mapAetherytes)
            {
                var aetheryteCoord = new TreasureCoordinate(
                    aetheryte.Position.X, 
                    aetheryte.Position.Y, 
                    aetheryte.MapArea,
                    aetheryte.Name);
                aetheryteCoords.Add(aetheryteCoord);
            }
            
            if (aetheryteCoords.Count == 0)
            {
                Plugin.Log.Warning("No aetherytes found for the map. Path optimization may be suboptimal.");
                // If no aetherytes available, just return the coordinates as is
                return new List<TreasureCoordinate>(coordinates);
            }

            // Use the first aetheryte for logging purposes
            var primaryAetheryte = mapAetherytes[0];
                
            // Log recording
            Plugin.Log.Debug($"Starting path optimization, start point: ({startLocation.X:F1}, {startLocation.Y:F1}), {coordinates.Count} treasure points, {aetheryteCoords.Count} aetherytes available");
            
            // For 7 or fewer points, use permutation to find the optimal solution
            // This threshold is chosen based on the current maximum limit of 8 coordinates
            if (coordinates.Count <= 7)
            {
                Plugin.Log.Debug($"Using permutation method to optimize path for {coordinates.Count} points");
                var route = FindOptimalRouteByPermutation(startLocation, coordinates, aetheryteCoords);
                
                // Apply 2-opt local optimization
                return ApplyTwoOpt(route, aetheryteCoords);
            }
            
            // For 8 points (at the maximum limit), use a hybrid approach
            if (coordinates.Count == 8)
            {
                Plugin.Log.Debug("Using hybrid approach for 8 points - comparing permutation and nearest neighbor");
                
                // Try both methods and choose the better result
                // First, use the nearest neighbor algorithm with 2-opt
                var nnRoute = FindOptimalRouteByNearestNeighbor(startLocation, coordinates, aetheryteCoords);
                var optimizedNnRoute = ApplyTwoOpt(nnRoute, aetheryteCoords);
                float nnTime = CalculateRouteTotalTime(optimizedNnRoute);
                
                // Then, use permutation (which is computationally feasible for 8 points)
                var permRoute = FindOptimalRouteByPermutation(startLocation, coordinates, aetheryteCoords);
                var optimizedPermRoute = ApplyTwoOpt(permRoute, aetheryteCoords);
                float permTime = CalculateRouteTotalTime(optimizedPermRoute);
                
                // Choose the better result
                if (permTime < nnTime)
                {
                    Plugin.Log.Debug($"Permutation result is better: {permTime:F2}s vs {nnTime:F2}s");
                    return optimizedPermRoute;
                }
                else
                {
                    Plugin.Log.Debug($"Nearest neighbor result is better or equal: {nnTime:F2}s vs {permTime:F2}s");
                    return optimizedNnRoute;
                }
            }
            
            // For more than 8 points (future-proofing for possible limit increases)
            Plugin.Log.Debug($"Using approximation algorithm to optimize path for {coordinates.Count} points");
            var initialRoute = FindOptimalRouteByNearestNeighbor(startLocation, coordinates, aetheryteCoords);
            var optimizedRoute = ApplyTwoOpt(initialRoute, aetheryteCoords);
            
            // For a very large number of points (15+), further use simulated annealing
            if (coordinates.Count >= 15)
            {
                Plugin.Log.Debug("Using simulated annealing algorithm to further optimize the path");
                return OptimizeBySimulatedAnnealing(optimizedRoute, aetheryteCoords);
            }
            
            return optimizedRoute;
        }

        /// <summary>
        /// Use permutation method to find the optimal path (suitable for a small number of points)
        /// </summary>
        private List<TreasureCoordinate> FindOptimalRouteByPermutation(
            TreasureCoordinate startLocation, 
            List<TreasureCoordinate> coordinates, 
            List<TreasureCoordinate> aetheryteCoords)
        {
            var allPermutations = GetAllPermutations(coordinates);
            var bestRoute = new List<TreasureCoordinate>();
            float bestTime = float.MaxValue;
            
            foreach (var perm in allPermutations)
            {
                float totalTime = 0;
                var route = new List<TreasureCoordinate>();
                var currentPos = startLocation;
                bool isCurrentAetheryte = IsLocationAtAetheryte(currentPos, aetheryteCoords);
                
                // For each point in this permutation
                foreach (var coord in perm)
                {
                    // Calculate the best path to this coordinate, considering all aetherytes
                    TreasureCoordinate bestAetheryte = null;
                    float bestTimeCost = CalculateBestTimeCost(currentPos, coord, isCurrentAetheryte, aetheryteCoords, out bestAetheryte);
                    totalTime += bestTimeCost;
                    
                    // If using an aetheryte is better, add it to the path first
                    if (bestAetheryte != null)
                    {
                        // Create a teleport coordinate
                        var teleportCoord = new TreasureCoordinate(bestAetheryte.X, bestAetheryte.Y, bestAetheryte.MapArea, bestAetheryte.Name, bestAetheryte.PlayerName);
                        teleportCoord.Name = "[Teleport] " + teleportCoord.Name;
                        teleportCoord.NavigationInstruction = $"Teleport from ({currentPos.X:F1}, {currentPos.Y:F1}) to {bestAetheryte.Name} ({bestAetheryte.X:F1}, {bestAetheryte.Y:F1})";
                        
                        // Add to route
                        route.Add(teleportCoord);
                        
                        // Add navigation instruction for the treasure point
                        coord.NavigationInstruction = $"Travel from {bestAetheryte.Name} ({bestAetheryte.X:F1}, {bestAetheryte.Y:F1}) to ({coord.X:F1}, {coord.Y:F1})";
                        
                        // Update current position and flag
                        currentPos = coord;
                        isCurrentAetheryte = false;
                    }
                    else
                    {
                        // Add direct travel navigation instruction
                        coord.NavigationInstruction = $"Direct travel from ({currentPos.X:F1}, {currentPos.Y:F1}) to ({coord.X:F1}, {coord.Y:F1})";
                        
                        // Update current position and flag
                        currentPos = coord;
                        isCurrentAetheryte = false;
                    }
                    
                    // Add the coordinate to the route
                    route.Add(coord);
                }
                
                // Update best route if this permutation is better
                if (totalTime < bestTime)
                {
                    bestTime = totalTime;
                    bestRoute = new List<TreasureCoordinate>(route);
                    Plugin.Log.Debug($"Found better path, total time: {bestTime:F2} seconds");
                }
            }
            
            return bestRoute;
        }
        
        /// <summary>
        /// Checks if a location is at or very near to any aetheryte
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <param name="aetherytes">List of all aetherytes</param>
        /// <returns>True if the location is at any aetheryte</returns>
        private bool IsLocationAtAetheryte(TreasureCoordinate location, List<TreasureCoordinate> aetherytes)
        {
            if (aetherytes == null || aetherytes.Count == 0)
                return false;
                
            foreach (var aetheryte in aetherytes)
            {
                if (location.DistanceTo(aetheryte) < 1.0f)
                    return true;
            }
            
            return false;
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
        /// Use improved nearest neighbor algorithm to find approximate optimal path (suitable for a larger number of points)
        /// </summary>
        private List<TreasureCoordinate> FindOptimalRouteByNearestNeighbor(
            TreasureCoordinate startLocation, 
            List<TreasureCoordinate> coordinates, 
            List<TreasureCoordinate> aetheryteCoords)
        {
            var remainingCoords = new List<TreasureCoordinate>(coordinates);
            var route = new List<TreasureCoordinate>();
            var currentPos = startLocation;
            bool isCurrentAetheryte = currentPos.MapArea != coordinates[0].MapArea || 
                                     IsLocationAtAetheryte(currentPos, aetheryteCoords);
            
            // While there are still unvisited treasure points
            while (remainingCoords.Count > 0)
            {
                // Calculate the shortest time to each remaining treasure point
                var bestNextCoord = remainingCoords[0];
                float bestTime = float.MaxValue;
                TreasureCoordinate bestAetheryte = null;
                
                foreach (var coord in remainingCoords)
                {
                    // Calculate best time cost considering all aetherytes
                    TreasureCoordinate currentBestAetheryte = null;
                    float timeCost = CalculateBestTimeCost(currentPos, coord, isCurrentAetheryte, aetheryteCoords, out currentBestAetheryte);
                    
                    // If this is better than the current best time, update
                    if (timeCost < bestTime)
                    {
                        bestTime = timeCost;
                        bestNextCoord = coord;
                        bestAetheryte = currentBestAetheryte;
                    }
                }
                
                // Add to path
                if (bestAetheryte != null)
                {
                    // First teleport to the aetheryte
                    var teleportCoord = new TreasureCoordinate(bestAetheryte.X, bestAetheryte.Y, bestAetheryte.MapArea, bestAetheryte.Name, bestAetheryte.PlayerName);
                    teleportCoord.Name = "[Teleport] " + teleportCoord.Name;
                    teleportCoord.NavigationInstruction = $"Teleport from ({currentPos.X:F1}, {currentPos.Y:F1}) to {bestAetheryte.Name} ({bestAetheryte.X:F1}, {bestAetheryte.Y:F1})";
                    route.Add(teleportCoord);
                    
                    // Add navigation instruction for travel from aetheryte to target
                    bestNextCoord.NavigationInstruction = $"Travel from {bestAetheryte.Name} ({bestAetheryte.X:F1}, {bestAetheryte.Y:F1}) to ({bestNextCoord.X:F1}, {bestNextCoord.Y:F1})";
                    route.Add(bestNextCoord);
                    currentPos = bestNextCoord;
                    isCurrentAetheryte = false;
                }
                else
                {
                    // Add navigation instruction for direct travel
                    bestNextCoord.NavigationInstruction = $"Direct travel from ({currentPos.X:F1}, {currentPos.Y:F1}) to ({bestNextCoord.X:F1}, {bestNextCoord.Y:F1})";
                    route.Add(bestNextCoord);
                    currentPos = bestNextCoord;
                    isCurrentAetheryte = false;
                }
                
                // Remove visited treasure point from remaining list
                remainingCoords.Remove(bestNextCoord);
            }
            
            return route;
        }

        /// <summary>
        /// Use 2-opt algorithm to optimize the path
        /// </summary>
        private List<TreasureCoordinate> ApplyTwoOpt(List<TreasureCoordinate> route, List<TreasureCoordinate> aetheryteCoords)
        {
            if (route.Count <= 3)
                return route;
                
            bool improved = true;
            int iteration = 0;
            int maxIterations = 100; // Avoid infinite loop
            
            while (improved && iteration < maxIterations)
            {
                improved = false;
                float bestGain = 0;
                int bestI = -1;
                int bestJ = -1;
                
                // Try to swap all possible edges
                for (int i = 0; i < route.Count - 2; i++)
                {
                    for (int j = i + 2; j < route.Count - 1; j++)
                    {
                        // Skip edges containing aetherytes (points marked with [Teleport])
                        if (route[i].Name.Contains("[Teleport]") || route[i+1].Name.Contains("[Teleport]") ||
                            route[j].Name.Contains("[Teleport]") || route[j+1].Name.Contains("[Teleport]"))
                            continue;
                        
                        // Calculate time before swap using best aetheryte options
                        TreasureCoordinate unused1, unused2;
                        float timeBefore = CalculateBestTimeCost(route[i], route[i+1], false, aetheryteCoords, out unused1) + 
                                         CalculateBestTimeCost(route[j], route[j+1], false, aetheryteCoords, out unused2);
                        
                        // Calculate time after swap using best aetheryte options
                        TreasureCoordinate unused3, unused4;
                        float timeAfter = CalculateBestTimeCost(route[i], route[j], false, aetheryteCoords, out unused3) + 
                                        CalculateBestTimeCost(route[i+1], route[j+1], false, aetheryteCoords, out unused4);
                        
                        // If time is shorter after swap
                        float gain = timeBefore - timeAfter;
                        if (gain > bestGain)
                        {
                            bestGain = gain;
                            bestI = i;
                            bestJ = j;
                            improved = true;
                        }
                    }
                }
                
                // If a better path is found, swap the edges
                if (improved)
                {
                    // Reverse the segment from i+1 to j
                    int left = bestI + 1;
                    int right = bestJ;
                    while (left < right)
                    {
                        var temp = route[left];
                        route[left] = route[right];
                        route[right] = temp;
                        left++;
                        right--;
                    }
                    
                    Plugin.Log.Debug($"Applied 2-opt optimization, improvement time: {bestGain:F2} seconds");
                }
                
                iteration++;
            }
            
            return route;
        }

        /// <summary>
        /// Use simulated annealing algorithm to optimize the path
        /// </summary>
        private List<TreasureCoordinate> OptimizeBySimulatedAnnealing(
            List<TreasureCoordinate> initialRoute, 
            List<TreasureCoordinate> aetheryteCoords)
        {
            if (initialRoute.Count <= 3)
                return initialRoute;
                
            Random random = new Random();
            var currentRoute = new List<TreasureCoordinate>(initialRoute);
            var bestRoute = new List<TreasureCoordinate>(initialRoute);
            
            float currentEnergy = CalculateRouteTotalTime(currentRoute);
            float bestEnergy = currentEnergy;
            
            double temperature = 100.0;
            double coolingRate = 0.995;
            int iterations = 1000;
            
            for (int i = 0; i < iterations; i++)
            {
                // Create a new candidate solution
                var newRoute = new List<TreasureCoordinate>(currentRoute);
                
                // Randomly select two non-aetheryte positions and swap them
                int pos1, pos2;
                bool validSwap = false;
                int attempts = 0;
                
                // Try to find two swappable non-aetheryte positions
                do
                {
                    pos1 = random.Next(0, newRoute.Count);
                    pos2 = random.Next(0, newRoute.Count);
                    
                    validSwap = pos1 != pos2 && 
                               !newRoute[pos1].Name.Contains("[Teleport]") && 
                               !newRoute[pos2].Name.Contains("[Teleport]");
                               
                    attempts++;
                    if (attempts > 100) break; // Avoid infinite loop
                }
                while (!validSwap);
                
                if (validSwap)
                {
                    // Swap two positions
                    var temp = newRoute[pos1];
                    newRoute[pos1] = newRoute[pos2];
                    newRoute[pos2] = temp;
                    
                    // Calculate the energy (time) of the new path
                    float newEnergy = CalculateRouteTotalTime(newRoute);
                    
                    // Decide whether to accept the new solution
                    if (AcceptNewSolution(currentEnergy, newEnergy, temperature, random))
                    {
                        currentRoute = newRoute;
                        currentEnergy = newEnergy;
                        
                        // Update the best solution
                        if (newEnergy < bestEnergy)
                        {
                            bestRoute = new List<TreasureCoordinate>(newRoute);
                            bestEnergy = newEnergy;
                            Plugin.Log.Debug($"Simulated annealing found better path, total time: {bestEnergy:F2} seconds");
                        }
                    }
                }
                
                // Cooling down
                temperature *= coolingRate;
            }
            
            return bestRoute;
        }

        private bool AcceptNewSolution(float currentEnergy, float newEnergy, double temperature, Random random)
        {
            // If the new solution is better, always accept it
            if (newEnergy < currentEnergy)
                return true;
            
            // If the new solution is worse, accept it with a certain probability
            double acceptanceProbability = Math.Exp((currentEnergy - newEnergy) / temperature);
            return random.NextDouble() < acceptanceProbability;
        }

        private float CalculateRouteTotalTime(List<TreasureCoordinate> route)
        {
            if (route.Count <= 1)
                return 0;
                
            // Calculate the total time of the path
            float totalTime = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                bool isStartAetheryte = route[i].Name.Contains("[Teleport]");
                bool isEndAetheryte = route[i+1].Name.Contains("[Teleport]");
                totalTime += CalculateTimeCost(route[i], route[i+1], isStartAetheryte, isEndAetheryte);
            }
            return totalTime;
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
            for (int i = 0; i < route.Count - 1; i++)
            {
                bool isStartAetheryte = route[i].Name.Contains("[Teleport]");
                bool isEndAetheryte = route[i+1].Name.Contains("[Teleport]");
                totalTime += CalculateTimeCost(route[i], route[i+1], isStartAetheryte, isEndAetheryte);
            }
            
            // Consider collection time for each treasure point (assumed to be 10 seconds)
            int treasurePoints = route.Count(c => !c.Name.Contains("[Teleport]"));
            totalTime += treasurePoints * 10;
            
            return totalTime;
        }
    }
}
