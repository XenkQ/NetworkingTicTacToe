namespace NetworkingTicTacToe.Tests.Hubs;

using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NetworkingTicTacToe.Server.DTOs;
using NetworkingTicTacToe.Server.Hubs;
using NetworkingTicTacToe.Server.Services;

public sealed class GameHubTests
{
    private const string PLAYER_X_ID = "conn-hub-x";
    private const string PLAYER_O_ID = "conn-hub-o";
    private const string SPECTATOR_ID = "conn-hub-spectator";

    [Fact]
    public async Task JoinGame_SecondPlayerJoins_BroadcastsGameStateUpdatedToParticipants()
    {
        // Arrange
        GameSessionService sessionService = new();
        RecordingHubCallerClients clients = new();

        using GameHub hubX = CreateHub(sessionService, clients, PLAYER_X_ID);
        JoinGameResponseDto firstResponse = await hubX.JoinGame();

        using GameHub hubO = CreateHub(sessionService, clients, PLAYER_O_ID);

        // Act
        JoinGameResponseDto secondResponse = await hubO.JoinGame();

        // Assert
        Assert.True(firstResponse.IsJoined);
        Assert.Equal("X", firstResponse.AssignedSymbol);
        Assert.True(secondResponse.IsJoined);
        Assert.Equal("O", secondResponse.AssignedSymbol);
        Assert.Single(clients.BroadcastStates);
        Assert.Equal("InProgress", clients.BroadcastStates[0].Status);
    }

    [Fact]
    public async Task MakeMove_InvalidMove_SendsOnMoveRejectedToCallerOnly()
    {
        // Arrange
        GameSessionService sessionService = new();
        RecordingHubCallerClients clients = new();

        using GameHub hubX = CreateHub(sessionService, clients, PLAYER_X_ID);
        await hubX.JoinGame();

        using GameHub hubO = CreateHub(sessionService, clients, PLAYER_O_ID);
        await hubO.JoinGame();
        clients.Reset();

        // Act - Player O attempts to move out of turn
        MoveResultDto result = await hubO.MakeMove(new MakeMoveRequestDto(0, 0));

        // Assert
        Assert.False(result.IsAccepted);
        Assert.Equal("OutOfTurn", result.ErrorCode);
        Assert.Empty(clients.BroadcastStates);
        RecordingGameClient callerClient = clients.GetOrAddClient(PLAYER_O_ID);
        Assert.Single(callerClient.RejectedMoves);
        Assert.Equal("OutOfTurn", callerClient.RejectedMoves[0].ErrorCode);
    }

