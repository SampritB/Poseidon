using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Poseidon.Models
{
    public class LabelModel
    {
        public int Scale { get; set; }        
        public int TranslateX { get; set; }
        public int TranslateY { get; set; }
        public List<Label> Labels { get; set; }
        public ImageInfo ImageInfo { get; set; }
    }
}