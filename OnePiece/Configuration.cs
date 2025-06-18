using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnePiece;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string Language { get; set; } = "en";

    // Chat monitoring settings
    public bool EnableChatMonitoring { get; set; } = false;
    public ChatChannelType MonitoredChatChannel { get; set; } = ChatChannelType.Party;
    
    // Custom message settings
    public List<string> CustomMessages { get; set; } = new List<string>();
    public List<MessageComponent> SelectedMessageComponents { get; set; } = new List<MessageComponent>();

    // Message template settings
    public List<MessageTemplate> MessageTemplates { get; set; } = new List<MessageTemplate>();
    public int ActiveTemplateIndex { get; set; } = -1;
    /// <summary>
    /// Validates and fixes basic configuration issues.
    /// </summary>
    public void ValidateAndFix()
    {
        // Ensure collections are not null
        MessageTemplates ??= new List<MessageTemplate>();
        SelectedMessageComponents ??= new List<MessageComponent>();
        CustomMessages ??= new List<string>();

        // Ensure basic properties have valid values
        Language ??= "en";

        // Fix invalid chat channel
        if (!Enum.IsDefined(typeof(ChatChannelType), MonitoredChatChannel))
        {
            MonitoredChatChannel = ChatChannelType.Party;
        }

        // Fix invalid active template index
        if (ActiveTemplateIndex < -1 || ActiveTemplateIndex >= MessageTemplates.Count)
        {
            ActiveTemplateIndex = -1;
        }

        // Remove null templates and components
        MessageTemplates = MessageTemplates.Where(t => t != null).ToList();
        SelectedMessageComponents = SelectedMessageComponents.Where(c => c != null).ToList();
        CustomMessages = CustomMessages.Where(m => !string.IsNullOrEmpty(m)).ToList();
    }

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
/// Represents a component of the custom message to be sent in chat.
/// </summary>
[Serializable]
public class MessageComponent
{
    /// <summary>
    /// The type of the message component.
    /// </summary>
    public MessageComponentType Type { get; set; }
    
    /// <summary>
    /// The index of the custom message if Type is CustomMessage.
    /// </summary>
    public int CustomMessageIndex { get; set; } = -1;
    
    public MessageComponent() { }
    
    public MessageComponent(MessageComponentType type, int customMessageIndex = -1)
    {
        Type = type;
        CustomMessageIndex = customMessageIndex;
    }
}

/// <summary>
/// Represents the type of a message component.
/// </summary>
public enum MessageComponentType
{
    PlayerName,     // The name of the player who shared the coordinates
    Coordinates,    // The coordinates (including map name if available)
    CustomMessage,  // A custom message defined by the user
    Number,         // Displays a number based on the coordinate index (game icons 1-9, then text)
    BoxedNumber,    // Displays a boxed number based on the coordinate index (game icons 1-31, then bracketed text)
    BoxedOutlinedNumber // Displays a boxed outlined number based on the coordinate index (game icons 1-9, then special bracketed text)
}

/// <summary>
/// Represents a template for custom messages.
/// </summary>
[Serializable]
public class MessageTemplate
{
    /// <summary>
    /// The name of the template.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The list of message components in this template.
    /// </summary>
    public List<MessageComponent> Components { get; set; } = new List<MessageComponent>();
    
    /// <summary>
    /// Creates a new instance of the MessageTemplate class.
    /// </summary>
    public MessageTemplate() { }
    
    /// <summary>
    /// Creates a new instance of the MessageTemplate class with the specified name.
    /// </summary>
    /// <param name="name">The name of the template.</param>
    public MessageTemplate(string name)
    {
        Name = name;
    }
    
    /// <summary>
    /// Creates a deep copy of this template.
    /// </summary>
    /// <returns>A new instance of MessageTemplate with the same values.</returns>
    public MessageTemplate Clone()
    {
        var template = new MessageTemplate(Name);
        foreach (var component in Components)
        {
            template.Components.Add(new MessageComponent(component.Type, component.CustomMessageIndex));
        }
        return template;
    }
}
