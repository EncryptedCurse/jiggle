using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Timer = System.Timers.Timer;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using Debug = System.Diagnostics.Debug;
using Jiggle.Properties;

namespace Jiggle {
    internal static class Program {
        private static readonly Random random = new Random();

        private static readonly Timer timer = new Timer(Settings.Default.ActivityInterval * 1000);

        private static readonly MenuItem menuToggle = new MenuItem("Enabled", HandleToggle);
        private static readonly MenuItem menuMode = new MenuItem(
            "Mode",
            new MenuItem[] {
                new MenuItem("Mouse jump", HandleMode) { Name = "mouseJump", Checked = Settings.Default.Mode == "mouseJump", RadioCheck = true },
                new MenuItem("Start menu", HandleMode) { Name = "startMenu", Checked = Settings.Default.Mode == "startMenu", RadioCheck = true }
            }
        );
        private static readonly MenuItem menuAutoPause = new MenuItem("Auto pause", HandleAutoPause) { Checked = Settings.Default.AutoPause };
        private static readonly MenuItem menuActivityInterval = new MenuItem($"Activity: {Settings.Default.ActivityInterval} s") { Enabled = false };
        private static readonly MenuItem menuPauseInterval = new MenuItem($"Pause: {Settings.Default.PauseInterval} s") { Enabled = false };
        private static readonly MenuItem menuIntervals = new MenuItem("Intervals", new MenuItem[] {
            menuActivityInterval,
            menuPauseInterval,
            new MenuItem("Edit", HandleInterval)
        });
        private static readonly MenuItem menuExit = new MenuItem("Exit", HandleExit);
        private static readonly ContextMenu contextMenu = new ContextMenu(new MenuItem[] {
            menuToggle,
            new MenuItem("-"),
            menuMode,
            menuAutoPause,
            menuIntervals,
            new MenuItem("-"),
            menuExit
        });
        private static readonly NotifyIcon trayIcon = new NotifyIcon() {
            Icon = Resources.AppIcon,
            ContextMenu = contextMenu,
            Visible = true
        };

        private static IntervalWindow intervalWindow;

        [STAThread]
        private static void Main() {
            timer.Elapsed += SimulateActivity;
            Settings.Default.PropertyChanged += SettingsChanged;
            if (Settings.Default.Enabled) menuToggle.PerformClick();
            Application.Run();
        }

