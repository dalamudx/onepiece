using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using OnePiece.Models;
using OnePiece.Localization;

namespace OnePiece.Helpers;

/// <summary>
/// Helper class for displaying coordinates in UI with consistent formatting.
/// Provides specialized coordinate display methods while leveraging UIHelper for general UI calculations.
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
            displayText += $"{coordinate.PlayerName} ";
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

        // Pop collected styling if applied
        if (actuallyCollected)
        {
            ImGui.PopStyleColor();
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
            displayText += $"{coordinate.PlayerName} ";
        }

        if (!string.IsNullOrEmpty(coordinate.MapArea))
        {
            displayText += $"{coordinate.MapArea} ({coordinate.X:F1}, {coordinate.Y:F1})";
        }
        else
        {
            displayText += $"({coordinate.X:F1}, {coordinate.Y:F1})";
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

    /// <summary>
    /// Renders a complete coordinate entry with optimal layout using both coordinate-specific and general UI helpers.
    /// This method combines the best of both CoordinateDisplayHelper and UIHelper.
    /// </summary>
    /// <param name="coordinate">The coordinate to display</param>
    /// <param name="index">The display index (1-based)</param>
    /// <param name="availableWidth">Available width for the entire row</param>
    /// <param name="onTeleport">Callback for teleport action</param>
    /// <param name="onSendToChat">Callback for send to chat action</param>
    /// <param name="onToggleCollected">Callback for toggle collected action</param>
    /// <param name="showPlayerName">Whether to show player name</param>
    /// <returns>True if any action was performed</returns>
    public static bool DisplayCoordinateWithOptimalLayout(
        TreasureCoordinate coordinate,
        int index,
        float availableWidth,
        Action<TreasureCoordinate>? onTeleport = null,
        Action<TreasureCoordinate>? onSendToChat = null,
        Action<int, bool>? onToggleCollected = null,
        bool showPlayerName = true)
    {
        // Calculate optimal text area width using UIHelper
        int buttonCount = (onTeleport != null && coordinate.AetheryteId > 0 ? 1 : 0) +
                         (onSendToChat != null ? 1 : 0) +
                         (onToggleCollected != null ? 1 : 0);

        float textAreaWidth = UIHelper.CalculateTextAreaWidth(availableWidth, buttonCount);

        // Get coordinate display text
        string displayText = GetCoordinateDisplayText(coordinate, index, showPlayerName);

        // Store starting position
        float lineStartY = ImGui.GetCursorPosY();
        float lineStartX = ImGui.GetCursorPosX();

        // Render coordinate text with proper styling
        if (coordinate.IsCollected)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, CollectedTextColor);
        }

        // Calculate text size for proper layout
        var textSize = ImGui.CalcTextSize(displayText, false, textAreaWidth);

        // Render text in a contained area
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + textAreaWidth);
        ImGui.TextWrapped(displayText);
        ImGui.PopTextWrapPos();

        if (coordinate.IsCollected)
        {
            ImGui.PopStyleColor();
        }

        // Position cursor for buttons
        float buttonStartX = lineStartX + textAreaWidth + 15f;
        ImGui.SetCursorPos(new Vector2(buttonStartX, lineStartY));

        // Display action buttons using our specialized method
        bool actionPerformed = DisplayCoordinateActions(coordinate, index, onTeleport, onSendToChat, onToggleCollected);

        // Ensure proper line spacing
        ImGui.SetCursorPosY(Math.Max(ImGui.GetCursorPosY(), lineStartY + textSize.Y + 8f));

        return actionPerformed;
    }
}
