using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Dalamud.Game;
using ECommons.DalamudServices;

using OnePiece.Helpers;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for importing and exporting treasure coordinates.
/// </summary>
public class CoordinateImportExportService
{
    private readonly Plugin plugin;
    private readonly AetheryteService aetheryteService;
    private readonly MapAreaTranslationService mapAreaTranslationService;
    private readonly PlayerNameProcessingService playerNameProcessingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateImportExportService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="aetheryteService">The aetheryte service.</param>
    /// <param name="mapAreaTranslationService">The map area translation service.</param>
    /// <param name="playerNameProcessingService">The player name processing service.</param>
    public CoordinateImportExportService(Plugin plugin, AetheryteService aetheryteService, MapAreaTranslationService mapAreaTranslationService, PlayerNameProcessingService playerNameProcessingService)
    {
        this.plugin = plugin;
        this.aetheryteService = aetheryteService;
        this.mapAreaTranslationService = mapAreaTranslationService;
        this.playerNameProcessingService = playerNameProcessingService;
    }

    /// <summary>
    /// Imports coordinates from text.
    /// </summary>
    /// <param name="text">The text containing coordinates.</param>
    /// <param name="addCoordinateAction">Action to add each imported coordinate.</param>
    /// <returns>The number of coordinates imported.</returns>
    public int ImportCoordinates(string text, Action<TreasureCoordinate> addCoordinateAction)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var importedCount = 0;

        // Check if the text is a Base64 encoded string
        if (IsBase64String(text))
        {
            try
            {
                // Decode the Base64 string
                var decodedBytes = Convert.FromBase64String(text);
                var decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);

                // Parse the decoded text as JSON - try optimized format first, then fall back to full format
                List<TreasureCoordinate>? coordinates = null;

                try
                {
                    // Try to deserialize as optimized format (Dictionary-based)
                    var optimizedData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(decodedText);
                    if (optimizedData != null)
                    {
                        coordinates = optimizedData.Select(CreateCoordinateFromOptimizedData).ToList();
                        Plugin.Log.Debug("Successfully imported optimized format coordinates");
                    }
                }
                catch
                {
                    // Fall back to full format
                    try
                    {
                        coordinates = System.Text.Json.JsonSerializer.Deserialize<List<TreasureCoordinate>>(decodedText);
                        Plugin.Log.Debug("Successfully imported full format coordinates");
                    }
                    catch (Exception fallbackEx)
                    {
                        Plugin.Log.Error($"Failed to deserialize both optimized and full format: {fallbackEx.Message}");
                    }
                }

                if (coordinates != null)
                {
                    // Limit to maximum of 8 coordinates for Number/BoxedNumber components
                    foreach (var coordinate in coordinates.Take(8))
                    {
                        // Validate that coordinate has a map area - skip if missing
                        if (string.IsNullOrWhiteSpace(coordinate.MapArea))
                        {
                            Plugin.Log.Warning($"Skipping coordinate ({coordinate.X:F1}, {coordinate.Y:F1}) - missing map area information");
                            continue;
                        }

                        // Translate and validate map area name, but keep original for display
                        var (isValid, englishMapArea, originalMapArea) = MapAreaHelper.TranslateAndValidateMapArea(
                            coordinate.MapArea,
                            mapAreaTranslationService,
                            aetheryteService,
                            $"({coordinate.X:F1}, {coordinate.Y:F1})");

                        if (!isValid)
                        {
                            Plugin.Log.Warning($"Skipping coordinate ({coordinate.X:F1}, {coordinate.Y:F1}) - invalid map area");
                            continue;
                        }

                        // Keep the original map area name for display purposes
                        // coordinate.MapArea remains unchanged

                        // Clean player name from special characters
                        if (!string.IsNullOrEmpty(coordinate.PlayerName))
                        {
                            coordinate.PlayerName = playerNameProcessingService.ProcessPlayerName(coordinate.PlayerName);
                        }

                        addCoordinateAction(coordinate);
                        importedCount++;
                    }

                    // Log a warning if more than 8 coordinates were provided
                    if (coordinates.Count > 8)
                    {
                        Plugin.Log.Warning($"Only imported the first 8 coordinates out of {coordinates.Count}. Additional coordinates were ignored.");
                    }

                    Plugin.Log.Debug($"Imported {importedCount} coordinates from Base64 encoded data");
                    if (importedCount > 0)
                    {
                        Plugin.Log.Debug("Aetheryte assignments will be optimized during route planning for best teleport efficiency");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error importing coordinates from Base64: {ex.Message}");

                // If Base64 import fails, try regular text import
                importedCount = ImportCoordinatesFromText(text, addCoordinateAction);
            }
        }
        else
        {
            // Regular text import
            importedCount = ImportCoordinatesFromText(text, addCoordinateAction);
        }

        return importedCount;
    }

