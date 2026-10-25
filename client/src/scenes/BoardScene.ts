import Phaser from 'phaser';
import type {
  ConnectionStateType,
  GameStateDto,
  MoveResultDto,
  PlayerAssignmentDto,
  PlayerSymbolType,
  WinningLineDto
} from '../types/GameContracts';
import type { IGameNetworkService } from '../services/GameNetworkService';
import { BoardHud } from '../ui/BoardHud';

const CELL_SIZE = 110;
const CELL_GAP = 12;
const GRID_CENTER_X = 400;
const GRID_CENTER_Y = 310;

const COL_CENTERS = [
  GRID_CENTER_X - CELL_SIZE - CELL_GAP,
  GRID_CENTER_X,
  GRID_CENTER_X + CELL_SIZE + CELL_GAP
] as const;

const ROW_CENTERS = [
  GRID_CENTER_Y - CELL_SIZE - CELL_GAP,
  GRID_CENTER_Y,
  GRID_CENTER_Y + CELL_SIZE + CELL_GAP
] as const;

/**
 * Main Phaser Scene managing the 3x3 grid, symbol tweens, winning line highlights, and input.
 */
export class BoardScene extends Phaser.Scene {
  private readonly _networkService: IGameNetworkService;
  private _hud: BoardHud | null = null;

  private _symbols: (Phaser.GameObjects.Text | null)[][] = [
    [null, null, null],
    [null, null, null],
    [null, null, null]
  ];
  private _cellBgs: Phaser.GameObjects.Rectangle[][] = [];
  private _cellZones: Phaser.GameObjects.Zone[][] = [];
  private _winningLineGraphics: Phaser.GameObjects.Graphics | null = null;

  private _isMovePending = false;
  private _latestState: GameStateDto | null = null;
  private _localSymbol: PlayerSymbolType | null = null;
  private _isJoined = false;

  constructor(networkService: IGameNetworkService) {
    super({ key: 'BoardScene' });
    this._networkService = networkService;
  }

  public init(): void {
    this._symbols = [
      [null, null, null],
      [null, null, null],
      [null, null, null]
    ];
    this._cellBgs = [];
    this._cellZones = [];
    this._winningLineGraphics = null;
    this._isMovePending = false;
    this._latestState = null;
    this._localSymbol = null;
    this._isJoined = false;
  }

