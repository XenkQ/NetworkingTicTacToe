namespace NetworkingTicTacToe.Server.DTOs;

/// <summary>
/// Response payload returned when a client invokes RequestRematch RPC.
/// </summary>
public sealed record RematchResponseDto(
    bool IsAccepted,
    string? ErrorMessage,
    bool DidRematchStart,
    GameStateDto GameState);
