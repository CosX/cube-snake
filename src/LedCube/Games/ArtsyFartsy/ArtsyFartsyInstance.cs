using Color = RPiRgbLEDMatrix.Color;

namespace LedCube.Games.ArtsyFartsy;

public class ArtsyFartsyInstance
{
    private Color[,] Pixels { get; } = new Color[64, 64 * 5];
    public bool IsRunning { get; private set; } 
    public void Start()
    {
        IsRunning = true;
    }
    public void Stop()
    {
        IsRunning = false;
    }
    public void PlacePixel(int x, int y, Color color)
    {
        Pixels[x, y] = color;
    }
    public Color GetPixel(int x, int y)
    {
        return Pixels[x, y];
    }
}