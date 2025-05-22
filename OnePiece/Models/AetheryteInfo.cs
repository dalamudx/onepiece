using System;
using System.Numerics;

namespace OnePiece.Models;

/// <summary>
/// Represents information about an aetheryte (teleport crystal).
/// </summary>
public class AetheryteInfo
{
    /// <summary>
    /// Gets or sets the ID of the aetheryte.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the aetheryte.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the territory ID where the aetheryte is located.
    /// </summary>
    public uint TerritoryId { get; set; }

    /// <summary>
    /// Gets or sets the map ID where the aetheryte is located.
    /// </summary>
    public uint MapId { get; set; }

    /// <summary>
    /// Gets or sets the map area name where the aetheryte is located.
    /// </summary>
    public string MapArea { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position of the aetheryte on the map.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the base teleport fee for this aetheryte.
    /// </summary>
    public int BaseTeleportFee { get; set; }

    /// <summary>
    /// Gets or sets the actual teleport fee for this aetheryte.
    /// </summary>
    public int ActualTeleportFee { get; set; }

    /// <summary>
    /// Gets or sets whether this aetheryte is a favorite (reduced teleport cost).
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets whether this aetheryte is the free destination (no teleport cost).
    /// </summary>
    public bool IsFreeDestination { get; set; }

    /// <summary>
    /// Gets or sets the aetheryte row ID in the game data.
    /// </summary>
    public uint AetheryteRowId { get; set; }

    /// <summary>
    /// Calculates the teleport fee to this aetheryte.
    /// </summary>
    /// <returns>The teleport fee in gil.</returns>
    public int CalculateTeleportFee()
    {
        // If we have an actual teleport fee from the game, use that
        if (ActualTeleportFee > 0)
            return ActualTeleportFee;

        // Otherwise fall back to the simplified calculation
        if (IsFreeDestination)
            return 0;

        if (IsFavorite)
            return BaseTeleportFee / 2;

        return BaseTeleportFee;
    }

    /// <summary>
    /// Calculates the distance from this aetheryte to a coordinate.
    /// </summary>
    /// <param name="coordinate">The coordinate to calculate distance to.</param>
    /// <returns>The distance in map units.</returns>
    public float DistanceTo(TreasureCoordinate coordinate)
    {
        // Calculate Euclidean distance between the two points
        float dx = Position.X - coordinate.X;
        float dy = Position.Y - coordinate.Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        // Removed debug logging to improve performance

        return distance;
    }
}
