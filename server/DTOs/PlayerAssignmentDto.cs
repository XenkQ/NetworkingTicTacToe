namespace NetworkingTicTacToe.Server.DTOs;

/// <summary>
/// Notification payload sent to an individual player when their assigned symbol changes.
/// </summary>
public sealed record PlayerAssignmentDto(
    string AssignedSymbol,
    GameStateDto GameState);
