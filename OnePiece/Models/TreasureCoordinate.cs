using System;
using System.Numerics;

namespace OnePiece.Models;

/// <summary>
/// Represents a treasure coordinate point on the map.
/// </summary>
/// <summary>
/// Represents the coordinate system type used for a coordinate.
/// </summary>
public enum CoordinateSystemType
{
    /// <summary>
    /// Map coordinates as shown on the in-game map
    /// </summary>
    Map,
    
    /// <summary>
    /// World coordinates used by the game engine
    /// </summary>
    World
}

public class TreasureCoordinate
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public float X { get; set; }
    
    /// <summary>
    /// Gets or sets the coordinate system type.
    /// </summary>
    public CoordinateSystemType CoordinateSystem { get; set; } = CoordinateSystemType.Map;

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
    /// Gets or sets a general purpose tag to store additional information.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation instruction for reaching this coordinate from the previous point.
    /// </summary>
    public string NavigationInstruction { get; set; } = string.Empty;

    /// <summary>
    /// Gets the position as a Vector2.
    /// </summary>
    public Vector2 Position => new(X, Y);

    /// <summary>
    /// Initializes a new instance of the <see cref="TreasureCoordinate"/> class.
    /// </summary>
    public TreasureCoordinate()
    {
        CoordinateSystem = CoordinateSystemType.Map; // Default to map coordinates
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TreasureCoordinate"/> class.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="mapArea">The map area.</param>
    /// <param name="coordinateSystem">The coordinate system type.</param>
    /// <param name="name">The name or description of this treasure point.</param>
    /// <param name="playerName">The player name who shared this coordinate.</param>
    public TreasureCoordinate(float x, float y, string mapArea = "", CoordinateSystemType coordinateSystem = CoordinateSystemType.Map, string name = "", string playerName = "")
    {
        X = x;
        Y = y;
        MapArea = mapArea;
        CoordinateSystem = coordinateSystem;
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
        // Ensure coordinates are in the same system before calculating distance
        var normalizedOther = EnsureMatchingCoordinateSystem(other);
        
        // Calculate Euclidean distance between the two points
        float dx = X - normalizedOther.X;
        float dy = Y - normalizedOther.Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
        
#if DEBUG
        if (this.CoordinateSystem != other.CoordinateSystem)
        {
            Plugin.Log.Debug($"Calculated distance between different coordinate systems: {this.CoordinateSystem} vs {other.CoordinateSystem} (original), normalized to {this.CoordinateSystem}");
        }
#endif
        
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
        
        // Add coordinate system type for debugging
#if DEBUG
        result += $" [{CoordinateSystem}]";
#endif

        return result;
    }
    
    /// <summary>
    /// Ensures that the other coordinate uses the same coordinate system as this one.
    /// If not, creates a new coordinate with converted values.
    /// </summary>
    /// <param name="other">The other coordinate to check against.</param>
    /// <returns>A coordinate in the same system as this one.</returns>
    public TreasureCoordinate EnsureMatchingCoordinateSystem(TreasureCoordinate other)
    {
        if (this.CoordinateSystem == other.CoordinateSystem)
        {
            return other; // Already using the same system
        }
        
        // Convert the other coordinate to match this coordinate's system
        return other.ToCoordinateSystem(this.CoordinateSystem);
    }
    
    /// <summary>
    /// Converts this coordinate to the specified coordinate system.
    /// </summary>
    /// <param name="targetSystem">The target coordinate system.</param>
    /// <returns>A new coordinate in the target system.</returns>
    public TreasureCoordinate ToCoordinateSystem(CoordinateSystemType targetSystem)
    {
        if (this.CoordinateSystem == targetSystem)
        {
            return this; // Already in the target system
        }
        
        // Create a new coordinate with the same properties
        var result = new TreasureCoordinate
        {
            MapArea = this.MapArea,
            Name = this.Name,
            PlayerName = this.PlayerName,
            IsCollected = this.IsCollected,
            Tag = this.Tag,
            NavigationInstruction = this.NavigationInstruction,
            CoordinateSystem = targetSystem
        };
        
        // Convert coordinates based on the direction of conversion
        if (this.CoordinateSystem == CoordinateSystemType.World && targetSystem == CoordinateSystemType.Map)
        {
            // World to Map conversion requires scale and offset values which we don't have here
            // This should be handled by the PlayerLocationService instead
            throw new InvalidOperationException("World to Map conversion requires scale and offset values. Use PlayerLocationService for this conversion.");
        }
        else if (this.CoordinateSystem == CoordinateSystemType.Map && targetSystem == CoordinateSystemType.World)
        {
            // Map to World conversion requires scale and offset values which we don't have here
            // This should be handled by the PlayerLocationService instead
            throw new InvalidOperationException("Map to World conversion requires scale and offset values. Use PlayerLocationService for this conversion.");
        }
        
        // If we reach here, we have an unsupported conversion
        throw new InvalidOperationException($"Unsupported coordinate system conversion: {this.CoordinateSystem} to {targetSystem}");
    }
}
