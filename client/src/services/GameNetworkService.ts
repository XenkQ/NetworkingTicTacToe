import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import type {
  ConnectionStateType,
  GameStateDto,
  JoinGameResponseDto,
  MakeMoveRequestDto,
  MoveResultDto,
  PlayerAssignmentDto,
  PlayerSymbolType,
  RematchResponseDto
} from '../types';

const DEFAULT_HUB_URL = '/hubs/game';
const RECONNECT_INTERVALS_MS = [0, 1000, 2000, 5000, 10000];

/**
 * Service contract for SignalR transport and real-time game coordination.
 */
export interface IGameNetworkService {
  readonly connectionState: ConnectionStateType;
  readonly assignedSymbol: PlayerSymbolType | null;
  readonly latestGameState: GameStateDto | null;
  readonly isJoined: boolean;

  connectAndJoinAsync(): Promise<JoinGameResponseDto>;
  retryJoinAsync(): Promise<JoinGameResponseDto>;
  makeMoveAsync(row: number, col: number): Promise<MoveResultDto>;
  requestRematchAsync(): Promise<RematchResponseDto>;
  getGameStateAsync(): Promise<GameStateDto>;
  disconnectAsync(): Promise<void>;

  onGameStateUpdated(listener: (state: GameStateDto) => void): () => void;
  offGameStateUpdated(listener: (state: GameStateDto) => void): void;
  onPlayerAssignmentChanged(listener: (assignment: PlayerAssignmentDto) => void): () => void;
  offPlayerAssignmentChanged(listener: (assignment: PlayerAssignmentDto) => void): void;
  onMoveRejected(listener: (rejection: MoveResultDto) => void): () => void;
  offMoveRejected(listener: (rejection: MoveResultDto) => void): void;
  onConnectionStateChanged(listener: (state: ConnectionStateType) => void): () => void;
  offConnectionStateChanged(listener: (state: ConnectionStateType) => void): void;
  onSessionJoined(listener: (response: JoinGameResponseDto) => void): () => void;
  offSessionJoined(listener: (response: JoinGameResponseDto) => void): void;
}

/**
 * Encapsulates SignalR WebSocket transport, automatic reconnection, and typed RPC/event dispatch.
 */
export class GameNetworkService implements IGameNetworkService {
  private readonly _connection: HubConnection;
  private _connectionState: ConnectionStateType = 'Disconnected';
  private _assignedSymbol: PlayerSymbolType | null = null;
  private _latestGameState: GameStateDto | null = null;
  private _latestAssignmentVersion = 0;
  private _isJoined = false;

  private readonly _gameStateListeners = new Set<(state: GameStateDto) => void>();
  private readonly _assignmentListeners = new Set<(assignment: PlayerAssignmentDto) => void>();
  private readonly _rejectionListeners = new Set<(rejection: MoveResultDto) => void>();
  private readonly _connectionStateListeners = new Set<(state: ConnectionStateType) => void>();
  private readonly _sessionJoinedListeners = new Set<(response: JoinGameResponseDto) => void>();

  constructor(hubUrl: string = DEFAULT_HUB_URL) {
    this._connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect(RECONNECT_INTERVALS_MS)
      .configureLogging(LogLevel.Information)
      .build();

    this.registerSignalREventHandlers();
  }

  public get connectionState(): ConnectionStateType {
    return this._connectionState;
  }

  public get assignedSymbol(): PlayerSymbolType | null {
    return this._assignedSymbol;
  }

  public get latestGameState(): GameStateDto | null {
    return this._latestGameState;
  }

  public get isJoined(): boolean {
    return this._isJoined;
  }

  public async connectAndJoinAsync(): Promise<JoinGameResponseDto> {
    await this.ensureConnectedAsync();

    const response = await this._connection.invoke<JoinGameResponseDto>('JoinGame');
    this._isJoined = response.isJoined;
    this._assignedSymbol = response.assignedSymbol;
    this._latestAssignmentVersion = response.gameState.stateVersion;
    this.tryUpdateLatestGameState(response.gameState);

    const effectiveResponse: JoinGameResponseDto = {
      ...response,
      gameState: this._latestGameState ?? response.gameState
    };
    this.dispatchSessionJoined(effectiveResponse);

    return effectiveResponse;
  }

