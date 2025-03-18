using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using CommunityToolkit.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    public sealed partial class MFImageFolderControl : UserControl
    {
        public ImageFolder Folder
        {
            get { return (ImageFolder)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }
        public static readonly DependencyProperty FolderProperty = DependencyProperty.Register(
            nameof(Folder),
            typeof(ImageFolder),
            typeof(MFImageFolderControl),
            new PropertyMetadata(null));

        public MFImageFolderControl()
        {
            this.InitializeComponent();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            foreach (VisualStateGroup g in VisualStateManager.GetVisualStateGroups(this))
            {
                g.CurrentStateChanging += G_CurrentStateChanging;
            }
        }

        private void G_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            Debug.WriteLine("State Change");
        }

        private void PathText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainPage page = ((Application.Current as App)?.Window as MainWindow)?.MainFrame?.Content as MainPage;

            page?.HandlePathTextSizeChange(sender as TextBlock);
        }

        private void ImageCount_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainPage page = ((Application.Current as App)?.Window as MainWindow)?.MainFrame?.Content as MainPage;

            page?.HandleImageCountSizeChange(sender as TextBlock);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainPage page = ((Application.Current as App)?.Window as MainWindow)?.MainFrame?.Content as MainPage;
            
            page?.HandleGridSizeChange(sender as Grid);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(control: this as Control, "Normal", true);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this as Control, "LoadingCount", true);
        }
    }
}
