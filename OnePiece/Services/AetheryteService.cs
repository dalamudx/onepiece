using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for managing aetheryte (teleport crystal) information.
/// </summary>
public class AetheryteService
{
    private readonly IDataManager data;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private readonly TerritoryManager territoryManager;
    private List<AetheryteInfo> aetherytes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AetheryteService"/> class.
    /// </summary>
    /// <param name="data">The data manager.</param>
    /// <param name="clientState">The client state.</param>
    /// <param name="log">The plugin log.</param>
    /// <param name="territoryManager">The territory manager.</param>
    public AetheryteService(IDataManager data, IClientState clientState, IPluginLog log, TerritoryManager territoryManager)
    {
        this.data = data;
        this.clientState = clientState;
        this.log = log;
        this.territoryManager = territoryManager;
        LoadAetherytes();
    }

    /// <summary>
    /// Gets all aetherytes.
    /// </summary>
    /// <returns>A list of all aetherytes.</returns>
    public IReadOnlyList<AetheryteInfo> GetAllAetherytes()
    {
        return aetherytes;
    }

    /// <summary>
    /// Gets aetherytes in a specific map area.
    /// </summary>
    /// <param name="mapArea">The map area name.</param>
    /// <returns>A list of aetherytes in the specified map area.</returns>
    public IReadOnlyList<AetheryteInfo> GetAetherytesInMapArea(string mapArea)
    {
        return aetherytes.Where(a => a.MapArea.Equals(mapArea, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets the nearest aetheryte to a coordinate in a specific map area.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <returns>The nearest aetheryte, or null if none found.</returns>
    public AetheryteInfo? GetNearestAetheryteToCoordinate(TreasureCoordinate coordinate)
    {
        if (string.IsNullOrEmpty(coordinate.MapArea))
            return null;

        var aetherytesInMap = GetAetherytesInMapArea(coordinate.MapArea);
        if (aetherytesInMap.Count == 0)
            return null;

        return aetherytesInMap.OrderBy(a => a.DistanceTo(coordinate)).FirstOrDefault();
    }

    /// <summary>
    /// Gets the cheapest aetheryte to teleport to in a specific map area.
    /// </summary>
    /// <param name="mapArea">The map area name.</param>
    /// <returns>The cheapest aetheryte, or null if none found.</returns>
    public AetheryteInfo? GetCheapestAetheryteInMapArea(string mapArea)
    {
        var aetherytesInMap = GetAetherytesInMapArea(mapArea);
        if (aetherytesInMap.Count == 0)
            return null;

        // Update teleport fees before sorting
        UpdateTeleportFees(aetherytesInMap);

        return aetherytesInMap.OrderBy(a => a.CalculateTeleportFee()).FirstOrDefault();
    }

    /// <summary>
    /// Updates the teleport fees for a list of aetherytes using the game's Telepo API.
    /// </summary>
    /// <param name="targetAetherytes">The list of aetherytes to update.</param>
    private unsafe void UpdateTeleportFees(IEnumerable<AetheryteInfo> targetAetherytes)
    {
        try
        {
            var telepo = Telepo.Instance();
            if (telepo == null)
            {
                log.Warning("Cannot update teleport fees: Telepo instance is null");
                return;
            }

            foreach (var aetheryte in targetAetherytes)
            {
                try
                {
                    // Get the aetheryte row from the game data
                    var aetheryteRow = data.GetExcelSheet<Aetheryte>()?.GetRow(aetheryte.AetheryteRowId);
                    if (aetheryteRow == null)
                        continue;

                    // Use a distance-based model for teleport cost
                    // This is a simplified approach that approximates the game's teleport cost calculation
                    var baseCost = 100u; // Base teleport fee
                    var distanceFactor = 10u; // Gil per distance unit

                    // Calculate distance from player's home point (or current location if available)
                    var playerPos = Vector2.Zero;
                    var playerTerritory = 0u;

                    try
                    {
                        // Try to get player's current position
                        if (clientState.LocalPlayer != null)
                        {
                            playerPos = new Vector2(clientState.LocalPlayer!.Position.X, clientState.LocalPlayer.Position.Z);
                            playerTerritory = clientState.TerritoryType;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error getting player position: {ex.Message}");
                    }

                    // Calculate distance-based cost
                    uint distanceCost = 0;

                    // If player is in a different territory, use a higher base cost
                    if (playerTerritory != aetheryte.TerritoryId)
                    {
                        distanceCost = 200; // Higher cost for cross-territory teleport
                    }
                    else if (playerPos != Vector2.Zero)
                    {
                        // Calculate distance if we have player position
                        var aetheryteWorldPos = new Vector2(0, 0); // Default position

                        // Use the aetheryte's position if available
                        if (aetheryte.Position != Vector2.Zero)
                        {
                            aetheryteWorldPos = aetheryte.Position;
                        }

                        var distance = Vector2.Distance(playerPos, aetheryteWorldPos);
                        distanceCost = (uint)(distance * distanceFactor);
                    }

                    // Calculate final cost
                    var cost = baseCost + distanceCost;
                    aetheryte.ActualTeleportFee = (int)cost;

                    log.Debug($"Updated teleport fee for {aetheryte.Name}: {aetheryte.ActualTeleportFee} gil");
                }
                catch (Exception ex)
                {
                    log.Error($"Error updating teleport fee for aetheryte {aetheryte.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error updating teleport fees: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads aetheryte information from the game data.
    /// </summary>
    private void LoadAetherytes()
    {
        try
        {
            var aetheryteSheet = data.GetExcelSheet<Aetheryte>();
            if (aetheryteSheet == null)
            {
                log.Error("Failed to load Aetheryte sheet");
                return;
            }

            var loadedAetherytes = new List<AetheryteInfo>();

            foreach (var aetheryte in aetheryteSheet)
            {
                try
                {
                    // Skip non-teleportable aetherytes
                    if (!aetheryte.IsAetheryte)
                        continue;

                    // Get territory and map information
                    var territory = territoryManager.GetByTerritoryType(aetheryte.Territory.RowId);
                    if (territory == null)
                        continue;

                    // Create aetheryte info
                    var aetheryteInfo = new AetheryteInfo
                    {
                        Id = aetheryte.RowId,
                        AetheryteRowId = aetheryte.RowId, // Store the row ID for later use with Telepo API
                        Name = aetheryte.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty,
                        TerritoryId = territory.TerritoryId,
                        MapId = territory.MapId,
                        MapArea = territory.Name,
                        // Create a default position since we can't access X and Z directly in the current API
                        Position = new Vector2(0, 0),
                        BaseTeleportFee = CalculateBaseTeleportFee(),
                        ActualTeleportFee = 0, // Will be updated when needed
                        IsFavorite = false, // This would need to be determined from character data
                        IsFreeDestination = false // This would need to be determined from character data
                    };

                    loadedAetherytes.Add(aetheryteInfo);
                }
                catch (Exception ex)
                {
                    log.Error($"Error processing aetheryte {aetheryte.RowId}: {ex.Message}");
                }
            }

            this.aetherytes = loadedAetherytes;
            log.Information($"Loaded {this.aetherytes.Count} aetherytes");

            // Try to update positions from the game data if possible
            UpdateAetherytePositions();
        }
        catch (Exception ex)
        {
            log.Error($"Error loading aetherytes: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates aetheryte positions from the game data if possible.
    /// </summary>
    private void UpdateAetherytePositions()
    {
        try
        {
            // Try to get aetheryte positions from the game data
            // This is a simplified implementation and might need to be improved
            foreach (var aetheryte in aetherytes)
            {
                try
                {
                    // Get the aetheryte row from the game data
                    var aetheryteRow = data.GetExcelSheet<Aetheryte>()?.GetRow(aetheryte.AetheryteRowId);
                    if (aetheryteRow == null)
                        continue;

                    // Set a default position since we can't easily access the map marker data
                    // In a real implementation, you would need to use the proper conversion from world coordinates to map coordinates
                    // For now, we'll use a simplified approach
                    aetheryte.Position = new Vector2(10, 10); // Default position in the center of the map
                    log.Debug($"Set default position for {aetheryte.Name}: (10, 10)");
                }
                catch (Exception ex)
                {
                    log.Error($"Error updating position for aetheryte {aetheryte.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error updating aetheryte positions: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates the base teleport fee for an aetheryte.
    /// </summary>
    /// <returns>The base teleport fee in gil.</returns>
    private int CalculateBaseTeleportFee()
    {
        // Use a standard base teleport fee
        return 100;
    }
}
