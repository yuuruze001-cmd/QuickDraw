using QuickDraw.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDraw.Models
{

    public class MFImageFolderList : INotifyCollectionChanged
    {
        public List<MFImageFolder> ImageFolders { get; set; } = [];

        public event NotifyCollectionChangedEventHandler CollectionChanged;

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

        public void AddFolderPath(string path)
        {
            var folder = new MFImageFolder(path, 0, true);

            var existingFolder = ImageFolders.FirstOrDefault<MFImageFolder>((f) => f.Path == folder.Path);
            var folderIndex = ImageFolders.IndexOf(existingFolder);

            if (folderIndex != -1)
            {
                ImageFolders[folderIndex] = folder;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, folder, existingFolder, folderIndex));
            }
            else
            {
                ImageFolders.Add(folder);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, folder));
            }

            Task.Run(async () =>
            {
                return await GetFolderImages(path);
            }).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    // Log error
                } else
                {
                    folder.ImageCount = t.Result.Count();
                    folder.IsLoading = false;

                    existingFolder = ImageFolders.FirstOrDefault<MFImageFolder>((f) => f.Path == folder.Path);
                    folderIndex = ImageFolders.IndexOf(existingFolder);

                    if (folderIndex != -1)
                    {
                        ImageFolders[folderIndex] = folder;
                        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, folder, existingFolder, folderIndex));
                    }
                }
            });
        }

        public void AddFolderPaths(IEnumerable<string> paths)
        {
            // TODO: Add paths in order, then load image counts in parallel
            foreach (var path in paths)
            {
                AddFolderPath(path);
            }
        }
    }
}
