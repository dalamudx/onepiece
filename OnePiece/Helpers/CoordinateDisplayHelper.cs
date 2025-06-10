using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using OnePiece.Models;
using OnePiece.Localization;

namespace OnePiece.Helpers;

/// <summary>
/// Helper class for displaying coordinates in UI with consistent formatting.
/// </summary>
public static class CoordinateDisplayHelper
{
    // Predefined colors for consistent UI appearance
    public static readonly Vector4 MapAreaColor = new(0.5f, 0.8f, 1.0f, 1.0f);
    public static readonly Vector4 DeletedMapAreaColor = new(0.4f, 0.6f, 0.8f, 1.0f);
    public static readonly Vector4 CollectedTextColor = new(0.5f, 0.5f, 0.5f, 1.0f);
    public static readonly Vector4 PlayerNameColor = new(0.8f, 0.9f, 1.0f, 1.0f);

    /// <summary>
    /// Displays a coordinate with consistent formatting.
    /// </summary>
    /// <param name="coordinate">The coordinate to display.</param>
    /// <param name="index">The display index (1-based).</param>
    /// <param name="showPlayerName">Whether to show the player name.</param>
    /// <param name="isDeleted">Whether this is a deleted coordinate (affects styling).</param>
    /// <param name="isCollected">Whether this coordinate is collected (affects styling).</param>
    public static void DisplayCoordinate(
        TreasureCoordinate coordinate, 
        int index, 
        bool showPlayerName = true, 
        bool isDeleted = false, 
        bool? isCollected = null)
    {
        var actuallyCollected = isCollected ?? coordinate.IsCollected;
        
        // Apply collected styling if needed
        if (actuallyCollected)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, CollectedTextColor);
        }

        // Build the display text
        var displayText = $"{index}. ";
        
        // Add player name if available and requested
        if (showPlayerName && !string.IsNullOrEmpty(coordinate.PlayerName))
        {
            displayText += $"{coordinate.PlayerName}: ";
        }

        // Display the base text
        ImGui.TextUnformatted(displayText);

        // Add map area with colored text if available
        if (!string.IsNullOrEmpty(coordinate.MapArea))
        {
            ImGui.SameLine(0, 0);
            var mapColor = isDeleted ? DeletedMapAreaColor : MapAreaColor;
            ImGui.TextColored(mapColor, coordinate.MapArea);
            ImGui.SameLine(0, 5);
            ImGui.TextUnformatted($"({coordinate.X:F1}, {coordinate.Y:F1})");
        }
        else
        {
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted($"({coordinate.X:F1}, {coordinate.Y:F1})");
        }

        // Add coordinate name if available
        if (!string.IsNullOrEmpty(coordinate.Name))
        {
            ImGui.SameLine(0, 5);
            ImGui.TextUnformatted($"- {coordinate.Name}");
        }

        // Pop collected styling if applied
        if (actuallyCollected)
        {
            ImGui.PopStyleColor();
        }
    }

    /// <summary>
    /// Displays a coordinate with inline editing capabilities.
    /// </summary>
    /// <param name="coordinate">The coordinate to display.</param>
    /// <param name="index">The display index (1-based).</param>
    /// <param name="onEdit">Callback when coordinate is edited.</param>
    /// <param name="onDelete">Callback when coordinate is deleted.</param>
    /// <param name="onCollectedToggle">Callback when collected status is toggled.</param>
    public static void DisplayEditableCoordinate(
        TreasureCoordinate coordinate,
        int index,
        Action<TreasureCoordinate>? onEdit = null,
        Action<int>? onDelete = null,
        Action<int, bool>? onCollectedToggle = null)
    {
        DisplayCoordinate(coordinate, index);

        // Add action buttons on the same line
        ImGui.SameLine();

        // Collected checkbox
        if (onCollectedToggle != null)
        {
            var isCollected = coordinate.IsCollected;
            if (ImGui.Checkbox($"##collected_{index}", ref isCollected))
            {
                onCollectedToggle(index - 1, isCollected); // Convert to 0-based index
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Collected);
            }
            
            ImGui.SameLine();
        }

        // Delete button
        if (onDelete != null)
        {
            if (ImGui.Button($"{Strings.Delete}##delete_{index}"))
            {
                onDelete(index - 1); // Convert to 0-based index
            }
        }
    }

    /// <summary>
    /// Displays a map area header with consistent styling.
    /// </summary>
    /// <param name="mapArea">The map area name.</param>
    public static void DisplayMapAreaHeader(string mapArea)
    {
        if (!string.IsNullOrEmpty(mapArea))
        {
            ImGui.TextColored(MapAreaColor, mapArea);
        }
        else
        {
            ImGui.TextColored(MapAreaColor, Strings.UnknownArea);
        }
    }

    /// <summary>
    /// Calculates the display text for a coordinate without rendering it.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="index">The display index (1-based).</param>
    /// <param name="showPlayerName">Whether to include player name.</param>
    /// <returns>The formatted display text.</returns>
    public static string GetCoordinateDisplayText(
        TreasureCoordinate coordinate, 
        int index, 
        bool showPlayerName = true)
    {
        var displayText = $"{index}. ";
        
        if (showPlayerName && !string.IsNullOrEmpty(coordinate.PlayerName))
        {
            displayText += $"{coordinate.PlayerName}: ";
        }

        if (!string.IsNullOrEmpty(coordinate.MapArea))
        {
            displayText += $"{coordinate.MapArea} ({coordinate.X:F1}, {coordinate.Y:F1})";
        }
        else
        {
            displayText += $"({coordinate.X:F1}, {coordinate.Y:F1})";
        }

        if (!string.IsNullOrEmpty(coordinate.Name))
        {
            displayText += $" - {coordinate.Name}";
        }

        return displayText;
    }

    /// <summary>
    /// Displays action buttons for coordinates with consistent styling and spacing.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="index">The coordinate index.</param>
    /// <param name="onTeleport">Callback for teleport action.</param>
    /// <param name="onSendToChat">Callback for send to chat action.</param>
    /// <param name="onToggleCollected">Callback for toggle collected action.</param>
    /// <returns>True if any action was performed.</returns>
    public static bool DisplayCoordinateActions(
        TreasureCoordinate coordinate,
        int index,
        Action<TreasureCoordinate>? onTeleport = null,
        Action<TreasureCoordinate>? onSendToChat = null,
        Action<int, bool>? onToggleCollected = null)
    {
        bool actionPerformed = false;
        
        // Get button texts from localization using strongly-typed strings
        string teleportText = Strings.TeleportButton;
        string chatText = Strings.SendToChat;
        string collectedText = Strings.Collected;

        // Calculate button widths for consistent sizing using the unified method
        float teleportWidth = UIHelper.CalculateButtonWidth(teleportText, 80f);
        float chatWidth = UIHelper.CalculateButtonWidth(chatText, 80f);
        float collectedWidth = UIHelper.CalculateButtonWidth(collectedText, 80f);

        // Teleport button (if coordinate has aetheryte ID)
        if (onTeleport != null && coordinate.AetheryteId > 0)
        {
            if (ImGui.Button($"{teleportText}##teleport_{index}", new Vector2(teleportWidth, 0)))
            {
                onTeleport(coordinate);
                actionPerformed = true;
            }
            ImGui.SameLine();
        }

        // Send to chat button
        if (onSendToChat != null)
        {
            if (ImGui.Button($"{chatText}##chat_{index}", new Vector2(chatWidth, 0)))
            {
                onSendToChat(coordinate);
                actionPerformed = true;
            }
            ImGui.SameLine();
        }

        // Collected toggle button
        if (onToggleCollected != null)
        {
            var buttonColor = coordinate.IsCollected 
                ? new Vector4(0.2f, 0.7f, 0.2f, 1.0f)  // Green for collected
                : new Vector4(0.7f, 0.7f, 0.7f, 1.0f); // Gray for not collected

            ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
            
            if (ImGui.Button($"{collectedText}##collected_{index}", new Vector2(collectedWidth, 0)))
            {
                onToggleCollected(index, !coordinate.IsCollected);
                actionPerformed = true;
            }
            
            ImGui.PopStyleColor();
        }

        return actionPerformed;
    }

    /// <summary>
    /// Displays a summary of coordinates with statistics.
    /// </summary>
    /// <param name="coordinates">The list of coordinates.</param>
    /// <param name="title">The title for the summary.</param>
    public static void DisplayCoordinateSummary(System.Collections.Generic.List<TreasureCoordinate> coordinates, string title)
    {
        if (coordinates.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"{title}: {Strings.NoCoordinates}");
            return;
        }

        var collected = coordinates.Count(c => c.IsCollected);
        var total = coordinates.Count;
        var mapAreas = coordinates.Select(c => c.MapArea).Distinct().Count();

        ImGui.Text($"{title}: {total} coordinates");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"({collected} collected)");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.5f, 1.0f), $"({mapAreas} areas)");
    }


}
