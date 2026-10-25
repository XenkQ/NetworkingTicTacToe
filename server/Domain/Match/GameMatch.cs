namespace NetworkingTicTacToe.Server.Domain.Match;

using NetworkingTicTacToe.Server.Domain.Board;
using NetworkingTicTacToe.Server.Domain.Results;

/// <summary>
/// Manages the domain rules, player seating, turn flow, and outcome evaluation for a Tic-Tac-Toe match.
/// </summary>
public sealed class GameMatch
{
    public GameMatch()
    {
        Board = new Board();
        Status = GameStatus.WaitingForPlayers;
    }

    public Board Board { get; }

    public string? PlayerXConnectionId { get; private set; }

    public string? PlayerOConnectionId { get; private set; }

    public PlayerSymbol? CurrentTurn { get; private set; }

    public GameStatus Status { get; private set; }

    public PlayerSymbol? Winner { get; private set; }

    public WinningLine? WinningLine { get; private set; }

    public bool HasPlayerXAcceptedRematch { get; private set; }

    public bool HasPlayerOAcceptedRematch { get; private set; }

    public PlayerSymbol? GetPlayerSymbol(string connectionId)
    {
        if (connectionId == PlayerXConnectionId)
        {
            return PlayerSymbol.X;
        }

        if (connectionId == PlayerOConnectionId)
        {
            return PlayerSymbol.O;
        }

        return null;
    }

    public bool IsParticipant(string connectionId)
    {
        return connectionId == PlayerXConnectionId || connectionId == PlayerOConnectionId;
    }

    public JoinMatchResult TryAddPlayer(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        if (connectionId == PlayerXConnectionId)
        {
            return new JoinMatchResult(true, PlayerSymbol.X, null, Status == GameStatus.InProgress);
        }

        if (connectionId == PlayerOConnectionId)
        {
            return new JoinMatchResult(true, PlayerSymbol.O, null, Status == GameStatus.InProgress);
        }

        if (PlayerXConnectionId is null)
        {
            PlayerXConnectionId = connectionId;
            Status = GameStatus.WaitingForPlayers;
            CurrentTurn = null;
            Winner = null;
            WinningLine = null;
            HasPlayerXAcceptedRematch = false;
            HasPlayerOAcceptedRematch = false;
            Board.Reset();

            return new JoinMatchResult(true, PlayerSymbol.X, null, false);
        }

        if (PlayerOConnectionId is null)
        {
            PlayerOConnectionId = connectionId;
            Board.Reset();
            Status = GameStatus.InProgress;
            CurrentTurn = PlayerSymbol.X;
            Winner = null;
            WinningLine = null;
            HasPlayerXAcceptedRematch = false;
            HasPlayerOAcceptedRematch = false;

            return new JoinMatchResult(true, PlayerSymbol.O, null, true);
        }

        return new JoinMatchResult(false, null, "Game is currently full (2/2 players active).", false);
    }

