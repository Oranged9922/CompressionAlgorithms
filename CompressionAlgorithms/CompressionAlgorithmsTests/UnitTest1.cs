using CompressionAlgorithms.Common;
using CompressionAlgorithms.Huffman;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace CompressionAlgorithmsTests
{
    public class Test_TreeBuild
    {
        public class Own_simple_tests
        {
            [TestCase("aaabbc", "6 *97:3 3 *99:1 *98:2", TestName = "Input: aaabbc, tests whether it builds tree properly")]
            [TestCase("", null, TestName = "no bytes read, should return null")]
            [TestCase("a", "*97:1", TestName = "builds tree using one(two) nodes correctly")]
            [TestCase("ab", "2 *97:1 *98:1", TestName = "builds tree using two(three) nodes correctly")]
            [TestCase("abc", "3 *99:1 2 *97:1 *98:1", TestName = "builds tree using three(five) nodes correctly")]
            [TestCase("abcd", "4 2 *97:1 *98:1 2 *99:1 *100:1", TestName = "builds tree using four(seven) nodes correctly")]
            public void OwnTest(string @in, string @out)
            {

                var res = Huffman.BuildTree(new StreamReader(new MemoryStream(@in.Select(x => (byte)x).ToArray())));
                Assert.AreEqual(@out, res.ToString());
            }
        }
        public class Provided_tests
        {
            [TestCase("insOuts/h1/binary.in", "insOuts/h1/binary.out", TestName = "big file 10kbit binary")]
            [TestCase("insOuts/h1/simple.in", "insOuts/h1/simple.out", TestName = "small file simple")]
            [TestCase("insOuts/h1/simple2.in", "insOuts/h1/simple2.out", TestName = "small file simple2")]
            [TestCase("insOuts/h1/simple3.in", "insOuts/h1/simple3.out", TestName = "small file simple3")]
            [TestCase("insOuts/h1/simple4.in", "insOuts/h1/simple4.out", TestName = "small file simple4")]
            public void ProvidedTestBinary(string @in, string @out)
            {
                var res = Huffman.BuildTree(new Reader(@in));
                var shouldBe = File.ReadAllText(@out);
                Assert.AreEqual(shouldBe, res.ToString());
            }
        }
    }
    public class Test_BinaryEncoding
    {
        public class Own_simple_tests
        {

        }
        public class Provided_tests
        {
            [TestCase("insOuts/h2/binary.in", "insOuts/h2/binary.in.huff", TestName = "big file 10kbit binary")]
            [TestCase("insOuts/h2/simple.in", "insOuts/h2/simple.in.huff", TestName = "small file simple")]
            [TestCase("insOuts/h2/simple2.in", "insOuts/h2/simple2.in.huff", TestName = "small file simple2")]
            [TestCase("insOuts/h2/simple3.in", "insOuts/h2/simple3.in.huff", TestName = "small file simple3")]
            [TestCase("insOuts/h2/simple4.in", "insOuts/h2/simple4.in.huff", TestName = "small file simple4")]
            [TestCase("insOuts/h2/oneCharacterOnly.in", "insOuts/h2/oneCharacterOnly.in.huff", TestName = "n* one symbol")]
            [TestCase("insOuts/h2/htmlFile.in", "insOuts/h2/htmlFile.in.huff", TestName = "html file")]
            [TestCase("insOuts/h2/asd.txt", "insOuts/h2/asd.txt.huff", TestName = "really simple test")]
            [TestCase("insOuts/h2/test.txt", "insOuts/h2/test.txt.huff", TestName = "big html page")]
            public void ProvidedTests(string @in, string @out)
            {
                var r = new Reader(@in);
                var tree = Huffman.BuildTree(r);
                r.ResetPointer();
                var w = new HuffmanFileWriter(toMemoryStream: true);
                Huffman.EncodeBy(tree, r, w);
                var t = new BinaryReader(new FileStream(@out, FileMode.Open));
                var shouldBe = t.ReadBytes((int)t.BaseStream.Length);
                var res = w.GetMemoryStream();
                var result = res.ToArray();
                Assert.AreEqual(shouldBe, result);

            }
        }
    }
}

