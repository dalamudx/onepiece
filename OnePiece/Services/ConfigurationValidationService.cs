using System;
using System.Collections.Generic;
using System.Linq;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for validating and fixing configuration issues.
/// </summary>
public class ConfigurationValidationService : IDisposable
{
    private readonly Plugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public ConfigurationValidationService(Plugin plugin)
    {
        this.plugin = plugin;
    }

    /// <summary>
    /// Validates the configuration and returns a validation result.
    /// </summary>
    /// <returns>The validation result containing any issues found.</returns>
    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();
        var config = plugin.Configuration;

        try
        {
            // Validate language setting
            ValidateLanguageSetting(config, result);

            // Validate log level
            ValidateLogLevel(config, result);

            // Validate chat channel
            ValidateChatChannel(config, result);

            // Validate message templates
            ValidateMessageTemplates(config, result);

            // Validate active template index
            ValidateActiveTemplateIndex(config, result);

            // Validate selected message components
            ValidateSelectedMessageComponents(config, result);

            Plugin.Log.Information($"Configuration validation completed. Found {result.Errors.Count} errors and {result.Warnings.Count} warnings.");
        }
        catch (Exception ex)
        {
            result.AddError($"Unexpected error during configuration validation: {ex.Message}");
            Plugin.Log.Error($"Error during configuration validation: {ex}");
        }

