using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
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
        
        // Try to load aetheryte positions from JSON file
        LoadAetherytePositionsFromJson();
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

            // Load positions from the JSON file instead of setting default positions
            LoadAetherytePositionsFromJson();
        }
        catch (Exception ex)
        {
            log.Error($"Error loading aetherytes: {ex.Message}");
        }
    }

    /// <summary>
    /// This method is kept for API compatibility but no longer sets default positions.
    /// Positions are now exclusively loaded from the JSON file.
    /// </summary>
    private void UpdateAetherytePositions()
    {
        // This method is intentionally empty as we no longer set default positions
        // All aetheryte positions should be loaded from the JSON file
        log.Information("UpdateAetherytePositions is disabled - using JSON data for positions");
    }
    
    /// <summary>
    /// Loads aetheryte positions from the aetheryte.json file.
    /// </summary>
    private void LoadAetherytePositionsFromJson()
    {
        try
        {
            // 使用 PluginInterface 获取插件目录
            string pluginDirectory = Path.GetDirectoryName(Plugin.PluginInterface.AssemblyLocation.FullName);
            log.Information($"Plugin directory from PluginInterface: {pluginDirectory}");
            
            // 定义要加载的文件名
            const string fileName = "aetheryte.json";
            
            // 在插件目录下查找文件
            string aetheryteJsonPath = Path.Combine(pluginDirectory, fileName);
            log.Information($"Looking for aetheryte.json in plugin directory: {aetheryteJsonPath}");
            
            // 检查文件是否存在
            if (!File.Exists(aetheryteJsonPath))
            {
                log.Error($"Aetheryte JSON file not found in plugin directory: {aetheryteJsonPath}");
                return;
            }
            
            log.Information($"Found aetheryte.json at: {aetheryteJsonPath}");
            
            // 读取JSON文件
            string jsonContent = File.ReadAllText(aetheryteJsonPath);
            log.Debug($"Read JSON content with length: {jsonContent.Length}");
            
            // 如果JSON内容为空，记录错误并返回
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                log.Error("aetheryte.json file is empty");
                return;
            }
            
            // 尝试反序列化JSON
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
            
            // 更新现有的传送点数据或添加新的传送点
            int updatedCount = 0;
            foreach (var jsonAetheryte in aetheryteData.Aetherytes)
            {
                var existingAetheryte = aetherytes.FirstOrDefault(a => a.AetheryteRowId == jsonAetheryte.AetheryteRowId);
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
                    // 创建新的传送点如果之前的列表中不存在
                    var newAetheryte = new AetheryteInfo
                    {
                        Id = jsonAetheryte.AetheryteRowId,
                        AetheryteRowId = jsonAetheryte.AetheryteRowId,
                        Name = jsonAetheryte.Name,
                        MapArea = jsonAetheryte.MapArea,
                        Position = new Vector2((float)jsonAetheryte.X, (float)jsonAetheryte.Y),
                        BaseTeleportFee = jsonAetheryte.BaseTeleportFee,
                        ActualTeleportFee = 0, // 在需要时更新
                        TerritoryId = 0, // JSON中没有这个信息
                        MapId = 0 // JSON中没有这个信息
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
    /// 记录指定目录及其子目录的内容，用于调试
    /// </summary>
    /// <param name="directory">要检查的目录</param>
    private void LogAllDirectoryContents(string directory)
    {
        try
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                log.Error($"Cannot log directory contents: Directory does not exist or is null: {directory}");
                return;
            }
            
            log.Information($"Logging contents of directory: {directory}");
            
            // 记录当前目录中的所有文件
            var files = Directory.GetFiles(directory);
            log.Information($"Found {files.Length} files in {directory}:");
            foreach (var file in files)
            {
                log.Information($"  - {Path.GetFileName(file)}");
            }
            
            // 记录子目录
            var subDirs = Directory.GetDirectories(directory);
            log.Information($"Found {subDirs.Length} subdirectories in {directory}:");
            foreach (var subDir in subDirs)
            {
                log.Information($"  - {Path.GetFileName(subDir)}");
                
                // 递归检查子目录中的文件（仅一级，避免日志过多）
                try
                {
                    var subFiles = Directory.GetFiles(subDir);
                    log.Information($"    Contains {subFiles.Length} files");
                    // 只记录前5个文件，避免日志过多
                    foreach (var file in subFiles.Take(5))
                    {
                        log.Information($"    - {Path.GetFileName(file)}");
                    }
                    if (subFiles.Length > 5)
                    {
                        log.Information($"    ... and {subFiles.Length - 5} more files");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error listing files in subdirectory {subDir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error logging directory contents: {ex.Message}");
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
    /// Gets or sets the aetheryte row ID.
    /// </summary>
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
