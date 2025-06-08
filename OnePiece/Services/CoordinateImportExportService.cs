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
    private readonly AetheryteService aetheryteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateImportExportService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="textParsingService">The text parsing service.</param>
    /// <param name="aetheryteService">The aetheryte service.</param>
    public CoordinateImportExportService(Plugin plugin, TextParsingService textParsingService, AetheryteService aetheryteService)
    {
        this.plugin = plugin;
        this.textParsingService = textParsingService;
        this.aetheryteService = aetheryteService;
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

                        // Validate that the map area is valid - skip if invalid
                        if (!aetheryteService.IsValidMapArea(coordinate.MapArea))
                        {
                            Plugin.Log.Warning($"Skipping coordinate ({coordinate.X:F1}, {coordinate.Y:F1}) - invalid map area '{coordinate.MapArea}'");
                            continue;
                        }

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
        
        // Regular expression to match coordinates with map area in the format "MapName (x, y)"
        // Map area is now required - coordinates without map area will not match
        // Group 1: Map area (required)
        // Group 2: X coordinate
        // Group 3: Y coordinate
        var coordinateRegex = new Regex(@"([A-Za-z0-9\s']+)\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);

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
                        mapArea = textParsingService.RemovePlayerNameFromMapArea(mapArea, playerName);
                    }

                    // Final validation - ensure map area is not empty after cleaning
                    if (string.IsNullOrWhiteSpace(mapArea))
                    {
                        Plugin.Log.Warning($"Skipping coordinate ({x:F1}, {y:F1}) - map area became empty after processing");
                        continue;
                    }

                    // Validate that the map area is valid - skip if invalid
                    if (!aetheryteService.IsValidMapArea(mapArea))
                    {
                        Plugin.Log.Warning($"Skipping coordinate ({x:F1}, {y:F1}) - invalid map area '{mapArea}'");
                        continue;
                    }

                    // Create coordinate with player name if available
                    var coordinate = new TreasureCoordinate(x, y, mapArea, CoordinateSystemType.Map, "", playerName);

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
