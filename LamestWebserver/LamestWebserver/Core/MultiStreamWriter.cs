using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Stream Writer who can writer multiple streams
    /// </summary>
    public class MultiStreamWriter : IDisposable
    {
        private List<StreamWriter> _streamWriters = new List<StreamWriter>();

        /// <summary>
        /// Creates a MultiStreamWriter
        /// </summary>
        /// <param name="streams"></param>
        public MultiStreamWriter(IEnumerable<Stream> streams)
        {
            foreach(Stream stream in streams)
            {
                _streamWriters.Add(new StreamWriter(stream));
            }
        }

        /// <summary>
        /// Creates a MultiStreamWriter
        /// </summary>
        /// <param name="streams"></param>
        public MultiStreamWriter(params Stream[] streams)
        {
            foreach (Stream stream in streams)
            {
                _streamWriters.Add(new StreamWriter(stream));
            }
        }

        /// <summary>
        /// Write to all streams
        /// </summary>
        /// <param name="value"></param>
        public void Write(String value)
        {
            foreach(StreamWriter sw in _streamWriters)
            {
                sw.Write(value);
            }
        }

        /// <summary>
        /// Write a Line to all streams
        /// </summary>
        /// <param name="value"></param>
        public void WriteLine(String value)
        {
            foreach (StreamWriter sw in _streamWriters)
            {
                sw.WriteLine(value);
            }
        }

        /// <summary>
        /// Flush all streams
        /// </summary>
        public void Flush()
        {
            foreach (StreamWriter sw in _streamWriters)
            {
                sw.Flush();
            }
        }

        /// <summary>
        /// Close all streams
        /// </summary>
        public void Close()
        {
            foreach (StreamWriter sw in _streamWriters)
            {
                sw.Close();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach(StreamWriter sw in _streamWriters)
            {
                sw.Dispose();
            }
        }

        /// <summary>
        /// Deconstructor flush and closes all streams 
        /// </summary>
        ~MultiStreamWriter()
        {
            Dispose();
        }
    }
}
