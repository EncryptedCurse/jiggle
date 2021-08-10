using System;
using System.Threading;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Jiggle.Properties;
using Timer = System.Timers.Timer;

namespace Jiggle {
    static class Program {
        [STAThread]
        static void Main() {
            // Application.EnableVisualStyles();
            // Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppContext());
        }
    }

    class AppContext : ApplicationContext {
        private Timer timer;
        private NotifyIcon trayIcon;
        private ContextMenu contextMenu;
        private MenuItem menuToggle, menuExit;
        private MenuItem menuMode, menuMode_mouseJump, menuMode_mouseSlide, menuMode_startSearch;
        private MenuItem menuInterval, menuInterval_60s, menuInterval_150s, menuInterval_300s;
        private MenuItem[] menuModes, menuIntervals;
        private Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
        private Random random = new Random();

        public AppContext() {
            timer = new Timer();
            timer.Elapsed += SimulateActivity;

            menuToggle = new MenuItem("Enabled", Toggle);

            menuMode = new MenuItem("Mode");
            menuMode_mouseJump = new MenuItem("Mouse jump", Mode);
            menuMode_mouseSlide = new MenuItem("Mouse slide", Mode);
            menuMode_startSearch = new MenuItem("Start search", Mode);
            menuMode_mouseJump.RadioCheck = true;
            menuMode_mouseSlide.RadioCheck = true;
            menuMode_startSearch.RadioCheck = true;
            menuModes = new MenuItem[] { menuMode_mouseJump, menuMode_mouseSlide, menuMode_startSearch };
            menuMode.MenuItems.AddRange(menuModes);

            menuInterval = new MenuItem("Interval");
            menuInterval_60s = new MenuItem("60 seconds", Interval);
            menuInterval_150s = new MenuItem("2.5 minutes", Interval);
            menuInterval_300s = new MenuItem("5 minutes", Interval);
            menuInterval_60s.RadioCheck = true;
            menuInterval_150s.RadioCheck = true;
            menuInterval_300s.RadioCheck = true;
            menuIntervals = new MenuItem[] { menuInterval_60s, menuInterval_150s, menuInterval_300s };
            menuInterval.MenuItems.AddRange(menuIntervals);

            menuExit = new MenuItem("Exit", Exit);

            contextMenu = new ContextMenu(new MenuItem[] { menuToggle, menuMode, menuInterval, new MenuItem("-"), menuExit });

            menuMode_startSearch.PerformClick();
            menuInterval_150s.PerformClick();
            menuToggle.PerformClick();

            trayIcon = new NotifyIcon() {
                Icon = Resources.AppIcon,
                ContextMenu = contextMenu,
                Visible = true
            };
        }

        void Toggle(object sender, EventArgs e) {
            menuToggle.Checked = !menuToggle.Checked;
            if (menuToggle.Checked) {
                timer.Start();
                Sleep.Prevent();
            } else {
                timer.Stop();
                Sleep.Allow();
            }
        }

        void Mode(object sender, EventArgs e) {
            var menuItem = sender as MenuItem;
            foreach (MenuItem mode in menuModes) {
                mode.Checked = mode == menuItem;
            }
            timer.Stop();
            timer.Start();
        }

        void Interval(object sender, EventArgs e) {
            var menuItem = sender as MenuItem;
            foreach (MenuItem interval in menuIntervals) {
                interval.Checked = interval == menuItem;
            }
            timer.Stop();
            if (menuItem == menuInterval_60s) {
                timer.Interval = 60 * 1000;
            } else if (menuItem == menuInterval_150s) {
                timer.Interval = 150 * 1000;
            } else if (menuItem == menuInterval_300s) {
                timer.Interval = 300 * 1000;
            }
            timer.Start();
        }

        void Exit(object sender, EventArgs e) {
            timer.Stop();
            Sleep.Allow();
            trayIcon.Visible = false;
            Application.Exit();
        }

        void SimulateActivity(object source, ElapsedEventArgs e) {
            if (menuMode_mouseJump.Checked) {
                int randomX = random.Next(screenBounds.Width);
                int randomY = random.Next(screenBounds.Height);
                Cursor.Position = new Point(randomX, randomY);
            } else if (menuMode_mouseSlide.Checked) {
                int randomX = random.Next(screenBounds.Width);
                int randomY = random.Next(screenBounds.Height);
                Point currPoint = Cursor.Position;
                PointF tempPoint = currPoint;
                Point nextPoint = new Point(randomX, randomY);

                int steps = 100;
                PointF slope = new PointF(nextPoint.X - currPoint.X, nextPoint.Y - currPoint.Y);
                slope.X /= steps;
                slope.Y /= steps;

                for (int i = 0; i < steps; i++) {
                    tempPoint = new PointF(tempPoint.X + slope.X, tempPoint.Y + slope.Y);
                    Cursor.Position = Point.Round(tempPoint);
                    Thread.Sleep(10);
                }
                Cursor.Position = nextPoint;
            } else if (menuMode_startSearch.Checked) {
                SendKeys.SendWait("^{ESC}");
                Thread.Sleep(1000);
                foreach (string key in new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" }) {
                    SendKeys.SendWait(key);
                    Thread.Sleep(200);
                }
                SendKeys.SendWait("{ESC}");
            }
        }
    }

    class Sleep {
        [Flags]
        enum EXECUTION_STATE : uint {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public static void Prevent() {
            SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
        }

        public static void Allow() {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }
    }
}
