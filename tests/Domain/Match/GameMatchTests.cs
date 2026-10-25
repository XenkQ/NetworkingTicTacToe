namespace NetworkingTicTacToe.Tests.Domain.Match;

using NetworkingTicTacToe.Server.Domain.Board;
using NetworkingTicTacToe.Server.Domain.Match;
using NetworkingTicTacToe.Server.Domain.Results;

public sealed class GameMatchTests
{
    private const string PLAYER_1 = "conn-player-1";
    private const string PLAYER_2 = "conn-player-2";
    private const string PLAYER_3 = "conn-player-3";

    [Fact]
    public void Constructor_InitializesInWaitingState()
    {
        // Arrange & Act
        GameMatch match = new();

        // Assert
        Assert.Equal(GameStatus.WaitingForPlayers, match.Status);
        Assert.Null(match.PlayerXConnectionId);
        Assert.Null(match.PlayerOConnectionId);
        Assert.Null(match.CurrentTurn);
        Assert.Null(match.Winner);
        Assert.Null(match.WinningLine);
        Assert.False(match.HasPlayerXAcceptedRematch);
        Assert.False(match.HasPlayerOAcceptedRematch);
    }

    [Fact]
    public void TryAddPlayer_AssignsFirstPlayerToX_AndWaitsForSecond()
    {
        // Arrange
        GameMatch match = new();

        // Act
        JoinMatchResult result = match.TryAddPlayer(PLAYER_1);

        // Assert
        Assert.True(result.IsJoined);
        Assert.Equal(PlayerSymbol.X, result.AssignedSymbol);
        Assert.False(result.DidMatchStart);
        Assert.Null(result.RejectionReason);
        Assert.Equal(PLAYER_1, match.PlayerXConnectionId);
        Assert.Null(match.PlayerOConnectionId);
        Assert.Equal(GameStatus.WaitingForPlayers, match.Status);
        Assert.Null(match.CurrentTurn);
    }

