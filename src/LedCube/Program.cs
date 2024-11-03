using LedCube;
using LedCube.Games.ArtsyFartsy;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<PlaceMyArtsyFartsyPixel>();
            x.UsingAzureServiceBus((context,cfg) =>
            {
                cfg.Host("");
                
                cfg.SubscriptionEndpoint<PlaceMyArtsyFartsyPixel>("game-started", e =>
                {
                    e.ConfigureConsumer<PlaceMyArtsyFartsyPixel>(context);
                    e.ConfigureDeadLetterQueueDeadLetterTransport();
                    e.ConfigureDeadLetterQueueErrorTransport();
                });
            });
        });
        services.AddHostedService<GameHostedService>();
        services.AddSingleton<CubeContext>();
        services.AddSingleton<MediaHandler>();
    })
    .Build();

host.Run();
