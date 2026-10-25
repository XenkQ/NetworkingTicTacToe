/**
 * Player assignment event DTO synchronized with server/DTOs/PlayerAssignmentDto.cs.
 */

import type { PlayerSymbolType } from './GameDomainTypes';
import type { GameStateDto } from './GameStateDtos';

/**
 * Notification payload sent to an individual player when their assigned symbol changes.
 */
export interface PlayerAssignmentDto {
  readonly assignedSymbol: PlayerSymbolType;
  readonly gameState: GameStateDto;
}
