namespace OnePiece.Localization;

/// <summary>
/// Interface for localization data providing all translated strings for a specific language.
/// </summary>
public interface ILocalizationData
{
    // Window and UI Elements
    string MainWindowTitle { get; }
    string GeneralSettings { get; }
    string Language { get; }
    string MessageSettings { get; }
    string CustomMessageSettings { get; }

    // Actions and Buttons
    string Edit { get; }
    string Save { get; }
    string Cancel { get; }
    string Delete { get; }
    string Restore { get; }
    string Create { get; }
    string Add { get; }
    string Export { get; }
    string Import { get; }
    string ClearAll { get; }

    // Route Optimization
    string OptimizeRoute { get; }
    string ResetOptimization { get; }
    string NoCoordinatesToOptimize { get; }

    // Coordinates and Collection
    string NoCoordinates { get; }
    string Coordinates { get; }
    string Collected { get; }
    string NotCollected { get; }

    // Teleportation
    string TeleportButton { get; }

    // Chat and Communication
    string SendToChat { get; }
    string SelectChatChannel { get; }
    string CurrentChannel { get; }
    string StartMonitoring { get; }
    string StopMonitoring { get; }

    // Trash Management
    string TrashBin { get; }
    string ClearTrash { get; }
    string EmptyTrashBin { get; }

    // Status Messages
    string UnknownArea { get; }
    string NotLoggedIn { get; }
    string ClipboardEmpty { get; }

    // Message Templates
    string MessageTemplateManagement { get; }
    string SavedTemplates { get; }
    string NoSavedTemplates { get; }
    string SetAsActiveTemplate { get; }
    string ClearActiveTemplate { get; }
    string DeleteTemplate { get; }
    string TemplateName { get; }
    string NoActiveMessageTemplate { get; }

    // Message Components
    string CustomMessages { get; }
    string AddNewMessage { get; }
    string EditMessage { get; }
    string CurrentMessageComponentList { get; }
    string NoComponents { get; }
    string AddComponents { get; }
    string AddCustomMessage { get; }
    string MessagePreview { get; }
    string SaveTemplateChanges { get; }

    // Component Types
    string PlayerName { get; }
    string Number { get; }
    string BoxedNumber { get; }
    string BoxedOutlinedNumber { get; }
    string MoveUp { get; }
    string MoveDown { get; }

    // Examples and Previews
    string PlayerNameExample { get; }
    string LocationExample { get; }

    // Chat Channels
    string Say { get; }
    string Yell { get; }
    string Shout { get; }
    string Party { get; }
    string Alliance { get; }
    string FreeCompany { get; }
    string LinkShell1 { get; }
    string LinkShell2 { get; }
    string LinkShell3 { get; }
    string LinkShell4 { get; }
    string LinkShell5 { get; }
    string LinkShell6 { get; }
    string LinkShell7 { get; }
    string LinkShell8 { get; }
    string CrossWorldLinkShell1 { get; }
    string CrossWorldLinkShell2 { get; }
    string CrossWorldLinkShell3 { get; }
    string CrossWorldLinkShell4 { get; }
    string CrossWorldLinkShell5 { get; }
    string CrossWorldLinkShell6 { get; }
    string CrossWorldLinkShell7 { get; }
    string CrossWorldLinkShell8 { get; }

    // Status and Error Messages
    string NoCoordinatesImported { get; }
    string CoordinatesExportedToClipboard { get; }
    string InvalidCustomMessage { get; }
    string UnknownComponent { get; }
    string CoordinateOnlyMessage { get; }
    string EditCurrentMessageComponents { get; }
    string ViewingActiveTemplateReadOnly { get; }

    // Window-specific strings
    string OpenCustomMessageWindow { get; }

    // Formatted Messages (with parameters)
    string CoordinatesWithCount(int count);
    string OptimizedRouteWithCount(int count);
    string TrashBinWithCount(int count);
    string CoordinatesImported(int count);
    string CoordinatesImportedFromClipboard(int count);
    string CoordinateDetected(string source, string coordinate);
    string RouteOptimized(int count);
    string EditingTemplate(string templateName);
    string CurrentActiveTemplate(string templateName);
    string CustomMessagePrefix(string message);
    string TeleportToLocation(string location);
    string TeleportCostAmount(string cost);
    string PlayerNameAlreadyAdded { get; }
    string CoordinatesAlreadyAdded { get; }
}
