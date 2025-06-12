namespace OnePiece.Localization;

/// <summary>
/// Strongly-typed localization strings with compile-time safety and IntelliSense support.
/// This class provides type-safe access to all localized strings in the plugin.
/// </summary>
public static class Strings
{
    private static ILocalizationData Current => LocalizationManager.Current;

    // Window and UI Elements
    public static string MainWindowTitle => Current.MainWindowTitle;
    public static string GeneralSettings => Current.GeneralSettings;
    public static string Language => Current.Language;
    public static string MessageSettings => Current.MessageSettings;
    public static string CustomMessageSettings => Current.CustomMessageSettings;

    // Actions and Buttons
    public static string Edit => Current.Edit;
    public static string Save => Current.Save;
    public static string Cancel => Current.Cancel;
    public static string Delete => Current.Delete;
    public static string Restore => Current.Restore;
    public static string Create => Current.Create;
    public static string Add => Current.Add;
    public static string Export => Current.Export;
    public static string Import => Current.Import;
    public static string ClearAll => Current.ClearAll;

    // Route Optimization
    public static string OptimizeRoute => Current.OptimizeRoute;
    public static string ResetOptimization => Current.ResetOptimization;
    public static string NoCoordinatesToOptimize => Current.NoCoordinatesToOptimize;

    // Coordinates and Collection
    public static string NoCoordinates => Current.NoCoordinates;
    public static string Coordinates => Current.Coordinates;
    public static string Collected => Current.Collected;
    public static string NotCollected => Current.NotCollected;

    // Teleportation
    public static string TeleportButton => Current.TeleportButton;
    public static string TeleportToLocation(string location) => Current.TeleportToLocation(location);
    public static string TeleportCostAmount(string cost) => Current.TeleportCostAmount(cost);

    // Chat and Communication
    public static string SendToChat => Current.SendToChat;
    public static string SelectChatChannel => Current.SelectChatChannel;
    public static string CurrentChannel => Current.CurrentChannel;
    public static string StartMonitoring => Current.StartMonitoring;
    public static string StopMonitoring => Current.StopMonitoring;

    // Trash Management
    public static string TrashBin => Current.TrashBin;
    public static string ClearTrash => Current.ClearTrash;
    public static string EmptyTrashBin => Current.EmptyTrashBin;

    // Status Messages
    public static string UnknownArea => Current.UnknownArea;
    public static string NotLoggedIn => Current.NotLoggedIn;
    public static string ClipboardEmpty => Current.ClipboardEmpty;

    // Message Templates
    public static string MessageTemplateManagement => Current.MessageTemplateManagement;
    public static string SavedTemplates => Current.SavedTemplates;
    public static string NoSavedTemplates => Current.NoSavedTemplates;
    public static string SetAsActiveTemplate => Current.SetAsActiveTemplate;
    public static string ClearActiveTemplate => Current.ClearActiveTemplate;
    public static string DeleteTemplate => Current.DeleteTemplate;
    public static string TemplateName => Current.TemplateName;
    public static string NoActiveMessageTemplate => Current.NoActiveMessageTemplate;

    // Message Components
    public static string CustomMessages => Current.CustomMessages;
    public static string AddNewMessage => Current.AddNewMessage;
    public static string EditMessage => Current.EditMessage;
    public static string CurrentMessageComponentList => Current.CurrentMessageComponentList;
    public static string NoComponents => Current.NoComponents;
    public static string AddComponents => Current.AddComponents;
    public static string AddCustomMessage => Current.AddCustomMessage;
    public static string MessagePreview => Current.MessagePreview;
    public static string SaveTemplateChanges => Current.SaveTemplateChanges;

    // Component Types
    public static string PlayerName => Current.PlayerName;
    public static string Number => Current.Number;
    public static string BoxedNumber => Current.BoxedNumber;
    public static string BoxedOutlinedNumber => Current.BoxedOutlinedNumber;
    public static string MoveUp => Current.MoveUp;
    public static string MoveDown => Current.MoveDown;

    // Examples and Previews
    public static string PlayerNameExample => Current.PlayerNameExample;
    public static string LocationExample => Current.LocationExample;

    // Chat Channels
    public static class ChatChannels
    {
        public static string Say => Current.Say;
        public static string Yell => Current.Yell;
        public static string Shout => Current.Shout;
        public static string Party => Current.Party;
        public static string Alliance => Current.Alliance;
        public static string FreeCompany => Current.FreeCompany;
        public static string LinkShell1 => Current.LinkShell1;
        public static string LinkShell2 => Current.LinkShell2;
        public static string LinkShell3 => Current.LinkShell3;
        public static string LinkShell4 => Current.LinkShell4;
        public static string LinkShell5 => Current.LinkShell5;
        public static string LinkShell6 => Current.LinkShell6;
        public static string LinkShell7 => Current.LinkShell7;
        public static string LinkShell8 => Current.LinkShell8;
        public static string CrossWorldLinkShell1 => Current.CrossWorldLinkShell1;
        public static string CrossWorldLinkShell2 => Current.CrossWorldLinkShell2;
        public static string CrossWorldLinkShell3 => Current.CrossWorldLinkShell3;
        public static string CrossWorldLinkShell4 => Current.CrossWorldLinkShell4;
        public static string CrossWorldLinkShell5 => Current.CrossWorldLinkShell5;
        public static string CrossWorldLinkShell6 => Current.CrossWorldLinkShell6;
        public static string CrossWorldLinkShell7 => Current.CrossWorldLinkShell7;
        public static string CrossWorldLinkShell8 => Current.CrossWorldLinkShell8;
    }

    // Formatted Messages (with parameters)
    public static class Messages
    {
        public static string CoordinatesWithCount(int count) => Current.CoordinatesWithCount(count);
        public static string OptimizedRouteWithCount(int count) => Current.OptimizedRouteWithCount(count);
        public static string TrashBinWithCount(int count) => Current.TrashBinWithCount(count);
        public static string CoordinatesImported(int count) => Current.CoordinatesImported(count);
        public static string CoordinatesImportedFromClipboard(int count) => Current.CoordinatesImportedFromClipboard(count);
        public static string CoordinateDetected(string source, string coordinate) => Current.CoordinateDetected(source, coordinate);
        public static string RouteOptimized(int count) => Current.RouteOptimized(count);
        public static string EditingTemplate(string templateName) => Current.EditingTemplate(templateName);
        public static string CurrentActiveTemplate(string templateName) => Current.CurrentActiveTemplate(templateName);
        public static string CustomMessagePrefix(string message) => Current.CustomMessagePrefix(message);
        public static string PlayerNameAlreadyAdded => Current.PlayerNameAlreadyAdded;
        public static string CoordinatesAlreadyAdded => Current.CoordinatesAlreadyAdded;
    }

    // Status and Error Messages
    public static class Status
    {
        public static string NoCoordinatesImported => Current.NoCoordinatesImported;
        public static string CoordinatesExportedToClipboard => Current.CoordinatesExportedToClipboard;
        public static string InvalidCustomMessage => Current.InvalidCustomMessage;
        public static string UnknownComponent => Current.UnknownComponent;
        public static string CoordinateOnlyMessage => Current.CoordinateOnlyMessage;
        public static string EditCurrentMessageComponents => Current.EditCurrentMessageComponents;
        public static string ViewingActiveTemplateReadOnly => Current.ViewingActiveTemplateReadOnly;
    }

    // Window-specific strings
    public static class Windows
    {
        public static string OpenCustomMessageWindow => Current.OpenCustomMessageWindow;
    }
}