    [Fact]
    public async Task RequestRematch_DuplicateAndMutualVotes_DispatchesAssignmentsAndSingleBroadcastPerVote()
    {
        // Arrange
        GameSessionService sessionService = new();
        RecordingHubCallerClients clients = new();

        using GameHub hubX = CreateHub(sessionService, clients, PLAYER_X_ID);
        await hubX.JoinGame();

        using GameHub hubO = CreateHub(sessionService, clients, PLAYER_O_ID);
        await hubO.JoinGame();

        await hubX.MakeMove(new MakeMoveRequestDto(0, 0));
        await hubO.MakeMove(new MakeMoveRequestDto(1, 0));
        await hubX.MakeMove(new MakeMoveRequestDto(0, 1));
        await hubO.MakeMove(new MakeMoveRequestDto(1, 1));
        await hubX.MakeMove(new MakeMoveRequestDto(0, 2)); // X wins
        clients.Reset();

        // Act 1 - Player X votes once, then clicks a duplicate second time
        RematchResponseDto firstVote = await hubX.RequestRematch();
        RematchResponseDto duplicateVote = await hubX.RequestRematch();

        // Assert 1 - Only 1 broadcast occurred for the two calls by Player X
        Assert.True(firstVote.IsAccepted);
        Assert.False(firstVote.DidRematchStart);
        Assert.True(duplicateVote.IsAccepted);
        Assert.Single(clients.BroadcastStates);

        // Act 2 - Player O votes to start the rematch
        RematchResponseDto secondVote = await hubO.RequestRematch();

        // Assert 2 - Assignments swapped and sent to both players
        Assert.True(secondVote.DidRematchStart);
        Assert.Equal(2, clients.BroadcastStates.Count);

        RecordingGameClient formerOClient = clients.GetOrAddClient(PLAYER_O_ID);
        RecordingGameClient formerXClient = clients.GetOrAddClient(PLAYER_X_ID);

        Assert.Single(formerOClient.Assignments);
        Assert.Equal("X", formerOClient.Assignments[0].AssignedSymbol);
        Assert.Single(formerXClient.Assignments);
        Assert.Equal("O", formerXClient.Assignments[0].AssignedSymbol);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ActivePlayerDisconnects_SendsSingleAssignmentNotificationToRemainingPlayer()
    {
        // Arrange
        GameSessionService sessionService = new();
        RecordingHubCallerClients clients = new();

        using GameHub hubX = CreateHub(sessionService, clients, PLAYER_X_ID);
        await hubX.JoinGame();

        using GameHub hubO = CreateHub(sessionService, clients, PLAYER_O_ID);
        await hubO.JoinGame();
        clients.Reset();

        // Act
        await hubX.OnDisconnectedAsync(null);

        // Assert
        RecordingGameClient remainingClient = clients.GetOrAddClient(PLAYER_O_ID);
        Assert.Single(remainingClient.Assignments);
        Assert.Equal("X", remainingClient.Assignments[0].AssignedSymbol);
        Assert.Equal("AbortedByDisconnect", remainingClient.Assignments[0].GameState.Status);
        Assert.Empty(remainingClient.States);
    }

    [Fact]
    public async Task GetGameState_ReturnsCurrentSnapshot()
    {
        // Arrange
        GameSessionService sessionService = new();
        RecordingHubCallerClients clients = new();
        using GameHub hub = CreateHub(sessionService, clients, SPECTATOR_ID);

        // Act
        GameStateDto state = await hub.GetGameState();

        // Assert
        Assert.NotNull(state);
        Assert.Equal("WaitingForPlayers", state.Status);
    }

    private static GameHub CreateHub(
        IGameSessionService sessionService,
        RecordingHubCallerClients clients,
        string connectionId)
    {
        clients.ActiveCallerId = connectionId;
        GameHub hub = new(sessionService, NullLogger<GameHub>.Instance)
        {
            Clients = clients,
            Context = new TestHubCallerContext(connectionId)
        };

        return hub;
    }

    private sealed class RecordingGameClient : IGameClient
    {
        public List<GameStateDto> States { get; } = new();

        public List<PlayerAssignmentDto> Assignments { get; } = new();

        public List<MoveResultDto> RejectedMoves { get; } = new();

        public Task OnGameStateUpdated(GameStateDto gameState)
        {
            States.Add(gameState);
            return Task.CompletedTask;
        }

        public Task OnPlayerAssignmentChanged(PlayerAssignmentDto assignment)
        {
            Assignments.Add(assignment);
            return Task.CompletedTask;
        }

        public Task OnMoveRejected(MoveResultDto rejection)
        {
            RejectedMoves.Add(rejection);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHubCallerClients : IHubCallerClients<IGameClient>
    {
        private readonly Dictionary<string, RecordingGameClient> _clients = new();

        public string ActiveCallerId { get; set; } = string.Empty;

        public List<GameStateDto> BroadcastStates { get; } = new();

        public IGameClient Caller => GetOrAddClient(ActiveCallerId);

        public IGameClient Others => new RecordingGameClient();

        public IGameClient All => new RecordingGameClient();

        public RecordingGameClient GetOrAddClient(string connectionId)
        {
            if (!_clients.TryGetValue(connectionId, out RecordingGameClient? client))
            {
                client = new RecordingGameClient();
                _clients[connectionId] = client;
            }

            return client;
        }

        public void Reset()
        {
            BroadcastStates.Clear();
            foreach (RecordingGameClient client in _clients.Values)
            {
                client.States.Clear();
                client.Assignments.Clear();
                client.RejectedMoves.Clear();
            }
        }

        public IGameClient AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return new RecordingGameClient();
        }

        public IGameClient Client(string connectionId)
        {
            return GetOrAddClient(connectionId);
        }

        public IGameClient Clients(IReadOnlyList<string> connectionIds)
        {
            return new BroadcastRecordingClient(this, connectionIds);
        }

        public IGameClient Group(string groupName)
        {
            return new RecordingGameClient();
        }

        public IGameClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return new RecordingGameClient();
        }

        public IGameClient Groups(IReadOnlyList<string> groupNames)
        {
            return new RecordingGameClient();
        }

        public IGameClient OthersInGroup(string groupName)
        {
            return new RecordingGameClient();
        }

        public IGameClient User(string userId)
        {
            return new RecordingGameClient();
        }

        public IGameClient Users(IReadOnlyList<string> userIds)
        {
            return new RecordingGameClient();
        }

        private sealed class BroadcastRecordingClient : IGameClient
        {
            private readonly RecordingHubCallerClients _parent;
            private readonly IReadOnlyList<string> _connectionIds;

            public BroadcastRecordingClient(RecordingHubCallerClients parent, IReadOnlyList<string> connectionIds)
            {
                _parent = parent;
                _connectionIds = connectionIds;
            }

            public Task OnGameStateUpdated(GameStateDto gameState)
            {
                _parent.BroadcastStates.Add(gameState);
                foreach (string id in _connectionIds)
                {
                    _parent.GetOrAddClient(id).States.Add(gameState);
                }

                return Task.CompletedTask;
            }

            public Task OnPlayerAssignmentChanged(PlayerAssignmentDto assignment)
            {
                foreach (string id in _connectionIds)
                {
                    _parent.GetOrAddClient(id).Assignments.Add(assignment);
                }

                return Task.CompletedTask;
            }

            public Task OnMoveRejected(MoveResultDto rejection)
            {
                foreach (string id in _connectionIds)
                {
                    _parent.GetOrAddClient(id).RejectedMoves.Add(rejection);
                }

                return Task.CompletedTask;
            }
        }
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly Dictionary<object, object?> _items = new();
        private readonly FeatureCollection _features = new();

        public TestHubCallerContext(string connectionId)
        {
            _connectionId = connectionId;
        }

        public override string ConnectionId => _connectionId;

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User => null;

        public override IDictionary<object, object?> Items => _items;

        public override IFeatureCollection Features => _features;

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
