namespace NetworkingTicTacToe.Server.Domain.Match;

/// <summary>
/// Represents the overall lifecycle status of a Tic-Tac-Toe match.
/// </summary>
public enum GameStatus
{
    WaitingForPlayers = 0,
    InProgress = 1,
    Won = 2,
    Draw = 3,
    AbortedByDisconnect = 4
}
