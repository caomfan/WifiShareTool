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

namespace WifiShareTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point startPos;
        private bool isDown;
        private bool canStartWifi = true;
        private bool wifiIsStarted;
        private INetSharingConfiguration sharingCfg;
        private INetSharingConfiguration priSharingCfg;
        /// <summary>
        /// 无线WiFi接口名称
        /// </summary>
        private string wifiInterface { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            StartWifiCmd = new DelegateCommand(DoStartOrCloseWifi);
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            wifiIsStarted = !StopWiFi(out string stopWiFiOut);
            wifiIsStarted = !DisallowWifi(out string disallowWifiOut);
            wifiIsStarted = !JShareWIFI(false, out string stopJShareWIFIOut);
        }

        private void DoStartOrCloseWifi()
        {
            var wifiName = txtWifiName.Text;
            var wifiPwd = txtWifiPwd.Text;
            if (canStartWifi)
            {
                canStartWifi = false;
                btnStartWifi.IsEnabled = false;

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (wifiIsStarted)
                        {
                            wifiIsStarted = !StopWiFi(out string stopWiFiOut);
                            wifiIsStarted = !DisallowWifi(out string disallowWifiOut);
                            wifiIsStarted = !JShareWIFI(false, out string stopJShareWIFIOut);
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(wifiName))
                        {
                            MessageBox.Show(this, "WiFi名称不能为空！", "提示");
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(wifiPwd) || wifiPwd.Length < 8)
                        {
                            MessageBox.Show(this, "WiFi密码不能小于8位！", "提示");
                            return;
                        }

                        wifiIsStarted = IsSupportHostedNetwork();
                        if (!wifiIsStarted)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show(this, "当前网卡不支持承载网络！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }

                        wifiIsStarted = AllowWiFi(wifiName, wifiPwd, out string allowOut);
                        if (!wifiIsStarted)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show(this, allowOut, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                        wifiIsStarted = StartWiFi(out string startWiFiOut);
                        if (!wifiIsStarted)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show(this, startWiFiOut, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                        wifiIsStarted = JShareWIFI(true, out string startJShareWIFIOut);
                        if (!wifiIsStarted)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show(this, startJShareWIFIOut, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                    }
                    catch (Exception e)
                    { Console.WriteLine(e.Message); }
                    finally
                    {
                        canStartWifi = true;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            btnStartWifi.IsEnabled = true;
                            if (wifiIsStarted)
                            {
                                btnStartWifi.Content = "关闭WiFi";
                                txtWifiName.IsEnabled = false;
                                txtWifiPwd.IsEnabled = false;
                            }
                            else
                            {
                                btnStartWifi.Content = "开启WiFi";
                                wifiIsStarted = false;
                                txtWifiName.IsEnabled = true;
                                txtWifiPwd.IsEnabled = true;
                            }
                        }));
                    }
                });
            }
        }

        public ICommand StartWifiCmd { get; set; }

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

        private string executeCmd(string command)
        {
            Process process = new Process
            {
                StartInfo = { FileName = " cmd.exe ", UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true }
            };
            process.Start();
            process.StandardInput.WriteLine(command);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();
            string str = process.StandardOutput.ReadToEnd();
            process.Close();
            return str;
        }

        /// <summary>
        /// 共享网络
        /// </summary>
        /// <param name="wifiName">WiFi名称</param>
        /// <param name="wifiPassword">WiFi密码(不少于8位)</param>
        /// <returns>"新建共享网络成功！"||"搭建失败，请重试！"</returns>
        public bool AllowWiFi(string wifiName, string wifiPassword, out string createWifiRet)
        {
            createWifiRet = "搭建失败，请重试！";
            try
            {
                string command = "netsh wlan set hostednetwork mode=allow ssid=" + wifiName.Trim() + " key=" + wifiPassword.Trim();
                string cmdExecRet = executeCmd(command);
                createWifiRet = cmdExecRet;
                if (((createWifiRet.IndexOf("承载网络模式已设置为允许") > -1) && (createWifiRet.IndexOf("已成功更改承载网络的 SSID。") > -1)) && (createWifiRet.IndexOf("已成功更改托管网络的用户密钥密码。") > -1))
                {
                    createWifiRet = "新建共享网络成功！";
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                createWifiRet = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 禁止共享网络
        /// </summary>
        /// <returns>disallowWifiRet = "禁止共享网络成功！"||"操作失败，请重试！"</returns>
        public bool DisallowWifi(out string disallowWifiRet)
        {
            disallowWifiRet = "操作失败，请重试！";
            try
            {
                string command = "netsh wlan set hostednetwork mode=disallow";
                if (executeCmd(command).IndexOf("承载网络模式已设置为禁止") > -1)
                {
                    disallowWifiRet = "禁止共享网络成功！";
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                disallowWifiRet = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 启动承载网络(WiFi)
        /// </summary>
        /// <returns>"已启动承载网络！"||"启动承载网络失败，请尝试新建网络共享！"</returns>
        public bool StartWiFi(out string startWifiRet)
        {
            startWifiRet = "启动承载网络失败，请尝试新建网络共享！";
            try
            {
                if (executeCmd("netsh wlan start hostednetwork").IndexOf("已启动承载网络") > -1)
                {
                    startWifiRet = "已启动承载网络！";
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                startWifiRet = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 停止承载网络(WiFi)
        /// </summary>
        /// <returns>"已停止承载网络！"||"停止承载网络失败！"</returns>
        public bool StopWiFi(out string stopWifiRet)
        {
            stopWifiRet = "停止承载网络失败！";
            try
            {
                if (executeCmd("netsh wlan stop hostednetwork").IndexOf("已停止承载网络") > -1)
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                stopWifiRet = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 创建ICS共享
        /// </summary>
        /// <param name="isShare"></param>
        /// <param name="jShareWIFIRet"></param>
        /// <returns></returns>
        public bool JShareWIFI(bool isShare, out string jShareWIFIRet)
        {
            jShareWIFIRet = "未找到本地网络连接！";
            if (!isShare)
            {
                if (sharingCfg != null)
                {
                    sharingCfg = null;
                }
                return true;
            }
            try
            {
                string connectionToShare = getBestInterface()?.Name; // 被共享的网络连接
                string sharedForConnection = ""; // 共享的家庭网络连接

                var manager = new NetSharingManager();
                var connections = manager.EnumEveryConnection;

                NetworkInterface[] Ninterface = NetworkInterface.GetAllNetworkInterfaces();//确定虚拟网络名称
                foreach (NetworkInterface IN in Ninterface)
                {
                    if (IN.Description == "Microsoft Hosted Network Virtual Adapter")
                    {
                        sharedForConnection = IN.Name;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionToShare) || string.IsNullOrWhiteSpace(sharedForConnection))
                    return false;

                foreach (INetConnection c in connections)
                {
                    var props = manager.NetConnectionProps[c];
                    INetSharingConfiguration tempSharingCfg = manager.INetSharingConfigurationForINetConnection[c];
                    if (props.Name == connectionToShare)
                    {
                        tempSharingCfg.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                        if (tempSharingCfg.SharingEnabled == true)
                        {
                            jShareWIFIRet = "已设置" + props.Name + "用于共享";
                            sharingCfg = tempSharingCfg;
                        }
                    }
                    else if (props.Name == sharedForConnection)
                    {
                        tempSharingCfg.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
                        priSharingCfg = tempSharingCfg;
                    }
                }


                jShareWIFIRet = "Success";
                return true;
            }
            catch (Exception e)
            {
                jShareWIFIRet = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 获取当前上网的网卡
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private NetworkInterface getBestInterface(int port = 80)
        {
            IPAddress ip;
            try
            {
                IPHostEntry hostentry = Dns.GetHostEntry("www.baidu.com");
                ip = hostentry.AddressList[0];
            }
            catch (Exception ex)
            {
                IPHostEntry hostentry = Dns.GetHostEntry("www.360.com");
                ip = hostentry.AddressList[0];
                throw ex;
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            NetworkInterface outgoingInterface = null;
            try
            {
                socket.Connect(new IPEndPoint(ip, port));
                if (socket.Connected)
                {
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    bool isBreak = false;
                    foreach (NetworkInterface nic in interfaces)
                    {
                        if (isBreak)
                            break;
                        IPInterfaceProperties prop = nic.GetIPProperties();
                        foreach (UnicastIPAddressInformation unicastAddr in prop.UnicastAddresses)
                        {
                            if (unicastAddr.Address.Equals(((IPEndPoint)socket.LocalEndPoint).Address))
                            {
                                outgoingInterface = nic;
                                isBreak = true;
                                break;
                            }
                        }
                    }
                    return outgoingInterface;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            socket.Dispose();
            return null;
        }

        /// <summary>
        /// 检测是否支持承载网络
        /// </summary>
        /// <returns></returns>
        private bool IsSupportHostedNetwork()
        {
            try
            {
                var result = executeCmd("NETSH WLAN SHOW DRIVERS");
                if (result.IndexOf("支持的承载网络  : 是") > -1)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
