using RPiRgbLEDMatrix;

namespace LedCube.Games.ArtsyFartsy;

public class ArtsyFartsyRunner
{
    private readonly ArtsyFartsyInstance _instance;
    private readonly int _width;
    private readonly int _height;

    public ArtsyFartsyRunner(ArtsyFartsyInstance instance, int width, int height)
    {
        _instance = instance;
        _width = width;
        _height = height;
    }

    public void Run()
    {
        _instance.Start();
        var matrix =  new RGBLedMatrix(new RGBLedMatrixOptions
        {
            Cols = 64,
            Rows = 64,
            ChainLength = 5,
            HardwareMapping = "adafruit-hat-pwm",
            Brightness = 80,
            GpioSlowdown = 2
        });
        var canvas = matrix.CreateOffscreenCanvas();
        
        while (_instance.IsRunning)
        {
            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    var color = _instance.GetPixel(x, y);
                    canvas.SetPixel(x, y, color);
                }
            }
            matrix.SwapOnVsync(canvas);
        }
    }
}