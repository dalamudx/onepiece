using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for importing and exporting treasure coordinates.
/// </summary>
public class CoordinateImportExportService
{
    private readonly Plugin plugin;
    private readonly TextParsingService textParsingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateImportExportService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="textParsingService">The text parsing service.</param>
    public CoordinateImportExportService(Plugin plugin, TextParsingService textParsingService)
    {
        this.plugin = plugin;
        this.textParsingService = textParsingService;
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
                        // Clean player name from special characters
                        if (!string.IsNullOrEmpty(coordinate.PlayerName))
                        {
                            coordinate.PlayerName = textParsingService.RemoveSpecialCharactersFromName(coordinate.PlayerName);
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
        
        // Regular expression to match coordinates with map area in the format "MapName (x, y)" or just "(x, y)"
        // Group 1: Map area (optional)
        // Group 2: X coordinate
        // Group 3: Y coordinate
        var coordinateRegex = new Regex(@"(?:([A-Za-z0-9\s']+)?\s*\(?\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)?)", RegexOptions.IgnoreCase);

        var importedCount = 0;
        var matchCount = 0;
        
        // Split text into segments if it contains multiple entries
        string[] segments = textParsingService.SplitTextIntoSegments(text);
        
        // Process each segment
        foreach (string segment in segments)
        {
            // Try to extract player name from segment
            string playerName = textParsingService.ExtractPlayerNameFromSegment(segment, playerNameRegex);
            
            var matches = coordinateRegex.Matches(segment);
            foreach (Match match in matches)
            {
                // Count all matches, even if we don't import them
                matchCount++;

                // Only process the first 8 matches
                if (importedCount < 8 &&
                    match.Groups.Count >= 4 &&
                    float.TryParse(match.Groups[2].Value, out var x) &&
                    float.TryParse(match.Groups[3].Value, out var y))
                {
                    // Extract map area (if present)
                    string mapArea = match.Groups[1].Success ? match.Groups[1].Value.Trim() : string.Empty;
                    
                    // Remove player name from map area if it was incorrectly captured
                    if (!string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(mapArea))
                    {
                        mapArea = textParsingService.RemovePlayerNameFromMapArea(mapArea, playerName);
                    }

                    // Create coordinate with player name if available
                    var coordinate = new TreasureCoordinate(x, y, mapArea, CoordinateSystemType.Map, "", playerName);
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
}
