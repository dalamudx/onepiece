# Changelog

All notable changes to the OnePiece plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- 

### Changed
- 

### Fixed
- Fixed ImGui style color stack imbalance when clicking collected button

### Removed
- 

## [1.0.0.0] - 2025-06-11

### Added
- Initial release of OnePiece treasure hunting plugin
- Coordinate management with import/export functionality
- Route optimization based on travel time calculations
- Multi-language support (English, Japanese, Chinese, German, French)
- Chat channel monitoring for automatic coordinate detection
- Custom message templates with component-based design
- Teleport integration with cost calculation
- Coordinate editing with map area validation
- Trash bin functionality for deleted coordinates
- Adaptive UI layout for different languages

### Features
- **Coordinate Management**
  - Multi-source import from clipboard and chat channels
  - Base64 encoded data export/import
  - Real-time editing of coordinates and player names
  - Status tracking for collected/uncollected treasures
  - Safe deletion with restore functionality

- **Route Optimization**
  - Time-based optimization algorithm
  - Smart teleport decision making
  - Cross-map route planning
  - Dynamic re-planning after collection
  - Teleport point optimization

- **Chat Integration**
  - Real-time coordinate detection from multiple channels
  - Support for Say, Yell, Shout, Party, Alliance, Free Company
  - Linkshell 1-8 and Cross-world Linkshell 1-8 monitoring
  - Automatic coordinate parsing and validation

- **Message System**
  - Template management with create/edit/delete operations
  - Active template selection for quick application
  - Component-based message building
  - Special character support (numbers, boxed numbers, outlined numbers)
  - Real-time message preview
  - Custom message library management

- **Internationalization**
  - Full localization for 5 languages
  - Client-specific map area name adaptation
  - Dynamic translation for route optimization
  - Language-specific coordinate import patterns

- **User Interface**
  - Adaptive button widths for different languages
  - Consistent spacing and alignment
  - Responsive layout design
  - Intuitive coordinate list management
  - Integrated settings and configuration

## Release Notes Template

When creating a new release, use this template for the changelog:

```
### Added
- New features or functionality

### Changed
- Changes to existing functionality

### Fixed
- Bug fixes and corrections

### Removed
- Removed features or deprecated functionality

### Technical
- Internal improvements, refactoring, or technical changes
```

## Version History

- **1.0.0.0**: Initial release with core functionality
