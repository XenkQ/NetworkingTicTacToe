namespace NetworkingTicTacToe.Tests.Services;

using NetworkingTicTacToe.Server.Domain.Match;
using NetworkingTicTacToe.Server.DTOs;
using NetworkingTicTacToe.Server.Services;

public sealed class GameSessionServiceTests
{
    private const string PLAYER_X_ID = "conn-player-x";
    private const string PLAYER_O_ID = "conn-player-o";
    private const string SPECTATOR_ID = "conn-spectator";

    [Fact]
    public void JoinSession_FirstPlayer_AssignedXAndWaiting()
    {
        // Arrange
        GameSessionService service = new();

        // Act
        JoinSessionResult result = service.JoinSession(PLAYER_X_ID);

        // Assert
        Assert.True(result.Response.IsJoined);
        Assert.Equal("X", result.Response.AssignedSymbol);
        Assert.Null(result.Response.RejectionReason);
        Assert.False(result.DidMatchStart);
        Assert.Equal("WaitingForPlayers", result.Response.GameState.Status);
        Assert.Single(result.ParticipantConnectionIds);
        Assert.Contains(PLAYER_X_ID, result.ParticipantConnectionIds);
    }

    [Fact]
    public void JoinSession_SecondPlayer_AssignedOAndStartsMatch()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);

        // Act
        JoinSessionResult result = service.JoinSession(PLAYER_O_ID);

        // Assert
        Assert.True(result.Response.IsJoined);
        Assert.Equal("O", result.Response.AssignedSymbol);
        Assert.Null(result.Response.RejectionReason);
        Assert.True(result.DidMatchStart);
        Assert.Equal("InProgress", result.Response.GameState.Status);
        Assert.Equal("X", result.Response.GameState.CurrentTurn);
        Assert.Equal(2, result.ParticipantConnectionIds.Count);
        Assert.Contains(PLAYER_X_ID, result.ParticipantConnectionIds);
        Assert.Contains(PLAYER_O_ID, result.ParticipantConnectionIds);
    }

    [Fact]
    public void JoinSession_ThirdPlayer_RejectedFull()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        JoinSessionResult result = service.JoinSession(SPECTATOR_ID);

        // Assert
        Assert.False(result.Response.IsJoined);
        Assert.Null(result.Response.AssignedSymbol);
        Assert.Contains("full", result.Response.RejectionReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.DidMatchStart);
        Assert.Equal(2, result.ParticipantConnectionIds.Count);
    }

    [Fact]
    public void JoinSession_ExistingPlayerRejoins_ReturnsAssignedSymbolIdempotently()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        JoinSessionResult reJoinResult = service.JoinSession(PLAYER_X_ID);

        // Assert
        Assert.True(reJoinResult.Response.IsJoined);
        Assert.Equal("X", reJoinResult.Response.AssignedSymbol);
        Assert.Null(reJoinResult.Response.RejectionReason);
        Assert.True(reJoinResult.DidMatchStart);
    }

    [Fact]
    public void MakeMove_PlayerXFirstMove_AcceptedAndTurnSwitches()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        MoveSessionResult result = service.MakeMove(PLAYER_X_ID, 0, 0);

        // Assert
        Assert.True(result.IsAccepted);
        Assert.Equal("None", result.Response.ErrorCode);
        Assert.Null(result.Response.ErrorMessage);
        Assert.Equal("X", result.Response.Symbol);
        Assert.Equal("X", result.Response.GameState.Cells[0][0]);
        Assert.Equal("O", result.Response.GameState.CurrentTurn);
        Assert.Equal("InProgress", result.Response.GameState.Status);
    }

    [Fact]
    public void MakeMove_PlayerOMoveOutOfTurn_RejectedOutOfTurn()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        MoveSessionResult result = service.MakeMove(PLAYER_O_ID, 0, 0);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.Equal("OutOfTurn", result.Response.ErrorCode);
        Assert.Equal("Empty", result.Response.GameState.Cells[0][0]);
        Assert.Equal("X", result.Response.GameState.CurrentTurn);
    }

    [Fact]
    public void MakeMove_NonParticipant_RejectedNotParticipant()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        MoveSessionResult result = service.MakeMove(SPECTATOR_ID, 0, 0);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.Equal("NotParticipant", result.Response.ErrorCode);
    }

    [Fact]
    public void MakeMove_OutOfBounds_RejectedOutOfBounds()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        MoveSessionResult result = service.MakeMove(PLAYER_X_ID, 3, 3);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.Equal("OutOfBounds", result.Response.ErrorCode);
    }

    [Fact]
    public void MakeMove_CellOccupied_RejectedCellOccupied()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);
        service.MakeMove(PLAYER_X_ID, 0, 0);

        // Act
        MoveSessionResult result = service.MakeMove(PLAYER_O_ID, 0, 0);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.Equal("CellOccupied", result.Response.ErrorCode);
        Assert.Equal("X", result.Response.GameState.Cells[0][0]);
    }

    [Fact]
    public void MakeMove_WinningMove_DetectsWinAndWinningLine()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // X: (0,0), O: (1,0)
        service.MakeMove(PLAYER_X_ID, 0, 0);
        service.MakeMove(PLAYER_O_ID, 1, 0);

        // X: (0,1), O: (1,1)
        service.MakeMove(PLAYER_X_ID, 0, 1);
        service.MakeMove(PLAYER_O_ID, 1, 1);

        // Act - X: (0,2) wins top row
        MoveSessionResult winResult = service.MakeMove(PLAYER_X_ID, 0, 2);

        // Assert
        Assert.True(winResult.IsAccepted);
        Assert.Equal("Won", winResult.Response.GameState.Status);
        Assert.Equal("X", winResult.Response.GameState.Winner);
        Assert.Null(winResult.Response.GameState.CurrentTurn);
        Assert.NotNull(winResult.Response.GameState.WinningLine);
        Assert.Equal(3, winResult.Response.GameState.WinningLine.Cells.Length);
        Assert.Equal(0, winResult.Response.GameState.WinningLine.Cells[0].Row);
        Assert.Equal(0, winResult.Response.GameState.WinningLine.Cells[0].Col);
        Assert.Equal(0, winResult.Response.GameState.WinningLine.Cells[2].Row);
        Assert.Equal(2, winResult.Response.GameState.WinningLine.Cells[2].Col);
    }

    [Fact]
    public void MakeMove_PostGameMove_RejectedGameNotInProgress()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        service.MakeMove(PLAYER_X_ID, 0, 0);
        service.MakeMove(PLAYER_O_ID, 1, 0);
        service.MakeMove(PLAYER_X_ID, 0, 1);
        service.MakeMove(PLAYER_O_ID, 1, 1);
        service.MakeMove(PLAYER_X_ID, 0, 2); // X wins

        // Act
        MoveSessionResult postGameResult = service.MakeMove(PLAYER_O_ID, 2, 2);

        // Assert
        Assert.False(postGameResult.IsAccepted);
        Assert.Equal("GameNotInProgress", postGameResult.Response.ErrorCode);
    }

    [Fact]
    public void MakeMove_DrawMove_DetectsDraw()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Board layout for draw:
        // X O X
        // X X O
        // O X O
        service.MakeMove(PLAYER_X_ID, 0, 0); // X
        service.MakeMove(PLAYER_O_ID, 0, 1); // O
        service.MakeMove(PLAYER_X_ID, 0, 2); // X
        service.MakeMove(PLAYER_O_ID, 1, 2); // O
        service.MakeMove(PLAYER_X_ID, 1, 0); // X
        service.MakeMove(PLAYER_O_ID, 2, 0); // O
        service.MakeMove(PLAYER_X_ID, 1, 1); // X
        service.MakeMove(PLAYER_O_ID, 2, 2); // O

        // Act
        MoveSessionResult drawResult = service.MakeMove(PLAYER_X_ID, 2, 1); // X at (2,1) fills 9th cell

        // Assert
        Assert.True(drawResult.IsAccepted);
        Assert.Equal("Draw", drawResult.Response.GameState.Status);
        Assert.Null(drawResult.Response.GameState.Winner);
        Assert.Null(drawResult.Response.GameState.WinningLine);
        Assert.Null(drawResult.Response.GameState.CurrentTurn);
    }

    [Fact]
    public void RequestRematch_NonParticipant_Rejected()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        RematchSessionResult result = service.RequestRematch(SPECTATOR_ID);

        // Assert
        Assert.False(result.Response.IsAccepted);
        Assert.False(result.DidRematchStart);
    }

    [Fact]
    public void RequestRematch_MatchNotInFinishedState_Rejected()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act (Match is InProgress)
        RematchSessionResult result = service.RequestRematch(PLAYER_X_ID);

        // Assert
        Assert.False(result.Response.IsAccepted);
        Assert.False(result.DidRematchStart);
    }

    [Fact]
    public void RequestRematch_BothPlayersVote_StartsRematchWithSwappedSymbols()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Complete match with win for X
        service.MakeMove(PLAYER_X_ID, 0, 0);
        service.MakeMove(PLAYER_O_ID, 1, 0);
        service.MakeMove(PLAYER_X_ID, 0, 1);
        service.MakeMove(PLAYER_O_ID, 1, 1);
        service.MakeMove(PLAYER_X_ID, 0, 2);

        // Act 1: First player votes
        RematchSessionResult firstVote = service.RequestRematch(PLAYER_X_ID);

        // Assert 1
        Assert.True(firstVote.Response.IsAccepted);
        Assert.False(firstVote.DidRematchStart);
        Assert.True(firstVote.Response.GameState.HasPlayerXAcceptedRematch);
        Assert.False(firstVote.Response.GameState.HasPlayerOAcceptedRematch);

        // Act 2: Second player votes
        RematchSessionResult secondVote = service.RequestRematch(PLAYER_O_ID);

        // Assert 2
        Assert.True(secondVote.Response.IsAccepted);
        Assert.True(secondVote.DidRematchStart);

        // Symbols swapped! Former player O is now Player X
        Assert.Equal(PLAYER_O_ID, secondVote.NewPlayerXConnectionId);
        Assert.Equal(PLAYER_X_ID, secondVote.NewPlayerOConnectionId);
        Assert.NotNull(secondVote.PlayerXAssignment);
        Assert.Equal("X", secondVote.PlayerXAssignment.AssignedSymbol);
        Assert.NotNull(secondVote.PlayerOAssignment);
        Assert.Equal("O", secondVote.PlayerOAssignment.AssignedSymbol);

        // Board reset & in progress
        Assert.Equal("InProgress", secondVote.Response.GameState.Status);
        Assert.Equal("X", secondVote.Response.GameState.CurrentTurn);
        Assert.Equal("Empty", secondVote.Response.GameState.Cells[0][0]);

        // Former Player O (now X) can make the first move!
        MoveSessionResult firstMoveAfterRematch = service.MakeMove(PLAYER_O_ID, 1, 1);
        Assert.True(firstMoveAfterRematch.IsAccepted);
        Assert.Equal("X", firstMoveAfterRematch.Response.Symbol);
        Assert.Equal("O", firstMoveAfterRematch.Response.GameState.CurrentTurn);
    }

    [Fact]
    public void HandleDisconnect_NonParticipant_ReturnsWasParticipantFalse()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act
        DisconnectSessionResult result = service.HandleDisconnect(SPECTATOR_ID);

        // Assert
        Assert.False(result.WasParticipant);
        Assert.Null(result.RemainingPlayerConnectionId);
    }

    [Fact]
    public void HandleDisconnect_ActivePlayerDisconnects_RemainingPlayerPromotedToX()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);
        service.MakeMove(PLAYER_X_ID, 0, 0);

        // Act - Player X disconnects
        DisconnectSessionResult result = service.HandleDisconnect(PLAYER_X_ID);

        // Assert
        Assert.True(result.WasParticipant);
        Assert.Equal(PLAYER_O_ID, result.RemainingPlayerConnectionId);
        Assert.NotNull(result.RemainingPlayerAssignment);
        Assert.Equal("X", result.RemainingPlayerAssignment.AssignedSymbol);
        Assert.NotNull(result.GameState);
        Assert.Equal("AbortedByDisconnect", result.GameState.Status);
        Assert.Equal("Empty", result.GameState.Cells[0][0]); // Board was reset

        // When a new player joins, they become O and game restarts!
        JoinSessionResult newJoin = service.JoinSession(SPECTATOR_ID);
        Assert.True(newJoin.Response.IsJoined);
        Assert.Equal("O", newJoin.Response.AssignedSymbol);
        Assert.True(newJoin.DidMatchStart);
        Assert.Equal("InProgress", newJoin.Response.GameState.Status);
    }

    [Fact]
    public void HandleDisconnect_RemainingPlayerDisconnects_MatchResetToWaiting()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);

        // Act - Sole waiting player disconnects
        DisconnectSessionResult result = service.HandleDisconnect(PLAYER_X_ID);

        // Assert
        Assert.True(result.WasParticipant);
        Assert.Null(result.RemainingPlayerConnectionId);

        GameStateDto state = service.GetCurrentGameState();
        Assert.Equal("WaitingForPlayers", state.Status);
        Assert.False(state.IsPlayerXConnected);
        Assert.False(state.IsPlayerOConnected);
    }

    [Fact]
    public void Concurrency_SimultaneousJoinRequests_OnlyTwoPlayersSeated()
    {
        // Arrange
        GameSessionService service = new();
        const int ATTEMPTS = 20;
        List<JoinSessionResult> results = new();
        Lock resultsLock = new();

        // Act
        Parallel.For(0, ATTEMPTS, i =>
        {
            string connectionId = $"conn-concurrent-{i}";
            JoinSessionResult result = service.JoinSession(connectionId);
            lock (resultsLock)
            {
                results.Add(result);
            }
        });

        // Assert
        int joinedCount = results.Count(r => r.Response.IsJoined);
        int rejectedCount = results.Count(r => !r.Response.IsJoined);

        Assert.Equal(2, joinedCount);
        Assert.Equal(ATTEMPTS - 2, rejectedCount);

        IReadOnlyList<string> participants = service.GetParticipantConnectionIds();
        Assert.Equal(2, participants.Count);
    }

    [Fact]
    public void Concurrency_SimultaneousMoves_SerializedThreadSafely()
    {
        // Arrange
        GameSessionService service = new();
        service.JoinSession(PLAYER_X_ID);
        service.JoinSession(PLAYER_O_ID);

        // Act - Both players simultaneously attempt multiple moves on various cells
        List<MoveSessionResult> moveResults = new();
        Lock moveResultsLock = new();

        Parallel.For(0, 9, i =>
        {
            int row = i / 3;
            int col = i % 3;
            string callerId = i % 2 == 0 ? PLAYER_X_ID : PLAYER_O_ID;

            MoveSessionResult result = service.MakeMove(callerId, row, col);
            lock (moveResultsLock)
            {
                moveResults.Add(result);
            }
        });

        // Assert
        // The service should not crash, and moves should be validated safely
        Assert.Equal(9, moveResults.Count);
        GameStateDto state = service.GetCurrentGameState();
        Assert.NotNull(state);
    }
}
