namespace OnePiece.Localization;

/// <summary>
/// German localization implementation.
/// </summary>
public class DE : ILocalizationData
{
    // Window and UI Elements
    public string MainWindowTitle => "One Piece";
    public string GeneralSettings => "Allgemeine Einstellungen";
    public string Language => "Sprache";
    public string MessageSettings => "Nachrichten-Einstellungen";
    public string CustomMessageSettings => "Benutzerdefinierte Nachrichten-Einstellungen";

    // Actions and Buttons
    public string Edit => "Bearbeiten";
    public string Save => "Speichern";
    public string Cancel => "Abbrechen";
    public string Delete => "Löschen";
    public string Restore => "Wiederherstellen";
    public string Create => "Erstellen";
    public string Add => "Hinzufügen";
    public string Export => "Exportieren";
    public string Import => "Importieren";
    public string ClearAll => "Alle löschen";

    // Route Optimization
    public string OptimizeRoute => "Route optimieren";
    public string ResetOptimization => "Optimierung zurücksetzen";
    public string NoCoordinatesToOptimize => "Keine Koordinaten zum Optimieren. Bitte importieren Sie zuerst Koordinaten.";

    // Coordinates and Collection
    public string NoCoordinates => "Keine Koordinaten importiert.";
    public string Coordinates => "Koordinaten";
    public string Collected => "Gesammelt";
    public string NotCollected => "Nicht gesammelt";

    // Teleportation
    public string TeleportButton => "Teleportieren";

    // Chat and Communication
    public string SendToChat => "An Chat senden";
    public string SelectChatChannel => "Kanal auswählen";
    public string CurrentChannel => "Aktueller Kanal";
    public string StartMonitoring => "Überwachung starten";
    public string StopMonitoring => "Überwachung stoppen";

    // Trash Management
    public string TrashBin => "Papierkorb";
    public string ClearTrash => "Papierkorb leeren";
    public string EmptyTrashBin => "Papierkorb ist leer.";

    // Status Messages
    public string UnknownArea => "Unbekanntes Gebiet";
    public string NotLoggedIn => "Bitte loggen Sie sich ins Spiel ein, um dieses Plugin zu verwenden.";
    public string ClipboardEmpty => "Zwischenablage ist leer.";

    // Message Templates
    public string MessageTemplateManagement => "Nachrichten-Vorlagen-Verwaltung";
    public string SavedTemplates => "Gespeicherte Vorlagen:";
    public string NoSavedTemplates => "Keine Vorlagen gespeichert";
    public string SetAsActiveTemplate => "Als aktive Vorlage festlegen";
    public string ClearActiveTemplate => "Aktive Vorlage löschen";
    public string DeleteTemplate => "Vorlage löschen";
    public string TemplateName => "Vorlagenname:";
    public string NoActiveMessageTemplate => "Keine aktive Nachrichten-Vorlage";

    // Message Components
    public string CustomMessages => "Benutzerdefinierte Nachrichten";
    public string AddNewMessage => "Neue Nachricht hinzufügen:";
    public string EditMessage => "Nachricht bearbeiten:";
    public string CurrentMessageComponentList => "Aktuelle Komponentenliste:";
    public string NoComponents => "Noch keine Komponenten hinzugefügt";
    public string AddComponents => "Komponenten hinzufügen:";
    public string AddCustomMessage => "Benutzerdefinierte Nachricht:";
    public string MessagePreview => "Nachrichten-Vorschau:";
    public string SaveTemplateChanges => "Vorlagen-Änderungen speichern";

    // Component Types
    public string PlayerName => "Spielername";
    public string Number => "Nummer (1-9)";
    public string BoxedNumber => "Umrahmte Nummer (1-31)";
    public string BoxedOutlinedNumber => "Umrahmte Kontur-Nummer (1-9)";
    public string MoveUp => "Nach oben";
    public string MoveDown => "Nach unten";

