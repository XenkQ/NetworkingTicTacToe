import Phaser from 'phaser';
import { GameNetworkService } from './services/GameNetworkService';

export const gameNetworkService = new GameNetworkService();

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  width: 800,
  height: 600,
  backgroundColor: '#1a1a24',
  parent: 'game-container',
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH
  }
};

export const game = new Phaser.Game(config);
