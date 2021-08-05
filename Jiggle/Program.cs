using System;
using System.Drawing;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Windows.Forms;
using Jiggle.Properties;

namespace Jiggle {
    static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppContext());
        }
    }

    public class AppContext : ApplicationContext {
        private Timer timer;
        private NotifyIcon trayIcon;
        private ContextMenu contextMenu;
        private MenuItem menuToggle, menuExit;
        private Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
        private Random random = new Random();

        public AppContext() {
            timer = new Timer(2.5 * 60 * 1000);
            timer.Elapsed += MoveCursor;

            menuToggle = new MenuItem("Enabled", Toggle);
            menuExit = new MenuItem("Exit", Exit);
            contextMenu = new ContextMenu(new MenuItem[] { menuToggle, menuExit });
            trayIcon = new NotifyIcon() {
                Icon = Resources.AppIcon,
                ContextMenu = contextMenu,
                Visible = true
            };
        }

        void Exit(object sender, EventArgs e) {
            trayIcon.Visible = false;
            Application.Exit();
        }

        void Toggle(object sender, EventArgs e) {
            menuToggle.Checked = !menuToggle.Checked;
            if (menuToggle.Checked) {
                timer.Start();
            } else {
                timer.Stop();
            }
        }

        void MoveCursor(object source, ElapsedEventArgs e) {
            int randomX = random.Next(screenBounds.Width);
            int randomY = random.Next(screenBounds.Height);
            Cursor.Position = new Point(randomX, randomY);
        }
    }
}
