using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WifiShareTool.Command;
using WifiShareTool.Utils;

namespace WifiShareTool.ViewModel
{
    public class MainVm:VmBase
    {
        private SharWiFiUtils shareWiFi;
        private bool canStartWifi = true;
        private bool wifiIsStarted;

        public event Action<string> BalloonTipNotify;

        private static MainVm instance;
        public static MainVm Insatnce
        {
            get
            {
                if (instance == null)
                {
                    instance = new MainVm();
                }
                return instance;
            }
        }

        private MainVm()
        {
            shareWiFi = new SharWiFiUtils();
            BtnName = "开启WiFi";
            WiFiNameEnabled = true;
            WiFiPwdEnabled = true;
            WiFiName = "WiFi共享精灵";
            WiFiPwd = "1234567890";
            StartWifiCmd = new DelegateCommand(DoStartOrCloseWifi,()=> canStartWifi);
        }

        private string _WiFiName;

        public string WiFiName
        {
            get { return _WiFiName; }
            set { _WiFiName = value;RaisePropertyChanged(() => WiFiName); }
        }

        private string _WiFiPwd;

        public string WiFiPwd
        {
            get { return _WiFiPwd; }
            set { _WiFiPwd = value; RaisePropertyChanged(() => WiFiPwd); }
        }

        private string _BtnName;

        public string BtnName
        {
            get { return _BtnName; }
            set { _BtnName = value;RaisePropertyChanged(() => BtnName); }
        }

        private bool _WiFiNameEnabled;

        public bool WiFiNameEnabled
        {
            get { return _WiFiNameEnabled; }
            set { _WiFiNameEnabled = value;RaisePropertyChanged(() => WiFiNameEnabled); }
        }

        private bool _WiFiPwdEnabled;

        public bool WiFiPwdEnabled
        {
            get { return _WiFiPwdEnabled; }
            set { _WiFiPwdEnabled = value;RaisePropertyChanged(() => WiFiPwdEnabled); }
        }


        public ICommand StartWifiCmd { get; set; }

        private void DoStartOrCloseWifi()
        {
            var wifiName = WiFiName;
            var wifiPwd = WiFiPwd;
            if (canStartWifi)
            {
                canStartWifi = false;

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (wifiIsStarted)
                        {
                            wifiIsStarted = !shareWiFi.StopWiFi(out string stopWiFiOut);
                            wifiIsStarted = !shareWiFi.DisallowWifi(out string disallowWifiOut);
                            wifiIsStarted = !shareWiFi.JShareWIFI(false, out string stopJShareWIFIOut);
                            return;
                        }

                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (string.IsNullOrWhiteSpace(wifiName))
                            {
                                MessageBox.Show("WiFi名称不能为空！", "提示");
                                return;
                            }
                        }));

                        if (string.IsNullOrWhiteSpace(wifiPwd) || wifiPwd.Length < 8)
                        {
                            MessageBox.Show( "WiFi密码不能小于8位！", "提示");
                            return;
                        }

                        wifiIsStarted = shareWiFi.AllowWiFi(wifiName, wifiPwd, out string allowOut);
                        if (!wifiIsStarted)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show( allowOut, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                        wifiIsStarted = shareWiFi.StartWiFi(out string startWiFiOut);
                        if (!wifiIsStarted)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show( startWiFiOut, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                        wifiIsStarted = shareWiFi. JShareWIFI(true, out string startJShareWIFIOut);
                        if (!wifiIsStarted)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show( startJShareWIFIOut, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            return;
                        }
                    }
                    catch (Exception e)
                    { Console.WriteLine(e.Message); }
                    finally
                    {
                        canStartWifi = true;
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (wifiIsStarted)
                            {
                                BtnName = "关闭WiFi";
                                WiFiNameEnabled = false;
                                WiFiPwdEnabled = false;

                                BalloonTipNotify?.Invoke("WiFi共享已开启！");
                            }
                            else
                            {
                                BtnName = "开启WiFi";
                                wifiIsStarted = false;
                                WiFiNameEnabled = true;
                                WiFiPwdEnabled = true;
                                BalloonTipNotify?.Invoke("WiFi共享已关闭！");
                            }
                        }));
                    }
                });
            }
        }

        public void Close()
        {
            wifiIsStarted = !shareWiFi.StopWiFi(out string stopWiFiOut);
            wifiIsStarted = !shareWiFi.DisallowWifi(out string disallowWifiOut);
            wifiIsStarted = !shareWiFi.JShareWIFI(false, out string stopJShareWIFIOut);
        }
    }
}
