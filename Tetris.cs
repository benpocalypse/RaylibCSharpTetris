using Raylib_cs;
using System.Numerics;

namespace RaylibCSharpTetris
{
    public static class Game
    {
        private const int GridWidth = 10;
        private const int GridHeight = 20;
        private const int CellSize = 30;
        private const int BorderOffset = 10;

        private static int[,] grid = new int[GridHeight, GridWidth];
        private static Block? currentBlock;
        private static Block? nextBlock;
        private static float dropTimer = 0f;
        private static float dropInterval = 0.5f;
        private static int score = 0;
        private static int level = 1;
        private static int lines = 0;
        private static bool gameOver = false;
        private static Random random = new Random();

        // Tetromino shapes - matching original C implementation
        private static readonly int[][][] Tetrominoes = new int[][][]
        {
            // I
            new int[][] { new int[] { 0, 0, 0, 0 }, new int[] { 1, 1, 1, 1 }, new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 } },
            // O
            new int[][] { new int[] { 1, 1 }, new int[] { 1, 1 } },
            // T
            new int[][] { new int[] { 0, 1, 0 }, new int[] { 1, 1, 1 }, new int[] { 0, 0, 0 } },
            // S
            new int[][] { new int[] { 0, 1, 1 }, new int[] { 1, 1, 0 }, new int[] { 0, 0, 0 } },
            // Z
            new int[][] { new int[] { 1, 1, 0 }, new int[] { 0, 1, 1 }, new int[] { 0, 0, 0 } },
            // L
            new int[][] { new int[] { 1, 0, 0 }, new int[] { 1, 1, 1 }, new int[] { 0, 0, 0 } },
            // J
            new int[][] { new int[] { 0, 0, 1 }, new int[] { 1, 1, 1 }, new int[] { 0, 0, 0 } }
        };

        // Original C implementation colors (similar to classic Tetris)
        private static readonly Color[] Colors = new Color[]
        {
            Color.SkyBlue,   // I
            Color.Yellow,    // O
            Color.Purple,    // T
            Color.Green,     // S
            Color.Red,       // Z
            Color.Orange,    // L
            Color.Blue       // J
        };

        public static void Main()
        {
            const int screenWidth = 800;
            const int screenHeight = 700;
            
            Raylib.InitWindow(screenWidth, screenHeight, "Raylib C# Tetris");
            Raylib.SetTargetFPS(60);

            // Initialize audio
            TetrisMusicGenerator.Initialize();
            
            InitGame();

            while (!Raylib.WindowShouldClose())
            {
                float deltaTime = Raylib.GetFrameTime();

                // Handle input
                if (Raylib.IsKeyPressed(KeyboardKey.Left)) MoveLeft();
                if (Raylib.IsKeyPressed(KeyboardKey.Right)) MoveRight();
                if (Raylib.IsKeyPressed(KeyboardKey.Up)) RotateBlock();
                if (Raylib.IsKeyPressed(KeyboardKey.Down)) HardDrop();
                if (Raylib.IsKeyPressed(KeyboardKey.Space)) HardDrop();
                if (Raylib.IsKeyPressed(KeyboardKey.R)) RestartGame();
                if (Raylib.IsKeyPressed(KeyboardKey.M)) ToggleMusic();

                Update(deltaTime);

                Raylib.BeginDrawing();
                Draw();
                Raylib.EndDrawing();
            }

            TetrisMusicGenerator.Dispose();
            Raylib.CloseWindow();
        }

        private static void InitGame()
        {
            // Clear the grid
            for (int row = 0; row < GridHeight; row++)
            {
                for (int col = 0; col < GridWidth; col++)
                {
                    grid[row, col] = -1;
                }
            }

            score = 0;
            level = 1;
            lines = 0;
            dropInterval = 0.5f;
            dropTimer = 0f;
            gameOver = false;

            // Spawn first block
            SpawnBlock();
            // Spawn next block
            SpawnNextBlock();
            
            TetrisMusicGenerator.PlayBackgroundMusic();
        }

        private static void SpawnBlock()
        {
            if (nextBlock == null)
            {
                SpawnNextBlock();
            }
            
            // Current block becomes the next block
            currentBlock = new Block(
                nextBlock.Shape, 
                nextBlock.ColorIndex, 
                GridWidth / 2 - 1, 
                0
            );
            
            SpawnNextBlock();

            if (!IsValidMove(currentBlock.Shape, currentBlock.X, currentBlock.Y))
            {
                gameOver = true;
                currentBlock = null;
                TetrisMusicGenerator.StopBackgroundMusic();
            }
        }

        private static void SpawnNextBlock()
        {
            int type = random.Next(Tetrominoes.Length);
            int colorIndex = type;
            nextBlock = new Block(Tetrominoes[type], colorIndex, 0, 0);
        }

