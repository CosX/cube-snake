using MassTransit;
using RPiRgbLEDMatrix;

namespace LedCube.Games.ArtsyFartsy;

public class PlaceMyArtsyFartsyPixel(ArtsyFartsyInstance instance) : IConsumer<Cube.Contracts.ArtsyFartsyPixel>
{
    public async Task Consume(ConsumeContext<Cube.Contracts.ArtsyFartsyPixel> context)
    {
        if (instance.IsRunning)
        {
            var pixel = context.Message;
            instance.PlacePixel(pixel.X, pixel.Y, new Color(pixel.R, pixel.G, pixel.B));
        }
    }
}