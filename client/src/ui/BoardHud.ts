import Phaser from 'phaser';
import type {
  ConnectionStateType,
  GameStateDto,
  PlayerSymbolType
} from '../types/GameContracts';

export interface BoardHudCallbacks {
  readonly onRematchClicked: () => void;
  readonly onRetryJoinClicked: () => void;
}

const TOP_BAR_Y = 28;
const CONNECTION_PILL_X = 135;
const CONNECTION_PILL_WIDTH = 210;
const CONNECTION_PILL_HEIGHT = 32;

const SYMBOL_BADGE_X = 665;
const SYMBOL_BADGE_WIDTH = 190;
const SYMBOL_BADGE_HEIGHT = 32;

const STATUS_BANNER_Y = 78;
const FEEDBACK_BANNER_Y = 112;

const ACTION_BUTTON_Y = 538;
const REMATCH_BUTTON_WIDTH = 260;
const RETRY_BUTTON_WIDTH = 220;
const ACTION_BUTTON_HEIGHT = 44;

/**
 * Manages the top HUD, status banners, feedback messages, and contextual action buttons.
 */
export class BoardHud {
  private readonly _scene: Phaser.Scene;
  private readonly _callbacks: BoardHudCallbacks;

  private readonly _connectionContainer: Phaser.GameObjects.Container;
  private readonly _connectionDot: Phaser.GameObjects.Arc;
  private readonly _connectionText: Phaser.GameObjects.Text;

  private readonly _symbolContainer: Phaser.GameObjects.Container;
  private readonly _symbolText: Phaser.GameObjects.Text;

  private readonly _statusText: Phaser.GameObjects.Text;
  private readonly _feedbackText: Phaser.GameObjects.Text;
  private _feedbackTimer: Phaser.Time.TimerEvent | null = null;

  private readonly _rematchContainer: Phaser.GameObjects.Container;
  private readonly _rematchBg: Phaser.GameObjects.Rectangle;
  private readonly _rematchText: Phaser.GameObjects.Text;
  private _isRematchEnabled = false;

  private readonly _retryContainer: Phaser.GameObjects.Container;
  private readonly _retryBg: Phaser.GameObjects.Rectangle;
  private readonly _retryText: Phaser.GameObjects.Text;

  private _connectionState: ConnectionStateType = 'Disconnected';

