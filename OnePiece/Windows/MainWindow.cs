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
    private string[] logLevels = Array.Empty<string>();
    private int selectedLanguageIndex;
    private int selectedChatChannelIndex;
    private int selectedLogLevelIndex;
    
    // We don't need a local reference to CustomMessageWindow anymore
    // since we'll use the one from Plugin instance

    public MainWindow(Plugin plugin)
        : base(Strings.GetString("MainWindowTitle") + "##OnePiece", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
        supportedLanguages = Strings.GetSupportedLanguages();
        selectedLanguageIndex = Array.IndexOf(supportedLanguages, this.plugin.Configuration.Language);
        if (selectedLanguageIndex < 0) selectedLanguageIndex = 0;

        // Initialize log level selection
        InitializeLogLevels();
        selectedLogLevelIndex = (int)this.plugin.Configuration.LogLevel;

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
            Strings.GetString("Say"),
            Strings.GetString("Yell"),
            Strings.GetString("Shout"),
            Strings.GetString("Party"),
            Strings.GetString("Alliance"),
            Strings.GetString("FreeCompany"),
            Strings.GetString("LinkShell1"),
            Strings.GetString("LinkShell2"),
            Strings.GetString("LinkShell3"),
            Strings.GetString("LinkShell4"),
            Strings.GetString("LinkShell5"),
            Strings.GetString("LinkShell6"),
            Strings.GetString("LinkShell7"),
            Strings.GetString("LinkShell8"),
            Strings.GetString("CrossWorldLinkShell1"),
            Strings.GetString("CrossWorldLinkShell2"),
            Strings.GetString("CrossWorldLinkShell3"),
            Strings.GetString("CrossWorldLinkShell4"),
            Strings.GetString("CrossWorldLinkShell5"),
            Strings.GetString("CrossWorldLinkShell6"),
            Strings.GetString("CrossWorldLinkShell7"),
            Strings.GetString("CrossWorldLinkShell8")
        };
    }

    private void InitializeLogLevels()
    {
        // Create localized log level names
        logLevels = new[]
        {
            Strings.GetString("LogLevelMinimal"),
            Strings.GetString("LogLevelNormal"),
            Strings.GetString("LogLevelVerbose")
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
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.0f, 1.0f)); // Yellow warning text
            string message = Strings.GetString("NotLoggedIn");
            float warningWidth = ImGui.CalcTextSize(message).X;
            ImGui.SetCursorPosX((windowWidth - warningWidth) * 0.5f);
            ImGui.TextUnformatted(message);
            ImGui.PopStyleColor();
            
            ImGui.Separator();
        }

        // Display subtitle (centered)
        string subtitle = Strings.GetString("MainWindowSubtitle");
        float textWidth = ImGui.CalcTextSize(subtitle).X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
        ImGui.TextUnformatted(subtitle);

        ImGui.Separator();
        
        // Begin disabled group if player is not logged in
        if (!isLoggedIn)
        {
            ImGui.BeginDisabled();
        }

        // Calculate optimal width for labels and controls dynamically based on content
        float maxLabelWidth = 0;
        
        // Calculate the width needed for the longest label
        string[] labelsToMeasure = new string[] {
            Strings.GetString("Language"),
            Strings.GetString("LogLevel"),
            Strings.GetString("SelectChatChannel")
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
        if (ImGui.CollapsingHeader(Strings.GetString("GeneralSettings")))
        {
            // Language selection
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Strings.GetString("Language"));
            ImGui.SameLine(labelWidth);
            ImGui.SetNextItemWidth(controlWidth);
            if (ImGui.Combo("##LanguageSelector", ref selectedLanguageIndex, supportedLanguages, supportedLanguages.Length))
            {
                plugin.Configuration.Language = supportedLanguages[selectedLanguageIndex];
                Strings.SetLanguage(plugin.Configuration.Language);
                plugin.Configuration.Save();

                // Refresh localized strings
                InitializeChatChannelNames();
                InitializeLogLevels();
            }

            // Log level
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Strings.GetString("LogLevel"));
            ImGui.SameLine(labelWidth);
            ImGui.SetNextItemWidth(controlWidth);
            if (ImGui.Combo("##LogLevelSelector", ref selectedLogLevelIndex, logLevels, logLevels.Length))
            {
                plugin.Configuration.LogLevel = (LogLevel)selectedLogLevelIndex;
                plugin.Configuration.Save();
            }

            // Show tooltip for the selected log level
            if (ImGui.IsItemHovered())
            {
                switch (plugin.Configuration.LogLevel)
                {
                    case LogLevel.Minimal:
                        ImGui.SetTooltip(Strings.GetString("LogLevelMinimalTooltip"));
                        break;
                    case LogLevel.Normal:
                        ImGui.SetTooltip(Strings.GetString("LogLevelNormalTooltip"));
                        break;
                    case LogLevel.Verbose:
                        ImGui.SetTooltip(Strings.GetString("LogLevelVerboseTooltip"));
                        break;
                }
            }
        }

        ImGui.Separator();

        // Channel settings section with collapsing header
        if (ImGui.CollapsingHeader(Strings.GetString("ChannelSettings")))
        {
            // Chat channel selection - moved to be inline with the label
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(Strings.GetString("SelectChatChannel"));
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
                if (ImGui.Button(Strings.GetString("StopMonitoring"), new Vector2(150, 0)))
                {
                    plugin.ChatMonitorService.StopMonitoring();
                }
            }
            else
            {
                if (ImGui.Button(Strings.GetString("StartMonitoring"), new Vector2(150, 0)))
                {
                    plugin.ChatMonitorService.StartMonitoring();
                }
            }
            
            // End of ChannelSettings section
        }
        
        ImGui.Separator();
        
        // Message settings section with collapsing header (parallel to ChannelSettings)
        if (ImGui.CollapsingHeader(Strings.GetString("MessageSettings")))
        {
            // Button to open custom message settings window
            float buttonWidth = 200; // Fixed width for the button
            
            if (ImGui.Button(Strings.GetString("OpenSettingsWindow"), new Vector2(buttonWidth, 0)))
            {
                // Force show the window through the plugin instance which owns it
                plugin.ShowCustomMessageWindow();
                Plugin.Log.Information("Opening Custom Message Window");
            }
            
            ImGui.Spacing();
            
            // Show active template information if there is one
            if (plugin.Configuration.ActiveTemplateIndex >= 0 && 
                plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count)
            {
                string templateName = plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Name;
                ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.0f, 1.0f), string.Format(Strings.GetString("CurrentActiveTemplate"), templateName));
                
                // Preview of the active template
                ImGui.Spacing();
                ImGui.Text(Strings.GetString("MessagePreview"));
                string previewMessage = GeneratePreviewMessage();
                ImGui.TextWrapped(previewMessage);
            }
            else
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Strings.GetString("NoActiveMessageTemplate"));
                
                // If there are components but no template, still show preview
                if (plugin.Configuration.SelectedMessageComponents.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Text(Strings.GetString("MessagePreview"));
                    string previewMessage = GeneratePreviewMessage();
                    ImGui.TextWrapped(previewMessage);
                }
            }
        }

        ImGui.Separator();

        // Action buttons
        if (ImGui.Button(Strings.GetString("ClearAll")))
        {
            plugin.TreasureHuntService.ClearCoordinates();
        }

        ImGui.SameLine();

        if (plugin.TreasureHuntService.IsRouteOptimized)
        {
            if (ImGui.Button(Strings.GetString("ResetOptimization")))
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
            
            if (ImGui.Button(Strings.GetString("OptimizeRoute")))
            {
                plugin.TreasureHuntService.OptimizeRoute();
            }
            
            if (!hasCoordinates)
            {
                ImGui.EndDisabled();
                
                // 显示悬停提示，说明为什么按钮被禁用
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.GetString("NoCoordinatesToOptimize"));
                }
            }
        }

        ImGui.SameLine();

        // Export button
        if (ImGui.Button(Strings.GetString("Export")))
        {
            var exportedData = plugin.TreasureHuntService.ExportCoordinates();
            if (!string.IsNullOrEmpty(exportedData))
            {
                ImGui.SetClipboardText(exportedData);
                Plugin.ChatGui.Print(Strings.GetString("CoordinatesExportedToClipboard"));
            }
        }

        ImGui.SameLine();

        // Import button
        if (ImGui.Button(Strings.GetString("Import")))
        {
            var clipboardText = ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                var importedCount = plugin.TreasureHuntService.ImportCoordinates(clipboardText);
                if (importedCount > 0)
                {
                    Plugin.ChatGui.Print(string.Format(Strings.GetString("CoordinatesImportedFromClipboard"), importedCount));
                }
                else
                {
                    Plugin.ChatGui.Print(Strings.GetString("NoCoordinatesImported"));
                }
            }
            else
            {
                Plugin.ChatGui.Print(Strings.GetString("ClipboardEmpty"));
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
                        // Count non-teleport coordinates for display
                        int nonTeleportCount = 0;
                        foreach (var coord in optimizedRoute)
                        {
                            if (coord.Name == null || !coord.Name.Contains("[Teleport]"))
                            {
                                nonTeleportCount++;
                            }
                        }
                        
                        // Display optimized route title with count (excluding teleport points)
                        ImGui.TextUnformatted(string.Format(Strings.GetString("OptimizedRouteWithCount"), nonTeleportCount));

                        // Group coordinates by map area - optimize by using a more efficient approach
                        // Pre-allocate the dictionary with expected capacity to avoid resizing
                        var uniqueMapAreas = new HashSet<string>(optimizedRoute.Select(c => c.MapArea));
                        var coordinatesByMap = new Dictionary<string, List<TreasureCoordinate>>(uniqueMapAreas.Count);

                        // Manually group coordinates to avoid multiple LINQ operations
                        foreach (var mapArea in uniqueMapAreas)
                        {
                            coordinatesByMap[mapArea] = new List<TreasureCoordinate>();
                        }

                        // Fill the groups in a single pass through the coordinates
                        foreach (var coord in optimizedRoute)
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
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), Strings.GetString("UnknownArea"));
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

                                // Check if this is a teleport point coordinate (teleport point name contains [Teleport])
                                bool isTeleportPoint = coord.Name != null && coord.Name.Contains("[Teleport]");

                                // If it's a teleport point, skip displaying it but pass its information to the next coordinate
                                if (isTeleportPoint)
                                {
                                    // Store teleport info to add to the next coordinate
                                    if (i + 1 < mapAreaCoords.Count)
                                    {
                                        var nextCoord = mapAreaCoords[i + 1];
                                        // Extract teleport information
                                        string aetheryteName = "";
                                        if (coord.Name.StartsWith("[Teleport] "))
                                        {
                                            aetheryteName = coord.Name.Substring("[Teleport] ".Length);
                                            // Store teleport info in the next coordinate's Tag property
                                            nextCoord.Tag = $"[Teleport to {aetheryteName}]";
                                        }
                                    }
                                    // Skip displaying teleport points
                                    continue;
                                }

                                // Calculate the actual index (only counting non-teleport coordinates)
                                int realIndex = 0; // Start from 0 and increment first
                                for (int j = 0; j <= optimizedRoute.IndexOf(coord); j++)
                                {
                                    var checkCoord = optimizedRoute[j];
                                    if (!(checkCoord.Name != null && checkCoord.Name.Contains("[Teleport]")))
                                    {
                                        realIndex++;
                                    }
                                }

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

                                ImGui.TextUnformatted(displayText);

                                // Check if this coordinate has teleport information
                                bool isTeleport = coord.NavigationInstruction != null && coord.NavigationInstruction.Contains("Teleport");
                                bool hasTeleportTag = !string.IsNullOrEmpty(coord.Tag);
                                
                                // Add teleport button if needed
                                if (isTeleport || hasTeleportTag)
                                {
                                    // Get teleport information
                                    string aetheryteName = "";
                                    int teleportPrice = 0;
                                    
                                    if (hasTeleportTag)
                                    {
                                        // Extract aetheryte name from tag
                                        if (coord.Tag.Contains("[Teleport to "))
                                        {
                                            aetheryteName = coord.Tag.Replace("[Teleport to ", "").Replace("]", "");
                                        }
                                    }
                                    else if (isTeleport)
                                    {
                                        // Extract aetheryte name from navigation instruction
                                        int teleportToIndex = coord.NavigationInstruction.IndexOf("to ");
                                        int endIndex = coord.NavigationInstruction.IndexOf(" (", teleportToIndex);
                                        if (teleportToIndex > 0 && endIndex > teleportToIndex)
                                        {
                                            aetheryteName = coord.NavigationInstruction.Substring(teleportToIndex + 3, endIndex - teleportToIndex - 3);
                                        }
                                    }
                                    
                                    // Calculate estimated teleport price based on distance (simplified calculation)
                                    if (!string.IsNullOrEmpty(aetheryteName))
                                    {
                                        // Find aetheryte information if available
                                        var aetheryteInfo = plugin.AetheryteService?.GetAetheryteByName(aetheryteName);
                                        if (aetheryteInfo != null)
                                        {
                                            teleportPrice = plugin.AetheryteService.CalculateTeleportPrice(aetheryteInfo);
                                        }
                                        else
                                        {
                                            // Fallback price estimate
                                            teleportPrice = 999; // Unknown price
                                        }
                                        
                                        // Add teleport button
                                        ImGui.SameLine();
                                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 1.0f, 0.7f));
                                        if (ImGui.SmallButton($"{Strings.GetString("TeleportButton")}##{optimizedRoute.IndexOf(coord)}"))
                                        {
                                            // Teleport action - can be implemented to call the teleport function
                                            if (aetheryteInfo != null)
                                            {
                                                plugin.AetheryteService.TeleportToAetheryte(aetheryteInfo);
                                            }
                                        }
                                        
                                        // Add tooltip with teleport information
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.BeginTooltip();
                                            ImGui.Text(string.Format(Strings.GetString("TeleportTo"), aetheryteName));
                                            if (teleportPrice > 0 && teleportPrice < 999)
                                            {
                                                ImGui.Text(string.Format(Strings.GetString("TeleportCost"), teleportPrice));
                                            }
                                            else if (teleportPrice >= 999)
                                            {
                                                ImGui.Text(Strings.GetString("TeleportCostUnknown"));
                                            }
                                            ImGui.EndTooltip();
                                        }
                                        
                                        ImGui.PopStyleColor();
                                    }
                                }

                                if (isCollected)
                                {
                                    ImGui.PopStyleColor();
                                }

                                ImGui.SameLine();

                                // Send to Chat button - disable if the coordinate is already collected
                                if (isCollected)
                                {
                                    ImGui.BeginDisabled();
                                }
                                
                                if (ImGui.SmallButton(Strings.GetString("SendToChat") + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    plugin.ChatMonitorService.SendCoordinateToChat(coord);
                                }
                                
                                if (isCollected)
                                {
                                    ImGui.EndDisabled();
                                }

                                ImGui.SameLine();

                                // Collected button
                                if (ImGui.SmallButton(Strings.GetString("Collected") + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    var index = plugin.TreasureHuntService.Coordinates.IndexOf(coord);
                                    if (index >= 0)
                                    {
                                        plugin.TreasureHuntService.MarkAsCollected(index);
                                    }
                                }

                                // No Delete button for optimized route
                            }
                        }
                    }
                    else
                    {
                        // Display the raw coordinates if no optimized route
                        ImGui.TextUnformatted(string.Format(Strings.GetString("CoordinatesWithCount"), coordinates.Count));

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

                            if (ImGui.SmallButton(Strings.GetString("Delete") + $"##raw{i}"))
                            {
                                plugin.TreasureHuntService.DeleteCoordinate(i);
                            }
                        }
                    }
                }
                else
                {
                    ImGui.TextUnformatted(Strings.GetString("NoCoordinates"));
                }

                // Display trash bin section if there are deleted coordinates
                if (plugin.TreasureHuntService.DeletedCoordinates.Count > 0)
                {
                    ImGui.Separator();

                    // Display trash bin title with count
                    ImGui.TextUnformatted(string.Format(Strings.GetString("TrashBinWithCount"), plugin.TreasureHuntService.DeletedCoordinates.Count));

                    // Clear trash button
                    ImGui.SameLine(ImGui.GetWindowWidth() - 100);
                    if (ImGui.SmallButton(Strings.GetString("ClearTrash")))
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
                        if (ImGui.SmallButton(Strings.GetString("Restore") + $"##trash{i}"))
                        {
                            plugin.TreasureHuntService.RestoreCoordinate(i);
                        }
                    }
                }
                else if (ImGui.CollapsingHeader(Strings.GetString("TrashBin")))
                {
                    ImGui.TextUnformatted(Strings.GetString("EmptyTrashBin"));
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
        // Only show log message if log level is Normal or higher
        if (plugin.Configuration.LogLevel >= LogLevel.Normal)
        {
            Plugin.ChatGui.Print(string.Format(Strings.GetString("CoordinatesImported"), count));
        }
    }

    private void OnRouteOptimized(object? sender, int count)
    {
        // Only show log message if log level is Normal or higher
        if (plugin.Configuration.LogLevel >= LogLevel.Normal)
        {
            Plugin.ChatGui.Print(string.Format(Strings.GetString("RouteOptimized"), count));
        }
    }
    
    // Handles the MessageTemplateUpdated event from CustomMessageWindow
    private void OnMessageTemplateUpdated(object? sender, EventArgs e)
    {
        // Log the event at debug level
        if (plugin.Configuration.LogLevel >= LogLevel.Verbose)
        {
            Plugin.Log.Debug("MainWindow received MessageTemplateUpdated event, refreshing preview");
        }
        
        // Note: No specific action needed here as the Draw method will call GeneratePreviewMessage
        // which reads directly from the configuration. The UI will be refreshed on the next frame.
        // If needed, we could force a redraw here, but the ImGui system will handle it automatically.
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
            
            // If the active template has no components, show only treasure marker message
            if (componentsToPreview.Count == 0)
            {
                return Strings.GetString("OnlyTreasureMarker");
            }
        }
        else
        {
            // No active template, use selected components
            componentsToPreview = plugin.Configuration.SelectedMessageComponents;
            
            // If no selected components, show only treasure marker message
            if (componentsToPreview.Count == 0)
            {
                return Strings.GetString("OnlyTreasureMarker");
            }
        }
        
        var previewParts = new List<string>();
        
        foreach (var component in componentsToPreview)
        {
            switch (component.Type)
            {
                case MessageComponentType.PlayerName:
                    // Use a specific player name example for better preview
                    previewParts.Add("Tataru Taru");
                    break;
                case MessageComponentType.Coordinates:
                    // Use a specific map location example with special LinkMarker character from SeIconChar
                    string linkMarker = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.LinkMarker);
                    previewParts.Add($"{linkMarker} Limsa Lominsa Lower Decks ( 9.5 , 11.2 )");
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
