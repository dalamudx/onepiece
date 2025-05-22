using Dalamud.Configuration;
using System;

namespace OnePiece;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string Language { get; set; } = "en";
    public LogLevel LogLevel { get; set; } = LogLevel.Normal;

    // Treasure hunt settings
    public bool AutoOptimizeRoute { get; set; } = true;
    
    // Chat monitoring settings
    public bool EnableChatMonitoring { get; set; } = false;
    public ChatChannelType MonitoredChatChannel { get; set; } = ChatChannelType.Party;
    
    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

/// <summary>
/// Represents the type of chat channel to monitor.
/// </summary>
public enum ChatChannelType
{
    Say,
    Yell,
    Shout,
    Party,
    Alliance,
    FreeCompany,
    LinkShell1,
    LinkShell2,
    LinkShell3,
    LinkShell4,
    LinkShell5,
    LinkShell6,
    LinkShell7,
    LinkShell8,
    CrossWorldLinkShell1,
    CrossWorldLinkShell2,
    CrossWorldLinkShell3,
    CrossWorldLinkShell4,
    CrossWorldLinkShell5,
    CrossWorldLinkShell6,
    CrossWorldLinkShell7,
    CrossWorldLinkShell8
}

/// <summary>
/// Represents the level of notifications to display.
/// </summary>
public enum LogLevel
{
    Minimal,   // Only show critical information
    Normal,    // Show normal notifications
    Verbose    // Show all information
}
