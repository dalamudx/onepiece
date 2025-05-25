using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for getting player location information.
/// </summary>
public class PlayerLocationService
{
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private readonly TerritoryManager territoryManager;
    private readonly IGameGui gameGui;
    private readonly IDataManager dataManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerLocationService"/> class.
    /// </summary>
    /// <param name="clientState">The client state.</param>
    /// <param name="log">The plugin log.</param>
    /// <param name="territoryManager">The territory manager.</param>
    public PlayerLocationService(IClientState clientState, IPluginLog log, TerritoryManager territoryManager, IGameGui gameGui, IDataManager dataManager)
    {
        this.clientState = clientState;
        this.log = log;
        this.territoryManager = territoryManager;
        this.gameGui = gameGui;
        this.dataManager = dataManager;
    }

    /// <summary>
    /// Gets the current player location as a TreasureCoordinate.
    /// </summary>
    /// <returns>The player's current location, or null if not available.</returns>
    public TreasureCoordinate? GetCurrentLocation()
    {
        try
        {
            if (clientState.LocalPlayer == null)
            {
                log.Warning("Cannot get player location: LocalPlayer is null");
                return null;
            }

            var position = clientState.LocalPlayer.Position;
            var territoryId = clientState.TerritoryType;

            // Convert world position to map coordinates
            var territory = territoryManager.GetByTerritoryType(territoryId);
            if (territory == null)
            {
                log.Warning($"Cannot get player location: Unknown territory {territoryId}");
                return null;
            }

            // Get map offset values dynamically from game data
            var (offsetX, offsetY, scale) = GetMapOffsets(territory);
            
            // Convert world position to map coordinates using accurate formula
            var mapX = ConvertWorldToMapCoordinate(position.X, scale, offsetX);
            var mapY = ConvertWorldToMapCoordinate(position.Z, scale, offsetY);
            
#if DEBUG
            log.Debug($"Converting player position from world ({position.X:F1}, {position.Z:F1}) to map ({mapX:F1}, {mapY:F1}) in {territory.Name}");
#endif

            // Explicitly set coordinate system type to Map since we've converted from world coordinates
            return new TreasureCoordinate(mapX, mapY, territory.Name, CoordinateSystemType.Map);
        }
        catch (Exception ex)
        {
            log.Error($"Error getting player location: {ex.Message}");
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
                    log.Debug($"Got dynamic map offsets from AgentMap: X={offsetX}, Y={offsetY}, Scale={scale} (territory.Scale={territory.Scale})");
#endif
                }
                else
                {
                    log.Warning("Could not get map offsets from AgentMap, using zero offsets");
                    // No fallback, just use zeros
                    offsetX = 0;
                    offsetY = 0;
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error getting map offsets: {ex.Message}. Using fallback values.");
            
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
            log.Warning("Map scale factor is zero, using default value of 100");
            scale = 100;
        }
        else if (scale > 10000)
        {
            log.Warning($"Map scale factor {scale} seems unusually high, using default value of 100");
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
        log.Debug($"Map coordinate calculation: {offsetComponent} (offset) + {scaleComponent} (scale) + {coordComponent} (coord) + 1 = {result}");
#endif
        
        // Sanity check - map coordinates should typically be between 0 and 100
        if (result < 0 || result > 100)
        {
            log.Warning($"Calculated map coordinate {result} is outside typical range (0-100) for world coordinate {worldCoord}, scale {scale}, offset {offset}");
        }
        
        return result;
    }
}
