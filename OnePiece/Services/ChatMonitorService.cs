using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
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
    private readonly IPluginLog log;
    private bool isMonitoring;
    private bool isImportingCoordinates = true; // Flag to control coordinate import

    // Regular expression to match coordinates with map area in the format "MapName (x, y)" or just "(x, y)"
    // Group 1: Map area (optional)
    // Group 2: X coordinate
    // Group 3: Y coordinate
    private static readonly Regex CoordinateRegex = new(@"(?:([A-Za-z0-9\s']+)?\s*\(?\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)?)", RegexOptions.IgnoreCase);
    
    // Regular expression to match player names in copied chat messages like [21:50](Player Name) Text...
    // Group 1: Timestamp (optional)
    // Group 2: Player name
    private static readonly Regex PlayerNameRegex = new(@"\[\d+:\d+\]\s*\(([^)]+)\)|\(([^)]+)\)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Event raised when a coordinate is detected in chat.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateDetected;

    /// <summary>
    /// Gets whether the service is currently monitoring chat.
    /// </summary>
    public bool IsMonitoring => isMonitoring;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMonitorService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public ChatMonitorService(Plugin plugin)
    {
        this.plugin = plugin;
        this.log = Plugin.Log;
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
        // Continue importing coordinates even after route optimization
        // We've removed the line that sets isImportingCoordinates = false
        log.Information("Route optimized. Coordinate import remains active.");
    }

    /// <summary>
    /// Handles the RouteOptimizationReset event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnRouteOptimizationReset(object? sender, EventArgs e)
    {
        // Resume importing coordinates after route optimization reset
        isImportingCoordinates = true;
        log.Information("Route optimization reset. Coordinate import resumed.");
    }

    /// <summary>
    /// Starts monitoring the specified chat channel.
    /// </summary>
    public void StartMonitoring()
    {
        if (isMonitoring)
            return;

        // Subscribe to chat messages
        Plugin.ChatGui.ChatMessage += OnChatMessage;
        isMonitoring = true;
        log.Information($"Started monitoring chat channel: {plugin.Configuration.MonitoredChatChannel}");

        // Only show notification if log level is Normal or higher
        if (plugin.Configuration.LogLevel >= LogLevel.Normal)
        {
            log.Information($"Started monitoring {GetChatChannelName(plugin.Configuration.MonitoredChatChannel)} for coordinates.");
        }
    }

    /// <summary>
    /// Stops monitoring chat.
    /// </summary>
    public void StopMonitoring()
    {
        if (!isMonitoring)
            return;

        // Unsubscribe from chat messages
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
        isMonitoring = false;
        log.Information("Stopped monitoring chat");

        // Only show notification if log level is Normal or higher
        if (plugin.Configuration.LogLevel >= LogLevel.Normal)
        {
            log.Information("Stopped monitoring chat for coordinates.");
        }
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
            ChatChannelType.Say => Strings.GetString("Say"),
            ChatChannelType.Yell => Strings.GetString("Yell"),
            ChatChannelType.Shout => Strings.GetString("Shout"),
            ChatChannelType.Party => Strings.GetString("Party"),
            ChatChannelType.Alliance => Strings.GetString("Alliance"),
            ChatChannelType.FreeCompany => Strings.GetString("FreeCompany"),
            ChatChannelType.LinkShell1 => Strings.GetString("LinkShell1"),
            ChatChannelType.LinkShell2 => Strings.GetString("LinkShell2"),
            ChatChannelType.LinkShell3 => Strings.GetString("LinkShell3"),
            ChatChannelType.LinkShell4 => Strings.GetString("LinkShell4"),
            ChatChannelType.LinkShell5 => Strings.GetString("LinkShell5"),
            ChatChannelType.LinkShell6 => Strings.GetString("LinkShell6"),
            ChatChannelType.LinkShell7 => Strings.GetString("LinkShell7"),
            ChatChannelType.LinkShell8 => Strings.GetString("LinkShell8"),
            ChatChannelType.CrossWorldLinkShell1 => Strings.GetString("CrossWorldLinkShell1"),
            ChatChannelType.CrossWorldLinkShell2 => Strings.GetString("CrossWorldLinkShell2"),
            ChatChannelType.CrossWorldLinkShell3 => Strings.GetString("CrossWorldLinkShell3"),
            ChatChannelType.CrossWorldLinkShell4 => Strings.GetString("CrossWorldLinkShell4"),
            ChatChannelType.CrossWorldLinkShell5 => Strings.GetString("CrossWorldLinkShell5"),
            ChatChannelType.CrossWorldLinkShell6 => Strings.GetString("CrossWorldLinkShell6"),
            ChatChannelType.CrossWorldLinkShell7 => Strings.GetString("CrossWorldLinkShell7"),
            ChatChannelType.CrossWorldLinkShell8 => Strings.GetString("CrossWorldLinkShell8"),
            _ => Strings.GetString("Party")
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
            log.Error($"Error processing chat message: {ex.Message}");
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
        return RemoveSpecialCharactersFromName(playerName);
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
        return RemoveSpecialCharactersFromName(messageText);
    }
    
    /// <summary>
    /// Removes special characters from player names like BoxedNumber and BoxedOutlinedNumber
    /// </summary>
    /// <param name="name">The name that might contain special characters</param>
    /// <returns>The name with special characters removed</returns>
    private string RemoveSpecialCharactersFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Create a StringBuilder to build the cleaned name
        var cleanedName = new StringBuilder(name.Length);
        
        // Process each character in the name
        foreach (var c in name)
        {   
            // Check for BoxedNumber character range (U+2460 to U+2473)
            // These are ①,②,③, etc.
            if (c >= '①' && c <= '⑳')
                continue;
                
            // Check for BoxedOutlinedNumber character range (U+2776 to U+277F)
            // These are ❶,❷,❸, etc.
            if (c >= '❶' && c <= '❿')
                continue;
            
            // Any other special characters that need to be filtered can be added here
            
            // Add the character to the cleaned name if it passed all filters
            cleanedName.Append(c);
        }
        
        return cleanedName.ToString().Trim();
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
        log.Debug("Coordinate import is paused. Ignoring coordinates from chat.");
        return;
    }
    
    // Clean message text from special characters that might interfere with coordinate extraction
    // This is important for party chat where messages may contain BoxedNumber characters
    string cleanedText = RemoveSpecialCharactersFromMessage(messageText);

    var matches = CoordinateRegex.Matches(cleanedText);
    foreach (Match match in matches)
    {
        if (match.Groups.Count >= 4 &&
            float.TryParse(match.Groups[2].Value, out var x) &&
            float.TryParse(match.Groups[3].Value, out var y))
        {
            // Extract map area (if present)
            string mapArea = match.Groups[1].Success ? match.Groups[1].Value.Trim() : string.Empty;

            // Use the cleaned player name to create the coordinate
            var coordinate = new TreasureCoordinate(x, y, mapArea, CoordinateSystemType.Map, "", playerName);

            log.Information($"Detected coordinate from {playerName}: {mapArea} ({x}, {y})");

            // Directly add the coordinate to preserve player name instead of re-importing
            plugin.TreasureHuntService.AddCoordinate(coordinate);

            // Notify the user based on log level
            if (plugin.Configuration.LogLevel >= LogLevel.Normal)
            {
                log.Information(string.Format(Strings.GetString("CoordinateDetected"),
                    playerName, coordinate));
            }

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
            
            // Look for coordinates in the segment
            var matches = CoordinateRegex.Matches(segment);
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

                    var coordinate = new TreasureCoordinate(x, y, mapArea, CoordinateSystemType.Map, "", effectivePlayerName);

                    log.Information($"Manually processed coordinate from {effectivePlayerName}: {mapArea} ({x}, {y})");

                    // Directly add the coordinate to preserve player name instead of re-importing
                    plugin.TreasureHuntService.AddCoordinate(coordinate);

                    // Notify the user based on log level
                    if (plugin.Configuration.LogLevel >= LogLevel.Normal)
                    {
                        log.Information(string.Format(Strings.GetString("CoordinateDetected"),
                            effectivePlayerName, coordinate));
                    }

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
        log.Error($"Error processing chat message: {ex.Message}");
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
    // Try to split the message at timestamps like [21:50]
    var timestampSegments = Regex.Split(message, @"(?=\[\d+:\d+\])");
    
    // If no timestamps found or only one segment, return the whole message
    if (timestampSegments.Length <= 1)
    {
        return new[] { message };
    }
    
    // Filter out empty segments
    return timestampSegments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
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
        // Group 1 contains the player name if it matched the first pattern ([21:50](Player Name))
        // Group 2 contains the player name if it matched the second pattern ((Player Name))
        string playerName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return RemoveSpecialCharactersFromName(playerName.Trim());
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
        
        // If map area starts with "さん" or other player suffixes, remove them too
        string[] suffixes = new[] { "さん", "san", "の", "no" };
        foreach (var suffix in suffixes)
        {
            if (mapArea.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                mapArea = mapArea.Substring(suffix.Length).Trim();
                break;
            }
        }
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
                log.Error("Cannot create map link: Map area is empty");
                return null;
            }

            // Try to get territory details from TerritoryManager
            var territoryDetail = plugin.TerritoryManager.GetByZoneName(coordinate.MapArea);
            if (territoryDetail == null)
            {
                log.Error($"Cannot create map link: Map area '{coordinate.MapArea}' not found");
                return null;
            }

            // Create map link using territory details
            log.Information($"Creating map link for {coordinate.MapArea} ({coordinate.X}, {coordinate.Y}): " +
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
            log.Error($"Error creating map link: {ex.Message}");
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
                        
                        log.Information($"Successfully sent message using template '{activeTemplate.Name}' to {channelName}: {customMessage}");
                    }
                    else
                    {
                        // If no active template, just send the flag
                        string mapLinkCommand = $"{chatCommand} <flag>";
                        Chat.ExecuteCommand(mapLinkCommand);
                        log.Information($"Successfully sent map link to {channelName} (no active template)");
                    }
                    
                    // Only show notification if log level is Normal or higher
                    if (plugin.Configuration.LogLevel >= LogLevel.Normal)
                    {
                        log.Information($"Successfully sent to {channelName}");
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
                        log.Information($"Successfully sent custom message with text coordinates to {channelName}: {customMessage}");
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
                        log.Information($"Successfully sent coordinate text to {channelName}: {coordText}");
                    }
                }
                
                // Only log if log level is Normal or higher
                if (plugin.Configuration.LogLevel >= LogLevel.Normal)
                {
                    log.Information($"Successfully sent to {channelName}");
                }
            }
            catch (Exception ex)
            {
                // Always log error messages
                log.Error($"Error sending coordinate to chat: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Always log error messages
            log.Error($"Error sending coordinate to chat: {ex.Message}");
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
                    if (index >= 0 && index < 8)
                    {
                        // Insert the Number1-Number8 special characters into chat using direct Unicode values
                        // According to SeIconChar source, Number1 = 0xE061, and so on
                        // This is a more direct approach that inserts the actual character into the text
                        int unicodeValue = 0xE061 + index; // SeIconChar.Number1 + index offset
                        string iconChar = char.ConvertFromUtf32(unicodeValue);
                        messageParts.Add(iconChar);
                    }
                    break;
                    
                case MessageComponentType.BoxedNumber:
                    // Get index from coordinate or active route
                    int boxedIndex = GetCoordinateIndex(coordinate);
                    if (boxedIndex >= 0 && boxedIndex < 8)
                    {
                        // Insert the BoxedNumber1-BoxedNumber8 special characters into chat using direct Unicode values
                        // According to SeIconChar source, BoxedNumber1 = 0xE090, and so on
                        // This is a more direct approach that inserts the actual character into the text
                        int unicodeValue = 0xE090 + boxedIndex; // SeIconChar.BoxedNumber1 + index offset
                        string iconChar = char.ConvertFromUtf32(unicodeValue);
                        messageParts.Add(iconChar);
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
                        (component.Type == MessageComponentType.Number && GetCoordinateIndex(coordinate) >= 0 && GetCoordinateIndex(coordinate) < 8) ||
                        (component.Type == MessageComponentType.BoxedNumber && GetCoordinateIndex(coordinate) >= 0 && GetCoordinateIndex(coordinate) < 8) ||
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
                        (component.Type == MessageComponentType.Number && GetCoordinateIndex(coordinate) >= 0 && GetCoordinateIndex(coordinate) < 8) ||
                        (component.Type == MessageComponentType.BoxedNumber && GetCoordinateIndex(coordinate) >= 0 && GetCoordinateIndex(coordinate) < 8) ||
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
            // Filter out teleport points from the route
            var nonTeleportCoordinates = new List<TreasureCoordinate>();
            foreach (var coord in route)
            {
                // Skip teleport points (teleport point name contains "[Teleport]")
                if (coord.Name == null || !coord.Name.Contains("[Teleport]"))
                {
                    nonTeleportCoordinates.Add(coord);
                }
            }
            
            // Count how many times this coordinate appears before the current instance
            int currentInstance = 0;
            int index = 0;
            
            // First pass: count instances of the same coordinate
            for (int i = 0; i < nonTeleportCoordinates.Count && i < 8; i++)
            {
                var routeCoord = nonTeleportCoordinates[i];
                
                // If this is the same coordinate (within tolerance)
                if (Math.Abs(routeCoord.X - coordinate.X) < 0.1f && Math.Abs(routeCoord.Y - coordinate.Y) < 0.1f)
                {
                    // If this is our coordinate, use the current index
                    if (ReferenceEquals(routeCoord, coordinate))
                    {
                        return index;
                    }
                    // Otherwise increment the instance count for duplicates
                    currentInstance++;
                }
                
                // Always increment the index for each non-teleport coordinate
                index++;
            }
            
            // Second pass: if we didn't find an exact reference match, use the instance count
            index = 0;
            int instanceFound = 0;
            for (int i = 0; i < nonTeleportCoordinates.Count && i < 8; i++)
            {
                var routeCoord = nonTeleportCoordinates[i];
                
                // If this is the same coordinate (within tolerance)
                if (Math.Abs(routeCoord.X - coordinate.X) < 0.1f && Math.Abs(routeCoord.Y - coordinate.Y) < 0.1f)
                {
                    // If this is the instance we're looking for
                    if (instanceFound == currentInstance)
                    {
                        return index;
                    }
                    instanceFound++;
                }
                
                // Always increment the index for each non-teleport coordinate
                index++;
            }

            // If the coordinate was not found in the filtered route, check if it's a teleport point
            if (coordinate.Name != null && coordinate.Name.Contains("[Teleport]"))
            {
                // Don't assign an index to teleport points
                return -1;
            }
        }
        
        // If we couldn't find the coordinate in the route or there is no active route,
        // use the total count of coordinates sent so far as the index
        // This ensures sequential numbering even for duplicate coordinates
        return plugin.TreasureHuntService.Coordinates.Count(c => !c.IsCollected) % 8;
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
