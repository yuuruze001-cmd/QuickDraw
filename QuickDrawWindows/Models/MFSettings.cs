using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickDraw.Models
{
    public class MFSettings : INotifyPropertyChanged
    {
        public MFImageFolderList ImageFolderList { get; set; } = new MFImageFolderList();

        [JsonIgnore]
        private Task writeTask;
        [JsonIgnore]
        private Queue<Func<Task>> writeTasksQueue = new();

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task _writeSettings()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

            var qdDataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

            var file = await qdDataFolder.CreateFileAsync("settings.json", Windows.Storage.CreationCollisionOption.OpenIfExists);

            using var stream = await file.OpenStreamForWriteAsync();
            await JsonSerializer.SerializeAsync(stream, this);
            stream.SetLength(stream.Position);
            stream.Dispose();
        }

        // Writes folder, makes sure we don't overlap with other writes
        public void WriteSettings()
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

                writeTask = _writeSettings().ContinueWith(Task =>
                {
                    WriteContinue();
                });
            }
            else
            {
                writeTasksQueue.Enqueue(_writeSettings);
            }
        }

        private void UpdateSettings(MFSettings newSettings)
        {
            this.ImageFolderList = newSettings.ImageFolderList;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ImageFolderList)));
        }

        public void ReadSettings()
        {
            Task.Run(async () =>
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

                var qdDataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

                MFSettings newSettings = new();

                try
                {
                    var file = await qdDataFolder.CreateFileAsync("settings.json", Windows.Storage.CreationCollisionOption.OpenIfExists);

                    using var stream = await file.OpenStreamForReadAsync();

                    newSettings = await JsonSerializer.DeserializeAsync<MFSettings>(stream);
                    stream.Dispose();
                }
                catch (JsonException)
                {
                    // Log this
                }
                catch
                {
                    // Other errors
                }

                UpdateSettings(newSettings);
            });
        }
    }
}
