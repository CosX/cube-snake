using MassTransit;
using Microsoft.Extensions.Logging;
using RPiRgbLEDMatrix;

namespace LedCube.Games.ArtsyFartsy;

public class ArtsyFartsyPixelConsumer(ArtsyFartsyInstance instance, ILogger<ArtsyFartsyPixelConsumer> logger) : IConsumer<Cube.Contracts.ArtsyFartsyPixel>
{
    public async Task Consume(ConsumeContext<Cube.Contracts.ArtsyFartsyPixel> context)
    {
        if (instance.IsRunning)
        {
            var pixel = context.Message;
            logger.LogInformation("Place pixel at {X}, {Y} with color {R}, {G}, {B}", pixel.X, pixel.Y, pixel.R, pixel.G, pixel.B);
            instance.PlacePixel(pixel.X, pixel.Y, new Color(pixel.R, pixel.G, pixel.B));
        }
    }
}