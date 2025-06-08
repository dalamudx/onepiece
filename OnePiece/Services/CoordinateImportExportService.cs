using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly TextParsingService textParsingService;
    private readonly AetheryteService aetheryteService;
    private readonly MapAreaTranslationService mapAreaTranslationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateImportExportService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="textParsingService">The text parsing service.</param>
    /// <param name="aetheryteService">The aetheryte service.</param>
    /// <param name="mapAreaTranslationService">The map area translation service.</param>
    public CoordinateImportExportService(Plugin plugin, TextParsingService textParsingService, AetheryteService aetheryteService, MapAreaTranslationService mapAreaTranslationService)
    {
        this.plugin = plugin;
        this.textParsingService = textParsingService;
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
        if (textParsingService.IsBase64String(text))
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
                            coordinate.PlayerName = textParsingService.RemoveSpecialCharactersFromName(coordinate.PlayerName);
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

        Plugin.Log.Information($"Processing coordinate import with text: '{text}'");
        Plugin.Log.Information($"Text length: {text.Length}, bytes: {System.Text.Encoding.UTF8.GetByteCount(text)}");
        Plugin.Log.Information($"Current game language: {Svc.ClientState.ClientLanguage}");
        Plugin.Log.Information($"Using regex pattern: {coordinateRegex}");

        // Test the regex directly on the input text first
        var directMatches = coordinateRegex.Matches(text);
        Plugin.Log.Information($"Direct regex test on full text found {directMatches.Count} matches");

        // Split text into segments if it contains multiple entries
        string[] segments = textParsingService.SplitTextIntoSegments(text);
        Plugin.Log.Information($"Split text into {segments.Length} segments");

        // Process each segment
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            Plugin.Log.Information($"Processing segment {i + 1}/{segments.Length}: '{segment}'");
            Plugin.Log.Information($"Segment length: {segment.Length}, bytes: {System.Text.Encoding.UTF8.GetByteCount(segment)}");

            // Try to extract player name from segment
            string playerName = textParsingService.ExtractPlayerNameFromSegment(segment, playerNameRegex);
            if (!string.IsNullOrEmpty(playerName))
            {
                Plugin.Log.Information($"Extracted player name: '{playerName}'");
            }

            var matches = coordinateRegex.Matches(segment);
            Plugin.Log.Information($"Found {matches.Count} coordinate matches in segment");

            // If no matches, let's try some diagnostic tests
            if (matches.Count == 0)
            {
                Plugin.Log.Warning($"No matches found for segment: '{segment}'");

                // Test if the segment contains Japanese characters
                bool hasJapanese = segment.Any(c => c >= 0x3040 && c <= 0x309F || c >= 0x30A0 && c <= 0x30FF || c >= 0x4E00 && c <= 0x9FAF);
                Plugin.Log.Information($"Segment contains Japanese characters: {hasJapanese}");

                // Test if the segment contains coordinate pattern
                bool hasCoordinatePattern = System.Text.RegularExpressions.Regex.IsMatch(segment, @"\(\s*\d+(?:\.\d+)?\s*,\s*\d+(?:\.\d+)?\s*\)");
                Plugin.Log.Information($"Segment contains coordinate pattern: {hasCoordinatePattern}");

                // Try a very simple regex to see what we can match
                var simpleRegex = new Regex(@"(.+?)\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)");
                var simpleMatches = simpleRegex.Matches(segment);
                Plugin.Log.Information($"Simple regex found {simpleMatches.Count} matches");
                if (simpleMatches.Count > 0)
                {
                    foreach (Match simpleMatch in simpleMatches)
                    {
                        Plugin.Log.Information($"Simple match - Area: '{simpleMatch.Groups[1].Value}', X: '{simpleMatch.Groups[2].Value}', Y: '{simpleMatch.Groups[3].Value}'");
                    }
                }
            }
            foreach (Match match in matches)
            {
                // Count all matches, even if we don't import them
                matchCount++;

                Plugin.Log.Information($"Match {matchCount}: Full match = '{match.Value}'");
                Plugin.Log.Information($"  Group 0 (full): '{match.Groups[0].Value}'");
                Plugin.Log.Information($"  Group 1 (map area): '{match.Groups[1].Value}'");
                Plugin.Log.Information($"  Group 2 (X coord): '{match.Groups[2].Value}'");
                Plugin.Log.Information($"  Group 3 (Y coord): '{match.Groups[3].Value}'");

                // Only process the first 8 matches that have valid coordinates and map area
                if (importedCount < 8 &&
                    match.Groups.Count >= 4 &&
                    float.TryParse(match.Groups[2].Value, out var x) &&
                    float.TryParse(match.Groups[3].Value, out var y))
                {
                    // Extract map area (guaranteed to be present due to regex requirement)
                    string mapArea = match.Groups[1].Value.Trim();
                    Plugin.Log.Information($"  Extracted map area: '{mapArea}', coordinates: ({x}, {y})");

                    // Remove player name from map area if it was incorrectly captured
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        mapArea = textParsingService.RemovePlayerNameFromMapArea(mapArea, playerName);
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
}
