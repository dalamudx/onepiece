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
    public unsafe void UpdateTeleportFees(IEnumerable<AetheryteInfo> targetAetherytes)
    {
        try
        {
            var telepo = Telepo.Instance();
            if (telepo == null)
            {
                log.Warning("Cannot update teleport fees: Telepo instance is null");
                return;
            }

            // Get current player territory ID
            uint playerTerritory = 0;
            try
            {
                if (clientState.LocalPlayer != null)
                {
                    playerTerritory = clientState.TerritoryType;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error getting player territory: {ex.Message}");
            }

            // Update the aetheryte list to ensure we have current data
            telepo->UpdateAetheryteList();

            foreach (var aetheryte in targetAetherytes)
            {
                try
                {
                    // Check if player is in the same territory as the aetheryte
                    if (playerTerritory != 0 && playerTerritory == aetheryte.TerritoryId)
                    {
                        // If in the same map, teleport cost is fixed at 70 gil
                        aetheryte.ActualTeleportFee = 70;
                        log.Debug($"Same territory teleport fee for {aetheryte.Name}: 70 gil");
                        continue;
                    }

                    // Get the real teleport cost from Telepo API
                    bool foundCost = false;
                    
                    // Search for the aetheryte in the teleport list
                    int count = telepo->TeleportList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var info = telepo->TeleportList[i];
                        if (info.AetheryteId == aetheryte.AetheryteRowId)
                        {
                            aetheryte.ActualTeleportFee = (int)info.GilCost;
                            foundCost = true;
                            log.Debug($"Found teleport fee for {aetheryte.Name} from Telepo API: {aetheryte.ActualTeleportFee} gil");
                            break;
                        }
                    }

                    // If we couldn't find the cost, use a reasonable fallback
                    if (!foundCost)
                    {
                        // Different territory teleport - use a reasonable default
                        // In real game, this can vary from ~100-600 gil depending on distance
                        aetheryte.ActualTeleportFee = 300;
                        log.Debug($"Using default teleport fee for {aetheryte.Name}: 300 gil");
                    }
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
