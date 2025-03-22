using QuickDraw.Utilities;
using System;
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
                return await Filesystem.GetFolderImages(path);
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
