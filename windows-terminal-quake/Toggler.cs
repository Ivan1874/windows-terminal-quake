﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsTerminalQuake.Native;

namespace WindowsTerminalQuake
{
	public class Toggler : IDisposable
	{
		private Process _process;

		private readonly List<int> _registeredHotKeys = new List<int>();

		public Toggler(Process process)
		{
			_process = process;

			// Hide from taskbar
			User32.SetWindowLong(_process.MainWindowHandle, User32.GWL_EX_STYLE, (User32.GetWindowLong(_process.MainWindowHandle, User32.GWL_EX_STYLE) | User32.WS_EX_TOOLWINDOW) & ~User32.WS_EX_APPWINDOW);

			User32.Rect rect = default;
			var ok = User32.GetWindowRect(_process.MainWindowHandle, ref rect);
			var isOpen = false;

			var screen = GetScreenWithCursor();
			var bounds = GetBounds(screen, 1, 0);
			User32.MoveWindow(_process.MainWindowHandle, bounds.X, bounds.Y, bounds.Width, bounds.Height, true);

			// Register hotkeys
			Settings.Get(s =>
			{
				_registeredHotKeys.ForEach(hk => HotKeyManager.UnregisterHotKey(hk));
				_registeredHotKeys.Clear();

				s.Hotkeys.ForEach(hk =>
				{
					Log.Information($"Registering hot key {hk.Modifiers} + {hk.Key}");
					var reg = HotKeyManager.RegisterHotKey(hk.Key, hk.Modifiers);
					_registeredHotKeys.Add(reg);
				});
			});

			if (Settings.Instance.Topmost)
			{
				User32.SetWindowPos(_process.MainWindowHandle, User32.HWND_TOPMOST, 0, 0, 0, 0, 0);
			}

			HotKeyManager.HotKeyPressed += (s, a) =>
			{
				var stepCount = (int)Math.Max(Math.Ceiling(Settings.Instance.ToggleDurationMs / 25f), 1f);
				var stepDelayMs = Settings.Instance.ToggleDurationMs / stepCount;
				var screen = GetScreenWithCursor();

				if (isOpen)
				{
					isOpen = false;
					Log.Information("Close");

					User32.ShowWindow(_process.MainWindowHandle, NCmdShow.RESTORE);
					User32.SetForegroundWindow(_process.MainWindowHandle);

					for (int i = stepCount - 1; i >= 0; i--)
					{
						var bounds = GetBounds(screen, stepCount, i);

						User32.MoveWindow(_process.MainWindowHandle, bounds.X, bounds.Y, bounds.Width, bounds.Height, true);

						Task.Delay(TimeSpan.FromMilliseconds(stepDelayMs)).GetAwaiter().GetResult();
					}

					// Minimize, so the last window gets focus
					User32.ShowWindow(_process.MainWindowHandle, NCmdShow.MINIMIZE);

					// Hide, so the terminal windows doesn't linger on the desktop
					User32.ShowWindow(_process.MainWindowHandle, NCmdShow.HIDE);
				}
				else
				{
					isOpen = true;
					Log.Information("Open");

					User32.ShowWindow(_process.MainWindowHandle, NCmdShow.RESTORE);
					User32.SetForegroundWindow(_process.MainWindowHandle);

					for (int i = 1; i <= stepCount; i++)
					{
						var bounds = GetBounds(screen, stepCount, i);
						User32.MoveWindow(_process.MainWindowHandle, bounds.X, bounds.Y, bounds.Width, bounds.Height, true);

						Task.Delay(TimeSpan.FromMilliseconds(stepDelayMs)).GetAwaiter().GetResult();
					}

					if (Settings.Instance.VerticalScreenCoverage >= 100 && Settings.Instance.HorizontalScreenCoverage >= 100)
					{
						User32.ShowWindow(_process.MainWindowHandle, NCmdShow.MAXIMIZE);
					}
				}
			};
		}

		public Rectangle GetBounds(Screen screen, int stepCount, int step)
		{
			var bounds = screen.Bounds;

			var scrWidth = bounds.Width;
			var horWidthPct = (float)Settings.Instance.HorizontalScreenCoverage;

			var horWidth = (int)Math.Ceiling(scrWidth / 100f * horWidthPct);
			var x = (int)Math.Ceiling(scrWidth / 2f - horWidth / 2f);

			bounds.Height = (int)Math.Ceiling((bounds.Height / 100f) * Settings.Instance.VerticalScreenCoverage);

			return new Rectangle(bounds.X + x, bounds.Y + -bounds.Height + (bounds.Height / stepCount * step), horWidth, bounds.Height);
		}

		public void Dispose()
		{
			ResetTerminal(_process);
		}

		private static Screen GetScreenWithCursor()
		{
			return Screen.AllScreens.FirstOrDefault(s => s.Bounds.Contains(Cursor.Position));
		}

		private static void ResetTerminal(Process process)
		{
			var bounds = GetScreenWithCursor().Bounds;

			// Restore taskbar icon
			User32.SetWindowLong(process.MainWindowHandle, User32.GWL_EX_STYLE, (User32.GetWindowLong(process.MainWindowHandle, User32.GWL_EX_STYLE) | User32.WS_EX_TOOLWINDOW) & User32.WS_EX_APPWINDOW);

			// Reset position
			User32.MoveWindow(process.MainWindowHandle, bounds.X, bounds.Y, bounds.Width, bounds.Height, true);

			// Restore window
			User32.ShowWindow(process.MainWindowHandle, NCmdShow.MAXIMIZE);
		}
	}
}