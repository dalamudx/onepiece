using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Linq;
using OnePiece.Localization;
using OnePiece.Models;
using OnePiece.Helpers;

namespace OnePiece.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin plugin;
    private string[] chatChannelNames = Array.Empty<string>(); // Initialize with empty array
    private string[] supportedLanguages;
    private int selectedLanguageIndex;
    private int selectedChatChannelIndex;

    // UI performance optimization - cached values
    private string? cachedNotLoggedInMessage;
    private float cachedNotLoggedInWidth;
    private bool needsUIRecalculation = true;

    // Edit state tracking for coordinates
    private readonly Dictionary<int, bool> editingStates = new();
    private readonly Dictionary<int, string> editingPlayerNames = new();
    private readonly Dictionary<int, string> editingXCoords = new();
    private readonly Dictionary<int, string> editingYCoords = new();

    // We don't need a local reference to CustomMessageWindow anymore
    // since we'll use the one from Plugin instance

    public MainWindow(Plugin plugin)
        : base(Strings.MainWindowTitle + "##OnePiece", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400), // Increased minimum width
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;

        // Initialize chat channel names
        InitializeChatChannelNames();

        // Initialize language selection
        supportedLanguages = LocalizationManager.GetSupportedLanguages();
        selectedLanguageIndex = Array.IndexOf(supportedLanguages, this.plugin.Configuration.Language);
        if (selectedLanguageIndex < 0) selectedLanguageIndex = 0;



        // Initialize chat channel selection
        selectedChatChannelIndex = (int)this.plugin.Configuration.MonitoredChatChannel;

        // We no longer initialize CustomMessageWindow here
        // It's now fully managed by the Plugin class

        // Subscribe to events
        this.plugin.TreasureHuntService.OnCoordinatesImported += OnCoordinatesImported;
        this.plugin.TreasureHuntService.OnRouteOptimized += OnRouteOptimized;
        
        // Subscribe to message template updates from CustomMessageWindow
        this.plugin.MessageTemplateUpdated += OnMessageTemplateUpdated;
    }

    private void InitializeChatChannelNames()
    {
        // Create localized chat channel names
        chatChannelNames = new[]
        {
            Strings.ChatChannels.Say,
            Strings.ChatChannels.Yell,
            Strings.ChatChannels.Shout,
            Strings.ChatChannels.Party,
            Strings.ChatChannels.Alliance,
            Strings.ChatChannels.FreeCompany,
            Strings.ChatChannels.LinkShell1,
            Strings.ChatChannels.LinkShell2,
            Strings.ChatChannels.LinkShell3,
            Strings.ChatChannels.LinkShell4,
            Strings.ChatChannels.LinkShell5,
            Strings.ChatChannels.LinkShell6,
            Strings.ChatChannels.LinkShell7,
            Strings.ChatChannels.LinkShell8,
            Strings.ChatChannels.CrossWorldLinkShell1,
            Strings.ChatChannels.CrossWorldLinkShell2,
            Strings.ChatChannels.CrossWorldLinkShell3,
            Strings.ChatChannels.CrossWorldLinkShell4,
            Strings.ChatChannels.CrossWorldLinkShell5,
            Strings.ChatChannels.CrossWorldLinkShell6,
            Strings.ChatChannels.CrossWorldLinkShell7,
            Strings.ChatChannels.CrossWorldLinkShell8
        };
    }



    public void Dispose()
    {
        // Unsubscribe from events
        plugin.TreasureHuntService.OnCoordinatesImported -= OnCoordinatesImported;
        plugin.TreasureHuntService.OnRouteOptimized -= OnRouteOptimized;
        plugin.MessageTemplateUpdated -= OnMessageTemplateUpdated;
    }

    public override void Draw()
    {
        // Get window width and height for centering
        float windowWidth = ImGui.GetWindowWidth();
        float windowHeight = ImGui.GetWindowHeight();
        
        // Check if player is logged in
        bool isLoggedIn = Plugin.ClientState.IsLoggedIn;
        
        // If not logged in, display warning at the top of the window
        if (!isLoggedIn)
        {
            // Cache the warning message and width calculation
            if (needsUIRecalculation || cachedNotLoggedInMessage == null)
            {
                cachedNotLoggedInMessage = Strings.NotLoggedIn;
                cachedNotLoggedInWidth = ImGui.CalcTextSize(cachedNotLoggedInMessage).X;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.0f, 1.0f)); // Yellow warning text
            ImGui.SetCursorPosX((windowWidth - cachedNotLoggedInWidth) * 0.5f);
            ImGui.TextUnformatted(cachedNotLoggedInMessage);
            ImGui.PopStyleColor();

            ImGui.Separator();
        }


        
        // Begin disabled group if player is not logged in
        if (!isLoggedIn)
        {
            ImGui.BeginDisabled();
        }

        // Calculate optimal width for labels for consistent alignment
        string[] labelsToMeasure = new string[] {
            Strings.Language,
            Strings.SelectChatChannel,
            Strings.MessageSettings
        };

        // Use UIHelper for consistent label width calculation with optimized parameters for tight layout
        float labelWidth = UIHelper.CalculateLabelWidth(labelsToMeasure);

        // Calculate precise combo widths based on actual content using UIHelper with tight fit parameters
        float languageComboWidth = UIHelper.CalculateComboWidth(supportedLanguages);
        float channelComboWidth = UIHelper.CalculateComboWidth(chatChannelNames);

        // Get monitoring status once for use in multiple sections
        bool isMonitoring = plugin.ChatMonitorService.IsMonitoring;

        // General Settings section with collapsing header
        if (ImGui.CollapsingHeader(Strings.GeneralSettings))
        {
            // Language selection
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Strings.Language);
            ImGui.SameLine(labelWidth);
            // Set reasonable max width for combo box to prevent it from being too wide
            ImGui.SetNextItemWidth(languageComboWidth);
            if (ImGui.Combo("##LanguageSelector", ref selectedLanguageIndex, supportedLanguages, supportedLanguages.Length))
            {
                plugin.Configuration.Language = supportedLanguages[selectedLanguageIndex];
                LocalizationManager.SetLanguage(plugin.Configuration.Language);
                plugin.Configuration.Save();

                // Refresh localized LocalizationManager
                InitializeChatChannelNames();

                // Trigger UI recalculation for cached values
                needsUIRecalculation = true;
            }

            ImGui.Spacing();

            // Chat channel selection - moved from ChannelSettings
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Strings.SelectChatChannel);
            ImGui.SameLine(labelWidth);
            // Set reasonable max width for combo box to prevent it from being too wide
            ImGui.SetNextItemWidth(channelComboWidth);

            // Disable the combo box if monitoring is active
            if (isMonitoring)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Combo("##ChatChannelSelector", ref selectedChatChannelIndex, chatChannelNames, chatChannelNames.Length))
            {
                plugin.Configuration.MonitoredChatChannel = (ChatChannelType)selectedChatChannelIndex;
                plugin.Configuration.Save();
            }

            // End the disabled state if monitoring is active
            if (isMonitoring)
            {
                ImGui.EndDisabled();
            }

            ImGui.Spacing();

            // Message settings button (moved under General Settings)
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Strings.MessageSettings);
            ImGui.SameLine(labelWidth);

            // Remove fixed width - let button size itself based on content
            if (ImGui.Button(Strings.Windows.OpenCustomMessageWindow))
            {
                plugin.ShowCustomMessageWindow();
            }
        }

        ImGui.Separator();

        // Message settings section (no collapsing header, always visible)
        // Show current monitoring channel information
        string currentChannelName = chatChannelNames[selectedChatChannelIndex];
        ImGui.TextColored(new Vector4(0.0f, 0.6f, 1.0f, 1.0f), $"{Strings.CurrentChannel}: {currentChannelName}");

        // Show active template information if there is one
        if (plugin.Configuration.ActiveTemplateIndex >= 0 &&
            plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            string templateName = plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Name;
            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.0f, 1.0f), Strings.Messages.CurrentActiveTemplate(templateName));

            // Preview of the active template
            ImGui.Spacing();
            ImGui.Text(Strings.MessagePreview);
            string previewMessage = GeneratePreviewMessage();
            ImGui.TextWrapped(previewMessage);
        }
        else
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Strings.NoActiveMessageTemplate);

            // When no active template is selected, show only LocationExample in client language
            ImGui.Spacing();
            ImGui.Text(Strings.MessagePreview);
            string clientLocationExample = LocalizationManager.GetClientLanguageLocationExample();
            ImGui.TextWrapped(clientLocationExample);
        }

        ImGui.Separator();

        // Action buttons with adaptive wrapping layout
        RenderActionButtons(isMonitoring);

        ImGui.Separator();

        // Display coordinates and route
        using (var child = ImRaii.Child("CoordinatesDisplay", new Vector2(-1, -1), true))
        {
            if (child.Success)
            {
                var coordinates = plugin.TreasureHuntService.Coordinates;
                var optimizedRoute = plugin.TreasureHuntService.OptimizedRoute;

                if (coordinates.Count > 0)
                {
                    // Display the optimized route
                    if (optimizedRoute.Count > 0)
                    {
                        // Create a new list for display, but we need to keep using the original optimizedRoute
                        // to maintain proper indexing and relationship between teleport points and their destinations
                        var displayRoute = optimizedRoute;
                        
                        // Count actual treasure points for display
                        int treasurePointCount = displayRoute.Count(c => c.Type == CoordinateType.TreasurePoint);
                        
                        // Display optimized route title with count (excluding teleport points)
                        ImGui.TextUnformatted(Strings.Messages.OptimizedRouteWithCount(treasurePointCount));

                        // Group coordinates by map area - optimize by using a more efficient approach
                        // Pre-allocate the dictionary with expected capacity to avoid resizing
                        var uniqueMapAreas = new HashSet<string>(displayRoute.Select(c => c.MapArea));
                        var coordinatesByMap = new Dictionary<string, List<TreasureCoordinate>>(uniqueMapAreas.Count);

                        // Manually group coordinates to avoid multiple LINQ operations
                        foreach (var mapArea in uniqueMapAreas)
                        {
                            coordinatesByMap[mapArea] = new List<TreasureCoordinate>();
                        }

                        // Fill the groups in a single pass through the coordinates (only add non-teleport points)
                        foreach (var coord in displayRoute)
                        {
                            coordinatesByMap[coord.MapArea].Add(coord);
                        }

                        // Display coordinates grouped by map area
                        foreach (var mapGroup in coordinatesByMap)
                        {
                            var mapArea = mapGroup.Key;

                            // Display map area header
                            if (!string.IsNullOrEmpty(mapArea))
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), mapArea);
                            }
                            else
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), Strings.UnknownArea);
                            }

                            // Get coordinates for this map area while preserving the original order
                            // This is more efficient than using Where().ToList() on every frame
                            var mapAreaCoords = mapGroup.Value;

                            // Calculate optimal player name column width for this map area
                            float playerNameColumnWidth = UIHelper.CalculatePlayerNameColumnWidth(mapAreaCoords, showPlayerName: true);
                            for (var i = 0; i < mapAreaCoords.Count; i++)
                            {
                                var coord = mapAreaCoords[i];
                                var isCollected = coord.IsCollected;

                                if (isCollected)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                                }

                                // Note: CoordinateType.TeleportPoint means this treasure coordinate should show a teleport button
                                // We should display all treasure coordinates, regardless of their type
                                
                                // Note: We no longer need to pass AetheryteId to the next coordinate here
                                // since we're now setting it directly in TimeBasedPathFinder.cs

                                // Calculate the actual index (all coordinates should be counted and numbered)
                                // Both TreasurePoint and TeleportPoint types represent treasure coordinates that need to be visited
                                int realIndex = optimizedRoute.IndexOf(coord) + 1; // +1 to start from 1 instead of 0

                                // Use column-aligned rendering for better visual organization
                                // No MapArea since already grouped by map
                                float availableWidth = ImGui.GetContentRegionAvail().X;
                                int buttonCount = 3; // Always reserve space for Teleport + Chat + Collected

                                var layoutInfo = UIHelper.RenderCoordinateEntryWithColumns(
                                    coord,
                                    realIndex,
                                    playerNameColumnWidth,
                                    showPlayerName: true,
                                    showMapArea: false,
                                    isCollected,
                                    availableWidth,
                                    buttonCount);

                                // Check if this coordinate has AetheryteId for teleportation
                                bool hasTeleportId = coord.AetheryteId > 0;

                                // Calculate consistent button widths
                                float teleportButtonWidth = UIHelper.CalculateButtonWidth(Strings.TeleportButton);
                                float chatButtonWidth = UIHelper.CalculateButtonWidth(Strings.SendToChat);
                                string collectedButtonText = coord.IsCollected ? Strings.NotCollected : Strings.Collected;
                                float collectedButtonWidth = UIHelper.CalculateButtonWidth(collectedButtonText);

                                float buttonSpacing = 8f;
                                float rightMargin = 15f; // Consistent right margin for all languages

                                // Calculate button positions from right to left to ensure consistent right margin
                                float optimizedWindowWidth = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();

                                // Position 3: Collected button (rightmost)
                                float collectedButtonX = optimizedWindowWidth - rightMargin - collectedButtonWidth;

                                // Position 2: Send to Chat button
                                float chatButtonX = collectedButtonX - buttonSpacing - chatButtonWidth;

                                // Position 1: Teleport button (leftmost, if exists)
                                float teleportButtonX = chatButtonX - buttonSpacing - teleportButtonWidth;

                                // Render buttons from left to right
                                if (hasTeleportId)
                                {
                                    // Position and render teleport button
                                    ImGui.SetCursorPos(new Vector2(teleportButtonX, layoutInfo.LineStartY));

                                    // Get aetheryte information directly using ID
                                    var aetheryteInfo = plugin.AetheryteService.GetAetheryteById(coord.AetheryteId);
                                    int teleportPrice = 0;

                                    if (aetheryteInfo != null)
                                    {
                                        // Calculate teleport price using aetheryte info
                                        teleportPrice = aetheryteInfo.CalculateTeleportFee();

                                        // Disable teleport button if coordinate is collected
                                        if (isCollected)
                                        {
                                            ImGui.BeginDisabled();
                                        }

                                        // Add teleport button with consistent width
                                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 1.0f, 0.7f));
                                        if (ImGui.SmallButton($"{Strings.TeleportButton}##{optimizedRoute.IndexOf(coord)}"))
                                        {
                                            // Teleport directly using the aetheryte info
                                            plugin.AetheryteService.TeleportToAetheryte(aetheryteInfo);
                                        }

                                        // Add tooltip with teleport information
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.BeginTooltip();
                                            ImGui.Text(Strings.TeleportToLocation(aetheryteInfo.Name));
                                            if (teleportPrice > 0)
                                            {
                                                ImGui.Text(Strings.TeleportCostAmount(teleportPrice.ToString()));
                                            }
                                            ImGui.EndTooltip();
                                        }

                                        ImGui.PopStyleColor();

                                        // End disabled state if coordinate is collected
                                        if (isCollected)
                                        {
                                            ImGui.EndDisabled();
                                        }
                                    }
                                }

                                // Position and render Send to Chat button
                                ImGui.SetCursorPos(new Vector2(chatButtonX, layoutInfo.LineStartY));

                                // Send to Chat button - disable if the coordinate is already collected
                                if (isCollected)
                                {
                                    ImGui.BeginDisabled();
                                }

                                if (ImGui.SmallButton(Strings.SendToChat + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    plugin.ChatMonitorService.SendCoordinateToChat(coord);
                                }

                                if (isCollected)
                                {
                                    ImGui.EndDisabled();
                                }

                                // Position and render Collected button
                                ImGui.SetCursorPos(new Vector2(collectedButtonX, layoutInfo.LineStartY));

                                // Collected button - toggle between collected and not collected
                                if (ImGui.SmallButton(collectedButtonText + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    // Toggle the collected state
                                    coord.IsCollected = !coord.IsCollected;
                                }

                                // Move cursor to next line, accounting for text height with additional spacing
                                float lineSpacing = 4f; // Add spacing between coordinate entries
                                float nextLineY = layoutInfo.LineStartY + Math.Max(layoutInfo.TextHeight, ImGui.GetFrameHeight()) + lineSpacing;
                                ImGui.SetCursorPosY(nextLineY);

                                // Pop style color if it was pushed for collected coordinates
                                if (isCollected)
                                {
                                    ImGui.PopStyleColor();
                                }

                                // No Delete button for optimized route
                            }
                        }
                    }
                    else
                    {
                        // Display the raw coordinates if no optimized route
                        ImGui.TextUnformatted(Strings.Messages.CoordinatesWithCount(coordinates.Count));

                        // Calculate optimal player name column width for all coordinates
                        float playerNameColumnWidth = UIHelper.CalculatePlayerNameColumnWidth(coordinates, showPlayerName: true);

                        for (var i = 0; i < coordinates.Count; i++)
                        {
                            var coord = coordinates[i];
                            int coordIndex = i + 1; // Use 1-based index for consistency

                            // Check if this coordinate is being edited
                            bool isEditing = editingStates.GetValueOrDefault(coordIndex, false);

                            // Declare layoutInfo outside the conditional blocks
                            UIHelper.CoordinateLayoutInfo layoutInfo = default;

                            if (isEditing)
                            {
                                // Display editable fields
                                ImGui.TextUnformatted($"{coordIndex}. ");
                                ImGui.SameLine();

                                // Player name input
                                string playerName = editingPlayerNames.GetValueOrDefault(coordIndex, coord.PlayerName ?? "");
                                ImGui.SetNextItemWidth(120);
                                if (ImGui.InputText($"##PlayerNameRaw{coordIndex}", ref playerName, 100))
                                {
                                    editingPlayerNames[coordIndex] = playerName;
                                }
                                ImGui.SameLine();
                                ImGui.TextUnformatted(": ");
                                ImGui.SameLine();

                                // Map area display (not editable)
                                if (!string.IsNullOrEmpty(coord.MapArea))
                                {
                                    ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), coord.MapArea);
                                    ImGui.SameLine();
                                }

                                ImGui.TextUnformatted("(");
                                ImGui.SameLine();

                                // X coordinate input
                                string xCoord = editingXCoords.GetValueOrDefault(coordIndex, coord.X.ToString("F1"));
                                ImGui.SetNextItemWidth(60);
                                if (ImGui.InputText($"##XCoordRaw{coordIndex}", ref xCoord, 10))
                                {
                                    editingXCoords[coordIndex] = xCoord;
                                }
                                ImGui.SameLine();
                                ImGui.TextUnformatted(", ");
                                ImGui.SameLine();

                                // Y coordinate input
                                string yCoord = editingYCoords.GetValueOrDefault(coordIndex, coord.Y.ToString("F1"));
                                ImGui.SetNextItemWidth(60);
                                if (ImGui.InputText($"##YCoordRaw{coordIndex}", ref yCoord, 10))
                                {
                                    editingYCoords[coordIndex] = yCoord;
                                }
                                ImGui.SameLine();
                                ImGui.TextUnformatted(")");

                                // Set default layout info for editing mode
                                layoutInfo = new UIHelper.CoordinateLayoutInfo
                                {
                                    LineStartY = ImGui.GetCursorPosY(),
                                    TextHeight = ImGui.GetFrameHeight()
                                };
                            }
                            else
                            {
                                // Use column-aligned rendering for better visual organization
                                // Include MapArea for raw coordinates
                                float availableWidth = ImGui.GetContentRegionAvail().X;
                                int buttonCount = 2; // Edit + Delete

                                layoutInfo = UIHelper.RenderCoordinateEntryWithColumns(
                                    coord,
                                    coordIndex,
                                    playerNameColumnWidth,
                                    showPlayerName: true,
                                    showMapArea: true,
                                    isCollected: false,
                                    availableWidth,
                                    buttonCount);
                            }

                            // Render buttons with right-aligned layout for consistent margins
                            if (isEditing)
                            {
                                // Calculate button widths for editing mode
                                float saveButtonWidth = UIHelper.CalculateButtonWidth(Strings.Save);
                                float cancelButtonWidth = UIHelper.CalculateButtonWidth(Strings.Cancel);
                                float deleteButtonWidth = UIHelper.CalculateButtonWidth(Strings.Delete);

                                float buttonSpacing = 8f;
                                float rightMargin = 15f;
                                float editingWindowWidth = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();

                                // Calculate positions from right to left
                                float deleteButtonX = editingWindowWidth - rightMargin - deleteButtonWidth;
                                float cancelButtonX = deleteButtonX - buttonSpacing - cancelButtonWidth;
                                float saveButtonX = cancelButtonX - buttonSpacing - saveButtonWidth;

                                // Position and render Save button
                                ImGui.SetCursorPos(new Vector2(saveButtonX, layoutInfo.LineStartY));
                                if (ImGui.SmallButton($"{Strings.Save}##raw{coordIndex}"))
                                {
                                    // Validate and save changes
                                    if (float.TryParse(editingXCoords.GetValueOrDefault(coordIndex, "0"), out float newX) &&
                                        float.TryParse(editingYCoords.GetValueOrDefault(coordIndex, "0"), out float newY))
                                    {
                                        coord.X = newX;
                                        coord.Y = newY;
                                        coord.PlayerName = editingPlayerNames.GetValueOrDefault(coordIndex, "");

                                        // Clear editing state
                                        editingStates.Remove(coordIndex);
                                        editingPlayerNames.Remove(coordIndex);
                                        editingXCoords.Remove(coordIndex);
                                        editingYCoords.Remove(coordIndex);

                                        // Re-assign aetheryte since coordinates changed
                                        var nearestAetheryte = plugin.AetheryteService.GetNearestAetheryteToCoordinate(coord, plugin.MapAreaTranslationService);
                                        if (nearestAetheryte != null)
                                        {
                                            coord.AetheryteId = nearestAetheryte.AetheryteId;
                                        }
                                    }
                                }

                                // Position and render Cancel button
                                ImGui.SetCursorPos(new Vector2(cancelButtonX, layoutInfo.LineStartY));
                                if (ImGui.SmallButton($"{Strings.Cancel}##raw{coordIndex}"))
                                {
                                    // Clear editing state without saving
                                    editingStates.Remove(coordIndex);
                                    editingPlayerNames.Remove(coordIndex);
                                    editingXCoords.Remove(coordIndex);
                                    editingYCoords.Remove(coordIndex);
                                }

                                // Position and render Delete button
                                ImGui.SetCursorPos(new Vector2(deleteButtonX, layoutInfo.LineStartY));
                                if (ImGui.SmallButton(Strings.Delete + $"##raw{coordIndex}"))
                                {
                                    plugin.TreasureHuntService.DeleteCoordinate(i);

                                    // Clear editing state if this coordinate was being edited
                                    editingStates.Remove(coordIndex);
                                    editingPlayerNames.Remove(coordIndex);
                                    editingXCoords.Remove(coordIndex);
                                    editingYCoords.Remove(coordIndex);
                                }
                            }
                            else
                            {
                                // Calculate button widths for non-editing mode
                                float editButtonWidth = UIHelper.CalculateButtonWidth(Strings.Edit);
                                float deleteButtonWidth = UIHelper.CalculateButtonWidth(Strings.Delete);

                                float buttonSpacing = 8f;
                                float rightMargin = 15f;
                                float rawWindowWidth = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();

                                // Calculate positions from right to left
                                float deleteButtonX = rawWindowWidth - rightMargin - deleteButtonWidth;
                                float editButtonX = deleteButtonX - buttonSpacing - editButtonWidth;

                                // Position and render Edit button
                                ImGui.SetCursorPos(new Vector2(editButtonX, layoutInfo.LineStartY));
                                if (ImGui.SmallButton($"{Strings.Edit}##raw{coordIndex}"))
                                {
                                    // Enter editing mode
                                    editingStates[coordIndex] = true;
                                    editingPlayerNames[coordIndex] = coord.PlayerName ?? "";
                                    editingXCoords[coordIndex] = coord.X.ToString("F1");
                                    editingYCoords[coordIndex] = coord.Y.ToString("F1");
                                }

                                // Position and render Delete button
                                ImGui.SetCursorPos(new Vector2(deleteButtonX, layoutInfo.LineStartY));
                                if (ImGui.SmallButton(Strings.Delete + $"##raw{coordIndex}"))
                                {
                                    plugin.TreasureHuntService.DeleteCoordinate(i);

                                    // Clear editing state if this coordinate was being edited
                                    editingStates.Remove(coordIndex);
                                    editingPlayerNames.Remove(coordIndex);
                                    editingXCoords.Remove(coordIndex);
                                    editingYCoords.Remove(coordIndex);
                                }
                            }

                            // Move cursor to next line, accounting for text height if not in editing mode
                            if (!isEditing)
                            {
                                float lineSpacing = 4f; // Add spacing between coordinate entries
                                float nextLineY = layoutInfo.LineStartY + Math.Max(layoutInfo.TextHeight, ImGui.GetFrameHeight()) + lineSpacing;
                                ImGui.SetCursorPosY(nextLineY);
                            }
                        }
                    }
                }
                else
                {
                    ImGui.TextUnformatted(Strings.NoCoordinates);
                }

                // Display trash bin section if there are deleted coordinates and route is not optimized
                if (plugin.TreasureHuntService.DeletedCoordinates.Count > 0 && !plugin.TreasureHuntService.IsRouteOptimized)
                {
                    ImGui.Separator();

                    // Display trash bin title with count and clear button on the same line
                    ImGui.TextUnformatted(Strings.Messages.TrashBinWithCount(plugin.TreasureHuntService.DeletedCoordinates.Count));

                    // Clear trash button - positioned to the right of the title with adequate spacing
                    ImGui.SameLine();
                    if (ImGui.SmallButton(Strings.ClearTrash))
                    {
                        plugin.TreasureHuntService.ClearTrash();
                    }

                    // Calculate optimal player name column width for deleted coordinates
                    float deletedPlayerNameColumnWidth = UIHelper.CalculatePlayerNameColumnWidth(plugin.TreasureHuntService.DeletedCoordinates, showPlayerName: true);

                    // Display deleted coordinates with unified layout
                    for (var i = 0; i < plugin.TreasureHuntService.DeletedCoordinates.Count; i++)
                    {
                        var coord = plugin.TreasureHuntService.DeletedCoordinates[i];

                        // Use column-aligned rendering for better visual organization
                        // Include MapArea for deleted coordinates
                        float availableWidth = ImGui.GetContentRegionAvail().X;
                        int buttonCount = 1; // Only restore button

                        var layoutInfo = UIHelper.RenderCoordinateEntryWithColumns(
                            coord,
                            i + 1,
                            deletedPlayerNameColumnWidth,
                            showPlayerName: true,
                            showMapArea: true,
                            isCollected: true, // Use collected styling for deleted items
                            availableWidth,
                            buttonCount);

                        // Position Restore button with consistent right margin
                        float restoreButtonWidth = UIHelper.CalculateButtonWidth(Strings.Restore);
                        float rightMargin = 15f;
                        float trashWindowWidth = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();
                        float restoreButtonX = trashWindowWidth - rightMargin - restoreButtonWidth;

                        ImGui.SetCursorPos(new Vector2(restoreButtonX, layoutInfo.LineStartY));
                        if (ImGui.SmallButton(Strings.Restore + $"##trash{i}"))
                        {
                            plugin.TreasureHuntService.RestoreCoordinate(i);
                        }

                        // Move cursor to next line, accounting for text height with additional spacing
                        float lineSpacing = 4f; // Add spacing between coordinate entries
                        float nextLineY = layoutInfo.LineStartY + Math.Max(layoutInfo.TextHeight, ImGui.GetFrameHeight()) + lineSpacing;
                        ImGui.SetCursorPosY(nextLineY);
                    }
                }
                else if (!plugin.TreasureHuntService.IsRouteOptimized && ImGui.CollapsingHeader(Strings.TrashBin))
                {
                    ImGui.TextUnformatted(Strings.EmptyTrashBin);
                }
            }
        }
        
        // End disabled group if player is not logged in
        if (!isLoggedIn)
        {
            ImGui.EndDisabled();
        }
    }

    /// <summary>
    /// Renders action buttons with adaptive wrapping layout to prevent button truncation in different languages.
    /// </summary>
    /// <param name="isMonitoring">Whether monitoring is currently active</param>
    private void RenderActionButtons(bool isMonitoring)
    {
        float availableWidth = ImGui.GetContentRegionAvail().X;
        float buttonSpacing = 8f;
        float lineSpacing = 4f; // Compact line spacing
        float currentLineWidth = 0f;
        bool isFirstButton = true;
        float startY = ImGui.GetCursorPosY();
        float currentY = startY;

        // Helper function to render a button and handle wrapping
        void RenderButtonWithWrap(string buttonText, System.Action buttonAction, bool isDisabled = false, string? tooltip = null)
        {
            float buttonWidth = UIHelper.CalculateButtonWidth(buttonText);

            // Check if button fits on current line (accounting for spacing)
            float neededWidth = buttonWidth + (isFirstButton ? 0 : buttonSpacing);

            if (!isFirstButton && currentLineWidth + neededWidth > availableWidth)
            {
                // Start new line with compact spacing
                currentY += ImGui.GetFrameHeight() + lineSpacing;
                ImGui.SetCursorPos(new Vector2(ImGui.GetCursorStartPos().X, currentY));
                currentLineWidth = 0f;
                isFirstButton = true;
            }

            // Add spacing if not first button on line
            if (!isFirstButton)
            {
                ImGui.SameLine();
            }

            // Render button
            if (isDisabled)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button(buttonText))
            {
                buttonAction();
            }

            if (isDisabled)
            {
                ImGui.EndDisabled();

                // Show tooltip if provided
                if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(tooltip);
                }
            }

            // Update line width tracking
            currentLineWidth += neededWidth;
            isFirstButton = false;
        }

        // Render monitoring button
        if (isMonitoring)
        {
            RenderButtonWithWrap(Strings.StopMonitoring, () => plugin.ChatMonitorService.StopMonitoring());
        }
        else
        {
            RenderButtonWithWrap(Strings.StartMonitoring, () => plugin.ChatMonitorService.StartMonitoring());
        }

        // Render clear all button
        RenderButtonWithWrap(Strings.ClearAll, () => {
            plugin.TreasureHuntService.ClearCoordinates();

            // Reset route optimization if it was active
            if (plugin.TreasureHuntService.IsRouteOptimized)
            {
                plugin.TreasureHuntService.ResetRouteOptimization();
            }

            ClearEditingStates(); // Clear editing states when coordinates are cleared
        });

        // Render optimize/reset button
        if (plugin.TreasureHuntService.IsRouteOptimized)
        {
            RenderButtonWithWrap(Strings.ResetOptimization, () => {
                plugin.TreasureHuntService.ResetRouteOptimization();
                ClearEditingStates(); // Clear editing states when optimization is reset
            });
        }
        else
        {
            bool hasCoordinates = plugin.TreasureHuntService.Coordinates.Count > 0;
            RenderButtonWithWrap(Strings.OptimizeRoute,
                () => plugin.TreasureHuntService.OptimizeRoute(),
                !hasCoordinates,
                Strings.NoCoordinatesToOptimize);
        }

        // Render export button
        RenderButtonWithWrap(Strings.Export, () => {
            var exportedData = plugin.TreasureHuntService.ExportCoordinates();
            if (!string.IsNullOrEmpty(exportedData))
            {
                ImGui.SetClipboardText(exportedData);
                Plugin.Log.Information(Strings.Status.CoordinatesExportedToClipboard);
            }
        });

        // Render import button
        RenderButtonWithWrap(Strings.Import, () => {
            var clipboardText = ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                var importedCount = plugin.TreasureHuntService.ImportCoordinates(clipboardText);
                if (importedCount > 0)
                {
                    Plugin.Log.Information(Strings.Messages.CoordinatesImportedFromClipboard(importedCount));
                }
                else
                {
                    Plugin.Log.Information(Strings.Status.NoCoordinatesImported);
                }
            }
            else
            {
                Plugin.Log.Information(Strings.ClipboardEmpty);
            }
        });
    }

    private void OnCoordinatesImported(object? sender, int count)
    {
        Plugin.Log.Information(Strings.Messages.CoordinatesImported(count));
        ClearEditingStates(); // Clear editing states when coordinates are imported
    }

    private void OnRouteOptimized(object? sender, int count)
    {
        Plugin.Log.Information(Strings.Messages.RouteOptimized(count));
        ClearEditingStates(); // Clear editing states when route is optimized
    }

    /// <summary>
    /// Clears all editing states when coordinate list changes.
    /// Only affects raw coordinate editing since optimized route doesn't support editing.
    /// </summary>
    private void ClearEditingStates()
    {
        editingStates.Clear();
        editingPlayerNames.Clear();
        editingXCoords.Clear();
        editingYCoords.Clear();
    }
    
    // Handles the MessageTemplateUpdated event from CustomMessageWindow
    private void OnMessageTemplateUpdated(object? sender, EventArgs e)
    {
        // No specific action needed here as the Draw method will call GeneratePreviewMessage
        // which reads directly from the configuration. The UI will be refreshed on the next frame.
    }
    
    // Generates a preview of the message that will be sent
    private string GeneratePreviewMessage()
    {
        // Get the components to preview - either from active template or from selected components
        List<MessageComponent> componentsToPreview;
        
        // Check if there's an active template
        if (plugin.Configuration.ActiveTemplateIndex >= 0 && 
            plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            // Use components from the active template
            componentsToPreview = plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Components;
            
            // If the active template has no components, show only coordinate message
            if (componentsToPreview.Count == 0)
            {
                return Strings.Status.CoordinateOnlyMessage;
            }
        }
        else
        {
            // No active template, use selected components
            componentsToPreview = plugin.Configuration.SelectedMessageComponents;

            // If no selected components, show only coordinate message
            if (componentsToPreview.Count == 0)
            {
                return Strings.Status.CoordinateOnlyMessage;
            }
        }
        
        var previewParts = new List<string>();
        
        foreach (var component in componentsToPreview)
        {
            switch (component.Type)
            {
                case MessageComponentType.PlayerName:
                    // Use a localized player name example for better preview
                    previewParts.Add(Strings.PlayerNameExample);
                    break;
                case MessageComponentType.Coordinates:
                    // Use a localized map location example with special LinkMarker character from SeIconChar
                    // Use client language for LocationExample to match what will actually be sent to chat
                    string linkMarker = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.LinkMarker);
                    string clientLocationExample = LocalizationManager.GetClientLanguageLocationExample();
                    previewParts.Add($"{linkMarker} {clientLocationExample}");
                    break;
                case MessageComponentType.Number:
                    // Show specific Number1 special character using the actual Unicode value from SeIconChar
                    string number1 = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.Number1);
                    previewParts.Add(number1);
                    break;
                case MessageComponentType.BoxedNumber:
                    // Show specific BoxedNumber1 special character using the actual Unicode value from SeIconChar
                    string boxedNumber1 = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.BoxedNumber1);
                    previewParts.Add(boxedNumber1);
                    break;
                case MessageComponentType.BoxedOutlinedNumber:
                    // Show specific BoxedOutlinedNumber1 special character using the actual Unicode value from SeIconChar
                    string boxedOutlinedNumber1 = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.BoxedOutlinedNumber1);
                    previewParts.Add(boxedOutlinedNumber1);
                    break;
                case MessageComponentType.CustomMessage:
                    if (component.CustomMessageIndex >= 0 && component.CustomMessageIndex < plugin.Configuration.CustomMessages.Count)
                    {
                        previewParts.Add(plugin.Configuration.CustomMessages[component.CustomMessageIndex]);
                    }
                    break;
            }
        }
        
        return string.Join(" ", previewParts);
    }

}
