using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompressionAlgorithms.Common
{

    /// <summary>
    /// Interface of Reader used in this library
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Reads next byte and moves pointer back
        /// </summary>
        /// <returns>next byte</returns>
        public int Peek();
        /// <summary>
        /// Reads from underlaying stream, saves to buffer up to 4kb to minimize syscalls
        /// </summary>
        /// <returns>next byte</returns>
        public IEnumerable<int> Read();


    }
    /// <summary>
    /// Implementation of IReader interface used in this library
    /// </summary>
    public class Reader : IReader
    {
        readonly FileStream fs;

        /// <summary>
        /// Initializes new instance of Reader
        /// </summary>
        /// <param name="dataFile">path to file</param>
        public Reader(string dataFile)
        {
            fs = File.OpenRead(dataFile);
        }

        /// <summary>
        /// Sets position of pointer to start
        /// </summary>
        public void ResetPointer()
        {
            fs.Position = 0;
        }
        public int Peek()
        {
            int res = fs.ReadByte();
            if (fs.Position > 0) fs.Position--;
            return res;
        }
        public IEnumerable<int> Read()
        {

            while (Peek() != -1)
            {
                var buffer = new byte[4096*4];
                int max = fs.Read(buffer);
                int ptr = 0;
                while (ptr != max)
                    yield return buffer[ptr++];
            }
        }
    }

    /// <summary>
    /// Interface of writer used in this library
    /// </summary>
    public interface IWriter
    {
        long BytesCached { get; }

        /// <summary>
        /// Writes string into underlaying stream
        /// </summary>
        /// <param name="s">string to be written to underlaying stream</param>
        public void Write(string s);
        /// <summary>
        /// Writes StringBuilder into underlaying stream
        /// </summary>
        /// <param name="s">StringBuilder to be written to underlaying stream</param>
        public void Write(StringBuilder s);
        /// <summary>
        /// Writes string intu underlaying stream and adds new line
        /// </summary>
        /// <param name="s">string to be written to underlaying stream</param>
        public void WriteLine(string s);
        /// <summary>
        /// writes byte array into underlaying stream
        /// </summary>
        /// <param name="bytes">byte array to be written to underlaying stream.</param>
        public void Write(byte[] bytes);
        /// <summary>
        /// Writes StringBuilder into underlaying stream and adds new line
        /// </summary>
        /// <param name="s">StringBuilder to be written to underlaying stream</param>
        public void WriteLine(StringBuilder s);
        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered data to be written to the underlaying device.
        /// </summary>
        public void Flush();
        /// <summary>
        /// writes BitArray into underlaying stream
        /// </summary>
        /// <param name="bits">BitArray to be written to underlaying stream.</param>
        void Write(BitArray bits);
    }

    // if i were to be correct, I would also use TextWriter here, but that would just
    // complicate more things.
    /// <summary>
    /// Abstract definition of Writer used in this library
    /// </summary>
    public abstract class Writer : /*TextWriter, */IWriter, IDisposable
    {
        public abstract long BytesCached { get; }

        public abstract void Dispose();
        public abstract void Flush();
        public abstract void Write(string s);
        public abstract void Write(byte[] bytes);
        public abstract void Write(StringBuilder s);
        public abstract void Write(BitArray bits);
        public abstract void WriteLine(string s);
        public abstract void WriteLine(StringBuilder s);
    }


}
