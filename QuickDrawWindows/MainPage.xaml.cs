using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Text.Json;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    /// <summary>
    /// Converts the value of the internal slider into text.
    /// </summary>
    /// <remarks>Internal use only.</remarks>
    internal class StringToEnumConverter : IValueConverter
    {
        private readonly Type _enum;

        public StringToEnumConverter(Type type)
        {
            _enum = type;
        }

        public object Convert(object value,
                Type targetType,
                object parameter,
                string language)
        {
            var _name = Enum.ToObject(_enum, (int)Double.Parse((string)value));

            // Look for a 'Display' attribute.
            var _member = _enum
                .GetRuntimeFields()
                .FirstOrDefault(x => x.Name == _name.ToString());
            if (_member == null)
            {
                return _name;
            }

            var _attr = (DisplayAttribute)_member
                .GetCustomAttribute(typeof(DisplayAttribute));
            if (_attr == null)
            {
                return _name;
            }

            return _attr.Name;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            string language)
        {
            return value; // Never called
        }

    }

    /// <summary>
    /// Converts the value of the internal slider into text.
    /// </summary>
    /// <remarks>Internal use only.</remarks>
    internal partial class DoubleToEnumConverter : IValueConverter
    {
        private readonly Type _enum;

        public DoubleToEnumConverter(Type type)
        {
            _enum = type;
        }

        public object Convert(object value,
                Type targetType,
                object parameter,
                string language)
        {
            var _name = Enum.ToObject(_enum, (int)(double)value);

            // Look for a 'Display' attribute.
            var _member = _enum
                .GetRuntimeFields()
                .FirstOrDefault(x => x.Name == _name.ToString());
            if (_member == null)
            {
                return _name;
            }

            var _attr = (DisplayAttribute)_member
                .GetCustomAttribute(typeof(DisplayAttribute));
            if (_attr == null)
            {
                return _name;
            }

            return _attr.Name;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            string language)
        {
            return value; // Never called
        }
    }
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

    public class ImageFolder
    {
        public string Path { get; set; }
        public int ImageCount { get; set; }

        [JsonIgnore]
        public bool IsLoading { get; set; } = false;

        public ImageFolder(string path, int imageCount, bool isLoading = false)
        {
            Path = path;
            ImageCount = imageCount;
            IsLoading = isLoading;
        }
    }

    public class GridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double val = (double)value;
            GridLength gridLength = new(val);

            return gridLength;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            GridLength val = (GridLength)value;

            return val.Value;
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static bool InvertBool(bool value)
        {
            return !value;
        }

        enum TimerEnum
        {
            [Display(Name = "30s")]
            T30s,
            [Display(Name = "1m")]
            T1m,
            [Display(Name = "2m")]
            T2m,
            [Display(Name = "5m")]
            T5m,
            [Display(Name = "No Limit")]
            NoLimit
        };

        public ObservableCollection<ImageFolder> ImageFolders { get; set; }
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

        private readonly SortedSet<TextBlock> _pathElems = new(new ByWidth());
        private readonly SortedSet<TextBlock> _imageCountElems = new(new ByWidth());
        private readonly HashSet<ColumnDefinition> _pathColumns = [];
        private readonly HashSet<ColumnDefinition> _imageCountColumns = [];

        public double PathColumnWidth { get; set; } = 1000;
        public double ImageCountColumnWidth { get; set; } = 1000;

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Resources.Add("doubleToEnumConverter", new DoubleToEnumConverter(typeof(TimerEnum)));
            this.Resources.Add("stringToEnumConverter", new StringToEnumConverter(typeof(TimerEnum)));

            this.Loaded += async (_, _) =>
            {
                await ReadFolders();
            };


        }
        Task writeTask;
        Queue<Func<Task>> writeTasksQueue = new();

        private async Task _writeFolders()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

            var qdDataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

            var file = await qdDataFolder.CreateFileAsync("folders.json", Windows.Storage.CreationCollisionOption.OpenIfExists);

            using var stream = await file.OpenStreamForWriteAsync();
            await JsonSerializer.SerializeAsync(stream, ImageFolders);
            stream.SetLength(stream.Position);
            stream.Dispose();
        }

        // Writes folder, makes sure we don't overlap with other writes
        void WriteFolders()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (writeTask == null)
                {
                    void WriteContinue()
                    {
                        if (writeTasksQueue.Count > 0)
                        {
                            writeTask = writeTasksQueue.Dequeue()().ContinueWith(Task =>
                            {
                                WriteContinue();
                            });
                        }
                        else
                        {
                            writeTask = null;
                        }
                    }

                    writeTask = _writeFolders().ContinueWith(Task =>
                    {
                        WriteContinue();
                    });
                }
                else
                {
                    writeTasksQueue.Enqueue(_writeFolders);
                }
            });
        }

        async Task ReadFolders()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

            var qdDataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

            var file = await qdDataFolder.GetFileAsync("folders.json");

            using var stream = await file.OpenStreamForReadAsync();
            try
            {
                ImageFolders = await JsonSerializer.DeserializeAsync<ObservableCollection<ImageFolder>>(stream);
                stream.Dispose();
            }
            catch
            {
                // No data or other errors
                ImageFolders = [];
            }

            ImageFolderListView.ItemsSource = ImageFolders;
            ImageFolders.CollectionChanged += (sender, e) =>
            {
               WriteFolders();
            };
        }
        
        private void UpdateColumnWidths(Grid grid)
        {
            if (_pathElems.Count < 1 || _imageCountElems.Count < 1) { return; }

            ProgressRing progressRing = _imageCountElems.Max.Parent.FindDescendant<ProgressRing>();
            ImageCountColumnWidth = Math.Max(_imageCountElems.Max.ActualWidth, progressRing.ActualWidth) + 20;

            var gridWidth = grid.ActualWidth;
            var availableWidth = gridWidth - (grid.ColumnDefinitions[3].ActualWidth + ImageCountColumnWidth + 20);

            var maxPathColumnWidth = _pathElems.Max != null ? _pathElems.Max.PreWrappedWidth() + 1 : 0;
            PathColumnWidth = Math.Max(100, Math.Min(availableWidth, maxPathColumnWidth));

            foreach (ColumnDefinition pathColumn in _pathColumns)
            {
                pathColumn.Width = new GridLength(PathColumnWidth, GridUnitType.Pixel);
            }

            foreach (ColumnDefinition imageCountColumn in _imageCountColumns)
            {
                imageCountColumn.Width = new GridLength(ImageCountColumnWidth, GridUnitType.Pixel);
            }
        }

        public void HandlePathTextSizeChange(TextBlock pathText)
        {
            var grid = (Grid)pathText.Parent;

            _pathElems.Remove(pathText);
            _pathElems.Add(pathText);

            _pathColumns.Add(grid.ColumnDefinitions[0]);

            UpdateColumnWidths(grid);
        }
        
        public void HandleImageCountSizeChange(TextBlock imageCount)
        {
            var grid = (Grid)imageCount.Parent;

            _imageCountElems.Remove(imageCount);
            _imageCountElems.Add(imageCount);

            _imageCountColumns.Add(grid.ColumnDefinitions[1]);

            UpdateColumnWidths(grid);
        }

        public void HandleGridSizeChange(Grid grid)
        {
            UpdateColumnWidths(grid);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.Window.NavigateToSlideshow();
        }

        private static IEnumerable<string> GetFolderImages(string filepath)
        {
            var enumerationOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System | System.IO.FileAttributes.ReparsePoint
            };

            IEnumerable<string> files = Directory.EnumerateFiles(filepath, "*.*", enumerationOptions)
                                    .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                            || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

            return files;
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
                        string filepath = null;
                        shellItemArray.GetCount(out count);

                        Task.Run(() =>
                        {
                            for (uint i = 0; i < count; i++)
                            {
                                shellItemArray.GetItemAt(i, out IShellItem shellItem);

                                if (shellItem != null)
                                {
                                    shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr i_result);
                                    filepath = Marshal.PtrToStringAuto(i_result);
                                    Marshal.FreeCoTaskMem(i_result);

                                    var folder = new ImageFolder(filepath, 0, true);

                                    DispatcherQueue.TryEnqueue(() => {
                                        var existingFolder = ImageFolders.FirstOrDefault<ImageFolder>((f) => f.Path == folder.Path);
                                        var folderIndex = ImageFolders.IndexOf(existingFolder);

                                        if (folderIndex != -1)
                                        {
                                            ImageFolders[folderIndex] = folder;
                                        }
                                        else
                                        {
                                            ImageFolders.Add(folder);
                                        }
                                    });

                                    IEnumerable<string> files = GetFolderImages(filepath);

                                    folder.ImageCount = files.Count();
                                    folder.IsLoading = false;

                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        var existingFolder = ImageFolders.FirstOrDefault<ImageFolder>((f) => f.Path == folder.Path);
                                        var folderIndex = ImageFolders.IndexOf(existingFolder);

                                        if (folderIndex != -1)
                                        {
                                            ImageFolders[folderIndex] = folder;
                                        }
                                    });
                                }
                            }
                        });
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
