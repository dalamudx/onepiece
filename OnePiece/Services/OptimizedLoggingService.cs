using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using OnePiece.Models;

namespace OnePiece.Services;

/// <summary>
/// Service for optimized logging that reduces redundant log messages.
/// </summary>
public class OptimizedLoggingService : IDisposable
{
    private readonly IPluginLog log;
    private readonly Configuration configuration;
    private readonly Dictionary<string, DateTime> lastLogTimes = new();
    private readonly Dictionary<string, int> logCounts = new();
    private readonly TimeSpan throttleInterval = TimeSpan.FromSeconds(5); // Throttle duplicate messages for 5 seconds

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimizedLoggingService"/> class.
    /// </summary>
    /// <param name="log">The plugin log.</param>
    /// <param name="configuration">The plugin configuration.</param>
    public OptimizedLoggingService(IPluginLog log, Configuration configuration)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Logs an information message with throttling for duplicates.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="category">Optional category for grouping related messages.</param>
    public void LogInformation(string message, string? category = null)
    {
        if (ShouldLog(LogLevel.Normal) && ShouldLogMessage(message, category))
        {
            log.Information(FormatMessage(message, category));
        }
    }

    /// <summary>
    /// Logs a debug message with throttling for duplicates.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="category">Optional category for grouping related messages.</param>
    public void LogDebug(string message, string? category = null)
    {
        if (ShouldLog(LogLevel.Verbose) && ShouldLogMessage(message, category))
        {
            log.Debug(FormatMessage(message, category));
        }
    }

    /// <summary>
    /// Logs a warning message with throttling for duplicates.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="category">Optional category for grouping related messages.</param>
    public void LogWarning(string message, string? category = null)
    {
        if (ShouldLog(LogLevel.Normal) && ShouldLogMessage(message, category))
        {
            log.Warning(FormatMessage(message, category));
        }
    }

    /// <summary>
    /// Logs an error message (always logged, regardless of level).
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="category">Optional category for grouping related messages.</param>
    public void LogError(string message, string? category = null)
    {
        // Errors are always logged, but still throttled to prevent spam
        if (ShouldLogMessage(message, category, TimeSpan.FromSeconds(1))) // Shorter throttle for errors
        {
            log.Error(FormatMessage(message, category));
        }
    }

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="category">Optional category for grouping related messages.</param>
    public void LogError(string message, Exception exception, string? category = null)
    {
        var fullMessage = $"{message}: {exception.Message}";
        if (ShouldLogMessage(fullMessage, category, TimeSpan.FromSeconds(1)))
        {
            log.Error(exception, FormatMessage(message, category));
        }
    }

