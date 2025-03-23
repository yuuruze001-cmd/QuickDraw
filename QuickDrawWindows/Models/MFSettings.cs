using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickDraw.Models
{
    public static class ObjectExtension
    {
        public static void CopyPropertiesTo(this object fromObject, object toObject)
        {
            PropertyInfo[] toObjectProperties = toObject.GetType().GetProperties();
            foreach (PropertyInfo propTo in toObjectProperties)
            {
                PropertyInfo propFrom = fromObject.GetType().GetProperty(propTo.Name);
                if (propFrom != null && propFrom.CanWrite)
                    propTo.SetValue(toObject, propFrom.GetValue(fromObject, null), null);
            }
        }
    }

    public enum TimerEnum
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

    static class TimerEnumExtension
    {
        private static Dictionary<TimerEnum, uint> TimerEnumToSeconds { get; } = new()
        {
            { TimerEnum.T30s, 30 },
            { TimerEnum.T1m, 60 },
            { TimerEnum.T2m, 120 },
            { TimerEnum.T5m, 300 },
            { TimerEnum.NoLimit, 0 }
        };

        public static uint ToSeconds(this TimerEnum e)
        {
            return TimerEnumToSeconds[e];
        }

        public static double ToSliderValue(this TimerEnum e)
        {
            return (double)((int)e);
        }

        public static TimerEnum ToTimerEnum(this double e)
        {
            return (TimerEnum)(Math.Clamp((int)e,0, 4));
        }
    }

    public class MFSettings : INotifyPropertyChanged
    {

        public MFImageFolderList ImageFolderList { get; set; } = new MFImageFolderList();

        [JsonIgnore]
        private Task writeTask;
        [JsonIgnore]
        private Queue<Func<Task>> writeTasksQueue = new();

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public List<string> SlidePaths { get; set; } = [];

        public TimerEnum SlideTimerDuration { 
            get; 
            set; 
        }

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

                newSettings.CopyPropertiesTo(this);
            });
        }
    }
}
