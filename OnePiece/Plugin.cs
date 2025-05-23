using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using OnePiece.Windows;
using OnePiece.Services;
using OnePiece.Localization;
using ECommons;

namespace OnePiece;

public sealed class Plugin : IDalamudPlugin
{
    // Event to notify when message templates or components have been updated
    public event EventHandler? MessageTemplateUpdated;
    
    // Method to trigger the MessageTemplateUpdated event
    public void NotifyMessageTemplateUpdated()
    {
        MessageTemplateUpdated?.Invoke(this, EventArgs.Empty);
        Log.Information("MessageTemplateUpdated event triggered");
    }
    
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    private const string CommandName = "/onepiece";

    public Configuration Configuration { get; init; }

    public TreasureHuntService TreasureHuntService { get; init; }
    public ChatMonitorService ChatMonitorService { get; init; }
    public TerritoryManager TerritoryManager { get; init; }
    public PlayerLocationService PlayerLocationService { get; init; }
    public AetheryteService AetheryteService { get; init; }

    public readonly WindowSystem WindowSystem = new("OnePiece");
    private MainWindow MainWindow { get; init; }
    private CustomMessageWindow CustomMessageWindow { get; init; }
    
    // Toggle method for custom message window visibility
    public void ShowCustomMessageWindow()
    {
        if (CustomMessageWindow != null)
        {
            Log.Information("Setting CustomMessageWindow.IsOpen = true");
            CustomMessageWindow.IsOpen = true;
            
            // Force the window to be visible - sometimes just setting IsOpen isn't enough
            CustomMessageWindow.BringToFront();
            
            // Additional log to confirm window is opened
            Log.Information("CustomMessageWindow should now be visible");
        }
        else
        {
            Log.Error("CustomMessageWindow is null!");
        }
    }

    public Plugin()
    {
        // Initialize ECommons with only the modules we need
        ECommonsMain.Init(PluginInterface, this);

        // Initialize localization
        Strings.Initialize();

        // Initialize services in the correct order
        TerritoryManager = new TerritoryManager(DataManager, Log);

        // Load configuration before initializing other services
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize player location service
        PlayerLocationService = new PlayerLocationService(ClientState, Log, TerritoryManager);

        // Initialize aetheryte service
        AetheryteService = new AetheryteService(DataManager, ClientState, Log, TerritoryManager);

        // Initialize treasure hunt service
        TreasureHuntService = new TreasureHuntService(this);

        // Initialize ChatMonitorService after TreasureHuntService and Configuration
        ChatMonitorService = new ChatMonitorService(this);

        // Set language from configuration
        Strings.SetLanguage(Configuration.Language);

        // Initialize main window (without logo)
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);
        
        // Initialize custom message window
        CustomMessageWindow = new CustomMessageWindow(this);
        WindowSystem.AddWindow(CustomMessageWindow);

        // Register command
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open One Piece treasure hunt route planner"
        });

        // Register UI events
        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Start chat monitoring if enabled in configuration
        if (Configuration.EnableChatMonitoring)
        {
            ChatMonitorService.StartMonitoring();
        }

        // Log initialization
        Log.Information($"===One Piece Treasure Hunt Plugin Loaded===");
    }

    public void Dispose()
    {
        // Dispose services
        ChatMonitorService.Dispose();

        // Dispose UI
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        CustomMessageWindow.Dispose();

        // Remove command handler
        CommandManager.RemoveHandler(CommandName);

        // Dispose ECommons
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUi();
    }

    public void ToggleConfigUi() => ToggleMainUi();
    public void ToggleMainUi() => MainWindow.Toggle();
    
    // Helper method to ensure the Draw call is happening
    private bool hasLoggedFirstDraw = false;
    private void DrawUi() 
    {
        if (!hasLoggedFirstDraw)
        {
            Log.Information("First DrawUi call");
            hasLoggedFirstDraw = true;
        }
        
        // We previously had logging here for CustomMessageWindow, but it was causing a loop issue
        // Removed the logging to prevent excessive log output
        
        WindowSystem.Draw();
    }
}