    /// <summary>
    /// Logs a performance metric.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="duration">The duration in milliseconds.</param>
    /// <param name="details">Optional additional details.</param>
    public void LogPerformance(string operation, double duration, string? details = null)
    {
        if (ShouldLog(LogLevel.Verbose))
        {
            var message = $"Performance: {operation} took {duration:F2}ms";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" ({details})";
            }
            
            // Only log if duration is significant or if it's a slow operation
            if (duration > 100 || ShouldLogMessage(message, "Performance"))
            {
                log.Debug(message);
            }
        }
    }

    /// <summary>
    /// Logs a batch summary instead of individual items.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="count">The number of items processed.</param>
    /// <param name="duration">Optional duration in milliseconds.</param>
    public void LogBatchSummary(string operation, int count, double? duration = null)
    {
        if (ShouldLog(LogLevel.Normal) && count > 0)
        {
            var message = $"Batch {operation}: processed {count} items";
            if (duration.HasValue)
            {
                message += $" in {duration.Value:F2}ms";
            }
            
            log.Information(message);
        }
    }

    /// <summary>
    /// Logs coordinate-related operations with smart throttling.
    /// </summary>
    /// <param name="operation">The operation (e.g., "Added", "Removed", "Updated").</param>
    /// <param name="coordinate">The coordinate involved.</param>
    public void LogCoordinateOperation(string operation, TreasureCoordinate coordinate)
    {
        if (ShouldLog(LogLevel.Normal))
        {
            var message = $"{operation} coordinate: {coordinate.MapArea} ({coordinate.X:F1}, {coordinate.Y:F1})";
            if (!string.IsNullOrEmpty(coordinate.PlayerName))
            {
                message += $" from {coordinate.PlayerName}";
            }
            
            // Group coordinate operations to reduce spam
            if (ShouldLogMessage(message, "Coordinates"))
            {
                log.Information(message);
            }
        }
    }

    /// <summary>
    /// Logs route optimization progress.
    /// </summary>
    /// <param name="stage">The optimization stage.</param>
    /// <param name="progress">Progress information.</param>
    public void LogOptimizationProgress(string stage, string progress)
    {
        if (ShouldLog(LogLevel.Normal))
        {
            var message = $"Route optimization - {stage}: {progress}";
            
            // Only log significant optimization milestones
            if (stage.Contains("Started") || stage.Contains("Completed") || stage.Contains("Error"))
            {
                log.Information(message);
            }
            else if (ShouldLog(LogLevel.Verbose))
            {
                log.Debug(message);
            }
        }
    }

    /// <summary>
    /// Clears the throttling cache (useful for testing or when log level changes).
    /// </summary>
    public void ClearThrottleCache()
    {
        lastLogTimes.Clear();
        logCounts.Clear();
    }

    /// <summary>
    /// Gets statistics about throttled messages.
    /// </summary>
    /// <returns>A dictionary of message keys and their throttle counts.</returns>
    public Dictionary<string, int> GetThrottleStatistics()
    {
        return new Dictionary<string, int>(logCounts);
    }

    /// <summary>
    /// Determines if a message should be logged based on configuration and throttling.
    /// </summary>
    private bool ShouldLogMessage(string message, string? category = null, TimeSpan? customThrottle = null)
    {
        var key = category != null ? $"{category}:{message}" : message;
        var now = DateTime.UtcNow;
        var throttle = customThrottle ?? throttleInterval;

        if (lastLogTimes.TryGetValue(key, out var lastTime))
        {
            if (now - lastTime < throttle)
            {
                // Increment throttle count
                logCounts[key] = logCounts.GetValueOrDefault(key, 0) + 1;
                return false;
            }
        }

        // Update last log time
        lastLogTimes[key] = now;
        
        // If this message was throttled before, log the count
        if (logCounts.TryGetValue(key, out var count) && count > 0)
        {
            log.Debug($"Previous message was throttled {count} times");
            logCounts[key] = 0;
        }

        return true;
    }

    /// <summary>
    /// Determines if logging should occur based on the current log level configuration.
    /// </summary>
    private bool ShouldLog(LogLevel requiredLevel)
    {
        return configuration.LogLevel >= requiredLevel;
    }

    /// <summary>
    /// Formats a log message with optional category.
    /// </summary>
    private string FormatMessage(string message, string? category)
    {
        return category != null ? $"[{category}] {message}" : message;
    }

    /// <summary>
    /// Performs cleanup of old throttle entries to prevent memory leaks.
    /// </summary>
    private void CleanupOldEntries()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(10); // Keep entries for 10 minutes
        var keysToRemove = lastLogTimes
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            lastLogTimes.Remove(key);
            logCounts.Remove(key);
        }
    }

    /// <summary>
    /// Disposes the service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        // Log final throttle statistics if there were any throttled messages
        var totalThrottled = logCounts.Values.Sum();
        if (totalThrottled > 0)
        {
            log.Information($"OptimizedLoggingService: Throttled {totalThrottled} duplicate log messages during session");
        }

        lastLogTimes.Clear();
        logCounts.Clear();
    }
}

/// <summary>
/// Extension methods for easier logging with the optimized logging service.
/// </summary>
public static class OptimizedLoggingExtensions
{
    /// <summary>
    /// Logs with automatic performance timing.
    /// </summary>
    /// <param name="loggingService">The logging service.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="action">The action to time and execute.</param>
    public static void LogWithTiming(this OptimizedLoggingService loggingService, string operation, Action action)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            action();
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            loggingService.LogPerformance(operation, duration);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            loggingService.LogError($"Error in {operation}", ex, "Performance");
            throw;
        }
    }

    /// <summary>
    /// Logs with automatic performance timing for async operations.
    /// </summary>
    /// <param name="loggingService">The logging service.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="func">The async function to time and execute.</param>
    public static async System.Threading.Tasks.Task LogWithTimingAsync(this OptimizedLoggingService loggingService, string operation, Func<System.Threading.Tasks.Task> func)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await func();
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            loggingService.LogPerformance(operation, duration);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            loggingService.LogError($"Error in {operation}", ex, "Performance");
            throw;
        }
    }
}
