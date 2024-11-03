using RPiRgbLEDMatrix;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = RPiRgbLEDMatrix.Color;

namespace LedCube;

public class MediaHandler(CancellationTokenSource cancellationTokenSource)
{
    private bool IsPlayingGame;
    private readonly ScreenSaver _defaultScreenSaver = new("martin_snake_5_sides.gif", 80, 5);
    
    private readonly IEnumerable<ScreenSaver> _defaultScreenSavers =
    [
        new ScreenSaver("this-is-fine.gif", 30),
        new ScreenSaver("circle.gif", 50),
        new ScreenSaver("illusioncolor.gif", 30),
        new ScreenSaver("star-wars.gif", 40),
        new ScreenSaver("outline.gif", 100)
    ];
    
    private readonly IEnumerable<ScreenSaver> _christmasScreenSavers =
    [
        new ScreenSaver("campfire.gif", 50),
        new ScreenSaver("grinch.gif", 30),
        new ScreenSaver("merry-christmas.gif", 80),
        new ScreenSaver("parrot.gif", 60),
        new ScreenSaver("santa.gif", 30)
    ];

    private IEnumerable<ScreenSaver> GetTimeAppropriateScreenSavers()
    {
        var now = DateTime.Now;

        return now.Month > 10 ? _christmasScreenSavers : _defaultScreenSavers;
    }

    public async Task StartCycle()
    {
        var displayDefault = true;
        IsPlayingGame = false;
        do
        {
            var rnd = new Random();
            var screensavers = GetTimeAppropriateScreenSavers().ToList();
            var command = displayDefault ? _defaultScreenSaver : screensavers.ElementAt(rnd.Next(0, screensavers.Count));
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(30_000);
            try {
                await PlayGif(command.Image, command.Brightness, command.Chain, true, cancellationTokenSource.Token);
            } catch (OperationCanceledException){
            }

            displayDefault = !displayDefault;
        } while (!IsPlayingGame);
    }

    public async Task PlayGif(string path, int brightness, int chain, bool loop, CancellationToken cancellationToken)
    {
        using var matrix = new RGBLedMatrix(new RGBLedMatrixOptions
        {
            Rows = 64,
            Cols = 64,
            GpioSlowdown = 2,
            ChainLength = chain,
            HardwareMapping = "adafruit-hat-pwm",
            Brightness = brightness
        });
        var canvas = matrix.CreateOffscreenCanvas();

        Configuration.Default.PreferContiguousImageBuffers = true;
        using var image = Image.Load<Rgb24>("images/" + path);
        image.Mutate(o => o.Resize(canvas.Width, canvas.Height));

        var frames = image.Frames
            .Select(f => (
                Pixels: f.DangerousTryGetSinglePixelMemory(out var memory)
                    ? memory.ToArray()
                    : throw new Exception("Could not get pixel buffer"),
                Delay: f.Metadata.GetGifMetadata().FrameDelay * 10
            )).ToArray();

        var frame = -1;

        while (!cancellationToken.IsCancellationRequested || (!loop && frame < frames.Length - 1))
        {
            frame = (frame + 1) % frames.Length;
            var data = frames[frame].Pixels.Select(p => new Color(p.R, p.G, p.B)).ToArray();
            canvas.SetPixels(0, 0, canvas.Width, canvas.Height, data);

            await Task.Run(() => matrix.SwapOnVsync(canvas), cancellationToken);
            await Task.Delay(frames[frame].Delay, cancellationToken);
        }
    }

    public void Dispose()
    {
        IsPlayingGame = true;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }
}

public record ScreenSaver(string Image, int Brightness = 80, int Chain = 1);