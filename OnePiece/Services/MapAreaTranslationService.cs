using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace OnePiece.Services
{
    /// <summary>
    /// Service for translating map area names from current client language to English.
    /// Optimized to only load mappings for the current client language for better performance.
    /// </summary>
    public class MapAreaTranslationService
    {
        private readonly Dictionary<string, string> translationCache;
        private readonly Dictionary<string, string> currentLanguageMapping;
        private readonly ClientLanguage currentClientLanguage;
        private bool isInitialized = false;

        public MapAreaTranslationService()
        {
            translationCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            currentLanguageMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            currentClientLanguage = Svc.ClientState.ClientLanguage;
        }

        /// <summary>
        /// Initializes the translation service by building mapping for current client language only.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            try
            {
                Plugin.Log.Information($"Initializing MapAreaTranslationService for client language: {currentClientLanguage}...");
                var startTime = DateTime.UtcNow;

                BuildCurrentLanguageMapping();

                var initTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                isInitialized = true;
                Plugin.Log.Information($"MapAreaTranslationService initialized in {initTime:F2}ms with {currentLanguageMapping.Count} mappings");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to initialize MapAreaTranslationService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Translates a map area name from current client language to English.
        /// </summary>
        /// <param name="mapAreaName">The map area name to translate.</param>
        /// <returns>The English map area name, or the original name if no translation found.</returns>
        public string TranslateToEnglish(string mapAreaName)
        {
            if (string.IsNullOrWhiteSpace(mapAreaName))
                return mapAreaName;

            // Ensure service is initialized
            if (!isInitialized)
                Initialize();

            // Check cache first
            if (translationCache.TryGetValue(mapAreaName, out var cachedTranslation))
            {
                Plugin.Log.Debug($"Found cached translation: '{mapAreaName}' -> '{cachedTranslation}'");
                return cachedTranslation;
            }

            // If client language is English, no translation needed
            if (currentClientLanguage == ClientLanguage.English)
            {
                translationCache[mapAreaName] = mapAreaName;
                return mapAreaName;
            }

            // Check if input is already English
            if (IsEnglishMapArea(mapAreaName))
            {
                translationCache[mapAreaName] = mapAreaName;
                return mapAreaName;
            }

            // Search in current language mapping
            if (currentLanguageMapping.TryGetValue(mapAreaName, out var englishName))
            {
                Plugin.Log.Information($"Translated map area: '{mapAreaName}' -> '{englishName}'");
                translationCache[mapAreaName] = englishName;
                return englishName;
            }

            Plugin.Log.Warning($"No translation found for map area: '{mapAreaName}' in language: {currentClientLanguage}");
            return mapAreaName; // Return original if no translation found
        }

        /// <summary>
        /// Checks if a map area name is already in English by looking it up in English PlaceName data.
        /// </summary>
        /// <param name="mapAreaName">The map area name to check.</param>
        /// <returns>True if the name exists in English PlaceName data.</returns>
        private bool IsEnglishMapArea(string mapAreaName)
        {
            try
            {
                var englishPlaceNames = Svc.Data.GetExcelSheet<PlaceName>(ClientLanguage.English);
                if (englishPlaceNames == null)
                    return false;

                return englishPlaceNames.Any(place => 
                    place.Name.ToString().Equals(mapAreaName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"Error checking if '{mapAreaName}' is English: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Builds mapping from current client language to English using PlaceName Excel data.
        /// Only loads data for the current client language for optimal performance.
        /// </summary>
        private void BuildCurrentLanguageMapping()
        {
            // If client language is English, no mapping needed
            if (currentClientLanguage == ClientLanguage.English)
            {
                Plugin.Log.Information("Client language is English, no translation mapping needed");
                return;
            }

            // Check if current language is supported
            var supportedLanguages = new[]
            {
                ClientLanguage.Japanese,
                ClientLanguage.German,
                ClientLanguage.French
            };

            if (!supportedLanguages.Contains(currentClientLanguage))
            {
                Plugin.Log.Warning($"Current client language {currentClientLanguage} is not supported for translation");
                return;
            }

            try
            {
                // Get English place names as the target
                var englishPlaceNames = Svc.Data.GetExcelSheet<PlaceName>(ClientLanguage.English);
                if (englishPlaceNames == null)
                {
                    Plugin.Log.Error("Failed to load English PlaceName data");
                    return;
                }

                // Get current language place names
                var localizedPlaceNames = Svc.Data.GetExcelSheet<PlaceName>(currentClientLanguage);
                if (localizedPlaceNames == null)
                {
                    Plugin.Log.Error($"Failed to load PlaceName data for current language: {currentClientLanguage}");
                    return;
                }

                // Create mapping from current language to English
                var mappingCount = 0;
                foreach (var localizedPlace in localizedPlaceNames)
                {
                    var localizedName = localizedPlace.Name.ToString();
                    if (string.IsNullOrWhiteSpace(localizedName))
                        continue;

                    // Find corresponding English name using RowId
                    var englishPlace = englishPlaceNames.GetRowOrDefault(localizedPlace.RowId);
                    if (englishPlace == null)
                        continue;

                    var englishName = englishPlace.Value.Name.ToString();
                    if (string.IsNullOrWhiteSpace(englishName))
                        continue;

                    // Only add if names are different (avoid same-language mappings)
                    if (!localizedName.Equals(englishName, StringComparison.OrdinalIgnoreCase))
                    {
                        currentLanguageMapping[localizedName] = englishName;
                        mappingCount++;
                        Plugin.Log.Debug($"Added mapping: '{localizedName}' -> '{englishName}'");
                    }
                }

                Plugin.Log.Information($"Built {currentClientLanguage} to English mapping with {mappingCount} entries");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error building mapping for current language {currentClientLanguage}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the number of cached translations.
        /// </summary>
        public int CacheSize => translationCache.Count;

        /// <summary>
        /// Clears the translation cache. Useful for testing or if data needs to be refreshed.
        /// </summary>
        public void ClearCache()
        {
            translationCache.Clear();
            Plugin.Log.Information("Translation cache cleared");
        }

        /// <summary>
        /// Gets translation statistics for debugging purposes.
        /// </summary>
        /// <returns>A dictionary with language statistics.</returns>
        public Dictionary<string, int> GetTranslationStats()
        {
            var stats = new Dictionary<string, int>
            {
                ["CurrentLanguage"] = currentClientLanguage == ClientLanguage.English ? 0 : currentLanguageMapping.Count,
                ["CacheSize"] = translationCache.Count,
                ["ClientLanguage"] = (int)currentClientLanguage
            };

            return stats;
        }

        /// <summary>
        /// Gets the current client language being used for translation.
        /// </summary>
        public ClientLanguage CurrentClientLanguage => currentClientLanguage;
    }
}
