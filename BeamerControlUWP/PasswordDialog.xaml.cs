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
    public sealed partial class PasswordDialog : ContentDialog
    {
        public bool Result { get; private set; }
        private int wrongPwCnt = 0;
        private const int maxWrongPw = 3;

        public PasswordDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
            if ((bool)localSettings.Values["reqPw"] && pwPW.Password != (string) localSettings.Values["setPw"])
            {
                wrongPwCnt++;
                if(wrongPwCnt >= maxWrongPw)
                {
                    this.Result = false;
                    return;
                }
                args.Cancel = true;
                errorTextBlock.Text = "Wrong Password entered.";
                pwPW.Password = "";
            }
            else
            {
                this.Result = true;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Result = false;
        }
    }
}
