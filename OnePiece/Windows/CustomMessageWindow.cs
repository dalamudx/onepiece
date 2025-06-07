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
        : base(LocalizationManager.GetString("CustomMessageSettings") + "##OnePiece", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        // Store plugin reference
        this.plugin = plugin;
        
        // Log window creation
        Plugin.Log.Information("CustomMessageWindow created");
        
        // Make sure the window is closed by default
        this.IsOpen = false;
        
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(650, 450),
            MaximumSize = new Vector2(1000, 800)
        };

        this.plugin = plugin;
        
        // Initialize selected template
        UpdateCurrentTemplate();
    }
    
    public override void Draw()
    {
        // Update window title to reflect current language
        WindowName = LocalizationManager.GetString("CustomMessageSettings") + "##OnePiece";
        
        // Get window width for centering
        float windowWidth = ImGui.GetWindowWidth();
        
        // Left and right panels
        float leftPanelWidth = windowWidth * 0.4f;
        float rightPanelWidth = windowWidth * 0.58f;
        
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
        if (ImGui.CollapsingHeader(LocalizationManager.GetString("MessageTemplateManagement"), ImGuiTreeNodeFlags.DefaultOpen))
        {
            // List of templates
            ImGui.Text(LocalizationManager.GetString("SavedTemplates"));
            
            if (plugin.Configuration.MessageTemplates.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), LocalizationManager.GetString("NoSavedTemplates"));
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
                    
                    if (ImGui.Button(LocalizationManager.GetString("ClearActiveTemplate")))
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
                    
                    if (ImGui.Button(LocalizationManager.GetString("SetAsActiveTemplate")))
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
                
                if (ImGui.Button(LocalizationManager.GetString("DeleteTemplate")))
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
            ImGui.Text(LocalizationManager.GetString("TemplateName"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(panelWidth * 0.45f);
            ImGui.InputText("##TemplateName", ref newTemplateName, 50);
            
            ImGui.SameLine();
            if (ImGui.Button(LocalizationManager.GetString("Create")) && !string.IsNullOrWhiteSpace(newTemplateName))
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
        if (ImGui.CollapsingHeader(LocalizationManager.GetString("CustomMessages"), ImGuiTreeNodeFlags.DefaultOpen))
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
                if (ImGui.SmallButton(LocalizationManager.GetString("Edit") + "##" + i))
                {
                    editingCustomMessageIndex = i;
                    editingCustomMessage = message;
                }
                
                // Delete button
                ImGui.SameLine();
                if (ImGui.SmallButton(LocalizationManager.GetString("Delete") + "##" + i))
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
            ImGui.Text(LocalizationManager.GetString("AddNewMessage"));
            ImGui.SetNextItemWidth(panelWidth * 0.7f);
            ImGui.InputText("##NewCustomMessage", ref newCustomMessage, 100);
            
            ImGui.SameLine();
            if (ImGui.Button(LocalizationManager.GetString("Add")) && !string.IsNullOrWhiteSpace(newCustomMessage))
            {
                plugin.Configuration.CustomMessages.Add(newCustomMessage);
                plugin.Configuration.Save();
                newCustomMessage = string.Empty;
            }
            
            // Edit custom message dialog
            if (editingCustomMessageIndex >= 0)
            {
                ImGui.Separator();
                ImGui.Text(LocalizationManager.GetString("EditMessage"));
                ImGui.SetNextItemWidth(panelWidth * 0.7f);
                ImGui.InputText("##EditCustomMessage", ref editingCustomMessage, 100);
                
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.GetString("Save")) && !string.IsNullOrWhiteSpace(editingCustomMessage))
                {
                    plugin.Configuration.CustomMessages[editingCustomMessageIndex] = editingCustomMessage;
                    plugin.Configuration.Save();
                    editingCustomMessageIndex = -1;
                    editingCustomMessage = string.Empty;
                }
                
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.GetString("Cancel")))
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
            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.8f, 1.0f), string.Format(LocalizationManager.GetString("EditingTemplate"), currentTemplate.Name));
        }
        else if (plugin.Configuration.ActiveTemplateIndex >= 0)
        {
            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.0f, 1.0f), 
                string.Format(LocalizationManager.GetString("CurrentActiveTemplate"), plugin.Configuration.MessageTemplates[plugin.Configuration.ActiveTemplateIndex].Name));
        }
        else
        {
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.0f, 1.0f), LocalizationManager.GetString("EditCurrentMessageComponents"));
        }
        
        // Note: All separators in this section have been removed as requested
        ImGui.Text(LocalizationManager.GetString("CurrentMessageComponentList"));
        
        List<MessageComponent> componentsToEdit = currentTemplate != null ?
            currentTemplate.Components : plugin.Configuration.SelectedMessageComponents;
        
        if (componentsToEdit.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), LocalizationManager.GetString("NoComponents"));
        }
        else
        {
            // Display components with simple list view (no drag and drop)
            for (int i = 0; i < componentsToEdit.Count; i++)
            {
                // Get component text for display
                string componentText = GetComponentDisplayText(componentsToEdit[i]);
                
                ImGui.PushID($"component_{i}");
                
                // Simple display without drag functionality
                ImGui.Text($"{i + 1}. {componentText}");
                
                // Up/Down buttons for reordering with fixed width and tooltips
                ImGui.SameLine();
                string moveUpText = LocalizationManager.GetString("MoveUp");
                // Check if window width is too small for full text
                if (ImGui.GetWindowWidth() < 550)
                {
                    moveUpText = "↑"; // Use just an up arrow symbol if window is small
                }
                
                if (i > 0 && ImGui.SmallButton(moveUpText + "##" + i))
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
                    ImGui.SetTooltip(LocalizationManager.GetString("MoveUp"));
                }
                
                ImGui.SameLine();
                string moveDownText = LocalizationManager.GetString("MoveDown");
                // Check if window width is too small for full text
                if (ImGui.GetWindowWidth() < 550)
                {
                    moveDownText = "↓"; // Use just a down arrow symbol if window is small
                }
                
                if (i < componentsToEdit.Count - 1 && ImGui.SmallButton(moveDownText + "##" + i))
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
                    ImGui.SetTooltip(LocalizationManager.GetString("MoveDown"));
                }
                
                // Remove button with adaptive text based on window width
                ImGui.SameLine();
                string deleteText = LocalizationManager.GetString("Delete");
                // Check if window width is too small for full text
                if (ImGui.GetWindowWidth() < 550)
                {
                    deleteText = "X"; // Use just an X symbol if window is small
                }
                
                if (ImGui.SmallButton(deleteText + "##" + i))
                {
                    componentsToEdit.RemoveAt(i);
                    SaveComponentChanges();
                    i--; // Adjust for the removed item
                }
                
                // Add tooltip for the delete button
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(LocalizationManager.GetString("Delete"));
                }
                
                ImGui.PopID();
            }
        }
        
        ImGui.Separator();
        
        // Add components section
        ImGui.Text(LocalizationManager.GetString("AddComponents"));
        
        // Player name checkbox
        if (ImGui.Button(LocalizationManager.GetString("AddPlayerName")))
        {
            componentsToEdit.Add(new MessageComponent(MessageComponentType.PlayerName));
            SaveComponentChanges();
        }
        
        ImGui.SameLine();
        
        // Coordinates checkbox
        if (ImGui.Button(LocalizationManager.GetString("AddCoordinates")))
        {
            componentsToEdit.Add(new MessageComponent(MessageComponentType.Coordinates));
            SaveComponentChanges();
        }
        
        ImGui.SameLine();
        
        // Number button
        if (ImGui.Button(LocalizationManager.GetString("AddNumber")))
        {
            componentsToEdit.Add(new MessageComponent(MessageComponentType.Number));
            SaveComponentChanges();
        }
        
        ImGui.SameLine();
        
        // BoxedNumber button
        if (ImGui.Button(LocalizationManager.GetString("AddBoxedNumber")))
        {
            componentsToEdit.Add(new MessageComponent(MessageComponentType.BoxedNumber));
            SaveComponentChanges();
        }
        
        ImGui.SameLine();
        
        // BoxedOutlinedNumber button
        if (ImGui.Button(LocalizationManager.GetString("AddBoxedOutlinedNumber")))
        {
            componentsToEdit.Add(new MessageComponent(MessageComponentType.BoxedOutlinedNumber));
            SaveComponentChanges();
        }
        
        // Display all custom messages with direct add buttons
        if (plugin.Configuration.CustomMessages.Count > 0)
        {
            ImGui.Separator();
            ImGui.Text(LocalizationManager.GetString("AddCustomMessage"));
            
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
        ImGui.Text(LocalizationManager.GetString("MessagePreview"));
        string previewMessage = GeneratePreviewMessage(componentsToEdit);
        ImGui.TextWrapped(previewMessage);
        
        // Save changes to template
        if (currentTemplate != null)
        {
            ImGui.Separator();
            if (ImGui.Button(LocalizationManager.GetString("SaveTemplateChanges")))
            {
                SaveTemplateChanges();
            }
            
            ImGui.SameLine();
            if (ImGui.Button(LocalizationManager.GetString("Cancel")))
            {
                // Reset to original template
                selectedTemplateIndex = -1;
                UpdateCurrentTemplate();
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
            return LocalizationManager.GetString("WillOnlySendFlag");
        }
        
        var previewParts = new List<string>();
        
        foreach (var component in components)
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
    
    // Gets a display text for a component
    private string GetComponentDisplayText(MessageComponent component)
    {
        switch (component.Type)
        {
            case MessageComponentType.PlayerName:
                return LocalizationManager.GetString("PlayerName");
            case MessageComponentType.Coordinates:
                return LocalizationManager.GetString("TreasureMapCoordinates");
            case MessageComponentType.Number:
                return LocalizationManager.GetString("Number");
            case MessageComponentType.BoxedNumber:
                return LocalizationManager.GetString("BoxedNumber");
            case MessageComponentType.BoxedOutlinedNumber:
                return LocalizationManager.GetString("BoxedOutlinedNumber");
            case MessageComponentType.CustomMessage:
                if (component.CustomMessageIndex >= 0 && component.CustomMessageIndex < plugin.Configuration.CustomMessages.Count)
                {
                    return string.Format(LocalizationManager.GetString("CustomMessagePrefix"), plugin.Configuration.CustomMessages[component.CustomMessageIndex]);
                }
                return LocalizationManager.GetString("InvalidCustomMessage");
            default:
                return LocalizationManager.GetString("UnknownComponent");
        }
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
    
    // Saves changes to components
    private void SaveComponentChanges()
    {
        if (currentTemplate == null)
        {
            // Saving changes to main selected components
            plugin.Configuration.Save();
            
            // Notify other windows about the component update
            plugin.NotifyMessageTemplateUpdated();
        }
    }
    
    public void Dispose()
    {
        // Nothing to dispose
    }
}