    // Examples and Previews
    public string PlayerNameExample => "Tataru Taru";
    public string LocationExample => "Limsa Lominsa - Untere Decks ( 9.5 , 11.2 )";

    // Chat Channels
    public string Say => "Sagen";
    public string Yell => "Rufen";
    public string Shout => "Schreien";
    public string Party => "Gruppe";
    public string Alliance => "Allianz";
    public string FreeCompany => "Freie Gesellschaft";
    public string LinkShell1 => "Linkshell 1";
    public string LinkShell2 => "Linkshell 2";
    public string LinkShell3 => "Linkshell 3";
    public string LinkShell4 => "Linkshell 4";
    public string LinkShell5 => "Linkshell 5";
    public string LinkShell6 => "Linkshell 6";
    public string LinkShell7 => "Linkshell 7";
    public string LinkShell8 => "Linkshell 8";
    public string CrossWorldLinkShell1 => "Welten-Linkshell 1";
    public string CrossWorldLinkShell2 => "Welten-Linkshell 2";
    public string CrossWorldLinkShell3 => "Welten-Linkshell 3";
    public string CrossWorldLinkShell4 => "Welten-Linkshell 4";
    public string CrossWorldLinkShell5 => "Welten-Linkshell 5";
    public string CrossWorldLinkShell6 => "Welten-Linkshell 6";
    public string CrossWorldLinkShell7 => "Welten-Linkshell 7";
    public string CrossWorldLinkShell8 => "Welten-Linkshell 8";

    // Status and Error Messages
    public string NoCoordinatesImported => "Keine Koordinaten wurden importiert. Koordinaten müssen ein gültiges Kartengebiet im Format 'Kartenname (x, y)' enthalten.";
    public string CoordinatesExportedToClipboard => "Koordinaten in die Zwischenablage exportiert.";
    public string InvalidCustomMessage => "Ungültige benutzerdefinierte Nachricht";
    public string UnknownComponent => "Unbekannte Komponente";
    public string CoordinateOnlyMessage => "Nur Koordinaten senden";
    public string EditCurrentMessageComponents => "Aktuelle Nachrichten-Komponenten bearbeiten (Keine aktive Vorlage)";
    public string ViewingActiveTemplateReadOnly => "Aktive Vorlagen-Komponenten anzeigen (schreibgeschützt). Wählen Sie die Vorlage aus, um sie zu bearbeiten.";

    // Window-specific strings
    public string OpenCustomMessageWindow => "Benutzerdefinierte Nachricht";

    // Formatted Messages (with parameters)
    public string CoordinatesWithCount(int count) => $"Koordinaten ({count})";
    public string OptimizedRouteWithCount(int count) => $"Optimierte Route ({count}):";
    public string TrashBinWithCount(int count) => $"Papierkorb ({count})";
    public string CoordinatesImported(int count) => $"{count} Koordinaten importiert.";
    public string CoordinatesImportedFromClipboard(int count) => $"{count} Koordinaten aus der Zwischenablage importiert.";
    public string CoordinateDetected(string source, string coordinate) => $"Koordinate von {source} erkannt: {coordinate}";
    public string RouteOptimized(int count) => $"Route mit {count} Punkten optimiert.";
    public string EditingTemplate(string templateName) => $"Vorlage bearbeiten: {templateName}";
    public string CurrentActiveTemplate(string templateName) => $"Aktuelle aktive Vorlage: {templateName}";
    public string CustomMessagePrefix(string message) => $"Benutzerdefiniert: {message}";
    public string TeleportToLocation(string location) => $"Teleportieren nach: {location}";
    public string TeleportCostAmount(string cost) => $"Kosten: {cost} Gil";
    public string PlayerNameAlreadyAdded => "Spielername kann nur einmal hinzugefügt werden";
    public string CoordinatesAlreadyAdded => "Koordinaten können nur einmal hinzugefügt werden";

    // Component range warnings
    public string ComponentRangeWarning(string components) => $"Hinweis: {components} Komponente(n) überschreiten den Anzeigebereich und werden nicht wirksam";
}
