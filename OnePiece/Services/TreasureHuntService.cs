using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for handling treasure hunt coordinates and route optimization.
/// Acts as a coordinator between various specialized services.
/// </summary>
public class TreasureHuntService : IDisposable
{
    private readonly Plugin plugin;

    /// <summary>
    /// Service for importing and exporting coordinates.
    /// </summary>
    private readonly CoordinateImportExportService importExportService;

    /// <summary>
    /// Service for optimizing routes.
    /// </summary>
    private readonly RouteOptimizationService routeOptimizer;

    /// <summary>
    /// Gets the list of treasure coordinates.
    /// </summary>
    public List<TreasureCoordinate> Coordinates { get; private set; } = new();

    /// <summary>
    /// Gets the list of deleted treasure coordinates (trash bin).
    /// </summary>
    public List<TreasureCoordinate> DeletedCoordinates { get; private set; } = new();



    /// <summary>
    /// Gets the optimized route through the coordinates.
    /// </summary>
    public List<TreasureCoordinate> OptimizedRoute => routeOptimizer.OptimizedRoute;

    /// <summary>
    /// Gets whether the route is currently optimized.
    /// </summary>
    public bool IsRouteOptimized => routeOptimizer.IsRouteOptimized;

    /// <summary>
    /// Event raised when coordinates are imported.
    /// </summary>
    public event EventHandler<int>? OnCoordinatesImported;

    /// <summary>
    /// Event raised when coordinates are exported.
    /// </summary>
    public event EventHandler<string>? OnCoordinatesExported;

    /// <summary>
    /// Event forwarded from CoordinateManagementService.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateDeleted;

    /// <summary>
    /// Event forwarded from CoordinateManagementService.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateRestored;

    /// <summary>
    /// Event forwarded from CoordinateManagementService.
    /// </summary>
    public event EventHandler? OnCoordinatesCleared;

    /// <summary>
    /// Event forwarded from RouteOptimizationService.
    /// </summary>
    public event EventHandler<int>? OnRouteOptimized;

    /// <summary>
    /// Event forwarded from RouteOptimizationService.
    /// </summary>
    public event EventHandler? OnRouteOptimizationReset;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreasureHuntService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    public TreasureHuntService(Plugin plugin)
    {
        this.plugin = plugin;
        this.importExportService = new CoordinateImportExportService(plugin, plugin.AetheryteService, plugin.MapAreaTranslationService);
        this.routeOptimizer = new RouteOptimizationService(plugin);

        // Wire up event handlers
        routeOptimizer.OnRouteOptimized += (sender, count) => OnRouteOptimized?.Invoke(this, count);
        routeOptimizer.OnRouteOptimizationReset += (sender, args) => OnRouteOptimizationReset?.Invoke(this, args);
    }

    /// <summary>
    /// Adds a coordinate to the list.
    /// </summary>
    /// <param name="coordinate">The coordinate to add.</param>
    public void AddCoordinate(TreasureCoordinate coordinate)
    {
        // Clean player name from special characters if it's not empty
        if (!string.IsNullOrEmpty(coordinate.PlayerName))
        {
            coordinate.PlayerName = RemoveSpecialCharactersFromName(coordinate.PlayerName);
        }

        // Add the coordinate
        Coordinates.Add(coordinate);
    }

    /// <summary>
    /// Imports coordinates from text.
    /// </summary>
    /// <param name="text">The text to import coordinates from.</param>
    /// <returns>The number of coordinates imported.</returns>
    public int ImportCoordinates(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        int importedCount = importExportService.ImportCoordinates(text, AddCoordinate);

        if (importedCount > 0)
        {
            // Raise the event
            OnCoordinatesImported?.Invoke(this, importedCount);
        }

        return importedCount;
    }

    /// <summary>
    /// Exports coordinates to a Base64 encoded string.
    /// </summary>
    /// <returns>A Base64 encoded string containing the coordinates.</returns>
    public string ExportCoordinates()
    {
        return importExportService.ExportCoordinates(Coordinates.ToList());
    }

    /// <summary>
    /// Exports coordinates and raises the OnCoordinatesExported event.
    /// </summary>
    public void ExportCoordinatesAndRaiseEvent()
    {
        var exportedString = ExportCoordinates();

        if (!string.IsNullOrEmpty(exportedString))
        {
            // Raise the event
            OnCoordinatesExported?.Invoke(this, exportedString);
        }
    }

