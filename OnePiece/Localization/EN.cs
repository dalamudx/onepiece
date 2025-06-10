namespace OnePiece.Localization;

/// <summary>
/// English localization implementation.
/// </summary>
public class EN : ILocalizationData
{
    // Window and UI Elements
    public string MainWindowTitle => "One Piece";
    public string GeneralSettings => "General Settings";
    public string Language => "Language";
    public string MessageSettings => "Message Settings";
    public string CustomMessageSettings => "Custom Message Settings";

    // Actions and Buttons
    public string Edit => "Edit";
    public string Save => "Save";
    public string Cancel => "Cancel";
    public string Delete => "Delete";
    public string Restore => "Restore";
    public string Create => "Create";
    public string Add => "Add";
    public string Export => "Export";
    public string Import => "Import";
    public string ClearAll => "Clear All";

    // Route Optimization
    public string OptimizeRoute => "Optimize Route";
    public string ResetOptimization => "Reset Optimization";
    public string NoCoordinatesToOptimize => "No coordinates to optimize. Please import coordinates first.";

    // Coordinates and Collection
    public string NoCoordinates => "No coordinates imported.";
    public string Coordinates => "Coordinates";
    public string Collected => "Collected";
    public string NotCollected => "Not Collected";

    // Teleportation
    public string TeleportButton => "Teleport";

    // Chat and Communication
    public string SendToChat => "Send to Chat";
    public string SelectChatChannel => "Select Channel";
    public string StartMonitoring => "Start Monitoring";
    public string StopMonitoring => "Stop Monitoring";

    // Trash Management
    public string TrashBin => "Trash Bin";
    public string ClearTrash => "Clear Trash";
    public string EmptyTrashBin => "Trash bin is empty.";

    // Status Messages
    public string UnknownArea => "Unknown Area";
    public string NotLoggedIn => "Please log into the game to use this plugin.";
    public string ClipboardEmpty => "Clipboard is empty.";

    // Message Templates
    public string MessageTemplateManagement => "Message Template Management";
    public string SavedTemplates => "Saved Templates:";
    public string NoSavedTemplates => "No templates saved";
    public string SetAsActiveTemplate => "Set as Active Template";
    public string ClearActiveTemplate => "Clear Active Template";
    public string DeleteTemplate => "Delete Template";
    public string TemplateName => "Template Name:";
    public string NoActiveMessageTemplate => "No active message template";

    // Message Components
    public string CustomMessages => "Custom Messages";
    public string AddNewMessage => "Add New Message:";
    public string EditMessage => "Edit Message:";
    public string CurrentMessageComponentList => "Current Component List:";
    public string NoComponents => "No components added yet";
    public string AddComponents => "Add Components:";
    public string AddCustomMessage => "Custom Message:";
    public string MessagePreview => "Message Preview:";
    public string SaveTemplateChanges => "Save Template Changes";

    // Component Types
    public string PlayerName => "Player Name";
    public string Number => "Number (1-8)";
    public string BoxedNumber => "Boxed Number (1-8)";
    public string BoxedOutlinedNumber => "Boxed Outlined Number (1-8)";
    public string MoveUp => "Move Up";
    public string MoveDown => "Move Down";

    // Examples and Previews
    public string PlayerNameExample => "Tataru Taru";
    public string LocationExample => "Limsa Lominsa Lower Decks ( 9.5 , 11.2 )";
    public string DeleteButtonShort => "X";

    // Chat Channels
    public string Say => "Say";
    public string Yell => "Yell";
    public string Shout => "Shout";
    public string Party => "Party";
    public string Alliance => "Alliance";
    public string FreeCompany => "Free Company";
    public string LinkShell1 => "Linkshell 1";
    public string LinkShell2 => "Linkshell 2";
    public string LinkShell3 => "Linkshell 3";
    public string LinkShell4 => "Linkshell 4";
    public string LinkShell5 => "Linkshell 5";
    public string LinkShell6 => "Linkshell 6";
    public string LinkShell7 => "Linkshell 7";
    public string LinkShell8 => "Linkshell 8";
    public string CrossWorldLinkShell1 => "Cross-world Linkshell 1";
    public string CrossWorldLinkShell2 => "Cross-world Linkshell 2";
    public string CrossWorldLinkShell3 => "Cross-world Linkshell 3";
    public string CrossWorldLinkShell4 => "Cross-world Linkshell 4";
    public string CrossWorldLinkShell5 => "Cross-world Linkshell 5";
    public string CrossWorldLinkShell6 => "Cross-world Linkshell 6";
    public string CrossWorldLinkShell7 => "Cross-world Linkshell 7";
    public string CrossWorldLinkShell8 => "Cross-world Linkshell 8";

    // Status and Error Messages
    public string NoCoordinatesImported => "No coordinates were imported. Coordinates must include valid map area in format: 'MapName (x, y)'.";
    public string CoordinatesExportedToClipboard => "Coordinates exported to clipboard.";
    public string InvalidCustomMessage => "Invalid Custom Message";
    public string UnknownComponent => "Unknown Component";
    public string CoordinateOnlyMessage => "Will only send coordinate";
    public string EditCurrentMessageComponents => "Edit Current Message Components (No Active Template)";
    public string ViewingActiveTemplateReadOnly => "Viewing active template components (read-only). Select the template to edit it.";

    // Window-specific strings
    public string OpenCustomMessageWindow => "Custom Message";

    // Formatted Messages (with parameters)
    public string CoordinatesWithCount(int count) => $"Coordinates ({count})";
    public string OptimizedRouteWithCount(int count) => $"Optimized Route ({count}):";
    public string TrashBinWithCount(int count) => $"Trash Bin ({count})";
    public string CoordinatesImported(int count) => $"{count} coordinates imported.";
    public string CoordinatesImportedFromClipboard(int count) => $"Imported {count} coordinates from clipboard.";
    public string CoordinateDetected(string source, string coordinate) => $"Coordinate detected from {source}: {coordinate}";
    public string RouteOptimized(int count) => $"Route optimized with {count} points.";
    public string EditingTemplate(string templateName) => $"Editing Template: {templateName}";
    public string CurrentActiveTemplate(string templateName) => $"Current Active Template: {templateName}";
    public string CustomMessagePrefix(string message) => $"Custom: {message}";
    public string TeleportToLocation(string location) => $"Teleport to: {location}";
    public string TeleportCostAmount(string cost) => $"Cost: {cost} gil";
}
