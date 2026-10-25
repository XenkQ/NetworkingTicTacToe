/**
 * Shared domain literal union types and client connection state types.
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
