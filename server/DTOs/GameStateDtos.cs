namespace NetworkingTicTacToe.Server.DTOs;

/// <summary>
/// Coordinate pair for a cell on the 3x3 board.
/// </summary>
public sealed record BoardCellCoordinateDto(int Row, int Col);

/// <summary>
/// Winning line coordinates containing the 3 winning cell positions.
/// </summary>
public sealed record WinningLineDto(BoardCellCoordinateDto[] Cells);

/// <summary>
/// Authoritative snapshot of the match state serialized to connected clients.
/// </summary>
public sealed record GameStateDto(
    long StateVersion,
    string[][] Cells,
    string Status,
    string? CurrentTurn,
    string? Winner,
    WinningLineDto? WinningLine,
    bool IsPlayerXConnected,
    bool IsPlayerOConnected,
    bool HasPlayerXAcceptedRematch,
    bool HasPlayerOAcceptedRematch,
    string StatusMessage);
