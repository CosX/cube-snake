using DoomSharp.Core;
using Microsoft.Extensions.Logging;

namespace LedCube.Games.Doom;

public class LoggerConsole : IConsole
{
    private readonly ILogger<LoggerConsole> _logger;

    public LoggerConsole(ILogger<LoggerConsole> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Write(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogDebug("Write called with an empty or null message.");
            return;
        }

        _logger.LogInformation(message);
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("SetTitle called with an empty or null title.");
            return;
        }

        _logger.LogInformation("Setting console title to: {Title}", title);

        try
        {
            Console.Title = title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set console title.");
        }
    }

    public void Shutdown()
    {
        _logger.LogInformation("Console is shutting down.");
    }
}