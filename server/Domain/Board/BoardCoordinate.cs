namespace NetworkingTicTacToe.Server.Domain.Board;

/// <summary>
/// Represents zero-based row and column coordinates on the 3x3 board.
/// </summary>
public readonly record struct BoardCoordinate(int Row, int Col);
