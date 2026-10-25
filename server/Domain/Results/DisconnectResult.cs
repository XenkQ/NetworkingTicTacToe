namespace NetworkingTicTacToe.Server.Domain.Results;

using NetworkingTicTacToe.Server.Domain.Match;

/// <summary>
/// Encapsulates the result of cleaning up a client disconnection.
/// </summary>
public sealed record DisconnectResult(
    bool WasParticipant,
    string? RemainingPlayerConnectionId,
    PlayerSymbol? RemainingPlayerNewSymbol);
