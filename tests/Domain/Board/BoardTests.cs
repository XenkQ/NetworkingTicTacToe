namespace NetworkingTicTacToe.Tests.Domain.Board;

using NetworkingTicTacToe.Server.Domain.Board;
using NetworkingTicTacToe.Server.Domain.Match;

public sealed class BoardTests
{
    [Fact]
    public void Constructor_InitializesEmptyBoard()
    {
        // Arrange & Act
        Board board = new();

        // Assert
        Assert.Equal(0, board.OccupiedCount);
        Assert.False(board.IsFull());

        for (int row = 0; row < Board.BOARD_SIZE; row++)
        {
            for (int col = 0; col < Board.BOARD_SIZE; col++)
            {
                Assert.True(board.IsCellEmpty(row, col));
                Assert.Equal(CellState.Empty, board.GetCell(row, col));
            }
        }
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0, 2, true)]
    [InlineData(1, 1, true)]
    [InlineData(2, 0, true)]
    [InlineData(2, 2, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(3, 0, false)]
    [InlineData(0, 3, false)]
    [InlineData(5, 5, false)]
    public void IsWithinBounds_ValidatesCoordinatesCorrectly(int row, int col, bool expectedResult)
    {
        // Arrange
        Board board = new();

        // Act
        bool result = board.IsWithinBounds(row, col);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GetCell_ThrowsArgumentOutOfRangeException_WhenOutOfBounds()
    {
        // Arrange
        Board board = new();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            board.GetCell(3, 0);
        });
    }

    [Fact]
    public void PlaceSymbol_PlacesSymbolAndUpdatesOccupiedCount()
    {
        // Arrange
        Board board = new();

        // Act
        board.PlaceSymbol(1, 1, PlayerSymbol.X);

        // Assert
        Assert.False(board.IsCellEmpty(1, 1));
        Assert.Equal(CellState.X, board.GetCell(1, 1));
        Assert.Equal(1, board.OccupiedCount);
    }

    [Fact]
    public void PlaceSymbol_ThrowsArgumentOutOfRangeException_WhenOutOfBounds()
    {
        // Arrange
        Board board = new();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            board.PlaceSymbol(-1, 0, PlayerSymbol.X);
        });
    }

    [Fact]
    public void PlaceSymbol_ThrowsInvalidOperationException_WhenCellAlreadyOccupied()
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, 0, PlayerSymbol.X);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            board.PlaceSymbol(0, 0, PlayerSymbol.O);
        });
    }

    [Fact]
    public void Reset_ClearsAllCellsAndOccupiedCount()
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, 0, PlayerSymbol.X);
        board.PlaceSymbol(1, 1, PlayerSymbol.O);

        // Act
        board.Reset();

        // Assert
        Assert.Equal(0, board.OccupiedCount);
        Assert.False(board.IsFull());
        Assert.True(board.IsCellEmpty(0, 0));
        Assert.True(board.IsCellEmpty(1, 1));
    }

    [Theory]
    [InlineData(0, PlayerSymbol.X)]
    [InlineData(1, PlayerSymbol.X)]
    [InlineData(2, PlayerSymbol.X)]
    [InlineData(0, PlayerSymbol.O)]
    [InlineData(1, PlayerSymbol.O)]
    [InlineData(2, PlayerSymbol.O)]
    public void EvaluateWin_DetectsRowWin(int row, PlayerSymbol symbol)
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(row, 0, symbol);
        board.PlaceSymbol(row, 1, symbol);
        board.PlaceSymbol(row, 2, symbol);

        // Act
        WinningLine? line = board.EvaluateWin(symbol);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(new BoardCoordinate(row, 0), line.First);
        Assert.Equal(new BoardCoordinate(row, 1), line.Second);
        Assert.Equal(new BoardCoordinate(row, 2), line.Third);
    }

    [Theory]
    [InlineData(0, PlayerSymbol.X)]
    [InlineData(1, PlayerSymbol.X)]
    [InlineData(2, PlayerSymbol.X)]
    [InlineData(0, PlayerSymbol.O)]
    [InlineData(1, PlayerSymbol.O)]
    [InlineData(2, PlayerSymbol.O)]
    public void EvaluateWin_DetectsColumnWin(int col, PlayerSymbol symbol)
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, col, symbol);
        board.PlaceSymbol(1, col, symbol);
        board.PlaceSymbol(2, col, symbol);

        // Act
        WinningLine? line = board.EvaluateWin(symbol);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(new BoardCoordinate(0, col), line.First);
        Assert.Equal(new BoardCoordinate(1, col), line.Second);
        Assert.Equal(new BoardCoordinate(2, col), line.Third);
    }

    [Theory]
    [InlineData(PlayerSymbol.X)]
    [InlineData(PlayerSymbol.O)]
    public void EvaluateWin_DetectsMainDiagonalWin(PlayerSymbol symbol)
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, 0, symbol);
        board.PlaceSymbol(1, 1, symbol);
        board.PlaceSymbol(2, 2, symbol);

        // Act
        WinningLine? line = board.EvaluateWin(symbol);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(new BoardCoordinate(0, 0), line.First);
        Assert.Equal(new BoardCoordinate(1, 1), line.Second);
        Assert.Equal(new BoardCoordinate(2, 2), line.Third);
    }

    [Theory]
    [InlineData(PlayerSymbol.X)]
    [InlineData(PlayerSymbol.O)]
    public void EvaluateWin_DetectsAntiDiagonalWin(PlayerSymbol symbol)
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, 2, symbol);
        board.PlaceSymbol(1, 1, symbol);
        board.PlaceSymbol(2, 0, symbol);

        // Act
        WinningLine? line = board.EvaluateWin(symbol);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(new BoardCoordinate(0, 2), line.First);
        Assert.Equal(new BoardCoordinate(1, 1), line.Second);
        Assert.Equal(new BoardCoordinate(2, 0), line.Third);
    }

    [Fact]
    public void EvaluateWin_ReturnsNull_WhenIncompleteLine()
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, 0, PlayerSymbol.X);
        board.PlaceSymbol(0, 1, PlayerSymbol.X);
        board.PlaceSymbol(0, 2, PlayerSymbol.O);

        // Act
        WinningLine? line = board.EvaluateWin(PlayerSymbol.X);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void IsFull_ReturnsTrue_WhenAllCellsAreOccupied()
    {
        // Arrange
        Board board = new();

        // X O X
        // X O O
        // O X X
        board.PlaceSymbol(0, 0, PlayerSymbol.X);
        board.PlaceSymbol(0, 1, PlayerSymbol.O);
        board.PlaceSymbol(0, 2, PlayerSymbol.X);

        board.PlaceSymbol(1, 0, PlayerSymbol.X);
        board.PlaceSymbol(1, 1, PlayerSymbol.O);
        board.PlaceSymbol(1, 2, PlayerSymbol.O);

        board.PlaceSymbol(2, 0, PlayerSymbol.O);
        board.PlaceSymbol(2, 1, PlayerSymbol.X);
        board.PlaceSymbol(2, 2, PlayerSymbol.X);

        // Act & Assert
        Assert.True(board.IsFull());
        Assert.Null(board.EvaluateWin(PlayerSymbol.X));
        Assert.Null(board.EvaluateWin(PlayerSymbol.O));
    }

    [Fact]
    public void ToJaggedArray_ReturnsFormattedStringGrid()
    {
        // Arrange
        Board board = new();
        board.PlaceSymbol(0, 0, PlayerSymbol.X);
        board.PlaceSymbol(1, 1, PlayerSymbol.O);

        // Act
        string[][] jagged = board.ToJaggedArray();

        // Assert
        Assert.Equal(3, jagged.Length);
        Assert.Equal("X", jagged[0][0]);
        Assert.Equal("Empty", jagged[0][1]);
        Assert.Equal("O", jagged[1][1]);
        Assert.Equal("Empty", jagged[2][2]);
    }
}
