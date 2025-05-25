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
            
            if (isStartAetheryte)
            {
                // Aetheryte → Treasure Map: Casting(5s) + Loading(3s) + Mount Summoning(1s) + Movement
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
                    // Treasure Map → Treasure Map: Mount Summoning(1s) + Movement
                    timeCost = MOUNT_SUMMON_TIME + (distance / MOUNT_SPEED);
                }
            }
            
            return timeCost;
        }

        /// <summary>
        /// Optimize path within the same map, considering time factors
        /// </summary>
        /// <param name="startLocation">Starting location</param>
        /// <param name="coordinates">Treasure map coordinates to collect</param>
        /// <param name="aetheryte">Aetheryte in the map</param>
        /// <returns>Optimized path</returns>
        public List<TreasureCoordinate> OptimizeRouteByTime(
            TreasureCoordinate startLocation, 
            List<TreasureCoordinate> coordinates, 
            AetheryteInfo aetheryte)
        {
            if (coordinates.Count <= 1)
                return new List<TreasureCoordinate>(coordinates);
            
            // Create aetheryte coordinates
            var aetheryteCoord = new TreasureCoordinate(
                aetheryte.Position.X, 
                aetheryte.Position.Y, 
                aetheryte.MapArea);
                
            // Log recording
            Plugin.Log.Debug($"Starting path optimization, start point: ({startLocation.X:F1}, {startLocation.Y:F1}), {coordinates.Count} treasure points, aetheryte: {aetheryte.Name} ({aetheryteCoord.X:F1}, {aetheryteCoord.Y:F1})");
            
            // For a small number of points, use permutation to find the optimal solution
            if (coordinates.Count <= 6)
            {
                Plugin.Log.Debug($"Using permutation method to optimize path for {coordinates.Count} points");
                var route = FindOptimalRouteByPermutation(startLocation, coordinates, aetheryteCoord);
                
                // Apply 2-opt local optimization
                return ApplyTwoOpt(route, aetheryteCoord);
            }
            
            // For a large number of points, use approximation algorithm
            Plugin.Log.Debug($"Using approximation algorithm to optimize path for {coordinates.Count} points");
            var initialRoute = FindOptimalRouteByNearestNeighbor(startLocation, coordinates, aetheryteCoord);
            
            // Apply 2-opt local optimization
            var optimizedRoute = ApplyTwoOpt(initialRoute, aetheryteCoord);
            
            // For a very large number of points, can further use simulated annealing
            if (coordinates.Count > 10)
            {
                Plugin.Log.Debug("Using simulated annealing algorithm to further optimize the path");
                return OptimizeBySimulatedAnnealing(optimizedRoute, aetheryteCoord);
            }
            
            return optimizedRoute;
        }

        /// <summary>
        /// Use permutation method to find the optimal path (suitable for a small number of points)
        /// </summary>
        private List<TreasureCoordinate> FindOptimalRouteByPermutation(
            TreasureCoordinate startLocation, 
            List<TreasureCoordinate> coordinates, 
            TreasureCoordinate aetheryteCoord)
        {
            // Get all possible coordinate permutations
            var permutations = GetAllPermutations(coordinates);
            float bestTime = float.MaxValue;
            List<TreasureCoordinate> bestRoute = null;
            
            // Calculate total time for each permutation
            foreach (var permutation in permutations)
            {
                var route = new List<TreasureCoordinate>();
                float totalTime = 0;
                var currentPos = startLocation;
                bool isCurrentAetheryte = currentPos.MapArea != coordinates[0].MapArea || 
                                        currentPos.DistanceTo(aetheryteCoord) < 1.0f;
                
                // Traverse all treasure points
                foreach (var coord in permutation)
                {
                    // Calculate time for direct travel
                    float directTime = CalculateTimeCost(currentPos, coord, isCurrentAetheryte, false);
                    
                    // Calculate time via aetheryte
                    float viaAetheryteTime = float.MaxValue;
                    if (!isCurrentAetheryte) // If not currently at an aetheryte
                    {
                        float toAetheryteTime = CalculateTimeCost(currentPos, aetheryteCoord, isCurrentAetheryte, true);
                        float fromAetheryteTime = CalculateTimeCost(aetheryteCoord, coord, true, false);
                        viaAetheryteTime = toAetheryteTime + fromAetheryteTime;
                    }
                    
                    // Choose the shorter path by time
                    if (directTime <= viaAetheryteTime)
                    {
                        totalTime += directTime;
                        route.Add(coord);
                        currentPos = coord;
                        isCurrentAetheryte = false;
                    }
                    else
                    {
                        // First teleport to the aetheryte
                        var teleportCoord = new TreasureCoordinate(aetheryteCoord.X, aetheryteCoord.Y, aetheryteCoord.MapArea, aetheryteCoord.Name, aetheryteCoord.PlayerName);
                        teleportCoord.Name = "[Teleport] " + teleportCoord.Name;
                        route.Add(teleportCoord);
                        
                        totalTime += viaAetheryteTime;
                        route.Add(coord);
                        currentPos = coord;
                        isCurrentAetheryte = false;
                    }
                }
                
                // If this path is better, update the best path
                if (totalTime < bestTime)
                {
                    bestTime = totalTime;
                    bestRoute = route;
                    Plugin.Log.Debug($"Found better path, total time: {bestTime:F2} seconds");
                }
            }
            
            return bestRoute;
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
            if (start == coordinates.Count - 1)
            {
                result.Add(new List<TreasureCoordinate>(coordinates));
                return;
            }
            
            for (int i = start; i < coordinates.Count; i++)
            {
                // Swap elements
                var temp = coordinates[start];
                coordinates[start] = coordinates[i];
                coordinates[i] = temp;
                
                // Recursively generate permutations
                PermuteHelper(coordinates, start + 1, result);
                
                // Restore original order
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
            TreasureCoordinate aetheryteCoord)
        {
            var remainingCoords = new List<TreasureCoordinate>(coordinates);
            var route = new List<TreasureCoordinate>();
            var currentPos = startLocation;
            bool isCurrentAetheryte = currentPos.MapArea != coordinates[0].MapArea || 
                                    currentPos.DistanceTo(aetheryteCoord) < 1.0f;
            
            // While there are still unvisited treasure points
            while (remainingCoords.Count > 0)
            {
                // Calculate the shortest time to each remaining treasure point
                var bestNextCoord = remainingCoords[0];
                float bestTime = float.MaxValue;
                bool viaAetheryte = false;
                
                foreach (var coord in remainingCoords)
                {
                    // Calculate time for direct travel
                    float directTime = CalculateTimeCost(currentPos, coord, isCurrentAetheryte, false);
                    
                    // Calculate time via aetheryte
                    float viaAetheryteTime = float.MaxValue;
                    if (!isCurrentAetheryte) // If not currently at an aetheryte
                    {
                        float toAetheryteTime = CalculateTimeCost(currentPos, aetheryteCoord, isCurrentAetheryte, true);
                        float fromAetheryteTime = CalculateTimeCost(aetheryteCoord, coord, true, false);
                        viaAetheryteTime = toAetheryteTime + fromAetheryteTime;
                    }
                    
                    // Choose the shorter path by time
                    if (directTime <= viaAetheryteTime && directTime < bestTime)
                    {
                        bestTime = directTime;
                        bestNextCoord = coord;
                        viaAetheryte = false;
                    }
                    else if (viaAetheryteTime < directTime && viaAetheryteTime < bestTime)
                    {
                        bestTime = viaAetheryteTime;
                        bestNextCoord = coord;
                        viaAetheryte = true;
                    }
                }
                
                // Add to path
                if (viaAetheryte)
                {
                    // First teleport to the aetheryte
                    var teleportCoord = new TreasureCoordinate(aetheryteCoord.X, aetheryteCoord.Y, aetheryteCoord.MapArea, aetheryteCoord.Name, aetheryteCoord.PlayerName);
                    teleportCoord.Name = "[Teleport] " + teleportCoord.Name;
                    route.Add(teleportCoord);
                    
                    route.Add(bestNextCoord);
                    currentPos = bestNextCoord;
                    isCurrentAetheryte = false;
                }
                else
                {
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
        private List<TreasureCoordinate> ApplyTwoOpt(List<TreasureCoordinate> route, TreasureCoordinate aetheryteCoord)
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
                        
                        // Calculate time before swap
                        float timeBefore = CalculateTimeCost(route[i], route[i+1], false, false) + 
                                        CalculateTimeCost(route[j], route[j+1], false, false);
                        
                        // Calculate time after swap
                        float timeAfter = CalculateTimeCost(route[i], route[j], false, false) + 
                                        CalculateTimeCost(route[i+1], route[j+1], false, false);
                        
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
            TreasureCoordinate aetheryteCoord)
        {
            if (initialRoute.Count <= 3)
                return initialRoute;
                
            Random random = new Random();
            var currentRoute = new List<TreasureCoordinate>(initialRoute);
            var bestRoute = new List<TreasureCoordinate>(initialRoute);
            
            float currentEnergy = CalculateRouteTotalTime(currentRoute, aetheryteCoord);
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
                    float newEnergy = CalculateRouteTotalTime(newRoute, aetheryteCoord);
                    
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

        private float CalculateRouteTotalTime(List<TreasureCoordinate> route, TreasureCoordinate aetheryteCoord)
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
