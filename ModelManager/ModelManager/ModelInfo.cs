using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager
{
    public class ModelInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? InfoJsonPath { get; set; }
        public string? ThumbnailPath { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public Image? Thumbnail { get; set; }
    }
}
