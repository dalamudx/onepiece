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
    public event EventHandler? MessageTemplateUpdated;

    public void NotifyMessageTemplateUpdated()
    {
        MessageTemplateUpdated?.Invoke(this, EventArgs.Empty);
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
    public MapAreaTranslationService MapAreaTranslationService { get; init; }

    public readonly WindowSystem WindowSystem = new("OnePiece");
    private MainWindow MainWindow { get; init; }
    private CustomMessageWindow CustomMessageWindow { get; init; }
    
    public void ShowCustomMessageWindow()
    {
        if (CustomMessageWindow != null)
        {
            CustomMessageWindow.IsOpen = true;
            CustomMessageWindow.BringToFront();
        }
        else
        {
            Log.Error("CustomMessageWindow is null!");
        }
    }

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this);
        ThreadSafetyHelper.InitializeMainThreadId();
        LocalizationManager.Initialize();

        TerritoryManager = new TerritoryManager(DataManager, Log);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        try
        {
            ConfigurationValidationService = new ConfigurationValidationService(this);
            var validationResult = ConfigurationValidationService.SafeValidateAndFixConfiguration();
            if (validationResult.HasIssues)
            {
                Log.Warning($"Configuration validation: {validationResult.GetSummary()}");
                foreach (var error in validationResult.Errors)
                {
                    Log.Error($"Configuration error: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize configuration validation: {ex}");
            ConfigurationValidationService = null!;
        }

        PlayerLocationService = new PlayerLocationService(ClientState, Log, TerritoryManager, GameGui, DataManager);
        AetheryteService = new AetheryteService(DataManager, ClientState, Log, TerritoryManager, ChatGui, CommandManager);
        MapAreaTranslationService = new MapAreaTranslationService(Log);
        TreasureHuntService = new TreasureHuntService(this);
        ChatMonitorService = new ChatMonitorService(this);

        LocalizationManager.SetLanguage(Configuration.Language);

        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);

        CustomMessageWindow = new CustomMessageWindow(this);
        WindowSystem.AddWindow(CustomMessageWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open One Piece treasure hunt route planner"
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        if (Configuration.EnableChatMonitoring)
        {
            ChatMonitorService.StartMonitoring();
        }

        Log.Information("One Piece Treasure Hunt Plugin Loaded");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        ChatMonitorService?.Dispose();

        if (TreasureHuntService is IDisposable treasureHuntDisposable)
            treasureHuntDisposable.Dispose();

        if (AetheryteService is IDisposable aetheryteDisposable)
            aetheryteDisposable.Dispose();

        if (PlayerLocationService is IDisposable playerLocationDisposable)
            playerLocationDisposable.Dispose();

        if (TerritoryManager is IDisposable territoryDisposable)
            territoryDisposable.Dispose();

        if (MapAreaTranslationService is IDisposable mapAreaTranslationDisposable)
            mapAreaTranslationDisposable.Dispose();

        try
        {
            ConfigurationValidationService?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error($"Error disposing ConfigurationValidationService: {ex}");
        }

        WindowSystem.RemoveAllWindows();
        MainWindow?.Dispose();
        CustomMessageWindow?.Dispose();

        CommandManager.RemoveHandler(CommandName);
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUi();
    }

    public void ToggleConfigUi() => ToggleMainUi();
    public void ToggleMainUi() => MainWindow.Toggle();

    private void DrawUi()
    {
        WindowSystem.Draw();
    }
}
