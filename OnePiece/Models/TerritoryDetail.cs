namespace OnePiece.Models;

/// <summary>
/// Represents details about a territory in the game.
/// </summary>
public class TerritoryDetail
{
    /// <summary>
    /// Gets or sets the name of the territory.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the territory ID.
    /// </summary>
    public uint TerritoryId { get; set; }

    /// <summary>
    /// Gets or sets the map ID.
    /// </summary>
    public uint MapId { get; set; }

    /// <summary>
    /// Gets or sets the size factor of the map.
    /// </summary>
    public ushort SizeFactor { get; set; }

    /// <summary>
    /// Gets the scale factor calculated from the size factor.
    /// </summary>
    public float Scale => SizeFactor / 100.0f;
}
