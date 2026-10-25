namespace NetworkingTicTacToe.Server.Domain.Match;

/// <summary>
/// Specific error codes for rejected move attempts.
/// </summary>
public enum MoveErrorCode
{
    None = 0,
    GameNotInProgress = 1,
    NotParticipant = 2,
    OutOfTurn = 3,
    OutOfBounds = 4,
    CellOccupied = 5
}
