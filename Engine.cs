#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;

namespace ConsoleGameEngine
{
	public abstract partial class Engine
	{
		private SafeFileHandle _h;
		public int ScreenWidth { get; private set; }
		public int ScreenHeight { get; private set; }
		public int FontWidth { get; private set; }
		public int FontHeight { get; private set; }
		public bool Borderless { get; private set; }
		public ConsoleKey ExitKey { get; private set; }
		public CharInfo[] Buffer { get; set; }
		private SmallRect bufferRegion = new() { Left = 0, Top = 0 };

		public int Framerate { get; set; }
		private Thread _gameThread;
		private double[] _framerateSamples;

		private DateTime prevFrametime;
		private DateTime currentFrametime;
		public TimeSpan elapsedTime;
		private bool running;

		private short[] _oldKeyState = new short[256];
		private short[] _newKeyState = new short[256];
		public KeyState[] KeyStates { get; private set; }

		/// <summary>
		/// Constructor for the ConsoleGameEngine written as a normal Method so that it can be called from inherited classes.
		/// There is probably a built-in way to do that, but I don't know it yet.
		/// </summary>
		/// <param name="resX">The amount of real pixels desired for the horizontal dimension</param>
		/// <param name="resY">The amount of real pixels desired for the vertical dimension</param>
		/// <param name="fontW">The width of the font the Engine will use to print a 'pixel'</param>
		/// <param name="fontH">The height of the font the Engine will use to print a 'pixel'</param>
		/// <param name="framerate">The desired framerate for the Console window to refresh at, set 0 for no limit.</param>
		/// <param name="borderless">Toggle for rendering the Console borderless.</param>
		public void Construct(int resX = 1920, int resY = 1080, int fontW = 4, int fontH = 4, int framerate = 30, bool borderless = false, ConsoleKey exitKey = ConsoleKey.Escape)
		{
			ScreenWidth = resX / fontW;
			ScreenHeight = resY / fontH;
			FontWidth = fontW;
			FontHeight = fontH;
			Framerate = framerate;
			Borderless = borderless;
			ExitKey = exitKey;

			_h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

			if (!_h.IsInvalid)
			{
				Console.Title = "Console Game Engine Default";
				Console.CursorVisible = false;
				_framerateSamples = new double[Framerate];
				KeyStates = new KeyState[256];

				ScreenInit();
				BufferInit();
				GetConsoleMode(STD_INPUT_HANDLE, out uint mode);
				mode &= ~ENABLE_QUICK_EDIT_MODE;
				mode |= ENABLE_MOUSE_INPUT;

				SetConsoleMode(STD_INPUT_HANDLE, mode);
				//SetConsoleMode(STD_OUTPUT_HANDLE, mode);

				Create();

				_gameThread = new Thread(() => GameLoop());
				running = true;

				_gameThread.Start();
				_gameThread.Join();
			}
		}

		#region Initialization
		/// <summary>
		/// Creates a array of CharInfo that will serve as the screen buffer that will be output to the Console window.
		/// Also initializes are the values to Black spaces.
		/// </summary>
		public void BufferInit()
		{
			Buffer = new CharInfo[ScreenWidth * ScreenHeight];
			for(int i = 0; i < Buffer.Length; i++)
			{
				Buffer[i] = new CharInfo(' ', 0xF0);
			}
			Console.SetBufferSize(ScreenWidth, ScreenHeight);
		}

