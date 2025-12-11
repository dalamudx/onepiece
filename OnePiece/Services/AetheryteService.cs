using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using OnePiece.Models;
using OnePiece.Helpers;

namespace OnePiece.Services;

/// <summary>
/// Service for managing aetheryte (teleport crystal) information.
/// Loads data exclusively from aetheryte.json static file.
/// </summary>
public class AetheryteService : IDisposable
{
    private readonly IClientState clientState;
    private readonly IObjectTable objectTable;
    private List<AetheryteInfo> aetherytes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AetheryteService"/> class.
    /// </summary>
    /// <param name="clientState">The client state.</param>
    /// <param name="objectTable">The object table.</param>
    public AetheryteService(IClientState clientState, IObjectTable objectTable)
    {
        this.clientState = clientState;
        this.objectTable = objectTable;
        LoadAetherytesFromJson();
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
    /// <param name="mapArea">The map area name (should be in English).</param>
    /// <returns>A list of aetherytes in the specified map area.</returns>
    public IReadOnlyList<AetheryteInfo> GetAetherytesInMapArea(string mapArea)
    {
        return aetherytes.Where(a => a.MapArea.Equals(mapArea, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets the nearest aetheryte to a coordinate in a specific map area.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="translationService">Optional translation service for map area names.</param>
    /// <returns>The nearest aetheryte, or null if none found.</returns>
    public AetheryteInfo? GetNearestAetheryteToCoordinate(TreasureCoordinate coordinate, OnePiece.Services.MapAreaTranslationService? translationService = null)
    {
        if (string.IsNullOrEmpty(coordinate.MapArea))
            return null;

        // Use English map area name for aetheryte lookup
        var englishMapArea = coordinate.GetEnglishMapArea(translationService);
        var aetherytesInMap = GetAetherytesInMapArea(englishMapArea);
        if (aetherytesInMap.Count == 0)
            return null;

        return aetherytesInMap.OrderBy(a => a.DistanceTo(coordinate)).FirstOrDefault();
    }
    
    /// <summary>
    /// Gets an aetheryte by its name.
    /// </summary>
    /// <param name="name">The name of the aetheryte.</param>
    /// <returns>The aetheryte info, or null if not found.</returns>
    public AetheryteInfo? GetAetheryteByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
            
        return aetherytes.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets an aetheryte by its ID.
    /// </summary>
    /// <param name="id">The ID of the aetheryte.</param>
    /// <returns>The aetheryte info, or null if not found.</returns>
    public AetheryteInfo? GetAetheryteById(uint id)
    {
        if (id == 0)
            return null;

        return aetherytes.FirstOrDefault(a => a.AetheryteId == id);
    }

    /// <summary>
    /// Gets all valid map areas that have aetherytes.
    /// </summary>
    /// <returns>A set of valid map area names.</returns>
    public IReadOnlySet<string> GetValidMapAreas()
    {
        return aetherytes
            .Where(a => !string.IsNullOrWhiteSpace(a.MapArea))
            .Select(a => a.MapArea)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a map area is valid (has aetherytes).
    /// </summary>
    /// <param name="mapArea">The map area to check (should be in English).</param>
    /// <returns>True if the map area is valid, false otherwise.</returns>
    public bool IsValidMapArea(string mapArea)
    {
        if (string.IsNullOrWhiteSpace(mapArea))
            return false;

        return aetherytes.Any(a => a.MapArea.Equals(mapArea, StringComparison.OrdinalIgnoreCase));
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
                Plugin.Log.Warning("Cannot update teleport fees: Telepo instance is null");
                return;
            }

            // Get current player territory ID (must be called from main thread)
            uint playerTerritory = 0;
            try
            {
                // Check if we're on the main thread before accessing game state
                if (ThreadSafetyHelper.IsMainThread() && objectTable.LocalPlayer != null)
                {
                    playerTerritory = clientState.TerritoryType;
                }
                else
                {
                    Plugin.Log.Debug($"Skipping territory check - not on main thread ({ThreadSafetyHelper.GetThreadInfo()}) or player not available");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error getting player territory: {ex.Message}");
            }

            // Update the aetheryte list to ensure we have current data
            telepo->UpdateAetheryteList();

            foreach (var aetheryte in targetAetherytes)
            {
                try
                {
                    // Note: Since we're using JSON-only data, we don't have TerritoryId
                    // We'll rely on the Telepo API to provide accurate costs for all cases

                    // Get the real teleport cost from Telepo API
                    bool foundCost = false;
                    
                    // Search for the aetheryte in the teleport list
                    int count = telepo->TeleportList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var info = telepo->TeleportList[i];
                        if (info.AetheryteId == aetheryte.AetheryteId)
                        {
                            aetheryte.ActualTeleportFee = (int)info.GilCost;
                            foundCost = true;
                            Plugin.Log.Debug($"Found teleport fee for {aetheryte.Name} from Telepo API: {aetheryte.ActualTeleportFee} gil");
                            break;
                        }
                    }

                    // If we couldn't find the cost, leave ActualTeleportFee as 0
                    if (!foundCost)
                    {
                        Plugin.Log.Debug($"Could not find teleport fee for {aetheryte.Name} in Telepo API");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Error updating teleport fee for aetheryte {aetheryte.AetheryteId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error updating teleport fees: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads aetheryte information exclusively from aetheryte.json file.
    /// </summary>
    private void LoadAetherytesFromJson()
    {
        try
        {
            // Use PluginInterface to get the plugin directory
            string pluginDirectory = Path.GetDirectoryName(Plugin.PluginInterface.AssemblyLocation.FullName);
            Plugin.Log.Information($"Plugin directory: {pluginDirectory}");

            // Define the file name to load
            const string fileName = "aetheryte.json";

            // Look for the file in the plugin directory
            string aetheryteJsonPath = Path.Combine(pluginDirectory, fileName);
            Plugin.Log.Information($"Loading aetherytes from: {aetheryteJsonPath}");

            // Check if the file exists
            if (!File.Exists(aetheryteJsonPath))
            {
                Plugin.Log.Error($"Aetheryte JSON file not found: {aetheryteJsonPath}");
                return;
            }

            // Read the JSON file
            string jsonContent = File.ReadAllText(aetheryteJsonPath);
            Plugin.Log.Debug($"Read JSON content with length: {jsonContent.Length}");

            // If the JSON content is empty, log an error and return
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Plugin.Log.Error("aetheryte.json file is empty");
                return;
            }

            // Try to deserialize JSON
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true
            };

            var aetheryteData = JsonSerializer.Deserialize<AetheryteData>(jsonContent, options);

            if (aetheryteData == null)
            {
                Plugin.Log.Error("Failed to deserialize aetheryte data from JSON: result was null");
                return;
            }

            if (aetheryteData.Aetherytes == null || aetheryteData.Aetherytes.Count == 0)
            {
                Plugin.Log.Error("Deserialized aetheryte data contains no aetherytes");
                return;
            }

            Plugin.Log.Information($"Successfully loaded {aetheryteData.Aetherytes.Count} aetherytes from JSON file");

            // Create aetheryte info objects from JSON data using the conversion method
            var loadedAetherytes = new List<AetheryteInfo>();
            foreach (var jsonAetheryte in aetheryteData.Aetherytes)
            {
                var aetheryteInfo = jsonAetheryte.ToAetheryteInfo();
                aetheryteInfo.ActualTeleportFee = 0; // Initialize teleport fee

                loadedAetherytes.Add(aetheryteInfo);
                Plugin.Log.Debug($"Loaded aetheryte: {aetheryteInfo.Name} in {aetheryteInfo.MapArea} at position ({aetheryteInfo.Position.X}, {aetheryteInfo.Position.Y})");
            }

            this.aetherytes = loadedAetherytes;
            Plugin.Log.Information($"Successfully loaded {this.aetherytes.Count} aetherytes from JSON file");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading aetherytes from JSON: {ex.Message}");
            Plugin.Log.Error(ex.StackTrace);
        }
    }



    /// <summary>
    /// Teleports the player to the specified aetheryte.
    /// </summary>
    /// <param name="aetheryte">The aetheryte to teleport to.</param>
    /// <returns>True if the teleport command was sent successfully, false otherwise.</returns>
    public bool TeleportToAetheryte(AetheryteInfo aetheryte)
    {
        if (aetheryte == null || aetheryte.AetheryteId == 0)
            return false;

        try
        {
            // Use Telepo API directly for reliable teleportation
            unsafe
            {
                var telepo = FFXIVClientStructs.FFXIV.Client.Game.UI.Telepo.Instance();
                if (telepo != null)
                {
                    telepo->Teleport(aetheryte.AetheryteId, 0);
                    Plugin.Log.Debug($"Teleported to {aetheryte.Name} (ID: {aetheryte.AetheryteId}) using Telepo API");
                    return true;
                }
                else
                {
                    Plugin.Log.Error("Telepo instance is null, cannot teleport");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error teleporting to aetheryte {aetheryte.Name} (ID: {aetheryte.AetheryteId}): {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disposes the service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        // Clear aetheryte list to free memory
        aetherytes.Clear();
    }
}
