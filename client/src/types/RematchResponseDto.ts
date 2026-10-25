/**
 * Rematch response DTO synchronized with server/DTOs/RematchResponseDto.cs.
 */

import type { GameStateDto } from './GameStateDtos';

/**
 * Response payload returned when invoking RequestRematch RPC.
 */
export interface RematchResponseDto {
  readonly isAccepted: boolean;
  readonly errorMessage: string | null;
  readonly didRematchStart: boolean;
  readonly gameState: GameStateDto;
}
