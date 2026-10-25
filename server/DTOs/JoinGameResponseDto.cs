namespace NetworkingTicTacToe.Server.DTOs;

/// <summary>
/// Response payload returned when a client invokes JoinGame RPC.
/// </summary>
public sealed record JoinGameResponseDto(
    bool IsJoined,
    string? AssignedSymbol,
    string? RejectionReason,
    GameStateDto GameState);
