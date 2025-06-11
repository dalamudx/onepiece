using System;
using System.Collections.Generic;
using System.Globalization;
using Dalamud.Plugin.Services;
using Dalamud.Game;

namespace OnePiece.Localization;

/// <summary>
/// Manages localization for the plugin using strongly-typed C# classes.
/// Provides compile-time safety and IntelliSense support for all localized strings.
/// </summary>
public static class LocalizationManager
{
    private static readonly Dictionary<string, ILocalizationData> LoadedLanguages = new();
    private static string CurrentLanguage = "en";
    private static readonly string[] SupportedLanguages = { "en", "ja", "de", "fr", "zh" };
    private static ILocalizationData? CurrentLocalizationData;

    /// <summary>
    /// Gets the current localization data instance.
    /// </summary>
    public static ILocalizationData Current => CurrentLocalizationData ?? new EN();
    
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

            Plugin.Log.Information($"Localization initialized with language: {CurrentLanguage}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error initializing localization: {ex.Message}");
            CurrentLanguage = "en"; // Fallback to English
            CurrentLocalizationData = new EN();
        }
    }

    /// <summary>
    /// Sets the current language and loads its localization data.
    /// </summary>
    /// <param name="language">The language code (e.g., "en", "ja").</param>
    /// <returns>True if the language was set successfully, false otherwise.</returns>
    public static bool SetLanguage(string language)
    {
        if (!IsLanguageSupported(language))
        {
            Plugin.Log.Error($"Language not supported: {language}");
            return false;
        }

        CurrentLanguage = language;
        LoadLanguage(language);

        Plugin.Log.Information($"Language set to: {language}");
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
    /// Gets the client language code from the game client.
    /// </summary>
    /// <returns>The client language code.</returns>
    public static string GetClientLanguage()
    {
        try
        {
            // Get the client language from the game client
            var clientLanguage = Plugin.ClientState.ClientLanguage;
            return clientLanguage switch
            {
                ClientLanguage.Japanese => "ja",
                ClientLanguage.English => "en",
                ClientLanguage.German => "de",
                ClientLanguage.French => "fr",
                _ => "en" // Default to English
            };
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting client language: {ex.Message}");
            return "en"; // Fallback to English
        }
    }

    /// <summary>
    /// Gets the LocationExample string in the client's language for message preview.
    /// This ensures the preview shows what will actually be sent to chat.
    /// </summary>
    /// <returns>The LocationExample string in client language.</returns>
    public static string GetClientLanguageLocationExample()
    {
        try
        {
            var clientLanguage = GetClientLanguage();

            // Get localization data for client language
            ILocalizationData clientLocalizationData = clientLanguage switch
            {
                "en" => new EN(),
                "ja" => new JA(),
                "zh" => new ZH(),
                "de" => new DE(),
                "fr" => new FR(),
                _ => new EN() // Fallback to English
            };

            return clientLocalizationData.LocationExample;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error getting client language location example: {ex.Message}");
            return "Limsa Lominsa - Lower Decks ( 9.5 , 11.2 )"; // Fallback to English
        }
    }

    /// <summary>
    /// Loads localization data for a specific language.
    /// </summary>
    /// <param name="language">The language code to load.</param>
    private static void LoadLanguage(string language)
    {
        try
        {
            // Check if already loaded
            if (LoadedLanguages.TryGetValue(language, out var existingData))
            {
                CurrentLocalizationData = existingData;
                return;
            }

            // Create localization data based on language
            ILocalizationData localizationData = language switch
            {
                "en" => new EN(),
                "ja" => new JA(),
                "zh" => new ZH(),
                "de" => new DE(),
                "fr" => new FR(),
                _ => new EN() // Fallback to English
            };

            LoadedLanguages[language] = localizationData;
            CurrentLocalizationData = localizationData;

            Plugin.Log.Information($"Loaded localization data for language: {language}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading language {language}: {ex.Message}");
            // Fallback to English
            CurrentLocalizationData = new EN();
        }
    }
}
