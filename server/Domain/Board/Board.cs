namespace NetworkingTicTacToe.Server.Domain.Board;

using NetworkingTicTacToe.Server.Domain.Match;

/// <summary>
/// Encapsulates the pure 3x3 Tic-Tac-Toe grid, symbol placement, and win/draw evaluation.
/// </summary>
public sealed class Board
{
    public const int BOARD_SIZE = 3;
    public const int TOTAL_CELLS = 9;

    private readonly CellState[,] _cells = new CellState[BOARD_SIZE, BOARD_SIZE];
    private int _occupiedCount;

    public Board()
    {
    }

    public int OccupiedCount => _occupiedCount;

    public bool IsWithinBounds(int row, int col)
    {
        return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
    }

    public CellState GetCell(int row, int col)
    {
        if (!IsWithinBounds(row, col))
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Coordinates ({row}, {col}) are out of board bounds.");
        }

        return _cells[row, col];
    }

    public bool IsCellEmpty(int row, int col)
    {
        if (!IsWithinBounds(row, col))
        {
            return false;
        }

        return _cells[row, col] == CellState.Empty;
    }

    public void PlaceSymbol(int row, int col, PlayerSymbol symbol)
    {
        if (!IsWithinBounds(row, col))
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Coordinates ({row}, {col}) are out of board bounds.");
        }

        if (_cells[row, col] != CellState.Empty)
        {
            throw new InvalidOperationException($"Cell ({row}, {col}) is already occupied by {_cells[row, col]}.");
        }

        CellState targetState = symbol == PlayerSymbol.X ? CellState.X : CellState.O;
        _cells[row, col] = targetState;
        _occupiedCount++;
    }

    public void Reset()
    {
        for (int row = 0; row < BOARD_SIZE; row++)
        {
            for (int col = 0; col < BOARD_SIZE; col++)
            {
                _cells[row, col] = CellState.Empty;
            }
        }

        _occupiedCount = 0;
    }

    public WinningLine? EvaluateWin(PlayerSymbol symbol)
    {
        CellState target = symbol == PlayerSymbol.X ? CellState.X : CellState.O;

        // Check rows
        for (int row = 0; row < BOARD_SIZE; row++)
        {
            if (_cells[row, 0] == target && _cells[row, 1] == target && _cells[row, 2] == target)
            {
                return new WinningLine(
                    new BoardCoordinate(row, 0),
                    new BoardCoordinate(row, 1),
                    new BoardCoordinate(row, 2));
            }
        }

        // Check columns
        for (int col = 0; col < BOARD_SIZE; col++)
        {
            if (_cells[0, col] == target && _cells[1, col] == target && _cells[2, col] == target)
            {
                return new WinningLine(
                    new BoardCoordinate(0, col),
                    new BoardCoordinate(1, col),
                    new BoardCoordinate(2, col));
            }
        }

        // Check main diagonal (top-left to bottom-right)
        if (_cells[0, 0] == target && _cells[1, 1] == target && _cells[2, 2] == target)
        {
            return new WinningLine(
                new BoardCoordinate(0, 0),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 2));
        }

        // Check anti-diagonal (top-right to bottom-left)
        if (_cells[0, 2] == target && _cells[1, 1] == target && _cells[2, 0] == target)
        {
            return new WinningLine(
                new BoardCoordinate(0, 2),
                new BoardCoordinate(1, 1),
                new BoardCoordinate(2, 0));
        }

        return null;
    }

    public bool IsFull()
    {
        return _occupiedCount >= TOTAL_CELLS;
    }

    public string[][] ToJaggedArray()
    {
        string[][] jagged = new string[BOARD_SIZE][];
        for (int row = 0; row < BOARD_SIZE; row++)
        {
            jagged[row] = new string[BOARD_SIZE];
            for (int col = 0; col < BOARD_SIZE; col++)
            {
                jagged[row][col] = _cells[row, col].ToString();
            }
        }

        return jagged;
    }
}