    /// <summary>
    /// Imports coordinates from plain text.
    /// </summary>
    /// <param name="text">The text to import coordinates from.</param>
    /// <param name="addCoordinateAction">Action to add each imported coordinate.</param>
    /// <returns>The number of coordinates imported.</returns>
    private int ImportCoordinatesFromText(string text, Action<TreasureCoordinate> addCoordinateAction)
    {
        // Regular expression to match player names in various chat formats:
        // [14:27][CWLS1]<Tataru T.> - with CrossWorldLinkShell channel
        // [14:27][1]<Tataru T.> - with LinkShell channel
        // [16:30](Tataru Taru) - with Party channel
        var playerNameRegex = new Regex(@"\[(?<time>\d{1,2}:\d{2})\](?:\[(?<channel>[^\]]+)\])?(?:<(?<player1>[^>]+)>|\((?<player2>[^)]+)\))", RegexOptions.IgnoreCase);
        
        // Get language-specific coordinate regex based on current game client language
        var coordinateRegex = GetCoordinateRegexForCurrentLanguage();

        var importedCount = 0;
        var matchCount = 0;

        Plugin.Log.Debug($"Processing coordinate import: {text.Length} chars, language: {Svc.ClientState.ClientLanguage}");

        // Split text into segments if it contains multiple entries
        string[] segments = SplitTextIntoSegments(text);
        Plugin.Log.Debug($"Split text into {segments.Length} segments");

        // Process each segment
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            Plugin.Log.Debug($"Processing segment {i + 1}/{segments.Length}");

            // Try to extract player name from segment
            string playerName = ExtractPlayerNameFromSegment(segment, playerNameRegex);
            if (!string.IsNullOrEmpty(playerName))
            {
                Plugin.Log.Debug($"Extracted player name: '{playerName}'");
            }

            var matches = coordinateRegex.Matches(segment);
            Plugin.Log.Debug($"Found {matches.Count} coordinate matches in segment");

            // If no matches, log for debugging
            if (matches.Count == 0)
            {
                Plugin.Log.Debug($"No coordinate matches found in segment");
            }
            foreach (Match match in matches)
            {
                // Count all matches, even if we don't import them
                matchCount++;

                Plugin.Log.Debug($"Match {matchCount}: {match.Groups[1].Value} ({match.Groups[2].Value}, {match.Groups[3].Value})");

                // Only process the first 8 matches that have valid coordinates and map area
                if (importedCount < 8 &&
                    match.Groups.Count >= 4 &&
                    float.TryParse(match.Groups[2].Value, out var x) &&
                    float.TryParse(match.Groups[3].Value, out var y))
                {
                    // Extract map area (guaranteed to be present due to regex requirement)
                    string mapArea = match.Groups[1].Value.Trim();

                    // Remove player name from map area if it was incorrectly captured
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        mapArea = RemovePlayerNameFromMapArea(mapArea, playerName);
                    }

                    // Final validation - ensure map area is not empty after cleaning
                    if (string.IsNullOrWhiteSpace(mapArea))
                    {
                        Plugin.Log.Warning($"Skipping coordinate ({x:F1}, {y:F1}) - map area became empty after processing");
                        continue;
                    }

                    // Translate and validate map area name, but keep original for display
                    var (isValid, englishMapArea, originalMapArea) = MapAreaHelper.TranslateAndValidateMapArea(
                        mapArea,
                        mapAreaTranslationService,
                        aetheryteService,
                        $"({x:F1}, {y:F1})");

                    if (!isValid)
                    {
                        Plugin.Log.Warning($"Skipping coordinate ({x:F1}, {y:F1}) - invalid map area");
                        continue;
                    }

                    // Create coordinate with original map area name for display
                    var coordinate = new TreasureCoordinate(x, y, originalMapArea, CoordinateSystemType.Map, playerName);

                    addCoordinateAction(coordinate);
                    importedCount++;
                }
            }
        }

        // Log import results
        if (matchCount > 8)
        {
            Plugin.Log.Warning($"Only imported the first 8 coordinates out of {matchCount} found in text. Additional coordinates were ignored.");
        }

        if (importedCount == 0)
        {
            if (matchCount == 0)
            {
                Plugin.Log.Information("No valid coordinates found. Coordinates must include valid map area information in the format: 'MapName (x, y)'");
            }
            else
            {
                Plugin.Log.Warning($"Found {matchCount} coordinate patterns but none were imported. Ensure coordinates include valid map area information in the format: 'MapName (x, y)'");
            }
            LogValidMapAreas();
        }
        else
        {
            Plugin.Log.Debug("Aetheryte assignments will be optimized during route planning for best teleport efficiency");
        }

        return importedCount;
    }

    /// <summary>
    /// Exports coordinates to a Base64 encoded string with optimized data size.
    /// Only includes non-default values to reduce export size.
    /// </summary>
    /// <param name="coordinates">The coordinates to export.</param>
    /// <returns>A Base64 encoded string containing the coordinates.</returns>
    public string ExportCoordinates(List<TreasureCoordinate> coordinates)
    {
        try
        {
            // Convert to optimized export format to reduce data size
            var optimizedCoordinates = coordinates.Select(coord => CreateOptimizedExportData(coord)).ToList();

            // Configure JSON serializer to ignore null values and default values
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                WriteIndented = false // Compact format for smaller size
            };

            // Serialize the optimized coordinates to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(optimizedCoordinates, options);

            // Encode the JSON as Base64
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            Plugin.Log.Debug($"Exported {coordinates.Count} coordinates. Original size estimate: {EstimateFullSize(coordinates)} chars, Optimized size: {json.Length} chars");

            return base64;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error exporting coordinates: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Creates an optimized export data object that only includes non-default values.
    /// </summary>
    /// <param name="coordinate">The coordinate to optimize.</param>
    /// <returns>An object containing only the necessary data for export.</returns>
    private object CreateOptimizedExportData(TreasureCoordinate coordinate)
    {
        var exportData = new Dictionary<string, object>();

        // Always include essential coordinate data
        exportData["X"] = coordinate.X;
        exportData["Y"] = coordinate.Y;

        // Include MapArea if not empty (this is usually required)
        if (!string.IsNullOrEmpty(coordinate.MapArea))
        {
            exportData["MapArea"] = coordinate.MapArea;
        }

        // Include PlayerName if not empty
        if (!string.IsNullOrEmpty(coordinate.PlayerName))
        {
            exportData["PlayerName"] = coordinate.PlayerName;
        }

        // Only include non-default values for optional fields
        if (coordinate.CoordinateSystem != CoordinateSystemType.Map)
        {
            exportData["CoordinateSystem"] = (int)coordinate.CoordinateSystem;
        }

        if (coordinate.IsCollected)
        {
            exportData["IsCollected"] = coordinate.IsCollected;
        }

        if (coordinate.Type != CoordinateType.TreasurePoint)
        {
            exportData["Type"] = (int)coordinate.Type;
        }

        if (coordinate.IsTeleportPoint)
        {
            exportData["IsTeleportPoint"] = coordinate.IsTeleportPoint;
        }

        if (coordinate.AetheryteId > 0)
        {
            exportData["AetheryteId"] = coordinate.AetheryteId;
        }

        return exportData;
    }

    /// <summary>
    /// Estimates the size of the full coordinate data for comparison.
    /// </summary>
    /// <param name="coordinates">The coordinates to estimate size for.</param>
    /// <returns>Estimated character count of full serialization.</returns>
    private int EstimateFullSize(List<TreasureCoordinate> coordinates)
    {
        try
        {
            var fullJson = System.Text.Json.JsonSerializer.Serialize(coordinates);
            return fullJson.Length;
        }
        catch
        {
            return 0; // Return 0 if estimation fails
        }
    }

    /// <summary>
    /// Creates a TreasureCoordinate from optimized export data.
    /// </summary>
    /// <param name="data">The optimized data dictionary.</param>
    /// <returns>A TreasureCoordinate with default values for missing fields.</returns>
    private TreasureCoordinate CreateCoordinateFromOptimizedData(Dictionary<string, JsonElement> data)
    {
        var coordinate = new TreasureCoordinate();

        // Required fields
        if (data.TryGetValue("X", out var xElement) && xElement.ValueKind == JsonValueKind.Number)
        {
            coordinate.X = xElement.GetSingle();
        }

        if (data.TryGetValue("Y", out var yElement) && yElement.ValueKind == JsonValueKind.Number)
        {
            coordinate.Y = yElement.GetSingle();
        }

        // Optional fields with defaults
        if (data.TryGetValue("MapArea", out var mapAreaElement) && mapAreaElement.ValueKind == JsonValueKind.String)
        {
            coordinate.MapArea = mapAreaElement.GetString() ?? string.Empty;
        }

        if (data.TryGetValue("PlayerName", out var playerNameElement) && playerNameElement.ValueKind == JsonValueKind.String)
        {
            coordinate.PlayerName = playerNameElement.GetString() ?? string.Empty;
        }

        if (data.TryGetValue("CoordinateSystem", out var coordSystemElement) && coordSystemElement.ValueKind == JsonValueKind.Number)
        {
            coordinate.CoordinateSystem = (CoordinateSystemType)coordSystemElement.GetInt32();
        }

        if (data.TryGetValue("IsCollected", out var collectedElement) && collectedElement.ValueKind == JsonValueKind.True)
        {
            coordinate.IsCollected = true;
        }

        if (data.TryGetValue("Type", out var typeElement) && typeElement.ValueKind == JsonValueKind.Number)
        {
            coordinate.Type = (CoordinateType)typeElement.GetInt32();
        }

        if (data.TryGetValue("IsTeleportPoint", out var teleportElement) && teleportElement.ValueKind == JsonValueKind.True)
        {
            coordinate.IsTeleportPoint = true;
        }

        if (data.TryGetValue("AetheryteId", out var aetheryteElement) && aetheryteElement.ValueKind == JsonValueKind.Number)
        {
            coordinate.AetheryteId = aetheryteElement.GetUInt32();
        }

        return coordinate;
    }



    /// <summary>
    /// Gets the appropriate coordinate regex based on the current game client language.
    /// </summary>
    /// <returns>A regex pattern optimized for the current language.</returns>
    private Regex GetCoordinateRegexForCurrentLanguage()
    {
        var currentLanguage = Svc.ClientState.ClientLanguage;
        Plugin.Log.Information($"Creating coordinate regex for language: {currentLanguage}");

        var regex = currentLanguage switch
        {
            ClientLanguage.Japanese => GetJapaneseCoordinateRegex(),
            ClientLanguage.German => GetGermanCoordinateRegex(),
            ClientLanguage.French => GetFrenchCoordinateRegex(),
            _ => GetEnglishCoordinateRegex() // Default to English for English and any other languages
        };

        Plugin.Log.Information($"Selected regex pattern: {regex}");
        return regex;
    }

    /// <summary>
    /// Gets regex pattern optimized for English map area names.
    /// </summary>
    private Regex GetEnglishCoordinateRegex()
    {
        // English pattern: supports ASCII letters, numbers, spaces, apostrophes, hyphens
        // Examples: "Heritage Found (15.0, 20.5)", "Ul'dah - Steps of Nald (8.2, 7.8)"
        return new Regex(@"([A-Za-z0-9\s''\-–—]+?)\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets regex pattern optimized for Japanese map area names.
    /// </summary>
    private Regex GetJapaneseCoordinateRegex()
    {
        // Japanese pattern: supports all Unicode letters and common punctuation used in Japanese
        // This includes Hiragana, Katakana, Kanji, ASCII letters, numbers, spaces, and Japanese punctuation
        // Examples: "ヘリテージファウンド (16.0, 21.3)", "リムサ・ロミンサ：下甲板層 (9.5, 11.2)"
        // Using \s+ to handle multiple spaces and \s* for flexible spacing around parentheses
        return new Regex(@"([\p{L}\p{N}\s''\-–—：・]+?)\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets regex pattern optimized for German map area names.
    /// </summary>
    private Regex GetGermanCoordinateRegex()
    {
        // German pattern: supports all Unicode letters, numbers, spaces, and common punctuation
        // This includes German umlauts and other special characters
        // Examples: "Östliche Noscea (21.0, 21.0)", "Mor Dhona (22.2, 7.9)"
        return new Regex(@"([\p{L}\p{N}\s''\-–—]+?)\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets regex pattern optimized for French map area names.
    /// </summary>
    private Regex GetFrenchCoordinateRegex()
    {
        // French pattern: supports all Unicode letters, numbers, spaces, and common punctuation
        // This includes French accented characters
        // Examples: "Noscea orientale (21.0, 21.0)", "Mor Dhona (22.2, 7.9)"
        return new Regex(@"([\p{L}\p{N}\s''\-–—]+?)\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Logs the first few valid map areas to help users understand the required format.
    /// </summary>
    private void LogValidMapAreas()
    {
        try
        {
            var allValidMapAreas = aetheryteService.GetValidMapAreas();
            var validMapAreas = allValidMapAreas.Take(10).ToList();
            if (validMapAreas.Count > 0)
            {
                Plugin.Log.Information($"Valid map areas include: {string.Join(", ", validMapAreas)}");
                if (allValidMapAreas.Count > 10)
                {
                    Plugin.Log.Information($"... and {allValidMapAreas.Count - 10} more areas available.");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error logging valid map areas: {ex.Message}");
        }
    }

    #region Text Parsing Methods (integrated from TextParsingService)

    /// <summary>
    /// Splits a text into segments that might contain individual coordinates.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>An array of text segments.</returns>
    private string[] SplitTextIntoSegments(string text)
    {
        // Try to split the text at various chat format patterns:
        // [14:27][CWLS1]<Tataru T.> - with CrossWorldLinkShell channel
        // [14:27][1]<Tataru T.> - with LinkShell channel
        // [16:30](Tataru Taru) - with Party channel
        var chatSegments = Regex.Split(text, @"(?=\[\d{1,2}:\d{2}\](?:\[[^\]]+\])?(?:<[^>]+>|\([^)]+\)))");

        // If no new format found, try legacy timestamp splitting
        if (chatSegments.Length <= 1)
        {
            chatSegments = Regex.Split(text, @"(?=\[\d+:\d+\])");
        }

        // If still no segments found, return the whole text
        if (chatSegments.Length <= 1)
        {
            return new[] { text };
        }

        // Filter out empty segments
        return chatSegments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }

    /// <summary>
    /// Extracts player name from a text segment.
    /// </summary>
    /// <param name="segment">The text segment.</param>
    /// <param name="playerNameRegex">The regex to use for player name extraction.</param>
    /// <returns>The extracted player name, or empty string if none found.</returns>
    private string ExtractPlayerNameFromSegment(string segment, Regex playerNameRegex)
    {
        var match = playerNameRegex.Match(segment);
        if (match.Success)
        {
            // Extract player name from either capture group (angle brackets or parentheses)
            string playerName = match.Groups["player1"].Success ? match.Groups["player1"].Value : match.Groups["player2"].Value;
            return playerNameProcessingService.ProcessPlayerName(playerName.Trim());
        }
        return string.Empty;
    }



    /// <summary>
    /// Removes player name from map area if it was incorrectly captured.
    /// </summary>
    /// <param name="mapArea">The map area string.</param>
    /// <param name="playerName">The player name to remove.</param>
    /// <returns>The cleaned map area string.</returns>
    private string RemovePlayerNameFromMapArea(string mapArea, string playerName)
    {
        // If the map area starts with the player name, remove it
        if (mapArea.StartsWith(playerName, StringComparison.OrdinalIgnoreCase))
        {
            mapArea = mapArea.Substring(playerName.Length).Trim();
        }

        return mapArea;
    }

    /// <summary>
    /// Checks if a string is a valid Base64 encoded string.
    /// </summary>
    /// <param name="s">The string to check.</param>
    /// <returns>True if the string is a valid Base64 encoded string, false otherwise.</returns>
    private bool IsBase64String(string s)
    {
        // Check if the string is a valid Base64 string
        if (string.IsNullOrWhiteSpace(s))
            return false;

        // Remove any whitespace
        s = s.Trim();

        // Check if the length is valid for Base64
        if (s.Length % 4 != 0)
            return false;

        // Check if the string contains only valid Base64 characters
        return s.All(c =>
            (c >= 'A' && c <= 'Z') ||
            (c >= 'a' && c <= 'z') ||
            (c >= '0' && c <= '9') ||
            c == '+' || c == '/' || c == '=');
    }

    #endregion
}
