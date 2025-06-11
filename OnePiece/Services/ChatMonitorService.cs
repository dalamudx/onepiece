using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

using OnePiece.Helpers;
using OnePiece.Models;
using OnePiece.Localization;
using ECommons.Automation;

namespace OnePiece.Services;

/// <summary>
/// Service for monitoring chat channels and extracting coordinates.
/// </summary>
public class ChatMonitorService : IDisposable
{
    private readonly Plugin plugin;
    private bool isMonitoring;
    private bool isImportingCoordinates = true; // Flag to control coordinate import

    // Regular expression to match player names in various chat formats:
    // [14:27][CWLS1]<Tataru T.> - with CrossWorldLinkShell channel
    // [14:27][1]<Tataru T.> - with LinkShell channel
    // [16:30](Tataru Taru) - with Party channel
    // Named groups: time, channel (optional), player1 (angle brackets), player2 (parentheses)
    private static readonly Regex PlayerNameRegex = new(@"\[(?<time>\d{1,2}:\d{2})\](?:\[(?<channel>[^\]]+)\])?(?:<(?<player1>[^>]+)>|\((?<player2>[^)]+)\))", RegexOptions.IgnoreCase);

    /// <summary>
    /// Event raised when a coordinate is detected in chat.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateDetected;

    /// <summary>
    /// Gets whether the service is currently monitoring chat.
    /// </summary>
    public bool IsMonitoring => isMonitoring;

    /// <summary>
    /// Gets whether the service is currently importing coordinates from chat.
    /// </summary>
    public bool IsImportingCoordinates => isImportingCoordinates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMonitorService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public ChatMonitorService(Plugin plugin)
    {
        this.plugin = plugin;
        this.isMonitoring = false;

        // Subscribe to TreasureHuntService events
        plugin.TreasureHuntService.OnRouteOptimized += OnRouteOptimized;
        plugin.TreasureHuntService.OnRouteOptimizationReset += OnRouteOptimizationReset;

        // Start monitoring if enabled in configuration
        if (plugin.Configuration.EnableChatMonitoring)
        {
            StartMonitoring();
        }
    }

    /// <summary>
    /// Handles the RouteOptimized event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="count">The count of coordinates in the optimized route.</param>
    private void OnRouteOptimized(object? sender, int count)
    {
        // Pause coordinate importing when route is optimized to prevent new coordinates from being added
        isImportingCoordinates = false;

        // Log the optimization completion for debugging purposes
        Plugin.Log.Information($"Route optimization completed with {count} coordinates. Coordinate importing paused.");
    }

    /// <summary>
    /// Handles the RouteOptimizationReset event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnRouteOptimizationReset(object? sender, EventArgs e)
    {
        // Resume coordinate importing when route optimization is reset
        isImportingCoordinates = true;

        Plugin.Log.Information("Route optimization reset. Coordinate importing resumed.");
    }

    /// <summary>
    /// Starts monitoring the specified chat channel.
    /// </summary>
    public void StartMonitoring()
    {
        if (isMonitoring)
            return;

        Plugin.ChatGui.ChatMessage += OnChatMessage;
        isMonitoring = true;

        // Only enable coordinate importing if route is not optimized
        if (!plugin.TreasureHuntService.IsRouteOptimized)
        {
            isImportingCoordinates = true;
        }
    }

    /// <summary>
    /// Stops monitoring chat.
    /// </summary>
    public void StopMonitoring()
    {
        if (!isMonitoring)
            return;

        Plugin.ChatGui.ChatMessage -= OnChatMessage;
        isMonitoring = false;

        // Stop coordinate importing when monitoring stops
        isImportingCoordinates = false;
    }

    /// <summary>
    /// Gets the XivChatType for the configured chat channel.
    /// </summary>
    /// <returns>The XivChatType for the configured chat channel.</returns>
    public XivChatType GetConfiguredChatType()
    {
        return plugin.Configuration.MonitoredChatChannel switch
        {
            ChatChannelType.Say => XivChatType.Say,
            ChatChannelType.Yell => XivChatType.Yell,
            ChatChannelType.Shout => XivChatType.Shout,
            ChatChannelType.Party => XivChatType.Party,
            ChatChannelType.Alliance => XivChatType.Alliance,
            ChatChannelType.FreeCompany => XivChatType.FreeCompany,
            ChatChannelType.LinkShell1 => XivChatType.Ls1,
            ChatChannelType.LinkShell2 => XivChatType.Ls2,
            ChatChannelType.LinkShell3 => XivChatType.Ls3,
            ChatChannelType.LinkShell4 => XivChatType.Ls4,
            ChatChannelType.LinkShell5 => XivChatType.Ls5,
            ChatChannelType.LinkShell6 => XivChatType.Ls6,
            ChatChannelType.LinkShell7 => XivChatType.Ls7,
            ChatChannelType.LinkShell8 => XivChatType.Ls8,
            ChatChannelType.CrossWorldLinkShell1 => XivChatType.CrossLinkShell1,
            ChatChannelType.CrossWorldLinkShell2 => XivChatType.CrossLinkShell2,
            ChatChannelType.CrossWorldLinkShell3 => XivChatType.CrossLinkShell3,
            ChatChannelType.CrossWorldLinkShell4 => XivChatType.CrossLinkShell4,
            ChatChannelType.CrossWorldLinkShell5 => XivChatType.CrossLinkShell5,
            ChatChannelType.CrossWorldLinkShell6 => XivChatType.CrossLinkShell6,
            ChatChannelType.CrossWorldLinkShell7 => XivChatType.CrossLinkShell7,
            ChatChannelType.CrossWorldLinkShell8 => XivChatType.CrossLinkShell8,
            _ => XivChatType.Party
        };
    }



