using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThumbSense4SurfacePro
{
    public partial class MainForm : Form
    {
        private TouchHandleMessageWindow _touchHandleWindow;

        public MainForm()
        {
            InitializeComponent();

            _notifyIcon.Icon = this.Icon;

            _touchHandleWindow = new TouchHandleMessageWindow();
        }

        protected override void OnClosed(EventArgs e)
        {
            _touchHandleWindow.Close();
            _notifyIcon.Visible = false;

            base.OnClosed(e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
