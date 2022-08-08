using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Drawing;

namespace ConsoleGameEngine
{
	public abstract partial class Engine 
	{
		#region Console Constants
		private readonly IntPtr STD_INPUT_HANDLE = GetStdHandle(-10);
		private readonly IntPtr STD_OUTPUT_HANDLE = GetStdHandle(-11);
		private readonly IntPtr STD_ERROR_HANDLE = GetStdHandle(-12);
		private readonly IntPtr _consoleHandle = GetConsoleWindow();
		private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
		private const uint ENABLE_MOUSE_INPUT = 0x0010;
		private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
		#endregion

		// A bunch of native C++ function contained in the Win32API that take full advantage of the Console
		#region NativeMethods
		[DllImport("user32.dll", SetLastError = true)]
		public static extern void SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int width, int height, uint flags);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern short GetAsyncKeyState(int vKey);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetCursorPos(out Point vKey);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetClientRect(IntPtr hWnd, ref Rectangle lpRect);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DrawMenuBar(IntPtr hWnd);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetConsoleWindow();
		
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern SafeFileHandle CreateFile(string fileName,
			[MarshalAs(UnmanagedType.U4)] uint fileAccess,
			[MarshalAs(UnmanagedType.U4)] uint fileShare,
			IntPtr securityAttributes,
			[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			[MarshalAs(UnmanagedType.U4)] int flags,
			IntPtr template);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool WriteConsoleOutputW(
			SafeFileHandle hConsole,
			CharInfo[] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpWriteRegion);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr SetCurrentConsoleFontEx(
			IntPtr ConsoleOutput,
			bool MaximumWindow,
			ref CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
		
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
		#endregion

		#region Native Structs
		[StructLayout(LayoutKind.Sequential)]
		public struct Coord
		{
			public short X = 0;
			public short Y = 0;

			public Coord(short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
		}

		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct CharInfo
		{
			[FieldOffset(0)] public char UnicodeChar = ' ';
			[FieldOffset(2)] public ushort Attributes = 0x00;

			public CharInfo(char uniChar, ushort attr)
			{
				UnicodeChar = uniChar;
				Attributes = attr;
			}
			public string ToHex()
			{
				return $"{(ushort)UnicodeChar:X4}{Attributes:X4}";
			}
			public override string ToString()
			{
				return UnicodeChar.ToString();
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SmallRect 
		{
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct CONSOLE_FONT_INFO_EX
		{
			public uint cbSize;
			public uint nFont;
			public Coord dwFontSize;
			public int FontFamily;
			public int FontWeight;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string FaceName;
		}
		#endregion
	}
}
