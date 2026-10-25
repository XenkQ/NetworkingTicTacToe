/**
 * Move request and evaluation result DTOs synchronized with server/DTOs/MoveDtos.cs.
 */

import type { MoveErrorCodeType, PlayerSymbolType } from './GameDomainTypes';
import type { GameStateDto } from './GameStateDtos';

/**
 * Move request payload dispatched when clicking a board cell.
 */
export interface MakeMoveRequestDto {
  readonly row: number;
  readonly col: number;
}

/**
 * Result payload returned and broadcast when a move attempt is evaluated.
 */
export interface MoveResultDto {
  readonly isAccepted: boolean;
  readonly errorCode: MoveErrorCodeType;
  readonly errorMessage: string | null;
  readonly row: number;
  readonly col: number;
  readonly symbol: PlayerSymbolType | null;
  readonly gameState: GameStateDto;
}