    public MoveAttemptResult TryMakeMove(string connectionId, int row, int col)
    {
        PlayerSymbol? callerSymbol = GetPlayerSymbol(connectionId);
        if (callerSymbol is null)
        {
            return new MoveAttemptResult(
                false,
                MoveErrorCode.NotParticipant,
                "Caller is not a registered player in this match.",
                null,
                new BoardCoordinate(row, col),
                Status,
                Winner,
                WinningLine);
        }

        if (Status != GameStatus.InProgress)
        {
            return new MoveAttemptResult(
                false,
                MoveErrorCode.GameNotInProgress,
                "Move rejected: game is not in progress.",
                callerSymbol,
                new BoardCoordinate(row, col),
                Status,
                Winner,
                WinningLine);
        }

        if (CurrentTurn != callerSymbol)
        {
            return new MoveAttemptResult(
                false,
                MoveErrorCode.OutOfTurn,
                "Move rejected: not your turn.",
                callerSymbol,
                new BoardCoordinate(row, col),
                Status,
                Winner,
                WinningLine);
        }

        if (!Board.IsWithinBounds(row, col))
        {
            return new MoveAttemptResult(
                false,
                MoveErrorCode.OutOfBounds,
                $"Move rejected: coordinate ({row}, {col}) is out of bounds.",
                callerSymbol,
                new BoardCoordinate(row, col),
                Status,
                Winner,
                WinningLine);
        }

        if (!Board.IsCellEmpty(row, col))
        {
            return new MoveAttemptResult(
                false,
                MoveErrorCode.CellOccupied,
                $"Move rejected: cell ({row}, {col}) is already occupied.",
                callerSymbol,
                new BoardCoordinate(row, col),
                Status,
                Winner,
                WinningLine);
        }

        Board.PlaceSymbol(row, col, callerSymbol.Value);
        WinningLine? detectedLine = Board.EvaluateWin(callerSymbol.Value);

        if (detectedLine is not null)
        {
            Status = GameStatus.Won;
            Winner = callerSymbol;
            WinningLine = detectedLine;
            CurrentTurn = null;

            return new MoveAttemptResult(
                true,
                MoveErrorCode.None,
                null,
                callerSymbol,
                new BoardCoordinate(row, col),
                Status,
                Winner,
                WinningLine);
        }

        if (Board.IsFull())
        {
            Status = GameStatus.Draw;
            Winner = null;
            WinningLine = null;
            CurrentTurn = null;

            return new MoveAttemptResult(
                true,
                MoveErrorCode.None,
                null,
                callerSymbol,
                new BoardCoordinate(row, col),
                Status,
                null,
                null);
        }

        CurrentTurn = callerSymbol == PlayerSymbol.X ? PlayerSymbol.O : PlayerSymbol.X;

        return new MoveAttemptResult(
            true,
            MoveErrorCode.None,
            null,
            callerSymbol,
            new BoardCoordinate(row, col),
            Status,
            null,
            null);
    }

    public RematchVoteResult TryAcceptRematch(string connectionId)
    {
        if (connectionId != PlayerXConnectionId && connectionId != PlayerOConnectionId)
        {
            return new RematchVoteResult(false, "Caller is not a registered player in this match.", false);
        }

        if (Status != GameStatus.Won && Status != GameStatus.Draw)
        {
            return new RematchVoteResult(false, "Cannot accept rematch: match is not in a finished state.", false);
        }

        if (connectionId == PlayerXConnectionId)
        {
            HasPlayerXAcceptedRematch = true;
        }
        else if (connectionId == PlayerOConnectionId)
        {
            HasPlayerOAcceptedRematch = true;
        }

        if (HasPlayerXAcceptedRematch && HasPlayerOAcceptedRematch)
        {
            string? previousPlayerX = PlayerXConnectionId;
            PlayerXConnectionId = PlayerOConnectionId;
            PlayerOConnectionId = previousPlayerX;

            Board.Reset();
            Status = GameStatus.InProgress;
            CurrentTurn = PlayerSymbol.X;
            Winner = null;
            WinningLine = null;
            HasPlayerXAcceptedRematch = false;
            HasPlayerOAcceptedRematch = false;

            return new RematchVoteResult(true, null, true);
        }

        return new RematchVoteResult(true, null, false);
    }

    public DisconnectResult HandleDisconnect(string connectionId)
    {
        if (connectionId != PlayerXConnectionId && connectionId != PlayerOConnectionId)
        {
            return new DisconnectResult(false, null, null);
        }

        string? remainingPlayerConnectionId = connectionId == PlayerXConnectionId
            ? PlayerOConnectionId
            : PlayerXConnectionId;

        Board.Reset();
        HasPlayerXAcceptedRematch = false;
        HasPlayerOAcceptedRematch = false;
        CurrentTurn = null;
        Winner = null;
        WinningLine = null;

        if (remainingPlayerConnectionId is not null)
        {
            PlayerXConnectionId = remainingPlayerConnectionId;
            PlayerOConnectionId = null;
            Status = GameStatus.AbortedByDisconnect;

            return new DisconnectResult(true, PlayerXConnectionId, PlayerSymbol.X);
        }

        PlayerXConnectionId = null;
        PlayerOConnectionId = null;
        Status = GameStatus.WaitingForPlayers;

        return new DisconnectResult(true, null, null);
    }
}
