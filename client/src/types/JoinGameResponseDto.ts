/**
 * Join game response DTO synchronized with server/DTOs/JoinGameResponseDto.cs.
 */

import type { PlayerSymbolType } from './GameDomainTypes';
import type { GameStateDto } from './GameStateDtos';

/**
 * Response payload returned when invoking JoinGame RPC.
 */
export interface JoinGameResponseDto {
  readonly isJoined: boolean;
  readonly assignedSymbol: PlayerSymbolType | null;
  readonly rejectionReason: string | null;
  readonly gameState: GameStateDto;
}
