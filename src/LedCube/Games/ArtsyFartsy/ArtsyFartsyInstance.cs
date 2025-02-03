using Color = RPiRgbLEDMatrix.Color;

namespace LedCube.Games.ArtsyFartsy;

public class ArtsyFartsyInstance
{
    private readonly object _lock = new();
    private readonly Color[,] Pixels = new Color[64 * 5, 64];

    private volatile bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set => _isRunning = value;
    }

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
        Array.Clear(Pixels, 0, Pixels.Length);
    }

    public void PlacePixel(int x, int y, Color color)
    {
        lock (_lock)
        {
            Pixels[x, y] = color;
        }
    }

    public Color GetPixel(int x, int y)
    {
        lock (_lock)
        {
            return Pixels[x, y];
        }
    }
}
