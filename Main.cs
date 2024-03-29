#region File Description
//-------------------------------------------------------------------------------------------------
// VolumetricRenderer.cs
//
// Base class and entry point for the VolumetricRenderer game.
//-------------------------------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Renderer.Diagnostics;
using Renderer.Graphics.Camera;
using Renderer.Graphics.Screen;
using Renderer.Input;
#endregion

namespace Renderer
{
    /// <summary>
	/// Base class for the game.
	/// </summary>
    public class VolumetricRendererGame : Microsoft.Xna.Framework.Game
    {
        private delegate void UpdateMethod(GameTime gameTime);
		private delegate void DrawMethod(GameTime gameTime);

		#region Fields
		private GraphicsDeviceManager graphicsDM;
		private ScreenManager screenManager;
		private InputState input;
        private Camera camera;

		private volatile GameTime gameTime;
		public Thread mainThread;			// Should be private, but 'Warnings as Errors' is turned on.
		public Thread updateThread;			// Should be private, but 'Warnings as Errors' is turned on.
		public Thread drawThread;			// Should be private, but 'Warnings as Errors' is turned on.
		public Thread handleAssertThread;	// Should be private, but 'Warnings as Errors' is turned on.
		public object updateLock;			// Should be private, but 'Warnings as Errors' is turned on.
		public object drawLock;				// Should be private, but 'Warnings as Errors' is turned on.
		private UpdateMethod updateMethod;
		private DrawMethod drawMethod;
		private bool assertHandled;
		#endregion

        #region Properties
		/// <summary>
		/// Gets the GraphicsDeviceManager instance for this game.
		/// </summary>
		public GraphicsDeviceManager GraphicsDM
		{
			get
			{ return graphicsDM; }
		}

		/// <summary>
		/// Gets the main ScreenManager instance for this game.
		/// </summary>
		public ScreenManager ScreenManager
		{
			get
			{ return screenManager; }
		}

		/// <summary>
		/// Gets the InputState instance for this game.
		/// </summary>
		public InputState Input
		{
			get
			{ return input; }
		}

        /// <summary>
        /// Gets the Camera instance for this game.
        /// </summary>
        public Camera Camera
        {
            get
			{ return camera; }
        }
		#endregion

		#region Initialization
		public VolumetricRendererGame()
		{
			Debug.OnAssert += new EventHandler<DebugEventArgs>(HandleAssert);
			//Debug.BlockAfterAssert = true;

			// Set up and run initial threads.
			//mainThread = Thread.CurrentThread;
			//mainThread.Name = "Main";

			//updateLock = new object();
			//updateThread = new Thread(new ThreadStart(UpdateThread));
			//updateThread.Name = "Update";
			//updateThread.IsBackground = true;
			//updateThread.Start();

			//drawLock = new object();
			//drawThread = new Thread(new ThreadStart(DrawThread));
			//drawThread.Name = "Draw";
			//drawThread.IsBackground = true;
			//drawThread.Start();

			// Set up the game.
			updateMethod = Update_Normal;
			drawMethod = Draw_Normal;
			assertHandled = false;

			Content.RootDirectory = "Content";

			graphicsDM = new GraphicsDeviceManager(this);
			graphicsDM.PreferredBackBufferWidth = 1280;
			graphicsDM.PreferredBackBufferHeight = 960;
            graphicsDM.PreferMultiSampling = false;
            graphicsDM.SynchronizeWithVerticalRetrace = false;
            graphicsDM.IsFullScreen = false;

			input = new InputState();
			screenManager = new ScreenManager(this, input);
			Components.Add(screenManager);

			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			base.Initialize();

			MenuScreen.LoadSharedContent();
			screenManager.AddScreen(new BackgroundScreen());
			screenManager.AddScreen(new MainMenuScreen());

            camera = new Camera(graphicsDM.GraphicsDevice.Viewport);
		}

		protected override void UnloadContent()
		{
			base.UnloadContent();

			MenuScreen.UnloadSharedContent();
		}
		#endregion

		#region Update
		protected override void Update(GameTime gameTime)
		{
			this.gameTime = gameTime;
			input.Update();
            camera.Update(gameTime);

			//updateMethod(gameTime);
			base.Update(gameTime);
		}

