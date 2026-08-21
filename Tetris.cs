/*******************************************************************************************
*
*   raylib_cs - classic game: tetris
*
*   C# port of the raylib "tetris" sample (originally by Marc Palau and Ramon Santamaria)
*   using the raylib_cs bindings (https://github.com/ChrisDill/Raylib-cs)
*
*   Original C source: raylib-games/classics/src/tetris.c
*   Copyright (c) 2015 Ramon Santamaria (@raysan5)
*
********************************************************************************************/

using System;
using System.Numerics;
using Raylib_cs;

namespace TetrisGame
{
    // Matches the C GridSquare enum
    public enum GridSquare
    {
        Empty,
        Moving,
        Full,
        Block,
        Fading
    }

    public static class Tetris
    {
        //----------------------------------------------------------------------------------
        // Some Defines
        //----------------------------------------------------------------------------------
        private const int SquareSize = 20;

        private const int GridHorizontalSize = 12;
        private const int GridVerticalSize = 20;

        private const int LateralSpeed = 10;
        private const int TurningSpeed = 12;
        private const int FastFallAwaitCounter = 30;

        private const int FadingTime = 33;

        //------------------------------------------------------------------------------------
        // Global Variables Declaration
        //------------------------------------------------------------------------------------
        private const int ScreenWidth = 800;
        private const int ScreenHeight = 450;

        private static bool gameOver = false;
        private static bool pause = false;

        // Matrices
        private static GridSquare[,] grid = new GridSquare[GridHorizontalSize, GridVerticalSize];
        private static GridSquare[,] piece = new GridSquare[4, 4];
        private static GridSquare[,] incomingPiece = new GridSquare[4, 4];

        // These variables keep track of the active piece position
        private static int piecePositionX = 0;
        private static int piecePositionY = 0;

        // Game parameters
        private static Color fadingColor;

        private static bool beginPlay = true; // Only true at the beginning of the game, used for the first matrix creations
        private static bool pieceActive = false;
        private static bool detection = false;
        private static bool lineToDelete = false;

        // Statistics
        private static int level = 1;
        private static int lines = 0;

        // Counters
        private static int gravityMovementCounter = 0;
        private static int lateralMovementCounter = 0;
        private static int turnMovementCounter = 0;
        private static int fastFallMovementCounter = 0;
        private static int fadeLineCounter = 0;

        // Based on level
        private static int gravitySpeed = 30;

        //------------------------------------------------------------------------------------
        // Program main entry point
        //------------------------------------------------------------------------------------
        public static void Main()
        {
            // Initialization
            //---------------------------------------------------------
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "classic game: tetris");

            InitGame();

            Raylib.SetTargetFPS(60);
            //--------------------------------------------------------------------------------------

            // Main game loop
            while (!Raylib.WindowShouldClose()) // Detect window close button or ESC key
            {
                // Update and Draw
                //----------------------------------------------------------------------------------
                UpdateDrawFrame();
                //----------------------------------------------------------------------------------
            }

            // De-Initialization
            //--------------------------------------------------------------------------------------
            UnloadGame(); // Unload loaded data (textures, sounds, models...)

            Raylib.CloseWindow(); // Close window and OpenGL context
            //--------------------------------------------------------------------------------------
        }

        //--------------------------------------------------------------------------------------
        // Game Module Functions Definition
        //--------------------------------------------------------------------------------------

