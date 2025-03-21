using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuickDraw.Models
{
    public class MFImageFolder
    {
        public string Path { get; set; }
        public int ImageCount { get; set; }

        [JsonIgnore]
        public bool IsLoading { get; set; } = false;

        public bool Selected { get; set; } = false;

        public MFImageFolder(string path, int imageCount, bool isLoading = false)
        {
            Path = path;
            ImageCount = imageCount;
            IsLoading = isLoading;
        }
    }
}
