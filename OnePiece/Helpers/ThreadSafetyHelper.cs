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
    /// Executes an action only if on the main thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="onNonMainThread">Optional action to execute if not on main thread.</param>
    /// <returns>True if action was executed, false if not on main thread.</returns>
    public static bool ExecuteOnMainThread(Action action, Action? onNonMainThread = null)
    {
        if (IsMainThread())
        {
            action();
            return true;
        }
        else
        {
            onNonMainThread?.Invoke();
            return false;
        }
    }

    /// <summary>
    /// Executes a function only if on the main thread.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="defaultValue">The default value to return if not on main thread.</param>
    /// <param name="onNonMainThread">Optional action to execute if not on main thread.</param>
    /// <returns>The function result if on main thread, otherwise the default value.</returns>
    public static T ExecuteOnMainThread<T>(Func<T> func, T defaultValue, Action? onNonMainThread = null)
    {
        if (IsMainThread())
        {
            return func();
        }
        else
        {
            onNonMainThread?.Invoke();
            return defaultValue;
        }
    }

    /// <summary>
    /// Throws an exception if not on the main thread.
    /// </summary>
    /// <param name="operationName">The name of the operation that requires main thread.</param>
    /// <exception cref="InvalidOperationException">Thrown if not on main thread.</exception>
    public static void EnsureMainThread(string operationName)
    {
        if (!IsMainThread())
        {
            throw new InvalidOperationException($"{operationName} must be called from the main thread");
        }
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
