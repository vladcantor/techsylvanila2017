using ImageTextRecognition.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTextRecognition
{
    public class ImageToTextResponseModel
    {
        public string language { get; set; }
        public string orientation { get; set; }

        public List<Region> regions { get; set; }
    }
}
