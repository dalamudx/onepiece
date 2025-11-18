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
-

### Removed
-

### Technical
-

## [1.0.1.9] - 2025-11-19

### Changed
- Merge submodule

### Fixed
- 

### Technical
- 

## [1.0.1.8] - 2025-10-11

### Changed
- Merge submodule

### Fixed
- 

### Technical
- 

## [1.0.1.7] - 2025-09-13

### Changed
- Merge Ecommons

### Fixed
- Updated api level to 13

### Technical
- 

## [1.0.1.6] - 2025-08-09

### Changed
- Migrated UI bindings
- Bump to API 13

### Fixed
- Updated ImGui.CalcTextSize calls

### Technical
- Refreshed lock file to reflect new toolchain versions

## [1.0.1.5] - 2025-06-18

### Added
- Warning messages when the selected number icon type cannot represent the current coordinate count

### Changed
- Optimized Number, BoxedNumber, and BoxedOutlinedNumber display methods with extended ranges via GameIconHelper

### Removed
- Removed all artificial coordinate quantity limits, including the previous 8-coordinate import restriction

### Technical
- Updated localization strings to reflect icon ranges; ChatMonitorService shows previews and range warnings
- CoordinateImportExportService now processes large coordinate sets without truncation

## [1.0.1.4] - 2025-06-18

### Changed
- Route optimization now uses actual teleport costs fetched via Telepo API through AetheryteService, regardless of player location availability

### Fixed
- Removed the fallback of assuming high costs for unknown areas; costs now come from Telepo API for more accurate planning

### Technical
- Updated RouteOptimizationService to always query AetheryteService/Telepo for costs

## [1.0.1.3] - 2025-06-12

### Added
- Download statistics badges in README files for both English and Chinese versions
- Plugin screenshots section with emoji indicators in README files
- Automated changelog generation from commit history in release workflow

### Changed
- Optimized coordinate list line spacing to prevent buttons from appearing too close together
- Improved release workflow to automatically generate changelog from Git commits
- Updated README files with synchronized content between English and Chinese versions

### Fixed
- Fixed coordinate list line spacing issues in imported, optimized, and trash bin lists
- Improved visual spacing between coordinate entries for better user experience
- Corrected CHANGELOG.md version dates and chronological ordering

### Removed
- Manual changelog input requirement from release workflow

### Technical
- Enhanced MainWindow.cs with 4-pixel line spacing for coordinate entries
- Implemented automatic commit-based changelog generation with CI commit filtering
- Added GitHub download count tracking badges to project documentation
- Refactored release workflow to exclude CI-generated commits from changelog
- Improved workflow automation with intelligent changelog generation

## [1.0.1.2] - 2025-06-12

### Added
- Column-aligned layout for coordinate lists with proper text alignment
- Component usage restrictions for player name and coordinate elements in message templates
- Client language adaptation for message preview LocationExample text
- Smart trash bin visibility that hides after route optimization

### Fixed
- Improved coordinate display layout with column alignment for better visual organization
- Removed colons from player names in coordinate lists for cleaner appearance
- Hidden trash bin functionality after route optimization to reduce interface clutter

### Removed
- Colons after player names in coordinate display lists

### Technical
- Refactored coordinate display logic with unified text building methods
- Enhanced UIHelper with column-aligned rendering capabilities
- Improved message template component validation and restriction logic
- Optimized player name column width calculation for better alignment
- Added client language detection for localized message previews

## [1.0.1.1] - 2025-06-11

### Fixed
- Fixed cross-map routing to prioritize teleport cost over distance for better route optimization
- Improved pathfinding algorithm efficiency
- Enhanced route cost calculation accuracy

### Technical
- Optimized route planning algorithms
- Improved teleport cost vs distance balancing

## [1.0.1.0] - 2025-06-11

### Added
- Support for different player name settings and configurations
- Standardized logging system across all services
- Enhanced PlayerNameProcessingService with comprehensive name handling
- Improved coordinate import/export functionality with better validation

### Changed
- Refactored coordinate display helpers for better performance
- Enhanced map area translation service
- Improved player location service accuracy
- Optimized route optimization algorithms

### Fixed
- Updated ECommons dependency and removed unnecessary log outputs
- Fixed various UI layout and translation issues
- Improved error handling in coordinate processing

### Technical
- Major code refactoring with 798 additions and 625 deletions
- Enhanced service architecture for better maintainability
- Improved logging and debugging capabilities

## [1.0.0.1] - 2025-06-11

### Fixed
- Fixed workflow permissions for automated releases
- Fixed workflow configuration issues
- Updated ECommons dependency
- Fixed collect feature functionality
- Fixed translations and UI layout issues

### Technical
- Reworked GitHub Actions workflow
- Added automated release workflow
- Improved CI/CD pipeline configuration

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

- **1.0.1.9** (2025-11-19): Merge submodule
- **1.0.1.8** (2025-10-11): Merge submodule
- **1.0.1.7** (2025-09-13): Merged ECommons; upgraded to API13
- **1.0.1.6** (2025-08-09): ImGui bindings migration to Dalamud.Bindings.ImGui; upgrade to API13
- **1.0.1.5** (2025-06-18): Removed coordinate limits; optimized number icon display; range warnings and localization updates
- **1.0.1.4** (2025-06-18): Use actual teleport costs from Telepo API for route optimization
- **1.0.1.3** (2025-06-12): Line spacing optimization, automated changelog generation, and documentation improvements
- **1.0.1.2** (2025-06-12): UI improvements with column alignment, component restrictions, and enhanced message preview
- **1.0.1.1** (2025-06-11): Cross-map routing optimization and teleport cost prioritization
- **1.0.1.0** (2025-06-11): Player name processing enhancements and standardized logging
- **1.0.0.1** (2025-06-11): Workflow fixes and CI/CD improvements
- **1.0.0.0** (2025-06-11): Initial release with core functionality