    /// <summary>
    /// Gets the name of a chat channel.
    /// </summary>
    /// <param name="channelType">The chat channel type.</param>
    /// <returns>The name of the chat channel.</returns>
    private string GetChatChannelName(ChatChannelType channelType)
    {
        return channelType switch
        {
            ChatChannelType.Say => Strings.ChatChannels.Say,
            ChatChannelType.Yell => Strings.ChatChannels.Yell,
            ChatChannelType.Shout => Strings.ChatChannels.Shout,
            ChatChannelType.Party => Strings.ChatChannels.Party,
            ChatChannelType.Alliance => Strings.ChatChannels.Alliance,
            ChatChannelType.FreeCompany => Strings.ChatChannels.FreeCompany,
            ChatChannelType.LinkShell1 => Strings.ChatChannels.LinkShell1,
            ChatChannelType.LinkShell2 => Strings.ChatChannels.LinkShell2,
            ChatChannelType.LinkShell3 => Strings.ChatChannels.LinkShell3,
            ChatChannelType.LinkShell4 => Strings.ChatChannels.LinkShell4,
            ChatChannelType.LinkShell5 => Strings.ChatChannels.LinkShell5,
            ChatChannelType.LinkShell6 => Strings.ChatChannels.LinkShell6,
            ChatChannelType.LinkShell7 => Strings.ChatChannels.LinkShell7,
            ChatChannelType.LinkShell8 => Strings.ChatChannels.LinkShell8,
            ChatChannelType.CrossWorldLinkShell1 => Strings.ChatChannels.CrossWorldLinkShell1,
            ChatChannelType.CrossWorldLinkShell2 => Strings.ChatChannels.CrossWorldLinkShell2,
            ChatChannelType.CrossWorldLinkShell3 => Strings.ChatChannels.CrossWorldLinkShell3,
            ChatChannelType.CrossWorldLinkShell4 => Strings.ChatChannels.CrossWorldLinkShell4,
            ChatChannelType.CrossWorldLinkShell5 => Strings.ChatChannels.CrossWorldLinkShell5,
            ChatChannelType.CrossWorldLinkShell6 => Strings.ChatChannels.CrossWorldLinkShell6,
            ChatChannelType.CrossWorldLinkShell7 => Strings.ChatChannels.CrossWorldLinkShell7,
            ChatChannelType.CrossWorldLinkShell8 => Strings.ChatChannels.CrossWorldLinkShell8,
            _ => Strings.ChatChannels.Party
        };
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        try
        {
            // Check if this is the channel we're monitoring
            if (type != GetConfiguredChatType())
                return;

            // Extract player information from sender
            string playerName = ExtractPlayerName(sender);

            // Extract the message text
            string messageText = message.TextValue;

            // Look for coordinates in the message
            ExtractCoordinates(messageText, playerName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error processing chat message: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts the player name from a SeString, removing any special characters like party numbers.
    /// </summary>
    /// <param name="sender">The sender SeString.</param>
    /// <returns>The player name without special characters.</returns>
    private string ExtractPlayerName(SeString sender)
    {
        string playerName = "";
        
        // Try to extract player information from PlayerPayload
        foreach (var payload in sender.Payloads)
        {
            if (payload is PlayerPayload playerPayload)
            {
                playerName = playerPayload.PlayerName;
                break;
            }
        }

        // Fallback to text value if no PlayerPayload is found
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = sender.TextValue;
        }
        
        // Remove special characters from the player name
        return plugin.PlayerNameProcessingService.ProcessPlayerName(playerName);
    }
    
    /// <summary>
    /// Removes special characters from a message text
    /// </summary>
    /// <param name="messageText">The message text that might contain special characters</param>
    /// <returns>The message with special characters removed</returns>
    private string RemoveSpecialCharactersFromMessage(string messageText)
    {
        if (string.IsNullOrEmpty(messageText))
            return messageText;
            
        // Use the same method as for player names, since we're looking for the same special characters
        return plugin.PlayerNameProcessingService.ProcessPlayerName(messageText);
    }
    




/// <summary>
/// Extracts coordinates from a message, handling any special characters.
/// </summary>
/// <param name="messageText">The message text.</param>
/// <param name="playerName">The player name.</param>
private void ExtractCoordinates(string messageText, string playerName)
{
    // Check if we should import coordinates
    if (!isImportingCoordinates)
    {
        Plugin.Log.Debug("Coordinate import is paused. Ignoring coordinates from chat.");
        return;
    }
    
    // Clean message text from special characters that might interfere with coordinate extraction
    // This is important for party chat where messages may contain BoxedNumber characters
    string cleanedText = RemoveSpecialCharactersFromMessage(messageText);

    // Get language-specific coordinate regex
    var coordinateRegex = GetCoordinateRegexForCurrentLanguage();

    Plugin.Log.Debug($"Chat monitoring: Processing message from {playerName}");
    var matches = coordinateRegex.Matches(cleanedText);
    Plugin.Log.Debug($"Found {matches.Count} coordinate matches");
    foreach (Match match in matches)
    {
        if (match.Groups.Count >= 4 &&
            float.TryParse(match.Groups[2].Value, out var x) &&
            float.TryParse(match.Groups[3].Value, out var y))
        {
            // Extract map area (if present)
            string mapArea = match.Groups[1].Success ? match.Groups[1].Value.Trim() : string.Empty;

            // Validate map area using English translation if needed, but keep original for display
            if (!string.IsNullOrEmpty(mapArea))
            {
                var (isValid, englishMapArea, originalMapArea) = MapAreaHelper.TranslateAndValidateMapArea(
                    mapArea,
                    plugin.MapAreaTranslationService,
                    plugin.AetheryteService,
                    "- chat monitoring");

                if (!isValid)
                {
                    Plugin.Log.Warning($"Chat monitoring: Skipping coordinate due to invalid map area");
                    return; // Skip this coordinate
                }
            }

            // Use the cleaned player name to create the coordinate with original map area name
            var coordinate = new TreasureCoordinate(x, y, mapArea, CoordinateSystemType.Map, playerName);

            Plugin.Log.Information($"Coordinate detected from {playerName}: {mapArea} ({x}, {y})");

            // Directly add the coordinate to preserve player name instead of re-importing
            plugin.TreasureHuntService.AddCoordinate(coordinate);

            // Raise the event
            OnCoordinateDetected?.Invoke(this, coordinate);
        }
    }
}

/// <summary>
/// Manually process a chat message to extract coordinates.
/// </summary>
/// <param name="playerName">The name of the player who sent the message.</param>
/// <param name="message">The message text.</param>
/// <returns>True if coordinates were found and processed, false otherwise.</returns>
public bool ProcessChatMessage(string playerName, string message)
{
    try
    {
        bool foundAnyCoordinates = false;
        
        // Break the message into segments, since it might contain multiple coordinates
        string[] segments = SplitMessageIntoSegments(message);
        
        foreach (string segment in segments)
        {
            // Try to extract player name from the segment
            string extractedPlayerName = ExtractPlayerNameFromSegment(segment);
            
            // Use extracted player name if available, otherwise use the provided one
            string effectivePlayerName = !string.IsNullOrEmpty(extractedPlayerName) ? extractedPlayerName : playerName;
            
            // Look for coordinates in the segment using language-specific regex
            var coordinateRegex = GetCoordinateRegexForCurrentLanguage();
            var matches = coordinateRegex.Matches(segment);
            Plugin.Log.Debug($"Manual chat processing: Found {matches.Count} matches in segment");
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4 &&
                    float.TryParse(match.Groups[2].Value, out var x) &&
                    float.TryParse(match.Groups[3].Value, out var y))
                {
                    // Extract map area (if present)
                    string mapArea = match.Groups[1].Success ? match.Groups[1].Value.Trim() : string.Empty;

                    // Remove player name from map area if it was incorrectly captured
                    if (!string.IsNullOrEmpty(effectivePlayerName) && !string.IsNullOrEmpty(mapArea))
                    {
                        mapArea = RemovePlayerNameFromMapArea(mapArea, effectivePlayerName);
                    }

                    // Validate map area using English translation if needed, but keep original for display
                    if (!string.IsNullOrEmpty(mapArea))
                    {
                        var (isValid, englishMapArea, originalMapArea) = MapAreaHelper.TranslateAndValidateMapArea(
                            mapArea,
                            plugin.MapAreaTranslationService,
                            plugin.AetheryteService,
                            "- manual chat processing");

                        if (!isValid)
                        {
                            Plugin.Log.Warning($"Manual chat processing: Skipping coordinate due to invalid map area");
                            continue; // Skip this coordinate
                        }
                    }

                    // Create coordinate with original map area name for display
                    var coordinate = new TreasureCoordinate(x, y, mapArea, CoordinateSystemType.Map, effectivePlayerName);

                    Plugin.Log.Information($"Coordinate detected from {effectivePlayerName}: {mapArea} ({x}, {y})");

                    // Directly add the coordinate to preserve player name instead of re-importing
                    plugin.TreasureHuntService.AddCoordinate(coordinate);

                    // Raise the event
                    OnCoordinateDetected?.Invoke(this, coordinate);
                    
                    foundAnyCoordinates = true;
                }
            }
        }
        
        return foundAnyCoordinates;
    }
    catch (Exception ex)
    {
        Plugin.Log.Error($"Error processing chat message: {ex.Message}");
        return false;
    }
}

/// <summary>
/// Splits a message into segments that might contain individual coordinates.
/// </summary>
/// <param name="message">The message to split.</param>
/// <returns>An array of message segments.</returns>
private string[] SplitMessageIntoSegments(string message)
{
    // Try to split the message at various chat format patterns:
    // [14:27][CWLS1]<Tataru T.> - with CrossWorldLinkShell channel
    // [14:27][1]<Tataru T.> - with LinkShell channel
    // [16:30](Tataru Taru) - with Party channel
    var chatSegments = Regex.Split(message, @"(?=\[\d{1,2}:\d{2}\](?:\[[^\]]+\])?(?:<[^>]+>|\([^)]+\)))");

    // If no new format found, try legacy timestamp splitting
    if (chatSegments.Length <= 1)
    {
        chatSegments = Regex.Split(message, @"(?=\[\d+:\d+\])");
    }

    // If still no segments found, return the whole message
    if (chatSegments.Length <= 1)
    {
        return new[] { message };
    }

    // Filter out empty segments
    return chatSegments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
}

/// <summary>
/// Extracts player name from a message segment.
/// </summary>
/// <param name="segment">The message segment.</param>
/// <returns>The extracted player name, or empty string if none found.</returns>
private string ExtractPlayerNameFromSegment(string segment)
{
    var match = PlayerNameRegex.Match(segment);
    if (match.Success)
    {
        // Extract player name from either capture group (angle brackets or parentheses)
        string playerName = match.Groups["player1"].Success ? match.Groups["player1"].Value : match.Groups["player2"].Value;
        return plugin.PlayerNameProcessingService.ProcessPlayerName(playerName.Trim());
    }
    return string.Empty;
}

/// <summary>
/// Removes player name from map area if it was incorrectly captured.
/// </summary>
/// <param name="mapArea">The map area string.</param>
/// <param name="playerName">The player name to remove.</param>
/// <returns>The cleaned map area string.</returns>
private string RemovePlayerNameFromMapArea(string mapArea, string playerName)
{
    // If the map area starts with the player name, remove it
    if (mapArea.StartsWith(playerName, StringComparison.OrdinalIgnoreCase))
    {
        mapArea = mapArea.Substring(playerName.Length).Trim();
    }
    
    return mapArea;
}

