using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace OnePiece.Services
{
    /// <summary>
    /// Service for translating map area names from different languages to English.
    /// Uses Dalamud's Excel data to provide accurate translations.
    /// </summary>
    public class MapAreaTranslationService
    {
        private readonly IPluginLog log;
        private readonly Dictionary<string, string> translationCache;
        private readonly Dictionary<ClientLanguage, Dictionary<string, string>> languageMappings;
        private bool isInitialized = false;

        public MapAreaTranslationService(IPluginLog logger)
        {
            log = logger ?? throw new ArgumentNullException(nameof(logger));
            translationCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            languageMappings = new Dictionary<ClientLanguage, Dictionary<string, string>>();
        }

        /// <summary>
        /// Initializes the translation service by building language mappings.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            try
            {
                log.Information("Initializing MapAreaTranslationService...");
                BuildLanguageMappings();
                isInitialized = true;
                log.Information($"MapAreaTranslationService initialized with {translationCache.Count} cached translations");
            }
            catch (Exception ex)
            {
                log.Error($"Failed to initialize MapAreaTranslationService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Translates a map area name from any supported language to English.
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
                log.Debug($"Found cached translation: '{mapAreaName}' -> '{cachedTranslation}'");
                return cachedTranslation;
            }

            // If input is already English, return as-is
            if (IsEnglishMapArea(mapAreaName))
            {
                translationCache[mapAreaName] = mapAreaName;
                return mapAreaName;
            }

            // Search through all language mappings
            foreach (var languageMapping in languageMappings.Values)
            {
                if (languageMapping.TryGetValue(mapAreaName, out var englishName))
                {
                    log.Information($"Translated map area: '{mapAreaName}' -> '{englishName}'");
                    translationCache[mapAreaName] = englishName;
                    return englishName;
                }
            }

            log.Warning($"No translation found for map area: '{mapAreaName}'");
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
                log.Warning($"Error checking if '{mapAreaName}' is English: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Builds language mappings from non-English languages to English using PlaceName Excel data.
        /// </summary>
        private void BuildLanguageMappings()
        {
            var supportedLanguages = new[]
            {
                ClientLanguage.Japanese,
                ClientLanguage.German,
                ClientLanguage.French
            };

            try
            {
                // Get English place names as the target
                var englishPlaceNames = Svc.Data.GetExcelSheet<PlaceName>(ClientLanguage.English);
                if (englishPlaceNames == null)
                {
                    log.Error("Failed to load English PlaceName data");
                    return;
                }

                // Build mappings for each supported language
                foreach (var language in supportedLanguages)
                {
                    try
                    {
                        var languageMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        var localizedPlaceNames = Svc.Data.GetExcelSheet<PlaceName>(language);
                        
                        if (localizedPlaceNames == null)
                        {
                            log.Warning($"Failed to load PlaceName data for language: {language}");
                            continue;
                        }

                        // Create mapping from localized name to English name
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

                            // Only add if names are different (avoid English->English mappings)
                            if (!localizedName.Equals(englishName, StringComparison.OrdinalIgnoreCase))
                            {
                                languageMapping[localizedName] = englishName;
                                log.Debug($"Added {language} mapping: '{localizedName}' -> '{englishName}'");
                            }
                        }

                        languageMappings[language] = languageMapping;
                        log.Information($"Built {language} mapping with {languageMapping.Count} entries");
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error building mapping for language {language}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error building language mappings: {ex.Message}");
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
            log.Information("Translation cache cleared");
        }

        /// <summary>
        /// Gets translation statistics for debugging purposes.
        /// </summary>
        /// <returns>A dictionary with language statistics.</returns>
        public Dictionary<string, int> GetTranslationStats()
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var kvp in languageMappings)
            {
                stats[kvp.Key.ToString()] = kvp.Value.Count;
            }
            
            stats["CacheSize"] = translationCache.Count;
            return stats;
        }
    }
}
