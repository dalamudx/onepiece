namespace OnePiece.Localization;

/// <summary>
/// Chinese localization implementation.
/// </summary>
public class ZH : ILocalizationData
{
    // Window and UI Elements
    public string MainWindowTitle => "One Piece";
    public string GeneralSettings => "常规设置";
    public string Language => "语言";
    public string MessageSettings => "消息设置";
    public string CustomMessageSettings => "自定义消息设置";

    // Actions and Buttons
    public string Edit => "编辑";
    public string Save => "保存";
    public string Cancel => "取消";
    public string Delete => "删除";
    public string Restore => "恢复";
    public string Create => "创建";
    public string Add => "添加";
    public string Export => "导出";
    public string Import => "导入";
    public string ClearAll => "全部清除";

    // Route Optimization
    public string OptimizeRoute => "优化路线";
    public string ResetOptimization => "重置优化";
    public string NoCoordinatesToOptimize => "没有可优化的坐标。请先导入坐标。";

    // Coordinates and Collection
    public string NoCoordinates => "尚未导入坐标。";
    public string Coordinates => "坐标";
    public string Collected => "已收集";
    public string NotCollected => "未收集";

    // Teleportation
    public string TeleportButton => "传送";

    // Chat and Communication
    public string SendToChat => "发送到聊天";
    public string SelectChatChannel => "选择频道";
    public string CurrentChannel => "当前频道";
    public string StartMonitoring => "开始监控";
    public string StopMonitoring => "停止监控";

    // Trash Management
    public string TrashBin => "回收站";
    public string ClearTrash => "清空回收站";
    public string EmptyTrashBin => "回收站为空。";

    // Status Messages
    public string UnknownArea => "未知区域";
    public string NotLoggedIn => "请登录游戏以使用此插件。";
    public string ClipboardEmpty => "剪贴板为空。";

    // Message Templates
    public string MessageTemplateManagement => "消息模板管理";
    public string SavedTemplates => "已保存的模板:";
    public string NoSavedTemplates => "没有保存的模板";
    public string SetAsActiveTemplate => "设为活动模板";
    public string ClearActiveTemplate => "清除活动模板";
    public string DeleteTemplate => "删除模板";
    public string TemplateName => "模板名称:";
    public string NoActiveMessageTemplate => "没有活动消息模板";

    // Message Components
    public string CustomMessages => "自定义消息";
    public string AddNewMessage => "添加新消息:";
    public string EditMessage => "编辑消息:";
    public string CurrentMessageComponentList => "当前组件列表:";
    public string NoComponents => "尚未添加组件";
    public string AddComponents => "添加组件:";
    public string AddCustomMessage => "自定义消息:";
    public string MessagePreview => "消息预览:";
    public string SaveTemplateChanges => "保存模板更改";

    // Component Types
    public string PlayerName => "玩家名称";
    public string Number => "数字 (1-9)";
    public string BoxedNumber => "方框数字 (1-31)";
    public string BoxedOutlinedNumber => "轮廓方框数字 (1-9)";
    public string MoveUp => "上移";
    public string MoveDown => "下移";

    // Examples and Previews
    public string PlayerNameExample => "Tataru Taru";
    public string LocationExample => "利姆萨·罗敏萨下层甲板 ( 9.5 , 11.2 )";

    // Chat Channels
    public string Say => "说话";
    public string Yell => "呼喊";
    public string Shout => "喊话";
    public string Party => "小队";
    public string Alliance => "团队";
    public string FreeCompany => "部队";
    public string LinkShell1 => "通讯贝 1";
    public string LinkShell2 => "通讯贝 2";
    public string LinkShell3 => "通讯贝 3";
    public string LinkShell4 => "通讯贝 4";
    public string LinkShell5 => "通讯贝 5";
    public string LinkShell6 => "通讯贝 6";
    public string LinkShell7 => "通讯贝 7";
    public string LinkShell8 => "通讯贝 8";
    public string CrossWorldLinkShell1 => "跨界通讯贝 1";
    public string CrossWorldLinkShell2 => "跨界通讯贝 2";
    public string CrossWorldLinkShell3 => "跨界通讯贝 3";
    public string CrossWorldLinkShell4 => "跨界通讯贝 4";
    public string CrossWorldLinkShell5 => "跨界通讯贝 5";
    public string CrossWorldLinkShell6 => "跨界通讯贝 6";
    public string CrossWorldLinkShell7 => "跨界通讯贝 7";
    public string CrossWorldLinkShell8 => "跨界通讯贝 8";

    // Status and Error Messages
    public string NoCoordinatesImported => "未导入坐标。坐标必须包含有效的地图区域，格式为：'地图名称 (x, y)'。";
    public string CoordinatesExportedToClipboard => "坐标已导出到剪贴板。";
    public string InvalidCustomMessage => "无效的自定义消息";
    public string UnknownComponent => "未知组件";
    public string CoordinateOnlyMessage => "仅发送坐标";
    public string EditCurrentMessageComponents => "编辑当前消息组件（无活动模板）";
    public string ViewingActiveTemplateReadOnly => "查看活动模板组件（只读）。选择模板以编辑它。";

    // Window-specific strings
    public string OpenCustomMessageWindow => "自定义消息";

    // Formatted Messages (with parameters)
    public string CoordinatesWithCount(int count) => $"坐标 ({count})";
    public string OptimizedRouteWithCount(int count) => $"优化路线 ({count}):";
    public string TrashBinWithCount(int count) => $"回收站 ({count})";
    public string CoordinatesImported(int count) => $"已导入 {count} 个坐标。";
    public string CoordinatesImportedFromClipboard(int count) => $"从剪贴板导入了 {count} 个坐标。";
    public string CoordinateDetected(string source, string coordinate) => $"从 {source} 检测到坐标: {coordinate}";
    public string RouteOptimized(int count) => $"路线已优化，包含 {count} 个点。";
    public string EditingTemplate(string templateName) => $"编辑模板: {templateName}";
    public string CurrentActiveTemplate(string templateName) => $"当前活动模板: {templateName}";
    public string CustomMessagePrefix(string message) => $"自定义: {message}";
    public string TeleportToLocation(string location) => $"传送到: {location}";
    public string TeleportCostAmount(string cost) => $"费用: {cost} 金币";
    public string PlayerNameAlreadyAdded => "玩家名称只能添加一次";
    public string CoordinatesAlreadyAdded => "坐标只能添加一次";

    // Component range warnings
    public string ComponentRangeWarning(string components) => $"提示：{components}组件超出显示范围将不会生效";
}
