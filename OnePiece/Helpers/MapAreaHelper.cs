using System;
using System.Linq;
using Dalamud.Plugin.Services;
using OnePiece.Models;
using OnePiece.Services;

namespace OnePiece.Helpers
{
    /// <summary>
    /// Helper class for map area operations including translation and validation.
    /// Centralizes all map area translation logic to avoid code duplication.
    /// </summary>
    public static class MapAreaHelper
    {
        /// <summary>
        /// Translates a map area name to English if needed.
        /// </summary>
        /// <param name="mapAreaName">The map area name to translate.</param>
        /// <param name="translationService">The translation service to use.</param>
        /// <returns>The English version of the map area name, or the original if translation fails.</returns>
        public static string GetEnglishMapArea(string mapAreaName, MapAreaTranslationService? translationService)
        {
            if (string.IsNullOrEmpty(mapAreaName))
                return mapAreaName;
                
            if (translationService == null)
                return mapAreaName; // Return original if no translation service provided
                
            try
            {
                return translationService.TranslateToEnglish(mapAreaName);
            }
            catch
            {
                return mapAreaName; // Fallback to original if translation fails
            }
        }

        /// <summary>
        /// Translates and validates a map area name, with logging.
        /// </summary>
        /// <param name="mapAreaName">The map area name to translate and validate.</param>
        /// <param name="translationService">The translation service to use.</param>
        /// <param name="aetheryteService">The aetheryte service for validation.</param>
        /// <param name="log">The logger for recording translation and validation results.</param>
        /// <param name="contextInfo">Additional context information for logging (e.g., coordinate info).</param>
        /// <returns>A tuple containing (isValid, englishMapArea, originalMapArea).</returns>
        public static (bool isValid, string englishMapArea, string originalMapArea) TranslateAndValidateMapArea(
            string mapAreaName, 
            MapAreaTranslationService? translationService, 
            AetheryteService aetheryteService, 
            IPluginLog log, 
            string contextInfo = "")
        {
            if (string.IsNullOrEmpty(mapAreaName))
                return (false, mapAreaName, mapAreaName);

            var originalMapArea = mapAreaName;
            var englishMapArea = GetEnglishMapArea(mapAreaName, translationService);
            
            // Log translation if it occurred
            if (!originalMapArea.Equals(englishMapArea, StringComparison.OrdinalIgnoreCase))
            {
                var logMessage = string.IsNullOrEmpty(contextInfo) 
                    ? $"Translated map area for validation: '{originalMapArea}' -> '{englishMapArea}'"
                    : $"Translated map area for validation {contextInfo}: '{originalMapArea}' -> '{englishMapArea}'";
                log.Information(logMessage);
            }

            // Validate using English map area name
            bool isValid = aetheryteService.IsValidMapArea(englishMapArea);
            
            if (!isValid)
            {
                var logMessage = string.IsNullOrEmpty(contextInfo)
                    ? $"Invalid map area '{englishMapArea}' (original: '{originalMapArea}')"
                    : $"Invalid map area '{englishMapArea}' (original: '{originalMapArea}') {contextInfo}";
                log.Warning(logMessage);
            }

            return (isValid, englishMapArea, originalMapArea);
        }

        /// <summary>
        /// Gets the English map area name from a TreasureCoordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate containing the map area.</param>
        /// <param name="translationService">The translation service to use.</param>
        /// <returns>The English version of the map area name.</returns>
        public static string GetEnglishMapAreaFromCoordinate(TreasureCoordinate coordinate, MapAreaTranslationService? translationService)
        {
            if (coordinate == null || string.IsNullOrEmpty(coordinate.MapArea))
                return string.Empty;
                
            return GetEnglishMapArea(coordinate.MapArea, translationService);
        }

        /// <summary>
        /// Gets the English map area name from a collection of coordinates that share the same map area.
        /// </summary>
        /// <param name="coordinates">The coordinates collection.</param>
        /// <param name="mapAreaName">The map area name to look for.</param>
        /// <param name="translationService">The translation service to use.</param>
        /// <returns>The English version of the map area name.</returns>
        public static string GetEnglishMapAreaFromCollection(
            System.Collections.Generic.IEnumerable<TreasureCoordinate> coordinates, 
            string mapAreaName, 
            MapAreaTranslationService? translationService)
        {
            var coordinate = coordinates?.FirstOrDefault(c => c.MapArea == mapAreaName);
            return coordinate != null 
                ? GetEnglishMapAreaFromCoordinate(coordinate, translationService) 
                : GetEnglishMapArea(mapAreaName, translationService);
        }
    }
}
