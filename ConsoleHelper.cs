using System;

namespace Svero.CopySpotlightPics;

/// <summary>
/// Specifies helper methods for the console.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Write the specified message on the console.
    /// </summary>
    /// <param name="message">Message to write</param>
    public static void WriteMessage(string message)
    {
        WriteMessage(message, Console.ForegroundColor);
    }

    /// <summary>
    /// Writes the specified message using the specified color on the console.
    /// </summary>
    /// <param name="message">Message to write</param>
    /// <param name="color">Foreground color to use</param>
    public static void WriteMessage(string message, ConsoleColor color)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            // Just an empty line
            Console.WriteLine();
            return;
        }

        var currentColor = Console.ForegroundColor;

        Console.ForegroundColor = color;
        Console.WriteLine(message);

        Console.ForegroundColor = currentColor;
    }
}