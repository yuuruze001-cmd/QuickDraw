using System;
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
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
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
    }
}
