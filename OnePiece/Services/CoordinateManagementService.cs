using System;
using System.Collections.Generic;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for managing treasure coordinates.
/// </summary>
public class CoordinateManagementService
{
    private readonly Plugin plugin;
    private readonly TextParsingService textParsingService;

    /// <summary>
    /// Gets the list of treasure coordinates.
    /// </summary>
    public List<TreasureCoordinate> Coordinates { get; private set; } = new();

    /// <summary>
    /// Gets the list of deleted treasure coordinates (trash bin).
    /// </summary>
    public List<TreasureCoordinate> DeletedCoordinates { get; private set; } = new();

    /// <summary>
    /// Event raised when a coordinate is deleted.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateDeleted;

    /// <summary>
    /// Event raised when a coordinate is restored from trash.
    /// </summary>
    public event EventHandler<TreasureCoordinate>? OnCoordinateRestored;

    /// <summary>
    /// Event raised when coordinates are cleared.
    /// </summary>
    public event EventHandler? OnCoordinatesCleared;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateManagementService"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="textParsingService">The text parsing service.</param>
    public CoordinateManagementService(Plugin plugin, TextParsingService textParsingService)
    {
        this.plugin = plugin;
        this.textParsingService = textParsingService;
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
            coordinate.PlayerName = textParsingService.RemoveSpecialCharactersFromName(coordinate.PlayerName);
        }
        
        // Add the coordinate
        Coordinates.Add(coordinate);
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
}
