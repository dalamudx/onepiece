using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using OnePiece.Models;
using OnePiece.Localization;
using OnePiece.Helpers;

namespace OnePiece.Services;

/// <summary>
/// Service for managing aetheryte (teleport crystal) information.
/// </summary>
public class AetheryteService : IDisposable
{
    private readonly IDataManager data;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private readonly TerritoryManager territoryManager;
    private readonly IChatGui chatGui;
    private readonly ICommandManager commandManager;
    private List<AetheryteInfo> aetherytes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AetheryteService"/> class.
    /// </summary>
    /// <param name="data">The data manager.</param>
    /// <param name="clientState">The client state.</param>
    /// <param name="log">The plugin log.</param>
    /// <param name="territoryManager">The territory manager.</param>
    /// <param name="chatGui">The chat GUI service.</param>
    /// <param name="commandManager">The command manager.</param>
    public AetheryteService(IDataManager data, IClientState clientState, IPluginLog log, TerritoryManager territoryManager, IChatGui chatGui, ICommandManager commandManager)
    {
        this.data = data;
        this.clientState = clientState;
        this.log = log;
        this.territoryManager = territoryManager;
        this.chatGui = chatGui;
        this.commandManager = commandManager;
        
        // LoadAetherytes also calls LoadAetherytePositionsFromJson internally
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

            // Get current player territory ID (must be called from main thread)
            uint playerTerritory = 0;
            try
            {
                // Check if we're on the main thread before accessing game state
                if (ThreadSafetyHelper.IsMainThread() && clientState.LocalPlayer != null)
                {
                    playerTerritory = clientState.TerritoryType;
                }
                else
                {
                    log.Debug($"Skipping territory check - not on main thread ({ThreadSafetyHelper.GetThreadInfo()}) or player not available");
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
                        if (info.AetheryteId == aetheryte.AetheryteId)
                        {
                            aetheryte.ActualTeleportFee = (int)info.GilCost;
                            foundCost = true;
                            log.Debug($"Found teleport fee for {aetheryte.Name} from Telepo API: {aetheryte.ActualTeleportFee} gil");
                            break;
                        }
                    }

                    // If we couldn't find the cost, leave ActualTeleportFee as 0
                    if (!foundCost)
                    {
                        log.Debug($"Could not find teleport fee for {aetheryte.Name} in Telepo API");
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
                        AetheryteId = aetheryte.RowId, // Store the ID for later use with Telepo API
                        Name = aetheryte.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty,
                        TerritoryId = territory.TerritoryId,
                        MapId = territory.MapId,
                        MapArea = territory.Name,
                        // Create a default position since we can't access X and Z directly in the current API
                        Position = new Vector2(0, 0),
                        BaseTeleportFee = 0, // Will be set from JSON data
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

            // Load positions from the JSON file instead of setting default positions
            LoadAetherytePositionsFromJson();
        }
        catch (Exception ex)
        {
            log.Error($"Error loading aetherytes: {ex.Message}");
        }
    }

    
    /// <summary>
    /// Loads aetheryte positions from the aetheryte.json file.
    /// </summary>
    private void LoadAetherytePositionsFromJson()
    {
        try
        {
            // Use PluginInterface to get the plugin directory
            string pluginDirectory = Path.GetDirectoryName(Plugin.PluginInterface.AssemblyLocation.FullName);
            log.Information($"Plugin directory from PluginInterface: {pluginDirectory}");
            
            // Define the file name to load
            const string fileName = "aetheryte.json";
            
            // Look for the file in the plugin directory
            string aetheryteJsonPath = Path.Combine(pluginDirectory, fileName);
            log.Information($"Looking for aetheryte.json in plugin directory: {aetheryteJsonPath}");
            
            // Check if the file exists
            if (!File.Exists(aetheryteJsonPath))
            {
                log.Error($"Aetheryte JSON file not found in plugin directory: {aetheryteJsonPath}");
                return;
            }
            
            log.Information($"Found aetheryte.json at: {aetheryteJsonPath}");
            
            // Read the JSON file
            string jsonContent = File.ReadAllText(aetheryteJsonPath);
            log.Debug($"Read JSON content with length: {jsonContent.Length}");
            
            // If the JSON content is empty, log an error and return
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                log.Error("aetheryte.json file is empty");
                return;
            }
            
            // Try to deserialize JSON
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true
            };
            
            var aetheryteData = JsonSerializer.Deserialize<AetheryteJsonData>(jsonContent, options);
            
            if (aetheryteData == null)
            {
                log.Error("Failed to deserialize aetheryte data from JSON: result was null");
                return;
            }
            
            if (aetheryteData.Aetherytes == null || aetheryteData.Aetherytes.Count == 0)
            {
                log.Error("Deserialized aetheryte data contains no aetherytes");
                return;
            }
            
            log.Information($"Successfully loaded {aetheryteData.Aetherytes.Count} aetherytes from JSON file");
            
            // Update existing aetheryte data or add new aetherytes
            int updatedCount = 0;
            foreach (var jsonAetheryte in aetheryteData.Aetherytes)
            {
                var existingAetheryte = aetherytes.FirstOrDefault(a => a.AetheryteId == jsonAetheryte.AetheryteRowId);
                if (existingAetheryte != null)
                {
                    existingAetheryte.Position = new Vector2((float)jsonAetheryte.X, (float)jsonAetheryte.Y);
                    existingAetheryte.MapArea = jsonAetheryte.MapArea;
                    existingAetheryte.BaseTeleportFee = jsonAetheryte.BaseTeleportFee;
                    updatedCount++;
                    log.Debug($"Updated existing aetheryte: {existingAetheryte.Name} in {existingAetheryte.MapArea} at position ({existingAetheryte.Position.X}, {existingAetheryte.Position.Y})");
                }
                else
                {
                    // Create a new aetheryte if it doesn't exist in the previous list
                    var newAetheryte = new AetheryteInfo
                    {
                        Id = jsonAetheryte.AetheryteRowId,
                        AetheryteId = jsonAetheryte.AetheryteRowId,
                        Name = jsonAetheryte.Name,
                        MapArea = jsonAetheryte.MapArea,
                        Position = new Vector2((float)jsonAetheryte.X, (float)jsonAetheryte.Y),
                        BaseTeleportFee = jsonAetheryte.BaseTeleportFee,
                        ActualTeleportFee = 0, // Update when needed
                        TerritoryId = 0, // This information is not in the JSON
                        MapId = 0 // This information is not in the JSON
                    };

                    aetherytes.Add(newAetheryte);
                    updatedCount++;
                    log.Debug($"Added new aetheryte: {newAetheryte.Name} in {newAetheryte.MapArea} at position ({newAetheryte.Position.X}, {newAetheryte.Position.Y})");
                }
            }
            
            log.Information($"Successfully updated or added {updatedCount} aetherytes from JSON file");
        }
        catch (Exception ex)
        {
            log.Error($"Error loading aetheryte positions from JSON: {ex.Message}");
            log.Error(ex.StackTrace);
        }
    }



    /// <summary>
    /// Calculates the teleport price for a given aetheryte.
    /// </summary>
    /// <param name="aetheryte">The aetheryte to calculate the teleport price for.</param>
    /// <returns>The teleport price in gil, or 0 if no price data is available.</returns>
    public int CalculateTeleportPrice(AetheryteInfo aetheryte)
    {
        if (aetheryte == null)
            return 0;

        // Only use actual teleport fee from game API
        return aetheryte.ActualTeleportFee;
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
            // Format the command for teleporting using AetheryteId
            // We use /tport which accepts IDs rather than names
            string teleportCommand = $"/tport {aetheryte.AetheryteId}";

            // Send the command through the chat system
            commandManager.ProcessCommand(teleportCommand);

            // Fallback: If /tport command doesn't work, also try the Telepo API directly
            unsafe
            {
                try
                {
                    var telepo = FFXIVClientStructs.FFXIV.Client.Game.UI.Telepo.Instance();
                    if (telepo != null)
                    {
                        telepo->Teleport(aetheryte.AetheryteId, 0);
                        log.Debug($"Used Telepo API to teleport to {aetheryte.Name} (ID: {aetheryte.AetheryteId})");
                    }
                }
                catch (Exception innerEx)
                {
                    log.Error($"Error using Telepo API: {innerEx.Message}");
                    // Continue with command-based approach
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Error teleporting to aetheryte {aetheryte.Name} (ID: {aetheryte.AetheryteId}): {ex.Message}");
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

        // No other resources to dispose currently
        // This method is here for future extensibility
    }
}

/// <summary>
/// Class for deserializing aetheryte.json data.
/// </summary>
public class AetheryteJsonData
{
    /// <summary>
    /// Gets or sets the timestamp when the data was generated.
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the game version.
    /// </summary>
    public string GameVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of aetherytes.
    /// </summary>
    public List<AetheryteJsonEntry> Aetherytes { get; set; } = new();
}

/// <summary>
/// Class for deserializing individual aetheryte entries from JSON.
/// </summary>
public class AetheryteJsonEntry
{
    /// <summary>
    /// Gets or sets the aetheryte ID.
    /// </summary>
    [JsonPropertyName("AetheryteRowId")] // Keep JSON compatibility
    public uint AetheryteRowId { get; set; }
    
    /// <summary>
    /// Gets or sets the aetheryte name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the map area name.
    /// </summary>
    public string MapArea { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the base teleport fee.
    /// </summary>
    public int BaseTeleportFee { get; set; }
    
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public double Y { get; set; }
}
