using CompressionAlgorithms.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompressionAlgorithms.Huffman
{

    /// <summary>
    /// Interface of node used in Huffman Compression algorithm
    /// </summary>
    interface INode : IComparable<INode>
    {
        /// <summary>
        /// Represents frequency of given character
        /// </summary>
        long Count { get; set; }
        /// <summary>
        /// Represents character in ascii (0-255)
        /// </summary>
        int Value { get; set; }
        /// <summary>
        /// Represents left node, may be null
        /// </summary>
        INode? Left { get; }
        /// <summary>
        /// Represents right node, may be null
        /// </summary>
        INode? Right { get; }
        /// <summary>
        /// Represents parent node, may be null
        /// </summary>
        INode? Parent { get; }
        /// <summary>
        /// Represents Path to given node, left = 0, right = 1
        /// </summary>
        BitArray Path { get; set; }
        /// <summary>
        /// Represents length of the path to the node (max 16)
        /// </summary>
        int PathLength { get; set; }
        /// <summary>
        /// Represents if the node is leaf or not.
        /// </summary>
        bool IsLeaf { get; }
        /// <summary>
        /// Computes the 64 bit version of node representation for encoding the tree
        /// </summary>
        /// <returns></returns>
        byte[]? GetEncodeRepresentation();
    }
    /// <summary>
    /// Implementation of the INode used in Huffman Compression algorithm
    /// </summary>
    class HuffmanNode : INode
    {
        public long Count { get; set; }
        public int Value { get; set; }
        public INode? Left { get; internal set; }
        public INode? Right { get; internal set; }
        public BitArray Path { get; set; } = new BitArray(256, false);
        public INode? Parent { get; internal set; }
        int INode.PathLength { get; set; }
        public bool IsLeaf { get => Left == null && Right == null; }
        /// <summary>
        /// private member that stores encoded 64 bit representation for tree encoding
        /// </summary>
        byte[]? _bitRepr;
        public override string ToString()
        {
            StringBuilder res = new StringBuilder();

            if (IsLeaf)
                res.Append($"*{Value}:{Count} ");
            else
            {
                res.Append($"{Count} ");
                // in case of one symbol
                if (Left != null) res.Append(Left.ToString());
                res.Append(Right.ToString());
            }
            return res.ToString();
        }
        public int CompareTo(INode other)
        {
            if (Count.CompareTo(other.Count) != 0)
            {
                return Count.CompareTo(other.Count);
            }
            else if (Value.CompareTo(other.Value) != 0)
            {
                return Value.CompareTo(other.Value);
            }
            else return 0;
        }
        public byte[]? GetEncodeRepresentation()
        {
            if (_bitRepr != null) return _bitRepr;

            var bitRepr = new BitArray(64);

            if (this.IsLeaf)
            {

                //bit 0: obsahuje hodnotu 1, která indikuje, že jde o list
                bitRepr[0] = true;

                var val = new BitArray(new byte[] { (byte)Value });
                //stored LSB to MSB
                // may need to use Array.Reverse

                //bity 56 - 63: 8 - bitová hodnota znaku uloženého v daném listu
                for (int i = 0; i < 8; i++)
                {
                    bitRepr[56 + i] = val[i];
                }

                var wei = new BitArray(BitConverter.GetBytes(Count));
                //stored LSB to MSB
                // may need to use Array.Reverse
                //bity 1-55: obsahují spodních 55 bitů váhy daného uzlu
                for (int i = 1; i < 56; i++)
                {
                    bitRepr[i] = wei[i - 1];
                }
            }
            else
            {
                //bit 0: obsahuje hodnotu 0, která indikuje, že jde o vnitřní uzel
                bitRepr[0] = false;

                //bity 56 - 63: jsou nastaveny na 0
                for (int i = 0; i < 8; i++)
                {
                    bitRepr[56 + i] = false;
                }

                var wei = new BitArray(BitConverter.GetBytes(Count));
                //bity 1-55: obsahují spodních 55 bitů váhy daného uzlu
                for (int i = 1; i < 56; i++)
                {
                    bitRepr[i] = wei[i - 1];
                }

            }
            _bitRepr = new byte[8];
            bitRepr.CopyTo(_bitRepr, 0);
            return _bitRepr;
        }
    }
    /// <summary>
    /// Represents Huffman coding algorithm.
    /// </summary>
    public static class Huffman
    {
        /// <summary>
        /// Builds the HuffmanTree tree used for encoding in Huffman's compression algorithm
        /// </summary>
        /// <param name="data">Input to read data from</param>
        /// <returns></returns>
        public static HuffmanTree BuildTree(IReader data)
        {
            // Using SortedSet instead of two lists for simpler (although less efficient
            // way of building the tree. 
            // BitArray[256] used as quick way of remembering whether the symbol has already been
            // stored. Could have used HashSet as well.

            SortedSet<HuffmanNode> nodes = new SortedSet<HuffmanNode>();
            HuffmanTree tree = new HuffmanTree();

            Span<long> items = stackalloc long[256];
            BitArray seen = new BitArray(256);

            if (data.Peek() == -1) return tree;

            // using IEnumerable method for IReader, because I find it more appealing,
            // when using it I don't have to deal with Peek() and Read() == -1 etc.
            foreach (int current in data.Read())
            {
                items[current]++;
                seen[current] = true;
            }

            CreateNodes(nodes, ref items, seen);

            #region build a tree
            HuffmanNode n1, n2;
            while (nodes.Count > 1)
            {
                n1 = nodes.Min;
                nodes.Remove(n1);
                n2 = nodes.Min;
                nodes.Remove(n2);

                HuffmanNode newNode = new HuffmanNode() { Count = n1.Count + n2.Count, Left = n1, Right = n2, Value = n1.Value + n2.Value + 256 };
                n1.Parent = newNode;
                n2.Parent = newNode;
                nodes.Add(newNode);
            }
            if (nodes.Count == 1)
            {
                tree.SetRoot(nodes.Min);
                nodes.Remove(nodes.Min);
            }
            #endregion
            #region Traverse the tree and precalculate paths to individual symbol
            tree.GeneratePaths();
            #endregion
            return tree;
        }
        /// <summary>
        /// Generates HuffmanNode nodes and adds them into nodes
        /// </summary>
        private static void CreateNodes(SortedSet<HuffmanNode> nodes, ref Span<long> items, BitArray seen)
        {
            // Basically just parsing array of longs and creating nodes.
            // Not sure if it would be quicker to insert List<HuffmanNode> into SortedSet at once
            // possible optimization
            for (int i = 0; i < items.Length; ++i)
                if (seen[i] == true)
                    nodes.Add(new HuffmanNode() { Count = items[i], Value = (byte)i });
        }
        /// <summary>
        /// Test method for allowing strings to be processed
        /// usage: Huffman.BuildTree(new StreamReader(new MemoryStream("aabbcde".Select(x => (byte)x).ToArray())))
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static HuffmanTree BuildTree(StreamReader data)
        {
            SortedSet<HuffmanNode> nodes = new SortedSet<HuffmanNode>();
            HuffmanTree tree = new HuffmanTree();

            Span<long> items = stackalloc long[256];
            BitArray seen = new BitArray(256);

            #region Read data from input and create nodes
            if (data.Peek() == -1) return tree;
            int current;

            while ((current = data.Read()) != -1)
            {
                items[current]++;
                seen[current] = true;
            }

            CreateNodes(nodes, ref items, seen);
            #endregion
            #region build a tree
            HuffmanNode n1, n2;
            while (nodes.Count > 1)
            {
                n1 = nodes.Min;
                nodes.Remove(n1);
                n2 = nodes.Min;
                nodes.Remove(n2);

                HuffmanNode newNode = new HuffmanNode() { Count = n1.Count + n2.Count, Left = n1, Right = n2, Value = n1.Value + n2.Value + 256 };
                n1.Parent = newNode;
                n2.Parent = newNode;
                nodes.Add(newNode);
            }
            if (nodes.Count == 1)
            {
                tree.SetRoot(nodes.Min);
                nodes.Remove(nodes.Min);
            }
            #endregion
            #region Traverse the tree and precalculate paths to individual symbol
            tree.GeneratePaths();
            #endregion
            return tree;
        }
        /// <summary>
        /// Encodes the input data and writes it encoded into IWriter
        /// </summary>
        /// <param name="tree">Tree that will be used to encode the data</param>
        /// <param name="data">Data to encode</param>
        /// <param name="writeTo">Output IWriter</param>
        public static void EncodeBy(HuffmanTree tree, IReader data, IWriter writeTo, int memBufferLimit=4096, int memWriteLimit=4096)
        {
            #region save encoded tree into the file
            tree.GetTreeEncoded(out byte[] treeEncoded);
            writeTo.Write(treeEncoded);
            #endregion
            #region Encode read dada from input
            int b = memBufferLimit;
            BitArray buffer = new BitArray(b);
            int ptr = 0;
            foreach (var symbol in data.Read())
            {
                BitArray path = tree.Paths[symbol].bitArray;
                int length = tree.Paths[symbol].bitsOccupied;
                for (int i = 0; i < length; i++)
                {
                    WriteBitToBuffer(ref buffer, ref ptr, path[i], writeTo, memLimit : memWriteLimit);
                }
            }
            #endregion
            #region write remaining data in buffer if any left
            if (ptr != 0)
            {
                Trim(ref buffer, ptr);
                writeTo.Write(buffer);
                writeTo.Flush();
            }
            #endregion
            writeTo.Flush();
        }

        /// <summary>
        /// Removes trailing zero bytes || UGLY ||
        /// </summary>
        /// <param name="buffer"></param>
        private static void Trim(ref BitArray buffer, int ptr)
        {
            byte[] @new = new byte[buffer.Count/8];
            buffer.CopyTo(@new, 0);
            // I need to trim bytes after pointer
            // last bit is in (ptr)th position, adding (ptr-1)%8 to move it to next whole byte 
            int roundedPtr;
            if ((8 - (ptr) % 8) == 8)
            {
                roundedPtr = ptr;
            }
            else
            {
                roundedPtr = ptr + (8 - (ptr) % 8);
            }

            // divide it by 8 to get number of bytes
            Array.Resize(ref @new, roundedPtr/8);
            buffer = new BitArray(@new);
        }

        /// <summary>
        /// Ensures that every time buffer is filled it is written and buffer is reset
        /// </summary>
        /// <param name="currentBuffer">reference to buffer</param>
        /// <param name="bit">bit to be writter</param>
        private static void WriteBitToBuffer(ref BitArray currentBuffer, ref int pointer, bool bit, IWriter writeTo, long memLimit = 4096)
        {
            // Checking whether buffer would overflow, in that case write to output and reset pointer
            // else write to buffer and bump the pointer

            if (currentBuffer.Length == pointer)
            {
                writeTo.Write(currentBuffer);
                currentBuffer = new BitArray(pointer);
                pointer = 0;
            }
            currentBuffer[pointer++] = bit;

            if(writeTo.BytesCached < memLimit)
            {
                writeTo.Flush();
            }

        }
        /// <summary>
        /// Decodes the input data and writes it decoded into IWriter
        /// </summary>
        /// <param name="data">Encoded data</param>
        /// <param name="writeTo">Output IWriter</param>
        /// <exception cref="NotImplementedException"></exception>
        public static void DecodeBy(IReader data, IWriter writeTo)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Represents data structure that is used in Huffman's compression algorithm 
        /// </summary>
        public class HuffmanTree
        {

            private static readonly byte[] header = { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };
            private static readonly byte[] footer = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            /// <summary>
            /// Root of the tree
            /// </summary>
            internal HuffmanNode Root { get; set; }
            /// <summary>
            /// Holds all bit representations of paths indexed by the symbol, 
            /// e.g Paths[97] = bit representation of a
            /// </summary>
            public (BitArray bitArray, int bitsOccupied)[] Paths { get; internal set; } = new (BitArray bitArray, int bitsOccupied)[256];

            /// <summary>
            /// sets the root of a tree
            /// </summary>
            /// <param name="node1">node to become a root node</param>
            internal void SetRoot(HuffmanNode node1)
            {

                this.Root = node1;
            }

            public override string ToString()
            {
                if (Root == null) return null;
                var r = Root.ToString();
                return r[0..^1];
            }

            /// <summary>
            /// Traverses the tree in prefix order
            /// </summary>
            /// <param name="node"></param>
            /// <returns>IEnumerable that yields nodes in prefix order</returns>
            private IEnumerable<INode> PrefixTraverse(INode node)
            {
                if (node != null)
                {
                    yield return node;
                    foreach (var child in PrefixTraverse(node.Left)) yield return child;
                    foreach (var child in PrefixTraverse(node.Right)) yield return child;
                }
            }

            /// <summary>
            /// Gets leaves of the tree
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            private IEnumerable<INode> GetLeaves(INode RootNode)
            {
                var nodes = PrefixTraverse(RootNode);
                foreach (var node in nodes)
                {
                    if (node.Parent != null)
                        node.PathLength = node.Parent.PathLength + 1;

                    if (node.IsLeaf) yield return node;
                }
            }

            /// <summary>
            /// Generates paths for each ascii character and saves it as BitArray into Paths property.
            /// </summary>
            internal void GeneratePaths()
            {
                EvaluateAllLeaves(Root, -1, true);
                foreach (var node in GetLeaves(Root))
                    Paths[node.Value] = (node.Path, node.PathLength);
            }

            /// <summary>
            /// Evaluets path for every node in tree
            /// </summary>
            private void EvaluateAllLeaves(INode node, int depth, bool IsRightChild)
            {
                if (node != null)
                {
                    if (depth + 1 != 0)
                    {
                        node.Path[depth] = IsRightChild;
                    }
                    byte[] path = new byte[node.Path.Length/8];

                    node.Path.CopyTo(path, 0);
                    if (node.Left != null)
                        node.Left.Path = new BitArray(path);
                    if (node.Right != null)
                        node.Right.Path = new BitArray(path);

                    EvaluateAllLeaves(node.Left, depth + 1, false);
                    EvaluateAllLeaves(node.Right, depth + 1, true);
                }
            }


            /// <summary>
            /// Encodes just the tree using header, footer and node EncodeRepresentation
            /// </summary>
            /// <param name="encoded">string containing bytes after encoding</param>
            public void GetTreeEncoded(out MemoryStream encoded)
            {
                GetTreeEncoded(out byte[] bytes);
                encoded = new MemoryStream(bytes);
            }

            /// <summary>
            /// Encodes just the tree using header, footer and node EncodeRepresentation
            /// </summary>
            /// <param name="encoded">byte array containing bytes after encoding</param>
            public void GetTreeEncoded(out byte[] encoded)
            {

                List<byte> enc = new List<byte>();
                for (int i = 0; i < header.Length; i++)
                {
                    enc.Add(header[i]);
                }
                foreach (var node in PrefixTraverse(Root))
                {
                    var nodeRepr = node.GetEncodeRepresentation();

                    for (int i = 0; i < nodeRepr.Length; i++)
                        enc.Add(nodeRepr[i]);
                }

                for (int i = 0; i < footer.Length; i++)
                {
                    enc.Add(footer[i]);
                }

                encoded = enc.ToArray();
            }
        }
    }
    /// <summary>
    /// Specific implementation of Writer used in Huffman encoding 
    /// </summary>
    public class HuffmanFileWriter : Writer
    {
        readonly BinaryWriter binaryWriter;
        readonly MemoryStream ms;
        long _position = 0;

        /// <summary>
        /// cached bytes before calling .Flush()
        /// </summary>
        public override long BytesCached { get => _position; }

        /// <summary>
        /// Creates an instance of HuffmanFileWriter that uses BinaryWriter, not MemoryStream.
        /// </summary>
        /// <param name="path">path to file</param>
        public HuffmanFileWriter(string path)
        {
            binaryWriter = new BinaryWriter(new FileStream(path, FileMode.Create));
        }
        /// <summary>
        /// Creates an instance of HuffmanFileWriter that uses MemoryStream, not BinaryWriter.
        /// </summary>
        /// <param name="toMemoryStream"></param>
        public HuffmanFileWriter(bool toMemoryStream)
        {
            if (toMemoryStream)
            {
                ms = new MemoryStream();
            }
        }
        /// <summary>
        /// Writes a byte to the underlaying stream.
        /// </summary>
        /// <param name="b">byte to be written</param>
        public void Write(byte b)
        {
            if (ms != null)
                ms.WriteByte(b);
            else
                this.binaryWriter.Write(b);

            _position++;

        }
        public override void Flush()
        {
            if (ms != null)
                ms.Flush();
            else
                this.binaryWriter.Flush();

            _position = 0;
        }
        #region to be implemented
        public override void Write(string s)
        {
            throw new NotImplementedException();
        }

        public override void Write(StringBuilder s)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string s)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(StringBuilder s)
        {
            throw new NotImplementedException();
        }
        #endregion
        public override void Write(BitArray bits)
        {
            int newSize = bits.Count / 8;
            byte[] a = new byte[newSize];
            bits.CopyTo(a, 0);

            if (ms != null)
                ms.Write(a);
            else
                binaryWriter.Write(a);

            _position += newSize;
        }
        /// <summary>
        /// Returns memoryStream if initialized with memorystream. else throws an exception.
        /// </summary>
        /// <returns>MemoryStream</returns>
        /// <exception cref="NullReferenceException"></exception>
        public MemoryStream GetMemoryStream()
        {
            if (ms == null) throw new NullReferenceException("MemoryStream not initialized!");
            return ms;
        }
        public override void Write(byte[] bytes)
        {
            foreach (var @byte in bytes)
            {
                Write(@byte);
            }
            _position+=bytes.Length;
        }

        public override void Dispose()
        {
            if (ms != null) ms.Dispose();
            else binaryWriter.Dispose();
        }
    }
}
