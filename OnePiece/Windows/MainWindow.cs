﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Linq;
using OnePiece.Localization;
using OnePiece.Models;

namespace OnePiece.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private string[] chatChannelNames = Array.Empty<string>(); // Initialize with empty array
    private string[] supportedLanguages = Array.Empty<string>();
    private string[] logLevels = Array.Empty<string>();
    private int selectedLanguageIndex;
    private int selectedChatChannelIndex;
    private int selectedLogLevelIndex;

    public MainWindow(Plugin plugin, string logoImagePath)
        : base(Strings.GetString("MainWindowTitle") + "##OnePiece", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400), // Increased minimum width
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        // Initialize chat channel names
        InitializeChatChannelNames();

        // Initialize language selection
        supportedLanguages = Strings.GetSupportedLanguages();
        selectedLanguageIndex = Array.IndexOf(supportedLanguages, Plugin.Configuration.Language);
        if (selectedLanguageIndex < 0) selectedLanguageIndex = 0;

        // Initialize log level selection
        InitializeLogLevels();
        selectedLogLevelIndex = (int)Plugin.Configuration.LogLevel;

        // Initialize chat channel selection
        selectedChatChannelIndex = (int)Plugin.Configuration.MonitoredChatChannel;

        // Subscribe to events
        Plugin.TreasureHuntService.CoordinatesImported += OnCoordinatesImported;
        Plugin.TreasureHuntService.RouteOptimized += OnRouteOptimized;
    }

    private void InitializeChatChannelNames()
    {
        // Create localized chat channel names
        chatChannelNames = new string[]
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
        logLevels = new string[]
        {
            Strings.GetString("LogLevelMinimal"),
            Strings.GetString("LogLevelNormal"),
            Strings.GetString("LogLevelVerbose")
        };
    }

    public void Dispose()
    {
        // Unsubscribe from events
        Plugin.TreasureHuntService.CoordinatesImported -= OnCoordinatesImported;
        Plugin.TreasureHuntService.RouteOptimized -= OnRouteOptimized;
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
            Plugin.Configuration.Language = supportedLanguages[selectedLanguageIndex];
            Strings.SetLanguage(Plugin.Configuration.Language);
            Plugin.Configuration.Save();

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
            Plugin.Configuration.LogLevel = (LogLevel)selectedLogLevelIndex;
            Plugin.Configuration.Save();
        }

        // Show tooltip for the selected log level
        if (ImGui.IsItemHovered())
        {
            switch (Plugin.Configuration.LogLevel)
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
        var autoOptimize = Plugin.Configuration.AutoOptimizeRoute;
        if (ImGui.Checkbox("##AutoOptimizeRoute", ref autoOptimize))
        {
            Plugin.Configuration.AutoOptimizeRoute = autoOptimize;
            Plugin.Configuration.Save();
        }

        ImGui.Separator();

        // Action buttons
        if (ImGui.Button(Strings.GetString("ClearAll")))
        {
            Plugin.TreasureHuntService.ClearCoordinates();
        }

        ImGui.SameLine();

        if (Plugin.TreasureHuntService.IsRouteOptimized)
        {
            if (ImGui.Button(Strings.GetString("ResetOptimization")))
            {
                Plugin.TreasureHuntService.ResetRouteOptimization();
            }
        }
        else
        {
            if (ImGui.Button(Strings.GetString("OptimizeRoute")))
            {
                Plugin.TreasureHuntService.OptimizeRoute();
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
            Plugin.Configuration.MonitoredChatChannel = (ChatChannelType)selectedChatChannelIndex;
            Plugin.Configuration.Save();
        }

        // Monitoring control buttons (without status display)
        var isMonitoring = Plugin.ChatMonitorService.IsMonitoring;
        if (isMonitoring)
        {
            if (ImGui.Button(Strings.GetString("StopMonitoring")))
            {
                Plugin.ChatMonitorService.StopMonitoring();
            }
        }
        else
        {
            if (ImGui.Button(Strings.GetString("StartMonitoring")))
            {
                Plugin.ChatMonitorService.StartMonitoring();
            }
        }

        ImGui.Separator();

        // Display coordinates and route
        using (var child = ImRaii.Child("CoordinatesDisplay", new Vector2(-1, -1), true))
        {
            if (child.Success)
            {
                var coordinates = Plugin.TreasureHuntService.Coordinates;
                var optimizedRoute = Plugin.TreasureHuntService.OptimizedRoute;

                if (coordinates.Count > 0)
                {
                    // Display the optimized route
                    if (optimizedRoute.Count > 0)
                    {
                        // Display optimized route title with count
                        ImGui.TextUnformatted(string.Format(Strings.GetString("OptimizedRouteWithCount"), optimizedRoute.Count));

                        // Group coordinates by map area
                        var coordinatesByMap = optimizedRoute.GroupBy(c => c.MapArea).ToDictionary(g => g.Key, g => g.ToList());

                        // Display coordinates grouped by map area
                        foreach (var mapGroup in coordinatesByMap)
                        {
                            var mapArea = mapGroup.Key;
                            var mapCoordinates = mapGroup.Value;

                            // Display map area header
                            if (!string.IsNullOrEmpty(mapArea))
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), mapArea);
                            }
                            else
                            {
                                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), Strings.GetString("UnknownArea"));
                            }

                            // Display coordinates for this map area
                            for (var i = 0; i < mapCoordinates.Count; i++)
                            {
                                var coord = mapCoordinates[i];
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
                                    Plugin.ChatMonitorService.SendCoordinateToChat(coord);
                                }

                                ImGui.SameLine();

                                // Collected button
                                if (ImGui.SmallButton(Strings.GetString("Collected") + $"##{optimizedRoute.IndexOf(coord)}"))
                                {
                                    var index = Plugin.TreasureHuntService.Coordinates.IndexOf(coord);
                                    if (index >= 0)
                                    {
                                        Plugin.TreasureHuntService.MarkAsCollected(index);
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
                                Plugin.TreasureHuntService.DeleteCoordinate(i);
                            }
                        }
                    }
                }
                else
                {
                    ImGui.TextUnformatted(Strings.GetString("NoCoordinates"));
                }

                // Display trash bin section if there are deleted coordinates
                if (Plugin.TreasureHuntService.DeletedCoordinates.Count > 0)
                {
                    ImGui.Separator();

                    // Display trash bin title with count
                    ImGui.TextUnformatted(string.Format(Strings.GetString("TrashBinWithCount"), Plugin.TreasureHuntService.DeletedCoordinates.Count));

                    // Clear trash button
                    ImGui.SameLine(ImGui.GetWindowWidth() - 100);
                    if (ImGui.SmallButton(Strings.GetString("ClearTrash")))
                    {
                        Plugin.TreasureHuntService.ClearTrash();
                    }

                    // Display deleted coordinates
                    for (var i = 0; i < Plugin.TreasureHuntService.DeletedCoordinates.Count; i++)
                    {
                        var coord = Plugin.TreasureHuntService.DeletedCoordinates[i];

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
                            Plugin.TreasureHuntService.RestoreCoordinate(i);
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
        if (Plugin.Configuration.LogLevel >= LogLevel.Normal)
        {
            Plugin.ChatGui.Print(string.Format(Strings.GetString("CoordinatesImported"), count));
        }
    }

    private void OnRouteOptimized(object? sender, int count)
    {
        // Only show log message if log level is Normal or higher
        if (Plugin.Configuration.LogLevel >= LogLevel.Normal)
        {
            Plugin.ChatGui.Print(string.Format(Strings.GetString("RouteOptimized"), count));
        }
    }
}
