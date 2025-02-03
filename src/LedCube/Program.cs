using Cube.Contracts;
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
            x.AddConsumer<ArtsyFartsyPixelConsumer>();
            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(Environment.GetEnvironmentVariable("AzureServiceBusConnectionString"));

                cfg.SubscriptionEndpoint<ArtsyFartsyPixel>("artsy-fartsy-pixel", e =>
                {
                    e.ConfigureConsumer<ArtsyFartsyPixelConsumer>(context);
                    e.ConfigureDeadLetterQueueDeadLetterTransport();
                    e.ConfigureDeadLetterQueueErrorTransport();
                });
            });
        });
        services.AddHostedService<GameHostedService>();
        services.AddSingleton<CubeContext>();
        services.AddSingleton<MediaHandler>();
        services.AddSingleton<ArtsyFartsyInstance>();
        services.AddSingleton<ArtsyFartsyRunner>();
    })
    .Build();

host.Run();
