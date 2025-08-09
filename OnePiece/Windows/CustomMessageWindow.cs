using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Linq;
using OnePiece.Localization;
using OnePiece.Models;
using OnePiece.Helpers;

namespace OnePiece.Windows;

public class CustomMessageWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    
    // Custom message variables
    private string newCustomMessage = string.Empty;
    private int editingCustomMessageIndex = -1;
    private string editingCustomMessage = string.Empty;
    
    // Template variables
    private string newTemplateName = string.Empty;
    private int selectedTemplateIndex = -1;
    private MessageTemplate? currentTemplate = null;
    
    // Component reordering
    private int draggedComponentIndex = -1;
    private int hoverComponentIndex = -1;
    
    // Component selection from available components
    private bool[] availableComponents = new bool[3] { false, false, false }; // PlayerName, Coordinates, CustomMessage
    private int selectedCustomMessageIndex = -1;
    
    public CustomMessageWindow(Plugin plugin)
        : base(Strings.CustomMessageSettings + "##OnePiece", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        this.IsOpen = false;
        
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(900, 450), // Increased minimum width to accommodate German text
            MaximumSize = new Vector2(1400, 800) // Increased maximum width for better flexibility
        };

        this.plugin = plugin;

        // Initialize selected template - if there's an active template, select it
        InitializeTemplateSelection();
    }
    
    public override void Draw()
    {
        // Update window title to reflect current language
        WindowName = Strings.CustomMessageSettings + "##OnePiece";
        
        // Get window width for centering
        float windowWidth = ImGui.GetWindowWidth();
        
        // Left and right panels - further optimized for better button display in right panel
        float leftPanelWidth = windowWidth * 0.35f; // Further reduced left panel width
        float rightPanelWidth = windowWidth * 0.63f; // Significantly increased right panel width for optimal button layout
        
        // Left panel - Templates and Custom Messages
        DrawLeftPanel(leftPanelWidth);
        
        ImGui.SameLine();
        
        // Vertical separator
        ImGui.BeginGroup();
        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        ImGui.SameLine();
        ImGui.PopStyleColor();
        ImGui.EndGroup();
        ImGui.SameLine();
        
        // Right panel - Template editing
        DrawRightPanel(rightPanelWidth);
    }
    
    private void DrawLeftPanel(float panelWidth)
    {
        ImGui.BeginChild("LeftPanel", new Vector2(panelWidth, -1), false);
        
        // Template management section
        if (ImGui.CollapsingHeader(Strings.MessageTemplateManagement, ImGuiTreeNodeFlags.DefaultOpen))
        {
            // List of templates
            ImGui.Text(Strings.SavedTemplates);

            if (plugin.Configuration.MessageTemplates.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Strings.NoSavedTemplates);
            }
            else
            {
                // Template list with selection
                for (int i = 0; i < plugin.Configuration.MessageTemplates.Count; i++)
                {
                    string templateName = plugin.Configuration.MessageTemplates[i].Name;
                    bool isSelected = selectedTemplateIndex == i;
                    bool isActive = plugin.Configuration.ActiveTemplateIndex == i;
                    
                    // Template name with selection highlight
                    if (ImGui.Selectable($"{templateName}##template{i}", isSelected))
                    {
                        selectedTemplateIndex = i;
                        UpdateCurrentTemplate();
                    }
                    
                    if (isActive)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "(Active)");
                    }
                }
            }
            
            // Template actions
            if (selectedTemplateIndex >= 0 && selectedTemplateIndex < plugin.Configuration.MessageTemplates.Count)
            {
                // Template selected, show actions
                bool isCurrentlyActive = plugin.Configuration.ActiveTemplateIndex == selectedTemplateIndex;
                
                // Show different button based on whether the template is already active
                if (isCurrentlyActive)
                {
                    // Show button to clear active template with red color
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                    
                    if (ImGui.Button(Strings.ClearActiveTemplate))
                    {
                        // Clear the active template
                        plugin.Configuration.ActiveTemplateIndex = -1;
                        plugin.Configuration.SelectedMessageComponents.Clear();
                        plugin.Configuration.Save();
                    }
                    
                    ImGui.PopStyleColor(3);
                }
                else
                {
                    // Show button to set as active template with green color
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.8f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.9f, 0.2f, 1.0f));
                    
                    if (ImGui.Button(Strings.SetAsActiveTemplate))
                    {
                        plugin.Configuration.ActiveTemplateIndex = selectedTemplateIndex;
                        plugin.Configuration.SelectedMessageComponents = new List<MessageComponent>();

                        // Copy components from template to current selection
                        foreach (var component in plugin.Configuration.MessageTemplates[selectedTemplateIndex].Components)
                        {
                            plugin.Configuration.SelectedMessageComponents.Add(
                                new MessageComponent(component.Type, component.CustomMessageIndex));
                        }

                        plugin.Configuration.Save();
                    }
                    
                    ImGui.PopStyleColor(3);
                }

                ImGui.SameLine();

                if (ImGui.Button(Strings.DeleteTemplate))
                {
                    // Adjust active template index if needed
                    if (plugin.Configuration.ActiveTemplateIndex == selectedTemplateIndex)
                    {
                        plugin.Configuration.ActiveTemplateIndex = -1;
                    }
                    else if (plugin.Configuration.ActiveTemplateIndex > selectedTemplateIndex)
                    {
                        plugin.Configuration.ActiveTemplateIndex--;
                    }

                    plugin.Configuration.MessageTemplates.RemoveAt(selectedTemplateIndex);
                    plugin.Configuration.Save();

                    selectedTemplateIndex = -1;
                    UpdateCurrentTemplate();
                }
            }
            
            // New template creation
            ImGui.Text(Strings.TemplateName);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(panelWidth * 0.45f);
            ImGui.InputText("##TemplateName", ref newTemplateName, 50);
            
            ImGui.SameLine();
            if (ImGui.Button(Strings.Create) && !string.IsNullOrWhiteSpace(newTemplateName))
            {
                // Create new template from current selection
                var newTemplate = new MessageTemplate(newTemplateName);
                
                // Copy current selection if any
                if (plugin.Configuration.SelectedMessageComponents.Count > 0)
                {
                    foreach (var component in plugin.Configuration.SelectedMessageComponents)
                    {
                        newTemplate.Components.Add(
                            new MessageComponent(component.Type, component.CustomMessageIndex));
                    }
                }
                
                // Clear the components of the new template as well
                newTemplate.Components.Clear();
                
                plugin.Configuration.MessageTemplates.Add(newTemplate);
                
                // Clear the current message component list after creating a new template
                plugin.Configuration.SelectedMessageComponents.Clear();
                
                plugin.Configuration.Save();
                
                // Select the new template
                selectedTemplateIndex = plugin.Configuration.MessageTemplates.Count - 1;
                UpdateCurrentTemplate();
                
                // Clear the input field
                newTemplateName = string.Empty;
            }
        }
        
        ImGui.Separator();
        
        // Custom messages section
        if (ImGui.CollapsingHeader(Strings.CustomMessages, ImGuiTreeNodeFlags.DefaultOpen))
        {
            // Display existing custom messages with edit and delete buttons
            for (int i = 0; i < plugin.Configuration.CustomMessages.Count; i++)
            {
                string message = plugin.Configuration.CustomMessages[i];
                ImGui.PushID($"custom_message_{i}");
                
                // Display the message
                ImGui.Text($"{i + 1}. {message}");
                
                // Edit button
                ImGui.SameLine();
                if (ImGui.SmallButton(Strings.Edit + "##" + i))
                {
                    editingCustomMessageIndex = i;
                    editingCustomMessage = message;
                }
                
                // Delete button
                ImGui.SameLine();
                if (ImGui.SmallButton(Strings.Delete + "##" + i))
                {
                    plugin.Configuration.CustomMessages.RemoveAt(i);
                    
                    // Update any message components that reference this or later custom messages
                    UpdateCustomMessageReferencesAfterDelete(i);
                    
                    plugin.Configuration.Save();
                    i--; // Adjust for the removed item
                }
                
                ImGui.PopID();
            }
            
            // Add new custom message
            ImGui.Separator();
            ImGui.Text(Strings.AddNewMessage);
            ImGui.SetNextItemWidth(panelWidth * 0.7f);
            ImGui.InputText("##NewCustomMessage", ref newCustomMessage, 100);
            
            ImGui.SameLine();
            if (ImGui.Button(Strings.Add) && !string.IsNullOrWhiteSpace(newCustomMessage))
            {
                plugin.Configuration.CustomMessages.Add(newCustomMessage);
                plugin.Configuration.Save();
                newCustomMessage = string.Empty;
            }
            
            // Edit custom message dialog
            if (editingCustomMessageIndex >= 0)
            {
                ImGui.Separator();
                ImGui.Text(Strings.EditMessage);
                ImGui.SetNextItemWidth(panelWidth * 0.7f);
                ImGui.InputText("##EditCustomMessage", ref editingCustomMessage, 100);
                
                ImGui.SameLine();
                if (ImGui.Button(Strings.Save) && !string.IsNullOrWhiteSpace(editingCustomMessage))
                {
                    plugin.Configuration.CustomMessages[editingCustomMessageIndex] = editingCustomMessage;
                    plugin.Configuration.Save();
                    editingCustomMessageIndex = -1;
                    editingCustomMessage = string.Empty;
                }
                
                ImGui.SameLine();
                if (ImGui.Button(Strings.Cancel))
                {
                    editingCustomMessageIndex = -1;
                    editingCustomMessage = string.Empty;
                }
            }
        }
        
        ImGui.EndChild();
    }
    
    private void DrawRightPanel(float panelWidth)
    {
        // Use borderless child window to avoid visual separator
        ImGui.BeginChild("RightPanel", new Vector2(panelWidth, -1), false, ImGuiWindowFlags.NoBackground);
        
        // No separator before title - directly show the header
        // Header with template name or indication that no template is selected
        if (currentTemplate != null)
        {
            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.8f, 1.0f), Strings.Messages.EditingTemplate(currentTemplate.Name));
        }
        else if (plugin.Configuration.ActiveTemplateIndex >= 0)
        {
            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.0f, 1.0f),
                Strings.Messages.CurrentActiveTemplate(plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Name));
        }
        else
        {
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.0f, 1.0f), Strings.Status.EditCurrentMessageComponents);
        }

        // Note: All separators in this section have been removed as requested
        ImGui.Text(Strings.CurrentMessageComponentList);

        // Determine which components to display and edit
        List<MessageComponent> componentsToEdit = GetComponentsToEdit();
        bool canEdit = CanEditComponents();

        if (componentsToEdit.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Strings.NoComponents);
        }
        else
        {
            // Display components with unified layout and right-aligned buttons
            for (int i = 0; i < componentsToEdit.Count; i++)
            {
                // Get component text for display
                string componentText = GetComponentDisplayText(componentsToEdit[i]);
                string displayText = $"{i + 1}. {componentText}";

                ImGui.PushID($"component_{i}");

                // Use unified coordinate rendering for consistent layout
                float availableWidth = ImGui.GetContentRegionAvail().X;
                int buttonCount = canEdit ? 3 : 0; // MoveUp + MoveDown + Delete when editing is allowed

                var layoutInfo = UIHelper.RenderCoordinateEntry(displayText, false, availableWidth, buttonCount);

                // Only show edit buttons if editing is allowed
                if (canEdit)
                {
                    // Calculate button widths
                    float moveUpButtonWidth = UIHelper.CalculateButtonWidth(Strings.MoveUp);
                    float moveDownButtonWidth = UIHelper.CalculateButtonWidth(Strings.MoveDown);
                    float deleteButtonWidth = UIHelper.CalculateButtonWidth(Strings.Delete);

                    float buttonSpacing = 8f;
                    float rightMargin = 15f; // Consistent right margin for all languages
                    float componentWindowWidth = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();

                    // Calculate button positions from right to left to ensure consistent right margin
                    // Position 3: Delete button (rightmost)
                    float deleteButtonX = componentWindowWidth - rightMargin - deleteButtonWidth;

                    // Position 2: Move Down button
                    float moveDownButtonX = deleteButtonX - buttonSpacing - moveDownButtonWidth;

                    // Position 1: Move Up button (leftmost)
                    float moveUpButtonX = moveDownButtonX - buttonSpacing - moveUpButtonWidth;

                    // Render Move Up button
                    ImGui.SetCursorPos(new Vector2(moveUpButtonX, layoutInfo.LineStartY));

                    // Disable move up button if this is the first item
                    if (i == 0)
                    {
                        ImGui.BeginDisabled();
                    }

                    if (ImGui.SmallButton($"{Strings.MoveUp}##moveup{i}"))
                    {
                        // Move component up
                        var component = componentsToEdit[i];
                        componentsToEdit.RemoveAt(i);
                        componentsToEdit.Insert(i - 1, component);
                        SaveComponentChanges();
                    }

                    // Add tooltip for the move up button
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Strings.MoveUp);
                    }

                    if (i == 0)
                    {
                        ImGui.EndDisabled();
                    }

                    // Render Move Down button
                    ImGui.SetCursorPos(new Vector2(moveDownButtonX, layoutInfo.LineStartY));

                    // Disable move down button if this is the last item
                    if (i >= componentsToEdit.Count - 1)
                    {
                        ImGui.BeginDisabled();
                    }

                    if (ImGui.SmallButton($"{Strings.MoveDown}##movedown{i}"))
                    {
                        // Move component down
                        var component = componentsToEdit[i];
                        componentsToEdit.RemoveAt(i);
                        componentsToEdit.Insert(i + 1, component);
                        SaveComponentChanges();
                    }

                    // Add tooltip for the move down button
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Strings.MoveDown);
                    }

                    if (i >= componentsToEdit.Count - 1)
                    {
                        ImGui.EndDisabled();
                    }

                    // Render Delete button
                    ImGui.SetCursorPos(new Vector2(deleteButtonX, layoutInfo.LineStartY));

                    if (ImGui.SmallButton($"{Strings.Delete}##delete{i}"))
                    {
                        componentsToEdit.RemoveAt(i);
                        SaveComponentChanges();
                        i--; // Adjust for the removed item
                    }

                    // Add tooltip for the delete button
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Strings.Delete);
                    }
                }

                // Move cursor to next line, accounting for text height
                float nextLineY = layoutInfo.LineStartY + Math.Max(layoutInfo.TextHeight, ImGui.GetFrameHeight());
                ImGui.SetCursorPosY(nextLineY);

                ImGui.PopID();
            }
        }
        
        ImGui.Separator();

        // Add components section - only show if editing is allowed
        if (canEdit)
        {
            ImGui.Text(Strings.AddComponents);

            // Create adaptive button layout for component buttons
            DrawAdaptiveButtonLayout(componentsToEdit);
        }
        else
        {
            // Show read-only message
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                Strings.Status.ViewingActiveTemplateReadOnly);
        }

        // Display all custom messages with direct add buttons - only if editing is allowed
        if (canEdit && plugin.Configuration.CustomMessages.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text(Strings.AddCustomMessage);

            // Display each custom message with its own add button
            for (int i = 0; i < plugin.Configuration.CustomMessages.Count; i++)
            {
                string message = plugin.Configuration.CustomMessages[i];
                if (message.Length > 30)
                {
                    message = message.Substring(0, 27) + "...";
                }

                if (ImGui.Button($"{message}##add_{i}"))
                {
                    componentsToEdit.Add(new MessageComponent(MessageComponentType.CustomMessage, i));
                    SaveComponentChanges();
                }

                // Create multiple columns of buttons for better layout
                if ((i + 1) % 2 != 0 && i < plugin.Configuration.CustomMessages.Count - 1)
                {
                    ImGui.SameLine();
                }
            }
        }
        
        ImGui.Separator();
        
        // Preview section
        ImGui.Text(Strings.MessagePreview);
        string previewMessage = GeneratePreviewMessage(componentsToEdit);
        ImGui.TextWrapped(previewMessage);

        // Show warning for number components that exceed range
        string rangeWarning = plugin.ChatMonitorService.GetNumberComponentRangeWarning();
        if (!string.IsNullOrEmpty(rangeWarning))
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(rangeWarning);
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
        }

        // Save changes to template
        if (currentTemplate != null)
        {
            ImGui.Separator();
            if (ImGui.Button(Strings.SaveTemplateChanges))
            {
                SaveTemplateChanges();
            }
            
            ImGui.SameLine();
            if (ImGui.Button(Strings.Cancel))
            {
                // Reset to original template content without changing selection
                CancelTemplateEditing();
            }
            
            // "Set as Active Template" button removed as requested
        }
        
        ImGui.EndChild();
    }
    
    // Updates custom message references after a custom message is deleted
    private void UpdateCustomMessageReferencesAfterDelete(int deletedIndex)
    {
        // Update main selected components
        for (int j = 0; j < plugin.Configuration.SelectedMessageComponents.Count; j++)
        {
            var component = plugin.Configuration.SelectedMessageComponents[j];
            if (component.Type == MessageComponentType.CustomMessage)
            {
                // If this component referenced the deleted message, remove it
                if (component.CustomMessageIndex == deletedIndex)
                {
                    plugin.Configuration.SelectedMessageComponents.RemoveAt(j);
                    j--; // Adjust index after removal
                }
                // If this component referenced a message after the deleted one, update its index
                else if (component.CustomMessageIndex > deletedIndex)
                {
                    component.CustomMessageIndex--;
                }
            }
        }
        
        // Update all templates
        foreach (var template in plugin.Configuration.MessageTemplates)
        {
            for (int j = 0; j < template.Components.Count; j++)
            {
                var component = template.Components[j];
                if (component.Type == MessageComponentType.CustomMessage)
                {
                    // If this component referenced the deleted message, remove it
                    if (component.CustomMessageIndex == deletedIndex)
                    {
                        template.Components.RemoveAt(j);
                        j--; // Adjust index after removal
                    }
                    // If this component referenced a message after the deleted one, update its index
                    else if (component.CustomMessageIndex > deletedIndex)
                    {
                        component.CustomMessageIndex--;
                    }
                }
            }
        }
    }
    
    // Generates a preview of the message that will be sent
    private string GeneratePreviewMessage(List<MessageComponent> components)
    {
        if (components.Count == 0)
        {
            return Strings.Status.CoordinateOnlyMessage;
        }

        var previewParts = new List<string>();
        var totalCoordinates = plugin.TreasureHuntService.Coordinates.Count;

        foreach (var component in components)
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
                    // Only show if coordinate count is within range
                    if (GameIconHelper.IsCoordinateCountWithinIconRange(MessageComponentType.Number, totalCoordinates))
                    {
                        // Show specific Number1 special character using the actual Unicode value from SeIconChar
                        string number1 = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.Number1);
                        previewParts.Add(number1);
                    }
                    break;
                case MessageComponentType.BoxedNumber:
                    // Only show if coordinate count is within range
                    if (GameIconHelper.IsCoordinateCountWithinIconRange(MessageComponentType.BoxedNumber, totalCoordinates))
                    {
                        // Show specific BoxedNumber1 special character using the actual Unicode value from SeIconChar
                        string boxedNumber1 = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.BoxedNumber1);
                        previewParts.Add(boxedNumber1);
                    }
                    break;
                case MessageComponentType.BoxedOutlinedNumber:
                    // Only show if coordinate count is within range
                    if (GameIconHelper.IsCoordinateCountWithinIconRange(MessageComponentType.BoxedOutlinedNumber, totalCoordinates))
                    {
                        // Show specific BoxedOutlinedNumber1 special character using the actual Unicode value from SeIconChar
                        string boxedOutlinedNumber1 = char.ConvertFromUtf32((int)Dalamud.Game.Text.SeIconChar.BoxedOutlinedNumber1);
                        previewParts.Add(boxedOutlinedNumber1);
                    }
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
    
    // Gets a display text for a component
    private string GetComponentDisplayText(MessageComponent component)
    {
        switch (component.Type)
        {
            case MessageComponentType.PlayerName:
                return Strings.PlayerName;
            case MessageComponentType.Coordinates:
                return Strings.Coordinates;
            case MessageComponentType.Number:
                return Strings.Number;
            case MessageComponentType.BoxedNumber:
                return Strings.BoxedNumber;
            case MessageComponentType.BoxedOutlinedNumber:
                return Strings.BoxedOutlinedNumber;
            case MessageComponentType.CustomMessage:
                if (component.CustomMessageIndex >= 0 && component.CustomMessageIndex < plugin.Configuration.CustomMessages.Count)
                {
                    return Strings.Messages.CustomMessagePrefix(plugin.Configuration.CustomMessages[component.CustomMessageIndex]);
                }
                return Strings.Status.InvalidCustomMessage;
            default:
                return Strings.Status.UnknownComponent;
        }
    }
    
    /// <summary>
    /// Initializes template selection when the window is opened
    /// </summary>
    private void InitializeTemplateSelection()
    {
        // If there's an active template, select it automatically
        if (plugin.Configuration.ActiveTemplateIndex >= 0 &&
            plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            selectedTemplateIndex = plugin.Configuration.ActiveTemplateIndex;
        }
        else
        {
            selectedTemplateIndex = -1;
        }

        UpdateCurrentTemplate();
    }

    /// <summary>
    /// Gets the appropriate components list to display and edit
    /// </summary>
    /// <returns>The components list that should be displayed</returns>
    private List<MessageComponent> GetComponentsToEdit()
    {
        // If we're editing a selected template, use its components
        if (currentTemplate != null)
        {
            return currentTemplate.Components;
        }

        // If there's an active template but no template is selected for editing,
        // show the active template's components (read-only view)
        if (plugin.Configuration.ActiveTemplateIndex >= 0 &&
            plugin.Configuration.ActiveTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            return plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Components;
        }

        // Otherwise, show the global selected components
        return plugin.Configuration.SelectedMessageComponents;
    }

    /// <summary>
    /// Determines whether components can be edited in the current context
    /// </summary>
    /// <returns>True if components can be edited, false if read-only</returns>
    private bool CanEditComponents()
    {
        // Can edit if we're editing a selected template
        if (currentTemplate != null)
        {
            return true;
        }

        // Can edit global components only if there's no active template
        if (plugin.Configuration.ActiveTemplateIndex < 0)
        {
            return true;
        }

        // Otherwise, it's a read-only view of the active template
        return false;
    }

    // Updates the current template based on selection
    private void UpdateCurrentTemplate()
    {
        if (selectedTemplateIndex >= 0 && selectedTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            // Clone the template to edit
            currentTemplate = plugin.Configuration.MessageTemplates[selectedTemplateIndex].Clone();
        }
        else
        {
            currentTemplate = null;
        }
    }
    
    // Saves changes to the current template
    private void SaveTemplateChanges()
    {
        if (currentTemplate != null && selectedTemplateIndex >= 0 && selectedTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            // Update the template in the configuration
            plugin.Configuration.MessageTemplates[selectedTemplateIndex] = currentTemplate;
            plugin.Configuration.Save();

            // Notify other windows about the template update
            plugin.NotifyMessageTemplateUpdated();
        }
    }

    /// <summary>
    /// Cancels template editing and reverts to the original template content
    /// </summary>
    private void CancelTemplateEditing()
    {
        if (selectedTemplateIndex >= 0 && selectedTemplateIndex < plugin.Configuration.MessageTemplates.Count)
        {
            // Reload the original template content, discarding any unsaved changes
            currentTemplate = plugin.Configuration.MessageTemplates[selectedTemplateIndex].Clone();
        }
        // Keep the selectedTemplateIndex unchanged to maintain the editing state
    }
    
    // Saves changes to components
    private void SaveComponentChanges()
    {
        if (currentTemplate == null)
        {
            // Only save to global components if there's no active template
            // or if we're specifically editing the global components
            if (plugin.Configuration.ActiveTemplateIndex < 0)
            {
                // Saving changes to main selected components
                plugin.Configuration.Save();

                // Notify other windows about the component update
                plugin.NotifyMessageTemplateUpdated();
            }
            // If there's an active template but we're not editing it,
            // we shouldn't allow modifications (this is a read-only view)
        }
        // If currentTemplate is not null, changes are automatically saved to the template
        // when SaveTemplateChanges() is called
    }
    
    /// <summary>
    /// Draws component buttons with adaptive layout that automatically wraps to new lines when needed
    /// </summary>
    /// <param name="componentsToEdit">The list of components to edit</param>
    private void DrawAdaptiveButtonLayout(List<MessageComponent> componentsToEdit)
    {
        // Define button data with their corresponding actions
        var buttonData = new[]
        {
            new { Text = Strings.PlayerName, Type = MessageComponentType.PlayerName },
            new { Text = Strings.Coordinates, Type = MessageComponentType.Coordinates },
            new { Text = Strings.Number, Type = MessageComponentType.Number },
            new { Text = Strings.BoxedNumber, Type = MessageComponentType.BoxedNumber },
            new { Text = Strings.BoxedOutlinedNumber, Type = MessageComponentType.BoxedOutlinedNumber }
        };

        // Get available content width
        float availableWidth = ImGui.GetContentRegionAvail().X;
        float currentLineWidth = 0f;
        bool isFirstButtonInLine = true;

        // Minimum spacing between buttons
        float buttonSpacing = ImGui.GetStyle().ItemSpacing.X;

        for (int i = 0; i < buttonData.Length; i++)
        {
            var button = buttonData[i];

            // Calculate button width using the unified method
            float buttonWidth = UIHelper.CalculateButtonWidth(button.Text, 60f, ImGui.GetStyle().FramePadding.X * 2);

            // Check if we need to wrap to next line
            if (!isFirstButtonInLine && currentLineWidth + buttonSpacing + buttonWidth > availableWidth)
            {
                // Start new line
                currentLineWidth = 0f;
                isFirstButtonInLine = true;
            }

            // Add spacing if not the first button in line
            if (!isFirstButtonInLine)
            {
                ImGui.SameLine();
                currentLineWidth += buttonSpacing;
            }

            // Check if this component type should be limited to one instance
            bool isLimitedComponent = button.Type == MessageComponentType.PlayerName ||
                                    button.Type == MessageComponentType.Coordinates;
            bool componentAlreadyExists = isLimitedComponent &&
                                        componentsToEdit.Any(c => c.Type == button.Type);

            // Disable button if component already exists and is limited
            if (componentAlreadyExists)
            {
                ImGui.BeginDisabled();
            }

            // Draw the button
            if (ImGui.Button(button.Text))
            {
                componentsToEdit.Add(new MessageComponent(button.Type));
                SaveComponentChanges();
            }

            // Add tooltip for disabled buttons
            if (componentAlreadyExists && ImGui.IsItemHovered())
            {
                string tooltipText = button.Type == MessageComponentType.PlayerName
                    ? Strings.Messages.PlayerNameAlreadyAdded
                    : Strings.Messages.CoordinatesAlreadyAdded;
                ImGui.SetTooltip(tooltipText);
            }

            if (componentAlreadyExists)
            {
                ImGui.EndDisabled();
            }

            // Update current line width and mark that we're no longer the first button
            currentLineWidth += buttonWidth;
            isFirstButtonInLine = false;
        }
    }



    public void Dispose()
    {
        // Nothing to dispose
    }
}
