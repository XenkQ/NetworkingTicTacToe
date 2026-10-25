namespace NetworkingTicTacToe.Server.Services;

using NetworkingTicTacToe.Server.Domain.Board;
using NetworkingTicTacToe.Server.Domain.Match;
using NetworkingTicTacToe.Server.Domain.Results;
using NetworkingTicTacToe.Server.DTOs;

/// <summary>
/// Service result for a player join operation.
/// </summary>
public sealed record JoinSessionResult(
    JoinGameResponseDto Response,
    bool DidMatchStart,
    IReadOnlyList<string> ParticipantConnectionIds);

/// <summary>
/// Service result for a move operation.
/// </summary>
public sealed record MoveSessionResult(
    MoveResultDto Response,
    bool IsAccepted,
    IReadOnlyList<string> ParticipantConnectionIds);

/// <summary>
/// Service result for a rematch acceptance operation.
/// </summary>
public sealed record RematchSessionResult(
    RematchResponseDto Response,
    bool DidRematchStart,
    string? NewPlayerXConnectionId,
    string? NewPlayerOConnectionId,
    PlayerAssignmentDto? PlayerXAssignment,
    PlayerAssignmentDto? PlayerOAssignment,
    IReadOnlyList<string> ParticipantConnectionIds,
    bool DidVoteChange = true);

/// <summary>
/// Service result for player disconnect handling.
/// </summary>
public sealed record DisconnectSessionResult(
    bool WasParticipant,
    string? RemainingPlayerConnectionId,
    PlayerAssignmentDto? RemainingPlayerAssignment,
    GameStateDto? GameState);

/// <summary>
/// Primary interface for thread-safe session coordination of the active Tic-Tac-Toe match.
/// </summary>
public interface IGameSessionService
{
    JoinSessionResult JoinSession(string connectionId);

    MoveSessionResult MakeMove(string connectionId, int row, int col);

    RematchSessionResult RequestRematch(string connectionId);

    DisconnectSessionResult HandleDisconnect(string connectionId);

    GameStateDto GetCurrentGameState();

    IReadOnlyList<string> GetParticipantConnectionIds();
}

/// <summary>
/// Thread-safe singleton service managing the active game match and snapshot creation.
/// </summary>
public sealed class GameSessionService : IGameSessionService
{
    private readonly Lock _syncRoot = new();
    private readonly GameMatch _match = new();
    private long _stateVersion;

    public GameSessionService()
    {
    }

