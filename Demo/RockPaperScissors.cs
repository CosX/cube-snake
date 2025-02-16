using RPiRgbLEDMatrix;

namespace Demo;

public class RockPaperScissors
{
    private const int Width = 64;
    private const int Height = 64;
    private const int ChainLength = 5;
    private const int TotalChainLength = Width * ChainLength;
    private readonly int[,] grid;
    private readonly Random random = new();
    private readonly RGBLedMatrix matrix;
    
    public RockPaperScissors()
    {
        matrix = new RGBLedMatrix(new RGBLedMatrixOptions
        {
            Cols = Width,
            Rows = Height,
            ChainLength = ChainLength,
            HardwareMapping = "adafruit-hat-pwm",
            Brightness = 80,
            GpioSlowdown = 2
        });
        grid = new int[TotalChainLength, Height];
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (var x = 0; x < TotalChainLength; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                grid[x, y] = random.Next(3); // 0 = Rock, 1 = Paper, 2 = Scissors
            }
        }
    }

    private int GetDominantState(int x, int y)
    {
        var counts = new int[3]; // 0: Rock, 1: Paper, 2: Scissors

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                var nx = (x + dx + TotalChainLength) % TotalChainLength;
                var ny = (y + dy + Height) % Height;

                counts[grid[nx, ny]]++;
            }
        }

        var current = grid[x, y];

        return current switch
        {
            0 when counts[1] > counts[2] => 1,
            1 when counts[2] > counts[0] => 2,
            2 when counts[0] > counts[1] => 0,
            _ => current
        };
    }

    private void UpdateGrid()
    {
        var newGrid = new int[TotalChainLength, Height];

        for (var x = 0; x < TotalChainLength; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                newGrid[x, y] = GetDominantState(x, y);
            }
        }

        Array.Copy(newGrid, grid, Width * Height);
    }

    private void Render()
    {
        var canvas = matrix.CreateOffscreenCanvas();
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var state = grid[x, y];
                switch (state)
                {
                    case 0: canvas.SetPixel(x, y, new Color(255, 0, 0)); break;   // Rock (Red)
                    case 1: canvas.SetPixel(x, y,  new Color(0, 255, 0)); break;   // Paper (Green)
                    case 2: canvas.SetPixel(x, y,  new Color(0, 0, 255)); break;   // Scissors (Blue)
                }
            }
        }
        matrix.SwapOnVsync(canvas);
    }

    public void Run()
    {
        while (true)
        {
            UpdateGrid();
            Render();
            Thread.Sleep(100);
        }
    }
}