    /// <summary>
    /// Clears all coordinates.
    /// </summary>
    public void ClearCoordinates()
    {
        Coordinates.Clear();

        // Raise the event
        OnCoordinatesCleared?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Deletes a coordinate and moves it to the trash bin.
    /// </summary>
    /// <param name="index">The index of the coordinate to delete.</param>
    /// <returns>True if the coordinate was deleted, false otherwise.</returns>
    public bool DeleteCoordinate(int index)
    {
        if (index < 0 || index >= Coordinates.Count)
            return false;

        var coordinate = Coordinates[index];
        DeletedCoordinates.Add(coordinate);
        Coordinates.RemoveAt(index);

        // Raise the event
        OnCoordinateDeleted?.Invoke(this, coordinate);

        return true;
    }

    /// <summary>
    /// Restores a coordinate from the trash bin.
    /// </summary>
    /// <param name="index">The index of the coordinate in the trash bin.</param>
    /// <returns>True if the coordinate was restored, false otherwise.</returns>
    public bool RestoreCoordinate(int index)
    {
        if (index < 0 || index >= DeletedCoordinates.Count)
            return false;

        var coordinate = DeletedCoordinates[index];
        Coordinates.Add(coordinate);
        DeletedCoordinates.RemoveAt(index);

        // Raise the event
        OnCoordinateRestored?.Invoke(this, coordinate);

        return true;
    }

    /// <summary>
    /// Clears all coordinates in the trash bin.
    /// </summary>
    public void ClearTrash()
    {
        DeletedCoordinates.Clear();
    }

    /// <summary>
    /// Gets a coordinate by its index.
    /// </summary>
    /// <param name="index">The index of the coordinate.</param>
    /// <returns>The coordinate at the specified index, or null if the index is out of range.</returns>
    public TreasureCoordinate? GetCoordinate(int index)
    {
        if (index < 0 || index >= Coordinates.Count)
            return null;

        return Coordinates[index];
    }

    /// <summary>
    /// Gets a coordinate from the trash bin by its index.
    /// </summary>
    /// <param name="index">The index of the coordinate in the trash bin.</param>
    /// <returns>The coordinate at the specified index, or null if the index is out of range.</returns>
    public TreasureCoordinate? GetDeletedCoordinate(int index)
    {
        if (index < 0 || index >= DeletedCoordinates.Count)
            return null;

        return DeletedCoordinates[index];
    }

    /// <summary>
    /// Marks a coordinate as collected or not collected.
    /// </summary>
    /// <param name="index">The index of the coordinate.</param>
    /// <param name="isCollected">Whether the coordinate is collected.</param>
    /// <returns>True if the coordinate was marked, false otherwise.</returns>
    public bool MarkCoordinateAsCollected(int index, bool isCollected)
    {
        var coordinate = GetCoordinate(index);
        if (coordinate == null)
            return false;

        coordinate.IsCollected = isCollected;
        return true;
    }

    /// <summary>
    /// Gets the current collection status of coordinates (how many are collected).
    /// </summary>
    /// <returns>A tuple containing the number of collected coordinates and the total number of coordinates.</returns>
    public (int Collected, int Total) GetCollectionStatus()
    {
        var collected = Coordinates.Count(c => c.IsCollected);
        var total = Coordinates.Count;
        return (collected, total);
    }

    /// <summary>
    /// Optimizes the route through the coordinates considering player location, teleport costs, and travel distances.
    /// </summary>
    public List<TreasureCoordinate> OptimizeRoute()
    {
        return routeOptimizer.OptimizeRoute(Coordinates.ToList());
    }

    /// <summary>
    /// Asynchronously optimizes the route through the coordinates considering player location, teleport costs, and travel distances.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the optimization process.</param>
    /// <returns>A task that represents the asynchronous optimization operation.</returns>
    public async Task<List<TreasureCoordinate>> OptimizeRouteAsync(CancellationToken cancellationToken = default)
    {
        // Get player location on main thread before async operation
        var playerLocation = plugin.PlayerLocationService.GetCurrentLocation();
        return await routeOptimizer.OptimizeRouteAsync(Coordinates.ToList(), cancellationToken, playerLocation);
    }

    /// <summary>
    /// Cancels any ongoing route optimization.
    /// </summary>
    public void CancelOptimization()
    {
        routeOptimizer.CancelOptimization();
    }

    /// <summary>
    /// Gets whether a route optimization is currently in progress.
    /// </summary>
    public bool IsOptimizationInProgress => routeOptimizer.IsOptimizationInProgress;

    /// <summary>
    /// Resets the route optimization, restoring the original order and resetting all collection states.
    /// </summary>
    /// <returns>The original order of coordinates.</returns>
    public List<TreasureCoordinate> ResetRouteOptimization()
    {
        return routeOptimizer.ResetRouteOptimization();
    }

    /// <summary>
    /// Disposes the service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        // Clear event handlers to prevent memory leaks
        OnCoordinatesImported = null;
        OnCoordinatesExported = null;
        OnCoordinateDeleted = null;
        OnCoordinateRestored = null;
        OnCoordinatesCleared = null;
        OnRouteOptimized = null;
        OnRouteOptimizationReset = null;
    }

    /// <summary>
    /// Removes special characters from player names like BoxedNumber and BoxedOutlinedNumber
    /// </summary>
    /// <param name="name">The name that might contain special characters</param>
    /// <returns>The name with special characters removed</returns>
    private string RemoveSpecialCharactersFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Create a StringBuilder to build the cleaned name
        var cleanedName = new StringBuilder(name.Length);

        // Process each character in the name
        foreach (var c in name)
        {
            // Check for BoxedNumber character range (0xE090 to 0xE097)
            // These are game-specific number icons
            if ((int)c >= 0xE090 && (int)c <= 0xE097)
                continue;

            // Check for BoxedOutlinedNumber character range (0xE0E1 to 0xE0E9)
            // These are game-specific outlined number icons
            if ((int)c >= 0xE0E1 && (int)c <= 0xE0E9)
                continue;

            // Remove star character (★) often used in player names
            if (c == '★')
                continue;

            // Any other special characters that need to be filtered can be added here

            // Add the character to the cleaned name if it passed all filters
            cleanedName.Append(c);
        }

        return cleanedName.ToString().Trim();
    }
}
