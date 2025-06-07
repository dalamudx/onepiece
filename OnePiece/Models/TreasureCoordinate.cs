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

/// <summary>
/// Represents the type of coordinate point.
/// </summary>
public enum CoordinateType
{
    /// <summary>
    /// A regular treasure hunt point
    /// </summary>
    TreasurePoint,
    
    /// <summary>
    /// A teleport point (aetheryte)
    /// </summary>
    TeleportPoint,
    
    /// <summary>
    /// A waypoint for navigation
    /// </summary>
    WayPoint,
    
    /// <summary>
    /// Player's current position
    /// </summary>
    PlayerPosition
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
    /// Gets or sets additional notes about this coordinate.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of this coordinate point.
    /// </summary>
    public CoordinateType Type { get; set; } = CoordinateType.TreasurePoint;
    
    /// <summary>
    /// Gets or sets a value indicating whether this coordinate is a teleport point.
    /// This property is maintained for backward compatibility.
    /// New code should use the Type property instead.
    /// </summary>
    public bool IsTeleportPoint
    {
        get { return Type == CoordinateType.TeleportPoint; }
        set { Type = value ? CoordinateType.TeleportPoint : CoordinateType.TreasurePoint; }
    }
    
    /// <summary>
    /// Gets or sets the Aetheryte ID for teleportation.
    /// This is the direct reference to an Aetheryte in the game data.
    /// </summary>
    public uint AetheryteId { get; set; }

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
    /// Creates a deep copy of this coordinate with all properties preserved.
    /// </summary>
    /// <returns>A new coordinate that is an exact copy of this one.</returns>
    public TreasureCoordinate DeepCopy()
    {
        return new TreasureCoordinate
        {
            X = this.X,
            Y = this.Y,
            MapArea = this.MapArea,
            Name = this.Name,
            PlayerName = this.PlayerName,
            IsCollected = this.IsCollected,
            Tag = this.Tag,
            NavigationInstruction = this.NavigationInstruction,
            Notes = this.Notes,
            CoordinateSystem = this.CoordinateSystem,
            Type = this.Type,
            AetheryteId = this.AetheryteId
        };
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

        // Create a new coordinate with ALL properties preserved
        var result = new TreasureCoordinate
        {
            MapArea = this.MapArea,
            Name = this.Name,
            PlayerName = this.PlayerName, // 确保玩家名被保留
            IsCollected = this.IsCollected,
            Tag = this.Tag,
            NavigationInstruction = this.NavigationInstruction,
            Notes = this.Notes,
            CoordinateSystem = targetSystem,
            Type = this.Type,
            AetheryteId = this.AetheryteId
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

/// <summary>
/// Builder class for creating TreasureCoordinate objects with preserved data.
/// </summary>
public class TreasureCoordinateBuilder
{
    private TreasureCoordinate coordinate;

    public TreasureCoordinateBuilder()
    {
        coordinate = new TreasureCoordinate();
    }

    /// <summary>
    /// Creates a builder from an existing coordinate, preserving all data.
    /// </summary>
    /// <param name="source">The source coordinate to copy from.</param>
    /// <returns>The builder instance.</returns>
    public static TreasureCoordinateBuilder FromExisting(TreasureCoordinate source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var builder = new TreasureCoordinateBuilder();
        builder.coordinate = source.DeepCopy();
        return builder;
    }

    /// <summary>
    /// Sets the coordinate position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>The builder instance.</returns>
    public TreasureCoordinateBuilder WithPosition(float x, float y)
    {
        coordinate.X = x;
        coordinate.Y = y;
        return this;
    }

    /// <summary>
    /// Sets the coordinate type.
    /// </summary>
    /// <param name="type">The coordinate type.</param>
    /// <returns>The builder instance.</returns>
    public TreasureCoordinateBuilder WithType(CoordinateType type)
    {
        coordinate.Type = type;
        return this;
    }

    /// <summary>
    /// Sets the aetheryte ID for teleportation.
    /// </summary>
    /// <param name="aetheryteId">The aetheryte ID.</param>
    /// <returns>The builder instance.</returns>
    public TreasureCoordinateBuilder WithAetheryteId(uint aetheryteId)
    {
        coordinate.AetheryteId = aetheryteId;
        return this;
    }

    /// <summary>
    /// Sets the player name.
    /// </summary>
    /// <param name="playerName">The player name.</param>
    /// <returns>The builder instance.</returns>
    public TreasureCoordinateBuilder WithPlayerName(string playerName)
    {
        coordinate.PlayerName = playerName ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets the map area.
    /// </summary>
    /// <param name="mapArea">The map area.</param>
    /// <returns>The builder instance.</returns>
    public TreasureCoordinateBuilder WithMapArea(string mapArea)
    {
        coordinate.MapArea = mapArea ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Builds the final coordinate object.
    /// </summary>
    /// <returns>The built coordinate.</returns>
    public TreasureCoordinate Build()
    {
        return coordinate.DeepCopy(); // Return a copy to prevent external modification
    }
}
