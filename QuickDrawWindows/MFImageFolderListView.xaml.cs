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
using QuickDraw.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using System.Threading.Tasks;
using System.Collections.Specialized;
using QuickDraw.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    public static class TextBlockExtensions
    {
        public static double PreWrappedWidth(this TextBlock textBlock)
        {
            var tempTextBlock = new TextBlock { Text = textBlock.Text };

            tempTextBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            tempTextBlock.Arrange(new Rect(new Point(), textBlock.DesiredSize));

            return tempTextBlock.ActualWidth;
        }
    }

    public sealed partial class MFImageFolderListView : UserControl, INotifyPropertyChanged
    {
        public static bool InvertBool(bool value)
        {
            return !value;
        }

        public class ByWidth : IComparer<TextBlock>
        {
            public int Compare(TextBlock x, TextBlock y)
            {
                int widthCompare = x.PreWrappedWidth().CompareTo(y.PreWrappedWidth());

                if (widthCompare == 0 && !ReferenceEquals(x, y))
                {
                    return 1;
                }

                return widthCompare;
            }
        }

        public MFObservableCollection<MFImageFolder> ImageFolderCollection = null;

        private readonly SortedSet<TextBlock> _pathTexts = new(new ByWidth());
        private readonly SortedSet<TextBlock> _imageCountTexts = new(new ByWidth());

        GridLength _desiredPathColumnWidth = new(0, GridUnitType.Auto);
        public GridLength DesiredPathColumnWidth
        {
            get => _desiredPathColumnWidth;
            set
            {
                _desiredPathColumnWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DesiredPathColumnWidth)));
            }
        }

        GridLength _desiredImageCountColumnWidth = new(0, GridUnitType.Auto);

        public event PropertyChangedEventHandler PropertyChanged;

        public GridLength DesiredImageCountColumnWidth
        {
            get => _desiredImageCountColumnWidth;
            set
            {
                _desiredImageCountColumnWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DesiredImageCountColumnWidth)));
            }
        }

        public MFImageFolderListView()
        {
            this.InitializeComponent();

            var app = (App.Current as App);

            void UpdateImageFolderCollection()
            {
                ImageFolderCollection = new MFObservableCollection<MFImageFolder>(app.Settings.ImageFolderList.ImageFolders);

                ImageFolderCollection.CollectionChanged += (sender, e) => {
                    if ((e as MFNotifyCollectionChangedEventArgs)?.FromModel ?? false)
                    {
                        return;
                    }
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Move:
                            app.Settings.ImageFolderList.ImageFolders.RemoveRange(0, e.OldItems.Count);
                            app.Settings.ImageFolderList.ImageFolders.InsertRange(e.NewStartingIndex, e.NewItems.OfType<MFImageFolder>());

                            break;
                        case NotifyCollectionChangedAction.Remove:
                            app.Settings.ImageFolderList.ImageFolders.RemoveRange(e.OldStartingIndex, e.OldItems.Count);

                            break;

                        case NotifyCollectionChangedAction.Add:
                            if (e.NewStartingIndex != -1)
                            {
                                app.Settings.ImageFolderList.ImageFolders.InsertRange(e.NewStartingIndex, e.NewItems.OfType<MFImageFolder>());
                            } else
                            {
                                app.Settings.ImageFolderList.ImageFolders.AddRange(e.NewItems.OfType<MFImageFolder>());
                            }

                                break;
                        default:
                            break;
                    }
                    app.Settings.WriteSettings();
                };

                app.Settings.ImageFolderList.CollectionChanged += (sender, e) =>
                {
                    DispatcherQueue.EnqueueAsync(() =>
                    {
                        var i = 0;
                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                i = 0;
                                foreach (var item in e.NewItems)
                                {
                                    ImageFolderCollection.AddFromModel(item as MFImageFolder);
                                    i++;
                                }
                                break;
                            case NotifyCollectionChangedAction.Replace:
                                i = 0;
                                foreach (var item in e.OldItems)
                                {
                                    ImageFolderCollection.SetFromModel(e.NewStartingIndex + i, item as MFImageFolder);
                                    i++;
                                }
                                break;
                            default:
                                break;
                        }
                    });

                    app.Settings.WriteSettings();
                };
            }

            UpdateImageFolderCollection();

            app.Settings.PropertyChanged += (sender, e) => {
                UpdateImageFolderCollection();
            };

        }

        private void UpdateColumnWidths(Grid grid)
        {
            if (_pathTexts.Count < 1 || _imageCountTexts.Count < 1) { return; }

            ProgressRing progressRing = _imageCountTexts.Max.Parent.FindDescendant<ProgressRing>();
            var ImageCountColumnWidth = Math.Max(_imageCountTexts.Max.ActualWidth, progressRing.ActualWidth) + 20;

            var gridWidth = grid.ActualWidth;
            var availableWidth = gridWidth - (grid.ColumnDefinitions[3].ActualWidth + ImageCountColumnWidth + 20);

            var maxPathColumnWidth = _pathTexts.Max != null ? _pathTexts.Max.PreWrappedWidth() + 1 : 0;
            var PathColumnWidth = Math.Max(100, Math.Min(availableWidth, maxPathColumnWidth));

            DesiredPathColumnWidth = new GridLength(PathColumnWidth);
            DesiredImageCountColumnWidth = new GridLength(ImageCountColumnWidth);
        }

        private void MFImageFolderControl_ColumnWidthChanged(object sender, MFImageFolderView.ColumnWidthChangedEventArgs args)
        {
            Grid grid = args.columnType == ColumnType.Grid ? sender as Grid : (sender as FrameworkElement).Parent as Grid;

            switch (args.columnType)
            {
                case ColumnType.Path:
                    TextBlock pathText = sender as TextBlock;
                    _pathTexts.Add(pathText);
                    break;

                case ColumnType.ImageCount:
                    TextBlock imageCountText = sender as TextBlock;
                    _imageCountTexts.Add(imageCountText);
                    break;

                default:
                    break;
            }

            UpdateColumnWidths(grid);
        }

        private void OpenFolders()
        {
            IFileOpenDialog dialog = null;
            uint count = 0;
            try
            {
                dialog = new NativeFileOpenDialog();
                dialog.SetOptions(
                    FileOpenDialogOptions.NoChangeDir
                    | FileOpenDialogOptions.PickFolders
                    | FileOpenDialogOptions.AllowMultiSelect
                    | FileOpenDialogOptions.PathMustExist
                );
                _ = dialog.Show(IntPtr.Zero);

                dialog.GetResults(out IShellItemArray shellItemArray);

                if (shellItemArray != null)
                {
                    string folderpath = null;
                    shellItemArray.GetCount(out count);

                    List<string> paths = new List<string>();

                    for (uint i = 0; i < count; i++)
                    {
                        shellItemArray.GetItemAt(i, out IShellItem shellItem);

                        if (shellItem != null)
                        {
                            shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr i_result);
                            folderpath = Marshal.PtrToStringAuto(i_result);
                            Marshal.FreeCoTaskMem(i_result);

                            paths.Add(folderpath);
                        }
                    }

                    var Settings = (App.Current as App).Settings;
                    Settings.ImageFolderList.AddFolderPaths(paths);
                }
            }
            catch (COMException)
            {
                // No files or other weird error, do nothing.
            }
            finally
            {
                if (dialog != null)
                {
                    _ = Marshal.FinalReleaseComObject(dialog);
                }
            }
        }

        private void AddFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolders();
        }
    }
}
