using DoomSharp.Core;
using DoomSharp.Core.Graphics;
using DoomSharp.Core.Input;
using RPiRgbLEDMatrix;

namespace LedCube.Games.Doom;

public class Graphics : IGraphics
{
    public static readonly Graphics Instance = new();

    public Graphics()
    {
        _stride = (_rectangle.Width * 8 /* bpp */ + 7) / 8;
        _screenBuffer = new byte[_rectangle.Height * _stride];
    }

    private string _title = "DooM#";
    private Color[]? _output;
    private byte[]? _newPalette;
    private readonly Int32Rect _rectangle = new(0, 0, Constants.ScreenWidth, Constants.ScreenHeight);
    private readonly int _stride;
    private readonly byte[] _screenBuffer;
    private readonly Queue<InputEvent> _events = new();
    private RGBLedMatrix _matrix;
    private RGBLedCanvas _canvas;

    public void Initialize()
    {
        _matrix = new RGBLedMatrix(new RGBLedMatrixOptions
        {
            Rows = 64,
            Cols = 64,
            GpioSlowdown = 2,
            ChainLength = 1,
            HardwareMapping = "adafruit-hat-pwm",
            Brightness = 80
        });
        _canvas = _matrix.CreateOffscreenCanvas();
    }

    public void UpdatePalette(byte[] palette)
    {
        _newPalette = palette;
    }

    public void ScreenReady(byte[] output)
    {
        Array.Copy(output, 0, _screenBuffer, 0, output.Length);

        if (_newPalette is not null)
        {
            _newPalette = null;
        }

        try
        {
            UpdateScreenBufferToMatrix();
        }
        catch (TaskCanceledException) { }
    }

    private void UpdateScreenBufferToMatrix()
    {
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                // Calculate the corresponding position in the original screen buffer
                var originalX = x * Constants.ScreenWidth / 64;
                var originalY = y * Constants.ScreenHeight / 64;
                var index = originalY * Constants.ScreenWidth + originalX;
                
                var colorIndex = _screenBuffer[index];
                var r = _newPalette[colorIndex * 3];
                var g = _newPalette[colorIndex * 3 + 1];
                var b = _newPalette[colorIndex * 3 + 2];
                _canvas.SetPixel(x, y, new Color(r, g, b));
            }
        }

        _matrix.SwapOnVsync(_canvas);
    }

    public void StartTic()
    {
        while (_events.TryDequeue(out var ev)) // has events
        {
            DoomGame.Instance.PostEvent(ev);
        }
    }

    public void AddEvent(InputEvent ev)
    {
        _events.Enqueue(ev);
    }
}
