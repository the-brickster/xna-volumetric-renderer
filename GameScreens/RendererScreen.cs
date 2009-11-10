﻿#region File Description
//-------------------------------------------------------------------------------------------------
// RendererScreen.cs
//
// 
//-------------------------------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Engine.Diagnostics;
using Graphics.Diagnostics;
#endregion

namespace Graphics.GameScreens
{
	public class RendererScreen : Engine.Screen.GameScreen
	{
		#region Fields
		private ContentManager rendererContent;
		private Texture2D background;
		private SpriteFont font;
		private SpriteBatch spriteBatch;

		private Texture3D teapot;
		#endregion

		#region Initialization
		public RendererScreen()
			: base()
		{
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
		}

		public override void LoadContent()
		{
			base.LoadContent();

			rendererContent = new ContentManager(ScreenManager.Game.Services, "Content\\GameScreens");
			background = rendererContent.Load<Texture2D>("renderer");
			font = rendererContent.Load<SpriteFont>("menufont");
			spriteBatch = new SpriteBatch(ScreenManager.GraphicsDevice);

			// Load the teapot volume data.
			teapot = new Texture3D(ScreenManager.GraphicsDevice, 256, 256, 178, 0, TextureUsage.Linear, SurfaceFormat.Single);
			Engine.Input.RawFileReader rawFileReader = new Engine.Input.RawFileReader();
			rawFileReader.Open("..\\..\\..\\BostonTeapot.raw");
			rawFileReader.GetRawData(teapot);
			rawFileReader.Close();

			ScreenManager.Game.ResetElapsedTime();
		}

		public override void UnloadContent()
		{
			rendererContent.Unload();

			base.UnloadContent();
		}
		#endregion

		#region Update
		public override void Update(GameTime gameTime, bool hasFocus, bool isObscured)
		{
			base.Update(gameTime, hasFocus, isObscured);
		}

		public override void HandleInput(Engine.Input.InputState input)
		{
			base.HandleInput(input);

			if (input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape, PlayerIndex.One))
			{
				// Reload the main menu and return.
				ScreenManager.AddScreen(new BackgroundScreen());
				ScreenManager.AddScreen(new MainMenuScreen());
				Finished();
			}
		}
		#endregion

		#region Draw
		public override void Draw(GameTime gameTime)
		{
			Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
			Rectangle fullscreen = new Rectangle(0, 0, viewport.Width, viewport.Height);
			byte alpha = TransitionAlpha;

			spriteBatch.Begin();
			spriteBatch.Draw(background, fullscreen, new Color(alpha, alpha, alpha));
			spriteBatch.DrawString(font, "Here's where our awesome renderer will be showcased!", new Vector2(250f, 450f), new Color(Color.White, alpha));
			spriteBatch.DrawString(font, "ESC - Exit Renderer", new Vector2(10f, 915f), new Color(Color.Yellow, alpha));
			spriteBatch.End();

			base.Draw(gameTime);
		}
		#endregion
	}
}