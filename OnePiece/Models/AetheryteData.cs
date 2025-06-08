using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Numerics;

namespace OnePiece.Models;

/// <summary>
/// Represents aetheryte data loaded from aetheryte.json file
/// </summary>
public class AetheryteData
{
    /// <summary>
    /// Gets or sets the timestamp of the data
    /// </summary>
    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the game version
    /// </summary>
    [JsonPropertyName("GameVersion")]
    public string GameVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of aetherytes
    /// </summary>
    [JsonPropertyName("Aetherytes")]
    public List<AetheryteDataEntry> Aetherytes { get; set; } = new List<AetheryteDataEntry>();
}

/// <summary>
/// Represents a single aetheryte entry from the json data file
/// </summary>
public class AetheryteDataEntry
{
    /// <summary>
    /// Gets or sets the aetheryte ID
    /// </summary>
    [JsonPropertyName("AetheryteRowId")] // Keep JSON compatibility
    public uint AetheryteId { get; set; }

    /// <summary>
    /// Gets or sets the name of the aetheryte
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the map area where the aetheryte is located
    /// </summary>
    [JsonPropertyName("MapArea")]
    public string MapArea { get; set; } = string.Empty;



    /// <summary>
    /// Gets or sets the X coordinate of the aetheryte on the map
    /// </summary>
    [JsonPropertyName("X")]
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the aetheryte on the map
    /// </summary>
    [JsonPropertyName("Y")]
    public float Y { get; set; }

    /// <summary>
    /// Converts this data entry to an AetheryteInfo object
    /// </summary>
    /// <returns>A new AetheryteInfo object populated with data from this entry</returns>
    public AetheryteInfo ToAetheryteInfo()
    {
        return new AetheryteInfo
        {
            AetheryteId = this.AetheryteId,
            Name = this.Name,
            MapArea = this.MapArea,
            Position = new Vector2(this.X, this.Y)
        };
    }
    
    /// <summary>
    /// Calculates the distance from this aetheryte to a coordinate
    /// </summary>
    /// <param name="coordinate">The target coordinate</param>
    /// <returns>The distance value</returns>
    public float DistanceTo(TreasureCoordinate coordinate)
    {
        // Check coordinate system consistency
        // We assume aetheryte coordinates are always in Map coordinate system
        if (coordinate.CoordinateSystem != CoordinateSystemType.Map)
        {
            // Log warning about mismatched coordinate systems
            Plugin.Log.Warning($"Comparing aetheryte with coordinate using different coordinate systems: Map vs {coordinate.CoordinateSystem}");
        }
        
        // Calculate Euclidean distance
        float dx = X - coordinate.X;
        float dy = Y - coordinate.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
}
