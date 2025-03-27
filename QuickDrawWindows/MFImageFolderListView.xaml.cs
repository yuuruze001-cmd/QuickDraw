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
using System.Diagnostics;
using WinRT;
using System.Collections.Concurrent;

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

            var settings = (App.Current as App)?.Settings;

            ImageFolderCollection = new MFObservableCollection<MFImageFolder>(settings.ImageFolderList.ImageFolders);

            ImageFolderListView.Loaded += (sender, e) => {
                foreach (var i in ImageFolderCollection.Index().Where(ft => ft.Item.Selected).Select(ft => ft.Index))
                {
                    ImageFolderListView.SelectRange(new(i, 1));
                }

                if (ImageFolderListView.SelectedItems.Count == ImageFolderCollection.Count)
                {
                    SelectAllCheckbox.IsChecked = true;
                }
                else
                {
                    SelectAllCheckbox.IsChecked = false;
                }

                ImageFolderListView.SelectionChanged += ImageFolderListView_SelectionChanged;

            };

            ImageFolderCollection.CollectionChanged += (sender, e) => {
                if ((e as MFNotifyCollectionChangedEventArgs)?.FromModel ?? false)
                {
                    return;
                }
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                        settings.ImageFolderList.ImageFolders.RemoveRange(e.OldStartingIndex, e.OldItems.Count);

                        break;

                    case NotifyCollectionChangedAction.Add:
                        if (e.NewStartingIndex != -1)
                        {
                            settings.ImageFolderList.ImageFolders.InsertRange(e.NewStartingIndex, e.NewItems.OfType<MFImageFolder>());
                        }
                        else
                        {
                            settings.ImageFolderList.ImageFolders.AddRange(e.NewItems.OfType<MFImageFolder>());
                        }

                        break;
                    default:
                        break;
                }
                settings.WriteSettings();
            };

            settings.ImageFolderList.CollectionChanged += (sender, e) =>
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
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in e.OldItems)
                            {
                                ImageFolderCollection.RemoveFromModel(item as MFImageFolder);
                            }
                            break;
                        default:
                            break;
                    }
                });
            };

        }

        private void UpdateColumnWidths()
        {
            if (_pathTexts.Count < 1 || _imageCountTexts.Count < 1) { return; }
            Grid grid = (_pathTexts.Max as FrameworkElement).Parent as Grid;

            ProgressRing progressRing = _imageCountTexts.Max.Parent?.FindDescendant<ProgressRing>();
            if (progressRing != null)
            {
                var ImageCountColumnWidth = Math.Max(_imageCountTexts.Max.ActualWidth, progressRing.ActualWidth) + 20;

                var gridWidth = grid.ActualWidth;
                var availableWidth = gridWidth - (grid.ColumnDefinitions[3].ActualWidth + ImageCountColumnWidth + 20);

                var maxPathColumnWidth = _pathTexts.Max != null ? _pathTexts.Max.PreWrappedWidth() + 1 : 0;
                var PathColumnWidth = Math.Max(100, Math.Min(availableWidth, maxPathColumnWidth));

                DesiredPathColumnWidth = new GridLength(PathColumnWidth);
                DesiredImageCountColumnWidth = new GridLength(ImageCountColumnWidth);
            }
        }

        private void MFImageFolderControl_ColumnWidthChanged(object sender, MFImageFolderView.ColumnWidthChangedEventArgs args)
        {
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

            UpdateColumnWidths();
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

                    var settings = (App.Current as App).Settings;
                    settings.ImageFolderList.AddFolderPaths(paths);
                    settings.WriteSettings();
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

        private void ImageFolderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var settings = (App.Current as App)?.Settings;

            foreach (var item in e.AddedItems.Cast<MFImageFolder>())
            {
                item.Selected = true;
                var index = ImageFolderCollection.IndexOf(item);

                settings.ImageFolderList.ImageFolders[index].Selected = true;
            }

            foreach (var item in e.RemovedItems.Cast<MFImageFolder>())
            {
                if (ItemsDraggedSelected.Contains(item))
                {
                    ImageFolderListView.SelectedItems.Add(item);
                    continue;
                }
                item.Selected = false;
                var index = ImageFolderCollection.IndexOf(item);

                if (index >= 0)
                {
                    settings.ImageFolderList.ImageFolders[index].Selected = false;
                }
            }

            if (ImageFolderListView.SelectedItems.Count == ImageFolderCollection.Count)
            {
                SelectAllCheckbox.IsChecked = true;
            } else
            {
                SelectAllCheckbox.IsChecked = false;
            }

            settings.WriteSettings();
        }

        private List<MFImageFolder> ItemsDraggedSelected = [];

        private void ImageFolderListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var items = e.Items.Cast<MFImageFolder>();

            var hovereditems = VisualTreeHelper.FindElementsInHostCoordinates(lastPointerPos, ImageFolderListView);
            var hoveritemelem = hovereditems.First(i => i is ListViewItem);
            var hoveritem = ImageFolderListView.ItemFromContainer(hoveritemelem as ListViewItem);

            ItemsDraggedSelected.AddRange(items.Where(f => f.Selected));


            foreach (var item in items)
            {
                if (item != hoveritem)
                {
                    var itemelem = ImageFolderListView.ContainerFromItem(item) as ListViewItem;

                    VisualStateManager.GoToState(itemelem, "DragHidden", true);


                }
            }
        }

        private void ImageFolderListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var items = args.Items.Cast<MFImageFolder>();

            foreach (var item in items)
            {
                var itemelem = ImageFolderListView.ContainerFromItem(item) as ListViewItem;

                VisualStateManager.GoToState(itemelem, "DragVisible", true);
            }
            ItemsDraggedSelected.Clear();
        }

        Point lastPointerPos = new();

        private void ImageFolderListView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            lastPointerPos = e.GetCurrentPoint(null).Position;
        }

        private void SelectAllCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckbox.IsChecked ?? false) { ImageFolderListView.SelectAll(); }
            else { ImageFolderListView.DeselectAll(); }

        }

        public async Task<IEnumerable<string>> GetSelectedFolders()
        {
            return await DispatcherQueue.EnqueueAsync(() =>
            {
                return ImageFolderListView.SelectedItems.Cast<MFImageFolder>().Select(f => f.Path).ToList();
            });
        }

        private void MFImageFolderView_ColumnDataRemove(object sender, MFImageFolderView.ColumnDataRemoveEventArgs args)
        {
            _pathTexts.Remove(args.PathText);
            _imageCountTexts.Remove(args.ImageCountText);

            UpdateColumnWidths();
        }
    }
}