  constructor(scene: Phaser.Scene, callbacks: BoardHudCallbacks) {
    this._scene = scene;
    this._callbacks = callbacks;

    const pillBg = scene.add.rectangle(0, 0, CONNECTION_PILL_WIDTH, CONNECTION_PILL_HEIGHT, 0x1f2330)
      .setStrokeStyle(1, 0x3a4058);
    this._connectionDot = scene.add.circle(-80, 0, 5, 0xe74c3c);
    this._connectionText = scene.add.text(-65, 0, 'Disconnected', {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '12px',
      fontStyle: 'bold',
      color: '#e74c3c'
    }).setOrigin(0, 0.5);

    this._connectionContainer = scene.add.container(CONNECTION_PILL_X, TOP_BAR_Y, [
      pillBg,
      this._connectionDot,
      this._connectionText
    ]);

    const badgeBg = scene.add.rectangle(0, 0, SYMBOL_BADGE_WIDTH, SYMBOL_BADGE_HEIGHT, 0x1f2330)
      .setStrokeStyle(1, 0x3a4058);
    this._symbolText = scene.add.text(0, 0, 'Connecting...', {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '13px',
      fontStyle: 'bold',
      color: '#dfe6e9'
    }).setOrigin(0.5, 0.5);

    this._symbolContainer = scene.add.container(SYMBOL_BADGE_X, TOP_BAR_Y, [
      badgeBg,
      this._symbolText
    ]);

    this._statusText = scene.add.text(400, STATUS_BANNER_Y, 'Connecting to server...', {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '22px',
      fontStyle: 'bold',
      color: '#ffffff',
      align: 'center'
    }).setOrigin(0.5, 0.5);

    this._feedbackText = scene.add.text(400, FEEDBACK_BANNER_Y, '', {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '14px',
      fontStyle: 'normal',
      color: '#ff7675',
      align: 'center'
    }).setOrigin(0.5, 0.5);

    this._rematchBg = scene.add.rectangle(0, 0, REMATCH_BUTTON_WIDTH, ACTION_BUTTON_HEIGHT, 0x4834d4)
      .setStrokeStyle(2, 0x6c5ce7);
    this._rematchText = scene.add.text(0, 0, 'Accept Rematch', {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '15px',
      fontStyle: 'bold',
      color: '#ffffff'
    }).setOrigin(0.5, 0.5);

    this._rematchContainer = scene.add.container(400, ACTION_BUTTON_Y, [
      this._rematchBg,
      this._rematchText
    ]);
    this._rematchContainer.setSize(REMATCH_BUTTON_WIDTH, ACTION_BUTTON_HEIGHT);
    this._rematchContainer.setInteractive({ useHandCursor: true });
    this._rematchContainer.setVisible(false);

    this._rematchContainer.on(Phaser.Input.Events.POINTER_OVER, () => {
      if (this._isRematchEnabled) {
        this._rematchBg.setFillStyle(0x5a48e8);
        this._rematchContainer.setScale(1.02);
      }
    });

    this._rematchContainer.on(Phaser.Input.Events.POINTER_OUT, () => {
      if (this._isRematchEnabled) {
        this._rematchBg.setFillStyle(0x4834d4);
      }
      this._rematchContainer.setScale(1.0);
    });

    this._rematchContainer.on(Phaser.Input.Events.POINTER_DOWN, () => {
      if (this._isRematchEnabled) {
        this._callbacks.onRematchClicked();
      }
    });

    this._retryBg = scene.add.rectangle(0, 0, RETRY_BUTTON_WIDTH, ACTION_BUTTON_HEIGHT, 0x00b894)
      .setStrokeStyle(2, 0x55efc4);
    this._retryText = scene.add.text(0, 0, 'Retry Join', {
      fontFamily: 'Arial, Helvetica, sans-serif',
      fontSize: '15px',
      fontStyle: 'bold',
      color: '#ffffff'
    }).setOrigin(0.5, 0.5);

    this._retryContainer = scene.add.container(400, ACTION_BUTTON_Y, [
      this._retryBg,
      this._retryText
    ]);
    this._retryContainer.setSize(RETRY_BUTTON_WIDTH, ACTION_BUTTON_HEIGHT);
    this._retryContainer.setInteractive({ useHandCursor: true });
    this._retryContainer.setVisible(false);

    this._retryContainer.on(Phaser.Input.Events.POINTER_OVER, () => {
      this._retryBg.setFillStyle(0x00d2a0);
      this._retryContainer.setScale(1.02);
    });

    this._retryContainer.on(Phaser.Input.Events.POINTER_OUT, () => {
      this._retryBg.setFillStyle(0x00b894);
      this._retryContainer.setScale(1.0);
    });

    this._retryContainer.on(Phaser.Input.Events.POINTER_DOWN, () => {
      this._callbacks.onRetryJoinClicked();
    });
  }

  public updateConnectionStatus(state: ConnectionStateType): void {
    this._connectionState = state;

    switch (state) {
      case 'Connected':
        this._connectionDot.setFillStyle(0x2ecc71);
        this._connectionText.setText('Connected (LAN)');
        this._connectionText.setColor('#2ecc71');
        break;
      case 'Connecting':
        this._connectionDot.setFillStyle(0xf39c12);
        this._connectionText.setText('Connecting...');
        this._connectionText.setColor('#f39c12');
        break;
      case 'Reconnecting':
        this._connectionDot.setFillStyle(0xf39c12);
        this._connectionText.setText('Reconnecting...');
        this._connectionText.setColor('#f39c12');
        break;
      case 'Disconnected':
        this._connectionDot.setFillStyle(0xe74c3c);
        this._connectionText.setText('Disconnected');
        this._connectionText.setColor('#e74c3c');
        break;
    }
  }

