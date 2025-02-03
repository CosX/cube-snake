using MassTransit;
using Microsoft.Extensions.Logging;
using Cube.Contracts;
using RPiRgbLEDMatrix;

namespace LedCube.Games.ArtsyFartsy;

public class ArtsyFartsyRunner(ArtsyFartsyInstance instance, IBus bus, ILogger<ArtsyFartsyRunner> logger)
{
    private CancellationTokenSource _cancellationTokenSource;
    private RGBLedMatrix? _matrix;

    public async Task Run()
    {
        logger.LogInformation("Starting ArtsyFartsyRunner");
        instance.Start();
        _cancellationTokenSource = new CancellationTokenSource();

        await bus.Publish(new CreateArtsyFartsyCanvas());

        _matrix = new RGBLedMatrix(new RGBLedMatrixOptions
        {
            Cols = 64,
            Rows = 64,
            ChainLength = 5,
            HardwareMapping = "adafruit-hat-pwm",
            Brightness = 80,
            GpioSlowdown = 2
        });
        var canvas = _matrix.CreateOffscreenCanvas();

        try
        {
            while (instance.IsRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                for (var x = 0; x < 5 * 64; x++)
                {
                    for (var y = 0; y < 64; y++)
                    {
                        var color = instance.GetPixel(x, y);
                        canvas.SetPixel(x, y, color);
                    }
                }
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
                await Task.Run(() => _matrix!.SwapOnVsync(canvas), _cancellationTokenSource.Token);
                await Task.Delay(1, _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Loop cancelled.");
        }
        finally
        {
            canvas.Clear();
            _matrix.Dispose();
            logger.LogInformation("Finished cleanup in finally block.");
        }
    }

    public async Task Stop()
    {
        logger.LogInformation("Stopping ArtsyFartsyRunner");
        await bus.Publish(new EndArtsyFartsyCanvas());
        instance.Stop();
        _cancellationTokenSource?.Cancel();
    }
}