        private static bool IsValidMove(int[][] shape, int offsetX, int offsetY)
        {
            for (int row = 0; row < shape.Length; row++)
            {
                for (int col = 0; col < shape[row].Length; col++)
                {
                    if (shape[row][col] != 0)
                    {
                        int newX = offsetX + col;
                        int newY = offsetY + row;

                        if (newX < 0 || newX >= GridWidth || newY >= GridHeight)
                        {
                            return false;
                        }

                        if (newY >= 0 && grid[newY, newX] != -1)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static void MoveLeft()
        {
            if (currentBlock != null && IsValidMove(currentBlock.Shape, currentBlock.X - 1, currentBlock.Y))
            {
                currentBlock.X--;
                TetrisMusicGenerator.PlayMoveSound();
            }
        }

        private static void MoveRight()
        {
            if (currentBlock != null && IsValidMove(currentBlock.Shape, currentBlock.X + 1, currentBlock.Y))
            {
                currentBlock.X++;
                TetrisMusicGenerator.PlayMoveSound();
            }
        }

        private static void RotateBlock()
        {
            if (currentBlock != null)
            {
                int[][] rotated = RotateShape(currentBlock.Shape);
                if (IsValidMove(rotated, currentBlock.X, currentBlock.Y))
                {
                    currentBlock.Shape = rotated;
                    TetrisMusicGenerator.PlayRotateSound();
                }
            }
        }

        private static int[][] RotateShape(int[][] shape)
        {
            int rows = shape.Length;
            int cols = shape[0].Length;
            int[][] rotated = new int[cols][];
            for (int i = 0; i < cols; i++)
            {
                rotated[i] = new int[rows];
                for (int j = 0; j < rows; j++)
                {
                    rotated[i][j] = shape[rows - 1 - j][i];
                }
            }
            return rotated;
        }

        private static void HardDrop()
        {
            if (currentBlock != null)
            {
                while (IsValidMove(currentBlock.Shape, currentBlock.X, currentBlock.Y + 1))
                {
                    currentBlock.Y++;
                }
                LockBlock();
                TetrisMusicGenerator.PlayDropSound();
            }
        }

        private static void LockBlock()
        {
            if (currentBlock == null) return;

            for (int row = 0; row < currentBlock.Shape.Length; row++)
            {
                for (int col = 0; col < currentBlock.Shape[row].Length; col++)
                {
                    if (currentBlock.Shape[row][col] != 0)
                    {
                        int gridY = currentBlock.Y + row;
                        int gridX = currentBlock.X + col;
                        if (gridY >= 0 && gridY < GridHeight && gridX >= 0 && gridX < GridWidth)
                        {
                            grid[gridY, gridX] = currentBlock.ColorIndex;
                        }
                    }
                }
            }

            int rowsCleared = ClearRows();
            if (rowsCleared > 0)
            {
                TetrisMusicGenerator.PlayClearSound();
                UpdateScore(rowsCleared);
            }

            SpawnBlock();
        }

        private static int ClearRows()
        {
            int rowsCleared = 0;
            for (int row = GridHeight - 1; row >= 0; )
            {
                bool full = true;
                for (int col = 0; col < GridWidth; col++)
                {
                    if (grid[row, col] == -1)
                    {
                        full = false;
                        break;
                    }
                }

                if (full)
                {
                    for (int r = row; r > 0; r--)
                    {
                        for (int col = 0; col < GridWidth; col++)
                        {
                            grid[r, col] = grid[r - 1, col];
                        }
                    }
                    for (int col = 0; col < GridWidth; col++)
                    {
                        grid[0, col] = -1;
                    }
                    rowsCleared++;
                }
                else
                {
                    row--;
                }
            }
            return rowsCleared;
        }

        private static void UpdateScore(int rowsCleared)
        {
            // Classic scoring
            int points = 0;
            switch (rowsCleared)
            {
                case 1: points = 100; break;
                case 2: points = 300; break;
                case 3: points = 500; break;
                case 4: points = 800; break;
            }
            score += points;
            lines += rowsCleared;

            // Level up every 10 lines
            int newLevel = (lines / 10) + 1;
            if (newLevel > level)
            {
                level = newLevel;
                dropInterval = Math.Max(0.05f, 0.5f - (level - 1) * 0.05f);
            }
        }

        private static void Update(float deltaTime)
        {
            if (gameOver || currentBlock == null) return;

            dropTimer += deltaTime;
            if (dropTimer >= dropInterval)
            {
                dropTimer = 0f;
                if (IsValidMove(currentBlock.Shape, currentBlock.X, currentBlock.Y + 1))
                {
                    currentBlock.Y++;
                }
                else
                {
                    LockBlock();
                }
            }
        }

        private static void Draw()
        {
            // Dark blue background like original C implementation
            Raylib.ClearBackground(new Color(20, 20, 40, 255));

            // Game board dimensions
            int boardWidth = GridWidth * CellSize + BorderOffset * 2;
            int boardHeight = GridHeight * CellSize + BorderOffset * 2;
            int startX = (Raylib.GetScreenWidth() - boardWidth - 200) / 2;
            int startY = (Raylib.GetScreenHeight() - boardHeight) / 2;

            // Draw border (like original C)
            Raylib.DrawRectangle(startX - 5, startY - 5, boardWidth + 10, boardHeight + 10, Color.DarkGray);
            Raylib.DrawRectangle(startX, startY, boardWidth, boardHeight, Color.Black);

            // Draw grid
            for (int row = 0; row < GridHeight; row++)
            {
                for (int col = 0; col < GridWidth; col++)
                {
                    int cellX = startX + BorderOffset + col * CellSize;
                    int cellY = startY + BorderOffset + row * CellSize;

                    if (grid[row, col] != -1)
                    {
                        Raylib.DrawRectangle(cellX + 1, cellY + 1, CellSize - 2, CellSize - 2, Colors[grid[row, col]]);
                    }
                    else
                    {
                        Raylib.DrawRectangleLines(cellX, cellY, CellSize, CellSize, new Color(50, 50, 50, 255));
                    }
                }
            }

            // Draw current block with border
            if (currentBlock != null)
            {
                for (int row = 0; row < currentBlock.Shape.Length; row++)
                {
                    for (int col = 0; col < currentBlock.Shape[row].Length; col++)
                    {
                        if (currentBlock.Shape[row][col] != 0)
                        {
                            int cellX = startX + BorderOffset + (currentBlock.X + col) * CellSize;
                            int cellY = startY + BorderOffset + (currentBlock.Y + row) * CellSize;
                            Raylib.DrawRectangle(cellX + 1, cellY + 1, CellSize - 2, CellSize - 2, Colors[currentBlock.ColorIndex]);
                            // Draw border for block
                            Raylib.DrawRectangleLines(cellX, cellY, CellSize, CellSize, Color.White);
                        }
                    }
                }
            }

            // Next block preview - positioned to the right of the board
            int previewX = startX + boardWidth + 40;
            int previewY = startY + 40;
            Raylib.DrawText("NEXT", previewX + 20, previewY, 20, Color.White);
            
            if (nextBlock != null)
            {
                for (int row = 0; row < nextBlock.Shape.Length; row++)
                {
                    for (int col = 0; col < nextBlock.Shape[row].Length; col++)
                    {
                        if (nextBlock.Shape[row][col] != 0)
                        {
                            int cellX = previewX + col * CellSize + 40;
                            int cellY = previewY + row * CellSize + 40;
                            Raylib.DrawRectangle(cellX + 1, cellY + 1, CellSize - 2, CellSize - 2, Colors[nextBlock.ColorIndex]);
                            Raylib.DrawRectangleLines(cellX, cellY, CellSize, CellSize, Color.White);
                        }
                    }
                }
            }

            // Score and stats - positioned below next block preview
            int infoX = previewX;
            int infoY = previewY + 200;
            Raylib.DrawText($"SCORE: {score}", infoX, infoY, 20, Color.White);
            Raylib.DrawText($"LEVEL: {level}", infoX, infoY + 35, 20, Color.White);
            Raylib.DrawText($"LINES: {lines}", infoX, infoY + 70, 20, Color.White);

            // Controls info
            int controlsY = infoY + 140;
            Raylib.DrawText("CONTROLS:", infoX, controlsY, 16, Color.White);
            Raylib.DrawText("←/→: Move", infoX + 10, controlsY + 30, 14, Color.Gray);
            Raylib.DrawText("↑: Rotate", infoX + 10, controlsY + 55, 14, Color.Gray);
            Raylib.DrawText("↓: Soft Drop", infoX + 10, controlsY + 80, 14, Color.Gray);
            Raylib.DrawText("SPACE: Hard Drop", infoX + 10, controlsY + 105, 14, Color.Gray);
            Raylib.DrawText("M: Toggle Music", infoX + 10, controlsY + 130, 14, Color.Gray);
            Raylib.DrawText("R: Restart", infoX + 10, controlsY + 155, 14, Color.Gray);

            // Game Over overlay
            if (gameOver)
            {
                int textWidth = Raylib.MeasureText("GAME OVER", 40);
                int textX = (Raylib.GetScreenWidth() - textWidth) / 2;
                int textY = Raylib.GetScreenHeight() / 2 - 20;
                Raylib.DrawRectangle(textX - 20, textY - 20, textWidth + 40, 80, Color.Black);
                Raylib.DrawRectangleLines(textX - 20, textY - 20, textWidth + 40, 80, Color.Red);
                Raylib.DrawText("GAME OVER", textX, textY, 40, Color.Red);
                Raylib.DrawText("Press R to restart", textX + 30, textY + 45, 20, Color.White);
            }
        }

        private static void RestartGame()
        {
            InitGame();
        }

        private static void ToggleMusic()
        {
            TetrisMusicGenerator.StopBackgroundMusic();
            TetrisMusicGenerator.PlayBackgroundMusic();
        }
    }

    public class Block
    {
        public int[][] Shape { get; set; }
        public int ColorIndex { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Block(int[][] shape, int colorIndex, int x, int y)
        {
            Shape = shape;
            ColorIndex = colorIndex;
            X = x;
            Y = y;
        }
    }
}
