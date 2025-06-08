using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Linq;
using OnePiece.Localization;
using OnePiece.Models;

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

    // We don't need a local reference to CustomMessageWindow anymore
    // since we'll use the one from Plugin instance

    public MainWindow(Plugin plugin)
        : base(LocalizationManager.GetString("MainWindowTitle") + "##OnePiece", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
            LocalizationManager.GetString("Say"),
            LocalizationManager.GetString("Yell"),
            LocalizationManager.GetString("Shout"),
            LocalizationManager.GetString("Party"),
            LocalizationManager.GetString("Alliance"),
            LocalizationManager.GetString("FreeCompany"),
            LocalizationManager.GetString("LinkShell1"),
            LocalizationManager.GetString("LinkShell2"),
            LocalizationManager.GetString("LinkShell3"),
            LocalizationManager.GetString("LinkShell4"),
            LocalizationManager.GetString("LinkShell5"),
            LocalizationManager.GetString("LinkShell6"),
            LocalizationManager.GetString("LinkShell7"),
            LocalizationManager.GetString("LinkShell8"),
            LocalizationManager.GetString("CrossWorldLinkShell1"),
            LocalizationManager.GetString("CrossWorldLinkShell2"),
            LocalizationManager.GetString("CrossWorldLinkShell3"),
            LocalizationManager.GetString("CrossWorldLinkShell4"),
            LocalizationManager.GetString("CrossWorldLinkShell5"),
            LocalizationManager.GetString("CrossWorldLinkShell6"),
            LocalizationManager.GetString("CrossWorldLinkShell7"),
            LocalizationManager.GetString("CrossWorldLinkShell8")
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
                cachedNotLoggedInMessage = LocalizationManager.GetString("NotLoggedIn");
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

        // Calculate optimal width for labels and controls dynamically based on content
        float maxLabelWidth = 0;
        
        // Calculate the width needed for the longest label
        string[] labelsToMeasure = new string[] {
            LocalizationManager.GetString("Language"),
            LocalizationManager.GetString("SelectChatChannel")
        };
        
        foreach (var label in labelsToMeasure)
        {
            float width = ImGui.CalcTextSize(label).X;
            maxLabelWidth = Math.Max(maxLabelWidth, width);
        }
        
        // Add padding to the calculated width
        float labelWidth = maxLabelWidth + 40; // Add padding for comfortable spacing
        // Ensure a minimum width
        labelWidth = Math.Max(labelWidth, 180);
        // Cap the maximum width to avoid taking too much space
        labelWidth = Math.Min(labelWidth, 260);
        
        float controlWidth = 250; // Keep the same control width

        // General Settings section with collapsing header
        if (ImGui.CollapsingHeader(LocalizationManager.GetString("GeneralSettings")))
        {
            // Language selection
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(LocalizationManager.GetString("Language"));
            ImGui.SameLine(labelWidth);
            ImGui.SetNextItemWidth(controlWidth);
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


        }

        ImGui.Separator();

        // Channel settings section with collapsing header
        if (ImGui.CollapsingHeader(LocalizationManager.GetString("ChannelSettings")))
        {
            // Chat channel selection - moved to be inline with the label
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(LocalizationManager.GetString("SelectChatChannel"));
            ImGui.SameLine(labelWidth);
            ImGui.SetNextItemWidth(controlWidth);
            
            // Get monitoring status
            bool isMonitoring = plugin.ChatMonitorService.IsMonitoring;
            
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

            // Monitoring control buttons (without status display)
            if (isMonitoring)
            {
                if (ImGui.Button(LocalizationManager.GetString("StopMonitoring"), new Vector2(150, 0)))
                {
                    plugin.ChatMonitorService.StopMonitoring();
                }
            }
            else
            {
                if (ImGui.Button(LocalizationManager.GetString("StartMonitoring"), new Vector2(150, 0)))
                {
                    plugin.ChatMonitorService.StartMonitoring();
                }
            }

            // End of ChannelSettings section
        }
        
        ImGui.Separator();

        // Message settings section with collapsing header (parallel to ChannelSettings)
        if (ImGui.CollapsingHeader(LocalizationManager.GetString("MessageSettings")))
        {
            // Button to open custom message settings window
            float buttonWidth = 200; // Fixed width for the button
            
            if (ImGui.Button(LocalizationManager.GetString("OpenSettingsWindow"), new Vector2(buttonWidth, 0)))
            {
                plugin.ShowCustomMessageWindow();
            }
            
            ImGui.Spacing();
            
            // Show active template information if there is one
            if (plugin.Configuration.ActiveTemplateIndex >= 0 && 
                plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count)
            {
                string templateName = plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Name;
                ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.0f, 1.0f), string.Format(LocalizationManager.GetString("CurrentActiveTemplate"), templateName));
                
                // Preview of the active template
                ImGui.Spacing();
                ImGui.Text(LocalizationManager.GetString("MessagePreview"));
                string previewMessage = GeneratePreviewMessage();
                ImGui.TextWrapped(previewMessage);
            }
            else
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), LocalizationManager.GetString("NoActiveMessageTemplate"));
                
                // If there are components but no template, still show preview
                if (plugin.Configuration.SelectedMessageComponents.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Text(LocalizationManager.GetString("MessagePreview"));
                    string previewMessage = GeneratePreviewMessage();
                    ImGui.TextWrapped(previewMessage);
                }
            }
        }

        ImGui.Separator();

        // Action buttons
        if (ImGui.Button(LocalizationManager.GetString("ClearAll")))
        {
            plugin.TreasureHuntService.ClearCoordinates();
        }

        ImGui.SameLine();

        if (plugin.TreasureHuntService.IsRouteOptimized)
        {
            if (ImGui.Button(LocalizationManager.GetString("ResetOptimization")))
            {
                plugin.TreasureHuntService.ResetRouteOptimization();
            }
        }
        else
        {
            // 检查坐标列表是否为空，如果为空则禁用优化路径按钮
            bool hasCoordinates = plugin.TreasureHuntService.Coordinates.Count > 0;
            
            if (!hasCoordinates)
            {
                ImGui.BeginDisabled();
            }
            
            if (ImGui.Button(LocalizationManager.GetString("OptimizeRoute")))
            {
                plugin.TreasureHuntService.OptimizeRoute();
            }
            
            if (!hasCoordinates)
            {
                ImGui.EndDisabled();
                
                // 显示悬停提示，说明为什么按钮被禁用
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(LocalizationManager.GetString("NoCoordinatesToOptimize"));
                }
            }
        }

        ImGui.SameLine();

        // Export button
        if (ImGui.Button(LocalizationManager.GetString("Export")))
        {
            var exportedData = plugin.TreasureHuntService.ExportCoordinates();
            if (!string.IsNullOrEmpty(exportedData))
            {
                ImGui.SetClipboardText(exportedData);
                Plugin.ChatGui.Print(LocalizationManager.GetString("CoordinatesExportedToClipboard"));
            }
        }

        ImGui.SameLine();

        // Import button
        if (ImGui.Button(LocalizationManager.GetString("Import")))
        {
            var clipboardText = ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                var importedCount = plugin.TreasureHuntService.ImportCoordinates(clipboardText);
                if (importedCount > 0)
                {
                    Plugin.ChatGui.Print(string.Format(LocalizationManager.GetString("CoordinatesImportedFromClipboard"), importedCount));
                }
                else
                {
                    Plugin.ChatGui.Print(LocalizationManager.GetString("NoCoordinatesImported"));
                }
            }
            else
            {
                Plugin.ChatGui.Print(LocalizationManager.GetString("ClipboardEmpty"));
            }
        }

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
                        ImGui.TextUnformatted(string.Format(LocalizationManager.GetString("OptimizedRouteWithCount"), treasurePointCount));

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
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), LocalizationManager.GetString("UnknownArea"));
                            }

                            // Get coordinates for this map area while preserving the original order
                            // This is more efficient than using Where().ToList() on every frame
                            var mapAreaCoords = mapGroup.Value;
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

                                // Display player name and coordinates with correct numbering
                                var displayText = $"{realIndex}. ";
                                if (!string.IsNullOrEmpty(coord.PlayerName))
                                {
                                    displayText += $"{coord.PlayerName}: ";
                                }

                                displayText += $"({coord.X:F1}, {coord.Y:F1})";

                                if (!string.IsNullOrEmpty(coord.Name))
                                {
                                    displayText += $" - {coord.Name}";
                                }

                                // Check if this coordinate has AetheryteId for teleportation
                                bool hasTeleportId = coord.AetheryteId > 0;



                                // Get current button texts from localization
                                string teleportText = LocalizationManager.GetString("TeleportButton");
                                string chatText = LocalizationManager.GetString("SendToChat");
                                string collectedText = LocalizationManager.GetString("Collected");

                                // Calculate actual text widths for current language
                                float teleportTextWidth = ImGui.CalcTextSize(teleportText).X;
                                float chatTextWidth = ImGui.CalcTextSize(chatText).X;
                                float collectedTextWidth = ImGui.CalcTextSize(collectedText).X;

                                // Add padding for button content (ImGui internal padding + extra space)
                                float buttonPadding = 24f;
                                float teleportButtonWidth = Math.Max(teleportTextWidth + buttonPadding, 80f); // Minimum 80px
                                float chatButtonWidth = Math.Max(chatTextWidth + buttonPadding, 100f); // Minimum 100px
                                float collectedButtonWidth = Math.Max(collectedTextWidth + buttonPadding, 80f); // Minimum 80px

                                // Use inline layout with consistent spacing like top buttons
                                ImGui.TextUnformatted(displayText);

                                // Add consistent spacing before buttons (same as top buttons)
                                ImGui.SameLine();

                                // Add minimal spacing to separate text from buttons (reduced from 10f to 5f for tighter layout)
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);

                                if (hasTeleportId)
                                {
                                    // Get aetheryte information directly using ID
                                    var aetheryteInfo = plugin.AetheryteService.GetAetheryteById(coord.AetheryteId);
                                    int teleportPrice = 0;

                                    if (aetheryteInfo != null)
                                    {
                                        // Calculate teleport price using aetheryte info
                                        teleportPrice = plugin.AetheryteService.CalculateTeleportPrice(aetheryteInfo);

                                        // Disable teleport button if coordinate is collected
                                        if (isCollected)
                                        {
                                            ImGui.BeginDisabled();
                                        }

                                        // Add teleport button
                                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 1.0f, 0.7f));
                                        if (ImGui.SmallButton($"{LocalizationManager.GetString("TeleportButton")}##{optimizedRoute.IndexOf(coord)}"))
                                        {
                                            // Teleport directly using the aetheryte info
                                            plugin.AetheryteService.TeleportToAetheryte(aetheryteInfo);
                                        }

                                        // Add tooltip with teleport information
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.BeginTooltip();
                                            ImGui.Text(string.Format(LocalizationManager.GetString("TeleportTo"), aetheryteInfo.Name));
                                            if (teleportPrice > 0)
                                            {
                                                ImGui.Text(string.Format(LocalizationManager.GetString("TeleportCost"), teleportPrice));
                                            }
                                            ImGui.EndTooltip();
                                        }

                                        ImGui.PopStyleColor();

                                        // End disabled state if coordinate is collected
                                        if (isCollected)
                                        {
                                            ImGui.EndDisabled();
                                        }

                                        // Add consistent spacing between buttons (same as top buttons)
                                        ImGui.SameLine();
                                    }
                                }
                                else
                                {
                                    // If no teleport button, we don't need SameLine() here since we're already on the same line
                                    // Just continue with the next button
                                }

                                if (isCollected)
                                {
                                    ImGui.PopStyleColor();
                                }

                                // Send to Chat button - disable if the coordinate is already collected
                                if (isCollected)
                                {
                                    ImGui.BeginDisabled();
                                }

                                if (ImGui.SmallButton(LocalizationManager.GetString("SendToChat") + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    plugin.ChatMonitorService.SendCoordinateToChat(coord);
                                }

                                if (isCollected)
                                {
                                    ImGui.EndDisabled();
                                }

                                // Add consistent spacing between buttons (same as top buttons)
                                ImGui.SameLine();

                                // Collected button - toggle between collected and not collected
                                string collectedButtonText = coord.IsCollected ?
                                    LocalizationManager.GetString("MarkAsNotCollected") :
                                    LocalizationManager.GetString("Collected");

                                if (ImGui.SmallButton(collectedButtonText + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    // Toggle the collected state
                                    coord.IsCollected = !coord.IsCollected;
                                }

                                // No Delete button for optimized route
                            }
                        }
                    }
                    else
                    {
                        // Display the raw coordinates if no optimized route
                        ImGui.TextUnformatted(string.Format(LocalizationManager.GetString("CoordinatesWithCount"), coordinates.Count));

                        for (var i = 0; i < coordinates.Count; i++)
                        {
                            var coord = coordinates[i];

                            // Display coordinate with map area if available
                            string displayText = $"{i + 1}. ";
                            
                            // Display player name if available
                            if (!string.IsNullOrEmpty(coord.PlayerName))
                            {
                                displayText += $"{coord.PlayerName}: ";
                            }

                            // Add map area with colored text if available
                            if (!string.IsNullOrEmpty(coord.MapArea))
                            {
                                ImGui.TextUnformatted(displayText);
                                ImGui.SameLine(0, 0);
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), coord.MapArea);
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted($"({coord.X:F1}, {coord.Y:F1})");
                            }
                            else
                            {
                                ImGui.TextUnformatted($"{displayText}({coord.X:F1}, {coord.Y:F1})");
                            }

                            // No Collected button for raw coordinates

                            ImGui.SameLine();

                            if (ImGui.SmallButton(LocalizationManager.GetString("Delete") + $"##raw{i}"))
                            {
                                plugin.TreasureHuntService.DeleteCoordinate(i);
                            }
                        }
                    }
                }
                else
                {
                    ImGui.TextUnformatted(LocalizationManager.GetString("NoCoordinates"));
                }

                // Display trash bin section if there are deleted coordinates
                if (plugin.TreasureHuntService.DeletedCoordinates.Count > 0)
                {
                    ImGui.Separator();

                    // Display trash bin title with count
                    ImGui.TextUnformatted(string.Format(LocalizationManager.GetString("TrashBinWithCount"), plugin.TreasureHuntService.DeletedCoordinates.Count));

                    // Clear trash button
                    ImGui.SameLine(ImGui.GetWindowWidth() - 100);
                    if (ImGui.SmallButton(LocalizationManager.GetString("ClearTrash")))
                    {
                        plugin.TreasureHuntService.ClearTrash();
                    }

                    // Display deleted coordinates
                    for (var i = 0; i < plugin.TreasureHuntService.DeletedCoordinates.Count; i++)
                    {
                        var coord = plugin.TreasureHuntService.DeletedCoordinates[i];

                        // Display with grayed out style
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

                        // Display coordinate info
                        var displayText = $"{i + 1}. ";

                        // Display player name if available
                        if (!string.IsNullOrEmpty(coord.PlayerName))
                        {
                            displayText += $"{coord.PlayerName}: ";
                        }

                        ImGui.TextUnformatted(displayText);

                        // Add map area with colored text if available
                        if (!string.IsNullOrEmpty(coord.MapArea))
                        {
                            ImGui.SameLine(0, 0);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.6f, 0.8f, 1.0f)); // Slightly darker blue for deleted items
                            ImGui.TextUnformatted(coord.MapArea);
                            ImGui.PopStyleColor();
                            ImGui.SameLine(0, 5);
                            ImGui.TextUnformatted($"({coord.X:F1}, {coord.Y:F1})");

                            // Add name if available
                            if (!string.IsNullOrEmpty(coord.Name))
                            {
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted($"- {coord.Name}");
                            }
                        }
                        else
                        {
                            // No map area, display coordinates and name directly
                            ImGui.SameLine(0, 0);
                            ImGui.TextUnformatted($"({coord.X:F1}, {coord.Y:F1})");

                            // Add name if available
                            if (!string.IsNullOrEmpty(coord.Name))
                            {
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted($"- {coord.Name}");
                            }
                        }

                        ImGui.PopStyleColor(); // Pop the gray style

                        ImGui.SameLine();

                        // Restore button
                        if (ImGui.SmallButton(LocalizationManager.GetString("Restore") + $"##trash{i}"))
                        {
                            plugin.TreasureHuntService.RestoreCoordinate(i);
                        }
                    }
                }
                else if (ImGui.CollapsingHeader(LocalizationManager.GetString("TrashBin")))
                {
                    ImGui.TextUnformatted(LocalizationManager.GetString("EmptyTrashBin"));
                }
            }
        }
        
        // End disabled group if player is not logged in
        if (!isLoggedIn)
        {
            ImGui.EndDisabled();
        }
    }

    private void OnCoordinatesImported(object? sender, int count)
    {
        Plugin.ChatGui.Print(string.Format(LocalizationManager.GetString("CoordinatesImported"), count));
    }

    private void OnRouteOptimized(object? sender, int count)
    {
        Plugin.ChatGui.Print(string.Format(LocalizationManager.GetString("RouteOptimized"), count));
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
                return LocalizationManager.GetString("SendCoordinateOnly");
            }
        }
        else
        {
            // No active template, use selected components
            componentsToPreview = plugin.Configuration.SelectedMessageComponents;

            // If no selected components, show only coordinate message
            if (componentsToPreview.Count == 0)
            {
                return LocalizationManager.GetString("SendCoordinateOnly");
            }
        }
        
        var previewParts = new List<string>();
        
        foreach (var component in componentsToPreview)
        {
            switch (component.Type)
            {
                case MessageComponentType.PlayerName:
                    // Use a localized player name example for better preview
                    previewParts.Add(LocalizationManager.GetString("PlayerNameExample"));
                    break;
                case MessageComponentType.Coordinates:
                    // Use a localized map location example with special LinkMarker character from SeIconChar
                    string linkMarker = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.LinkMarker);
                    previewParts.Add($"{linkMarker} {LocalizationManager.GetString("LocationExample")}");
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
