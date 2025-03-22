using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickDraw.Utilities
{
    public class Filesystem
    {
        public static async Task<IEnumerable<string>> GetFolderImages(string filepath)
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
    }
}
