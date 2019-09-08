using NETCONLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WifiShareTool.Command;
using WifiShareTool.Utils;
using WifiShareTool.ViewModel;

namespace WifiShareTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point startPos;
        private bool isDown;
        private bool isForceClose;
        private System.Windows.Forms.NotifyIcon notifyIcon;

        /// <summary>
        /// 无线WiFi接口名称
        /// </summary>
        private string wifiInterface { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = WifiShareTool.Properties.Resources.wifi_signal;

            notifyIcon.Text = "WiFi共享精灵";
            notifyIcon.Visible = true;
            notifyIcon.Click += NotifyIcon_Click;

            System.Windows.Forms.MenuItem menuOpenMain = new System.Windows.Forms.MenuItem("打开主窗口");
            System.Windows.Forms.MenuItem menuExit = new System.Windows.Forms.MenuItem("退出");
            menuOpenMain.Click += MenuOpenMain_Click;
            menuExit.Click += MenuExit_Click;

            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { menuOpenMain, menuExit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

            MainVm.Insatnce.BalloonTipNotify += Insatnce_BalloonTipNotify;

            this.Closing += MainWindow_Closing;
        }

        private void Insatnce_BalloonTipNotify(string notify)
        {
            notifyIcon.ShowBalloonTip(200,"提示",notify, System.Windows.Forms.ToolTipIcon.Info);
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            this.Activate();
            this.Show();
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            isForceClose = true;
            this.Close();
        }

        private void MenuOpenMain_Click(object sender, EventArgs e)
        {
            this.Activate();
            this.Show();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isForceClose)
            {
                MainVm.Insatnce.Close();
            }
            else
            {
                Hide();
                e.Cancel = true;
            }
        }


        private void ImgMin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ImgClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPos = e.GetPosition(this);
            isDown = true;
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDown)
            {
                var curPos = e.GetPosition(this);
                var offset = (curPos - startPos);
                this.Left += offset.X;
                this.Top += offset.Y;
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDown = false;
        }
    }
}