  public create(): void {
    this.createGrid();

    this._hud = new BoardHud(this, {
      onRematchClicked: () => {
        this.handleRematchClick();
      },
      onRetryJoinClicked: () => {
        this.handleRetryJoinClick();
      }
    });

    const unsubState = this._networkService.onGameStateUpdated((state: GameStateDto) => {
      this.handleGameStateUpdated(state);
    });

    const unsubAssign = this._networkService.onPlayerAssignmentChanged((assignment: PlayerAssignmentDto) => {
      this.handlePlayerAssignmentChanged(assignment);
    });

    const unsubReject = this._networkService.onMoveRejected((rejection: MoveResultDto) => {
      this.handleMoveRejected(rejection);
    });

    const unsubConn = this._networkService.onConnectionStateChanged((state: ConnectionStateType) => {
      this.handleConnectionStateChanged(state);
    });

    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
      unsubState();
      unsubAssign();
      unsubReject();
      unsubConn();
      this.tweens.killAll();
      this.time.removeAllEvents();
      this._hud?.destroy();
      this._hud = null;
    });

    void this.connectAndJoin();
  }

  private createGrid(): void {
    this._cellBgs = [];
    this._cellZones = [];

    for (let r = 0; r < 3; r++) {
      this._cellBgs[r] = [];
      this._cellZones[r] = [];

      for (let c = 0; c < 3; c++) {
        const x = COL_CENTERS[c];
        const y = ROW_CENTERS[r];

        const bg = this.add.rectangle(x, y, CELL_SIZE, CELL_SIZE, 0x252538)
          .setStrokeStyle(2, 0x3d3d54);
        this._cellBgs[r][c] = bg;

        const zone = this.add.zone(x, y, CELL_SIZE, CELL_SIZE)
          .setInteractive({ useHandCursor: true });

        zone.on(Phaser.Input.Events.POINTER_OVER, () => {
          const canHover = this._isJoined &&
            this._localSymbol !== null &&
            this._latestState?.status === 'InProgress' &&
            this._latestState.currentTurn === this._localSymbol &&
            this._latestState.cells[r][c] === 'Empty' &&
            !this._isMovePending;

          if (canHover) {
            this.highlightCell(r, c, true);
          }
        });

        zone.on(Phaser.Input.Events.POINTER_OUT, () => {
          this.highlightCell(r, c, false);
        });

        zone.on(Phaser.Input.Events.POINTER_DOWN, () => {
          this.handleCellClick(r, c);
        });

        this._cellZones[r][c] = zone;
      }
    }
  }

  private handleCellClick(row: number, col: number): void {
    if (this._isMovePending) {
      return;
    }

    if (!this._isJoined || this._localSymbol === null) {
      this._hud?.showFeedback('You are spectating. Only active players can make moves.', true);
      return;
    }

    if (this._latestState?.status !== 'InProgress') {
      return;
    }

    if (this._latestState.currentTurn !== this._localSymbol) {
      this._hud?.showFeedback("It is not your turn!", true);
      return;
    }

    if (this._latestState.cells[row][col] !== 'Empty') {
      this._hud?.showFeedback('Cell is already occupied!', true);
      return;
    }

    this._isMovePending = true;
    this._hud?.clearFeedback();
    this.highlightCell(row, col, false);

    this._networkService.makeMoveAsync(row, col)
      .then((result) => {
        this._isMovePending = false;
        if (!result.isAccepted) {
          this._hud?.showFeedback(result.errorMessage ?? 'Move was rejected', true);
        }
      })
      .catch((error: unknown) => {
        this._isMovePending = false;
        const message = error instanceof Error ? error.message : 'Network error making move';
        this._hud?.showFeedback(message, true);
      });
  }

  private applyGameState(state: GameStateDto): void {
    for (let r = 0; r < 3; r++) {
      for (let c = 0; c < 3; c++) {
        const cellValue = state.cells[r][c];
        const existing = this._symbols[r][c];

        if (cellValue === 'Empty') {
          if (existing !== null) {
            existing.destroy();
            this._symbols[r][c] = null;
          }
        } else {
          if (existing === null) {
            this.renderSymbolWithAnimation(r, c, cellValue);
          } else if (existing.text !== cellValue) {
            existing.destroy();
            this.renderSymbolWithAnimation(r, c, cellValue);
          }
        }
      }
    }

    if (state.status === 'Won' && state.winningLine && state.winningLine.cells.length >= 3) {
      if (this._winningLineGraphics === null) {
        this.renderWinningLine(state.winningLine);
      }
    } else {
      this.clearWinningLine();
    }

    this._hud?.updateGameState(state, this._localSymbol, this._isJoined);
  }

  private renderSymbolWithAnimation(row: number, col: number, symbol: 'X' | 'O'): void {
    const x = COL_CENTERS[col];
    const y = ROW_CENTERS[row];
    const color = symbol === 'X' ? '#ff5964' : '#35a7ff';

    const text = this.add.text(x, y, symbol, {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '68px',
      fontStyle: 'bold',
      color
    });
    text.setOrigin(0.5, 0.5);
    text.setScale(0);

    this.tweens.add({
      targets: text,
      scale: 1,
      duration: 220,
      ease: 'Back.easeOut'
    });

    this._symbols[row][col] = text;
  }

  private renderWinningLine(winningLine: WinningLineDto): void {
    this.clearWinningLine();

    const c0 = winningLine.cells[0];
    const c2 = winningLine.cells[2];

    const x1 = COL_CENTERS[c0.col];
    const y1 = ROW_CENTERS[c0.row];
    const x2 = COL_CENTERS[c2.col];
    const y2 = ROW_CENTERS[c2.row];

    const dx = x2 - x1;
    const dy = y2 - y1;
    const dist = Math.hypot(dx, dy);
    const ext = 35;

    const startX = dist > 0 ? x1 - (dx / dist) * ext : x1;
    const startY = dist > 0 ? y1 - (dy / dist) * ext : y1;
    const endX = dist > 0 ? x2 + (dx / dist) * ext : x2;
    const endY = dist > 0 ? y2 + (dy / dist) * ext : y2;

    const graphics = this.add.graphics();
    graphics.lineStyle(8, 0xffe066, 1.0);
    graphics.beginPath();
    graphics.moveTo(startX, startY);
    graphics.lineTo(endX, endY);
    graphics.strokePath();

    graphics.setAlpha(0);
    this.tweens.add({
      targets: graphics,
      alpha: 1,
      duration: 250,
      ease: 'Sine.easeOut'
    });

    this._winningLineGraphics = graphics;

    for (const cell of winningLine.cells) {
      if (cell.row >= 0 && cell.row < 3 && cell.col >= 0 && cell.col < 3) {
        this._cellBgs[cell.row][cell.col]?.setStrokeStyle(3, 0xffe066);
      }
    }
  }

  private clearWinningLine(): void {
    if (this._winningLineGraphics !== null) {
      this._winningLineGraphics.destroy();
      this._winningLineGraphics = null;
    }

    for (let r = 0; r < 3; r++) {
      for (let c = 0; c < 3; c++) {
        this._cellBgs[r]?.[c]?.setStrokeStyle(2, 0x3d3d54);
      }
    }
  }

  private highlightCell(row: number, col: number, isHovered: boolean): void {
    const cellBg = this._cellBgs[row]?.[col];
    if (!cellBg) {
      return;
    }

    if (this._winningLineGraphics !== null && this._latestState?.winningLine) {
      const isWinningCell = this._latestState.winningLine.cells.some(
        (coord) => coord.row === row && coord.col === col
      );
      if (isWinningCell) {
        return;
      }
    }

    if (isHovered) {
      cellBg.setFillStyle(0x34344e);
      cellBg.setStrokeStyle(2, 0x5c5c8a);
    } else {
      cellBg.setFillStyle(0x252538);
      cellBg.setStrokeStyle(2, 0x3d3d54);
    }
  }

  private handleGameStateUpdated(state: GameStateDto): void {
    this._latestState = state;
    this.applyGameState(state);
  }

  private handlePlayerAssignmentChanged(assignment: PlayerAssignmentDto): void {
    this._localSymbol = assignment.assignedSymbol;
    this._latestState = assignment.gameState;
    this.applyGameState(assignment.gameState);
  }

  private handleMoveRejected(rejection: MoveResultDto): void {
    this._isMovePending = false;
    this._hud?.showFeedback(rejection.errorMessage ?? 'Move was rejected', true);
  }

  private handleConnectionStateChanged(state: ConnectionStateType): void {
    this._hud?.updateConnectionStatus(state);
  }

  private handleRematchClick(): void {
    this._hud?.showFeedback('Requesting rematch...', false);
    this._networkService.requestRematchAsync()
      .then((response) => {
        this._latestState = response.gameState;
        this.applyGameState(response.gameState);
        if (!response.isAccepted) {
          this._hud?.showFeedback(response.errorMessage ?? 'Rematch request rejected', true);
        }
      })
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : 'Failed to request rematch';
        this._hud?.showFeedback(message, true);
      });
  }

  private handleRetryJoinClick(): void {
    this._hud?.showFeedback('Retrying join...', false);
    this._networkService.retryJoinAsync()
      .then((response) => {
        this._isJoined = response.isJoined;
        this._localSymbol = response.assignedSymbol;
        this._latestState = response.gameState;
        this.applyGameState(response.gameState);

        if (!response.isJoined && response.rejectionReason) {
          this._hud?.showFeedback(response.rejectionReason, true);
        } else {
          this._hud?.clearFeedback();
        }
      })
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : 'Failed to connect/join';
        this._hud?.showFeedback(message, true);
      });
  }

  private async connectAndJoin(): Promise<void> {
    this._hud?.updateConnectionStatus('Connecting');
    this._hud?.showFeedback('Connecting to server...', false);

    try {
      const response = await this._networkService.connectAndJoinAsync();
      this._isJoined = response.isJoined;
      this._localSymbol = response.assignedSymbol;
      this._latestState = response.gameState;
      this.applyGameState(response.gameState);

      if (!response.isJoined && response.rejectionReason) {
        this._hud?.showFeedback(response.rejectionReason, true);
      } else {
        this._hud?.clearFeedback();
      }
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Unable to connect to game server';
      this._hud?.showFeedback(message, true);
      this._hud?.updateConnectionStatus('Disconnected');
    }
  }
}
