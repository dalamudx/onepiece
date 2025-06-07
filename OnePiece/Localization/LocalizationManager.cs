using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Dalamud.Plugin.Services;

namespace OnePiece.Localization;

/// <summary>
/// Manages localization for the plugin using JSON-based language files.
/// </summary>
public static class LocalizationManager
{
    private static readonly IPluginLog Log = Plugin.Log;
    private static readonly Dictionary<string, Dictionary<string, string>> LoadedTranslations = new();
    private static string CurrentLanguage = "en";
    private static readonly string[] SupportedLanguages = { "en", "ja", "de", "fr", "zh" };
    
    /// <summary>
    /// Initializes the localization system.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // Set default language based on client culture
            var clientCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            CurrentLanguage = IsLanguageSupported(clientCulture) ? clientCulture : "en";

            // Load the current language
            LoadLanguage(CurrentLanguage);

            Log.Information($"Localization initialized with language: {CurrentLanguage}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error initializing localization: {ex.Message}");
            CurrentLanguage = "en"; // Fallback to English
            LoadFallbackTranslations();
        }
    }

    /// <summary>
    /// Gets a localized string.
    /// </summary>
    /// <param name="key">The string key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    public static string GetString(string key)
    {
        // Try current language first
        if (LoadedTranslations.TryGetValue(CurrentLanguage, out var languageStrings) &&
            languageStrings.TryGetValue(key, out var translation))
        {
            return translation;
        }

        // Fallback to English if not current language
        if (CurrentLanguage != "en" &&
            LoadedTranslations.TryGetValue("en", out var englishStrings) &&
            englishStrings.TryGetValue(key, out var englishTranslation))
        {
            return englishTranslation;
        }

        // If all else fails, return the key itself
        Log.Warning($"Missing translation for key: {key}");
        return key;
    }

    /// <summary>
    /// Sets the current language and loads its translations.
    /// </summary>
    /// <param name="language">The language code (e.g., "en", "ja").</param>
    /// <returns>True if the language was set successfully, false otherwise.</returns>
    public static bool SetLanguage(string language)
    {
        if (!IsLanguageSupported(language))
        {
            Log.Error($"Language not supported: {language}");
            return false;
        }

        CurrentLanguage = language;
        
        // Load the new language if not already loaded
        if (!LoadedTranslations.ContainsKey(language))
        {
            LoadLanguage(language);
        }

        Log.Information($"Language set to: {language}");
        return true;
    }

    /// <summary>
    /// Gets the current language.
    /// </summary>
    /// <returns>The current language code.</returns>
    public static string GetCurrentLanguage() => CurrentLanguage;

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    /// <returns>An array of supported language codes.</returns>
    public static string[] GetSupportedLanguages() => (string[])SupportedLanguages.Clone();

    /// <summary>
    /// Checks if a language is supported.
    /// </summary>
    /// <param name="language">The language code to check.</param>
    /// <returns>True if the language is supported, false otherwise.</returns>
    public static bool IsLanguageSupported(string language) =>
        Array.IndexOf(SupportedLanguages, language) >= 0;

    /// <summary>
    /// Loads translations for a specific language from JSON file.
    /// </summary>
    /// <param name="language">The language code to load.</param>
    private static void LoadLanguage(string language)
    {
        try
        {
            var languageFilePath = GetLanguageFilePath(language);
            
            if (!File.Exists(languageFilePath))
            {
                Log.Warning($"Language file not found: {languageFilePath}");
                
                // If it's not English, try to load English as fallback
                if (language != "en")
                {
                    LoadLanguage("en");
                }
                else
                {
                    LoadFallbackTranslations();
                }
                return;
            }

            var jsonContent = File.ReadAllText(languageFilePath);
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            
            if (translations != null)
            {
                LoadedTranslations[language] = translations;
                Log.Information($"Loaded {translations.Count} translations for language: {language}");
            }
            else
            {
                Log.Error($"Failed to deserialize translations for language: {language}");
                if (language == "en")
                {
                    LoadFallbackTranslations();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading language file for {language}: {ex.Message}");
            if (language == "en")
            {
                LoadFallbackTranslations();
            }
        }
    }

    /// <summary>
    /// Gets the file path for a language file.
    /// </summary>
    /// <param name="language">The language code.</param>
    /// <returns>The full path to the language file.</returns>
    private static string GetLanguageFilePath(string language)
    {
        var pluginDir = Plugin.PluginInterface.AssemblyLocation.Directory?.FullName ?? "";
        return Path.Combine(pluginDir, "Localization", "Languages", $"{language}.json");
    }

    /// <summary>
    /// Loads minimal fallback translations when JSON files are not available.
    /// </summary>
    private static void LoadFallbackTranslations()
    {
        var fallback = new Dictionary<string, string>
        {
            { "MainWindowTitle", "One Piece" },
            { "MainWindowSubtitle", "Plan and optimize your treasure hunt route" },
            { "ClearAll", "Clear All" },
            { "OptimizeRoute", "Optimize Route" },
            { "ResetOptimization", "Reset Optimization" },
            { "Export", "Export" },
            { "Import", "Import" },
            { "TeleportButton", "Teleport" },
            { "SendToChat", "Send to Chat" },
            { "Collected", "Collected" },
            { "Delete", "Delete" },
            { "Restore", "Restore" },
            { "Error", "Error" },
            { "Success", "Success" }
        };
        
        LoadedTranslations["en"] = fallback;
        Log.Information("Loaded fallback translations");
    }

    /// <summary>
    /// Reloads all loaded languages from disk.
    /// Useful for development and hot-reloading.
    /// </summary>
    public static void ReloadTranslations()
    {
        var loadedLanguages = new List<string>(LoadedTranslations.Keys);
        LoadedTranslations.Clear();
        
        foreach (var language in loadedLanguages)
        {
            LoadLanguage(language);
        }
        
        Log.Information("Reloaded all translations");
    }

    /// <summary>
    /// Preloads all supported languages.
    /// Useful for better performance if memory usage is not a concern.
    /// </summary>
    public static void PreloadAllLanguages()
    {
        foreach (var language in SupportedLanguages)
        {
            if (!LoadedTranslations.ContainsKey(language))
            {
                LoadLanguage(language);
            }
        }
        
        Log.Information("Preloaded all supported languages");
    }
}
