using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Encoders;

namespace EDDS_to_DDS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists(args[0]))
            {
                string ff = Path.GetExtension(args[0]);
                OpenFile(args[0]);
            }
        }
        static void OpenFile(string file)
        {
            List<int> copy_blocks = new List<int>();
            List<int> LZO_blocks = new List<int>();
            List<int> LZ4_blocks = new List<int>();
            List<byte> Decoded_blocks = new List<byte>();

            void FindBlocks(BinaryReader reader)
            {
                while (true)
                {
                    byte[] blocks = reader.ReadBytes(4);

                    char[] dd = Encoding.UTF8.GetChars(blocks);

                    string block = new string(dd);
                    int size = reader.ReadInt32();

                    switch (block)
                    {
                        case "COPY": copy_blocks.Add(size); break;
                        case "LZ4 ": LZ4_blocks.Add(size); break;
                        default: reader.BaseStream.Seek(-8, SeekOrigin.Current); return;
                    }
                }
            }

            using (var reader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                byte[] dds_header = reader.ReadBytes(128);
                FindBlocks(reader);

                foreach (int count in copy_blocks)
                {
                    byte[] buff = reader.ReadBytes(count);
                    Decoded_blocks.InsertRange(0, buff);
                }

                foreach (int Length in LZ4_blocks)
                {


                    uint size = reader.ReadUInt32();
                    byte[] target = new byte[size];

                    int num = 0;
                    LZ4ChainDecoder lz4ChainDecoder = new LZ4ChainDecoder(65536, 0);
                    int count1;
                    int idx = 0;
                    for (; num < Length - 4; num += (count1 + 4))
                    {
                        count1 = reader.ReadInt32() & int.MaxValue;
                        byte[] numArray = reader.ReadBytes(count1);
                        byte[] buffer = new byte[65536];
                        int count2 = 0;
                        LZ4EncoderExtensions.DecodeAndDrain((ILZ4Decoder)lz4ChainDecoder, numArray, 0, count1, buffer, 0, 65536, out count2);

                        Array.Copy(buffer, 0, target, idx, count2);

                        idx += count2;
                    }

                    Decoded_blocks.InsertRange(0, target);
                }

                Decoded_blocks.InsertRange(0, dds_header);
                byte[] final = Decoded_blocks.ToArray();

                using (var wr = File.Create(Path.GetFileNameWithoutExtension(file) +".dds"))
                {
                    wr.Write(final, 0, final.Length);
                }
            }

            
        }


    }
}
