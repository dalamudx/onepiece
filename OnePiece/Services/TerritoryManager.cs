using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Manager for territory and map information.
/// </summary>
public class TerritoryManager
{
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private readonly IEnumerable<TerritoryDetail> territoryDetails;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerritoryManager"/> class.
    /// </summary>
    /// <param name="data">The data manager.</param>
    /// <param name="log">The plugin log.</param>
    public TerritoryManager(IDataManager data, IPluginLog log)
    {
        this.data = data;
        this.log = log;
        territoryDetails = LoadTerritoryDetails();
    }

    /// <summary>
    /// Gets a territory detail by zone name.
    /// </summary>
    /// <param name="zone">The zone name.</param>
    /// <param name="matchPartial">Whether to match partial zone names.</param>
    /// <returns>The territory detail, or null if not found.</returns>
    public TerritoryDetail? GetByZoneName(string zone, bool matchPartial = true)
    {
        if (string.IsNullOrEmpty(zone)) return null;

        if (!this.territoryDetails.Any())
        {
            log.Warning("Territory details list is empty, reloading...");
            LoadTerritoryDetails();
        }

        var territoryDetails = this.territoryDetails
                                   .Where(x => x.Name.Equals(zone, StringComparison.OrdinalIgnoreCase) ||
                                               (matchPartial && x.Name.Contains(zone, StringComparison.CurrentCultureIgnoreCase)))
                                   .OrderBy(x => x.Name.Length);

        var territoryDetail = territoryDetails.FirstOrDefault();

        if (territoryDetail == null)
        {
            log.Warning($"Could not find territory for zone: {zone}");
        }
        else
        {
            log.Information($"Found territory for zone: {zone} -> {territoryDetail.Name} (TerritoryId: {territoryDetail.TerritoryId}, MapId: {territoryDetail.MapId})");
        }

        return territoryDetail;
    }

    /// <summary>
    /// Gets a territory detail by territory type.
    /// </summary>
    /// <param name="territoryType">The territory type.</param>
    /// <returns>The territory detail, or null if not found.</returns>
    public TerritoryDetail? GetByTerritoryType(uint territoryType)
    {
        if (!territoryDetails.Any())
        {
            log.Warning("Territory details list is empty, reloading...");
            LoadTerritoryDetails();
        }

        var territoryDetail = territoryDetails.FirstOrDefault(x => x.TerritoryId == territoryType);

        if (territoryDetail == null)
        {
            log.Warning($"Could not find territory for type: {territoryType}");
        }
        else
        {
            log.Information($"Found territory for type: {territoryType} -> {territoryDetail.Name} (MapId: {territoryDetail.MapId})");
        }

        return territoryDetail;
    }

    /// <summary>
    /// Loads territory details from the game data.
    /// </summary>
    /// <returns>A collection of territory details.</returns>
    private IEnumerable<TerritoryDetail> LoadTerritoryDetails()
    {
        try
        {
            var territoryTypes = data.GetExcelSheet<TerritoryType>();

            var details = new List<TerritoryDetail>();

            foreach (var territoryType in territoryTypes)
            {
                try
                {
                    var type = territoryType.Bg.ToString().Split('/');
                    if (type.Length < 3) continue;

                    // Only include town, field, and housing areas
                    if (type[2] != "twn" && type[2] != "fld" && type[2] != "hou") continue;

                    // Check if Map reference is valid
                    if (territoryType.Map.ValueNullable == null) continue;
                    var map = territoryType.Map.Value;

                    // Check if PlaceName reference is valid
                    if (map.PlaceName.ValueNullable == null) continue;
                    var placeName = map.PlaceName.Value;

                    // Check if the place name is not empty
                    if (string.IsNullOrWhiteSpace(placeName.Name.ToString())) continue;

                    details.Add(new TerritoryDetail
                    {
                        TerritoryId = territoryType.RowId,
                        MapId = map.RowId,
                        SizeFactor = map.SizeFactor,
                        Name = placeName.Name.ToString()
                    });
                }
                catch (Exception ex)
                {
                    log.Error($"Error processing territory type {territoryType.RowId}: {ex.Message}");
                }
            }

            log.Information($"Loaded {details.Count} territory details");
            return details;
        }
        catch (Exception ex)
        {
            log.Error($"Error loading territory details: {ex.Message}");
            return Enumerable.Empty<TerritoryDetail>();
        }
    }
}
