using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace V1JeuVideo
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch spriteBatch;

        private GameState gameState = GameState.Playing;
        private float transitionAlpha = 0f;
        private bool isOnSecondPlatform = false;

        private double collapseTimer = 0;
        private double collapseInterval = 1.0;

        private GamePlayer player;
        private Platform platform1;
        private Platform platform2;

        private Texture2D cellTexture;
        private Texture2D cellTexture2;
        private Texture2D playerTexture;
        private Texture2D blackTexture;

        private int gridWidth;
        private int gridHeight;
        private int cellSize = 80;

        private SpriteFont font;

        private SoundEffect deathSound;
        private SoundEffect roundEndSound;

        private double autoSaveTimer = 0;
        private const double autoSaveInterval = 5.0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            string xmlPath = "/home/lydiaskr/Téléchargements/V1JeuVideo/V1JeuVideo/GameSchema.xml";
            Console.WriteLine($"Chemin XML : {xmlPath}");
            GameProject gameData = LoadGameProject(xmlPath);

            if (gameData != null)
            {
                // Utiliser les données du fichier XML
                gridWidth = gameData.GameSettings.GridWidth;
                gridHeight = gameData.GameSettings.GridHeight;
                cellSize = gameData.GameSettings.CellSize;
                collapseInterval = gameData.GameSettings.CollapseInterval;

                platform1 = new Platform(gridWidth, gridHeight, cellSize, null);
                platform2 = new Platform(gridWidth, gridHeight, cellSize, null);

                player = new GamePlayer(new Vector2(
                    gameData.Entities.Player.Position.X,
                    gameData.Entities.Player.Position.Y
                ), null, cellSize);

                // Initialiser les plateformes avec des cellules
                InitializePlatforms();

                // Configurer la taille de la fenêtre
                _graphics.PreferredBackBufferWidth = gridWidth * cellSize;
                _graphics.PreferredBackBufferHeight = gridHeight * cellSize;
                _graphics.ApplyChanges();
                Console.WriteLine($"Fenêtre définie à {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}");
            }
            else
            {
                Console.WriteLine("Aucune donnée XML chargée. Impossible de continuer.");
                Exit();
            }

            base.Initialize();
        }

        private void InitializePlatforms()
        {
            platform1.InitializeCells();
            platform2.InitializeCells();
            Console.WriteLine("Plateformes initialisées avec des cellules intactes.");
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            try
            {
                font = Content.Load<SpriteFont>("DefaultFont");
                Console.WriteLine("Font chargée avec succès.");

                cellTexture = Content.Load<Texture2D>("Cell");
                Console.WriteLine("CellTexture chargée avec succès.");

                cellTexture2 = Content.Load<Texture2D>("Cell2");
                Console.WriteLine("CellTexture2 chargée avec succès.");

                playerTexture = Content.Load<Texture2D>("Player");
                Console.WriteLine("PlayerTexture chargée avec succès.");

             //   deathSound = Content.Load<SoundEffect>("death");
               // Console.WriteLine("DeathSound chargé avec succès.");

                //roundEndSound = Content.Load<SoundEffect>("round_end");
                //Console.WriteLine("RoundEndSound chargé avec succès.");

                blackTexture = new Texture2D(GraphicsDevice, 1, 1);
                blackTexture.SetData(new[] { Color.Black });
                Console.WriteLine("BlackTexture créée avec succès.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des ressources : {ex.Message}");
                Exit();
            }

            platform1.CellTexture = cellTexture;
            platform2.CellTexture = cellTexture2;
            player.Texture = playerTexture;
        }

        public GameProject LoadGameProject(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Erreur : le fichier '{filePath}' est introuvable.");
                return null;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(GameProject));
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                var gameData = (GameProject)serializer.Deserialize(fs);
                Console.WriteLine($"XML chargé : Grille {gameData.GameSettings.GridWidth}x{gameData.GameSettings.GridHeight}, Taille cellule : {gameData.GameSettings.CellSize}");
                return gameData;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            autoSaveTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveGameProject("GameProject.xml");
                autoSaveTimer = 0;
            }

            switch (gameState)
            {
                case GameState.Playing:
                    HandlePlayingState(gameTime, keyboardState);
                    break;
                case GameState.Transition:
                    HandleTransitionState(gameTime);
                    break;
                case GameState.TransitionToGameOver:
                    HandleTransitionToGameOver(gameTime);
                    break;
                case GameState.GameOver:
                    if (keyboardState.IsKeyDown(Keys.R))
                        RestartGame();
                    break;
            }

            base.Update(gameTime);
        }

        private void HandlePlayingState(GameTime gameTime, KeyboardState keyboardState)
        {
            collapseTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if (collapseTimer >= collapseInterval)
            {
                if (!isOnSecondPlatform)
                {
                    platform1.CollapseRandomCells(new Random(), 3);
                }
                else
                {
                    platform2.CollapseRandomCells(new Random(), 3);
                }

                collapseTimer = 0;
            }

            if (!isOnSecondPlatform)
            {
                player.Move(keyboardState, gameTime, gridWidth, gridHeight, platform1);
            }
            else
            {
                player.Move(keyboardState, gameTime, gridWidth, gridHeight, platform2);
            }

            int row = (int)player.Position.Y;
            int col = (int)player.Position.X;

            if (!isOnSecondPlatform && !platform1.IsCellIntact(row, col))
            {
                gameState = GameState.Transition;
                transitionAlpha = 0f;
               // deathSound.Play();
            }
            else if (isOnSecondPlatform && !platform2.IsCellIntact(row, col))
            {
                gameState = GameState.TransitionToGameOver;
                transitionAlpha = 0f;
                //deathSound.Play();
            }

            player.Update(gameTime);
        }

        private void HandleTransitionState(GameTime gameTime)
        {
            transitionAlpha += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;

            if (transitionAlpha >= 1f)
            {
                if (isOnSecondPlatform)
                {
                    gameState = GameState.GameOver;
                }
                else
                {
                    isOnSecondPlatform = true;
                    LoadSecondPlatform();
                    SaveGameProject("/home/lydiaskr/Téléchargements/V1JeuVideo/V1JeuVideo/GameSchema.xml");
                    gameState = GameState.Playing;
                    transitionAlpha = 0f;
                }
            }
        }

        private void HandleTransitionToGameOver(GameTime gameTime)
        {
            transitionAlpha += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;

            if (transitionAlpha >= 1f)
            {
               // roundEndSound.Play();
                SaveGameProject("/home/lydiaskr/Téléchargements/V1JeuVideo/V1JeuVideo/GameSchema.xml");
                gameState = GameState.GameOver;
            }
        }

        private void RestartGame()
        {
            isOnSecondPlatform = false;
            Initialize();
            gameState = GameState.Playing;
        }

        public void SaveGameProject(string filePath)
        {
            Console.WriteLine("Sauvegarde effectuée !");
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            if (gameState == GameState.Playing || gameState == GameState.Transition || gameState == GameState.TransitionToGameOver)
            {
                if (!isOnSecondPlatform)
                {
                    platform1.Draw(spriteBatch, font);
                }
                else
                {
                    platform2.Draw(spriteBatch, font);
                }

                player.Draw(spriteBatch);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void LoadSecondPlatform()
        {
            platform2 = new Platform(gridWidth, gridHeight, cellSize, cellTexture2);
            player.Position = new Vector2(gridWidth / 2, gridHeight - 1);
        }
    }

    public enum GameState
    {
        Playing,
        Transition,
        TransitionToGameOver,
        GameOver
    }
}
