using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Graphics
{
    /// <summary>
    /// A Thumbnail image.
    /// </summary>
    public class Thumbnail
    {
        /// <summary>
        /// The Thumbnail image as Bitmap.
        /// </summary>
        public Image Image;

        /// <summary>
        /// Creates a new Thumbnail from an Image.
        /// </summary>
        /// <param name="image">The image to create a thumbnail of.</param>
        /// <param name="maxWidth">The maximum width in pixels of the thumbnail.</param>
        /// <param name="maxHeight">The maximum height in pixels of the thumbnail.</param>
        public Thumbnail(Image image, int maxWidth, int maxHeight)
        {
            float invAspectRatio = (float)image.Height / (float)image.Width;

            int width = maxWidth;
            int height = (int)System.Math.Round((float)maxWidth * invAspectRatio);

            if (height > maxHeight)
            {
                height = maxHeight;
                width = (int)System.Math.Round((float)height / invAspectRatio);
            }

            Image = new Bitmap(width, height);
            
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(Image))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);
            }
        }

        /// <summary>
        /// Creates a new Thumbnail from a File.
        /// </summary>
        /// <param name="path">The Path of the File.</param>
        /// <param name="maxWidth">The maximum width in pixels of the thumbnail.</param>
        /// <param name="maxHeight">The maximum height in pixels of the thumbnail.</param>
        public Thumbnail(string path, int maxWidth, int maxHeight) : this(Bitmap.FromFile(path), maxWidth, maxHeight) { }

        /// <summary>
        /// Creates a new Thumbnail from a Stream.
        /// </summary>
        /// <param name="stream">The stream to load the file from.</param>
        /// <param name="maxWidth">The maximum width in pixels of the thumbnail.</param>
        /// <param name="maxHeight">The maximum height in pixels of the thumbnail.</param>
        public Thumbnail(Stream stream, int maxWidth, int maxHeight) : this(Bitmap.FromStream(stream), maxWidth, maxHeight) { }

        /// <summary>
        /// Saves the Thumbnail image to a File.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        public void Save(string path)
        {
            if (!Directory.GetParent(path).Exists)
                Directory.CreateDirectory(Directory.GetParent(path).FullName);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                Image.Save(memoryStream, ImageFormat.Jpeg);
                memoryStream.WriteTo(File.OpenWrite(path));
            }
        }

        /// <summary>
        /// Writes the Thumbnail to a Stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void Save(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Image.Save(memoryStream, ImageFormat.Jpeg);
                memoryStream.WriteTo(stream);
            }
        }
    }
}