  public async retryJoinAsync(): Promise<JoinGameResponseDto> {
    return this.connectAndJoinAsync();
  }

  public async makeMoveAsync(row: number, col: number): Promise<MoveResultDto> {
    await this.ensureConnectedAsync();

    const request: MakeMoveRequestDto = { row, col };
    const result = await this._connection.invoke<MoveResultDto>('MakeMove', request);

    if (result.isAccepted) {
      this.tryUpdateLatestGameState(result.gameState);
    }

    return result;
  }

  public async requestRematchAsync(): Promise<RematchResponseDto> {
    await this.ensureConnectedAsync();

    const response = await this._connection.invoke<RematchResponseDto>('RequestRematch');
    this.tryUpdateLatestGameState(response.gameState);

    return response;
  }

  public async getGameStateAsync(): Promise<GameStateDto> {
    await this.ensureConnectedAsync();

    const state = await this._connection.invoke<GameStateDto>('GetGameState');
    if (this.tryUpdateLatestGameState(state)) {
      this.dispatchGameStateUpdated(state);
    }

    return state;
  }

  public async disconnectAsync(): Promise<void> {
    if (this._connection.state !== HubConnectionState.Disconnected) {
      await this._connection.stop();
      this.resetSessionState();
      this.setConnectionState('Disconnected');
    }
  }

  public onGameStateUpdated(listener: (state: GameStateDto) => void): () => void {
    this._gameStateListeners.add(listener);
    return () => {
      this.offGameStateUpdated(listener);
    };
  }

  public offGameStateUpdated(listener: (state: GameStateDto) => void): void {
    this._gameStateListeners.delete(listener);
  }

  public onPlayerAssignmentChanged(listener: (assignment: PlayerAssignmentDto) => void): () => void {
    this._assignmentListeners.add(listener);
    return () => {
      this.offPlayerAssignmentChanged(listener);
    };
  }

  public offPlayerAssignmentChanged(listener: (assignment: PlayerAssignmentDto) => void): void {
    this._assignmentListeners.delete(listener);
  }

  public onMoveRejected(listener: (rejection: MoveResultDto) => void): () => void {
    this._rejectionListeners.add(listener);
    return () => {
      this.offMoveRejected(listener);
    };
  }

  public offMoveRejected(listener: (rejection: MoveResultDto) => void): void {
    this._rejectionListeners.delete(listener);
  }

  public onConnectionStateChanged(listener: (state: ConnectionStateType) => void): () => void {
    this._connectionStateListeners.add(listener);
    return () => {
      this.offConnectionStateChanged(listener);
    };
  }

  public offConnectionStateChanged(listener: (state: ConnectionStateType) => void): void {
    this._connectionStateListeners.delete(listener);
  }

  public onSessionJoined(listener: (response: JoinGameResponseDto) => void): () => void {
    this._sessionJoinedListeners.add(listener);
    return () => {
      this.offSessionJoined(listener);
    };
  }

  public offSessionJoined(listener: (response: JoinGameResponseDto) => void): void {
    this._sessionJoinedListeners.delete(listener);
  }

  private registerSignalREventHandlers(): void {
    this._connection.on('OnGameStateUpdated', (gameState: GameStateDto) => {
      if (this.tryUpdateLatestGameState(gameState)) {
        this.dispatchGameStateUpdated(gameState);
      }
    });

    this._connection.on('OnPlayerAssignmentChanged', (assignment: PlayerAssignmentDto) => {
      if (assignment.gameState.stateVersion < this._latestAssignmentVersion) {
        return;
      }

      this._latestAssignmentVersion = assignment.gameState.stateVersion;
      this._assignedSymbol = assignment.assignedSymbol;
      this.tryUpdateLatestGameState(assignment.gameState);

      const effectiveState = this._latestGameState ?? assignment.gameState;
      this.dispatchPlayerAssignmentChanged({
        assignedSymbol: assignment.assignedSymbol,
        gameState: effectiveState
      });
    });

    this._connection.on('OnMoveRejected', (rejection: MoveResultDto) => {
      this.dispatchMoveRejected(rejection);
    });

    this._connection.onreconnecting((_error?: Error) => {
      this.resetSessionState();
      this.setConnectionState('Reconnecting');
    });

    this._connection.onreconnected((_connectionId?: string) => {
      this.setConnectionState('Connected');
      this.rejoinAfterReconnect();
    });

    this._connection.onclose((_error?: Error) => {
      this.resetSessionState();
      this.setConnectionState('Disconnected');
    });
  }

