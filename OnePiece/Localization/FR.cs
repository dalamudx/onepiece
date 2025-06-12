namespace OnePiece.Localization;

/// <summary>
/// French localization implementation.
/// </summary>
public class FR : ILocalizationData
{
    // Window and UI Elements
    public string MainWindowTitle => "One Piece";
    public string GeneralSettings => "Paramètres généraux";
    public string Language => "Langue";
    public string MessageSettings => "Paramètres de message";
    public string CustomMessageSettings => "Paramètres de message personnalisé";

    // Actions and Buttons
    public string Edit => "Modifier";
    public string Save => "Sauvegarder";
    public string Cancel => "Annuler";
    public string Delete => "Supprimer";
    public string Restore => "Restaurer";
    public string Create => "Créer";
    public string Add => "Ajouter";
    public string Export => "Exporter";
    public string Import => "Importer";
    public string ClearAll => "Tout effacer";

    // Route Optimization
    public string OptimizeRoute => "Optimiser l'itinéraire";
    public string ResetOptimization => "Réinitialiser l'optimisation";
    public string NoCoordinatesToOptimize => "Aucune coordonnée à optimiser. Veuillez d'abord importer des coordonnées.";

    // Coordinates and Collection
    public string NoCoordinates => "Aucune coordonnée importée.";
    public string Coordinates => "Coordonnées";
    public string Collected => "Collecté";
    public string NotCollected => "Non collecté";

    // Teleportation
    public string TeleportButton => "Téléporter";

    // Chat and Communication
    public string SendToChat => "Envoyer au chat";
    public string SelectChatChannel => "Sélectionner le canal";
    public string CurrentChannel => "Canal actuel";
    public string StartMonitoring => "Démarrer la surveillance";
    public string StopMonitoring => "Arrêter la surveillance";

    // Trash Management
    public string TrashBin => "Corbeille";
    public string ClearTrash => "Vider la corbeille";
    public string EmptyTrashBin => "La corbeille est vide.";

    // Status Messages
    public string UnknownArea => "Zone inconnue";
    public string NotLoggedIn => "Veuillez vous connecter au jeu pour utiliser ce plugin.";
    public string ClipboardEmpty => "Le presse-papiers est vide.";

    // Message Templates
    public string MessageTemplateManagement => "Gestion des modèles de message";
    public string SavedTemplates => "Modèles sauvegardés :";
    public string NoSavedTemplates => "Aucun modèle sauvegardé";
    public string SetAsActiveTemplate => "Définir comme modèle actif";
    public string ClearActiveTemplate => "Effacer le modèle actif";
    public string DeleteTemplate => "Supprimer le modèle";
    public string TemplateName => "Nom du modèle :";
    public string NoActiveMessageTemplate => "Aucun modèle de message actif";

    // Message Components
    public string CustomMessages => "Messages personnalisés";
    public string AddNewMessage => "Ajouter un nouveau message :";
    public string EditMessage => "Modifier le message :";
    public string CurrentMessageComponentList => "Liste des composants actuels :";
    public string NoComponents => "Aucun composant ajouté";
    public string AddComponents => "Ajouter des composants :";
    public string AddCustomMessage => "Message personnalisé :";
    public string MessagePreview => "Aperçu du message :";
    public string SaveTemplateChanges => "Sauvegarder les modifications du modèle";

    // Component Types
    public string PlayerName => "Nom du joueur";
    public string Number => "Numéro (1-8)";
    public string BoxedNumber => "Numéro encadré (1-8)";
    public string BoxedOutlinedNumber => "Numéro encadré avec contour (1-8)";
    public string MoveUp => "Monter";
    public string MoveDown => "Descendre";

    // Examples and Previews
    public string PlayerNameExample => "Tataru Taru";
    public string LocationExample => "Limsa Lominsa - Pont inférieur ( 9.5 , 11.2 )";

    // Chat Channels
    public string Say => "Dire";
    public string Yell => "Crier";
    public string Shout => "Hurler";
    public string Party => "Équipe";
    public string Alliance => "Alliance";
    public string FreeCompany => "Compagnie libre";
    public string LinkShell1 => "Linkshell 1";
    public string LinkShell2 => "Linkshell 2";
    public string LinkShell3 => "Linkshell 3";
    public string LinkShell4 => "Linkshell 4";
    public string LinkShell5 => "Linkshell 5";
    public string LinkShell6 => "Linkshell 6";
    public string LinkShell7 => "Linkshell 7";
    public string LinkShell8 => "Linkshell 8";
    public string CrossWorldLinkShell1 => "Linkshell inter-monde 1";
    public string CrossWorldLinkShell2 => "Linkshell inter-monde 2";
    public string CrossWorldLinkShell3 => "Linkshell inter-monde 3";
    public string CrossWorldLinkShell4 => "Linkshell inter-monde 4";
    public string CrossWorldLinkShell5 => "Linkshell inter-monde 5";
    public string CrossWorldLinkShell6 => "Linkshell inter-monde 6";
    public string CrossWorldLinkShell7 => "Linkshell inter-monde 7";
    public string CrossWorldLinkShell8 => "Linkshell inter-monde 8";

    // Status and Error Messages
    public string NoCoordinatesImported => "Aucune coordonnée n'a été importée. Les coordonnées doivent inclure une zone de carte valide au format 'Nom de la carte (x, y)'.";
    public string CoordinatesExportedToClipboard => "Coordonnées exportées vers le presse-papiers.";
    public string InvalidCustomMessage => "Message personnalisé invalide";
    public string UnknownComponent => "Composant inconnu";
    public string CoordinateOnlyMessage => "Envoyer uniquement les coordonnées";
    public string EditCurrentMessageComponents => "Modifier les composants de message actuels (Aucun modèle actif)";
    public string ViewingActiveTemplateReadOnly => "Affichage des composants du modèle actif (lecture seule). Sélectionnez le modèle pour le modifier.";

    // Window-specific strings
    public string OpenCustomMessageWindow => "Message personnalisé";

    // Formatted Messages (with parameters)
    public string CoordinatesWithCount(int count) => $"Coordonnées ({count})";
    public string OptimizedRouteWithCount(int count) => $"Itinéraire optimisé ({count}) :";
    public string TrashBinWithCount(int count) => $"Corbeille ({count})";
    public string CoordinatesImported(int count) => $"{count} coordonnées importées.";
    public string CoordinatesImportedFromClipboard(int count) => $"{count} coordonnées importées depuis le presse-papiers.";
    public string CoordinateDetected(string source, string coordinate) => $"Coordonnée détectée de {source} : {coordinate}";
    public string RouteOptimized(int count) => $"Itinéraire optimisé avec {count} points.";
    public string EditingTemplate(string templateName) => $"Modification du modèle : {templateName}";
    public string CurrentActiveTemplate(string templateName) => $"Modèle actif actuel : {templateName}";
    public string CustomMessagePrefix(string message) => $"Personnalisé : {message}";
    public string TeleportToLocation(string location) => $"Téléporter vers : {location}";
    public string TeleportCostAmount(string cost) => $"Coût : {cost} gils";
    public string PlayerNameAlreadyAdded => "Le nom du joueur ne peut être ajouté qu'une seule fois";
    public string CoordinatesAlreadyAdded => "Les coordonnées ne peuvent être ajoutées qu'une seule fois";
}
