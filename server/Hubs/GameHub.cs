namespace NetworkingTicTacToe.Server.Hubs;

using Microsoft.AspNetCore.SignalR;
using NetworkingTicTacToe.Server.DTOs;
using NetworkingTicTacToe.Server.Services;

/// <summary>
/// Strongly-typed client contract for SignalR events dispatched by the server.
/// </summary>
public interface IGameClient
{
    Task OnGameStateUpdated(GameStateDto gameState);

    Task OnPlayerAssignmentChanged(PlayerAssignmentDto assignment);

    Task OnMoveRejected(MoveResultDto rejection);
}

/// <summary>
/// SignalR hub coordinating client RPCs and broadcasting game events.
/// </summary>
public sealed class GameHub : Hub<IGameClient>
{
    private readonly IGameSessionService _sessionService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameSessionService sessionService, ILogger<GameHub> logger)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JoinGameResponseDto> JoinGame()
    {
        JoinSessionResult result = _sessionService.JoinSession(Context.ConnectionId);

        _logger.LogInformation(
            "Connection {ConnectionId} attempted to join. IsJoined: {IsJoined}, Symbol: {Symbol}, DidMatchStart: {DidMatchStart}",
            Context.ConnectionId,
            result.Response.IsJoined,
            result.Response.AssignedSymbol,
            result.DidMatchStart);

        if (result.DidMatchStart)
        {
            await Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState);
        }

        return result.Response;
    }

    public async Task<MoveResultDto> MakeMove(MakeMoveRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        MoveSessionResult result = _sessionService.MakeMove(Context.ConnectionId, request.Row, request.Col);

        if (result.IsAccepted)
        {
            _logger.LogInformation(
                "Move accepted for {ConnectionId} at ({Row}, {Col}) with symbol {Symbol}. Resulting status: {Status}",
                Context.ConnectionId,
                request.Row,
                request.Col,
                result.Response.Symbol,
                result.Response.GameState.Status);

            await Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState);
        }
        else
        {
            _logger.LogWarning(
                "Move rejected for {ConnectionId} at ({Row}, {Col}). Error: {ErrorCode} - {ErrorMessage}",
                Context.ConnectionId,
                request.Row,
                request.Col,
                result.Response.ErrorCode,
                result.Response.ErrorMessage);

            await Clients.Caller.OnMoveRejected(result.Response);
        }

        return result.Response;
    }

    public async Task<RematchResponseDto> RequestRematch()
    {
        RematchSessionResult result = _sessionService.RequestRematch(Context.ConnectionId);

        if (result.DidRematchStart)
        {
            _logger.LogInformation(
                "Rematch started between Player X ({PlayerX}) and Player O ({PlayerO}).",
                result.NewPlayerXConnectionId,
                result.NewPlayerOConnectionId);

            if (result.NewPlayerXConnectionId is not null && result.PlayerXAssignment is not null)
            {
                await Clients.Client(result.NewPlayerXConnectionId).OnPlayerAssignmentChanged(result.PlayerXAssignment);
            }

            if (result.NewPlayerOConnectionId is not null && result.PlayerOAssignment is not null)
            {
                await Clients.Client(result.NewPlayerOConnectionId).OnPlayerAssignmentChanged(result.PlayerOAssignment);
            }

            await Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState);
        }
        else if (result.Response.IsAccepted)
        {
            _logger.LogInformation(
                "Rematch accepted by {ConnectionId}. Awaiting second player vote.",
                Context.ConnectionId);

            await Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState);
        }

        return result.Response;
    }

    public Task<GameStateDto> GetGameState()
    {
        GameStateDto gameState = _sessionService.GetCurrentGameState();
        return Task.FromResult(gameState);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection {ConnectionId} disconnected.", Context.ConnectionId);

        DisconnectSessionResult result = _sessionService.HandleDisconnect(Context.ConnectionId);

        if (result.WasParticipant && result.RemainingPlayerConnectionId is not null)
        {
            _logger.LogInformation(
                "Participant disconnected. Remaining player {RemainingPlayerId} promoted to waiting Player X.",
                result.RemainingPlayerConnectionId);

            if (result.RemainingPlayerAssignment is not null)
            {
                await Clients.Client(result.RemainingPlayerConnectionId).OnPlayerAssignmentChanged(result.RemainingPlayerAssignment);
            }

            if (result.GameState is not null)
            {
                await Clients.Client(result.RemainingPlayerConnectionId).OnGameStateUpdated(result.GameState);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
