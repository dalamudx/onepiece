using System;
using System.Collections.Generic;
using System.Globalization;
using Dalamud.Plugin.Services;

namespace OnePiece.Localization;

/// <summary>
/// Handles localization for the plugin.
/// </summary>
public class Strings
{
    private static readonly IPluginLog Log = Plugin.Log;
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new();
    private static string CurrentLanguage = "en";

    /// <summary>
    /// Initializes the localization system.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // Set default language based on client culture
            var clientCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            CurrentLanguage = IsLanguageSupported(clientCulture) ? clientCulture : "en";

            // Initialize translations
            InitializeTranslations();

            Log.Information($"Localization initialized with language: {CurrentLanguage}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error initializing localization: {ex.Message}");
            CurrentLanguage = "en"; // Fallback to English
        }
    }

    /// <summary>
    /// Gets a localized string.
    /// </summary>
    /// <param name="key">The string key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    public static string GetString(string key)
    {
        if (Translations.TryGetValue(CurrentLanguage, out var languageStrings) &&
            languageStrings.TryGetValue(key, out var translation))
        {
            return translation;
        }

        // Fallback to English
        if (CurrentLanguage != "en" &&
            Translations.TryGetValue("en", out var englishStrings) &&
            englishStrings.TryGetValue(key, out var englishTranslation))
        {
            return englishTranslation;
        }

        // If all else fails, return the key itself
        return key;
    }

    /// <summary>
    /// Sets the current language.
    /// </summary>
    /// <param name="language">The language code (e.g., "en", "ja").</param>
    /// <returns>True if the language was set successfully, false otherwise.</returns>
    public static bool SetLanguage(string language)
    {
        if (!IsLanguageSupported(language))
        {
            Log.Error($"Language not supported: {language}");
            return false;
        }

        CurrentLanguage = language;
        Log.Information($"Language set to: {language}");
        return true;
    }

    /// <summary>
    /// Gets the current language.
    /// </summary>
    /// <returns>The current language code.</returns>
    public static string GetCurrentLanguage() => CurrentLanguage;

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    /// <returns>An array of supported language codes.</returns>
    public static string[] GetSupportedLanguages() => new[] { "en", "ja", "de", "fr", "zh" };

    /// <summary>
    /// Checks if a language is supported.
    /// </summary>
    /// <param name="language">The language code to check.</param>
    /// <returns>True if the language is supported, false otherwise.</returns>
    public static bool IsLanguageSupported(string language) =>
        Array.IndexOf(GetSupportedLanguages(), language) >= 0;

    private static void InitializeTranslations()
    {
        // English (default)
        var en = new Dictionary<string, string>
        {
            // Main window
            { "MainWindowTitle", "One Piece" },
            { "MainWindowSubtitle", "Plan and optimize your treasure hunt route" },
            { "Edit", "Edit" },
            { "Settings", "Settings" },
            { "HideSettings", "Hide Settings" },
            { "ClearAll", "Clear All" },
            { "OptimizeRoute", "Optimize Route" },
            { "ResetOptimization", "Reset Optimization" },
            { "NoCoordinatesToOptimize", "No coordinates to optimize. Please import coordinates first." },
            { "NoCoordinates", "No coordinates imported yet." },
            { "UseImportSection", "Use the import section above to add coordinates." },
            { "Coordinates", "Coordinates: {0}" },
            { "CoordinatesWithCount", "Coordinates ({0})" },
            { "TotalRouteDistance", "Total Route Distance: {0:F1}" },
            { "OptimizedRoute", "Optimized Route:" },
            { "OptimizedRouteWithCount", "Optimized Route ({0}):" },
            { "Collected", "Collected" },
            { "Delete", "Delete" },
            { "Restore", "Restore" },
            { "SendToChat", "Send to Chat" },
            { "TrashBin", "Trash Bin" },
            { "TrashBinWithCount", "Trash Bin ({0})" },
            { "ClearTrash", "Clear Trash" },
            { "EmptyTrashBin", "Trash bin is empty." },
            { "ChannelSettings", "Channel Settings" },
            { "EnableChatMonitoring", "Enable Channel Monitoring" },
            { "SelectChatChannel", "Select Channel:" },
            { "StartMonitoring", "Start" },
            { "StopMonitoring", "Stop" },
            { "MonitoringActive", "Active: {0}" },
            { "CurrentMonitoredChannel", "Current Channel: {0}" },
            { "PlayerCoordinates", "Player: {0}, Coordinates: {1}" },
            { "UnknownArea", "Unknown Area" },
            { "MapArea", "Map Area: {0}" },
            
            // Login status messages
            { "NotLoggedIn", "Please log into the game to use this plugin." },

            // General settings section
            { "GeneralSettings", "General Settings" },

            { "Language", "Language" },
            { "LogSettings", "Log Settings" },
            { "LogLevel", "Log Level" },
            { "LogLevelMinimal", "Off" },
            { "LogLevelNormal", "Info" },
            { "LogLevelVerbose", "Debug" },
            { "LogLevelMinimalTooltip", "Turn off all logging" },
            { "LogLevelNormalTooltip", "Show normal log messages" },
            { "LogLevelVerboseTooltip", "Show detailed debug information" },

            // Chat channels
            { "Say", "Say" },
            { "Yell", "Yell" },
            { "Shout", "Shout" },
            { "Party", "Party" },
            { "Alliance", "Alliance" },
            { "FreeCompany", "Free Company" },
            { "LinkShell1", "Linkshell 1" },
            { "LinkShell2", "Linkshell 2" },
            { "LinkShell3", "Linkshell 3" },
            { "LinkShell4", "Linkshell 4" },
            { "LinkShell5", "Linkshell 5" },
            { "LinkShell6", "Linkshell 6" },
            { "LinkShell7", "Linkshell 7" },
            { "LinkShell8", "Linkshell 8" },
            { "CrossWorldLinkShell1", "Cross-world Linkshell 1" },
            { "CrossWorldLinkShell2", "Cross-world Linkshell 2" },
            { "CrossWorldLinkShell3", "Cross-world Linkshell 3" },
            { "CrossWorldLinkShell4", "Cross-world Linkshell 4" },
            { "CrossWorldLinkShell5", "Cross-world Linkshell 5" },
            { "CrossWorldLinkShell6", "Cross-world Linkshell 6" },
            { "CrossWorldLinkShell7", "Cross-world Linkshell 7" },
            { "CrossWorldLinkShell8", "Cross-world Linkshell 8" },

            // Messages
            { "CoordinatesImported", "{0} coordinates imported." },
            { "CoordinatesImportedFromClipboard", "Imported {0} coordinates from clipboard." },
            { "NoCoordinatesImported", "No coordinates were imported." },
            { "ClipboardEmpty", "Clipboard is empty." },
            { "CoordinatesExportedToClipboard", "Coordinates exported to clipboard." },
            { "Export", "Export" },
            { "Import", "Import" },
            { "CoordinateDetected", "Coordinate detected from {0}: {1}" },
            { "RouteOptimized", "Route optimized with {0} points." },
            { "Error", "Error" },
            { "Success", "Success" },
            
            // Teleport related
            { "TeleportButton", "Teleport" },
            { "TeleportingTo", "Teleporting to {0} (ID: {1})..." },
            { "TeleportTo", "Teleport to: {0}" },
            { "TeleportCost", "Cost: {0} gil" },
            { "TeleportCostUnknown", "Cost: Unknown" },
            
            // Custom message settings
            { "CustomMessageSettings", "Custom Message Settings" },
            { "MessageSettings", "Message Settings" },
            { "OpenSettingsWindow", "Open Settings Window" },
            { "MessageTemplateManagement", "Message Template Management" },
            { "SavedTemplates", "Saved Templates:" },
            { "NoSavedTemplates", "No saved templates yet" },
            { "SetAsActiveTemplate", "Set as Active Template" },
            { "ClearActiveTemplate", "Clear Active Template" },
            { "DeleteTemplate", "Delete Template" },
            { "CreateNewTemplate", "Create New Template" },
            { "TemplateName", "Template Name:" },
            { "Create", "Create" },
            { "CustomMessages", "Custom Messages" },
            { "AddNewMessage", "Add New Message:" },
            { "Add", "Add" },
            { "EditMessage", "Edit Message:" },
            { "Save", "Save" },
            { "Cancel", "Cancel" },
            { "EditingTemplate", "Editing Template: {0}" },
            { "CurrentActiveTemplate", "Current Active Template: {0}" },
            { "EditCurrentMessageComponents", "Edit Current Message Components (No Active Template)" },
            { "CurrentMessageComponentList", "Current Message Component List:" },
            { "NoComponents", "No components yet, please add from below" },
            { "AddComponents", "Add Components:" },
            // Custom message UI components
            { "AddPlayerName", "Player Name" },
            { "AddCoordinates", "Coordinates" },
            { "AddNumber", "Number (1-8)" },
            { "AddBoxedNumber", "Boxed Number (1-8)" },
            { "AddBoxedOutlinedNumber", "Boxed Outlined Number (1-8)" },
            { "AddCustomMessage", "Custom Message:" },
            { "MessagePreview", "Message Preview:" },
            { "SaveTemplateChanges", "Save Template Changes" },
            { "NoActiveMessageTemplate", "No active message template" },
            { "PlayerName", "Player Name" },
            { "TreasureMapCoordinates", "Treasure Map Coordinates" },
            { "Number", "Number (1-8)" },
            { "BoxedNumber", "Boxed Number (1-8)" },
            { "BoxedOutlinedNumber", "Boxed Outlined Number (1-8)" },
            { "CustomMessagePrefix", "Custom: {0}" },
            { "InvalidCustomMessage", "Invalid Custom Message" },
            { "UnknownComponent", "Unknown Component" },
            { "WillOnlySendFlag", "Will only send treasure map flag" },
            { "OnlyTreasureMarker", "Will only send treasure map marker" },
            { "PlayerNamePreview", "[Player Name]" },
            { "TreasureMapCoordinatesPreview", "[Treasure Map Coordinates]" },
            { "NumberPreview", "[Number based on map index]" },
            { "BoxedNumberPreview", "[Boxed number based on map index]" },
            { "BoxedOutlinedNumberPreview", "[Boxed outlined number based on map index]" },
            { "MoveUp", "Move Up" },
            { "MoveDown", "Move Down" }
        };
        Translations["en"] = en;

        // Japanese
        var ja = new Dictionary<string, string>
        {
            // Main window
            { "MainWindowTitle", "One Piece" },
            { "MainWindowSubtitle", "宝の地図の計画と最適化" },
            { "Edit", "編集" },
            { "Settings", "設定" },
            { "HideSettings", "設定を隠す" },
            { "ClearAll", "すべてクリア" },
            { "OptimizeRoute", "ルート最適化" },
            { "ResetOptimization", "最適化をリセット" },
            { "NoCoordinates", "まだ座標がインポートされていません。" },
            { "UseImportSection", "上のインポートセクションを使用して座標を追加してください。" },
            { "Coordinates", "座標: {0}" },
            { "CoordinatesWithCount", "座標 ({0})" },
            { "TotalRouteDistance", "総ルート距離: {0:F1}" },
            { "OptimizedRoute", "最適化されたルート:" },
            { "OptimizedRouteWithCount", "最適化されたルート ({0}):" },
            { "Collected", "収集済み" },
            { "Delete", "削除" },
            { "Restore", "復元" },
            { "SendToChat", "チャットに送信" },
            { "TrashBin", "ごみ箱" },
            { "TrashBinWithCount", "ごみ箱 ({0})" },
            { "ClearTrash", "ごみ箱を空にする" },
            { "EmptyTrashBin", "ごみ箱は空です。" },
            { "ChannelSettings", "チャンネル設定" },
            { "EnableChatMonitoring", "チャンネル監視を有効にする" },
            { "SelectChatChannel", "チャンネルを選択:" },
            { "StartMonitoring", "開始" },
            { "StopMonitoring", "停止" },
            { "MonitoringActive", "アクティブ: {0}" },
            { "CurrentMonitoredChannel", "現在のチャンネル: {0}" },
            { "PlayerCoordinates", "プレイヤー: {0}, 座標: {1}" },
            { "UnknownArea", "不明なエリア" },
            { "MapArea", "マップエリア: {0}" },
            
            // Login status messages
            { "NotLoggedIn", "このプラグインを使用するには、ゲームにログインしてください。" },

            // General settings section
            { "GeneralSettings", "一般設定" },

            { "Language", "言語" },
            { "LogSettings", "ログ設定" },
            { "LogLevel", "ログレベル" },
            { "LogLevelMinimal", "オフ" },
            { "LogLevelNormal", "情報" },
            { "LogLevelVerbose", "デバッグ" },
            { "LogLevelMinimalTooltip", "すべてのログをオフにする" },
            { "LogLevelNormalTooltip", "通常のログメッセージを表示" },
            { "LogLevelVerboseTooltip", "詳細なデバッグ情報を表示" },

            // Custom message settings
            { "CustomMessageSettings", "カスタムメッセージ設定" },
            { "MessageSettings", "メッセージ設定" },
            { "OpenSettingsWindow", "設定ウィンドウを開く" },
            { "MessageTemplateManagement", "メッセージテンプレート管理" },
            { "SavedTemplates", "保存したテンプレート:" },
            { "NoSavedTemplates", "まだテンプレートがありません" },
            { "SetAsActiveTemplate", "アクティブテンプレートとして設定" },
            { "ClearActiveTemplate", "アクティブテンプレートをクリア" },
            { "DeleteTemplate", "テンプレートを削除" },
            { "CreateNewTemplate", "新しいテンプレートを作成" },
            { "TemplateName", "テンプレート名:" },
            { "Create", "作成" },
            { "CustomMessages", "カスタムメッセージ" },
            { "AddNewMessage", "新しいメッセージを追加:" },
            { "Add", "追加" },
            { "EditMessage", "メッセージを編集:" },
            { "Save", "保存" },
            { "Cancel", "キャンセル" },
            { "EditingTemplate", "編集中のテンプレート: {0}" },
            { "CurrentActiveTemplate", "現在のアクティブテンプレート: {0}" },
            { "EditCurrentMessageComponents", "現在のメッセージコンポーネントを編集（アクティブテンプレートなし）" },
            { "CurrentMessageComponentList", "現在のメッセージコンポーネントリスト:" },
            { "NoComponents", "まだコンポーネントがありません、下から追加してください" },
            { "AddComponents", "コンポーネントを追加:" },
            // Custom message UI components
            { "AddPlayerName", "プレイヤー名を追加" },
            { "AddCoordinates", "座標を追加" },
            { "AddNumber", "番号を追加" },
            { "AddBoxedNumber", "ボックス番号を追加" },
            { "AddBoxedOutlinedNumber", "枠付き番号を追加" },
            { "AddCustomMessage", "カスタムメッセージ:" },
            { "MessagePreview", "メッセージプレビュー:" },
            { "SaveTemplateChanges", "テンプレートの変更を保存" },
            { "NoActiveMessageTemplate", "アクティブなメッセージテンプレートがありません" },
            { "PlayerName", "プレイヤー名" },
            { "TreasureMapCoordinates", "宝の地図の座標" },
            { "Number", "番号 (1-8)" },
            { "BoxedNumber", "図形付き番号 (1-8)" },
            { "BoxedOutlinedNumber", "枠付き番号 (1-8)" },
            { "CustomMessagePrefix", "カスタム: {0}" },
            { "InvalidCustomMessage", "無効なカスタムメッセージ" },
            { "UnknownComponent", "不明なコンポーネント" },
            { "WillOnlySendFlag", "宝の地図のフラグのみを送信します" },
            { "OnlyTreasureMarker", "宝の地図のマーカーのみを送信します" },
            { "PlayerNamePreview", "[プレイヤー名]" },
            { "TreasureMapCoordinatesPreview", "[宝の地図の座標]" },
            { "NumberPreview", "[マップインデックスに基づく番号]" },
            { "BoxedNumberPreview", "[マップインデックスに基づく図形付き番号]" },
            { "BoxedOutlinedNumberPreview", "[マップインデックスに基づく枠付き番号]" },
            { "MoveUp", "上に移動" },
            { "MoveDown", "下に移動" },
            
            // Chat channels
            { "Say", "会話" },
            { "Yell", "叫び" },
            { "Shout", "呼びかけ" },
            { "Party", "パーティ" },
            { "Alliance", "アライアンス" },
            { "FreeCompany", "フリーカンパニー" },
            { "LinkShell1", "リンクシェル 1" },
            { "LinkShell2", "リンクシェル 2" },
            { "LinkShell3", "リンクシェル 3" },
            { "LinkShell4", "リンクシェル 4" },
            { "LinkShell5", "リンクシェル 5" },
            { "LinkShell6", "リンクシェル 6" },
            { "LinkShell7", "リンクシェル 7" },
            { "LinkShell8", "リンクシェル 8" },
            { "CrossWorldLinkShell1", "クロスワールドリンクシェル 1" },
            { "CrossWorldLinkShell2", "クロスワールドリンクシェル 2" },
            { "CrossWorldLinkShell3", "クロスワールドリンクシェル 3" },
            { "CrossWorldLinkShell4", "クロスワールドリンクシェル 4" },
            { "CrossWorldLinkShell5", "クロスワールドリンクシェル 5" },
            { "CrossWorldLinkShell6", "クロスワールドリンクシェル 6" },
            { "CrossWorldLinkShell7", "クロスワールドリンクシェル 7" },
            { "CrossWorldLinkShell8", "クロスワールドリンクシェル 8" },

            // Messages
            { "CoordinatesImported", "{0}座標がインポートされました。" },
            { "CoordinatesImportedFromClipboard", "クリップボードから{0}座標をインポートしました。" },
            { "NoCoordinatesImported", "座標はインポートされませんでした。" },
            { "ClipboardEmpty", "クリップボードが空です。" },
            { "CoordinatesExportedToClipboard", "座標がクリップボードにエクスポートされました。" },
            { "Export", "エクスポート" },
            { "Import", "インポート" },
            { "CoordinateDetected", "{0}から座標を検出しました: {1}" },
            { "RouteOptimized", "{0}ポイントでルートが最適化されました。" },
            { "Error", "エラー" },
            { "Success", "成功" },
            
            // Teleport related
            { "TeleportButton", "テレポ" },
            { "TeleportingTo", "{0} (ID: {1})にテレポします..." },
            { "TeleportTo", "テレポ先: {0}" },
            { "TeleportCost", "料金: {0} ギル" },
            { "TeleportCostUnknown", "料金: 不明" }
        };
        Translations["ja"] = ja;

        // Chinese
        var zh = new Dictionary<string, string>
        {
            // Main window
            { "MainWindowTitle", "One Piece" },
            { "MainWindowSubtitle", "计划和优化你的寻宝路线" },
            { "Edit", "编辑" },
            { "Settings", "设置" },
            { "HideSettings", "隐藏设置" },
            { "ClearAll", "清除全部" },
            { "OptimizeRoute", "优化路线" },
            { "ResetOptimization", "重置优化" },
            { "NoCoordinatesToOptimize", "没有可优化的坐标。请先导入坐标。" },
            { "NoCoordinates", "尚未导入藏宝图坐标。" },
            { "UseImportSection", "使用上方的导入部分添加藏宝图坐标。" },
            { "Coordinates", "藏宝图坐标: {0}" },
            { "CoordinatesWithCount", "藏宝图坐标 ({0})" },
            { "TotalRouteDistance", "总路线距离: {0:F1}" },
            { "OptimizedRoute", "优化路线:" },
            { "OptimizedRouteWithCount", "优化路线 ({0}):" },
            { "Collected", "已收集" },
            { "Delete", "删除" },
            { "Restore", "恢复" },
            { "SendToChat", "发送到聊天" },
            { "TrashBin", "垃圾箱" },
            { "TrashBinWithCount", "垃圾箱 ({0})" },
            { "ClearTrash", "清空垃圾箱" },
            { "EmptyTrashBin", "垃圾箱是空的。" },
            { "ChannelSettings", "频道设置" },
            { "EnableChatMonitoring", "启用频道监控" },
            { "SelectChatChannel", "选择频道:" },
            { "StartMonitoring", "开始" },
            { "StopMonitoring", "停止" },
            { "MonitoringActive", "活动状态: {0}" },
            { "CurrentMonitoredChannel", "当前频道: {0}" },
            { "PlayerCoordinates", "玩家: {0}, 藏宝图坐标: {1}" },
            { "UnknownArea", "未知区域" },
            { "MapArea", "地图区域: {0}" },
            
            // Login status messages
            { "NotLoggedIn", "请登录游戏后再使用此插件。" },

            // General settings section
            { "GeneralSettings", "常规设置" },

            { "Language", "语言" },
            { "LogSettings", "日志设置" },
            { "LogLevel", "日志级别" },
            { "LogLevelMinimal", "关闭" },
            { "LogLevelNormal", "信息" },
            { "LogLevelVerbose", "调试" },
            { "LogLevelMinimalTooltip", "关闭所有日志" },
            { "LogLevelNormalTooltip", "显示普通日志消息" },
            { "LogLevelVerboseTooltip", "显示详细调试信息" },

            // Custom message settings
            { "CustomMessageSettings", "自定义消息设置" },
            { "MessageSettings", "消息设置" },
            { "OpenSettingsWindow", "打开设置窗口" },
            { "MessageTemplateManagement", "消息模板管理" },
            { "SavedTemplates", "已保存的模板:" },
            { "NoSavedTemplates", "暂无保存的模板" },
            { "SetAsActiveTemplate", "设置为活动模板" },
            { "ClearActiveTemplate", "取消活动模板" },
            { "DeleteTemplate", "删除模板" },
            { "CreateNewTemplate", "创建新模板" },
            { "TemplateName", "模板名称:" },
            { "Create", "创建" },
            { "CustomMessages", "自定义消息" },
            { "AddNewMessage", "添加新消息:" },
            { "Add", "添加" },
            { "EditMessage", "编辑消息:" },
            { "Save", "保存" },
            { "Cancel", "取消" },
            { "EditingTemplate", "正在编辑模板: {0}" },
            { "CurrentActiveTemplate", "当前活动模板: {0}" },
            { "EditCurrentMessageComponents", "编辑当前消息组件（无活动模板）" },
            { "CurrentMessageComponentList", "当前消息组件列表:" },
            { "NoComponents", "暂无组件，请从下方添加" },
            { "AddComponents", "添加组件:" },
            // Custom message UI components
            { "AddPlayerName", "玩家名称" },
            { "AddCoordinates", "坐标" },
            { "AddNumber", "数字 (1-8)" },
            { "AddBoxedNumber", "方框数字 (1-8)" },
            { "AddBoxedOutlinedNumber", "轮廓方框数字 (1-8)" },
            { "AddCustomMessage", "自定义消息:" },
            { "MessagePreview", "消息预览:" },
            { "SaveTemplateChanges", "保存模板更改" },
            { "NoActiveMessageTemplate", "没有活动的消息模板" },
            { "PlayerName", "玩家名称" },
            { "TreasureMapCoordinates", "藏宝图坐标" },
            { "Number", "数字 (1-8)" },
            { "BoxedNumber", "方框数字 (1-8)" },
            { "BoxedOutlinedNumber", "轮廓方框数字 (1-8)" },
            { "CustomMessagePrefix", "自定义: {0}" },
            { "InvalidCustomMessage", "无效的自定义消息" },
            { "UnknownComponent", "未知组件" },
            { "WillOnlySendFlag", "将只发送宝藏图标记" },
            { "OnlyTreasureMarker", "将只发送宝藏图标签" },
            { "PlayerNamePreview", "[玩家名称]" },
            { "TreasureMapCoordinatesPreview", "[藏宝图坐标]" },
            { "NumberPreview", "[基于地图索引的数字]" },
            { "BoxedNumberPreview", "[基于地图索引的方框数字]" },
            { "BoxedOutlinedNumberPreview", "[基于地图索引的轮廓方框数字]" },
            { "MoveUp", "上移" },
            { "MoveDown", "下移" },
            
            // Chat channels
            { "Say", "说话" },
            { "Yell", "呼喊" },
            { "Shout", "喊话" },
            { "Party", "小队" },
            { "Alliance", "团队" },
            { "FreeCompany", "部队" },
            { "LinkShell1", "通讯贝 1" },
            { "LinkShell2", "通讯贝 2" },
            { "LinkShell3", "通讯贝 3" },
            { "LinkShell4", "通讯贝 4" },
            { "LinkShell5", "通讯贝 5" },
            { "LinkShell6", "通讯贝 6" },
            { "LinkShell7", "通讯贝 7" },
            { "LinkShell8", "通讯贝 8" },
            { "CrossWorldLinkShell1", "跨服通讯贝 1" },
            { "CrossWorldLinkShell2", "跨服通讯贝 2" },
            { "CrossWorldLinkShell3", "跨服通讯贝 3" },
            { "CrossWorldLinkShell4", "跨服通讯贝 4" },
            { "CrossWorldLinkShell5", "跨服通讯贝 5" },
            { "CrossWorldLinkShell6", "跨服通讯贝 6" },
            { "CrossWorldLinkShell7", "跨服通讯贝 7" },
            { "CrossWorldLinkShell8", "跨服通讯贝 8" },

            // Messages
            { "CoordinatesImported", "{0} 个坐标已导入。" },
            { "CoordinatesImportedFromClipboard", "从剪贴板导入了{0} 个坐标。" },
            { "NoCoordinatesImported", "没有导入任何坐标。" },
            { "ClipboardEmpty", "剪贴板为空。" },
            { "CoordinatesExportedToClipboard", "坐标已导出到剪贴板。" },
            { "Export", "导出" },
            { "Import", "导入" },
            { "CoordinateDetected", "从{0}检测到坐标: {1}" },
            { "RouteOptimized", "已优化包含{0} 个点的路线。" },
            { "Error", "错误" },
            { "Success", "成功" },
            
            // Teleport related
            { "TeleportButton", "传送" },
            { "TeleportingTo", "正在传送至 {0} (ID: {1})..." },
            { "TeleportTo", "传送到: {0}" },
            { "TeleportCost", "花费: {0} 金币" },
            { "TeleportCostUnknown", "花费: 未知" }
        };
        Translations["zh"] = zh;

        // German
        var de = new Dictionary<string, string>
        {
            // Main window
            { "MainWindowTitle", "One Piece" },
            { "MainWindowSubtitle", "Plane und optimiere deine Schatzsuche" },
            { "Edit", "Bearbeiten" },
            { "Settings", "Einstellungen" },
            { "HideSettings", "Einstellungen ausblenden" },
            { "ClearAll", "Alles löschen" },
            { "OptimizeRoute", "Route optimieren" },
            { "ResetOptimization", "Optimierung zurücksetzen" },
            { "NoCoordinates", "Noch keine Koordinaten importiert." },
            { "UseImportSection", "Verwenden Sie den Import-Bereich oben, um Koordinaten hinzuzufügen." },
            { "Coordinates", "Koordinaten: {0}" },
            { "CoordinatesWithCount", "Koordinaten ({0})" },
            { "TotalRouteDistance", "Gesamtstrecke: {0:F1}" },
            { "OptimizedRoute", "Optimierte Route:" },
            { "OptimizedRouteWithCount", "Optimierte Route ({0}):" },
            { "Collected", "Gesammelt" },
            { "Delete", "Löschen" },
            { "Restore", "Wiederherstellen" },
            { "SendToChat", "An Chat senden" },
            { "TrashBin", "Papierkorb" },
            { "TrashBinWithCount", "Papierkorb ({0})" },
            { "ClearTrash", "Papierkorb leeren" },
            { "EmptyTrashBin", "Papierkorb ist leer." },
            { "ChannelSettings", "Kanal-Einstellungen" },
            { "EnableChatMonitoring", "Kanal-Überwachung aktivieren" },
            { "SelectChatChannel", "Kanal auswählen:" },
            { "StartMonitoring", "Start" },
            { "StopMonitoring", "Stopp" },
            { "MonitoringActive", "Aktiv: {0}" },
            { "CurrentMonitoredChannel", "Aktueller Kanal: {0}" },
            { "PlayerCoordinates", "Spieler: {0}, Koordinaten: {1}" },
            { "UnknownArea", "Unbekanntes Gebiet" },
            { "MapArea", "Kartengebiet: {0}" },
            
            // Login status messages
            { "NotLoggedIn", "Bitte melden Sie sich im Spiel an, um dieses Plugin zu verwenden." },

            // General settings section
            { "GeneralSettings", "Allgemeine Einstellungen" },

            { "Language", "Sprache" },
            { "LogSettings", "Protokoll-Einstellungen" },
            { "LogLevel", "Protokollierungsstufe" },
            { "LogLevelMinimal", "Aus" },
            { "LogLevelNormal", "Info" },
            { "LogLevelVerbose", "Debug" },
            { "LogLevelMinimalTooltip", "Alle Protokollierung ausschalten" },
            { "LogLevelNormalTooltip", "Normale Protokollmeldungen anzeigen" },
            { "LogLevelVerboseTooltip", "Detaillierte Debug-Informationen anzeigen" },

            // Custom message settings
            { "CustomMessageSettings", "Benutzerdefinierte Nachrichteneinstellungen" },
            { "MessageSettings", "Nachrichteneinstellungen" },
            { "OpenSettingsWindow", "Einstellungsfenster öffnen" },
            { "MessageTemplateManagement", "Nachrichtenvorlagen-Verwaltung" },
            { "SavedTemplates", "Gespeicherte Vorlagen:" },
            { "NoSavedTemplates", "Noch keine Vorlagen gespeichert" },
            { "SetAsActiveTemplate", "Als aktive Vorlage festlegen" },
            { "ClearActiveTemplate", "Aktive Vorlage entfernen" },
            { "DeleteTemplate", "Vorlage löschen" },
            { "CreateNewTemplate", "Neue Vorlage erstellen" },
            { "TemplateName", "Vorlagenname:" },
            { "Create", "Erstellen" },
            { "CustomMessages", "Benutzerdefinierte Nachrichten" },
            { "AddNewMessage", "Neue Nachricht hinzufügen:" },
            { "Add", "Hinzufügen" },
            { "EditMessage", "Nachricht bearbeiten:" },
            { "Save", "Speichern" },
            { "Cancel", "Abbrechen" },
            { "EditingTemplate", "Vorlage bearbeiten: {0}" },
            { "CurrentActiveTemplate", "Aktuelle aktive Vorlage: {0}" },
            { "EditCurrentMessageComponents", "Aktuelle Nachrichtenkomponenten bearbeiten (Keine aktive Vorlage)" },
            { "CurrentMessageComponentList", "Aktuelle Nachrichtenkomponentenliste:" },
            { "NoComponents", "Noch keine Komponenten, bitte unten hinzufügen" },
            { "AddComponents", "Komponenten hinzufügen:" },
            // Custom message UI components
            { "AddPlayerName", "Spielername" },
            { "AddCoordinates", "Koordinaten" },
            { "AddNumber", "Nummer (1-8)" },
            { "AddBoxedNumber", "Boxnummer (1-8)" },
            { "AddBoxedOutlinedNumber", "Umrandete Nummer (1-8)" },
            { "AddCustomMessage", "Benutzerdefinierte Nachricht:" },
            { "MessagePreview", "Nachrichtenvorschau:" },
            { "SaveTemplateChanges", "Vorlagenänderungen speichern" },
            { "NoActiveMessageTemplate", "Keine aktive Nachrichtenvorlage" },
            { "PlayerName", "Spielername" },
            { "TreasureMapCoordinates", "Schatzkarten-Koordinaten" },
            { "Number", "Nummer (1-8)" },
            { "BoxedNumber", "Umrahmte Nummer (1-8)" },
            { "BoxedOutlinedNumber", "Umrandete Nummer (1-8)" },
            { "CustomMessagePrefix", "Benutzerdefiniert: {0}" },
            { "InvalidCustomMessage", "Ungültige benutzerdefinierte Nachricht" },
            { "UnknownComponent", "Unbekannte Komponente" },
            { "WillOnlySendFlag", "Sendet nur die Schatzkarten-Markierung" },
            { "OnlyTreasureMarker", "Wird nur Schatzkarten-Markierung senden" },
            { "PlayerNamePreview", "[Spielername]" },
            { "TreasureMapCoordinatesPreview", "[Schatzkarten-Koordinaten]" },
            { "NumberPreview", "[Nummer basierend auf Kartenindex]" },
            { "BoxedNumberPreview", "[Umrahmte Nummer basierend auf Kartenindex]" },
            { "BoxedOutlinedNumberPreview", "[Umrandete Nummer basierend auf Kartenindex]" },
            { "MoveUp", "Nach oben" },
            { "MoveDown", "Nach unten" },
            
            // Chat channels
            { "Say", "Sagen" },
            { "Yell", "Rufen" },
            { "Shout", "Schreien" },
            { "Party", "Gruppe" },
            { "Alliance", "Allianz" },
            { "FreeCompany", "Freie Gesellschaft" },
            { "LinkShell1", "Linkshell 1" },
            { "LinkShell2", "Linkshell 2" },
            { "LinkShell3", "Linkshell 3" },
            { "LinkShell4", "Linkshell 4" },
            { "LinkShell5", "Linkshell 5" },
            { "LinkShell6", "Linkshell 6" },
            { "LinkShell7", "Linkshell 7" },
            { "LinkShell8", "Linkshell 8" },
            { "CrossWorldLinkShell1", "Cross-World Linkshell 1" },
            { "CrossWorldLinkShell2", "Cross-World Linkshell 2" },
            { "CrossWorldLinkShell3", "Cross-World Linkshell 3" },
            { "CrossWorldLinkShell4", "Cross-World Linkshell 4" },
            { "CrossWorldLinkShell5", "Cross-World Linkshell 5" },
            { "CrossWorldLinkShell6", "Cross-World Linkshell 6" },
            { "CrossWorldLinkShell7", "Cross-World Linkshell 7" },
            { "CrossWorldLinkShell8", "Cross-World Linkshell 8" },

            // Messages
            { "CoordinatesImported", "{0} Koordinaten importiert." },
            { "CoordinatesImportedFromClipboard", "{0} Koordinaten aus der Zwischenablage importiert." },
            { "NoCoordinatesImported", "Keine Koordinaten importiert." },
            { "ClipboardEmpty", "Zwischenablage ist leer." },
            { "CoordinatesExportedToClipboard", "Koordinaten in die Zwischenablage exportiert." },
            { "Export", "Exportieren" },
            { "Import", "Importieren" },
            { "CoordinateDetected", "Koordinate von {0} erkannt: {1}" },
            { "RouteOptimized", "Route mit {0} Punkten optimiert." },
            { "Error", "Fehler" },
            { "Success", "Erfolg" },
            
            // Teleport related
            { "TeleportButton", "Teleportieren" },
            { "TeleportingTo", "Teleportiere zu {0} (ID: {1})..." },
            { "TeleportTo", "Teleportieren zu: {0}" },
            { "TeleportCost", "Kosten: {0} Gil" },
            { "TeleportCostUnknown", "Kosten: Unbekannt" }
        };
        Translations["de"] = de;

        // French
        var fr = new Dictionary<string, string>
        {
            // Main window
            { "MainWindowTitle", "One Piece" },
            { "MainWindowSubtitle", "Planifiez et optimisez votre route de chasse au trésor" },
            { "Edit", "Modifier" },
            { "Settings", "Paramètres" },
            { "HideSettings", "Masquer les paramètres" },
            { "ClearAll", "Tout effacer" },
            { "OptimizeRoute", "Optimiser la route" },
            { "ResetOptimization", "Réinitialiser l'optimisation" },
            { "NoCoordinates", "Aucune coordonnée importée pour le moment." },
            { "UseImportSection", "Utilisez la section d'importation ci-dessus pour ajouter des coordonnées." },
            { "Coordinates", "Coordonnées: {0}" },
            { "CoordinatesWithCount", "Coordonnées ({0})" },
            { "TotalRouteDistance", "Distance totale du trajet: {0:F1}" },
            { "OptimizedRoute", "Route optimisée:" },
            { "OptimizedRouteWithCount", "Route optimisée ({0}):" },
            { "Collected", "Collecté" },
            { "Restore", "Restaurer" },
            { "SendToChat", "Envoyer au chat" },
            { "TrashBin", "Corbeille" },
            { "TrashBinWithCount", "Corbeille ({0})" },
            { "ClearTrash", "Vider la corbeille" },
            { "EmptyTrashBin", "La corbeille est vide." },
            { "ChannelSettings", "Paramètres du canal" },
            { "EnableChatMonitoring", "Activer la surveillance du canal" },
            { "SelectChatChannel", "Sélectionner le canal:" },
            { "StartMonitoring", "Démarrer" },
            { "StopMonitoring", "Arrêter" },
            { "MonitoringActive", "Actif: {0}" },
            { "CurrentMonitoredChannel", "Canal actuel: {0}" },
            { "PlayerCoordinates", "Joueur: {0}, Coordonnées: {1}" },
            { "UnknownArea", "Zone inconnue" },
            { "MapArea", "Zone de carte: {0}" },
            
            // Login status messages
            { "NotLoggedIn", "Veuillez vous connecter au jeu pour utiliser ce plugin." },

            // General settings section
            { "GeneralSettings", "Paramètres généraux" },

            { "Language", "Langue" },
            { "LogSettings", "Paramètres du journal" },
            { "LogLevel", "Niveau de journalisation" },
            { "LogLevelMinimal", "Désactivé" },
            { "LogLevelNormal", "Info" },
            { "LogLevelVerbose", "Débogage" },
            { "LogLevelMinimalTooltip", "Désactiver toute journalisation" },
            { "LogLevelNormalTooltip", "Afficher les messages de journal normaux" },
            { "LogLevelVerboseTooltip", "Afficher les informations de débogage détaillées" },

            // Custom message settings
            { "CustomMessageSettings", "Paramètres de message personnalisé" },
            { "MessageSettings", "Paramètres de message" },
            { "OpenSettingsWindow", "Ouvrir la fenêtre des paramètres" },
            { "MessageTemplateManagement", "Gestion des modèles de messages" },
            { "SavedTemplates", "Modèles enregistrés:" },
            { "NoSavedTemplates", "Pas encore de modèles enregistrés" },
            { "SetAsActiveTemplate", "Définir comme modèle actif" },
            { "ClearActiveTemplate", "Supprimer le modèle actif" },
            { "DeleteTemplate", "Supprimer le modèle" },
            { "CreateNewTemplate", "Créer un nouveau modèle" },
            { "TemplateName", "Nom du modèle:" },
            { "Create", "Créer" },
            { "CustomMessages", "Messages personnalisés" },
            { "AddNewMessage", "Ajouter un nouveau message:" },
            { "Add", "Ajouter" },
            { "EditMessage", "Modifier le message:" },
            { "Save", "Enregistrer" },
            { "Cancel", "Annuler" },
            { "EditingTemplate", "Modification du modèle: {0}" },
            { "CurrentActiveTemplate", "Modèle actif actuel: {0}" },
            { "EditCurrentMessageComponents", "Modifier les composants du message actuel (Pas de modèle actif)" },
            { "CurrentMessageComponentList", "Liste des composants du message actuel:" },
            { "NoComponents", "Pas encore de composants, veuillez ajouter ci-dessous" },
            { "AddComponents", "Ajouter des composants:" },
            // Custom message UI components
            { "AddPlayerName", "Nom du joueur" },
            { "AddCoordinates", "Coordonnées" },
            { "AddNumber", "Numéro (1-8)" },
            { "AddBoxedNumber", "Numéro encadré (1-8)" },
            { "AddBoxedOutlinedNumber", "Numéro encadré avec contour (1-8)" },
            { "AddCustomMessage", "Message personnalisé:" },
            { "MessagePreview", "Aperçu du message:" },
            { "SaveTemplateChanges", "Enregistrer les modifications du modèle" },
            { "NoActiveMessageTemplate", "Aucun modèle de message actif" },
            { "PlayerName", "Nom du joueur" },
            { "TreasureMapCoordinates", "Coordonnées de la carte au trésor" },
            { "Number", "Numéro (1-8)" },
            { "BoxedNumber", "Numéro encadré (1-8)" },
            { "BoxedOutlinedNumber", "Numéro encadré avec contour (1-8)" },
            { "CustomMessagePrefix", "Personnalisé: {0}" },
            { "InvalidCustomMessage", "Message personnalisé invalide" },
            { "UnknownComponent", "Composant inconnu" },
            { "WillOnlySendFlag", "Enverra uniquement le drapeau de carte au trésor" },
            { "OnlyTreasureMarker", "N'enverra que le marqueur de carte au trésor" },
            { "PlayerNamePreview", "[Nom du joueur]" },
            { "TreasureMapCoordinatesPreview", "[Coordonnées de la carte au trésor]" },
            { "NumberPreview", "[Numéro basé sur l'index de la carte]" },
            { "BoxedNumberPreview", "[Numéro encadré basé sur l'index de la carte]" },
            { "BoxedOutlinedNumberPreview", "[Numéro encadré avec contour basé sur l'index de la carte]" },
            { "MoveUp", "Déplacer vers le haut" },
            { "MoveDown", "Déplacer vers le bas" },

            // Chat channels
            { "Say", "Dire" },
            { "Yell", "Crier" },
            { "Shout", "Hurler" },
            { "Party", "Équipe" },
            { "Alliance", "Alliance" },
            { "FreeCompany", "Compagnie libre" },
            { "LinkShell1", "Linkshell 1" },
            { "LinkShell2", "Linkshell 2" },
            { "LinkShell3", "Linkshell 3" },
            { "LinkShell4", "Linkshell 4" },
            { "LinkShell5", "Linkshell 5" },
            { "LinkShell6", "Linkshell 6" },
            { "LinkShell7", "Linkshell 7" },
            { "LinkShell8", "Linkshell 8" },
            { "CrossWorldLinkShell1", "Linkshell inter-mondes 1" },
            { "CrossWorldLinkShell2", "Linkshell inter-mondes 2" },
            { "CrossWorldLinkShell3", "Linkshell inter-mondes 3" },
            { "CrossWorldLinkShell4", "Linkshell inter-mondes 4" },
            { "CrossWorldLinkShell5", "Linkshell inter-mondes 5" },
            { "CrossWorldLinkShell6", "Linkshell inter-mondes 6" },
            { "CrossWorldLinkShell7", "Linkshell inter-mondes 7" },
            { "CrossWorldLinkShell8", "Linkshell inter-mondes 8" },

            // Messages
            { "CoordinatesImported", "{0} coordonnées importées." },
            { "CoordinatesImportedFromClipboard", "{0} coordonnées importées du presse-papiers." },
            { "NoCoordinatesImported", "Aucune coordonnée n'a été importée." },
            { "ClipboardEmpty", "Le presse-papiers est vide." },
            { "CoordinatesExportedToClipboard", "Coordonnées exportées vers le presse-papiers." },
            { "Export", "Exporter" },
            { "Import", "Importer" },
            { "CoordinateDetected", "Coordonnée détectée de {0}: {1}" },
            { "RouteOptimized", "Route optimisée avec {0} points." },
            { "Error", "Erreur" },
            { "Success", "Succès" }
        };
        Translations["fr"] = fr;
    }
}