        private static void SettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "Enabled":
                    if (Settings.Default.Enabled) {
                        timer.Start();
                        Win32.PreventSleep();
                    } else {
                        timer.Stop();
                        Win32.AllowSleep();
                    }
                    break;
                case "Mode":
                    timer.Stop();
                    timer.Start();
                    break;
                case "ActivityInterval":
                    menuActivityInterval.Text = $"Activity: {Settings.Default.ActivityInterval} s";
                    timer.Stop();
                    timer.Interval = Settings.Default.ActivityInterval * 1000;
                    timer.Start();
                    break;
                case "PauseInterval":
                    menuPauseInterval.Text = $"Pause: {Settings.Default.PauseInterval} s";
                    timer.Stop();
                    timer.Start();
                    break;
            }
        }

        private static void HandleToggle(object sender, EventArgs e) {
            Settings.Default.Enabled = menuToggle.Checked = !menuToggle.Checked;
            Settings.Default.Save();
        }

        private static void HandleMode(object sender, EventArgs e) {
            MenuItem menuItem = sender as MenuItem;
            foreach (MenuItem mode in menuMode.MenuItems) mode.Checked = mode == menuItem;
            Settings.Default.Mode = menuItem.Name;
            Settings.Default.Save();
        }

        private static void HandleAutoPause(object sender, EventArgs e) {
            Settings.Default.AutoPause = menuAutoPause.Checked = !menuAutoPause.Checked;
            Settings.Default.Save();
        }

        private static void HandleInterval(object sender, EventArgs e) {
            if (intervalWindow == null) {
                intervalWindow = new IntervalWindow();
                intervalWindow.ShowDialog();
                intervalWindow = null;
            } else {
                intervalWindow.Focus();
            }
        }

        private static void HandleExit(object sender, EventArgs e) {
            trayIcon.Visible = false;
            Win32.AllowSleep();
            Application.Exit();
        }

        private static void SimulateActivity(object sender, ElapsedEventArgs e) {
            if (Settings.Default.AutoPause && Win32.GetIdleTime() < Settings.Default.PauseInterval * 1000) {
                Debug.WriteLine("skip activity");
                return;
            }

            Win32.PreventSleep();

            Debug.WriteLine("idle time before: " + Win32.GetIdleTime());
            switch (Settings.Default.Mode) {
                case "mouseJump":
                    Win32.SetCursorPosition(random.Next(Screen.PrimaryScreen.Bounds.Width), random.Next(Screen.PrimaryScreen.Bounds.Height));
                    break;
                case "startMenu":
                    SendKeys.SendWait("^{ESC}");
                    System.Threading.Thread.Sleep(500);
                    SendKeys.SendWait("{ESC}");
                    break;
                default:
                    Debug.WriteLine("invalid mode");
                    break;
            }
            Debug.WriteLine("idle time after: " + Win32.GetIdleTime());
        }
    }

    internal static class Win32 {
        /* flags */
        [Flags]
        private enum EXECUTION_STATE : uint {
            ES_CONTINUOUS        = 0x80000000,
            ES_SYSTEM_REQUIRED   = 0x00000001,
            ES_DISPLAY_REQUIRED  = 0x00000002,
            ES_AWAYMODE_REQUIRED = 0x00000040
        }

        [Flags]
        private enum InputType : uint {
            INPUT_MOUSE    = 0x00000000,
            INPUT_KEYBOARD = 0x00000001,
            INPUT_HARDWARE = 0x00000002
        }

        [Flags]
        private enum MOUSEEVENTF {
            MOUSEEVENTF_MOVE            = 0x0001,
            MOUSEEVENTF_LEFTDOWN        = 0x0002,
            MOUSEEVENTF_LEFTUP          = 0x0004,
            MOUSEEVENTF_RIGHTDOWN       = 0x0008,
            MOUSEEVENTF_RIGHTUP         = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN      = 0x0020,
            MOUSEEVENTF_MIDDLEUP        = 0x0040,
            MOUSEEVENTF_XDOWN           = 0x0080,
            MOUSEEVENTF_XUP             = 0x0100,
            MOUSEEVENTF_WHEEL           = 0x0800,
            MOUSEEVENTF_HWHEEL          = 0x1000,
            MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,
            MOUSEEVENTF_VIRTUALDESK     = 0x4000,
            MOUSEEVENTF_ABSOLUTE        = 0x8000
        }

        /* structs */
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO {
            public uint cbSize;
            public uint dwTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        private struct INPUT {
            public uint type;
            public InputUnion u;
        }

        /* imports */
        [DllImport("kernel32.dll")]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        private static LASTINPUTINFO lastInput;

        public static uint GetIdleTime() {
            lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint) Marshal.SizeOf(lastInput);
            if (!GetLastInputInfo(ref lastInput)) {
                throw new Exception(Marshal.GetLastWin32Error().ToString());
            } else {
                return (uint) Environment.TickCount - lastInput.dwTime;
            }
        }

        public static void PreventSleep() {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
        }

        public static void AllowSleep() {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

        public static void SetCursorPosition(int x, int y) {
            INPUT input = new INPUT() {
                type = (uint) InputType.INPUT_MOUSE,
                u = new InputUnion {
                    mi = new MOUSEINPUT {
                        dx = (x * 65536) / Screen.PrimaryScreen.Bounds.Width,
                        dy = (y * 65536) / Screen.PrimaryScreen.Bounds.Height,
                        dwFlags = (uint) (MOUSEEVENTF.MOUSEEVENTF_MOVE | MOUSEEVENTF.MOUSEEVENTF_ABSOLUTE),
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
