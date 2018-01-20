using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using LamestWebserver;

namespace Demos
{
    /// <summary>
    /// This Tutorial focuses on the usage of DataResponses.
    /// </summary>
    public class Tut05 : DataResponse
    {
        /// <summary>
        /// This Tutorial will be hosted at "/Tut05".
        /// The constructor for this class should be public and parameterless in order to be automatically discoverable.
        /// </summary>
        public Tut05() : base(nameof(Tut05)) { }

        /// <summary>
        /// Retrieves the Response from this DataResponse.
        /// </summary>
        /// <param name="sessionData">The current SessionData. (contains all kinds of information regarding HTTP-Parameters, User Handling, Cookies, etc.)</param>
        /// <param name="contentType">The returned ContentType of the Response.</param>
        /// <param name="encoding">The encoding of the returned content. Unicode by default.</param>
        /// <returns>Returns the response as byte array.</returns>
        protected override byte[] GetDataContents(HttpSessionData sessionData, out string contentType, ref Encoding encoding)
        {
            // Fill a 512x512px Bitmap with color gradients.
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
            
            // Save the bitmap to a MemoryStream;
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            // Set the content type.
            contentType = "image/png";

            // Return the MemoryStream as byte[].
            return stream.ToArray();
        }
    }
}
