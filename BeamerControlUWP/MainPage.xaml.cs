using rv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeamerControlUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string _ip = "";
        private string _pw = "";
        private int _port = 0;
        private int _startupRoundCntr = 0;
        private DispatcherTimer _timerMuteBlink = new DispatcherTimer(); //0.5s
        private DispatcherTimer _timerPowerCheck = new DispatcherTimer();//60s
        private DispatcherTimer _timerPowerBlink = new DispatcherTimer();//0.5s
        private Brush btndefBack;
        DispatcherTimer tmrTimer = new DispatcherTimer();

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = this;
            tmrTimer.Tick += tmrTimer_Tick;
            tmrTimer.Interval = new TimeSpan(0, 0, 1);
            tmrTimer.Start();

            
        }

        private bool LoadConfig()
        {
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings =
                    Windows.Storage.ApplicationData.Current.LocalSettings;
                _ip = (string)(localSettings.Values["ip"]);
                _pw = (string)(localSettings.Values["pw"]);
                _port = int.Parse((string)(localSettings.Values["port"]));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        private async Task<bool> WaitForSettings()
        {
            SettingsDialog dialog = new SettingsDialog();
            await dialog.ShowAsync();
            return true;
        }

        private async void btnPwr_Click(object sender, RoutedEventArgs e)
        {

            await Task.Run(async () =>
            {
                try
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        this.btnPwr.Background = new SolidColorBrush(Colors.LawnGreen);
                        this.btnMute.IsEnabled = false;
                        _timerPowerBlink.Start();
                    });

                    rv.PJLinkConnection c = connectBeamer();
                    PowerCommand.PowerStatus powStat = PowerCommand.PowerStatus.UNKNOWN;
                    try
                    {
                        powStat = await c.powerQuery();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            powStat = await c.powerQuery();
                        }
                        catch (Exception)
                        {

                        }
                    }
                    switch (powStat)
                    {
                        case PowerCommand.PowerStatus.OFF:
                        case PowerCommand.PowerStatus.COOLING:
                        case PowerCommand.PowerStatus.UNKNOWN:
                            await c.turnOn();
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                _timerPowerCheck.Interval = new TimeSpan(0, 0, 5); //10 sec;
                            });

                            break;
                        case PowerCommand.PowerStatus.ON:
                        case PowerCommand.PowerStatus.WARMUP:
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                this.btnPwr.Background = btndefBack;
                                this.btnMute.IsEnabled = false;
                                _timerMuteBlink.Stop();
                                _timerPowerBlink.Stop();
                                btnMute.Background = btndefBack;
                            });
                            await c.turnOff();
                            break;
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private async void btnMute_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                try
                {
                    rv.PJLinkConnection c = connectBeamer();
                    AVMuteCommand qcmd = new AVMuteCommand(AVMuteCommand.Action.QUERYSTATE);
                    if (await c.sendCommand(qcmd) == Command.Response.SUCCESS)
                    {
                        AVMuteCommand acmd;
                        if (qcmd.Status == AVMuteCommand.Action.MUTE)
                        {
                            acmd = new AVMuteCommand(AVMuteCommand.Action.UNMUTE);

                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                _timerMuteBlink.Stop();
                                btnMute.Background = btndefBack;
                            });
                        }
                        else
                        {
                            acmd = new AVMuteCommand(AVMuteCommand.Action.MUTE);
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                _timerMuteBlink.Start();
                            });
                        }
                        await c.sendCommand(acmd);
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private void tmrTimer_Tick(object sender, object e)
        {
            txtTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private async void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();

            view.TryResizeView(new Size(328, 200));

            bool showSettings = false;
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
            if ((bool)(localSettings.Values["reqPw"]??false))
            {
                PasswordDialog pwdiag = new PasswordDialog();
                await pwdiag.ShowAsync();

                if (pwdiag.Result)
                {
                    showSettings = true;
                }
            }
            else
            {
                showSettings = true;
            }
            if (showSettings)
            {
                view.TryResizeView(new Size(328, 500));
                SettingsDialog dialog = new SettingsDialog();
                await dialog.ShowAsync();
                if (!LoadConfig())
                {
                    tmrTimer.Stop();
                    txtTime.Text = "Invalid Config";
                    btnMute.Visibility = Visibility.Collapsed;
                    btnPwr.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Type type = this.Frame.CurrentSourcePageType;
                    object param = null;
                    if (this.Frame.BackStack.Any())
                    {
                        type = this.Frame.BackStack.Last().SourcePageType;
                        param = this.Frame.BackStack.Last().Parameter;
                    }
                    try { this.Frame.Navigate(type, param); }
                    finally { this.Frame.BackStack.Remove(this.Frame.BackStack.Last()); }
                }

            }
            view.TryResizeView(new Size(328, 100));
        }

        private PJLinkConnection connectBeamer()
        {
            //return new PJLinkConnection("192.168.2.110", 4352, "panasonic");
            return new PJLinkConnection(_ip, _port, _pw);

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!LoadConfig())
            {
                tmrTimer.Stop();
                txtTime.Text = "Invalid Config";
                btnMute.Visibility = Visibility.Collapsed;
                btnPwr.Visibility = Visibility.Collapsed;
            }
            btndefBack = btnPwr.Background;

            // Hook up the Elapsed event for the timer.
            _timerMuteBlink.Tick += _timerMuteBlink_Elapsed;

            // Set the Interval to 0.5 seconds (500 milliseconds).
            _timerMuteBlink.Interval = new TimeSpan(0,0,0,0,500);

            _timerPowerCheck.Tick += _timerPowerCheck_Elapsed;
            _timerPowerCheck.Interval = new TimeSpan(0,1,0); // 1min
            _timerPowerCheck.Start();

            _timerPowerBlink.Tick += _timerPowerBlink_Elapsed;
            _timerPowerBlink.Interval = new TimeSpan(0, 0, 0, 0, 500); // 0.5sec

            rv.PJLinkConnection c = connectBeamer();
            PowerCommand.PowerStatus powStat = PowerCommand.PowerStatus.UNKNOWN;
            try
            {
                powStat = await c.powerQuery();
            }
            catch (Exception)
            {
                try
                {
                    powStat = await c.powerQuery();
                }
                catch (Exception)
                {

                }
            }
            switch (powStat)
            {
                case PowerCommand.PowerStatus.OFF:
                case PowerCommand.PowerStatus.COOLING:
                case PowerCommand.PowerStatus.UNKNOWN:
                    this.btnPwr.Background = btndefBack;
                    this.btnMute.IsEnabled = false;
                    break;
                case PowerCommand.PowerStatus.ON:
                case PowerCommand.PowerStatus.WARMUP:
                    this.btnPwr.Background = new SolidColorBrush(Colors.LawnGreen);
                    this.btnMute.IsEnabled = true;
                    try
                    {
                        AVMuteCommand qcmd = new AVMuteCommand(AVMuteCommand.Action.QUERYSTATE);
                        if (await c.sendCommand(qcmd) == Command.Response.SUCCESS)
                        {
                            if (qcmd.Status == AVMuteCommand.Action.MUTE)
                            {
                                _timerMuteBlink.Start();
                            }
                            else
                            {
                                _timerMuteBlink.Stop();
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    break;
            }
        }

        private void _timerPowerBlink_Elapsed(object sender, object e)
        {
                // Change button color
                if ( ((SolidColorBrush) btnPwr.Background).Color == Colors.LawnGreen)
                {
                    btnPwr.Background = btndefBack;
                }
                else
                {
                    btnPwr.Background = new SolidColorBrush( Colors.LawnGreen);
                }
        }

        async void _timerPowerCheck_Elapsed(object source, object e)
        {
            await Task.Run(async () =>
            {
                try
                {
                    rv.PJLinkConnection c = connectBeamer();
                    PowerCommand.PowerStatus powStat = PowerCommand.PowerStatus.UNKNOWN;
                    try
                    {
                        powStat = await c.powerQuery();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            powStat = await c.powerQuery();
                        }
                        catch (Exception)
                        {

                        }
                    }
                    switch (powStat)
                    {
                        case PowerCommand.PowerStatus.OFF:
                        case PowerCommand.PowerStatus.COOLING:
                        case PowerCommand.PowerStatus.UNKNOWN:
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                this.btnPwr.Background = btndefBack;
                                this.btnMute.IsEnabled = false;
                                _startupRoundCntr++;
                                if (_timerPowerCheck.Interval == new TimeSpan(0, 0, 5) && powStat != PowerCommand.PowerStatus.UNKNOWN)
                                {
                                    _timerPowerCheck.Interval = new TimeSpan(0, 1, 0); // 1min
                                _startupRoundCntr = 0;
                                    _timerPowerBlink.Stop();
                                    this.btnPwr.Background = btndefBack;
                                }
                            });
                            break;
                        case PowerCommand.PowerStatus.ON:
                        case PowerCommand.PowerStatus.WARMUP:
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                this._timerPowerBlink.Stop();
                                this.btnPwr.Background = new SolidColorBrush(Colors.LawnGreen);
                                this.btnMute.IsEnabled = true;
                                AVMuteCommand qcmd = new AVMuteCommand(AVMuteCommand.Action.QUERYSTATE);
                                if (await c.sendCommand(qcmd) == Command.Response.SUCCESS)
                                {
                                    if (qcmd.Status == AVMuteCommand.Action.MUTE)
                                    {
                                        _timerMuteBlink.Start();
                                    }
                                    else
                                    {
                                        _timerMuteBlink.Stop();
                                        btnMute.Background = btndefBack;
                                    }
                                }
                                _startupRoundCntr++;
                                if (_timerPowerCheck.Interval == new TimeSpan(0, 0, 5) && _startupRoundCntr == 10)
                                {
                                    _timerPowerCheck.Interval = new TimeSpan(0, 1, 0); // 1min
                                _startupRoundCntr = 0;
                                    _timerPowerBlink.Stop();
                                    this.btnPwr.Background = new SolidColorBrush(Colors.LawnGreen);
                                }
                            });
                            break;
                    }
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (_timerPowerCheck.Interval == new TimeSpan(0, 0, 5) && _startupRoundCntr == 10)
                        {
                            _timerPowerCheck.Interval = new TimeSpan(0, 1, 0); // 1min
                        _startupRoundCntr = 0;
                            _timerPowerBlink.Stop();
                            this.btnPwr.Background = btndefBack;
                        }
                    });
                }
                catch (Exception)
                {
                }
            });
        }

        void _timerMuteBlink_Elapsed(object source, object e)
        {
            
                // Change button color
                if (((SolidColorBrush)btnMute.Background).Color == Colors.Red)
                {
                    btnMute.Background = btndefBack;
                }
                else
                {
                    btnMute.Background = new SolidColorBrush(Colors.Red);
                }
        }

    }
}
