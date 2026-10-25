namespace NetworkingTicTacToe.Server.Domain.Results;

/// <summary>
/// Encapsulates the result of a participant voting to accept a rematch.
/// </summary>
public sealed record RematchVoteResult(
    bool IsAccepted,
    string? ErrorMessage,
    bool DidRematchStart);
