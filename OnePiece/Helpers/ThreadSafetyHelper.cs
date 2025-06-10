using System;
using System.Threading;

namespace OnePiece.Helpers;

/// <summary>
/// Helper class for thread safety checks and operations.
/// </summary>
public static class ThreadSafetyHelper
{
    private static int? mainThreadId;

    /// <summary>
    /// Initializes the main thread ID. Should be called from the main thread during plugin initialization.
    /// </summary>
    public static void InitializeMainThreadId()
    {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    /// <summary>
    /// Gets a value indicating whether the current thread is the main thread.
    /// </summary>
    /// <returns>True if on main thread, false otherwise.</returns>
    public static bool IsMainThread()
    {
        var currentThreadId = Thread.CurrentThread.ManagedThreadId;

        // For FFXIV plugins, the main thread is typically thread ID 1
        // This is more reliable than relying on plugin initialization thread
        return currentThreadId == 1;
    }



    /// <summary>
    /// Gets the current thread information for debugging.
    /// </summary>
    /// <returns>A string containing thread information.</returns>
    public static string GetThreadInfo()
    {
        var currentId = Thread.CurrentThread.ManagedThreadId;
        var isMain = IsMainThread();
        var initId = mainThreadId?.ToString() ?? "unknown";

        return $"Current: {currentId}, InitThread: {initId}, IsMain: {isMain} (Main=1)";
    }
}
