using System.Collections.Generic;
using System.IO;

namespace LamestWebserver.Core
{
    /// <summary>
    /// MultiStream Class for easy writing of many streams
    /// </summary>
    public class MultiStream : Stream
    {
        /// <summary>
        /// List of all streams
        /// </summary>
        public readonly List<Stream> Streams = new List<Stream>();

        /// <summary>
        /// CanRead attribut of all streams
        /// </summary>
        public override bool CanRead
        {
            get
            {
                bool ret = false;
                foreach(Stream stream in Streams)
                {
                    ret = stream.CanRead;
                    if (!ret)
                        return ret;
                }
                return ret;
            }


        }

        /// <summary>
        /// CanSeek attribut of all streams
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                bool ret = false;
                foreach (Stream stream in Streams)
                {
                    ret = stream.CanSeek;
                    if (!ret)
                        return ret;
                }
                return ret;
            }


        }

        /// <summary>
        /// Write attribute of all streams
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                bool ret = false;
                foreach (Stream stream in Streams)
                {
                    ret = stream.CanWrite;
                    if (!ret)
                        return ret;
                }
                return ret;
            }

        }

        /// <summary>
        /// length of the first stream
        /// </summary>
        public override long Length
        {
            get
            {
                long ret = 0;
                foreach(Stream stream in Streams)
                {
                    if (Length > ret)
                        ret = Length;
                }
                return ret;
            }
        }

        /// <summary>
        /// position of all streams
        /// </summary>
        public override long Position
        {
            get
            {
                long ret = 0;
                foreach (Stream stream in Streams)
                {
                    if (Length > ret)
                        ret = Length;
                }
                return ret;
            }
            set
            {
                foreach(Stream stream in Streams)
                {
                    stream.Position = value;
                }
            }
        }

        /// <summary>
        /// flush all streams
        /// </summary>
        public override void Flush()
        {
            foreach(Stream stream in Streams)
            {
                stream.Flush();
            }
        }

        /// <summary>
        /// read all streams
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesWritten = 0; 
            foreach (Stream stream in Streams)
            {
                bytesWritten = stream.Read(buffer, offset, count);
            }

            return bytesWritten;
        }

        /// <summary>
        /// Seek all streams
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = 0;
            foreach (Stream stream in Streams)
            {
               newPosition = stream.Seek(offset, origin);
            }

            return newPosition;
        }

        /// <summary>
        /// Set Length of all streams
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            foreach (Stream stream in Streams)
            {
                stream.SetLength(value);
            }
        }

        /// <summary>
        /// write all streams
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (Stream stream in Streams)
            {
                stream.Write(buffer, offset, count);
            }
        }

        public override void Close()
        {
            foreach (Stream stream in Streams)
            {
                stream.Close();
            }
        }
    }
}