    [Fact]
    public void TryAddPlayer_AssignsSecondPlayerToO_AndStartsMatch()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);

        // Act
        JoinMatchResult result = match.TryAddPlayer(PLAYER_2);

        // Assert
        Assert.True(result.IsJoined);
        Assert.Equal(PlayerSymbol.O, result.AssignedSymbol);
        Assert.True(result.DidMatchStart);
        Assert.Null(result.RejectionReason);
        Assert.Equal(PLAYER_1, match.PlayerXConnectionId);
        Assert.Equal(PLAYER_2, match.PlayerOConnectionId);
        Assert.Equal(GameStatus.InProgress, match.Status);
        Assert.Equal(PlayerSymbol.X, match.CurrentTurn);
    }

    [Fact]
    public void TryAddPlayer_RejoiningPlayerReturnsExistingSymbol()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        JoinMatchResult rejoinResult = match.TryAddPlayer(PLAYER_1);

        // Assert
        Assert.True(rejoinResult.IsJoined);
        Assert.Equal(PlayerSymbol.X, rejoinResult.AssignedSymbol);
        Assert.True(rejoinResult.DidMatchStart);
    }

    [Fact]
    public void TryAddPlayer_RejectsThirdPlayer_WhenBothSlotsOccupied()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        JoinMatchResult result = match.TryAddPlayer(PLAYER_3);

        // Assert
        Assert.False(result.IsJoined);
        Assert.Null(result.AssignedSymbol);
        Assert.False(result.DidMatchStart);
        Assert.NotNull(result.RejectionReason);
        Assert.Contains("full", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PLAYER_1, match.PlayerXConnectionId);
        Assert.Equal(PLAYER_2, match.PlayerOConnectionId);
    }

    [Fact]
    public void TryAddPlayer_ThrowsArgumentException_WhenConnectionIdIsNullOrWhiteSpace()
    {
        // Arrange
        GameMatch match = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            match.TryAddPlayer("");
        });
    }

    [Fact]
    public void TryMakeMove_RejectsMove_WhenCallerIsNotParticipant()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        MoveAttemptResult result = match.TryMakeMove(PLAYER_3, 0, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(MoveErrorCode.NotParticipant, result.ErrorCode);
        Assert.Null(result.MovedSymbol);
        Assert.True(match.Board.IsCellEmpty(0, 0));
    }

    [Fact]
    public void TryMakeMove_RejectsMove_WhenGameIsNotInProgress()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);

        // Act (only 1 player joined)
        MoveAttemptResult result = match.TryMakeMove(PLAYER_1, 0, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(MoveErrorCode.GameNotInProgress, result.ErrorCode);
        Assert.True(match.Board.IsCellEmpty(0, 0));
    }

    [Fact]
    public void TryMakeMove_RejectsMove_WhenPlayerMovesOutOfTurn()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act (Player 2 is O, but Player 1 (X) starts)
        MoveAttemptResult result = match.TryMakeMove(PLAYER_2, 0, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(MoveErrorCode.OutOfTurn, result.ErrorCode);
        Assert.True(match.Board.IsCellEmpty(0, 0));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void TryMakeMove_RejectsMove_WhenOutOfBounds(int row, int col)
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        MoveAttemptResult result = match.TryMakeMove(PLAYER_1, row, col);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(MoveErrorCode.OutOfBounds, result.ErrorCode);
    }

    [Fact]
    public void TryMakeMove_RejectsMove_WhenCellIsAlreadyOccupied()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        match.TryMakeMove(PLAYER_1, 0, 0); // Player 1 places X at (0,0)

        // Act (Player 2 attempts to place O at occupied (0,0))
        MoveAttemptResult result = match.TryMakeMove(PLAYER_2, 0, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(MoveErrorCode.CellOccupied, result.ErrorCode);
        Assert.Equal(CellState.X, match.Board.GetCell(0, 0));
        Assert.Equal(PlayerSymbol.O, match.CurrentTurn);
    }

    [Fact]
    public void TryMakeMove_SwitchesTurn_OnValidNonWinningMove()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        MoveAttemptResult result = match.TryMakeMove(PLAYER_1, 1, 1);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(MoveErrorCode.None, result.ErrorCode);
        Assert.Equal(PlayerSymbol.X, result.MovedSymbol);
        Assert.Equal(new BoardCoordinate(1, 1), result.Coordinate);
        Assert.Equal(GameStatus.InProgress, match.Status);
        Assert.Equal(PlayerSymbol.O, match.CurrentTurn);
        Assert.Equal(CellState.X, match.Board.GetCell(1, 1));
    }

    [Fact]
    public void TryMakeMove_DetectsWin_AndEndsMatch()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // X: (0,0), O: (1,0)
        // X: (0,1), O: (1,1)
        // X: (0,2) -> Win for X
        match.TryMakeMove(PLAYER_1, 0, 0);
        match.TryMakeMove(PLAYER_2, 1, 0);
        match.TryMakeMove(PLAYER_1, 0, 1);
        match.TryMakeMove(PLAYER_2, 1, 1);

        // Act
        MoveAttemptResult winResult = match.TryMakeMove(PLAYER_1, 0, 2);

        // Assert
        Assert.True(winResult.IsValid);
        Assert.Equal(GameStatus.Won, winResult.ResultingStatus);
        Assert.Equal(PlayerSymbol.X, winResult.Winner);
        Assert.NotNull(winResult.WinningLine);
        Assert.Equal(GameStatus.Won, match.Status);
        Assert.Equal(PlayerSymbol.X, match.Winner);
        Assert.NotNull(match.WinningLine);
        Assert.Null(match.CurrentTurn);

        // Subsequent move after game won is rejected
        MoveAttemptResult postWinMove = match.TryMakeMove(PLAYER_2, 2, 2);
        Assert.False(postWinMove.IsValid);
        Assert.Equal(MoveErrorCode.GameNotInProgress, postWinMove.ErrorCode);
    }

    [Fact]
    public void TryMakeMove_DetectsDraw_WhenBoardIsFullWithNoWinner()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Grid sequence for draw:
        // X O X
        // X O O
        // O X X
        match.TryMakeMove(PLAYER_1, 0, 0); // X (0,0)
        match.TryMakeMove(PLAYER_2, 0, 1); // O (0,1)
        match.TryMakeMove(PLAYER_1, 0, 2); // X (0,2)
        match.TryMakeMove(PLAYER_2, 1, 1); // O (1,1)
        match.TryMakeMove(PLAYER_1, 1, 0); // X (1,0)
        match.TryMakeMove(PLAYER_2, 1, 2); // O (1,2)
        match.TryMakeMove(PLAYER_1, 2, 1); // X (2,1)
        match.TryMakeMove(PLAYER_2, 2, 0); // O (2,0)

        // Act (9th move by X at 2,2)
        MoveAttemptResult drawResult = match.TryMakeMove(PLAYER_1, 2, 2);

        // Assert
        Assert.True(drawResult.IsValid);
        Assert.Equal(GameStatus.Draw, drawResult.ResultingStatus);
        Assert.Null(drawResult.Winner);
        Assert.Null(drawResult.WinningLine);
        Assert.Equal(GameStatus.Draw, match.Status);
        Assert.Null(match.Winner);
        Assert.Null(match.CurrentTurn);
    }

    [Fact]
    public void TryAcceptRematch_Rejects_WhenGameIsStillInProgress()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        RematchVoteResult result = match.TryAcceptRematch(PLAYER_1);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.False(result.DidRematchStart);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void TryAcceptRematch_Rejects_WhenCallerIsNotParticipant()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Win the game
        match.TryMakeMove(PLAYER_1, 0, 0);
        match.TryMakeMove(PLAYER_2, 1, 0);
        match.TryMakeMove(PLAYER_1, 0, 1);
        match.TryMakeMove(PLAYER_2, 1, 1);
        match.TryMakeMove(PLAYER_1, 0, 2);

        // Act
        RematchVoteResult result = match.TryAcceptRematch(PLAYER_3);

        // Assert
        Assert.False(result.IsAccepted);
        Assert.False(result.DidRematchStart);
    }

    [Fact]
    public void TryAcceptRematch_SwapsSymbols_AndStartsNewGame_WhenBothAccept()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1); // Player 1 is X
        match.TryAddPlayer(PLAYER_2); // Player 2 is O

        // Win the game for X
        match.TryMakeMove(PLAYER_1, 0, 0);
        match.TryMakeMove(PLAYER_2, 1, 0);
        match.TryMakeMove(PLAYER_1, 0, 1);
        match.TryMakeMove(PLAYER_2, 1, 1);
        match.TryMakeMove(PLAYER_1, 0, 2);

        // Act - Player 1 accepts rematch
        RematchVoteResult firstVote = match.TryAcceptRematch(PLAYER_1);

        // Assert first vote
        Assert.True(firstVote.IsAccepted);
        Assert.False(firstVote.DidRematchStart);
        Assert.True(match.HasPlayerXAcceptedRematch);
        Assert.False(match.HasPlayerOAcceptedRematch);

        // Act - Player 2 accepts rematch
        RematchVoteResult secondVote = match.TryAcceptRematch(PLAYER_2);

        // Assert rematch started with symbol swap
        Assert.True(secondVote.IsAccepted);
        Assert.True(secondVote.DidRematchStart);
        Assert.Equal(GameStatus.InProgress, match.Status);
        Assert.Equal(PlayerSymbol.X, match.CurrentTurn);
        Assert.Null(match.Winner);
        Assert.Null(match.WinningLine);
        Assert.False(match.HasPlayerXAcceptedRematch);
        Assert.False(match.HasPlayerOAcceptedRematch);
        Assert.True(match.Board.IsCellEmpty(0, 0));

        // SYMBOL SWAP VERIFICATION:
        // Player 2 is now X, Player 1 is now O
        Assert.Equal(PLAYER_2, match.PlayerXConnectionId);
        Assert.Equal(PLAYER_1, match.PlayerOConnectionId);
        Assert.Equal(PlayerSymbol.X, match.GetPlayerSymbol(PLAYER_2));
        Assert.Equal(PlayerSymbol.O, match.GetPlayerSymbol(PLAYER_1));

        // Player 2 (new X) can make the first move
        MoveAttemptResult newMove = match.TryMakeMove(PLAYER_2, 1, 1);
        Assert.True(newMove.IsValid);
        Assert.Equal(PlayerSymbol.X, newMove.MovedSymbol);
        Assert.Equal(PlayerSymbol.O, match.CurrentTurn);

        // Third player is still rejected
        JoinMatchResult thirdJoin = match.TryAddPlayer(PLAYER_3);
        Assert.False(thirdJoin.IsJoined);
    }

    [Fact]
    public void HandleDisconnect_ReturnsNotParticipant_WhenCallerNotSeated()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);

        // Act
        DisconnectResult result = match.HandleDisconnect(PLAYER_3);

        // Assert
        Assert.False(result.WasParticipant);
        Assert.Null(result.RemainingPlayerConnectionId);
        Assert.Null(result.RemainingPlayerNewSymbol);
    }

    [Fact]
    public void HandleDisconnect_PlayerODisconnects_RetainsPlayerXInWaitingState()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);
        match.TryMakeMove(PLAYER_1, 0, 0);

        // Act (Player 2 disconnects)
        DisconnectResult result = match.HandleDisconnect(PLAYER_2);

        // Assert
        Assert.True(result.WasParticipant);
        Assert.Equal(PLAYER_1, result.RemainingPlayerConnectionId);
        Assert.Equal(PlayerSymbol.X, result.RemainingPlayerNewSymbol);
        Assert.Equal(GameStatus.AbortedByDisconnect, match.Status);
        Assert.Equal(PLAYER_1, match.PlayerXConnectionId);
        Assert.Null(match.PlayerOConnectionId);
        Assert.True(match.Board.IsCellEmpty(0, 0)); // Board was reset

        // Returning/new player joins as O and match immediately restarts
        JoinMatchResult newPlayerJoin = match.TryAddPlayer(PLAYER_3);
        Assert.True(newPlayerJoin.IsJoined);
        Assert.Equal(PlayerSymbol.O, newPlayerJoin.AssignedSymbol);
        Assert.True(newPlayerJoin.DidMatchStart);
        Assert.Equal(GameStatus.InProgress, match.Status);
        Assert.Equal(PlayerSymbol.X, match.CurrentTurn);
    }

    [Fact]
    public void HandleDisconnect_PlayerXDisconnects_PromotesPlayerOToWaitingPlayerX()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);
        match.TryAddPlayer(PLAYER_2);
        match.TryMakeMove(PLAYER_1, 0, 0);

        // Act (Player 1 disconnects)
        DisconnectResult result = match.HandleDisconnect(PLAYER_1);

        // Assert
        Assert.True(result.WasParticipant);
        Assert.Equal(PLAYER_2, result.RemainingPlayerConnectionId);
        Assert.Equal(PlayerSymbol.X, result.RemainingPlayerNewSymbol);
        Assert.Equal(GameStatus.AbortedByDisconnect, match.Status);
        Assert.Equal(PLAYER_2, match.PlayerXConnectionId); // Player 2 promoted to X
        Assert.Null(match.PlayerOConnectionId);
        Assert.True(match.Board.IsCellEmpty(0, 0)); // Board was reset

        // Returning/new player joins as O and fresh match starts
        JoinMatchResult newPlayerJoin = match.TryAddPlayer(PLAYER_3);
        Assert.True(newPlayerJoin.IsJoined);
        Assert.Equal(PlayerSymbol.O, newPlayerJoin.AssignedSymbol);
        Assert.True(newPlayerJoin.DidMatchStart);
        Assert.Equal(GameStatus.InProgress, match.Status);
        Assert.Equal(PlayerSymbol.X, match.CurrentTurn);
    }

    [Fact]
    public void HandleDisconnect_SoleWaitingPlayerDisconnects_ResetsToWaitingForPlayers()
    {
        // Arrange
        GameMatch match = new();
        match.TryAddPlayer(PLAYER_1);

        // Act
        DisconnectResult result = match.HandleDisconnect(PLAYER_1);

        // Assert
        Assert.True(result.WasParticipant);
        Assert.Null(result.RemainingPlayerConnectionId);
        Assert.Null(result.RemainingPlayerNewSymbol);
        Assert.Equal(GameStatus.WaitingForPlayers, match.Status);
        Assert.Null(match.PlayerXConnectionId);
        Assert.Null(match.PlayerOConnectionId);
    }
}