    /// <summary>
    /// Prepares map coordinates for use with MapLinkPayload.
    /// </summary>
    /// <param name="pos">The map coordinate (1-42 range).</param>
    /// <returns>The coordinate value for MapLinkPayload.</returns>
    private static float ConvertMapCoordinateToRawPosition(float pos)
    {
        // it appears that MapLinkPayload expects the raw coordinate values
        // without any conversion.
        return pos;
    }

    /// <summary>
    /// Creates a map link for a coordinate.
    /// </summary>
    /// <param name="coordinate">The coordinate to create a link for.</param>
    /// <returns>The map link payload, or null if it couldn't be created.</returns>
    public MapLinkPayload? CreateMapLink(TreasureCoordinate coordinate)
    {
        try
        {
            if (string.IsNullOrEmpty(coordinate.MapArea))
            {
                Plugin.Log.Error("Cannot create map link: Map area is empty");
                return null;
            }

            // Try to get territory details from TerritoryManager
            var territoryDetail = plugin.TerritoryManager.GetByZoneName(coordinate.MapArea);
            if (territoryDetail == null)
            {
                Plugin.Log.Error($"Cannot create map link: Map area '{coordinate.MapArea}' not found");
                return null;
            }

            // Create map link using territory details
            Plugin.Log.Information($"Creating map link for {coordinate.MapArea} ({coordinate.X}, {coordinate.Y}): " +
                           $"TerritoryId={territoryDetail.TerritoryId}, MapId={territoryDetail.MapId}, SizeFactor={territoryDetail.SizeFactor}");

            return new MapLinkPayload(
                territoryDetail.TerritoryId,
                territoryDetail.MapId,
                ConvertMapCoordinateToRawPosition(coordinate.X),
                ConvertMapCoordinateToRawPosition(coordinate.Y)
            );
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error creating map link: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sends a coordinate to the currently monitored chat channel.
    /// </summary>
    /// <param name="coordinate">The coordinate to send.</param>
    public void SendCoordinateToChat(TreasureCoordinate coordinate)
    {
        try
        {
            // Get the monitored chat channel name for logging
            string channelName = GetChatChannelName(plugin.Configuration.MonitoredChatChannel);

            // Create a map link for the coordinate
            MapLinkPayload? mapLink = CreateMapLink(coordinate);

            // Get the chat command for the monitored channel
            string chatCommand = GetChatCommand(plugin.Configuration.MonitoredChatChannel);
            
            try
            {
                // Make sure we have a valid map link
                if (mapLink != null)
                {
                    // Open the map with the marker
                    Plugin.GameGui.OpenMapWithMapLink(mapLink);
                    
                    // Check if we have an active template
                    bool hasActiveTemplate = plugin.Configuration.ActiveTemplateIndex >= 0 && 
                                          plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count;
                    
                    if (hasActiveTemplate)
                    {
                        // Get the active template
                        var activeTemplate = plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex];
                        
                        // Use the components from the active template instead of the selected components
                        List<MessageComponent> originalComponents = plugin.Configuration.SelectedMessageComponents;
                        plugin.Configuration.SelectedMessageComponents = activeTemplate.Components;
                        
                        // Build the custom message with <flag> integrated
                        string customMessage = BuildCustomMessage(coordinate, true);
                        
                        // Restore the original selected components
                        plugin.Configuration.SelectedMessageComponents = originalComponents;
                        
                        // Send the custom message with flag
                        string customCommand = $"{chatCommand} {customMessage}";
                        Chat.ExecuteCommand(customCommand);
                        
                        Plugin.Log.Information($"Successfully sent message using template '{activeTemplate.Name}' to {channelName}: {customMessage}");
                    }
                    else
                    {
                        // If no active template, just send the flag
                        string mapLinkCommand = $"{chatCommand} <flag>";
                        Chat.ExecuteCommand(mapLinkCommand);
                        Plugin.Log.Information($"Successfully sent map link to {channelName} (no active template)");
                    }
                    
                    return;
                }
                else
                {
                    // If we couldn't create a map link, fall back to text-only message
                    bool hasCustomMessageComponents = plugin.Configuration.SelectedMessageComponents.Count > 0;
                    
                    if (hasCustomMessageComponents)
                    {
                        // Build the custom message with text coordinates
                        string customMessage = BuildCustomMessage(coordinate, false);
                        string customCommand = $"{chatCommand} {customMessage}";
                        Chat.ExecuteCommand(customCommand);
                        Plugin.Log.Information($"Successfully sent custom message with text coordinates to {channelName}: {customMessage}");
                    }
                    else
                    {
                        // Just send text coordinates
                        string mapName = !string.IsNullOrEmpty(coordinate.MapArea) ? coordinate.MapArea : "";
                        string coordText = string.IsNullOrEmpty(mapName)
                            ? $"({coordinate.X:F1}, {coordinate.Y:F1})"
                            : $"{mapName} ({coordinate.X:F1}, {coordinate.Y:F1})";

                        string fullCommand = $"{chatCommand} {coordText}";
                        Chat.ExecuteCommand(fullCommand);
                        Plugin.Log.Information($"Successfully sent coordinate text to {channelName}: {coordText}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error sending coordinate to chat: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error sending coordinate to chat: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds a custom message based on the selected message components.
    /// </summary>
    /// <param name="coordinate">The coordinate to include in the message.</param>
    /// <param name="useFlag">Whether to use <flag> for coordinates or text representation.</param>
    /// <returns>The built custom message.</returns>
    private string BuildCustomMessage(TreasureCoordinate coordinate, bool useFlag)
    {
        // If no components are selected, return an empty string
        if (plugin.Configuration.SelectedMessageComponents.Count == 0)
        {
            return string.Empty;
        }
        
        var messageParts = new List<string>();
        bool hasCoordinates = false;
        
        // First pass: build all non-coordinate parts and check if coordinates are included
        foreach (var component in plugin.Configuration.SelectedMessageComponents)
        {
            if (component.Type == MessageComponentType.Coordinates)
            {
                hasCoordinates = true;
                continue; // Skip coordinates for now, we'll handle them specially
            }
            
            switch (component.Type)
            {
                case MessageComponentType.PlayerName:
                    if (!string.IsNullOrEmpty(coordinate.PlayerName))
                    {
                        messageParts.Add(coordinate.PlayerName);
                    }
                    break;
                    
                case MessageComponentType.Number:
                    // Get index from coordinate or active route
                    int index = GetCoordinateIndex(coordinate);
                    if (GameIconHelper.IsValidIndexForComponent(MessageComponentType.Number, index))
                    {
                        // Use GameIconHelper for consistent icon generation based on Dalamud's SeIconChar
                        string iconChar = GameIconHelper.GetNumberIcon(index);
                        if (!string.IsNullOrEmpty(iconChar))
                        {
                            messageParts.Add(iconChar);
                        }
                    }
                    break;

                case MessageComponentType.BoxedNumber:
                    // Get index from coordinate or active route
                    int boxedIndex = GetCoordinateIndex(coordinate);
                    if (GameIconHelper.IsValidIndexForComponent(MessageComponentType.BoxedNumber, boxedIndex))
                    {
                        // Use GameIconHelper for consistent icon generation based on Dalamud's SeIconChar
                        string iconChar = GameIconHelper.GetBoxedNumberIcon(boxedIndex);
                        if (!string.IsNullOrEmpty(iconChar))
                        {
                            messageParts.Add(iconChar);
                        }
                    }
                    break;

                case MessageComponentType.BoxedOutlinedNumber:
                    // Get index from coordinate or active route
                    int boxedOutlinedIndex = GetCoordinateIndex(coordinate);
                    if (GameIconHelper.IsValidIndexForComponent(MessageComponentType.BoxedOutlinedNumber, boxedOutlinedIndex))
                    {
                        // Use GameIconHelper for consistent icon generation based on Dalamud's SeIconChar
                        string iconChar = GameIconHelper.GetBoxedOutlinedNumberIcon(boxedOutlinedIndex);
                        if (!string.IsNullOrEmpty(iconChar))
                        {
                            messageParts.Add(iconChar);
                        }
                    }
                    break;

                case MessageComponentType.CustomMessage:
                    if (component.CustomMessageIndex >= 0 && component.CustomMessageIndex < plugin.Configuration.CustomMessages.Count)
                    {
                        messageParts.Add(plugin.Configuration.CustomMessages[component.CustomMessageIndex]);
                    }
                    break;
            }
        }
        
        // Now handle coordinates
        if (hasCoordinates)
        {
            if (useFlag)
            {
                // Insert <flag> at the appropriate position based on the order in SelectedMessageComponents
                int flagPosition = 0;
                for (int i = 0; i < plugin.Configuration.SelectedMessageComponents.Count; i++)
                {
                    var component = plugin.Configuration.SelectedMessageComponents[i];
                    if (component.Type == MessageComponentType.Coordinates)
                    {
                        // Found the coordinates component, insert <flag> at this position
                        messageParts.Insert(flagPosition, "<flag>");
                        break;
                    }
                    
                    // If this component was added to messageParts, increment the position
                    if ((component.Type == MessageComponentType.PlayerName && !string.IsNullOrEmpty(coordinate.PlayerName)) ||
                        (component.Type == MessageComponentType.Number && GameIconHelper.IsValidIndexForComponent(component.Type, GetCoordinateIndex(coordinate))) ||
                        (component.Type == MessageComponentType.BoxedNumber && GameIconHelper.IsValidIndexForComponent(component.Type, GetCoordinateIndex(coordinate))) ||
                        (component.Type == MessageComponentType.BoxedOutlinedNumber && GameIconHelper.IsValidIndexForComponent(component.Type, GetCoordinateIndex(coordinate))) ||
                        (component.Type == MessageComponentType.CustomMessage &&
                         component.CustomMessageIndex >= 0 &&
                         component.CustomMessageIndex < plugin.Configuration.CustomMessages.Count))
                    {
                        flagPosition++;
                    }
                }
            }
            else
            {
                // Use text representation of coordinates
                string mapName = !string.IsNullOrEmpty(coordinate.MapArea) ? coordinate.MapArea : "";
                string coordText = string.IsNullOrEmpty(mapName)
                    ? $"({coordinate.X:F1}, {coordinate.Y:F1})"
                    : $"{mapName} ({coordinate.X:F1}, {coordinate.Y:F1})";
                
                // Insert the text coordinates at the appropriate position
                int coordPosition = 0;
                for (int i = 0; i < plugin.Configuration.SelectedMessageComponents.Count; i++)
                {
                    var component = plugin.Configuration.SelectedMessageComponents[i];
                    if (component.Type == MessageComponentType.Coordinates)
                    {
                        // Found the coordinates component, insert the text at this position
                        messageParts.Insert(coordPosition, coordText);
                        break;
                    }
                    
                    // If this component was added to messageParts, increment the position
                    if ((component.Type == MessageComponentType.PlayerName && !string.IsNullOrEmpty(coordinate.PlayerName)) ||
                        (component.Type == MessageComponentType.Number && GameIconHelper.IsValidIndexForComponent(component.Type, GetCoordinateIndex(coordinate))) ||
                        (component.Type == MessageComponentType.BoxedNumber && GameIconHelper.IsValidIndexForComponent(component.Type, GetCoordinateIndex(coordinate))) ||
                        (component.Type == MessageComponentType.BoxedOutlinedNumber && GameIconHelper.IsValidIndexForComponent(component.Type, GetCoordinateIndex(coordinate))) ||
                        (component.Type == MessageComponentType.CustomMessage &&
                         component.CustomMessageIndex >= 0 &&
                         component.CustomMessageIndex < plugin.Configuration.CustomMessages.Count))
                    {
                        coordPosition++;
                    }
                }
            }
        }
        
        return string.Join(" ", messageParts);
    }

    /// <summary>
    /// Gets the index of a coordinate in the active route, or determines an appropriate index.
    /// </summary>
    /// <param name="coordinate">The coordinate to find the index for.</param>
    /// <returns>The index of the coordinate (0-7) or -1 if not found.</returns>
    private int GetCoordinateIndex(TreasureCoordinate coordinate)
    {
        // First check if this coordinate is in the optimized route
        var route = plugin.TreasureHuntService.OptimizedRoute;
        if (route != null && route.Count > 0)
        {
            // PRIORITY 1: Try to find exact reference match first
            for (int i = 0; i < route.Count; i++)
            {
                var routeCoord = route[i];
                if (ReferenceEquals(routeCoord, coordinate))
                {
                    return Math.Min(i, 7); // Use route position directly as display index
                }
            }

            // PRIORITY 2: If no exact reference match, find coordinate by position and use a unique identifier
            // For duplicate coordinates, we need to distinguish them somehow
            var matchingCoordinates = new List<(int index, TreasureCoordinate coord)>();

            for (int i = 0; i < route.Count; i++)
            {
                var routeCoord = route[i];

                // Check coordinates within tolerance
                if (Math.Abs(routeCoord.X - coordinate.X) < 0.1f &&
                    Math.Abs(routeCoord.Y - coordinate.Y) < 0.1f &&
                    string.Equals(routeCoord.MapArea, coordinate.MapArea, StringComparison.OrdinalIgnoreCase))
                {
                    matchingCoordinates.Add((i, routeCoord));
                }
            }

            if (matchingCoordinates.Count == 1)
            {
                // Only one match, use it
                var match = matchingCoordinates[0];
                return Math.Min(match.index, 7);
            }
            else if (matchingCoordinates.Count > 1)
            {
                // Multiple matches - try to distinguish by additional properties
                foreach (var (index, routeCoord) in matchingCoordinates)
                {
                    // Try to match by Type and other properties
                    if (routeCoord.Type == coordinate.Type &&
                        string.Equals(routeCoord.PlayerName, coordinate.PlayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return Math.Min(index, 7);
                    }
                }

                // If still no unique match, use the first one
                var firstMatch = matchingCoordinates[0];
                return Math.Min(firstMatch.index, 7);
            }
        }

        // If we couldn't find the coordinate in the route or there is no active route,
        // use a simple sequential numbering based on all coordinates
        var allCoordinates = plugin.TreasureHuntService.Coordinates;

        // Find the position of this coordinate in the all coordinates list
        for (int i = 0; i < allCoordinates.Count && i < 8; i++)
        {
            var coord = allCoordinates[i];
            if (ReferenceEquals(coord, coordinate))
            {
                return i;
            }
        }

        // Final fallback with coordinate matching
        for (int i = 0; i < allCoordinates.Count && i < 8; i++)
        {
            var coord = allCoordinates[i];
            if (Math.Abs(coord.X - coordinate.X) < 0.1f &&
                Math.Abs(coord.Y - coordinate.Y) < 0.1f &&
                string.Equals(coord.MapArea, coordinate.MapArea, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        // Fallback: return the count modulo 8 to ensure we stay within valid range
        int fallbackIndex = allCoordinates.Count % 8;
        return fallbackIndex;
    }
    
    /// <summary>
    /// Gets the chat command for a specific chat channel.
    /// </summary>
    /// <param name="channelType">The chat channel type.</param>
    /// <returns>The chat command for the specified channel.</returns>
    private string GetChatCommand(ChatChannelType channelType)
    {
        return channelType switch
        {
            ChatChannelType.Say => "/say",
            ChatChannelType.Yell => "/yell",
            ChatChannelType.Shout => "/shout",
            ChatChannelType.Party => "/p",
            ChatChannelType.Alliance => "/alliance",
            ChatChannelType.FreeCompany => "/fc",
            ChatChannelType.LinkShell1 => "/linkshell1",
            ChatChannelType.LinkShell2 => "/linkshell2",
            ChatChannelType.LinkShell3 => "/linkshell3",
            ChatChannelType.LinkShell4 => "/linkshell4",
            ChatChannelType.LinkShell5 => "/linkshell5",
            ChatChannelType.LinkShell6 => "/linkshell6",
            ChatChannelType.LinkShell7 => "/linkshell7",
            ChatChannelType.LinkShell8 => "/linkshell8",
            ChatChannelType.CrossWorldLinkShell1 => "/cwlinkshell1",
            ChatChannelType.CrossWorldLinkShell2 => "/cwlinkshell2",
            ChatChannelType.CrossWorldLinkShell3 => "/cwlinkshell3",
            ChatChannelType.CrossWorldLinkShell4 => "/cwlinkshell4",
            ChatChannelType.CrossWorldLinkShell5 => "/cwlinkshell5",
            ChatChannelType.CrossWorldLinkShell6 => "/cwlinkshell6",
            ChatChannelType.CrossWorldLinkShell7 => "/cwlinkshell7",
            ChatChannelType.CrossWorldLinkShell8 => "/cwlinkshell8",
            _ => "/p"
        };
    }


    
    /// <summary>
    /// Gets the appropriate coordinate regex based on the current game client language.
    /// </summary>
    /// <returns>A regex pattern optimized for the current language.</returns>
    private Regex GetCoordinateRegexForCurrentLanguage()
    {
        var currentLanguage = Svc.ClientState.ClientLanguage;
        Plugin.Log.Information($"Creating coordinate regex for chat monitoring, language: {currentLanguage}");

        var regex = currentLanguage switch
        {
            ClientLanguage.Japanese => GetJapaneseCoordinateRegex(),
            ClientLanguage.German => GetGermanCoordinateRegex(),
            ClientLanguage.French => GetFrenchCoordinateRegex(),
            _ => GetEnglishCoordinateRegex() // Default to English for English and any other languages
        };

        Plugin.Log.Information($"Selected chat monitoring regex pattern: {regex}");
        return regex;
    }

    /// <summary>
    /// Gets regex pattern optimized for English map area names.
    /// </summary>
    private Regex GetEnglishCoordinateRegex()
    {
        // English pattern: supports ASCII letters, numbers, spaces, apostrophes, hyphens
        // Examples: "Heritage Found (15.0, 20.5)", "Ul'dah - Steps of Nald (8.2, 7.8)"
        // Made optional map area with ? to handle coordinates without map names
        return new Regex(@"(?:([A-Za-z0-9\s''\-–—]+?)\s*)?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets regex pattern optimized for Japanese map area names.
    /// </summary>
    private Regex GetJapaneseCoordinateRegex()
    {
        // Japanese pattern: supports all Unicode letters and common punctuation used in Japanese
        // This includes Hiragana, Katakana, Kanji, ASCII letters, numbers, spaces, and Japanese punctuation
        // Examples: "ヘリテージファウンド (16.0, 21.3)", "リムサ・ロミンサ：下甲板層 (9.5, 11.2)"
        // Made optional map area with ? to handle coordinates without map names
        return new Regex(@"(?:([\p{L}\p{N}\s''\-–—：・]+?)\s*)?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets regex pattern optimized for German map area names.
    /// </summary>
    private Regex GetGermanCoordinateRegex()
    {
        // German pattern: supports all Unicode letters, numbers, spaces, and common punctuation
        // This includes German umlauts and other special characters
        // Examples: "Östliche Noscea (21.0, 21.0)", "Mor Dhona (22.2, 7.9)"
        // Made optional map area with ? to handle coordinates without map names
        return new Regex(@"(?:([\p{L}\p{N}\s''\-–—]+?)\s*)?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets regex pattern optimized for French map area names.
    /// </summary>
    private Regex GetFrenchCoordinateRegex()
    {
        // French pattern: supports all Unicode letters, numbers, spaces, and common punctuation
        // This includes French accented characters
        // Examples: "Noscea orientale (21.0, 21.0)", "Mor Dhona (22.2, 7.9)"
        // Made optional map area with ? to handle coordinates without map names
        return new Regex(@"(?:([\p{L}\p{N}\s''\-–—]+?)\s*)?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();

        // Unsubscribe from TreasureHuntService events
        plugin.TreasureHuntService.OnRouteOptimized -= OnRouteOptimized;
        plugin.TreasureHuntService.OnRouteOptimizationReset -= OnRouteOptimizationReset;
    }
}
