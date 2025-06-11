using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using OnePiece.Models;
using OnePiece.Helpers;

namespace OnePiece.Services;

/// <summary>
/// Service for getting player location information.
/// </summary>
public class PlayerLocationService
{
    private readonly IClientState clientState;
    private readonly TerritoryManager territoryManager;
    private readonly IGameGui gameGui;
    private readonly IDataManager dataManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerLocationService"/> class.
    /// </summary>
    /// <param name="clientState">The client state.</param>
    /// <param name="territoryManager">The territory manager.</param>
    public PlayerLocationService(IClientState clientState, TerritoryManager territoryManager, IGameGui gameGui, IDataManager dataManager)
    {
        this.clientState = clientState;
        this.territoryManager = territoryManager;
        this.gameGui = gameGui;
        this.dataManager = dataManager;
    }

    /// <summary>
    /// Gets the current player location as a TreasureCoordinate.
    /// This method must be called from the main thread.
    /// </summary>
    /// <returns>The player's current location, or null if not available.</returns>
    public TreasureCoordinate? GetCurrentLocation()
    {
        try
        {
            // Check if we're on the main thread
            if (!ThreadSafetyHelper.IsMainThread())
            {
                Plugin.Log.Warning($"GetCurrentLocation called from non-main thread ({ThreadSafetyHelper.GetThreadInfo()}), returning null");
                return null;
            }

            // Check if client state is valid
            if (clientState == null)
            {
                Plugin.Log.Error("Cannot get player location: ClientState is null");
                return null;
            }

            // Check if player is logged in
            if (!clientState.IsLoggedIn)
            {
                Plugin.Log.Warning("Cannot get player location: Player is not logged in");
                return null;
            }

            var localPlayer = clientState.LocalPlayer;
            if (localPlayer == null)
            {
                Plugin.Log.Warning("Cannot get player location: LocalPlayer is null");
                return null;
            }

            var position = localPlayer.Position;
            // Vector3 is a value type and cannot be null, but we can check for invalid coordinates
            if (float.IsNaN(position.X) || float.IsNaN(position.Y) || float.IsNaN(position.Z))
            {
                Plugin.Log.Warning("Cannot get player location: Player position contains invalid coordinates");
                return null;
            }

            var territoryId = clientState.TerritoryType;
            if (territoryId == 0)
            {
                Plugin.Log.Warning("Cannot get player location: Territory ID is 0");
                return null;
            }

            // Convert world position to map coordinates
            var territory = territoryManager.GetByTerritoryType(territoryId);
            if (territory == null)
            {
                Plugin.Log.Warning($"Cannot get player location: Unknown territory {territoryId}");
                return null;
            }

            // Get map offset values dynamically from game data
            var (offsetX, offsetY, scale) = GetMapOffsets(territory);
            
            // Convert world position to map coordinates using accurate formula
            var mapX = ConvertWorldToMapCoordinate(position.X, scale, offsetX);
            var mapY = ConvertWorldToMapCoordinate(position.Z, scale, offsetY);
            
#if DEBUG
            Plugin.Log.Debug($"Converting player position from world ({position.X:F1}, {position.Z:F1}) to map ({mapX:F1}, {mapY:F1}) in {territory.Name}");
#endif

            // Explicitly set coordinate system type to Map since we've converted from world coordinates
            return new TreasureCoordinate(mapX, mapY, territory.Name, CoordinateSystemType.Map);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting player location: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Gets map offsets and scale from AgentMap or fallback values.
    /// </summary>
    /// <param name="territory">The territory information.</param>
    /// <returns>Tuple containing offsetX, offsetY, and scale.</returns>
    private (int offsetX, int offsetY, uint scale) GetMapOffsets(TerritoryDetail territory)
    {
        int offsetX = 0;
        int offsetY = 0;
        uint scale = (uint)territory.Scale;
        
        try
        {
            // Check if we're on the main thread before accessing AgentMap
            if (!ThreadSafetyHelper.IsMainThread())
            {
                Plugin.Log.Debug($"GetMapOffsets called from non-main thread ({ThreadSafetyHelper.GetThreadInfo()}), using zero offsets");
                offsetX = 0;
                offsetY = 0;
                return (offsetX, offsetY, scale);
            }

            // Attempt to get current map information using FFXIVClientStructs
            unsafe
            {
                var agentMap = AgentMap.Instance();
                if (agentMap != null && agentMap->CurrentMapId > 0)
                {
                    // Offsets from AgentMap are sign-flipped compared to what we need
                    // Per Dalamud's MapUtil documentation: https://github.com/aers/FFXIVClientStructs/issues/1029
                    offsetX = -agentMap->CurrentOffsetX;
                    offsetY = -agentMap->CurrentOffsetY;
                    scale = (uint)agentMap->CurrentMapSizeFactor;

#if DEBUG
                    Plugin.Log.Debug($"Got dynamic map offsets from AgentMap: X={offsetX}, Y={offsetY}, Scale={scale} (territory.Scale={territory.Scale})");
#endif
                }
                else
                {
                    Plugin.Log.Warning("Could not get map offsets from AgentMap, using zero offsets");
                    // No fallback, just use zeros
                    offsetX = 0;
                    offsetY = 0;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting map offsets: {ex.Message}. Using fallback values.");

            // No fallback, just use zeros
            offsetX = 0;
            offsetY = 0;
        }
        
        return (offsetX, offsetY, scale);
    }

    /// <summary>
    /// Converts a world coordinate to a map coordinate using the accurate conversion formula.
    /// </summary>
    /// <param name="worldCoord">The world coordinate.</param>
    /// <param name="scale">The map scale factor.</param>
    /// <param name="offset">The dimension offset for either X or Z.</param>
    /// <returns>The map coordinate.</returns>
    private float ConvertWorldToMapCoordinate(float worldCoord, uint scale, int offset)
    {
        // Validate scale to avoid division by zero or very small values
        if (scale == 0)
        {
            Plugin.Log.Warning("Map scale factor is zero, using default value of 100");
            scale = 100;
        }
        else if (scale > 10000)
        {
            Plugin.Log.Warning($"Map scale factor {scale} seems unusually high, using default value of 100");
            scale = 100;
        }
        
        // Accurate conversion formula from Dalamud MapUtil
        // Formula: (0.02f * offset) + (2048f / scale) + (0.02f * worldCoord) + 1.0f
        float offsetComponent = 0.02f * offset;
        float scaleComponent = 2048f / scale;
        float coordComponent = 0.02f * worldCoord;
        float result = offsetComponent + scaleComponent + coordComponent + 1.0f;
        
#if DEBUG
        // Log detailed calculation for debugging
        Plugin.Log.Debug($"Map coordinate calculation: {offsetComponent} (offset) + {scaleComponent} (scale) + {coordComponent} (coord) + 1 = {result}");
#endif

        // Sanity check - map coordinates should typically be between 0 and 100
        if (result < 0 || result > 100)
        {
            Plugin.Log.Warning($"Calculated map coordinate {result} is outside typical range (0-100) for world coordinate {worldCoord}, scale {scale}, offset {offset}");
        }
        
        return result;
    }


}
