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
    /// <returns>The calculated width needed to display the longest item</returns>
    public static float CalculateComboWidth(string[] items)
    {
        float maxWidth = 0;
        
        foreach (var item in items)
        {
            float itemWidth = ImGui.CalcTextSize(item).X;
            maxWidth = Math.Max(maxWidth, itemWidth);
        }
        
        // Add padding for combo box arrow and internal spacing
        // ImGui combo boxes need extra space for the dropdown arrow and padding
        float comboBoxPadding = 40f; // Space for arrow + internal padding
        float calculatedWidth = maxWidth + comboBoxPadding;
        
        // Ensure minimum width for usability
        float minWidth = 120f;
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        
        // Cap maximum width to prevent overly wide controls
        float maxAllowedWidth = 300f;
        calculatedWidth = Math.Min(calculatedWidth, maxAllowedWidth);
        
        return calculatedWidth;
    }

    /// <summary>
    /// Calculates the optimal width for a button based on its text content.
    /// </summary>
    /// <param name="text">Button text</param>
    /// <param name="minWidth">Minimum width for the button (default: 80px)</param>
    /// <param name="padding">Additional padding beyond ImGui's default (default: 24px)</param>
    /// <returns>The calculated width needed for the button</returns>
    public static float CalculateButtonWidth(string text, float minWidth = 80f, float padding = 24f)
    {
        float textWidth = ImGui.CalcTextSize(text).X;
        float calculatedWidth = textWidth + padding;
        
        // Ensure minimum width for usability
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        
        return calculatedWidth;
    }

    /// <summary>
    /// Calculates the optimal width for multiple labels to ensure consistent alignment.
    /// </summary>
    /// <param name="labels">Array of label texts</param>
    /// <param name="padding">Additional padding (default: 40px)</param>
    /// <param name="minWidth">Minimum width (default: 180px)</param>
    /// <param name="maxWidth">Maximum width (default: 260px)</param>
    /// <returns>The calculated width needed for the longest label</returns>
    public static float CalculateLabelWidth(string[] labels, float padding = 40f, float minWidth = 180f, float maxWidth = 260f)
    {
        float maxLabelWidth = 0;
        
        foreach (var label in labels)
        {
            float width = ImGui.CalcTextSize(label).X;
            maxLabelWidth = Math.Max(maxLabelWidth, width);
        }
        
        // Add padding to the calculated width
        float calculatedWidth = maxLabelWidth + padding;
        
        // Ensure minimum and maximum width constraints
        calculatedWidth = Math.Max(calculatedWidth, minWidth);
        calculatedWidth = Math.Min(calculatedWidth, maxWidth);
        
        return calculatedWidth;
    }
}
