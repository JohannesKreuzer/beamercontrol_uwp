using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BeamerControlUWP
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
            txtIp.Text = (string) (localSettings.Values["ip"] ?? "");
            txtPort.Text = (string)(localSettings.Values["port"] ?? "");
            txtPw.Text = (string)(localSettings.Values["pw"] ?? "");
            swSettingsPw.IsOn = (bool)(localSettings.Values["reqPw"] ?? false);
            txtSettingsPw.Password = (string)(localSettings.Values["setPw"] ?? "");
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if(swSettingsPw.IsOn && txtSettingsPw.Password == "")
            {
                args.Cancel = true;
                errorTextBlock.Text = "If password protection is enabled, a passwort must be set for settings.";
                return;
            }
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["ip"] = txtIp.Text;
            localSettings.Values["port"] = txtPort.Text;
            localSettings.Values["pw"] = txtPw.Text;
            localSettings.Values["reqPw"] = swSettingsPw.IsOn;
            localSettings.Values["setPw"] = txtSettingsPw.Password;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