		/// <summary>
		/// Normal update method to run, which signals the update thread, as long as there's no assert.
		/// </summary>
		private void Update_Normal(GameTime gameTime)
		{
			lock (updateLock)
			{
				Monitor.Pulse(updateLock);
				Monitor.Wait(updateLock);
			}
		}

		/// <summary>
		/// Update method that we switch to after an assert, so the Update cycle can still be run
		/// after the update thread has been killed off.
		/// </summary>
		private void Update_Assert(GameTime gameTime)
		{
			while (!assertHandled)
				Thread.Sleep(TimeSpan.FromSeconds(1));

			base.Update(gameTime);
		}

		/// <summary>
		/// Thread for running the Update cycle, so it can be killed after an assert.
		/// </summary>
		private void UpdateThread()
		{
			while (true)
			{
				lock (updateLock)
				{
					try
					{
						Monitor.Wait(updateLock);
						base.Update(gameTime);
					}
					finally
					{
						Monitor.Pulse(updateLock);
					}
				}
			}
		}
		#endregion

		#region Draw
		protected override void Draw(GameTime gameTime)
		{
			this.gameTime = gameTime;

			//drawMethod(gameTime);
			GraphicsDevice.Clear(Color.Black);
			base.Draw(gameTime);
		}

		/// <summary>
		/// Normal draw method to run, which signals the draw thread, as long as there's no assert.
		/// </summary>
		private void Draw_Normal(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.Black);

			lock (drawLock)
			{
				Monitor.Pulse(drawLock);
				Monitor.Wait(drawLock);
			}
		}

		/// <summary>
		/// Draw method that we switch to after an assert, so the Draw cycle can still be run
		/// after the draw thread has been killed off.
		/// </summary>
		private void Draw_Assert(GameTime gameTime)
		{
			if (assertHandled)
				base.Draw(gameTime);
		}

		/// <summary>
		/// Thread for running the Draw cycle, so it can be killed after an assert.
		/// </summary>
		private void DrawThread()
		{
			while (true)
			{
				lock (drawLock)
				{
					try
					{
						Monitor.Wait(drawLock);
						base.Draw(gameTime);
					}
					finally
					{
						Monitor.Pulse(drawLock);
					}
				}
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Event handler for an assert.
		/// </summary>
		/// <param name="sender">Because Debug is a static class, this is always NULL.</param>
		/// <param name="args">Info about the assert.</param>
		private void HandleAssert(object sender, DebugEventArgs args)
		{
			updateMethod = Update_Assert;
			drawMethod = Draw_Assert;

			// This method is running on the thread that asserted, so start a new thread for the 
			// handler so it can kill off all the worker threads without killing itself.
			//handleAssertThread = new Thread(new ParameterizedThreadStart(HandleAssertThread));
			//handleAssertThread.Name = "HandleAssert";
			//handleAssertThread.IsBackground = true;
			//handleAssertThread.Start(args);
			HandleAssertThread(args);
		}

		/// <summary>
		/// Thread for handling an assert, so it can kill off other threads without being affected.
		/// </summary>
		/// <param name="obj">DebugEventArgs instance.</param>
		private void HandleAssertThread(object obj)
		{
			DebugEventArgs args = (DebugEventArgs)obj;

			// Abort all worker threads, and give them plenty of time to do so.
			//updateThread.Abort();
			//drawThread.Abort();
			//Thread.Sleep(TimeSpan.FromSeconds(2));

			Components.Clear();
			Components.Add(screenManager);

			screenManager.RemoveAllScreens();
			screenManager.AddScreen(new AssertScreen(args));

			assertHandled = true;
			//Debug.UnblockAssert();
		}
		#endregion
    }

	/// <summary>
	/// Entry point class for the game.
	/// </summary>
	public static class VolumetricRenderer
	{
		#region Fields
		private static VolumetricRendererGame game;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the VolumetricRendererGame instance.
		/// </summary>
		public static VolumetricRendererGame Game
		{
			get
			{ return game; }
		}
		#endregion

		#region Entry Point
		/// <summary>
		/// The main entry point for the game.
		/// </summary>
		/// <param name="args">Command-line arguments passed into the game.</param>
		public static void Main(string[] args)
		{
			Debug.SetRootDirName("VolumetricRenderer");

			try
			{
				game = new VolumetricRendererGame();
				game.Run();
			}
			finally
			{
				game.Dispose();
			}
		}
		#endregion
	}
}
