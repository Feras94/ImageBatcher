using System.Collections.Generic;

namespace ImageBatcher
{
    public class OperationParameters
    {
        public List<string> ImagesPathsList { get; set; }

        public string OutputPath { get; set; }

        public SizeType MaxSizeType { get; set; }

        public int MaxSize { get; set; }

        public ImageType ImageType { get; set; }
    }
}