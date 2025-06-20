<div align="center">

<b>中文</b> | <a href="README.md">English</a>

<img src="https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/OnePiece/img/logo.png" alt="One Piece Logo" width="128" height="128">

# One Piece FFXIV 插件

[![GitHub release](https://img.shields.io/github/v/release/dalamudx/onepiece?include_prereleases&style=flat)](https://github.com/dalamudx/onepiece/releases)
[![GitHub issues](https://img.shields.io/github/issues/dalamudx/onepiece)](https://github.com/dalamudx/onepiece/issues)
[![GitHub all releases](https://img.shields.io/github/downloads/dalamudx/onepiece/total?style=flat&label=总下载量)](https://github.com/dalamudx/onepiece/releases)
[![GitHub latest release](https://img.shields.io/github/downloads/dalamudx/onepiece/latest/total?style=flat&label=最新版本下载量)](https://github.com/dalamudx/onepiece/releases/latest)

**通过智能路线规划优化您的FFXIV寻宝之旅**

</div>

---

## 📋 简介

One Piece是为《最终幻想XIV》(FFXIV)设计的Dalamud插件，专注于帮助玩家规划和优化寻宝路线。无论您是经验丰富的寻宝者还是刚刚入门，这个工具都将使您的寻宝更加高效。

---

## ✨ 主要功能

### 📍 坐标管理
* **多源导入**：支持从剪贴板、游戏聊天频道导入藏宝图坐标，当然也可以使用导出的Base64编码数据重新导入藏宝图坐标
> [!WARNING]
> 目前支持直接复制聊天频道中的坐标信息进行导入操作，可以复制多行，但需要注意导入后的结果是否一致，如果与预期不符请将复制的内容一并复制并回报bug，当然回报时请对玩家名称进行涂抹或替换，不要泄露玩家信息
>暂不支持跨客户端语言坐标导入，感觉没有使用场景
* **坐标编辑**：直接在界面中编辑坐标信息，包括玩家名称和位置数据(不包含地图名称，感觉没必要)
* **状态跟踪**：跟踪已收集和未收集的藏宝图，支持一键标记
* **回收站功能**：安全删除坐标到回收站，支持一键恢复误删的坐标
* **批量操作**：支持批量清除、导出、导入和管理坐标数据

### 🛣️ 路线优化
* **时间优化算法**：基于实际旅行时间而非简单距离计算最优路线
* **智能传送决策**：自动判断何时使用传送更高效，考虑传送费用和时间成本
* **跨地图路线**：支持跨多个地图区域的复杂路线规划
* **动态重新规划**：收集坐标后自动重新优化剩余路线
* **传送点优化**：为每个坐标自动分配最优的传送点

### 💬 频道监控
* **实时坐标检测**：自动从选定聊天频道实时检测和导入坐标
* **多频道支持**：支持说话、呼喊、喊话、小队、团队、部队、通讯贝1-8、跨界通讯贝1-8

### 📝 自定义消息
* **模板管理**：创建、编辑、删除和管理多个可重用的消息模板
* **活动模板**：设置活动模板，快速应用常用的消息格式
* **组件化设计**：灵活组合玩家名称、坐标、数字标记、自定义文本等组件
* **特殊字符支持**：完整支持游戏内特殊字符，包括数字、方框数字、轮廓方框数字
> [!WARNING]
> 给定的数字、方框数字、轮廓方框数字模板部分只支持1-9，而满编队最多也只有8人，即8张藏宝图坐标，目前去除了坐标数量限制，且对数字特殊字符做了优化，如果你导入了大于该显示范围的坐标数量，则会自动忽略该数字组件，忽略后的消息，仍然可以通过查看消息预览确认
* **实时预览**：发送前预览消息在聊天中的实际显示效果
* **自定义消息库**：创建和管理个人自定义消息库

### 🌐 全面多语言支持
* **5种语言**：完整支持英语、日语、中文、德语和法语
* **客户端适配**：自动适配英语、日语、德语和法语(国际服客户端)的地图区域名称(暂不支持中文客户端，因为没有客户端和账号做测试)
* **动态翻译**：实时翻译地图区域名称，确保路线优化的准确性

---

## 🚀 使用方法

### 📥 安装

1. 确保您已安装[FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher)
2. 启用Dalamud插件
3. 在游戏聊天中输入`/xlsettings`打开Dalamud设置
4. 转到"实验性"选项卡
5. 找到"自定义插件仓库"部分，如有需要同意列出的条款，并粘贴以下链接：
   ```
   https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/repo.json
   ```
6. 点击"保存"按钮
7. 在插件安装器中搜索"One Piece"并安装

### 🔰 基本用法

1. 使用`/onepiece`命令打开主界面
2. 在"常规设置"中选择语言和监控的聊天频道
3. 通过剪贴板导入坐标或启用聊天频道监控自动检测并导入
4. 使用"优化路线"按钮计算最佳路线
5. 按照优化顺序访问坐标点，使用"已收集"按钮标记完成的点
6. 使用"全部清除"清除现有坐标信息，开始新的寻宝，或使用"重置优化"重新编辑坐标信息后重新进行路线规划

### 📸 插件截图
[主界面]

<img src="https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/OnePiece/img/example-main.png" alt="主界面" width="640">

[游戏频道消息]

<img src="https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/OnePiece/img/example-channel.png" alt="游戏频道消息" width="640">

[自定义消息]

<img src="https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/OnePiece/img/example-message.png" alt="自定义消息" width="640">

---

## 📝 开发路线图

### ✅ 已完成的主要功能
* **时间优化算法**：基于实际旅行时间的高级路线优化
* **多语言客户端支持**：完整支持英语、日语、德语、法语客户端的地图区域识别
* **高级消息系统**：模板管理、组件化设计和实时预览
* **智能坐标管理**：编辑、回收站、批量操作等完整功能

### 🚧 计划中的功能
* **暂无**：如果您有任何功能需求，请开issue告诉我

---

## 🤝 贡献和支持

如果您发现错误或有改进建议，请在[GitHub仓库](https://github.com/dalamudx/onepiece)提交问题或拉取请求。