		/// <summary>
		/// Sets the Console Font to Consolas since it has the ability to be sized in a square.
		/// Sets the Console window to the proper dimensions based on the ScreenWidth and ScreenHeight set in the constructor.
		/// Will render the Console borderless if the toggle is set to true.
		/// </summary>
		private void ScreenInit()
		{
			CONSOLE_FONT_INFO_EX cfi = new();
			cfi.cbSize = (uint)Marshal.SizeOf(cfi);
			cfi.nFont = 0;
			cfi.dwFontSize.X = (short)FontWidth;
			cfi.dwFontSize.Y = (short)FontHeight;
			cfi.FontWeight = 1000;
			cfi.FaceName = "Consolas";

			SetCurrentConsoleFontEx(STD_OUTPUT_HANDLE, false, ref cfi);

			bufferRegion.Right = (short)ScreenWidth;
			bufferRegion.Bottom = (short)ScreenHeight;

			// The pixel width and height of the Console window frame for Window 10 & 11.
			short xDif = 33;
			short yDif = 39;

			if (Borderless)
			{
				SetWindowLong(_consoleHandle, -16, 0x00080000);
				xDif = 0;
				yDif = 0;
			}

			IntPtr desktopHandle = GetDesktopWindow();
			GetWindowRect(desktopHandle, out Rectangle desktopRect);

			int wPosX = (desktopRect.Right / 2) - ((ScreenWidth * FontWidth) / 2);
			int wPosY = (desktopRect.Bottom / 2) - ((ScreenHeight * FontHeight) / 2);
			int wWidth = ScreenWidth * FontWidth + xDif;
			int wHeight = ScreenHeight * FontHeight + yDif;

			SetWindowPos(_consoleHandle, -2, wPosX, wPosY, wWidth, wHeight, 0x0040);
			DrawMenuBar(_consoleHandle);
		}

		/// <summary>
		/// The 'Draw Call'. Prints the Buffer to the Console window in one go.
		/// </summary>
		public void BufferDraw()
		{
			WriteConsoleOutputW(_h, Buffer, new Coord((short)ScreenWidth, (short)ScreenHeight), new Coord(0, 0), ref bufferRegion);
		}
		#endregion

		#region Drawing
		/// <summary>
		/// The one and only thing you need to draw in the Console.
		/// </summary>
		/// <param name="x">X coordinate in the Buffer.</param>
		/// <param name="y">Y coordinate in the Buffer.</param>
		/// <param name="c">The Unicode character to print.</param>
		/// <param name="col">The foreground and background color combined in ushort hexidecimal format.</param>
		public void SetPixel(int x, int y, char c, ushort col)
		{
			if (x >= 0 && x < ScreenWidth && y >= 0 && y < ScreenHeight)
			{
				Buffer[y * ScreenWidth + x].UnicodeChar = c;
				Buffer[y * ScreenWidth + x].Attributes = col;
			}
		}

		/// <summary>
		/// Formula used to clamp a value so that it is within the specified bounds.
		/// </summary>
		/// <param name="vec"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public int Clamp(int vec, int min, int max)
		{
			return Math.Min(Math.Max(vec, min), max);
		}


		public void DrawString(string s, int x, int y, int width, int height, double scaleValue, double scaleVal2)
		{
			for (double j = 0; j < height / (height / scaleVal2); j++)
			{
				for (double i = 0; i < width / (width / scaleValue); i++)
				{
					double sampleX = i / scaleValue;
					double sampleY = j / scaleVal2;
					var c = SampleString(s,width, height, sampleX, sampleY);
					if (c.Attributes != 0xFF)
						SetPixel((int)(x + i), y + (int)j, c.UnicodeChar, c.Attributes);
				} 
			}
		}
		/// <summary>
		/// Will take in a string representation of an ASCII art image and sample the character in the offset coordinate of x, y.
		/// </summary>
		/// <param name="width">The width of the string image. NOT the length of the string.</param>
		/// <param name="height">The height of the string image. NOT the length of the string.</param>
		/// <param name="scaleWidth">The width that the string image should be scaled to.</param>
		/// <param name="scaleHeight">The height that the string image should be scaled to.</param>
		/// <param name="s">The string image.</param>
		/// <returns></returns>
		public CharInfo SampleString(string s, double width, double height, double scaleWidth, double scaleHeight)
		{
			int sx = (int)(scaleWidth * width);
			int sy = (int)(scaleHeight * height);

			if (sx < 0 || sx >= width || sy < 0 || sy>= height)
			{
				// Black space
				return new CharInfo(' ', 0x00);
			}
			
			return new CharInfo(s[sy * (int)width + sx], 0x0F);
		}