  private resetSessionState(): void {
    this._isJoined = false;
    this._assignedSymbol = null;
    this._latestGameState = null;
    this._latestAssignmentVersion = 0;
  }

  private tryUpdateLatestGameState(incomingState: GameStateDto): boolean {
    if (
      this._latestGameState !== null &&
      incomingState.stateVersion < this._latestGameState.stateVersion
    ) {
      return false;
    }

    this._latestGameState = incomingState;
    return true;
  }

  private async ensureConnectedAsync(): Promise<void> {
    if (this._connection.state === HubConnectionState.Connected) {
      return;
    }

    if (this._connection.state === HubConnectionState.Disconnecting) {
      await this.waitForStateAsync(HubConnectionState.Disconnected);
    }

    if (this._connection.state === HubConnectionState.Disconnected) {
      this.setConnectionState('Connecting');
      try {
        await this._connection.start();
        this.setConnectionState('Connected');
      } catch (error: unknown) {
        this.setConnectionState('Disconnected');
        throw error;
      }
    } else if (
      this._connection.state === HubConnectionState.Connecting ||
      this._connection.state === HubConnectionState.Reconnecting
    ) {
      await this.waitForStateAsync(HubConnectionState.Connected);
    }
  }

  private async waitForStateAsync(targetState: HubConnectionState, timeoutMs = 5000): Promise<void> {
    const pollIntervalMs = 50;
    const maxAttempts = Math.ceil(timeoutMs / pollIntervalMs);
    let attempts = 0;

    while (this._connection.state !== targetState && attempts < maxAttempts) {
      await new Promise((resolve) => {
        setTimeout(resolve, pollIntervalMs);
      });
      attempts++;
    }

    if (this._connection.state !== targetState) {
      throw new Error(`Timed out waiting for HubConnection state ${targetState}. Current state: ${this._connection.state}`);
    }
  }

  private setConnectionState(newState: ConnectionStateType): void {
    if (this._connectionState !== newState) {
      this._connectionState = newState;
      this.dispatchConnectionStateChanged(newState);
    }
  }

  private rejoinAfterReconnect(): void {
    this.connectAndJoinAsync().catch((error: unknown) => {
      this.resetSessionState();
      console.warn('Failed to re-join game session after reconnection:', error);
      this.setConnectionState('Disconnected');
    });
  }

  private dispatchGameStateUpdated(state: GameStateDto): void {
    for (const listener of this._gameStateListeners) {
      try {
        listener(state);
      } catch (error: unknown) {
        console.error('Error executing OnGameStateUpdated listener:', error);
      }
    }
  }

  private dispatchPlayerAssignmentChanged(assignment: PlayerAssignmentDto): void {
    for (const listener of this._assignmentListeners) {
      try {
        listener(assignment);
      } catch (error: unknown) {
        console.error('Error executing OnPlayerAssignmentChanged listener:', error);
      }
    }
  }

  private dispatchMoveRejected(rejection: MoveResultDto): void {
    for (const listener of this._rejectionListeners) {
      try {
        listener(rejection);
      } catch (error: unknown) {
        console.error('Error executing OnMoveRejected listener:', error);
      }
    }
  }

  private dispatchConnectionStateChanged(state: ConnectionStateType): void {
    for (const listener of this._connectionStateListeners) {
      try {
        listener(state);
      } catch (error: unknown) {
        console.error('Error executing OnConnectionStateChanged listener:', error);
      }
    }
  }

  private dispatchSessionJoined(response: JoinGameResponseDto): void {
    for (const listener of this._sessionJoinedListeners) {
      try {
        listener(response);
      } catch (error: unknown) {
        console.error('Error executing OnSessionJoined listener:', error);
      }
    }
  }
}
