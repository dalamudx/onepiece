<div align="center">

<a href="README.zh.md">中文</a> | <b>English</b>

<img src="https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/OnePiece/img/logo.png" alt="One Piece Logo" width="128" height="128">

# One Piece FFXIV Plugin

[![GitHub release](https://img.shields.io/github/v/release/dalamudx/onepiece?include_prereleases&style=flat)](https://github.com/dalamudx/onepiece/releases)
[![GitHub issues](https://img.shields.io/github/issues/dalamudx/onepiece)](https://github.com/dalamudx/onepiece/issues)

**Optimize your FFXIV treasure hunting journey with intelligent route planning**

</div>

---

## 📋 Introduction

One Piece is a Dalamud plugin designed for Final Fantasy XIV (FFXIV), focused on helping players plan and optimize treasure hunting routes. Whether you're an experienced treasure hunter or just getting started, this tool will make your treasure hunting more efficient.

---

## ✨ Key Features

### 📍 Coordinate Management
* **Multi-source Import**: Support importing treasure map coordinates from clipboard, game chat channels, and of course you can also use exported Base64 encoded data to re-import treasure map coordinates
> [!WARNING]
> Currently supports directly copying coordinate information from chat channels for import operations. You can copy multiple lines, but please pay attention to whether the imported results are consistent. If they don't match expectations, please copy the content and report the bug. When reporting, please blur or replace player names to avoid leaking player information
> Cross-client language coordinate import is not currently supported, as there seems to be no use case for it
* **Coordinate Editing**: Edit coordinate information directly in the interface, including player names and position data (excluding map names, as it seems unnecessary)
* **Status Tracking**: Track collected and uncollected treasure maps, support one-click marking
* **Recycle Bin Function**: Safely delete coordinates to recycle bin, support one-click recovery of mistakenly deleted coordinates
* **Batch Operations**: Support batch clearing, exporting, importing and managing coordinate data

### 🛣️ Route Optimization
* **Time Optimization Algorithm**: Calculate optimal routes based on actual travel time rather than simple distance calculations
* **Smart Teleport Decisions**: Automatically determine when using teleport is more efficient, considering teleport costs and time costs
* **Cross-map Routes**: Support complex route planning across multiple map areas
* **Dynamic Re-planning**: Automatically re-optimize remaining routes after collecting coordinates
* **Teleport Point Optimization**: Automatically assign optimal teleport points for each coordinate

### 💬 Channel Monitoring
* **Real-time Coordinate Detection**: Automatically detect and import coordinates from selected chat channels in real-time
* **Multi-channel Support**: Support Say, Yell, Shout, Party, Alliance, Free Company, Linkshell 1-8, Cross-world Linkshell 1-8

### 📝 Custom Messages
* **Template Management**: Create, edit, delete and manage multiple reusable message templates
* **Active Templates**: Set active templates to quickly apply commonly used message formats
* **Component-based Design**: Flexibly combine player names, coordinates, number markers, custom text and other components
* **Special Character Support**: Full support for in-game special characters, including numbers, boxed numbers, outlined boxed numbers
> [!WARNING]
> The given number, boxed number, and outlined boxed number template parts only support 1-8, and a full party has at most 8 people, i.e., 8 treasure map coordinates, so no optimization is done here for now. Of course, you can also plan more than 8 coordinates, in which case you need to remove these three types of number characters from the message template to avoid unexpected message output
* **Real-time Preview**: Preview how messages will actually appear in chat before sending
* **Custom Message Library**: Create and manage personal custom message libraries

### 🌐 Comprehensive Multi-language Support
* **5 Languages**: Full support for English, Japanese, Chinese, German and French
* **Client Adaptation**: Automatically adapt map area names for English, Japanese, Chinese, German and French clients (Chinese client not currently supported due to lack of client and account for testing)
* **Dynamic Translation**: Real-time translation of map area names to ensure accuracy of route optimization

---

## 🚀 Usage

### 📥 Installation

1. Ensure you have [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) installed
2. Enable Dalamud plugins
3. Type `/xlsettings` in game chat to open Dalamud settings
4. Go to the "Experimental" tab
5. Find the "Custom Plugin Repositories" section, agree to the listed terms if needed, and paste the following link:
   ```
   https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/repo.json
   ```
6. Click the "Save" button
7. Search for "One Piece" in the plugin installer and install it

### 🔰 Basic Usage

1. Use the `/onepiece` command to open the main interface
2. In "General Settings", select language and chat channels to monitor
3. Import coordinates via clipboard or enable chat channel monitoring for automatic detection and import
4. Use the "Optimize Route" button to calculate the best route
5. Visit coordinate points in optimized order, use "Collected" button to mark completed points
6. Use "Clear All" to clear existing coordinate information and start new treasure hunting, or use "Reset Optimization" to re-edit coordinate information and re-plan routes

---

## 📝 Development Roadmap

### ✅ Completed Major Features
* **Time Optimization Algorithm**: Advanced route optimization based on actual travel time
* **Multi-language Client Support**: Full support for map area recognition in English, Japanese, German, French clients
* **Advanced Message System**: Template management, component-based design and real-time preview
* **Smart Coordinate Management**: Complete functionality including editing, recycle bin, batch operations

### 🚧 Planned Features
* **Optimize Coordinate Import**: Optimize regular expressions to better support coordinate copying and importing from chat channels

---

## 🤝 Contribution and Support

If you find bugs or have suggestions for improvements, please submit issues or pull requests at the [GitHub repository](https://github.com/dalamudx/onepiece).