		/// <summary>
		/// Will draw the sprite on the screen starting at the specified x and y coordinate and drawing it right and down. Any pixel that is pure white is treated as alpha.
		/// </summary>
		/// <param name="x">The x-coordinate<param>
		/// <param name="y">The y-coordinate</param>
		/// <param name="s">The non-null Sprite object</param>
		/// <param name="xOffset">The offset for the x-axis if the Sprite's picture is not centered in its dimensions.</param>
		public void DrawSprite(int x, int y,ref Sprite s , double scaleValue, double scaleValue2, double xOffset = 0.0)
		{
			//scaleValue = 
			for (double j = 0; j < s.Height / (s.Height / scaleValue2); j++)
			{
				for (double i = 0; i < s.Width / (s.Width / scaleValue); i++)
				{
					double sampleX = i / scaleValue;
					double sampleY = j / scaleValue2;
					var c = SampleSprite(s, sampleX, sampleY);
					if (c.Attributes != 0xFF)
						SetPixel((int)(x + i + xOffset), y + (int)j, c.UnicodeChar, c.Attributes);
				}
			}
		}

		/// <summary>
		/// Will sample the pixels in a Sprite by offsetting the x and y coordinates by scaling them to the scaleWidth and scaleHeight.
		/// </summary>
		/// <param name="s">A non-null Sprite object.</param>
		/// <param name="scaleWidth">The desired width to scale to.</param>
		/// <param name="scaleHeight">The desired height to scale to.</param>
		/// <returns></returns>
		public CharInfo SampleSprite(Sprite s, double scaleWidth, double scaleHeight)
		{
			int sx = (int)(scaleWidth * s.Width);
			int sy = (int)(scaleHeight * (s.Height - 1));
			if (sx < 0 || sx >= s.Width || sy < 0 || sy >= s.Height)
			{
				return new CharInfo(' ', 0xFF);
			}
			else
			{
				return s.Glyphs[sy * s.Width + sx].Attributes != 0xFF ? s.Glyphs[sy * s.Width + sx] : new CharInfo(' ', 0xFF);
			}
		}

		#endregion

		#region Overrides & GameLoop Logic
		/// <summary>
		/// This is the method that should contain all the initial object creation and setup for your game.
		/// </summary>
		public abstract void Create();
		
		/// <summary>
		/// Put all the dynamic logic here.
		/// </summary>
		public abstract void Update();
		
		/// <summary>
		/// Put your render call logic here.
		/// </summary>
		public abstract void Render();

		/// <summary>
		/// Loop logic. Creates a thread and starts it. If a framerate is set the thread is locked to it to the best of Task.Delay()'s ability.
		/// </summary>
		private void GameLoop()
		{
			var frametimeTarget = Framerate == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(1000 / Framerate);
			prevFrametime = DateTime.UtcNow;
			currentFrametime = DateTime.UtcNow;

			int frameCounter = 0;

			while (running)
			{
				currentFrametime = DateTime.UtcNow;
				elapsedTime = currentFrametime - prevFrametime;
				prevFrametime = currentFrametime;

				frameCounter = frameCounter % Framerate;

				GetKeyStates();
				Update();
				Render();

				_framerateSamples[frameCounter] = elapsedTime.TotalSeconds;

				if (Framerate > 0)
				{
					TimeSpan sleepDuration = frametimeTarget - elapsedTime;
					if (sleepDuration > TimeSpan.Zero)
					{
						Task.Delay(sleepDuration).Wait();
					}
				}

				frameCounter++;
				CheckForExit();
			}
		}

