namespace NetworkingTicTacToe.Server.Domain.Results;

using NetworkingTicTacToe.Server.Domain.Match;

/// <summary>
/// Encapsulates the result of a client attempting to join the match.
/// </summary>
public sealed record JoinMatchResult(
    bool IsJoined,
    PlayerSymbol? AssignedSymbol,
    string? RejectionReason,
    bool DidMatchStart);
