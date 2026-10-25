namespace NetworkingTicTacToe.Server.DTOs;

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