		/// <summary>
		/// Calculates the averaged frametime based on the desired framerate.
		/// </summary>
		/// <returns></returns>
		public double GetFramerate()
		{
			if (Framerate <= 0) return _framerateSamples.LastOrDefault();
			return 1.0 / (_framerateSamples.Sum() / Framerate);
		}

		/// <summary>
		/// Checks for the pressed exit key. The default is escape.
		/// </summary>
		private void CheckForExit()
		{
			if (GetKeyState(ExitKey).Pressed)
			{
				running = false;
			}
		}
		#endregion

		#region Input
		/// <summary>
		/// Struct that stores the state of keys.
		/// </summary>
		public struct KeyState
		{
			public bool Pressed;
			public bool Released;
			public bool Held;
		}

		/// <summary>
		/// Checks to see if the Console is the focused window.
		/// </summary>
		/// <returns></returns>
		private bool ConsoleFocused()
		{
			return _consoleHandle == GetForegroundWindow();
		}

		/// <summary>
		/// Returns the state of the target ConsoleKey. If the Console is not focuesed it returns null.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public KeyState GetKeyState(ConsoleKey key)
		{
			return KeyStates[(int)key];
		}

		public KeyState GetKeyState(int vKey)
		{
			return KeyStates[vKey];
		}

		/// <summary>
		/// To be ran in the game loop. Checks for the states of all the keys registered in the WinAPI.
		/// Only updates the state if the key's state isn't the same as the previous frame.
		/// </summary>
		public void GetKeyStates()
		{
			for (int i = 0; i < 0xFF; i++)
			{
				_newKeyState[i] = GetAsyncKeyState(i);

				KeyStates[i].Pressed = false;
				KeyStates[i].Released = false;

				if (_newKeyState[i] != _oldKeyState[i])
				{
					if ((_newKeyState[i] & 0x8000) != 0)
					{
						KeyStates[i].Pressed = !KeyStates[i].Held;
						KeyStates[i].Held = true;
					}
					else
					{
						KeyStates[i].Released = true;
						KeyStates[i].Held = false;
					}
				}
				_oldKeyState[i] = _newKeyState[i];
			}
		}
		#endregion

		/// <summary>
		/// The Sprite class that defines a image into a bitmap and translates its pixels into a CharInfo array.
		/// </summary>
		public class Sprite
		{
			public Engine eng;
			public int Width { get; set; }
			public int Height { get; set; }

			public CharInfo[]? Glyphs { get; set; }

			public Sprite() { }
			public Sprite(int w, int h)
			{
				Create(w, h);
			}
			public Sprite(string fileName)
			{
				if (!Load(fileName))
				{
					Create(8, 8);
				}
			}

			private void Create(int w, int h)
			{
				Width = w;
				Height = h;
				Glyphs = new CharInfo[Width * Height];

				for (int i = 0; i < Glyphs.Length; i++)
				{
					Glyphs[i].UnicodeChar = ' ';
					Glyphs[i].Attributes = 0x00;
				}
			}

			public bool Load(string fileName)
			{
				if (!File.Exists(fileName)) return false;
				Bitmap bm = new(fileName);
				Create(bm.Width, bm.Height);
				for (int y = 0; y < Height; y++)
				{
					for (int x = 0; x < Width; x++)
					{
						Glyphs[y * Width + x].UnicodeChar = ' ';
						Glyphs[(y * Width + x)].Attributes = FromColor(bm.GetPixel(x, y));
					}
				}
				return true;
			}

			public ushort FromColor(Color c)
			{
				if (c.R == 255 && c.G == 255 && c.B == 255) return 0xFF;
				int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 0x80 : 0x00;
				index |= (c.R > 64) ? 0x40 : 0x00;
				index |= (c.G > 64) ? 0x20 : 0x00;
				index |= (c.B > 64) ? 0x10 : 0x00;
				return (ushort)(ConsoleColor)index;
			}
		}
	}
}
