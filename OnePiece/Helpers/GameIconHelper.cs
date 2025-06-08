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
        /// Gets the string representation of a Number icon (1-8) based on index.
        /// </summary>
        /// <param name="index">The index (0-7) representing Number1-Number8</param>
        /// <returns>The string representation of the number icon, or empty string if index is invalid</returns>
        public static string GetNumberIcon(int index)
        {
            if (index < 0 || index >= 8)
                return string.Empty;

            var iconChar = SeIconChar.Number1 + index;
            return char.ConvertFromUtf32((int)iconChar);
        }

        /// <summary>
        /// Gets the string representation of a BoxedNumber icon (1-8) based on index.
        /// </summary>
        /// <param name="index">The index (0-7) representing BoxedNumber1-BoxedNumber8</param>
        /// <returns>The string representation of the boxed number icon, or empty string if index is invalid</returns>
        public static string GetBoxedNumberIcon(int index)
        {
            if (index < 0 || index >= 8)
                return string.Empty;

            var iconChar = SeIconChar.BoxedNumber1 + index;
            return char.ConvertFromUtf32((int)iconChar);
        }

        /// <summary>
        /// Gets the string representation of a BoxedOutlinedNumber icon (1-9) based on index.
        /// </summary>
        /// <param name="index">The index (0-8) representing BoxedOutlinedNumber1-BoxedOutlinedNumber9</param>
        /// <returns>The string representation of the boxed outlined number icon, or empty string if index is invalid</returns>
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
        /// Gets the maximum supported index for Number icons.
        /// </summary>
        public static int MaxNumberIndex => 7; // 0-7 (8 total)

        /// <summary>
        /// Gets the maximum supported index for BoxedNumber icons.
        /// </summary>
        public static int MaxBoxedNumberIndex => 7; // 0-7 (8 total)

        /// <summary>
        /// Gets the maximum supported index for BoxedOutlinedNumber icons.
        /// </summary>
        public static int MaxBoxedOutlinedNumberIndex => 8; // 0-8 (9 total)

        /// <summary>
        /// Checks if a character is a game-specific special character that should be filtered from player names.
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <returns>True if the character is a special game icon that should be filtered</returns>
        public static bool IsGameSpecialCharacter(char character)
        {
            int charValue = (int)character;

            // Check for Number icons (0xE061 to 0xE068)
            if (charValue >= (int)SeIconChar.Number1 && charValue <= (int)SeIconChar.Number8)
                return true;

            // Check for BoxedNumber icons (0xE090 to 0xE097)
            if (charValue >= (int)SeIconChar.BoxedNumber1 && charValue <= (int)SeIconChar.BoxedNumber8)
                return true;

            // Check for BoxedOutlinedNumber icons (0xE0E1 to 0xE0E9)
            if (charValue >= (int)SeIconChar.BoxedOutlinedNumber1 && charValue <= (int)SeIconChar.BoxedOutlinedNumber9)
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
        /// Validates if an index is valid for a specific icon type.
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
    }
}
