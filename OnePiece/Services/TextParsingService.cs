using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnePiece.Services;

/// <summary>
/// Service for parsing and processing text input.
/// </summary>
public class TextParsingService
{
    private readonly Plugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextParsingService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public TextParsingService(Plugin plugin)
    {
        this.plugin = plugin;
    }

    /// <summary>
    /// Splits a text into segments that might contain individual coordinates.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>An array of text segments.</returns>
    public string[] SplitTextIntoSegments(string text)
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
    public string ExtractPlayerNameFromSegment(string segment, Regex playerNameRegex)
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
    public string RemoveSpecialCharactersFromName(string name)
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
    public string RemovePlayerNameFromMapArea(string mapArea, string playerName)
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
    public bool IsBase64String(string s)
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
}
