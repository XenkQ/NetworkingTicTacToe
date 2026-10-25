namespace NetworkingTicTacToe.Server.Domain.Results;

using NetworkingTicTacToe.Server.Domain.Board;
using NetworkingTicTacToe.Server.Domain.Match;

/// <summary>
/// Encapsulates the deterministic outcome of an attempted board move.
/// </summary>
public sealed record MoveAttemptResult(
    bool IsValid,
    MoveErrorCode ErrorCode,
    string? ErrorMessage,
    PlayerSymbol? MovedSymbol,
    BoardCoordinate? Coordinate,
    GameStatus ResultingStatus,
    PlayerSymbol? Winner,
    WinningLine? WinningLine);
