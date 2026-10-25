import Phaser from 'phaser';
import { LOGICAL_HEIGHT, LOGICAL_WIDTH, RENDER_RESOLUTION } from './config/displayConfig';
import { GameNetworkService } from './services/GameNetworkService';
import { BoardScene } from './scenes/BoardScene';

export const gameNetworkService = new GameNetworkService();

const boardScene = new BoardScene(gameNetworkService);

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  width: LOGICAL_WIDTH * RENDER_RESOLUTION,
  height: LOGICAL_HEIGHT * RENDER_RESOLUTION,
  backgroundColor: '#1a1a24',
  parent: 'game-container',
  roundPixels: true,
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH
  },
  scene: [boardScene]
};

export const game = new Phaser.Game(config);
