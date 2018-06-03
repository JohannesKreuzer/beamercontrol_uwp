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
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BeamerControlUWP
{
    public sealed partial class InfoDialog : ContentDialog
    {

        public InfoDialog(string info)
        {
            this.InitializeComponent();

            Paragraph paragraph = new Paragraph();
            Run run = new Run();

            // Customize some properties on the RichTextBlock.
            rtboTxtBox.IsTextSelectionEnabled = true;
            rtboTxtBox.TextWrapping = TextWrapping.WrapWholeWords;
            run.Text = info;

            // Add the Run to the Paragraph, the Paragraph to the RichTextBlock.
            paragraph.Inlines.Add(run);
            rtboTxtBox.Blocks.Add(paragraph);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
