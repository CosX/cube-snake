namespace Cube.Contracts;

public record GameStarted;
public record StatusTicked(int Score, int StepsLeft);
public record GameEnded(int Score);
public record MultiPlayerGameEnded(string WinningColor);
public record ArtsyFartsyPixel(int X, int Y, int R, int G, int B);