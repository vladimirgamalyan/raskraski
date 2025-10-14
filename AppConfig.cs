using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace raskraski
{
    public class AppConfig
    {
        public string PictureDir { get; set; } = "";
        public int CursorHotSpotX { get; set; } = 0;
        public int CursorHotSpotY { get; set; } = 0;
        public double CursorScale { get; set; } = 1.0;
    }
}
