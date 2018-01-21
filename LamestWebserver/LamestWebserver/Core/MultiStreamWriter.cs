using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Stream Writer who can writer multiple streams.
    /// </summary>
    public class MultiStreamWriter : IDisposable
    {
        private List<StreamWriter> _streamWriters = new List<StreamWriter>();

        /// <summary>
        /// Singals if the MultiStreamwriter is already disposed
        /// </summary>
        public bool IsDisposed = false;

        /// <summary>
        /// Creates a MultiStreamWriter.
        /// </summary>
        /// <param name="streams"></param>
        public MultiStreamWriter(IEnumerable<Stream> streams)
        {
            foreach(Stream stream in streams)
            {
                _streamWriters.Add(new StreamWriter(stream) { AutoFlush = true });
            }
        }

        /// <summary>
        /// Creates a MultiStreamWriter.
        /// </summary>
        /// <param name="streams"></param>
        public MultiStreamWriter(params Stream[] streams)
        {
            foreach (Stream stream in streams)
            {
                _streamWriters.Add(new StreamWriter(stream) { AutoFlush = true });
            }
        }

        /// <summary>
        /// Write to all streams.
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
        /// Flush all streams.
        /// </summary>
        public void Flush()
        {
            foreach (StreamWriter sw in _streamWriters)
            {
                sw.Flush();
            }
        }

        /// <summary>
        /// Close all streams.
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
                try
                {
                    if(sw.BaseStream != null)
                        sw.Dispose();
                }
                catch { }
            }

            IsDisposed = true;
        }

        /// <summary>
        /// Disposes all streams except the ones listed in the parameter.
        /// </summary>
        /// <param name="streams">The Streams not to dispose.</param>
        public void DisposeExcept(IEnumerable<Stream> streams)
        {
            foreach (StreamWriter sw in _streamWriters)
            {
                if (streams.Contains(sw.BaseStream))
                    continue;

                try
                {
                    if (sw.BaseStream != null)
                        sw.Dispose();
                }
                catch { }
            }

            IsDisposed = true;
        }

        /// <summary>
        /// De-constructor flush and closes all streams.
        /// </summary>
        ~MultiStreamWriter()
        {
            Dispose();
        }
    }
}
