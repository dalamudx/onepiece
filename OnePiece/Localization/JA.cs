namespace OnePiece.Localization;

/// <summary>
/// Japanese localization implementation.
/// </summary>
public class JA : ILocalizationData
{
    // Window and UI Elements
    public string MainWindowTitle => "One Piece";
    public string GeneralSettings => "一般設定";
    public string Language => "言語";
    public string MessageSettings => "メッセージ設定";
    public string CustomMessageSettings => "カスタムメッセージ設定";

    // Actions and Buttons
    public string Edit => "編集";
    public string Save => "保存";
    public string Cancel => "キャンセル";
    public string Delete => "削除";
    public string Restore => "復元";
    public string Create => "作成";
    public string Add => "追加";
    public string Export => "エクスポート";
    public string Import => "インポート";
    public string ClearAll => "すべてクリア";

    // Route Optimization
    public string OptimizeRoute => "ルート最適化";
    public string ResetOptimization => "最適化をリセット";
    public string NoCoordinatesToOptimize => "最適化する座標がありません。まず座標をインポートしてください。";

    // Coordinates and Collection
    public string NoCoordinates => "座標がインポートされていません。";
    public string Coordinates => "座標";
    public string Collected => "収集済み";
    public string NotCollected => "未収集";

    // Teleportation
    public string TeleportButton => "テレポ";

    // Chat and Communication
    public string SendToChat => "チャットに送信";
    public string SelectChatChannel => "チャンネルを選択";
    public string CurrentChannel => "現在のチャンネル";
    public string StartMonitoring => "監視開始";
    public string StopMonitoring => "監視停止";

    // Trash Management
    public string TrashBin => "ごみ箱";
    public string ClearTrash => "ごみ箱を空にする";
    public string EmptyTrashBin => "ごみ箱は空です。";

    // Status Messages
    public string UnknownArea => "不明なエリア";
    public string NotLoggedIn => "このプラグインを使用するには、ゲームにログインしてください。";
    public string ClipboardEmpty => "クリップボードが空です。";

    // Message Templates
    public string MessageTemplateManagement => "メッセージテンプレート管理";
    public string SavedTemplates => "保存されたテンプレート:";
    public string NoSavedTemplates => "保存されたテンプレートがありません";
    public string SetAsActiveTemplate => "アクティブテンプレートに設定";
    public string ClearActiveTemplate => "アクティブテンプレートをクリア";
    public string DeleteTemplate => "テンプレートを削除";
    public string TemplateName => "テンプレート名:";
    public string NoActiveMessageTemplate => "アクティブなメッセージテンプレートがありません";

    // Message Components
    public string CustomMessages => "カスタムメッセージ";
    public string AddNewMessage => "新しいメッセージを追加:";
    public string EditMessage => "メッセージを編集:";
    public string CurrentMessageComponentList => "現在のコンポーネントリスト:";
    public string NoComponents => "まだコンポーネントが追加されていません";
    public string AddComponents => "コンポーネントを追加:";
    public string AddCustomMessage => "カスタムメッセージ:";
    public string MessagePreview => "メッセージプレビュー:";
    public string SaveTemplateChanges => "テンプレートの変更を保存";

    // Component Types
    public string PlayerName => "プレイヤー名";
    public string Number => "数字 (1-8)";
    public string BoxedNumber => "囲み数字 (1-8)";
    public string BoxedOutlinedNumber => "囲み輪郭数字 (1-8)";
    public string MoveUp => "上に移動";
    public string MoveDown => "下に移動";

    // Examples and Previews
    public string PlayerNameExample => "Tataru Taru";
    public string LocationExample => "リムサ・ロミンサ：下甲板層 ( 9.5 , 11.2 )";

    // Chat Channels
    public string Say => "会話";
    public string Yell => "叫び";
    public string Shout => "呼びかけ";
    public string Party => "パーティ";
    public string Alliance => "アライアンス";
    public string FreeCompany => "フリーカンパニー";
    public string LinkShell1 => "リンクシェル 1";
    public string LinkShell2 => "リンクシェル 2";
    public string LinkShell3 => "リンクシェル 3";
    public string LinkShell4 => "リンクシェル 4";
    public string LinkShell5 => "リンクシェル 5";
    public string LinkShell6 => "リンクシェル 6";
    public string LinkShell7 => "リンクシェル 7";
    public string LinkShell8 => "リンクシェル 8";
    public string CrossWorldLinkShell1 => "クロスワールドリンクシェル 1";
    public string CrossWorldLinkShell2 => "クロスワールドリンクシェル 2";
    public string CrossWorldLinkShell3 => "クロスワールドリンクシェル 3";
    public string CrossWorldLinkShell4 => "クロスワールドリンクシェル 4";
    public string CrossWorldLinkShell5 => "クロスワールドリンクシェル 5";
    public string CrossWorldLinkShell6 => "クロスワールドリンクシェル 6";
    public string CrossWorldLinkShell7 => "クロスワールドリンクシェル 7";
    public string CrossWorldLinkShell8 => "クロスワールドリンクシェル 8";

    // Status and Error Messages
    public string NoCoordinatesImported => "座標がインポートされませんでした。座標には有効なマップエリアが含まれている必要があります。形式：'マップ名 (x, y)'。";
    public string CoordinatesExportedToClipboard => "座標がクリップボードにエクスポートされました。";
    public string InvalidCustomMessage => "無効なカスタムメッセージ";
    public string UnknownComponent => "不明なコンポーネント";
    public string CoordinateOnlyMessage => "座標のみ送信";
    public string EditCurrentMessageComponents => "現在のメッセージコンポーネントを編集（アクティブテンプレートなし）";
    public string ViewingActiveTemplateReadOnly => "アクティブテンプレートコンポーネントを表示中（読み取り専用）。編集するにはテンプレートを選択してください。";

    // Window-specific strings
    public string OpenCustomMessageWindow => "カスタムメッセージ";

    // Formatted Messages (with parameters)
    public string CoordinatesWithCount(int count) => $"座標 ({count})";
    public string OptimizedRouteWithCount(int count) => $"最適化されたルート ({count}):";
    public string TrashBinWithCount(int count) => $"ごみ箱 ({count})";
    public string CoordinatesImported(int count) => $"{count}座標がインポートされました。";
    public string CoordinatesImportedFromClipboard(int count) => $"クリップボードから{count}座標をインポートしました。";
    public string CoordinateDetected(string source, string coordinate) => $"{source}から座標を検出: {coordinate}";
    public string RouteOptimized(int count) => $"{count}ポイントでルートが最適化されました。";
    public string EditingTemplate(string templateName) => $"テンプレートを編集中: {templateName}";
    public string CurrentActiveTemplate(string templateName) => $"現在のアクティブテンプレート: {templateName}";
    public string CustomMessagePrefix(string message) => $"カスタム: {message}";
    public string TeleportToLocation(string location) => $"テレポ先: {location}";
    public string TeleportCostAmount(string cost) => $"費用: {cost} ギル";
}
