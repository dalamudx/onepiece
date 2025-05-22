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

        // Subscribe to events
        this.plugin.TreasureHuntService.OnCoordinatesImported += OnCoordinatesImported;
        this.plugin.TreasureHuntService.OnRouteOptimized += OnRouteOptimized;
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
    }

    public override void Draw()
    {
        // Get window width for centering
        float windowWidth = ImGui.GetWindowWidth();

        // Display subtitle (centered)
        string subtitle = Strings.GetString("MainWindowSubtitle");
        float textWidth = ImGui.CalcTextSize(subtitle).X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
        ImGui.TextUnformatted(subtitle);

        ImGui.Separator();

        // General Settings section
        ImGui.TextUnformatted(Strings.GetString("GeneralSettings"));

        // Calculate optimal width for labels and controls
        float labelWidth = 220; // Increased from 180 to 220 to ensure labels are not cut off
        float controlWidth = 250; // Keep the same control width

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

        // Auto-optimize route
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(Strings.GetString("AutoOptimizeRoute"));
        ImGui.SameLine(labelWidth);
        var autoOptimize = plugin.Configuration.AutoOptimizeRoute;
        if (ImGui.Checkbox("##AutoOptimizeRoute", ref autoOptimize))
        {
            plugin.Configuration.AutoOptimizeRoute = autoOptimize;
            plugin.Configuration.Save();
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
            if (ImGui.Button(Strings.GetString("OptimizeRoute")))
            {
                plugin.TreasureHuntService.OptimizeRoute();
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

        // Chat monitoring section
        ImGui.TextUnformatted(Strings.GetString("ChatChannelMonitoring"));

        // Chat channel selection - moved to be inline with the label
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(Strings.GetString("SelectChatChannel"));
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(controlWidth);
        if (ImGui.Combo("##ChatChannelSelector", ref selectedChatChannelIndex, chatChannelNames, chatChannelNames.Length))
        {
            plugin.Configuration.MonitoredChatChannel = (ChatChannelType)selectedChatChannelIndex;
            plugin.Configuration.Save();
        }

        // Monitoring control buttons (without status display)
        var isMonitoring = plugin.ChatMonitorService.IsMonitoring;
        if (isMonitoring)
        {
            if (ImGui.Button(Strings.GetString("StopMonitoring")))
            {
                plugin.ChatMonitorService.StopMonitoring();
            }
        }
        else
        {
            if (ImGui.Button(Strings.GetString("StartMonitoring")))
            {
                plugin.ChatMonitorService.StartMonitoring();
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
                        // Display optimized route title with count
                        ImGui.TextUnformatted(string.Format(Strings.GetString("OptimizedRouteWithCount"), optimizedRoute.Count));

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

                                // Display player name and coordinates
                                var displayText = $"{optimizedRoute.IndexOf(coord) + 1}. ";
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

                                if (isCollected)
                                {
                                    ImGui.PopStyleColor();
                                }

                                ImGui.SameLine();

                                // Send to Chat button
                                if (ImGui.SmallButton(Strings.GetString("SendToChat") + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    plugin.ChatMonitorService.SendCoordinateToChat(coord);
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
                                ImGui.TextUnformatted($"{displayText}{coord}");
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
}
