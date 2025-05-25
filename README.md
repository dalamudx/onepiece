<div align="center">

<a href="README.zh.md">中文</a> | <b>English</b>

<img src="https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/OnePiece/img/logo.png" alt="One Piece Logo" width="128" height="128">

# One Piece FFXIV Plugin

[![GitHub release](https://img.shields.io/github/v/release/dalamudx/onepiece?include_prereleases&style=flat)](https://github.com/dalamudx/onepiece/releases)
[![GitHub issues](https://img.shields.io/github/issues/dalamudx/onepiece)](https://github.com/dalamudx/onepiece/issues)

**Optimize your FFXIV treasure hunts with intelligent route planning**

</div>

---

## 📋 Introduction

One Piece is a Dalamud plugin designed for Final Fantasy XIV (FFXIV), focused on helping players plan and optimize treasure hunting routes. Whether you're an experienced treasure hunter or just getting started, this tool will make your treasure searches more efficient.

---

## ✨ Main Features

### 📍 Treasure Coordinate Management
* **Import Coordinates**: Import treasure coordinates from clipboard or game chat channels
* **Export Coordinates**: Export coordinates to clipboard for easy sharing with teammates
* **Coordinate Collection**: Track collected and uncollected treasure points
* **Trash Bin Feature**: Temporarily store deleted coordinates for recovery when needed

### 🛣️ Route Optimization
* **Automatic Route Optimization**: Calculate the shortest or most effective route through all treasure points
* **Consider Teleport Costs**: Take into account Aetheryte teleport fees and distances when calculating routes
* **Map Area Recognition**: Automatically identify the map area where coordinates are located

### 💬 Channel Settings
* **Automatic Coordinate Detection**: Automatically detect and import coordinates from selected chat channels
* **Support for Multiple Chat Channels**: Including Say, Yell, Shout, Party, Alliance, Free Company, Linkshells, and more

### 📝 Custom Message System
* **Message Templates**: Create and manage reusable message templates for coordinate sharing
* **Special Character Support**: Utilize game special characters including numbers, boxed numbers, and boxed outlined numbers
* **Component-Based Design**: Mix and match different message components for flexible message creation

### 🖥️ User-Friendly Interface
* **Clear Coordinate List**: Display all imported coordinates and their status
* **Optimized Route Display**: Intuitively show the optimized route sequence
* **Login Status Detection**: Automatically disables functionality when not logged into the game

### 🌐 Multilingual Support
* **Supported Languages**: English, Japanese, Chinese, German, and French

---

## 🚀 How to Use

### 📥 Installation

1. Make sure you have installed [FFXIVQuickLauncher](https://github.com/goatcorp/FFXIVQuickLauncher)
2. Enable Dalamud plugins
3. Open Dalamud settings by typing `/xlsettings` in game chat
4. Go to "Experimental" tab
5. Find "Custom Plugin Repositories" section, agree with listed terms if needed and paste the following link:
   ```
   https://raw.githubusercontent.com/dalamudx/onepiece/refs/heads/master/repo.json
   ```
6. Click "Save" button
7. Search for "One Piece" in the plugin installer and install it

### 🔰 Basic Usage

1. Use the `/onepiece` command to open the main interface
2. In the "Import" section, import coordinates from clipboard or enable chat channel monitoring
3. Use the "Optimize Route" button to calculate the best route
4. Visit coordinate points in the optimized order, marking collected points

### 📎 Importing Coordinates

The plugin supports two ways to import coordinates:
* **Clipboard Import**: Copy text containing coordinates, then click the "Import" button
* **Chat Monitoring**: Enable channel monitoring to automatically detect coordinates from the selected channel

### 💬 Message Customization

The plugin offers flexible options for customizing coordinate sharing messages:
* **Message Templates**: Create, edit, and manage reusable message templates
* **Component Selection**: Mix and match different message components (player name, coordinates, custom text)
* **Special Characters**: Include numerical indicators using game special characters (numbers, boxed numbers, outlined numbers)
* **Preview System**: See exactly how your message will appear in chat before sending

### ⚙️ Route Optimization

Click the "Optimize Route" button, and the plugin will calculate the best path based on the following factors:
* Current player position
* Aetheryte teleport fees
* Distance between coordinates and between coordinates and aetheryte teleport points

---

## 📝 TODO

The following features are planned for future releases:

* **Path Selection Algorithm Optimization**: Enhance the route optimization algorithm for more efficient treasure hunting
* **Optimized Coordinate List Display**: Improve the visual presentation and organization of coordinate lists
* **Support for Related Plugins**: Add integration capabilities with complementary FFXIV plugins
* **Dynamic Aetheryte Teleport Point Coordinates**: Automatically fetch and update aetheryte teleport point locations
* **Non-English Client Adaptation**: Improve compatibility with non-English language clients for more comprehensive localization support

---

## 🤝 Contributing and Support

If you find bugs or have suggestions for improvements, please submit an issue or pull request on the [GitHub repository](https://github.com/dalamudx/onepiece).