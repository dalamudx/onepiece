using System;
using ImGuiNET;

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


}
