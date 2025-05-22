using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for getting player location information.
/// </summary>
public class PlayerLocationService
{
    private readonly IClientState _clientState;
    private readonly IPluginLog _log;
    private readonly TerritoryManager _territoryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerLocationService"/> class.
    /// </summary>
    /// <param name="clientState">The client state.</param>
    /// <param name="log">The plugin log.</param>
    /// <param name="territoryManager">The territory manager.</param>
    public PlayerLocationService(IClientState clientState, IPluginLog log, TerritoryManager territoryManager)
    {
        _clientState = clientState;
        _log = log;
        _territoryManager = territoryManager;
    }

    /// <summary>
    /// Gets the current player location as a TreasureCoordinate.
    /// </summary>
    /// <returns>The player's current location, or null if not available.</returns>
    public TreasureCoordinate? GetCurrentLocation()
    {
        try
        {
            if (_clientState.LocalPlayer == null)
            {
                _log.Warning("Cannot get player location: LocalPlayer is null");
                return null;
            }

            var position = _clientState.LocalPlayer.Position;
            var territoryId = _clientState.TerritoryType;

            // Convert world position to map coordinates
            var territory = _territoryManager.GetByTerritoryType(territoryId);
            if (territory == null)
            {
                _log.Warning($"Cannot get player location: Unknown territory {territoryId}");
                return null;
            }

            // Convert world position to map coordinates (this is a simplified conversion)
            // In a real implementation, you would need to use the proper conversion formula
            var mapX = ConvertWorldToMapCoordinate(position.X, territory.Scale);
            var mapY = ConvertWorldToMapCoordinate(position.Z, territory.Scale);

            return new TreasureCoordinate(mapX, mapY, territory.Name);
        }
        catch (Exception ex)
        {
            _log.Error($"Error getting player location: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a world coordinate to a map coordinate.
    /// </summary>
    /// <param name="worldCoord">The world coordinate.</param>
    /// <param name="scale">The map scale factor.</param>
    /// <returns>The map coordinate.</returns>
    private float ConvertWorldToMapCoordinate(float worldCoord, float scale)
    {
        // This is a simplified conversion formula
        // In a real implementation, you would need to use the proper conversion formula
        // based on the map's origin and scale
        return (worldCoord / scale) + 21.0f;
    }
}
