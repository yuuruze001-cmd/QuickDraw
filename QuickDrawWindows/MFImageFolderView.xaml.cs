using System;
using System.Diagnostics;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuickDraw.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    public enum ColumnType
    {
        Path,
        ImageCount,
        Grid
    }

    public sealed partial class MFImageFolderView : UserControl
    {
        public class ColumnWidthChangedEventArgs : EventArgs
        {
            public ColumnType columnType;

            public ColumnWidthChangedEventArgs(ColumnType columnType) => this.columnType = columnType;
        }
        public delegate void ColumnWidthChangedEventHandler(object sender, ColumnWidthChangedEventArgs args);

        public event ColumnWidthChangedEventHandler ColumnWidthChanged;

        public class ColumnDataRemoveEventArgs: EventArgs
        {
            public TextBlock PathText;
            public TextBlock ImageCountText;

            public ColumnDataRemoveEventArgs(TextBlock pathText, TextBlock imageCountText)
            {
                PathText = pathText;
                ImageCountText = imageCountText;
            }
        }

        public delegate void ColumnDataRemoveEventHandler(object sender, ColumnDataRemoveEventArgs args);

        public event ColumnDataRemoveEventHandler ColumnDataRemove;

        public GridLength DesiredPathColumnWidth
        {
            get { return (GridLength)GetValue(DesiredPathColumnWidthProperty); }
            set
            {
                SetValue(DesiredPathColumnWidthProperty, value);
            }
        }

        public static readonly DependencyProperty DesiredPathColumnWidthProperty = DependencyProperty.Register(
            nameof(DesiredPathColumnWidth),
            typeof(GridLength),
            typeof(MFImageFolderView),
            new PropertyMetadata(0.0));


        public GridLength DesiredImageCountColumnWidth
        {
            get { return (GridLength)GetValue(DesiredImageCountColumnWidthProperty); }
            set { SetValue(DesiredImageCountColumnWidthProperty, value); }
        }

        public static readonly DependencyProperty DesiredImageCountColumnWidthProperty = DependencyProperty.Register(
            nameof(DesiredImageCountColumnWidth),
            typeof(GridLength),
            typeof(MFImageFolderView),
            new PropertyMetadata(0.0));

        public MFImageFolder Folder
        {
            get { return (MFImageFolder)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }

        public static readonly DependencyProperty FolderProperty = DependencyProperty.Register(
            nameof(Folder),
            typeof(MFImageFolder),
            typeof(MFImageFolderView),
            new PropertyMetadata(null));

        public MFImageFolderView()
        {
            this.InitializeComponent();

            ColumnWidthChanged?.Invoke(PathText, new(ColumnType.Path));
            ColumnWidthChanged?.Invoke(ImageCountText, new(ColumnType.ImageCount));

            Unloaded += MFImageFolderView_Unloaded;

        }

        private void MFImageFolderView_Unloaded(object sender, RoutedEventArgs e)
        {
            ColumnDataRemove?.Invoke(this, new(PathText, ImageCountText));
        }

        private void PathText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ColumnWidthChanged?.Invoke(sender, new(ColumnType.Path));
        }

        private void ImageCount_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ColumnWidthChanged?.Invoke(sender, new(ColumnType.ImageCount));
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ColumnWidthChanged?.Invoke(sender, new(ColumnType.Grid));
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var list = (this as FrameworkElement).FindAscendant<ListView>() as ListView;
            var i = list?.IndexFromContainer(this.FindAscendant<ListViewItem>()) ?? -1;

            if (i != -1)
            {
                var settings = (App.Current as App).Settings;
                settings.ImageFolderList.RemoveFolderAt(i);
                settings.WriteSettings();
            }
        }
    }
}
