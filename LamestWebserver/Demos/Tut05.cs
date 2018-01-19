using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using System.Drawing;
using System.IO;

namespace Demos
{
    public class Tut05 : DataResponse
    {
        public Tut05() : base(nameof(Tut05), true) { }

        protected override byte[] GetDataContents(HttpSessionData sessionData, out string contentType, ref Encoding encoding)
        {
            Bitmap bitmap = new Bitmap(512, 512);

            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    if (Math.Sqrt(Math.Pow(x - 255, 2) + Math.Pow(y - 255, 2)) > 126f)
                        bitmap.SetPixel(x, y, Color.FromArgb(y / 2, (x + y) / 4, x / 2));
                    else
                        bitmap.SetPixel(x, y, Color.FromArgb(x / 2, 255 - x / 2, 255 - (x + y) / 4));
                }
            }

            MemoryStream stream = new MemoryStream();
            contentType = "image/png";

            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            return stream.ToArray();
        }
    }
}
