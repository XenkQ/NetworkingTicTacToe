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

/// <summary>
/// Response payload returned when a client invokes JoinGame RPC.
/// </summary>
public sealed record JoinGameResponseDto(
    bool IsJoined,
    string? AssignedSymbol,
    string? RejectionReason,
    GameStateDto GameState);

/// <summary>
/// Move request payload dispatched by a client when clicking a board cell.
/// </summary>
public sealed record MakeMoveRequestDto(int Row, int Col);

/// <summary>
/// Result payload returned and broadcast when a move attempt is evaluated.
/// </summary>
public sealed record MoveResultDto(
    bool IsAccepted,
    string ErrorCode,
    string? ErrorMessage,
    int Row,
    int Col,
    string? Symbol,
    GameStateDto GameState);

/// <summary>
/// Notification payload sent to an individual player when their assigned symbol changes.
/// </summary>
public sealed record PlayerAssignmentDto(
    string AssignedSymbol,
    GameStateDto GameState);

/// <summary>
/// Response payload returned when a client invokes RequestRematch RPC.
/// </summary>
public sealed record RematchResponseDto(
    bool IsAccepted,
    string? ErrorMessage,
    bool DidRematchStart,
    GameStateDto GameState);
