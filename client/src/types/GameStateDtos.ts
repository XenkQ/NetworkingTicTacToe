/**
 * Board coordinate, winning line, and authoritative match state DTOs
 * synchronized with server/DTOs/GameStateDtos.cs.
 */

import type { CellValue, GameStatusType, PlayerSymbolType } from './GameDomainTypes';

/**
 * Coordinate pair for a cell on the 3x3 board.
 */
export interface BoardCellCoordinateDto {
  readonly row: number;
  readonly col: number;
}

/**
 * Winning line coordinates containing the 3 winning cell positions.
 */
export interface WinningLineDto {
  readonly cells: readonly BoardCellCoordinateDto[];
}

/**
 * Authoritative snapshot of the match state received from the server.
 */
export interface GameStateDto {
  readonly stateVersion: number;
  readonly cells: readonly (readonly CellValue[])[];
  readonly status: GameStatusType;
  readonly currentTurn: PlayerSymbolType | null;
  readonly winner: PlayerSymbolType | null;
  readonly winningLine: WinningLineDto | null;
  readonly isPlayerXConnected: boolean;
  readonly isPlayerOConnected: boolean;
  readonly hasPlayerXAcceptedRematch: boolean;
  readonly hasPlayerOAcceptedRematch: boolean;
  readonly statusMessage: string;
}
