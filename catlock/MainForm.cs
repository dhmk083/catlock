using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace catlock {
    public partial class MainForm : Form {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BlockInput(bool block);

        public MainForm() {
            InitializeComponent();

            Block();
        }

        public void Block() {
            if (!BlockInput(true)) return;

            Opacity = 0.0;
            WindowState = FormWindowState.Normal;

            var state = 0;
            var pauseCnt = 0;
            var timer = new Timer();
            timer.Tick += (s, e) => {
                if (state == 0) {
                    Opacity += 0.1;
                    if (Opacity == 1.0) {
                        state = 1;
                    }
                }
                else if (state == 1) {
                    if (pauseCnt++ == 5) {
                        state = 2;
                    }
                }
                else {
                    Opacity -= 0.1;
                    if (Opacity == 0.0) {
                        WindowState = FormWindowState.Minimized;
                        timer.Dispose();
                    }
                }
            };
            timer.Interval = 100;
            timer.Start();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
