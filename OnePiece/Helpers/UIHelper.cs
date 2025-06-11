using System;
using System.Numerics;
using ImGuiNET;
using Dalamud.Interface.Utility.Raii;
using OnePiece.Localization;

namespace OnePiece.Helpers;

/// <summary>
/// Provides utility methods for UI calculations and layout.
/// </summary>
public static class UIHelper
{
    /// <summary>
    /// Calculates the optimal width for a combo box based on its content.
    /// </summary>
    /// <param name="items">Array of items in the combo box</param>
    /// <param name="padding">Padding for combo box arrow and internal spacing (default: 30f for better spacing)</param>
    /// <param name="minWidth">Minimum width for usability (default: 80f)</param>
    /// <param name="maxWidth">Maximum width to prevent overly wide controls (default: 300f, increased for Japanese long text)</param>
    /// <returns>The calculated width needed to display the longest item</returns>
    public static float CalculateComboWidth(string[] items, float padding = 30f, float minWidth = 80f, float maxWidth = 300f)
    {
        if (items == null || items.Length == 0)
            return minWidth;

        float maxItemWidth = 0f;

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item))
            {
                float itemWidth = ImGui.CalcTextSize(item).X;
                maxItemWidth = Math.Max(maxItemWidth, itemWidth);
            }
        }

        // Add padding for combo box arrow and internal spacing
        float calculatedWidth = maxItemWidth + padding;

        // Ensure minimum and maximum width constraints
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        calculatedWidth = Math.Min(calculatedWidth, maxWidth);

        return calculatedWidth;
    }

    /// <summary>
    /// Calculates the optimal width for a button based on its text content.
    /// </summary>
    /// <param name="text">Button text</param>
    /// <param name="minWidth">Minimum width for the button (default: 80px)</param>
    /// <param name="padding">Additional padding beyond ImGui's default (default: 24px)</param>
    /// <param name="maxWidth">Maximum width to prevent overly wide buttons (default: 250px for German/French)</param>
    /// <returns>The calculated width needed for the button</returns>
    public static float CalculateButtonWidth(string text, float minWidth = 80f, float padding = 24f, float maxWidth = 250f)
    {
        if (string.IsNullOrEmpty(text))
            return minWidth;

        float textWidth = ImGui.CalcTextSize(text).X;
        float calculatedWidth = textWidth + padding;

        // Ensure minimum and maximum width constraints
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        calculatedWidth = Math.Min(calculatedWidth, maxWidth);

        return calculatedWidth;
    }

    /// <summary>
    /// Calculates the optimal width for multiple labels to ensure consistent alignment.
    /// Optimized for tight, adaptive layout that works well with different languages.
    /// </summary>
    /// <param name="labels">Array of label texts</param>
    /// <param name="padding">Additional padding for spacing (default: 15f for tight fit)</param>
    /// <param name="minWidth">Minimum width for usability (default: 100f)</param>
    /// <param name="maxWidth">Maximum width to prevent excessive space (default: 250f, increased for German/French)</param>
    /// <returns>The calculated width needed for the longest label</returns>
    public static float CalculateLabelWidth(string[] labels, float padding = 15f, float minWidth = 100f, float maxWidth = 250f)
    {
        if (labels == null || labels.Length == 0)
            return minWidth;

        float maxLabelWidth = 0f;

        foreach (var label in labels)
        {
            if (!string.IsNullOrEmpty(label))
            {
                float width = ImGui.CalcTextSize(label).X;
                maxLabelWidth = Math.Max(maxLabelWidth, width);
            }
        }

        // Add padding to the calculated width
        float calculatedWidth = maxLabelWidth + padding;

        // Ensure minimum and maximum width constraints
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        calculatedWidth = Math.Min(calculatedWidth, maxWidth);

        return calculatedWidth;
    }

    /// <summary>
    /// Calculates precise label width for a single label, useful for individual label sizing.
    /// </summary>
    /// <param name="label">Label text</param>
    /// <param name="padding">Additional padding for spacing (default: 15f)</param>
    /// <param name="minWidth">Minimum width for usability (default: 80f)</param>
    /// <param name="maxWidth">Maximum width to prevent excessive space (default: 180f)</param>
    /// <returns>The calculated width needed for the label</returns>
    public static float CalculateSingleLabelWidth(string label, float padding = 15f, float minWidth = 80f, float maxWidth = 180f)
    {
        if (string.IsNullOrEmpty(label))
            return minWidth;

        float labelWidth = ImGui.CalcTextSize(label).X;
        float calculatedWidth = labelWidth + padding;

        // Ensure minimum and maximum width constraints
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        calculatedWidth = Math.Min(calculatedWidth, maxWidth);

        return calculatedWidth;
    }





    /// <summary>
    /// Calculates optimal text area width based on available space and button requirements.
    /// Optimized for different languages with varying button text lengths.
    /// </summary>
    /// <param name="availableWidth">Total available width</param>
    /// <param name="buttonCount">Number of buttons to display</param>
    /// <param name="buttonWidth">Average width per button (default: 130f, increased for German/French)</param>
    /// <param name="spacing">Spacing between elements (default: 15f)</param>
    /// <returns>Optimal text area width</returns>
    public static float CalculateTextAreaWidth(float availableWidth, int buttonCount, float buttonWidth = 130f, float spacing = 15f)
    {
        // Calculate actual button widths for current language to be more precise
        float actualButtonsWidth = 0f;

        if (buttonCount == 3)
        {
            // Teleport + Chat + Collected buttons
            actualButtonsWidth = CalculateButtonWidth(Strings.TeleportButton) +
                               CalculateButtonWidth(Strings.SendToChat) +
                               Math.Max(CalculateButtonWidth(Strings.Collected), CalculateButtonWidth(Strings.NotCollected)) +
                               (2 * spacing); // spacing between buttons
        }
        else if (buttonCount == 2)
        {
            // Chat + Collected buttons (most common for raw coordinates)
            actualButtonsWidth = CalculateButtonWidth(Strings.SendToChat) +
                               Math.Max(CalculateButtonWidth(Strings.Collected), CalculateButtonWidth(Strings.NotCollected)) +
                               spacing; // spacing between buttons
        }
        else
        {
            // Fallback to estimated calculation
            actualButtonsWidth = (buttonCount * buttonWidth) + ((buttonCount - 1) * spacing);
        }

        float textAreaWidth = availableWidth - actualButtonsWidth - (spacing * 2); // Extra margin spacing

        // Ensure minimum text area width, but allow it to be smaller if needed for long button text
        return Math.Max(textAreaWidth, 120f);
    }

    /// <summary>
    /// Renders a complete coordinate entry with aligned text and buttons.
    /// This provides a unified layout for both optimized and raw coordinate lists.
    /// Properly handles text wrapping to prevent overlap with next line.
    /// Note: For new coordinate-specific code, consider using CoordinateDisplayHelper.DisplayCoordinateWithOptimalLayout.
    /// </summary>
    /// <param name="displayText">The coordinate text to display</param>
    /// <param name="isCollected">Whether the coordinate is collected</param>
    /// <param name="availableWidth">Available width for the entire row</param>
    /// <param name="buttonCount">Number of buttons that will be displayed</param>
    /// <param name="estimatedButtonWidth">Estimated average button width (default: 130f for German/French)</param>
    /// <returns>Information about the rendered layout</returns>
    public static CoordinateLayoutInfo RenderCoordinateEntry(string displayText, bool isCollected, float availableWidth, int buttonCount = 3, float estimatedButtonWidth = 130f)
    {
        // Calculate optimal text area width with more generous button space
        float textAreaWidth = CalculateTextAreaWidth(availableWidth, buttonCount, estimatedButtonWidth, 15f);

        // Store the starting position for proper layout
        float lineStartY = ImGui.GetCursorPosY();
        float lineStartX = ImGui.GetCursorPosX();

        // Render the coordinate text with proper styling and wrapping
        if (isCollected)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        }

        // Calculate text size to determine if wrapping will occur
        var textSize = ImGui.CalcTextSize(displayText, textAreaWidth);

        // Use a child region to contain the text and prevent overlap
        using (var child = ImRaii.Child($"##text_{displayText.GetHashCode()}",
            new Vector2(textAreaWidth, textSize.Y + 4f), // Add small padding
            false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground))
        {
            if (child.Success)
            {
                ImGui.TextWrapped(displayText);
            }
        }

        if (isCollected)
        {
            ImGui.PopStyleColor();
        }

        // Calculate button start position - position buttons at the top of the text area
        float buttonStartX = lineStartX + textAreaWidth + 15f;

        // Position cursor for buttons aligned with the top of the text
        ImGui.SetCursorPos(new Vector2(buttonStartX, lineStartY));

        return new CoordinateLayoutInfo
        {
            TextAreaWidth = textAreaWidth,
            ButtonStartX = buttonStartX,
            LineStartY = lineStartY,
            IsCollected = isCollected,
            TextHeight = textSize.Y + 4f // Include the actual text height
        };
    }

    /// <summary>
    /// Information about the coordinate layout for button positioning.
    /// </summary>
    public struct CoordinateLayoutInfo
    {
        public float TextAreaWidth;
        public float ButtonStartX;
        public float LineStartY;
        public bool IsCollected;
        public float TextHeight;
    }
}
