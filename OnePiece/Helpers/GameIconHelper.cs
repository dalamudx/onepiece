using Dalamud.Game.Text;
using System;
using System.Collections.Generic;

namespace OnePiece.Helpers
{
    /// <summary>
    /// Helper class for handling game-specific special characters and icons using Dalamud's SeIconChar.
    /// Provides centralized methods for converting game icons to strings and managing special character ranges.
    /// </summary>
    public static class GameIconHelper
    {
        /// <summary>
        /// Gets the string representation of a Number icon (1-9) based on index.
        /// </summary>
        /// <param name="index">The index representing the coordinate number (0-8)</param>
        /// <returns>The string representation of the number icon</returns>
        public static string GetNumberIcon(int index)
        {
            if (index < 0 || index >= 9)
                return string.Empty;

            var iconChar = SeIconChar.Number1 + index;
            return char.ConvertFromUtf32((int)iconChar);
        }

        /// <summary>
        /// Gets the string representation of a BoxedNumber icon (1-31) based on index.
        /// </summary>
        /// <param name="index">The index representing the coordinate number (0-30)</param>
        /// <returns>The string representation of the boxed number icon</returns>
        public static string GetBoxedNumberIcon(int index)
        {
            if (index < 0 || index >= 31)
                return string.Empty;

            var iconChar = SeIconChar.BoxedNumber1 + index;
            return char.ConvertFromUtf32((int)iconChar);
        }

        /// <summary>
        /// Gets the string representation of a BoxedOutlinedNumber icon (1-9) based on index.
        /// </summary>
        /// <param name="index">The index representing the coordinate number (0-8)</param>
        /// <returns>The string representation of the boxed outlined number icon</returns>
        public static string GetBoxedOutlinedNumberIcon(int index)
        {
            if (index < 0 || index >= 9)
                return string.Empty;

            var iconChar = SeIconChar.BoxedOutlinedNumber1 + index;
            return char.ConvertFromUtf32((int)iconChar);
        }

        /// <summary>
        /// Gets the string representation of a LinkMarker icon for coordinate display.
        /// </summary>
        /// <returns>The string representation of the link marker icon</returns>
        public static string GetLinkMarkerIcon()
        {
            return char.ConvertFromUtf32((int)SeIconChar.LinkMarker);
        }

        /// <summary>
        /// Gets the maximum supported index for Number icons using game icons.
        /// </summary>
        public static int MaxNumberIndex => 8; // 0-8 (9 total) for game icons Number1-Number9

        /// <summary>
        /// Gets the maximum supported index for BoxedNumber icons using game icons.
        /// </summary>
        public static int MaxBoxedNumberIndex => 30; // 0-30 (31 total) for game icons BoxedNumber1-BoxedNumber31

        /// <summary>
        /// Gets the maximum supported index for BoxedOutlinedNumber icons using game icons.
        /// </summary>
        public static int MaxBoxedOutlinedNumberIndex => 8; // 0-8 (9 total) for game icons BoxedOutlinedNumber1-BoxedOutlinedNumber9

        /// <summary>
        /// Checks if a character is a game-specific special character that should be filtered from player names.
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <returns>True if the character is a special game icon that should be filtered</returns>
        public static bool IsGameSpecialCharacter(char character)
        {
            int charValue = character;

            // Check for Number icons (0xE060 to 0xE069) - Number0 to Number9
            if (charValue >= (int)SeIconChar.Number0 && charValue <= (int)SeIconChar.Number9)
                return true;

            // Check for BoxedNumber icons (0xE08F to 0xE0AE) - BoxedNumber0 to BoxedNumber31
            if (charValue >= (int)SeIconChar.BoxedNumber0 && charValue <= (int)SeIconChar.BoxedNumber31)
                return true;

            // Check for BoxedOutlinedNumber icons (0xE0E0 to 0xE0E9) - BoxedOutlinedNumber0 to BoxedOutlinedNumber9
            if (charValue >= (int)SeIconChar.BoxedOutlinedNumber0 && charValue <= (int)SeIconChar.BoxedOutlinedNumber9)
                return true;

            // Check for other common special characters
            if (charValue == (int)SeIconChar.LinkMarker)
                return true;

            return false;
        }

        /// <summary>
        /// Removes all game-specific special characters from a string (typically player names).
        /// </summary>
        /// <param name="input">The input string that may contain special characters</param>
        /// <returns>The cleaned string with special characters removed</returns>
        public static string RemoveGameSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder(input.Length);
            
            foreach (char c in input)
            {
                if (!IsGameSpecialCharacter(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Gets a preview string for a specific message component type.
        /// Used for UI previews and testing.
        /// </summary>
        /// <param name="componentType">The message component type</param>
        /// <returns>A preview string representation</returns>
        public static string GetComponentPreview(MessageComponentType componentType)
        {
            return componentType switch
            {
                MessageComponentType.Number => GetNumberIcon(0), // Show Number1
                MessageComponentType.BoxedNumber => GetBoxedNumberIcon(0), // Show BoxedNumber1
                MessageComponentType.BoxedOutlinedNumber => GetBoxedOutlinedNumberIcon(0), // Show BoxedOutlinedNumber1
                MessageComponentType.Coordinates => $"{GetLinkMarkerIcon()} Location Example",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Validates if an index is valid for a specific icon type within the game icon range.
        /// </summary>
        /// <param name="componentType">The message component type</param>
        /// <param name="index">The index to validate</param>
        /// <returns>True if the index is valid for the specified component type</returns>
        public static bool IsValidIndexForComponent(MessageComponentType componentType, int index)
        {
            return componentType switch
            {
                MessageComponentType.Number => index >= 0 && index <= MaxNumberIndex,
                MessageComponentType.BoxedNumber => index >= 0 && index <= MaxBoxedNumberIndex,
                MessageComponentType.BoxedOutlinedNumber => index >= 0 && index <= MaxBoxedOutlinedNumberIndex,
                _ => false
            };
        }

        /// <summary>
        /// Checks if a coordinate count is within the game icon range for a specific component type.
        /// </summary>
        /// <param name="componentType">The message component type</param>
        /// <param name="coordinateCount">The total number of coordinates</param>
        /// <returns>True if all coordinates can be displayed with game icons</returns>
        public static bool IsCoordinateCountWithinIconRange(MessageComponentType componentType, int coordinateCount)
        {
            return componentType switch
            {
                MessageComponentType.Number => coordinateCount <= 9, // Number1-Number9
                MessageComponentType.BoxedNumber => coordinateCount <= 31, // BoxedNumber1-BoxedNumber31
                MessageComponentType.BoxedOutlinedNumber => coordinateCount <= 9, // BoxedOutlinedNumber1-BoxedOutlinedNumber9
                _ => false
            };
        }

        /// <summary>
        /// Gets the display range string for a specific component type.
        /// </summary>
        /// <param name="componentType">The message component type</param>
        /// <returns>The range string (e.g., "1-9") or empty string if not applicable</returns>
        public static string GetComponentDisplayRange(MessageComponentType componentType)
        {
            return componentType switch
            {
                MessageComponentType.Number => "1-9",
                MessageComponentType.BoxedNumber => "1-31",
                MessageComponentType.BoxedOutlinedNumber => "1-9",
                _ => string.Empty
            };
        }
    }
}
