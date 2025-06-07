using System;
using System.Collections.Generic;
using OnePiece.Models;
using OnePiece.Services;

namespace OnePiece.Tests;

/// <summary>
/// Simple tests for configuration validation to ensure fixes work correctly.
/// </summary>
public static class ConfigurationValidationTests
{
    /// <summary>
    /// Tests configuration validation with various edge cases.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <returns>True if all tests pass.</returns>
    public static bool RunTests(Plugin plugin)
    {
        try
        {
            Console.WriteLine("Running Configuration Validation Tests...");
            
            // Test 1: Null collections
            TestNullCollections(plugin);
            
            // Test 2: Invalid enum values
            TestInvalidEnumValues(plugin);
            
            // Test 3: Null message components
            TestNullMessageComponents(plugin);
            
            // Test 4: Invalid template indices
            TestInvalidTemplateIndices(plugin);
            
            Console.WriteLine("All Configuration Validation Tests passed!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Configuration Validation Tests failed: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Tests handling of null collections.
    /// </summary>
    private static void TestNullCollections(Plugin plugin)
    {
        Console.WriteLine("Testing null collections...");
        
        var config = plugin.Configuration;
        var originalTemplates = config.MessageTemplates;
        var originalComponents = config.SelectedMessageComponents;
        var originalMessages = config.CustomMessages;
        
        try
        {
            // Set collections to null
            config.MessageTemplates = null!;
            config.SelectedMessageComponents = null!;
            config.CustomMessages = null!;
            
            // Run validation
            var validationService = new ConfigurationValidationService(plugin);
            var result = validationService.SafeValidateAndFixConfiguration();
            
            // Check that collections are no longer null
            if (config.MessageTemplates == null)
                throw new Exception("MessageTemplates should not be null after validation");
            if (config.SelectedMessageComponents == null)
                throw new Exception("SelectedMessageComponents should not be null after validation");
            if (config.CustomMessages == null)
                throw new Exception("CustomMessages should not be null after validation");
                
            Console.WriteLine("✓ Null collections test passed");
        }
        finally
        {
            // Restore original values
            config.MessageTemplates = originalTemplates;
            config.SelectedMessageComponents = originalComponents;
            config.CustomMessages = originalMessages;
        }
    }

    /// <summary>
    /// Tests handling of invalid enum values.
    /// </summary>
    private static void TestInvalidEnumValues(Plugin plugin)
    {
        Console.WriteLine("Testing invalid enum values...");
        
        var config = plugin.Configuration;
        var originalLogLevel = config.LogLevel;
        var originalChatChannel = config.MonitoredChatChannel;
        
        try
        {
            // Set invalid enum values
            config.LogLevel = (LogLevel)999;
            config.MonitoredChatChannel = (ChatChannelType)999;
            
            // Run validation
            var validationService = new ConfigurationValidationService(plugin);
            var result = validationService.SafeValidateAndFixConfiguration();
            
            // Check that enum values are now valid
            if (!Enum.IsDefined(typeof(LogLevel), config.LogLevel))
                throw new Exception("LogLevel should be valid after validation");
            if (!Enum.IsDefined(typeof(ChatChannelType), config.MonitoredChatChannel))
                throw new Exception("ChatChannelType should be valid after validation");
                
            Console.WriteLine("✓ Invalid enum values test passed");
        }
        finally
        {
            // Restore original values
            config.LogLevel = originalLogLevel;
            config.MonitoredChatChannel = originalChatChannel;
        }
    }

    /// <summary>
    /// Tests handling of null message components.
    /// </summary>
    private static void TestNullMessageComponents(Plugin plugin)
    {
        Console.WriteLine("Testing null message components...");
        
        var config = plugin.Configuration;
        var originalComponents = new List<MessageComponent>(config.SelectedMessageComponents);
        
        try
        {
            // Add null components
            config.SelectedMessageComponents.Clear();
            config.SelectedMessageComponents.Add(new MessageComponent(MessageComponentType.PlayerName));
            config.SelectedMessageComponents.Add(null!);
            config.SelectedMessageComponents.Add(new MessageComponent(MessageComponentType.Coordinates));
            config.SelectedMessageComponents.Add(null!);
            
            // Run validation
            var validationService = new ConfigurationValidationService(plugin);
            var result = validationService.SafeValidateAndFixConfiguration();
            
            // Check that null components are removed
            foreach (var component in config.SelectedMessageComponents)
            {
                if (component == null)
                    throw new Exception("Null components should be removed after validation");
            }
            
            if (config.SelectedMessageComponents.Count != 2)
                throw new Exception($"Expected 2 components after cleanup, got {config.SelectedMessageComponents.Count}");
                
            Console.WriteLine("✓ Null message components test passed");
        }
        finally
        {
            // Restore original values
            config.SelectedMessageComponents.Clear();
            config.SelectedMessageComponents.AddRange(originalComponents);
        }
    }

    /// <summary>
    /// Tests handling of invalid template indices.
    /// </summary>
    private static void TestInvalidTemplateIndices(Plugin plugin)
    {
        Console.WriteLine("Testing invalid template indices...");
        
        var config = plugin.Configuration;
        var originalIndex = config.ActiveTemplateIndex;
        var originalTemplates = new List<MessageTemplate>(config.MessageTemplates);
        
        try
        {
            // Set up test scenario
            config.MessageTemplates.Clear();
            config.MessageTemplates.Add(new MessageTemplate("Test Template"));
            
            // Set invalid index
            config.ActiveTemplateIndex = 999;
            
            // Run validation
            var validationService = new ConfigurationValidationService(plugin);
            var result = validationService.SafeValidateAndFixConfiguration();
            
            // Check that index is now valid
            if (config.ActiveTemplateIndex >= config.MessageTemplates.Count)
                throw new Exception("ActiveTemplateIndex should be valid after validation");
                
            Console.WriteLine("✓ Invalid template indices test passed");
        }
        finally
        {
            // Restore original values
            config.ActiveTemplateIndex = originalIndex;
            config.MessageTemplates.Clear();
            config.MessageTemplates.AddRange(originalTemplates);
        }
    }
}