  public updateGameState(
    state: GameStateDto,
    localSymbol: PlayerSymbolType | null,
    isJoined: boolean
  ): void {
    if (!isJoined) {
      this._symbolText.setText('Spectator (Room Busy)');
      this._symbolText.setColor('#a0a0b0');
    } else if (localSymbol === 'X') {
      this._symbolText.setText('Your Symbol: X');
      this._symbolText.setColor('#ff5964');
    } else if (localSymbol === 'O') {
      this._symbolText.setText('Your Symbol: O');
      this._symbolText.setColor('#35a7ff');
    } else {
      this._symbolText.setText('Assigning Symbol...');
      this._symbolText.setColor('#dfe6e9');
    }

    let bannerMessage = '';
    let bannerColor = '#ffffff';

    if (!isJoined) {
      bannerMessage = state.status === 'InProgress'
        ? 'Game In Progress (Spectating)'
        : 'Game is currently full (2/2 players active)';
      bannerColor = '#a0a0b0';
    } else {
      switch (state.status) {
        case 'WaitingForPlayers':
          bannerMessage = 'Waiting for opponent...';
          bannerColor = '#b2bec3';
          break;
        case 'AbortedByDisconnect':
          bannerMessage = 'Opponent disconnected. Waiting for a new opponent...';
          bannerColor = '#fdcb6e';
          break;
        case 'InProgress':
          if (state.currentTurn === localSymbol) {
            bannerMessage = `Your Turn (${localSymbol})`;
            bannerColor = localSymbol === 'X' ? '#ff7675' : '#74b9ff';
          } else if (state.currentTurn !== null) {
            bannerMessage = `Opponent's Turn (${state.currentTurn})`;
            bannerColor = '#dfe6e9';
          } else {
            bannerMessage = 'Match In Progress';
            bannerColor = '#dfe6e9';
          }
          break;
        case 'Won':
          if (state.winner === localSymbol) {
            bannerMessage = `You Win! (${state.winner})`;
            bannerColor = '#ffe066';
          } else if (state.winner !== null) {
            bannerMessage = `Opponent Wins (${state.winner})`;
            bannerColor = '#ff7675';
          } else {
            bannerMessage = 'Match Won!';
            bannerColor = '#ffe066';
          }
          break;
        case 'Draw':
          bannerMessage = 'Match Draw!';
          bannerColor = '#dfe6e9';
          break;
      }
    }

    this._statusText.setText(bannerMessage);
    this._statusText.setColor(bannerColor);

    const isGameOver = state.status === 'Won' || state.status === 'Draw';

    if (!isJoined) {
      this._rematchContainer.setVisible(false);
      this._retryContainer.setVisible(true);
    } else if (this._connectionState === 'Disconnected') {
      this._rematchContainer.setVisible(false);
      this._retryContainer.setVisible(true);
    } else if (isGameOver && localSymbol !== null) {
      this._retryContainer.setVisible(false);
      this._rematchContainer.setVisible(true);

      const localAccepted = localSymbol === 'X'
        ? state.hasPlayerXAcceptedRematch
        : state.hasPlayerOAcceptedRematch;

      const opponentAccepted = localSymbol === 'X'
        ? state.hasPlayerOAcceptedRematch
        : state.hasPlayerXAcceptedRematch;

      if (localAccepted) {
        this._isRematchEnabled = false;
        this._rematchText.setText('Waiting for Opponent (1/2)...');
        this._rematchBg.setFillStyle(0x2d3436);
        this._rematchBg.setStrokeStyle(2, 0x636e72);
      } else {
        this._isRematchEnabled = true;
        if (opponentAccepted) {
          this._rematchText.setText('Accept Rematch (1/2 Ready)');
        } else {
          this._rematchText.setText('Accept Rematch');
        }
        this._rematchBg.setFillStyle(0x4834d4);
        this._rematchBg.setStrokeStyle(2, 0x6c5ce7);
      }
    } else {
      this._rematchContainer.setVisible(false);
      this._retryContainer.setVisible(false);
    }
  }

  public showFeedback(message: string, isError = false): void {
    this._feedbackText.setText(message);
    this._feedbackText.setColor(isError ? '#ff7675' : '#74b9ff');
    this._feedbackText.setAlpha(1);

    if (this._feedbackTimer !== null) {
      this._feedbackTimer.remove();
    }

    this._feedbackTimer = this._scene.time.delayedCall(4000, () => {
      this.clearFeedback();
    });
  }

  public clearFeedback(): void {
    if (this._feedbackTimer !== null) {
      this._feedbackTimer.remove();
      this._feedbackTimer = null;
    }
    this._feedbackText.setText('');
  }

  public destroy(): void {
    this.clearFeedback();
    this._connectionContainer.destroy();
    this._symbolContainer.destroy();
    this._statusText.destroy();
    this._feedbackText.destroy();
    this._rematchContainer.destroy();
    this._retryContainer.destroy();
  }
}
