using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Threading;

namespace Rotris
{
    public class Rotris : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        int gameFieldDimensions;  //the number of boxes in the field, gFD x gFD, in a square

        GameObject[,] boxes; //this array holds all of the game's boxes except for the box to be dropped
        GameObject[] topBoxes;  //this is the array that the top box will be in
        int topRowPointer;
        KeyboardState oldState;
        public static Keys[] dPad = new Keys[] { Keys.Up, Keys.Down, Keys.Left, Keys.Right };
        public static Keys dropBox = Keys.Space;
        DPad myDPad = new DPad(dPad);
        Color[] color = new Color[] { Color.Red, Color.Blue, Color.Green };
        static Texture2D[] boxColors;
        float elapsedTime = 0;
        public Rotris()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
            oldState = Keyboard.GetState();
        }


        private bool InitGraphicsMode(int iWidth, int iHeight, bool bFullScreen)
        {
            /// <summary>
            /// Attempt to set the display mode to the desired resolution.  Itterates through the display
            /// capabilities of the default graphics adapter to determine if the graphics adapter supports the
            /// requested resolution.  If so, the resolution is set and the function returns true.  If not,
            /// no change is made and the function returns false.
            /// </summary>
            /// <param name="iWidth">Desired screen width.</param>
            /// <param name="iHeight">Desired screen height.</param>
            /// <param name="bFullScreen">True if you wish to go to Full Screen, false for Windowed Mode.</param>
            /// Reference: http://forums.create.msdn.com/forums/p/1031/107718.aspx
            // If we aren't using a full screen mode, the height and width of the window can
            // be set to anything equal to or smaller than the actual screen size.
            if (bFullScreen == false)
            {
                if ((iWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                    && (iHeight <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height))
                {
                    graphics.PreferredBackBufferWidth = iWidth;
                    graphics.PreferredBackBufferHeight = iHeight;
                    graphics.IsFullScreen = bFullScreen;
                    graphics.ApplyChanges();
                    return true;
                }
            }
            else
            {
                // If we are using full screen mode, we should check to make sure that the display
                // adapter can handle the video mode we are trying to set.  To do this, we will
                // iterate thorugh the display modes supported by the adapter and check them against
                // the mode we want to set.
                foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check the width and height of each mode against the passed values
                    if ((dm.Width == iWidth) && (dm.Height == iHeight))
                    {
                        // The mode is supported, so set the buffer formats, apply changes and return
                        graphics.PreferredBackBufferWidth = iWidth;
                        graphics.PreferredBackBufferHeight = iHeight;
                        graphics.IsFullScreen = bFullScreen;
                        graphics.ApplyChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            InitGraphicsMode(500, 500, false);  //height, width, fullscreen
            gameFieldDimensions = 15;  //this is the size of the game field
            boxes = new GameObject[gameFieldDimensions, gameFieldDimensions];  //this is the array of boxes that forms the gameField
            boxColors = new Texture2D[4];  //these are the textures used to identify the boxes
            InitializeBoxColors();
            topBoxes = new GameObject[gameFieldDimensions];
            topRowPointer = 0;
            SetTopRowPositions();
            SetGameFieldPositions();
            FillBottomRow();
            getRandomColor(topBoxes[0]);  //sets top box to a random color
        }

        private void InitializeBoxColors()
        {
            boxColors[0] = Content.Load<Texture2D>("blackBox");  //texture for blank box
            boxColors[1] = Content.Load<Texture2D>("redBox");
            boxColors[2] = Content.Load<Texture2D>("blueBox");
            boxColors[3] = Content.Load<Texture2D>("greenBox");
        }

        private void SetGameFieldPositions()
        {
            for (int row = 0; row < boxes.GetLength(0); row++)
            {
                for (int column = 0; column < boxes.GetLength(1); column++)
                {
                    Vector2 v = new Vector2((float)((GraphicsDevice.Viewport.Width / (gameFieldDimensions)) * (column)),
                        (GraphicsDevice.Viewport.Height / (gameFieldDimensions + 1)) * (row + 1));
                    boxes[row, column] = new GameObject(boxColors[0], v);
                }
            }
        }

        private void SetTopRowPositions()
        {
            for (int i = 0; i < topBoxes.Length; i++)  //positioning topBoxes in top row
            {
                Vector2 v = new Vector2((float)((GraphicsDevice.Viewport.Width / gameFieldDimensions) * i), 0);
                topBoxes[i] = new GameObject(boxColors[0], v);
            }
        }

        private void FillBottomRow()
        {
            for (int column = 0; column < boxes.GetLength(1); column++)  //fills in initial bottom row to start
            {
                getRandomColor(boxes[boxes.GetLength(0) - 1, column]);
                Thread.Sleep(10);
            }
        }

        protected override void UnloadContent() { }

        private void getRandomColor(GameObject go)
        {
            Random r = new Random();
            go.Texture = boxColors[(int)r.Next(1,4)];  //gets random non-black color
        }

        protected override void Update(GameTime gameTime)
        {
            this.IsFixedTimeStep = false;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f);
            UpdateInput(gameTime);
            base.Update(gameTime);
            //gravity happens on every update
            ApplyGravity(boxes, boxColors);
            //check for matches happens on every update
            CheckForMatches(boxes, boxColors);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            foreach (GameObject go in topBoxes)
                spriteBatch.Draw(go.Texture, go.Position, Color.White);

            foreach (GameObject go in boxes)
                spriteBatch.Draw(go.Texture, go.Position, Color.White);

            spriteBatch.End();
            
            base.Draw(gameTime);
        }

        private void UpdateInput(GameTime gameTime)
        {
            ///small delay algorithm referenced from
            ///http://social.msdn.microsoft.com/Forums/en/xnagamestudioexpress/thread/9a95fbfe-f28d-47ef-945f-baa2918e32d4
            elapsedTime += gameTime.ElapsedGameTime.Milliseconds;
            if (elapsedTime > 75)
            {
                KeyboardState newState = Keyboard.GetState();
                //this is where rotate and moveTopBox are called
                boxPositionChange(newState, dPad, dropBox, topBoxes, boxes);
                elapsedTime = 0;
            }
        }

        private void boxPositionChange(KeyboardState newState, Keys[] myKeys, Keys dropButton, GameObject[] topRow, GameObject[,] gameField)
        {//myKeys: up down left right
            if (newState.IsKeyDown(dropButton))
            {
                //drop the block
                DropBoxFromTop(topRow[topRowPointer], boxes);
                //check for matches

            }
            if (newState.IsKeyDown(myKeys[0]))
            {
                //reflect CCW
                RotateClockWise(boxes, boxColors);
                RotateClockWise(boxes, boxColors);
                RotateClockWise(boxes, boxColors);
                //gravity happens
            }
            if (newState.IsKeyDown(myKeys[1]))
            {
                //rotate clockwise
                RotateClockWise(boxes, boxColors);
            }
            if (newState.IsKeyDown(myKeys[2]))  //if left is pressed
            {
                try
                {
                    topRow[topRowPointer - 1].Texture = topRow[topRowPointer].Texture;
                    topRow[topRowPointer].Texture = boxColors[0];
                    topRowPointer--;
                }
                catch { }
            }

            if (newState.IsKeyDown(myKeys[3]))  //if right is pressed
            {
                try
                {
                    topRow[topRowPointer + 1].Texture = topRow[topRowPointer].Texture;
                    topRow[topRowPointer].Texture = boxColors[0];
                    topRowPointer++;
                }
                catch { }
            }
        }
        private void CheckForMatches(GameObject[,] box, Texture2D[] boxColors)
        {
            for (int i = 0; i < box.GetLength(0); i++)
            {
                for (int j = 0; j < box.GetLength(1); j++)
                {
                    try
                    {
                        if (box[i, j].Texture == box[i - 1, j].Texture && box[i, j].Texture== box[i - 2, j].Texture) //if the three boxes have the same color - 1
                        {
                            box[i, j].Texture = boxColors[0];
                            box[i - 1, j].Texture = boxColors[0];
                            box[i - 2, j].Texture = boxColors[0];
                        }
                    }
                    catch { }

                    try
                    {
                        if (box[i, j].Texture == box[i - 1, j].Texture && box[i, j].Texture == box[i + 1, j].Texture) //if the three boxes have the same color - 2
                        {
                            box[i, j].Texture = boxColors[0];
                            box[i - 1, j].Texture = boxColors[0];
                            box[i + 1, j].Texture = boxColors[0];
                        }
                    }
                    catch { }

                    try
                    {
                        if (box[i, j].Texture == box[i + 1, j].Texture && box[i, j].Texture == box[i + 2, j].Texture) //if the three boxes have the same color - 3
                        {
                            box[i, j].Texture = boxColors[0];
                            box[i + 1, j].Texture = boxColors[0];
                            box[i + 2, j].Texture = boxColors[0];
                        }
                    }
                    catch { }

                    try
                    {
                        if (box[i, j].Texture == box[i, j - 1].Texture && box[i, j].Texture == box[i, j - 2].Texture) //if the three boxes have the same color - 4
                        {
                            box[i, j].Texture = boxColors[0];
                            box[i, j - 1].Texture = boxColors[0];
                            box[i, j - 2].Texture = boxColors[0];
                        }
                    }
                    catch { }

                    try
                    {
                        if (box[i, j].Texture == box[i , j - 1].Texture && box[i, j].Texture == box[i, j + 1].Texture) //if the three boxes have the same color - 5
                        {
                            box[i, j].Texture = boxColors[0];
                            box[i, j - 1].Texture = boxColors[0];
                            box[i, j + 1].Texture = boxColors[0];
                        }
                    }
                    catch { }

                    try
                    {
                        if (box[i, j].Texture == box[i , j + 1].Texture && box[i, j].Texture == box[i, j + 2].Texture) //if the three boxes have the same color - 6
                        {
                            box[i, j].Texture = boxColors[0];
                            box[i, j + 1].Texture = boxColors[0];
                            box[i, j + 2].Texture = boxColors[0];
                        }
                    }
                    catch { }
                }
            }


        }

        private void ApplyGravity(GameObject[,] go, Texture2D[] boxColors)
        { //x axis is [1], y axis is [0]
          //i.e.  go[y,x] for actual game objects
            for (int y = go.GetLength(1) - 2; y >= 0; y--)
            {
                for (int x = go.GetLength(0) - 1; x >= 0; x--)
                {
                    if (go[y + 1, x].Texture == boxColors[0])  //if the box below is black
                    {
                        go[y + 1, x].Texture = go[y, x].Texture;
                        go[y, x].Texture = boxColors[0];
                    }
                }
            }
        }

        private void RotateClockWise(GameObject[,] go, Texture2D[] boxColors)
        {
            int n = go.GetLength(0);
            int m = go.GetLength(1);
            Texture2D[,] matrix = new Texture2D[n, m];
            for (int i = 0; i < go.GetLength(0); i++)
            {
                for (int j = 0; j < go.GetLength(1); j++)
                {
                    matrix[i,j] = go[i, j].Texture;
                }
            }

            Texture2D[,] temp1 = new Texture2D[n, m];
            Texture2D[,] temp2 = new Texture2D[n, m];
            Texture2D[,] temp3 = new Texture2D[n, m];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    temp1[i, j] = matrix[n - j - 1, i];
                }
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    temp2[i, j] = temp1[n - j - 1, i];
                }
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    temp3[i, j] = temp2[n - j - 1, i];
                }
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    go[i, j].Texture = temp3[i, j];
                }
            }
        }

        private void DropBoxFromTop(GameObject topBox, GameObject[,] boxes)
        {
            if (boxes[0, topRowPointer].Texture == boxColors[0])  //if the top space is empty, enter the main method
            {
                if (boxes[0, topRowPointer].Texture == boxColors[0] && boxes[1,topRowPointer].Texture != boxColors[0])
                {
                    boxes[0, topRowPointer].Texture = topBoxes[topRowPointer].Texture;
                }
                else
                {
                    for (int row = 0; row < boxes.GetLength(1) - 1; row++)
                    {
                        if (boxes[row + 1, topRowPointer].Texture == boxColors[0]) //if the space below is empty
                        {
                            boxes[row, topRowPointer].Texture = boxColors[0];
                            boxes[row + 1, topRowPointer].Texture = topBoxes[topRowPointer].Texture;
                        }
                        else
                        {
                            topBoxes[topRowPointer].Texture = boxColors[0];
                            getRandomColor(topBoxes[topRowPointer]);
                        }
                    }
                }

            }
            else { } //do nothing;  the box shouldn't be dropped
        }
    }
}