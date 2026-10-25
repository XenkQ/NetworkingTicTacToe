namespace NetworkingTicTacToe.Server.Domain.Board;

/// <summary>
/// Encapsulates the three board coordinates that formed a winning line.
/// </summary>
public sealed record WinningLine(
    BoardCoordinate First,
    BoardCoordinate Second,
    BoardCoordinate Third);
