using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateImportExportService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="aetheryteService">The aetheryte service.</param>
    /// <param name="mapAreaTranslationService">The map area translation service.</param>
    public CoordinateImportExportService(Plugin plugin, AetheryteService aetheryteService, MapAreaTranslationService mapAreaTranslationService)
    {
        this.plugin = plugin;
        this.aetheryteService = aetheryteService;
        this.mapAreaTranslationService = mapAreaTranslationService;
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

                // Parse the decoded text as JSON
                var coordinates = System.Text.Json.JsonSerializer.Deserialize<List<TreasureCoordinate>>(decodedText);

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
                            Plugin.Log,
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
                            coordinate.PlayerName = RemoveSpecialCharactersFromName(coordinate.PlayerName);
                        }

                        // Assign the nearest aetheryte to the coordinate for teleport functionality
                        AssignAetheryteToCoordinate(coordinate);

                        addCoordinateAction(coordinate);
                        importedCount++;
                    }

                    // Log a warning if more than 8 coordinates were provided
                    if (coordinates.Count > 8)
                    {
                        Plugin.Log.Warning($"Only imported the first 8 coordinates out of {coordinates.Count}. Additional coordinates were ignored.");
                    }

                    Plugin.Log.Debug($"Imported {importedCount} coordinates from Base64 encoded data");
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
        // Regular expression to match player names in copied chat messages like [21:50](Player Name) Text...
        var playerNameRegex = new Regex(@"\[\d+:\d+\]\s*\(([^)]+)\)|\(([^)]+)\)", RegexOptions.IgnoreCase);
        
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
                        Plugin.Log,
                        $"({x:F1}, {y:F1})");

                    if (!isValid)
                    {
                        Plugin.Log.Warning($"Skipping coordinate ({x:F1}, {y:F1}) - invalid map area");
                        continue;
                    }

                    // Create coordinate with original map area name for display
                    var coordinate = new TreasureCoordinate(x, y, originalMapArea, CoordinateSystemType.Map, "", playerName);

                    // Assign the nearest aetheryte to the coordinate for teleport functionality
                    AssignAetheryteToCoordinate(coordinate);

                    addCoordinateAction(coordinate);
                    importedCount++;
                }
            }
        }

        // Log a warning if more than 8 coordinates were found
        if (matchCount > 8)
        {
            Plugin.Log.Warning($"Only imported the first 8 coordinates out of {matchCount} found in text. Additional coordinates were ignored.");
        }

        // Log information about import requirements if no coordinates were imported
        if (importedCount == 0 && matchCount == 0)
        {
            Plugin.Log.Information("No valid coordinates found. Coordinates must include valid map area information in the format: 'MapName (x, y)'");
            LogValidMapAreas();
        }
        else if (importedCount == 0 && matchCount > 0)
        {
            Plugin.Log.Warning($"Found {matchCount} coordinate patterns but none were imported. Ensure coordinates include valid map area information in the format: 'MapName (x, y)'");
            LogValidMapAreas();
        }

        return importedCount;
    }

    /// <summary>
    /// Exports coordinates to a Base64 encoded string.
    /// </summary>
    /// <param name="coordinates">The coordinates to export.</param>
    /// <returns>A Base64 encoded string containing the coordinates.</returns>
    public string ExportCoordinates(List<TreasureCoordinate> coordinates)
    {
        try
        {
            // Serialize the coordinates to JSON
            var json = System.Text.Json.JsonSerializer.Serialize(coordinates);

            // Encode the JSON as Base64
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            return base64;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error exporting coordinates: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Assigns the nearest aetheryte to a coordinate for teleport functionality.
    /// This method is now disabled - AetheryteId should only be assigned during route optimization.
    /// </summary>
    /// <param name="coordinate">The coordinate to assign an aetheryte to.</param>
    private void AssignAetheryteToCoordinate(TreasureCoordinate coordinate)
    {
        // This method is now disabled to prevent automatic assignment of AetheryteId during import
        // AetheryteId should only be assigned during route optimization when teleportation is actually needed
        Plugin.Log.Debug($"Skipping automatic aetheryte assignment for coordinate ({coordinate.X:F1}, {coordinate.Y:F1}) in {coordinate.MapArea} - will be assigned during route optimization if needed");
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
            var validMapAreas = aetheryteService.GetValidMapAreas().Take(10).ToList();
            if (validMapAreas.Count > 0)
            {
                Plugin.Log.Information($"Valid map areas include: {string.Join(", ", validMapAreas)}");
                if (aetheryteService.GetValidMapAreas().Count > 10)
                {
                    Plugin.Log.Information($"... and {aetheryteService.GetValidMapAreas().Count - 10} more areas available.");
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
        // Try to split the text at timestamps like [21:50]
        var timestampSegments = Regex.Split(text, @"(?=\[\d+:\d+\])");

        // If no timestamps found or only one segment, return the whole text
        if (timestampSegments.Length <= 1)
        {
            return new[] { text };
        }

        // Filter out empty segments
        return timestampSegments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
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
            // Group 1 contains the player name if it matched the first pattern ([21:50](Player Name))
            // Group 2 contains the player name if it matched the second pattern ((Player Name))
            string playerName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return RemoveSpecialCharactersFromName(playerName.Trim());
        }
        return string.Empty;
    }

    /// <summary>
    /// Removes special characters from player names like BoxedNumber and BoxedOutlinedNumber
    /// </summary>
    /// <param name="name">The name that might contain special characters</param>
    /// <returns>The name with special characters removed</returns>
    private string RemoveSpecialCharactersFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Create a StringBuilder to build the cleaned name
        var cleanedName = new StringBuilder(name.Length);

        // Process each character in the name
        foreach (var c in name)
        {
            // Check for BoxedNumber character range (0xE090 to 0xE097)
            // These are game-specific number icons
            if ((int)c >= 0xE090 && (int)c <= 0xE097)
                continue;

            // Check for BoxedOutlinedNumber character range (0xE0E1 to 0xE0E9)
            // These are game-specific outlined number icons
            if ((int)c >= 0xE0E1 && (int)c <= 0xE0E9)
                continue;

            // Remove star character (★) often used in player names
            if (c == '★')
                continue;

            // Any other special characters that need to be filtered can be added here

            // Add the character to the cleaned name if it passed all filters
            cleanedName.Append(c);
        }

        return cleanedName.ToString().Trim();
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
