using System;
using System.Numerics;

namespace OnePiece.Models;

/// <summary>
/// Represents a treasure coordinate point on the map.
/// </summary>
public class TreasureCoordinate
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the name or description of this treasure point.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this treasure has been collected.
    /// </summary>
    public bool IsCollected { get; set; }

    /// <summary>
    /// Gets or sets the player name who shared this coordinate.
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the map area name.
    /// </summary>
    public string MapArea { get; set; } = string.Empty;

    /// <summary>
    /// Gets the position as a Vector2.
    /// </summary>
    public Vector2 Position => new(X, Y);

    /// <summary>
    /// Initializes a new instance of the <see cref="TreasureCoordinate"/> class.
    /// </summary>
    public TreasureCoordinate()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TreasureCoordinate"/> class.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="mapArea">The map area name.</param>
    /// <param name="name">The name or description of this treasure point.</param>
    /// <param name="playerName">The player name who shared this coordinate.</param>
    public TreasureCoordinate(float x, float y, string mapArea = "", string name = "", string playerName = "")
    {
        X = x;
        Y = y;
        MapArea = mapArea;
        Name = name;
        PlayerName = playerName;
    }

    /// <summary>
    /// Calculates the distance to another coordinate.
    /// </summary>
    /// <param name="other">The other coordinate.</param>
    /// <returns>The distance between the two coordinates.</returns>
    public float DistanceTo(TreasureCoordinate other)
    {
        // Calculate Euclidean distance between the two points
        float dx = X - other.X;
        float dy = Y - other.Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        // Removed debug logging to improve performance

        return distance;
    }

    /// <summary>
    /// Returns a string representation of this coordinate.
    /// </summary>
    /// <returns>A string representation of this coordinate.</returns>
    public override string ToString()
    {
        var result = $"{X:F1}, {Y:F1}";

        // Add map area if available
        if (!string.IsNullOrEmpty(MapArea))
        {
            result = $"{MapArea} ({result})";
        }

        // Add name if available
        if (!string.IsNullOrEmpty(Name))
        {
            result += $" - {Name}";
        }

        return result;
    }
}
