using Microsoft.UI.Xaml.Shapes;
using QuickDraw.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDraw.Models
{
    public class MFImageFolderList : INotifyCollectionChanged
    {
        public List<MFImageFolder> ImageFolders { get; set; } = [];

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        private static async Task<IEnumerable<string>> GetFolderImages(string filepath)
        {
            return await Task.Run(() =>
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
            });
        }

        public static async Task<IEnumerable<string>> GetImages(IEnumerable<string> folders)
        {
            ConcurrentBag<string> images = [];

            await Parallel.ForEachAsync<string>(folders, async (folder, ct) =>
            {
                IEnumerable<string> files = await GetFolderImages(folder);

                foreach (var file in files)
                {
                    images.Add(file);
                }
            });

            return images;
        }

        public void UpdateFolderCount(MFImageFolder existingFolder)
        {
            var folder = new MFImageFolder(existingFolder.Path, existingFolder.ImageCount, existingFolder.Selected, true);
            var folderIndex = ImageFolders.IndexOf(existingFolder);

            if (folderIndex == -1)
            {
                return; // Folder isn't in list, TODO: should log
            }

            ImageFolders[folderIndex] = folder;
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, folder, existingFolder, folderIndex));

            LoadFolderCount(folder);
        }

        public void UpdateFolderCounts()
        {
            List<MFImageFolder> folders = [.. ImageFolders];
            foreach(var folder in folders)
            {
                UpdateFolderCount(folder);
            }
        }

        private void LoadFolderCount(MFImageFolder folder)
        {
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                return await GetFolderImages(folder.Path);
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    // Log error
                }
                else
                {
                    folder.ImageCount = t.Result.Count();
                    folder.IsLoading = false;

                    var existingFolder = ImageFolders.FirstOrDefault<MFImageFolder>((f) => f.Path == folder.Path);
                    var folderIndex = existingFolder != null ? ImageFolders.IndexOf(existingFolder) : -1;

                    if (folderIndex != -1)
                    {
                        ImageFolders[folderIndex] = folder;
                        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, folder, existingFolder, folderIndex));
                    }
                }
            });
        }

        public void AddFolderPath(string path)
        {

            var (folderIndex, existingFolder) = ImageFolders.Index().FirstOrDefault((ft) => ft.Item.Path == path);
            MFImageFolder? folder = null;

            if (existingFolder != null)
            {
                folder = new MFImageFolder(existingFolder.Path, existingFolder.ImageCount, existingFolder.Selected, true);
                ImageFolders[folderIndex] = folder;
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, folder, existingFolder, folderIndex));
            }
            else
            {
                folder = new MFImageFolder(path, 0, false, true);
                ImageFolders.Add(folder);
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, folder));
            }

            if (folder != null)
            {
                LoadFolderCount(folder);
            }
        }

        public void AddFolderPaths(IEnumerable<string> paths)
        {
            // TODO: Add paths in order, then load image counts in parallel
            foreach (var path in paths)
            {
                AddFolderPath(path);
            }
        }

        public void RemoveFolder(MFImageFolder folder)
        {
            ImageFolders.Remove(folder);
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, folder));
        }
    }
}
