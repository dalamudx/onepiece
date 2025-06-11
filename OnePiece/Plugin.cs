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
    public MapAreaTranslationService MapAreaTranslationService { get; init; }
    public PlayerNameProcessingService PlayerNameProcessingService { get; init; }

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
            Plugin.Log.Error("CustomMessageWindow is null!");
        }
    }

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this);
        ThreadSafetyHelper.InitializeMainThreadId();
        LocalizationManager.Initialize();

        TerritoryManager = new TerritoryManager(DataManager);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Simple configuration validation and fix
        try
        {
            Configuration.ValidateAndFix();
            Configuration.Save();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to validate configuration: {ex}");
        }

        PlayerLocationService = new PlayerLocationService(ClientState, TerritoryManager, GameGui, DataManager);
        AetheryteService = new AetheryteService(ClientState);
        MapAreaTranslationService = new MapAreaTranslationService();
        PlayerNameProcessingService = new PlayerNameProcessingService();
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

        Plugin.Log.Information("One Piece Treasure Hunt Plugin Loaded");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        ChatMonitorService?.Dispose();
        TreasureHuntService?.Dispose();
        AetheryteService?.Dispose();

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
