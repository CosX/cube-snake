using LedCube.Games.Achtung;
using MassTransit;
using RPiRgbLEDMatrix;
using Microsoft.Extensions.Hosting;
using LedCube.Games.Shared;
using LedCube.Games.Snake;
using Cube.Contracts;
using DoomSharp.Core;
using DoomSharp.Core.Data;
using LedCube.Games.ArtsyFartsy;
using LedCube.Games.Doom;
using Microsoft.Extensions.Logging;
using SnakeGameContext = LedCube.Games.Snake.GameContext;
using GameObject = LedCube.Games.Snake.GameObject;
using Color = RPiRgbLEDMatrix.Color;

namespace LedCube;

public class GameHostedService(
    IBus bus,
    MediaHandler mediaHandler,
    ILogger<LoggerConsole> logger,
    ArtsyFartsyRunner artsyFartsyRunner) : IHostedService
{
    private readonly SnakeGame _snakeGame = new();
    private SnakeGameContext _snakeGameCtx;
    private readonly AchtungGame _achtungGame = new();
    private AchtungGameContext _achtungGameCtx;
    private readonly CubeContext _cubeCtx = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ConfigureGamepads(cancellationToken);
        Task.Run(mediaHandler.StartCycle, cancellationToken);
        WadFileCollection.Init(new WadStreamProvider());
        DoomGame.SetOutputRenderer(Graphics.Instance);
        DoomGame.SetConsole(new LoggerConsole(logger));
        return Task.CompletedTask;
    }

    private void ConfigureGamepads(CancellationToken cancellationToken)
    {
        foreach (var player in _cubeCtx.Players)
        {
            player.Gamepad.AxisChanged += (_, e) =>
            {
                if (e.Axis is not (0 or 2 or 6)) return;
                switch (e.Value)
                {
                    case 32767:
                        player.IsTurningLeft = true;
                        return;
                    case -32767:
                        player.IsTurningRight = true;
                        return;
                    default:
                        player.IsTurningRight = player.IsTurningLeft = false;
                        break;
                }
            };

            player.Gamepad.ButtonChanged += (_, e) =>
            {
                switch (e.Button)
                {
                    case 11 when e.Pressed && _cubeCtx.State == State.Idle:
                        Task.Run(() => PlaySnake(player.Id), cancellationToken);
                        break;
                    case 10 when e.Pressed && _cubeCtx.State == State.Idle:
                        Task.Run(PlayAchtung, cancellationToken);
                        break;
                    case 9 when e.Pressed && _cubeCtx.State == State.Idle:
                        Task.Run(StartArtsyFartsy, cancellationToken);
                        break;
                    case 8 when e.Pressed && _cubeCtx.State == State.PlayingArtsyFartsy:
                        Task.Run(StopArtsyFartsy, cancellationToken);
                        break;
                }
            };
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        mediaHandler.Dispose();
        foreach (var player in _cubeCtx.Players)
        {
            player.Gamepad.Dispose();
        }
        return Task.CompletedTask;
    }

    private async Task PlaySnake(int playerId)
    {
        _cubeCtx.State = State.Playing;
        mediaHandler.Dispose();
        await Task.Delay(100);
        Task.Run(() => bus.Publish(new GameStarted()));
        await mediaHandler.PlayGif("countdown.gif", 40, 1, false, 10, CancellationToken.None);

        var matrix = CreateRgbLedMatrix();

        var canvas = matrix.CreateOffscreenCanvas();
        _snakeGameCtx = _snakeGame.CreateGameContext();

        Task.Run(RunStatusTick);
        do
        {
            for (var x = 0; x < _snakeGameCtx.Map.GetLength(0); x++)
            {
                for (var y = 0; y < _snakeGameCtx.Map.GetLength(1); y++)
                {
                    var (obj, _) = _snakeGameCtx.Map[x, y];
                    switch (obj)
                    {
                        case GameObject.Ground:
                            canvas.SetPixel(x, y, new Color(0, 0, 0));
                            break;
                        case GameObject.Snake:
                            canvas.SetPixel(x, y, new Color(0, 255, 0));
                            break;
                        case GameObject.Food:
                            canvas.SetPixel(x, y, new Color(255, 0, 0));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            await Task.Delay(20);
            var player = _cubeCtx.GetActivePlayer(playerId);
            if (player.IsTurningLeft)
            {
                _snakeGameCtx = _snakeGame.Loop(_snakeGameCtx with { Direction = GetDirection.TurnLeft(_snakeGameCtx.Direction) });
                player.IsTurningLeft = false;
            }
            else if (player.IsTurningRight)
            {
                _snakeGameCtx = _snakeGame.Loop(_snakeGameCtx with { Direction = GetDirection.TurnRight(_snakeGameCtx.Direction) });
                player.IsTurningRight = false;
            }
            else
            {
                _snakeGameCtx = _snakeGame.Loop(_snakeGameCtx);
            }
            matrix.SwapOnVsync(canvas);
        } while (!_snakeGameCtx.Dead);

        canvas.Clear();
        matrix.Dispose();

        Task.Run(() => bus.Publish(new GameEnded(_snakeGameCtx.Score)));

        await mediaHandler.PlayGif("gameover.gif", 40, 1, false, 2, CancellationToken.None);

        _cubeCtx.State = State.Idle;
        Task.Run(mediaHandler.StartCycle);
    }

    private async Task PlayAchtung()
    {
        _cubeCtx.State = State.Playing;
        mediaHandler.Dispose();
        await Task.Delay(100);
        await mediaHandler.PlayGif("countdown.gif", 40, 1, false, 10, CancellationToken.None);

        var matrix = CreateRgbLedMatrix();

        var canvas = matrix.CreateOffscreenCanvas();
        _achtungGameCtx = _achtungGame.CreateGameContext();

        do
        {
            for (var x = 0; x < _achtungGameCtx.Map.GetLength(0); x++)
            {
                for (var y = 0; y < _achtungGameCtx.Map.GetLength(1); y++)
                {
                    var obj = _achtungGameCtx.Map[x, y];
                    switch (obj)
                    {
                        case LedCube.Games.Achtung.GameObject.Ground:
                            canvas.SetPixel(x, y, new Color(0, 0, 0));
                            break;
                        case LedCube.Games.Achtung.GameObject.BlueDot:
                            canvas.SetPixel(x, y, new Color(0, 0, 255));
                            break;
                        case LedCube.Games.Achtung.GameObject.RedDot:
                            canvas.SetPixel(x, y, new Color(255, 0, 0));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            await Task.Delay(30);
            foreach (var player in _achtungGameCtx.Players)
            {
                var gamepad = _cubeCtx.GetActivePlayer(player.Id);
                if (gamepad.IsTurningLeft)
                {
                    player.Direction = GetDirection.TurnLeft(player.Direction);
                    gamepad.IsTurningLeft = false;
                }
                else if (gamepad.IsTurningRight)
                {
                    player.Direction = GetDirection.TurnRight(player.Direction);
                    gamepad.IsTurningRight = false;
                }
            }

            _achtungGameCtx = _achtungGame.Loop(_achtungGameCtx);
            matrix.SwapOnVsync(canvas);
        } while (!_achtungGameCtx.Players.Any(p => p.Dead));

        var winner = _achtungGameCtx.Players.First(p => !p.Dead);
        Task.Run(() => bus.Publish(new MultiPlayerGameEnded(
            winner.Color == LedCube.Games.Achtung.GameObject.RedDot ? "red" : "blue")));
        for (var x = 0; x < _achtungGameCtx.Map.GetLength(0); x++)
        {
            for (var y = 0; y < _achtungGameCtx.Map.GetLength(1); y++)
            {
                canvas.SetPixel(x, y,
                    winner.Color == LedCube.Games.Achtung.GameObject.RedDot
                        ? new Color(255, 0, 0)
                        : new Color(0, 0, 255));
            }
            await Task.Delay(5);
            matrix.SwapOnVsync(canvas);
        }

        canvas.Clear();
        matrix.Dispose();

        _cubeCtx.State = State.Idle;
        Task.Run(mediaHandler.StartCycle);
    }

    private async Task StartArtsyFartsy()
    {
        _cubeCtx.State = State.PlayingArtsyFartsy;
        mediaHandler.Dispose();
        await Task.Delay(100);
        await mediaHandler.PlayGif("bobross.gif", 40, 1, false, 3, CancellationToken.None);
        Task.Run(artsyFartsyRunner.Run);
    }

    private async Task StopArtsyFartsy()
    {
        await artsyFartsyRunner.Stop();
        _cubeCtx.State = State.Idle;
        await Task.Delay(100);
        Task.Run(mediaHandler.StartCycle);
    }

    private async Task RunDoom()
    {
        _cubeCtx.State = State.Playing;
        mediaHandler.Dispose();
        await Task.Delay(100);
        await Task.Run(() => DoomGame.Instance.RunAsync(GameMode.Shareware, "/home/dietpi/wads/doom1.wad"));
    }

    private async Task RunStatusTick()
    {
        do
        {
            await bus.Publish(new StatusTicked(_snakeGameCtx.Score, _snakeGameCtx.StepsLeft));
            await Task.Delay(1000);
        } while (!_snakeGameCtx.Dead);
    }

    private static RGBLedMatrix CreateRgbLedMatrix()
    {
        return new RGBLedMatrix(new RGBLedMatrixOptions
        {
            Cols = 64,
            Rows = 64,
            ChainLength = 5,
            HardwareMapping = "adafruit-hat-pwm",
            Brightness = 80,
            GpioSlowdown = 2,
        });
    }
}
