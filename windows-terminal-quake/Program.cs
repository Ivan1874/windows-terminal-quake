using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using WindowsTerminalQuake.Native;
using WindowsTerminalQuake.UI;

namespace WindowsTerminalQuake
{
	public class Program
	{
		private static Toggler _toggler;
		private static TrayIcon _trayIcon;

		public static void Main(string[] args)
		{
			Mutex mutex = new Mutex(true, "windows-terminal-quake");

			_trayIcon = new TrayIcon((s, a) => Close());

			if (!mutex.WaitOne(TimeSpan.Zero, true))
			{
				_trayIcon.Notify(ToolTipIcon.Info, "Only one instance allowed");
				_trayIcon?.Dispose();
			}

			Logging.Configure();

			Semaphore lock_obj = new Semaphore(1, 1);
			var first_run = true;

			try
			{
				while (true)
				{
					lock_obj.WaitOne();
					Process process = Process.GetProcessesByName("WindowsTerminal").FirstOrDefault();
					if (process == null || process.MainWindowHandle == IntPtr.Zero)
					{
						process = new Process
						{
							StartInfo = new ProcessStartInfo
							{
								FileName = "wt",
								UseShellExecute = false,
								RedirectStandardOutput = true,
								WindowStyle = ProcessWindowStyle.Maximized
							}
						};
						process.Start();
						process.WaitForExit();
						process = Process.GetProcessesByName("WindowsTerminal").FirstOrDefault();
						if (process == null)
						{
							throw new ApplicationException("Wrapper was unable to start Windows Terminal");
						}
						Thread.Sleep(500);
					}

					process.EnableRaisingEvents = true;
					process.Exited += (sender, e) =>
					{
						if (Settings.Instance.Restart)
						{
							_toggler?.Dispose();
							_toggler = null;
							process = null;
							lock_obj.Release();
						}
						else
						{
							Close();
						}
					};
					_toggler = new Toggler(process);

					if (first_run)
					{
						var hks = string.Join(" or ", Settings.Instance.Hotkeys.Select(hk => $"{hk.Modifiers}+{hk.Key}"));
						_trayIcon.Notify(ToolTipIcon.Info, $"Windows Terminal Quake is running, press {hks} to toggle.");
						first_run = false;
					}
				}
			}
			catch (Exception ex)
			{
				_trayIcon.Notify(ToolTipIcon.Error, $"Cannot start: '{ex.Message}'.");

				Close();
			}
		}

		private static void Close()
		{
			_toggler?.Dispose();

			_trayIcon?.Dispose();
		}
	}
}