    public JoinSessionResult JoinSession(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_syncRoot)
        {
            bool wasAlreadyParticipant = _match.IsParticipant(connectionId);
            JoinMatchResult joinResult = _match.TryAddPlayer(connectionId);

            if (joinResult.IsJoined && !wasAlreadyParticipant)
            {
                _stateVersion++;
            }

            string statusMessage = !joinResult.IsJoined
                ? (joinResult.RejectionReason ?? "Game is currently full (2/2 players active).")
                : joinResult.DidMatchStart
                    ? "Match started! Player X takes the first turn."
                    : _match.Status == GameStatus.InProgress
                        ? $"Match in progress. Turn: {_match.CurrentTurn}"
                        : "Waiting for opponent...";

            GameStateDto gameState = CreateGameStateSnapshot(statusMessage);

            JoinGameResponseDto response = new(
                joinResult.IsJoined,
                joinResult.AssignedSymbol?.ToString(),
                joinResult.RejectionReason,
                gameState);

            return new JoinSessionResult(response, joinResult.DidMatchStart, GetParticipantConnectionIdsCore());
        }
    }

    public MoveSessionResult MakeMove(string connectionId, int row, int col)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_syncRoot)
        {
            MoveAttemptResult moveResult = _match.TryMakeMove(connectionId, row, col);

            if (!moveResult.IsValid)
            {
                GameStateDto errorGameState = CreateGameStateSnapshot();
                MoveResultDto errorResponse = new(
                    false,
                    moveResult.ErrorCode.ToString(),
                    moveResult.ErrorMessage,
                    row,
                    col,
                    moveResult.MovedSymbol?.ToString(),
                    errorGameState);

                return new MoveSessionResult(errorResponse, false, GetParticipantConnectionIdsCore());
            }

            _stateVersion++;

            string statusMessage = _match.Status switch
            {
                GameStatus.Won => $"Player {_match.Winner} wins!",
                GameStatus.Draw => "Match ended in a draw.",
                GameStatus.InProgress => $"Match in progress. Turn: {_match.CurrentTurn}",
                _ => ComputeDefaultStatusMessage()
            };

            GameStateDto gameState = CreateGameStateSnapshot(statusMessage);
            MoveResultDto response = new(
                true,
                MoveErrorCode.None.ToString(),
                null,
                row,
                col,
                moveResult.MovedSymbol?.ToString(),
                gameState);

            return new MoveSessionResult(response, true, GetParticipantConnectionIdsCore());
        }
    }

    public RematchSessionResult RequestRematch(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_syncRoot)
        {
            bool hadAlreadyVoted =
                (connectionId == _match.PlayerXConnectionId && _match.HasPlayerXAcceptedRematch) ||
                (connectionId == _match.PlayerOConnectionId && _match.HasPlayerOAcceptedRematch);

            RematchVoteResult rematchResult = _match.TryAcceptRematch(connectionId);

            if (!rematchResult.IsAccepted)
            {
                GameStateDto errorGameState = CreateGameStateSnapshot();
                RematchResponseDto errorResponse = new(
                    false,
                    rematchResult.ErrorMessage,
                    false,
                    errorGameState);

                return new RematchSessionResult(
                    errorResponse,
                    false,
                    null,
                    null,
                    null,
                    null,
                    GetParticipantConnectionIdsCore(),
                    false);
            }

            bool didVoteChange = !hadAlreadyVoted || rematchResult.DidRematchStart;
            if (didVoteChange)
            {
                _stateVersion++;
            }

            if (rematchResult.DidRematchStart)
            {
                string statusMessage = "Rematch started! Player X takes the first turn.";
                GameStateDto gameState = CreateGameStateSnapshot(statusMessage);

                string? newPlayerX = _match.PlayerXConnectionId;
                string? newPlayerO = _match.PlayerOConnectionId;

                PlayerAssignmentDto? assignmentX = newPlayerX is not null ? new PlayerAssignmentDto("X", gameState) : null;
                PlayerAssignmentDto? assignmentO = newPlayerO is not null ? new PlayerAssignmentDto("O", gameState) : null;

                RematchResponseDto response = new(true, null, true, gameState);

                return new RematchSessionResult(
                    response,
                    true,
                    newPlayerX,
                    newPlayerO,
                    assignmentX,
                    assignmentO,
                    GetParticipantConnectionIdsCore(),
                    true);
            }

            string waitingMessage = "Rematch requested (1/2 accepted)...";
            GameStateDto waitingGameState = CreateGameStateSnapshot(waitingMessage);
            RematchResponseDto waitingResponse = new(true, null, false, waitingGameState);

            return new RematchSessionResult(
                waitingResponse,
                false,
                null,
                null,
                null,
                null,
                GetParticipantConnectionIdsCore(),
                didVoteChange);
        }
    }

    public DisconnectSessionResult HandleDisconnect(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_syncRoot)
        {
            DisconnectResult disconnectResult = _match.HandleDisconnect(connectionId);

            if (!disconnectResult.WasParticipant)
            {
                return new DisconnectSessionResult(false, null, null, null);
            }

            _stateVersion++;

            if (disconnectResult.RemainingPlayerConnectionId is not null)
            {
                string statusMessage = "Opponent disconnected. Waiting for a new opponent...";
                GameStateDto gameState = CreateGameStateSnapshot(statusMessage);
                PlayerAssignmentDto assignment = new("X", gameState);

                return new DisconnectSessionResult(true, disconnectResult.RemainingPlayerConnectionId, assignment, gameState);
            }

            return new DisconnectSessionResult(true, null, null, null);
        }
    }

    public GameStateDto GetCurrentGameState()
    {
        lock (_syncRoot)
        {
            return CreateGameStateSnapshot();
        }
    }

    public IReadOnlyList<string> GetParticipantConnectionIds()
    {
        lock (_syncRoot)
        {
            return GetParticipantConnectionIdsCore();
        }
    }

    private IReadOnlyList<string> GetParticipantConnectionIdsCore()
    {
        List<string> ids = new(2);
        if (_match.PlayerXConnectionId is not null)
        {
            ids.Add(_match.PlayerXConnectionId);
        }

        if (_match.PlayerOConnectionId is not null)
        {
            ids.Add(_match.PlayerOConnectionId);
        }

        return ids;
    }

    private GameStateDto CreateGameStateSnapshot(string? customStatusMessage = null)
    {
        WinningLineDto? winningLineDto = null;
        if (_match.WinningLine is not null)
        {
            winningLineDto = new WinningLineDto(
            [
                new BoardCellCoordinateDto(_match.WinningLine.First.Row, _match.WinningLine.First.Col),
                new BoardCellCoordinateDto(_match.WinningLine.Second.Row, _match.WinningLine.Second.Col),
                new BoardCellCoordinateDto(_match.WinningLine.Third.Row, _match.WinningLine.Third.Col)
            ]);
        }

        string statusMessage = customStatusMessage ?? ComputeDefaultStatusMessage();

        return new GameStateDto(
            _stateVersion,
            _match.Board.ToJaggedArray(),
            _match.Status.ToString(),
            _match.CurrentTurn?.ToString(),
            _match.Winner?.ToString(),
            winningLineDto,
            _match.PlayerXConnectionId is not null,
            _match.PlayerOConnectionId is not null,
            _match.HasPlayerXAcceptedRematch,
            _match.HasPlayerOAcceptedRematch,
            statusMessage);
    }

    private string ComputeDefaultStatusMessage()
    {
        return _match.Status switch
        {
            GameStatus.WaitingForPlayers => "Waiting for opponent...",
            GameStatus.InProgress => $"Match in progress. Turn: {_match.CurrentTurn}",
            GameStatus.Won => $"Player {_match.Winner} wins!",
            GameStatus.Draw => "Match ended in a draw.",
            GameStatus.AbortedByDisconnect => "Opponent disconnected. Waiting for a new opponent...",
            _ => "Ready."
        };
    }
}
