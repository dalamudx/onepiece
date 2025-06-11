using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using OnePiece.Helpers;

namespace OnePiece.Services;

/// <summary>
/// Service for processing player names, removing special characters, job abbreviations, and server names.
/// </summary>
public class PlayerNameProcessingService
{
    private HashSet<string>? cachedJobAbbreviations;
    private HashSet<string>? cachedServerNames;
    private DateTime lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(5); // Cache for 5 minutes

    /// <summary>
    /// Processes a player name by removing special characters, job abbreviations, and server names.
    /// </summary>
    /// <param name="name">The raw player name that might contain special characters, job abbreviations, and server names</param>
    /// <returns>The cleaned player name</returns>
    public string ProcessPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Step 1: Remove game special characters (BoxedNumber, BoxedOutlinedNumber, etc.)
        var cleanedName = GameIconHelper.RemoveGameSpecialCharacters(name);

        // Step 2: Remove job abbreviations from the beginning
        cleanedName = RemoveJobAbbreviationFromName(cleanedName);

        // Step 3: Remove server names from the end
        cleanedName = RemoveServerNameFromPlayerName(cleanedName);

        return cleanedName;
    }

    /// <summary>
    /// Removes job abbreviations from the beginning of player names.
    /// </summary>
    /// <param name="name">The player name that might contain job abbreviations</param>
    /// <returns>The player name with job abbreviations removed</returns>
    private string RemoveJobAbbreviationFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        try
        {
            var jobAbbreviations = GetJobAbbreviations();
            var trimmedName = name.Trim();

            // Check if the name starts with any job abbreviation followed by a space
            foreach (var jobAbbr in jobAbbreviations)
            {
                if (trimmedName.StartsWith(jobAbbr + " ", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the job abbreviation and the following space
                    var cleanedName = trimmedName.Substring(jobAbbr.Length + 1).Trim();
                    Plugin.Log.Debug($"Removed job abbreviation '{jobAbbr}' from player name: '{trimmedName}' -> '{cleanedName}'");
                    return cleanedName;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting job abbreviations: {ex.Message}");
        }

        return name;
    }

    /// <summary>
    /// Removes server names from the end of player names using intelligent camelCase detection.
    /// Only removes server names from camelCase words, not from separate words.
    /// </summary>
    /// <param name="name">The player name that might contain server names</param>
    /// <returns>The player name with server names removed</returns>
    private string RemoveServerNameFromPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        try
        {
            var serverNames = GetServerNames();
            var trimmedName = name.Trim();

            // Split the name into parts by spaces
            var nameParts = trimmedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // If there are exactly two parts, check the second part for camelCase + server name
            if (nameParts.Length == 2)
            {
                var firstPart = nameParts[0];
                var secondPart = nameParts[1];

                // Check if the second part ends with a server name
                foreach (var serverName in serverNames)
                {
                    if (secondPart.EndsWith(serverName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if there's content before the server name
                        var serverStartIndex = secondPart.Length - serverName.Length;
                        if (serverStartIndex > 0)
                        {
                            var beforeServerPart = secondPart.Substring(0, serverStartIndex);

                            // Key logic: Check if the entire second part uses camelCase pattern
                            // This means it starts with a capital letter and has mixed case
                            if (IsCamelCaseWord(secondPart))
                            {
                                // Remove the server name from the second part
                                var cleanedSecondPart = beforeServerPart;
                                var cleanedName = $"{firstPart} {cleanedSecondPart}";

                                Plugin.Log.Debug($"Removed server name '{serverName}' from camelCase player name: '{trimmedName}' -> '{cleanedName}'");
                                return cleanedName;
                            }
                            else
                            {
                                // No camelCase pattern detected, keep the original name
                                Plugin.Log.Debug($"No camelCase pattern detected in '{secondPart}', keeping original name: '{trimmedName}'");
                            }
                        }
                        else
                        {
                            // The entire second part is just the server name, this is a separate word case
                            // Don't remove it as it's likely part of the player name
                            Plugin.Log.Debug($"Second part '{secondPart}' is entirely server name, keeping as player name: '{trimmedName}'");
                        }
                    }
                }
            }

            // For other cases (single word, more than two words), we don't remove server names
            // as they are likely legitimate parts of player names
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting server names: {ex.Message}");
        }

        return name;
    }

    /// <summary>
    /// Checks if a word follows camelCase naming pattern.
    /// In FFXIV, player names always start with capital letters. A camelCase word contains multiple capital letters.
    /// Examples: "TaruTonberry" (T + T = camelCase), "PlayerName" (P + N = camelCase), "Tonberry" (only T = not camelCase)
    /// </summary>
    /// <param name="word">The word to check</param>
    /// <returns>True if the word follows camelCase pattern</returns>
    private bool IsCamelCaseWord(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 2)
            return false;

        // Must start with a capital letter for our camelCase definition
        if (!char.IsUpper(word[0]))
            return false;

        // Count capital letters (excluding the first one)
        int capitalCount = 0;
        for (int i = 1; i < word.Length; i++)
        {
            if (char.IsUpper(word[i]))
            {
                capitalCount++;
            }
        }

        // Must have at least one more capital letter to be considered camelCase
        // This distinguishes "TaruTonberry" (camelCase) from "Tonberry" (single word)
        return capitalCount > 0;
    }

    /// <summary>
    /// Gets all job abbreviations from game data with caching.
    /// </summary>
    /// <returns>A set of job abbreviations</returns>
    private HashSet<string> GetJobAbbreviations()
    {
        // Check if cache is still valid
        if (cachedJobAbbreviations != null && DateTime.Now - lastCacheUpdate < cacheExpiry)
        {
            return cachedJobAbbreviations;
        }

        // Refresh cache
        var jobAbbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var classJobSheet = Svc.Data.GetExcelSheet<ClassJob>();
            if (classJobSheet != null)
            {
                foreach (var job in classJobSheet)
                {
                    var abbreviation = job.Abbreviation.ToString();
                    if (!string.IsNullOrWhiteSpace(abbreviation))
                    {
                        jobAbbreviations.Add(abbreviation);
                    }
                }
            }

            cachedJobAbbreviations = jobAbbreviations;
            lastCacheUpdate = DateTime.Now;
            Plugin.Log.Debug($"Cached {jobAbbreviations.Count} job abbreviations");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading job abbreviations: {ex.Message}");
            // Return empty set on error, but don't cache it
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return jobAbbreviations;
    }

    /// <summary>
    /// Gets all server names from game data with caching.
    /// </summary>
    /// <returns>A set of server names</returns>
    private HashSet<string> GetServerNames()
    {
        // Check if cache is still valid
        if (cachedServerNames != null && DateTime.Now - lastCacheUpdate < cacheExpiry)
        {
            return cachedServerNames;
        }

        // Refresh cache
        var serverNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Get all public worlds from all regions
            var allWorlds = ExcelWorldHelper.GetPublicWorlds();
            foreach (var world in allWorlds)
            {
                var worldName = world.Name.ToString();
                if (!string.IsNullOrWhiteSpace(worldName))
                {
                    serverNames.Add(worldName);
                }
            }

            cachedServerNames = serverNames;
            lastCacheUpdate = DateTime.Now;
            Plugin.Log.Debug($"Cached {serverNames.Count} server names");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading server names: {ex.Message}");
            // Return empty set on error, but don't cache it
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return serverNames;
    }

    /// <summary>
    /// Clears the cache, forcing a refresh on next access.
    /// </summary>
    public void ClearCache()
    {
        cachedJobAbbreviations = null;
        cachedServerNames = null;
        lastCacheUpdate = DateTime.MinValue;
        Plugin.Log.Debug("Player name processing cache cleared");
    }

    /// <summary>
    /// Gets cache statistics for debugging.
    /// </summary>
    /// <returns>Cache statistics</returns>
    public (int JobAbbreviations, int ServerNames, DateTime LastUpdate, bool IsExpired) GetCacheStats()
    {
        var isExpired = DateTime.Now - lastCacheUpdate >= cacheExpiry;
        return (
            cachedJobAbbreviations?.Count ?? 0,
            cachedServerNames?.Count ?? 0,
            lastCacheUpdate,
            isExpired
        );
    }
}