        // Initialize game variables
        private static void InitGame()
        {
            // Initialize game statistics
            level = 1;
            lines = 0;

            fadingColor = Color.Gray;

            piecePositionX = 0;
            piecePositionY = 0;

            pause = false;

            beginPlay = true;
            pieceActive = false;
            detection = false;
            lineToDelete = false;

            // Counters
            gravityMovementCounter = 0;
            lateralMovementCounter = 0;
            turnMovementCounter = 0;
            fastFallMovementCounter = 0;
            fadeLineCounter = 0;

            gravitySpeed = 30;

            // Initialize grid matrices
            for (int i = 0; i < GridHorizontalSize; i++)
            {
                for (int j = 0; j < GridVerticalSize; j++)
                {
                    if ((j == GridVerticalSize - 1) || (i == 0) || (i == GridHorizontalSize - 1))
                        grid[i, j] = GridSquare.Block;
                    else
                        grid[i, j] = GridSquare.Empty;
                }
            }

            // Initialize incoming piece matrices
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    incomingPiece[i, j] = GridSquare.Empty;
                }
            }
            
            // Initialize audio	
	    TetrisMusicGenerator.Initialize();

	   // Start background music when game begins
	   TetrisMusicGenerator.PlayBackgroundMusic();
        }

        // Update game (one frame)
        private static void UpdateGame()
        {
            if (!gameOver)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.P)) pause = !pause;

                if (!pause)
                {
                    if (!lineToDelete)
                    {
                        if (!pieceActive)
                        {
                            // Get another piece
                            pieceActive = CreatePiece();

                            // We leave a little time before starting the fast falling down
                            fastFallMovementCounter = 0;
                        }
                        else // Piece falling
                        {
                            // Counters update
                            fastFallMovementCounter++;
                            gravityMovementCounter++;
                            lateralMovementCounter++;
                            turnMovementCounter++;

                            // We make sure to move if we've pressed the key this frame
                            if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.Right))
                                lateralMovementCounter = LateralSpeed;

                            if (Raylib.IsKeyPressed(KeyboardKey.Up)) turnMovementCounter = TurningSpeed;

                            // Fall down
                            if (Raylib.IsKeyDown(KeyboardKey.Down) && (fastFallMovementCounter >= FastFallAwaitCounter))
                            {
                                // We make sure the piece is going to fall this frame
                                gravityMovementCounter += gravitySpeed;
                            }

                            if (gravityMovementCounter >= gravitySpeed)
                            {
                                // Basic falling movement
                                CheckDetection(ref detection);

                                // Check if the piece has collided with another piece or with the boundings
                                ResolveFallingMovement(ref detection, ref pieceActive);

                                // Check if we fulfilled a line and if so, erase the line and pull down the lines above
                                CheckCompletion(ref lineToDelete);

                                gravityMovementCounter = 0;
                            }

                            // Move laterally at player's will
                            if (lateralMovementCounter >= LateralSpeed)
                            {
                                // Update the lateral movement and if success, reset the lateral counter
                                if (!ResolveLateralMovement()) lateralMovementCounter = 0;
                            }

                            // Turn the piece at player's will
                            if (turnMovementCounter >= TurningSpeed)
                            {
                                // Update the turning movement and reset the turning counter
                                if (ResolveTurnMovement()) turnMovementCounter = 0;
                            }
                        }

                        // Game over logic
                        for (int j = 0; j < 2; j++)
                        {
                            for (int i = 1; i < GridHorizontalSize - 1; i++)
                            {
                                if (grid[i, j] == GridSquare.Full)
                                {
                                    gameOver = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Animation when deleting lines
                        fadeLineCounter++;

                        if (fadeLineCounter % 8 < 4) fadingColor = Color.Maroon;
                        else fadingColor = Color.Gray;

                        if (fadeLineCounter >= FadingTime)
                        {
                            int deletedLines = DeleteCompleteLines();
                            fadeLineCounter = 0;
                            lineToDelete = false;

                            lines += deletedLines;
                        }
                    }
                }
            }
            else
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    InitGame();
                    gameOver = false;
                }
            }
        }

        // Draw game (one frame)
        private static void DrawGame()
        {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.RayWhite);

            if (!gameOver)
            {
                // Draw gameplay area
                Vector2 offset;
                offset.X = ScreenWidth / 2 - (GridHorizontalSize * SquareSize / 2) - 50;
                offset.Y = ScreenHeight / 2 - ((GridVerticalSize - 1) * SquareSize / 2) + SquareSize * 2;
                offset.Y -= 50; // NOTE: Hardcoded position!

                float controller = offset.X;

                for (int j = 0; j < GridVerticalSize; j++)
                {
                    for (int i = 0; i < GridHorizontalSize; i++)
                    {
                        // Draw each square of the grid
                        switch (grid[i, j])
                        {
                            case GridSquare.Empty:
                                Raylib.DrawLine((int)offset.X, (int)offset.Y, (int)offset.X + SquareSize, (int)offset.Y, Color.LightGray);
                                Raylib.DrawLine((int)offset.X, (int)offset.Y, (int)offset.X, (int)offset.Y + SquareSize, Color.LightGray);
                                Raylib.DrawLine((int)offset.X + SquareSize, (int)offset.Y, (int)offset.X + SquareSize, (int)offset.Y + SquareSize, Color.LightGray);
                                Raylib.DrawLine((int)offset.X, (int)offset.Y + SquareSize, (int)offset.X + SquareSize, (int)offset.Y + SquareSize, Color.LightGray);
                                offset.X += SquareSize;
                                break;
                            case GridSquare.Full:
                                Raylib.DrawRectangle((int)offset.X, (int)offset.Y, SquareSize, SquareSize, Color.Gray);
                                offset.X += SquareSize;
                                break;
                            case GridSquare.Moving:
                                Raylib.DrawRectangle((int)offset.X, (int)offset.Y, SquareSize, SquareSize, Color.DarkGray);
                                offset.X += SquareSize;
                                break;
                            case GridSquare.Block:
                                Raylib.DrawRectangle((int)offset.X, (int)offset.Y, SquareSize, SquareSize, Color.LightGray);
                                offset.X += SquareSize;
                                break;
                            case GridSquare.Fading:
                                Raylib.DrawRectangle((int)offset.X, (int)offset.Y, SquareSize, SquareSize, fadingColor);
                                offset.X += SquareSize;
                                break;
                        }
                    }

                    offset.X = controller;
                    offset.Y += SquareSize;
                }

                // Draw incoming piece (hardcoded)
                offset.X = 500;
                offset.Y = 45;
                float controler = offset.X;

                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (incomingPiece[i, j] == GridSquare.Empty)
                        {
                            Raylib.DrawLine((int)offset.X, (int)offset.Y, (int)offset.X + SquareSize, (int)offset.Y, Color.LightGray);
                            Raylib.DrawLine((int)offset.X, (int)offset.Y, (int)offset.X, (int)offset.Y + SquareSize, Color.LightGray);
                            Raylib.DrawLine((int)offset.X + SquareSize, (int)offset.Y, (int)offset.X + SquareSize, (int)offset.Y + SquareSize, Color.LightGray);
                            Raylib.DrawLine((int)offset.X, (int)offset.Y + SquareSize, (int)offset.X + SquareSize, (int)offset.Y + SquareSize, Color.LightGray);
                            offset.X += SquareSize;
                        }
                        else if (incomingPiece[i, j] == GridSquare.Moving)
                        {
                            Raylib.DrawRectangle((int)offset.X, (int)offset.Y, SquareSize, SquareSize, Color.Gray);
                            offset.X += SquareSize;
                        }
                    }

                    offset.X = controler;
                    offset.Y += SquareSize;
                }

                Raylib.DrawText("INCOMING:", (int)offset.X, (int)offset.Y - 100, 10, Color.Gray);
                Raylib.DrawText($"LINES: {lines:0000}", (int)offset.X, (int)offset.Y + 20, 10, Color.Gray);

                if (pause)
                {
                    Raylib.DrawText(
                        "GAME PAUSED",
                        ScreenWidth / 2 - Raylib.MeasureText("GAME PAUSED", 40) / 2,
                        ScreenHeight / 2 - 40,
                        40,
                        Color.Gray);
                }
            }
            else
            {
                Raylib.DrawText(
                    "PRESS [ENTER] TO PLAY AGAIN",
                    Raylib.GetScreenWidth() / 2 - Raylib.MeasureText("PRESS [ENTER] TO PLAY AGAIN", 20) / 2,
                    Raylib.GetScreenHeight() / 2 - 50,
                    20,
                    Color.Gray);
            }

            Raylib.EndDrawing();
        }

        // Unload game variables
        private static void UnloadGame()
        {
            // TODO: Unload all dynamic loaded data (textures, sounds, models...)
        }

        // Update and Draw (one frame)
        private static void UpdateDrawFrame()
        {
            UpdateGame();
            DrawGame();
        }

        //--------------------------------------------------------------------------------------
        // Additional module functions
        //--------------------------------------------------------------------------------------
        private static bool CreatePiece()
        {
            piecePositionX = (GridHorizontalSize - 4) / 2;
            piecePositionY = 0;

            // If the game is starting and you are going to create the first piece, we create an extra one
            if (beginPlay)
            {
                GetRandomPiece();
                beginPlay = false;
            }

            // We assign the incoming piece to the actual piece
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    piece[i, j] = incomingPiece[i, j];
                }
            }

            // We assign a random piece to the incoming one
            GetRandomPiece();

            // Assign the piece to the grid
            for (int i = piecePositionX; i < piecePositionX + 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (piece[i - piecePositionX, j] == GridSquare.Moving) grid[i, j] = GridSquare.Moving;
                }
            }

            return true;
        }

        private static void GetRandomPiece()
        {
            int random = Raylib.GetRandomValue(0, 6);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    incomingPiece[i, j] = GridSquare.Empty;
                }
            }

            switch (random)
            {
                case 0: // Cube
                    incomingPiece[1, 1] = GridSquare.Moving;
                    incomingPiece[2, 1] = GridSquare.Moving;
                    incomingPiece[1, 2] = GridSquare.Moving;
                    incomingPiece[2, 2] = GridSquare.Moving;
                    break;
                case 1: // L
                    incomingPiece[1, 0] = GridSquare.Moving;
                    incomingPiece[1, 1] = GridSquare.Moving;
                    incomingPiece[1, 2] = GridSquare.Moving;
                    incomingPiece[2, 2] = GridSquare.Moving;
                    break;
                case 2: // L inverse
                    incomingPiece[1, 2] = GridSquare.Moving;
                    incomingPiece[2, 0] = GridSquare.Moving;
                    incomingPiece[2, 1] = GridSquare.Moving;
                    incomingPiece[2, 2] = GridSquare.Moving;
                    break;
                case 3: // Straight
                    incomingPiece[0, 1] = GridSquare.Moving;
                    incomingPiece[1, 1] = GridSquare.Moving;
                    incomingPiece[2, 1] = GridSquare.Moving;
                    incomingPiece[3, 1] = GridSquare.Moving;
                    break;
                case 4: // T
                    incomingPiece[1, 0] = GridSquare.Moving;
                    incomingPiece[1, 1] = GridSquare.Moving;
                    incomingPiece[1, 2] = GridSquare.Moving;
                    incomingPiece[2, 1] = GridSquare.Moving;
                    break;
                case 5: // S
                    incomingPiece[1, 1] = GridSquare.Moving;
                    incomingPiece[2, 1] = GridSquare.Moving;
                    incomingPiece[2, 2] = GridSquare.Moving;
                    incomingPiece[3, 2] = GridSquare.Moving;
                    break;
                case 6: // S inverse
                    incomingPiece[1, 2] = GridSquare.Moving;
                    incomingPiece[2, 2] = GridSquare.Moving;
                    incomingPiece[2, 1] = GridSquare.Moving;
                    incomingPiece[3, 1] = GridSquare.Moving;
                    break;
            }
        }

        private static void ResolveFallingMovement(ref bool detection, ref bool pieceActive)
        {
            // If we finished moving this piece, we stop it
            if (detection)
            {
                for (int j = GridVerticalSize - 2; j >= 0; j--)
                {
                    for (int i = 1; i < GridHorizontalSize - 1; i++)
                    {
                        if (grid[i, j] == GridSquare.Moving)
                        {
                            grid[i, j] = GridSquare.Full;
                            detection = false;
                            pieceActive = false;
                        }
                    }
                }
            }
            else // We move down the piece
            {
                for (int j = GridVerticalSize - 2; j >= 0; j--)
                {
                    for (int i = 1; i < GridHorizontalSize - 1; i++)
                    {
                        if (grid[i, j] == GridSquare.Moving)
                        {
                            grid[i, j + 1] = GridSquare.Moving;
                            grid[i, j] = GridSquare.Empty;
                        }
                    }
                }

                piecePositionY++;
            }
        }

        private static bool ResolveLateralMovement()
        {
            bool collision = false;

            // Piece movement
            if (Raylib.IsKeyDown(KeyboardKey.Left)) // Move left
            {
                // Check if is possible to move to left
                for (int j = GridVerticalSize - 2; j >= 0; j--)
                {
                    for (int i = 1; i < GridHorizontalSize - 1; i++)
                    {
                        if (grid[i, j] == GridSquare.Moving)
                        {
                            // Check if we are touching the left wall or we have a full square at the left
                            if ((i - 1 == 0) || (grid[i - 1, j] == GridSquare.Full)) collision = true;
                        }
                    }
                }

                // If able, move left
                if (!collision)
                {
                    for (int j = GridVerticalSize - 2; j >= 0; j--)
                    {
                        for (int i = 1; i < GridHorizontalSize - 1; i++) // We check the matrix from left to right
                        {
                            // Move everything to the left
                            if (grid[i, j] == GridSquare.Moving)
                            {
                                grid[i - 1, j] = GridSquare.Moving;
                                grid[i, j] = GridSquare.Empty;
                            }
                        }
                    }

                    piecePositionX--;
                }
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Right)) // Move right
            {
                // Check if is possible to move to right
                for (int j = GridVerticalSize - 2; j >= 0; j--)
                {
                    for (int i = 1; i < GridHorizontalSize - 1; i++)
                    {
                        if (grid[i, j] == GridSquare.Moving)
                        {
                            // Check if we are touching the right wall or we have a full square at the right
                            if ((i + 1 == GridHorizontalSize - 1) || (grid[i + 1, j] == GridSquare.Full))
                            {
                                collision = true;
                            }
                        }
                    }
                }

                // If able move right
                if (!collision)
                {
                    for (int j = GridVerticalSize - 2; j >= 0; j--)
                    {
                        for (int i = GridHorizontalSize - 1; i >= 1; i--) // We check the matrix from right to left
                        {
                            // Move everything to the right
                            if (grid[i, j] == GridSquare.Moving)
                            {
                                grid[i + 1, j] = GridSquare.Moving;
                                grid[i, j] = GridSquare.Empty;
                            }
                        }
                    }

                    piecePositionX++;
                }
            }

            return collision;
        }

        private static bool ResolveTurnMovement()
        {
            // Input for turning the piece
            if (Raylib.IsKeyDown(KeyboardKey.Up))
            {
                GridSquare aux;
                bool checker = false;

                // Check all turning possibilities
                if ((grid[piecePositionX + 3, piecePositionY] == GridSquare.Moving) &&
                    (grid[piecePositionX, piecePositionY] != GridSquare.Empty) &&
                    (grid[piecePositionX, piecePositionY] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 3, piecePositionY + 3] == GridSquare.Moving) &&
                    (grid[piecePositionX + 3, piecePositionY] != GridSquare.Empty) &&
                    (grid[piecePositionX + 3, piecePositionY] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX, piecePositionY + 3] == GridSquare.Moving) &&
                    (grid[piecePositionX + 3, piecePositionY + 3] != GridSquare.Empty) &&
                    (grid[piecePositionX + 3, piecePositionY + 3] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX, piecePositionY] == GridSquare.Moving) &&
                    (grid[piecePositionX, piecePositionY + 3] != GridSquare.Empty) &&
                    (grid[piecePositionX, piecePositionY + 3] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 1, piecePositionY] == GridSquare.Moving) &&
                    (grid[piecePositionX, piecePositionY + 2] != GridSquare.Empty) &&
                    (grid[piecePositionX, piecePositionY + 2] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 3, piecePositionY + 1] == GridSquare.Moving) &&
                    (grid[piecePositionX + 1, piecePositionY] != GridSquare.Empty) &&
                    (grid[piecePositionX + 1, piecePositionY] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 2, piecePositionY + 3] == GridSquare.Moving) &&
                    (grid[piecePositionX + 3, piecePositionY + 1] != GridSquare.Empty) &&
                    (grid[piecePositionX + 3, piecePositionY + 1] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX, piecePositionY + 2] == GridSquare.Moving) &&
                    (grid[piecePositionX + 2, piecePositionY + 3] != GridSquare.Empty) &&
                    (grid[piecePositionX + 2, piecePositionY + 3] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 2, piecePositionY] == GridSquare.Moving) &&
                    (grid[piecePositionX, piecePositionY + 1] != GridSquare.Empty) &&
                    (grid[piecePositionX, piecePositionY + 1] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 3, piecePositionY + 2] == GridSquare.Moving) &&
                    (grid[piecePositionX + 2, piecePositionY] != GridSquare.Empty) &&
                    (grid[piecePositionX + 2, piecePositionY] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 1, piecePositionY + 3] == GridSquare.Moving) &&
                    (grid[piecePositionX + 3, piecePositionY + 2] != GridSquare.Empty) &&
                    (grid[piecePositionX + 3, piecePositionY + 2] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX, piecePositionY + 1] == GridSquare.Moving) &&
                    (grid[piecePositionX + 1, piecePositionY + 3] != GridSquare.Empty) &&
                    (grid[piecePositionX + 1, piecePositionY + 3] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 1, piecePositionY + 1] == GridSquare.Moving) &&
                    (grid[piecePositionX + 1, piecePositionY + 2] != GridSquare.Empty) &&
                    (grid[piecePositionX + 1, piecePositionY + 2] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 2, piecePositionY + 1] == GridSquare.Moving) &&
                    (grid[piecePositionX + 1, piecePositionY + 1] != GridSquare.Empty) &&
                    (grid[piecePositionX + 1, piecePositionY + 1] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 2, piecePositionY + 2] == GridSquare.Moving) &&
                    (grid[piecePositionX + 2, piecePositionY + 1] != GridSquare.Empty) &&
                    (grid[piecePositionX + 2, piecePositionY + 1] != GridSquare.Moving)) checker = true;

                if ((grid[piecePositionX + 1, piecePositionY + 2] == GridSquare.Moving) &&
                    (grid[piecePositionX + 2, piecePositionY + 2] != GridSquare.Empty) &&
                    (grid[piecePositionX + 2, piecePositionY + 2] != GridSquare.Moving)) checker = true;

                if (!checker)
                {
                    aux = piece[0, 0];
                    piece[0, 0] = piece[3, 0];
                    piece[3, 0] = piece[3, 3];
                    piece[3, 3] = piece[0, 3];
                    piece[0, 3] = aux;

                    aux = piece[1, 0];
                    piece[1, 0] = piece[3, 1];
                    piece[3, 1] = piece[2, 3];
                    piece[2, 3] = piece[0, 2];
                    piece[0, 2] = aux;

                    aux = piece[2, 0];
                    piece[2, 0] = piece[3, 2];
                    piece[3, 2] = piece[1, 3];
                    piece[1, 3] = piece[0, 1];
                    piece[0, 1] = aux;

                    aux = piece[1, 1];
                    piece[1, 1] = piece[2, 1];
                    piece[2, 1] = piece[2, 2];
                    piece[2, 2] = piece[1, 2];
                    piece[1, 2] = aux;
                }

                for (int j = GridVerticalSize - 2; j >= 0; j--)
                {
                    for (int i = 1; i < GridHorizontalSize - 1; i++)
                    {
                        if (grid[i, j] == GridSquare.Moving)
                        {
                            grid[i, j] = GridSquare.Empty;
                        }
                    }
                }

                for (int i = piecePositionX; i < piecePositionX + 4; i++)
                {
                    for (int j = piecePositionY; j < piecePositionY + 4; j++)
                    {
                        if (piece[i - piecePositionX, j - piecePositionY] == GridSquare.Moving)
                        {
                            grid[i, j] = GridSquare.Moving;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private static void CheckDetection(ref bool detection)
        {
            for (int j = GridVerticalSize - 2; j >= 0; j--)
            {
                for (int i = 1; i < GridHorizontalSize - 1; i++)
                {
                    if ((grid[i, j] == GridSquare.Moving) &&
                        ((grid[i, j + 1] == GridSquare.Full) || (grid[i, j + 1] == GridSquare.Block)))
                        detection = true;
                }
            }
        }

        private static void CheckCompletion(ref bool lineToDelete)
        {
            int calculator;

            for (int j = GridVerticalSize - 2; j >= 0; j--)
            {
                calculator = 0;

                for (int i = 1; i < GridHorizontalSize - 1; i++)
                {
                    // Count each square of the line
                    if (grid[i, j] == GridSquare.Full)
                    {
                        calculator++;
                    }

                    // Check if we completed the whole line
                    if (calculator == GridHorizontalSize - 2)
                    {
                        lineToDelete = true;
                        calculator = 0;

                        // Mark the completed line
                        for (int z = 1; z < GridHorizontalSize - 1; z++)
                        {
                            grid[z, j] = GridSquare.Fading;
                        }
                    }
                }
            }
        }

        private static int DeleteCompleteLines()
        {
            int deletedLines = 0;

            // Erase the completed line
            for (int j = GridVerticalSize - 2; j >= 0; j--)
            {
                while (grid[1, j] == GridSquare.Fading)
                {
                    for (int i = 1; i < GridHorizontalSize - 1; i++)
                    {
                        grid[i, j] = GridSquare.Empty;
                    }

                    for (int j2 = j - 1; j2 >= 0; j2--)
                    {
                        for (int i2 = 1; i2 < GridHorizontalSize - 1; i2++)
                        {
                            if (grid[i2, j2] == GridSquare.Full)
                            {
                                grid[i2, j2 + 1] = GridSquare.Full;
                                grid[i2, j2] = GridSquare.Empty;
                            }
                            else if (grid[i2, j2] == GridSquare.Fading)
                            {
                                grid[i2, j2 + 1] = GridSquare.Fading;
                                grid[i2, j2] = GridSquare.Empty;
                            }
                        }
                    }

                    deletedLines++;
                }
            }

            return deletedLines;
        }
    }
}
