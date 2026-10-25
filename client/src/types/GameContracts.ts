/**
 * Pure domain types and SignalR DTO contracts synchronized with server/DTOs/GameContracts.cs.
 */

export type CellValue = 'Empty' | 'X' | 'O';

export type PlayerSymbolType = 'X' | 'O';

export type GameStatusType =
  | 'WaitingForPlayers'
  | 'InProgress'
  | 'Won'
  | 'Draw'
  | 'AbortedByDisconnect';

export type MoveErrorCodeType =
  | 'None'
  | 'GameNotInProgress'
  | 'NotParticipant'
  | 'OutOfTurn'
  | 'OutOfBounds'
  | 'CellOccupied';

export type ConnectionStateType =
  | 'Disconnected'
  | 'Connecting'
  | 'Connected'
  | 'Reconnecting';

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

/**
 * Response payload returned when invoking JoinGame RPC.
 */
export interface JoinGameResponseDto {
  readonly isJoined: boolean;
  readonly assignedSymbol: PlayerSymbolType | null;
  readonly rejectionReason: string | null;
  readonly gameState: GameStateDto;
}

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

/**
 * Notification payload sent to an individual player when their assigned symbol changes.
 */
export interface PlayerAssignmentDto {
  readonly assignedSymbol: PlayerSymbolType;
  readonly gameState: GameStateDto;
}

/**
 * Response payload returned when invoking RequestRematch RPC.
 */
export interface RematchResponseDto {
  readonly isAccepted: boolean;
  readonly errorMessage: string | null;
  readonly didRematchStart: boolean;
  readonly gameState: GameStateDto;
}
