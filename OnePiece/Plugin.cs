using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using OnePiece.Windows;
using OnePiece.Services;
using OnePiece.Localization;
using OnePiece.Helpers;
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
    public ConfigurationValidationService ConfigurationValidationService { get; init; }
    public OptimizedLoggingService OptimizedLoggingService { get; init; }

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

        // Initialize thread safety helper
        ThreadSafetyHelper.InitializeMainThreadId();

        // Initialize localization
        LocalizationManager.Initialize();

        // Initialize services in the correct order
        TerritoryManager = new TerritoryManager(DataManager, Log);

        // Load configuration before initializing other services
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize configuration validation service
        try
        {
            ConfigurationValidationService = new ConfigurationValidationService(this);

            // Initialize optimized logging service
            OptimizedLoggingService = new OptimizedLoggingService(Log, Configuration);

            // Validate and fix configuration issues using safe method
            var validationResult = ConfigurationValidationService.SafeValidateAndFixConfiguration();
            if (validationResult.HasIssues)
            {
                OptimizedLoggingService.LogWarning($"Configuration validation: {validationResult.GetSummary()}", "Configuration");
                foreach (var error in validationResult.Errors)
                {
                    OptimizedLoggingService.LogError($"Configuration error: {error}", "Configuration");
                }
                foreach (var warning in validationResult.Warnings)
                {
                    OptimizedLoggingService.LogWarning($"Configuration warning: {warning}", "Configuration");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize configuration validation: {ex}");
            // Continue with plugin initialization even if validation fails
            ConfigurationValidationService = null!;
            OptimizedLoggingService = new OptimizedLoggingService(Log, Configuration);
        }

        // Initialize player location service
        PlayerLocationService = new PlayerLocationService(ClientState, Log, TerritoryManager, GameGui, DataManager);

        // Initialize aetheryte service
        AetheryteService = new AetheryteService(DataManager, ClientState, Log, TerritoryManager, ChatGui, CommandManager);

        // Initialize treasure hunt service
        TreasureHuntService = new TreasureHuntService(this);

        // Initialize ChatMonitorService after TreasureHuntService and Configuration
        ChatMonitorService = new ChatMonitorService(this);

        // Set language from configuration
        LocalizationManager.SetLanguage(Configuration.Language);

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
        // Unsubscribe from UI events first
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        // Dispose services in reverse order of initialization
        ChatMonitorService?.Dispose();

        // Dispose other services if they implement IDisposable
        if (TreasureHuntService is IDisposable treasureHuntDisposable)
            treasureHuntDisposable.Dispose();

        if (AetheryteService is IDisposable aetheryteDisposable)
            aetheryteDisposable.Dispose();

        if (PlayerLocationService is IDisposable playerLocationDisposable)
            playerLocationDisposable.Dispose();

        if (TerritoryManager is IDisposable territoryDisposable)
            territoryDisposable.Dispose();

        try
        {
            ConfigurationValidationService?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error($"Error disposing ConfigurationValidationService: {ex}");
        }

        try
        {
            OptimizedLoggingService?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error($"Error disposing OptimizedLoggingService: {ex}");
        }

        // Dispose UI
        WindowSystem.RemoveAllWindows();
        MainWindow?.Dispose();
        CustomMessageWindow?.Dispose();

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
