﻿using System;
using System.Runtime.InteropServices;

namespace WindowsTerminalQuake.Native
{
	public static class User32
	{
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

		[DllImport("user32.dll")]
		public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, NCmdShow nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

		[DllImport("user32.dll")]
		public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

		public struct Rect
		{
			public int Left { get; set; }
			public int Top { get; set; }
			public int Right { get; set; }
			public int Bottom { get; set; }
		}

		public const int GWL_EX_STYLE = -20;

		public const int LWA_ALPHA = 0x2;
		public const int LWA_COLORKEY = 0x1;

		public const int WS_EX_APPWINDOW = 0x00040000;
		public const int WS_EX_LAYERED = 0x80000;
		public const int WS_EX_TOOLWINDOW = 0x00000080;

		public const int WS_EX_TOPMOST = 0x00000008;
		public const int HWND_TOPMOST = -1;
	}
}