        return result;
    }

    /// <summary>
    /// Performs a safe configuration validation that won't throw exceptions during plugin initialization.
    /// </summary>
    /// <returns>The validation result after fixes have been applied.</returns>
    public ValidationResult SafeValidateAndFixConfiguration()
    {
        try
        {
            // First, ensure basic configuration structure is valid
            EnsureBasicConfigurationStructure();

            // Then perform full validation and fixes
            return ValidateAndFixConfiguration();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Critical error during safe configuration validation: {ex}");

            // Try to reset configuration to defaults
            try
            {
                ResetConfigurationToDefaults();
                Plugin.Log.Information("Configuration reset to defaults due to critical errors");
            }
            catch (Exception resetEx)
            {
                Plugin.Log.Error($"Failed to reset configuration to defaults: {resetEx}");
            }

            var errorResult = new ValidationResult();
            errorResult.AddError($"Critical validation error, configuration reset to defaults: {ex.Message}");
            return errorResult;
        }
    }

    /// <summary>
    /// Validates and fixes configuration issues automatically where possible.
    /// </summary>
    /// <returns>The validation result after fixes have been applied.</returns>
    public ValidationResult ValidateAndFixConfiguration()
    {
        try
        {
            var result = ValidateConfiguration();

            if (result.HasIssues)
            {
                ApplyAutomaticFixes(result);

                // Re-validate after fixes
                result = ValidateConfiguration();

                if (result.IsValid)
                {
                    Plugin.Log.Information("Configuration issues were automatically fixed.");
                    plugin.Configuration.Save();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Critical error during configuration validation and fixing: {ex}");

            // Return a result with the error
            var errorResult = new ValidationResult();
            errorResult.AddError($"Critical validation error: {ex.Message}");
            return errorResult;
        }
    }

    /// <summary>
    /// Validates the language setting.
    /// </summary>
    private void ValidateLanguageSetting(Configuration config, ValidationResult result)
    {
        var supportedLanguages = new[] { "English", "Japanese", "German", "French" };
        
        if (string.IsNullOrEmpty(config.Language))
        {
            result.AddError("Language setting is empty");
        }
        else if (!supportedLanguages.Contains(config.Language))
        {
            result.AddWarning($"Language '{config.Language}' is not officially supported. Supported languages: {string.Join(", ", supportedLanguages)}");
        }
    }

    /// <summary>
    /// Validates the log level setting.
    /// </summary>
    private void ValidateLogLevel(Configuration config, ValidationResult result)
    {
        if (!Enum.IsDefined(typeof(LogLevel), config.LogLevel))
        {
            result.AddError($"Invalid log level: {config.LogLevel}");
        }
    }

    /// <summary>
    /// Validates the chat channel setting.
    /// </summary>
    private void ValidateChatChannel(Configuration config, ValidationResult result)
    {
        if (!Enum.IsDefined(typeof(ChatChannelType), config.MonitoredChatChannel))
        {
            result.AddError($"Invalid chat channel: {config.MonitoredChatChannel}");
        }
    }

    /// <summary>
    /// Validates message templates.
    /// </summary>
    private void ValidateMessageTemplates(Configuration config, ValidationResult result)
    {
        if (config.MessageTemplates == null)
        {
            result.AddError("MessageTemplates collection is null");
            return;
        }

        for (int i = 0; i < config.MessageTemplates.Count; i++)
        {
            var template = config.MessageTemplates[i];
            
            if (template == null)
            {
                result.AddError($"Message template at index {i} is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(template.Name))
            {
                result.AddWarning($"Message template at index {i} has empty name");
            }

            if (template.Components == null)
            {
                result.AddError($"Message template '{template.Name}' has null components collection");
            }
            else if (template.Components.Count == 0)
            {
                result.AddWarning($"Message template '{template.Name}' has no components");
            }
        }

        // Check for duplicate template names
        var duplicateNames = config.MessageTemplates
            .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name))
            .GroupBy(t => t.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            result.AddWarning($"Duplicate message template name: '{duplicateName}'");
        }
    }

    /// <summary>
    /// Validates the active template index.
    /// </summary>
    private void ValidateActiveTemplateIndex(Configuration config, ValidationResult result)
    {
        if (config.ActiveTemplateIndex < -1)
        {
            result.AddError($"Invalid active template index: {config.ActiveTemplateIndex}. Must be -1 or greater.");
        }
        else if (config.ActiveTemplateIndex >= 0)
        {
            if (config.MessageTemplates == null || config.ActiveTemplateIndex >= config.MessageTemplates.Count)
            {
                result.AddError($"Active template index {config.ActiveTemplateIndex} is out of range. Template count: {config.MessageTemplates?.Count ?? 0}");
            }
        }
    }

    /// <summary>
    /// Validates selected message components.
    /// </summary>
    private void ValidateSelectedMessageComponents(Configuration config, ValidationResult result)
    {
        if (config.SelectedMessageComponents == null)
        {
            result.AddError("SelectedMessageComponents collection is null");
            return;
        }

        // Check for invalid component types
        var validComponentTypes = Enum.GetValues(typeof(MessageComponentType)).Cast<MessageComponentType>().ToArray();
        var invalidComponents = config.SelectedMessageComponents
            .Where(c => c != null && !validComponentTypes.Contains(c.Type))
            .ToList();

        foreach (var invalidComponent in invalidComponents)
        {
            result.AddWarning($"Invalid message component type: {invalidComponent.Type}");
        }

        // Check for null components
        var nullComponents = config.SelectedMessageComponents.Where(c => c == null).Count();
        if (nullComponents > 0)
        {
            result.AddWarning($"Found {nullComponents} null message components");
        }

        // Check for invalid custom message indices
        foreach (var component in config.SelectedMessageComponents.Where(c => c != null && c.Type == MessageComponentType.CustomMessage))
        {
            if (component.CustomMessageIndex < 0 || component.CustomMessageIndex >= config.CustomMessages.Count)
            {
                result.AddWarning($"Message component has invalid custom message index: {component.CustomMessageIndex}. Valid range: 0-{config.CustomMessages.Count - 1}");
            }
        }
    }

    /// <summary>
    /// Applies automatic fixes to configuration issues.
    /// </summary>
    private void ApplyAutomaticFixes(ValidationResult result)
    {
        try
        {
            var config = plugin.Configuration;

            if (config == null)
            {
                Plugin.Log.Error("Configuration is null, cannot apply fixes");
                return;
            }

        // Fix language setting
        if (string.IsNullOrEmpty(config.Language))
        {
            config.Language = "English";
            Plugin.Log.Information("Fixed empty language setting to 'English'");
        }

        // Fix invalid log level
        if (!Enum.IsDefined(typeof(LogLevel), config.LogLevel))
        {
            config.LogLevel = LogLevel.Normal;
            Plugin.Log.Information("Fixed invalid log level to 'Normal'");
        }

        // Fix invalid chat channel
        if (!Enum.IsDefined(typeof(ChatChannelType), config.MonitoredChatChannel))
        {
            config.MonitoredChatChannel = ChatChannelType.Party;
            Plugin.Log.Information("Fixed invalid chat channel to 'Party'");
        }

        // Fix null collections
        if (config.MessageTemplates == null)
        {
            config.MessageTemplates = new List<MessageTemplate>();
            Plugin.Log.Information("Fixed null MessageTemplates collection");
        }

        if (config.SelectedMessageComponents == null)
        {
            config.SelectedMessageComponents = new List<MessageComponent>();
            Plugin.Log.Information("Fixed null SelectedMessageComponents collection");
        }

        if (config.CustomMessages == null)
        {
            config.CustomMessages = new List<string>();
            Plugin.Log.Information("Fixed null CustomMessages collection");
        }

        // Fix invalid active template index
        if (config.ActiveTemplateIndex < -1 || 
            (config.ActiveTemplateIndex >= 0 && config.ActiveTemplateIndex >= config.MessageTemplates.Count))
        {
            config.ActiveTemplateIndex = -1;
            Plugin.Log.Information("Fixed invalid active template index to -1 (no active template)");
        }

        // Remove null templates
        if (config.MessageTemplates.Any(t => t == null))
        {
            config.MessageTemplates = config.MessageTemplates.Where(t => t != null).ToList();
            Plugin.Log.Information("Removed null message templates");
        }

        // Fix templates with null components
        foreach (var template in config.MessageTemplates.Where(t => t.Components == null))
        {
            template.Components = new List<MessageComponent>();
            Plugin.Log.Information($"Fixed null components collection for template '{template.Name}'");
        }

        // Remove invalid message components (only if collection is not null)
        if (config.SelectedMessageComponents != null)
        {
            try
            {
                var validComponentTypes = Enum.GetValues(typeof(MessageComponentType)).Cast<MessageComponentType>().ToArray();
                var originalCount = config.SelectedMessageComponents.Count;

                // Remove null components and invalid component types
                config.SelectedMessageComponents = config.SelectedMessageComponents
                    .Where(c => c != null && validComponentTypes.Contains(c.Type))
                    .ToList();

                if (config.SelectedMessageComponents.Count != originalCount)
                {
                    Plugin.Log.Information($"Removed {originalCount - config.SelectedMessageComponents.Count} invalid message components");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error cleaning up message components: {ex}");
                // Reset to empty list if cleanup fails
                config.SelectedMessageComponents = new List<MessageComponent>();
            }

            // Fix invalid custom message indices
            try
            {
                foreach (var component in config.SelectedMessageComponents.Where(c => c != null && c.Type == MessageComponentType.CustomMessage))
                {
                    if (component.CustomMessageIndex < 0 || component.CustomMessageIndex >= config.CustomMessages.Count)
                    {
                        component.CustomMessageIndex = -1; // Reset to invalid index
                        Plugin.Log.Information($"Fixed invalid custom message index for component");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error fixing custom message indices: {ex}");
            }
        }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error applying automatic configuration fixes: {ex}");
        }
    }

    /// <summary>
    /// Ensures basic configuration structure is valid before detailed validation.
    /// </summary>
    private void EnsureBasicConfigurationStructure()
    {
        var config = plugin.Configuration;

        if (config == null)
        {
            throw new InvalidOperationException("Configuration is null");
        }

        // Ensure collections are not null
        config.MessageTemplates ??= new List<MessageTemplate>();
        config.SelectedMessageComponents ??= new List<MessageComponent>();
        config.CustomMessages ??= new List<string>();

        // Ensure basic string properties are not null
        config.Language ??= "English";
    }

    /// <summary>
    /// Resets configuration to safe defaults.
    /// </summary>
    private void ResetConfigurationToDefaults()
    {
        var config = plugin.Configuration;

        config.Language = "English";
        config.LogLevel = LogLevel.Normal;
        config.MonitoredChatChannel = ChatChannelType.Party;
        config.EnableChatMonitoring = false;
        config.ActiveTemplateIndex = -1;

        config.MessageTemplates = new List<MessageTemplate>();
        config.SelectedMessageComponents = new List<MessageComponent>();
        config.CustomMessages = new List<string>();

        Plugin.Log.Information("Configuration reset to safe defaults");
    }

    /// <summary>
    /// Disposes the service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        // No specific resources to dispose currently
        // This method is here for future extensibility and consistency
    }
}

/// <summary>
/// Represents the result of a configuration validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the validation passed without errors.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether there are any issues (errors or warnings).
    /// </summary>
    public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Gets a summary of the validation result.
    /// </summary>
    /// <returns>A string summary of errors and warnings.</returns>
    public string GetSummary()
    {
        if (IsValid && !HasIssues)
            return "Configuration is valid with no issues.";

        var summary = new List<string>();
        
        if (Errors.Count > 0)
            summary.Add($"{Errors.Count} error(s)");
            
        if (Warnings.Count > 0)
            summary.Add($"{Warnings.Count} warning(s)");

        return $"Configuration validation found: {string.Join(", ", summary)}.";
    }
}
