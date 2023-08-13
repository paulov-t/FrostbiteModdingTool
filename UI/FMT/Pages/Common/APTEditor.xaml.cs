using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for APTEditor.xaml
    /// </summary>
    public partial class APTEditor : UserControl
    {
        public BrowserOfBIG ParentBrowserOfBIG { get; set; }

        public APT APT { get; set; }
        public APTEditor(BrowserOfBIG parentBrowser)
        {
            InitializeComponent();
            ParentBrowserOfBIG = parentBrowser;
            APT = new APT();
            APT.Read(new MemoryStream(parentBrowser.SelectedEntry.Data));
        }
    }

    public class APT
    {
        public uint HeaderID = 544501825u;

        public uint HeaderData = 1635017028u;

        public byte[] HeaderDataType;

        public ushort HeaderLength;

        private BinaryReader binreader;

        public string currentloadfile;

        public string currentname;

        public int AstType = 0;

        public List<AssetIndex> Assets;

        public List<ulong> ParamOffsets;

        public List<ModelControl> Model_Controls;

        public APT()
        {
            HeaderDataType = new byte[6];
            HeaderLength = 26;
            ParamOffsets = new List<ulong>();
            Assets = new List<AssetIndex>();
            Model_Controls = new List<ModelControl>();
        }

        public string ReadString()
        {
            ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
            List<byte> list = new List<byte>();
            byte b = binreader.ReadByte();
            if (b != 0)
            {
                list.Add(b);
            }
            return aSCIIEncoding.GetString(list.ToArray());
        }

        public bool ReadAssets()
        {
            bool flag = false;
            List<AssetIndex> list = new List<AssetIndex>();
            while (!flag)
            {
                AssetIndex assetIndex = new AssetIndex(binreader);
                list.Add(assetIndex);
                if (assetIndex.AssetType == 0 && binreader.BaseStream.Position == (long)list[0].AssetNameOffset)
                {
                    flag = true;
                }
                else if (assetIndex.AssetType == 1 && binreader.BaseStream.Position == (long)list[0].PathNameOffset)
                {
                    flag = true;
                }
            }
            if (flag)
            {
                foreach (AssetIndex item in list)
                {
                    item.ReadNames(binreader);
                }
                binreader.BaseStream.Position = list[list.Count - 1].assetendpos;
            }
            for (int i = 0; i < list.Count; i++)
            {
                Assets.Add(list[i]);
            }
            if (list[list.Count - 1].AssetType == 0 && (ulong)binreader.BaseStream.Position >= list[list.Count - 1].AssetNameOffset)
            {
                return false;
            }
            return true;
        }

        public bool Read(string filename)
        {
            currentname = System.IO.Path.GetFileName(filename);
            currentloadfile = filename;
            return Read(File.Open(filename, FileMode.Open));
        }

        public bool Read(Stream stream)
        {
            binreader = new BinaryReader(stream);
            uint num = binreader.ReadUInt32();
            uint num2 = binreader.ReadUInt32();
            if (num != HeaderID && num2 != HeaderData)
            {
                binreader.Close();
                return false;
            }
            HeaderDataType = binreader.ReadBytes(6);
            HeaderLength = binreader.ReadUInt16();
            Assets.Clear();
            bool flag = true;
            while (flag)
            {
                flag = ReadAssets();
            }
            ParamOffsets.Clear();
            long position = binreader.BaseStream.Position;
            ParamOffsets.Add(binreader.ReadUInt64());
            long num3 = ((long)ParamOffsets[0] - position) / 8;
            for (int i = 1; i < num3; i++)
            {
                ParamOffsets.Add(binreader.ReadUInt64());
            }
            Model_Controls.Clear();
            for (int i = 0; i < ParamOffsets.Count; i++)
            {
                ModelControl modelControl = new ModelControl();
                if (ParamOffsets[i] != 0)
                {
                    binreader.BaseStream.Position = (long)ParamOffsets[i];
                    modelControl = new ModelControl(binreader);
                    modelControl.Process(binreader);
                    modelControl.ReadVertices(binreader);
                }
                Model_Controls.Add(modelControl);
            }
            binreader.BaseStream.Close();
            return true;
        }
    }

    public class AssetIndex
    {
        public ulong PathNameOffset;

        public ulong AssetNameOffset;

        public ulong Entries;

        public ulong Length;

        public string PathName;

        public string AssetName;

        public int AssetType;

        public bool stop = false;

        public long endpos;

        public long assetendpos;

        public void AdvanceReader(BinaryReader binreader)
        {
            double a = Convert.ToDouble(binreader.BaseStream.Position) / 8.0;
            long num = Convert.ToInt64(Math.Ceiling(a)) * 8 - binreader.BaseStream.Position;
            binreader.BaseStream.Position += num;
        }

        public AssetIndex()
        {
            PathNameOffset = 0uL;
            AssetNameOffset = 0uL;
            Entries = 0uL;
            Length = 0uL;
            PathName = "";
            AssetName = "";
        }

        public AssetIndex(BinaryReader binreader)
        {
            endpos = binreader.BaseStream.Position;
            ulong num = binreader.ReadUInt64();
            ulong num2 = binreader.ReadUInt64();
            if (num2 < num)
            {
                AssetType = 0;
                AssetNameOffset = num;
                Entries = num2;
            }
            else
            {
                AssetType = 1;
                PathNameOffset = num;
                AssetNameOffset = num2;
                Entries = binreader.ReadUInt64();
                Length = binreader.ReadUInt64();
            }
            endpos = binreader.BaseStream.Position;
        }

        public void ReadNames(BinaryReader binreader)
        {
            if (PathNameOffset >= (ulong)endpos)
            {
                binreader.BaseStream.Position = (long)PathNameOffset;
                PathName = ReadString(binreader);
                AdvanceReader(binreader);
            }
            binreader.BaseStream.Position = (long)AssetNameOffset;
            AssetName = ReadString(binreader);
            AdvanceReader(binreader);
            assetendpos = binreader.BaseStream.Position;
        }

        public string ReadString(BinaryReader binreader)
        {
            ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
            List<byte> list = new List<byte>();
            bool flag = false;
            while (!flag)
            {
                byte b = binreader.ReadByte();
                if (b != 0)
                {
                    list.Add(b);
                }
                else
                {
                    flag = true;
                }
            }
            return aSCIIEncoding.GetString(list.ToArray());
        }
    }

    public class ModelControl
    {
        public ulong Type = 0uL;

        public ulong AssetID = 0uL;

        public ushort Line1Offset1 = 0;

        public ushort Line1Offset2 = 0;

        public ushort Line1Offset3 = 0;

        public ushort Line1Offset4 = 0;

        public ushort Line1Offset5 = 0;

        public ushort Line1Offset6 = 0;

        public ushort Line1Offset7 = 0;

        public ushort Line1Offset8 = 0;

        public ushort Line2Offset1 = 0;

        public ushort Line2Offset2 = 0;

        public ushort Line2Offset3 = 0;

        public ushort Line2Offset4 = 0;

        public ushort Line2Offset5 = 0;

        public ushort Line2Offset6 = 0;

        public ushort Line2Offset7 = 0;

        public ushort Line2Offset8 = 0;

        public ushort Line3Offset1 = 0;

        public ushort Line3Offset2 = 0;

        public ushort Line3Offset3 = 0;

        public ushort Line3Offset4 = 0;

        public ushort Line3Offset5 = 0;

        public ushort Line3Offset6 = 0;

        public ushort Line3Offset7 = 0;

        public ushort Line3Offset8 = 0;

        public ushort Line4Offset1 = 0;

        public ushort Line4Offset2 = 0;

        public ushort Line4Offset3 = 0;

        public ushort Line4Offset4 = 0;

        public ushort Line4Offset5 = 0;

        public ushort Line4Offset6 = 0;

        public ushort Line4Offset7 = 0;

        public ushort Line4Offset8 = 0;

        public ushort Line5Offset1 = 0;

        public ushort Line5Offset2 = 0;

        public ushort Line5Offset3 = 0;

        public ushort Line5Offset4 = 0;

        public ushort Line5Offset5 = 0;

        public ushort Line5Offset6 = 0;

        public ushort Line5Offset7 = 0;

        public ushort Line5Offset8 = 0;

        public ushort Line6Offset1 = 0;

        public ushort Line6Offset2 = 0;

        public ushort Line6Offset3 = 0;

        public ushort Line6Offset4 = 0;

        public ushort Line6Offset5 = 0;

        public ushort Line6Offset6 = 0;

        public ushort Line6Offset7 = 0;

        public ushort Line6Offset8 = 0;

        public ushort Line7Offset1 = 0;

        public ushort Line7Offset2 = 0;

        public ushort Line7Offset3 = 0;

        public ushort Line7Offset4 = 0;

        public ushort ModelIndexTableOffset = 0;

        public ulong VertexIndexTableEntries = 0uL;

        public ulong VertexIndexTableOffset = 0uL;

        public List<ulong> VertexOffsets;

        public List<ushort> AssetOffsets;

        public List<Vertex> Vertices;

        public List<string> Names;

        public ModelControl()
        {
            Type = 0uL;
            AssetID = 0uL;
            Line1Offset1 = 0;
            Line1Offset2 = 0;
            Line1Offset3 = 0;
            Line1Offset4 = 0;
            Line1Offset5 = 0;
            Line1Offset6 = 0;
            Line1Offset7 = 0;
            Line1Offset8 = 0;
            Line2Offset1 = 0;
            Line2Offset2 = 0;
            Line2Offset3 = 0;
            Line2Offset4 = 0;
            Line2Offset5 = 0;
            Line2Offset6 = 0;
            Line2Offset7 = 0;
            Line2Offset8 = 0;
            Line3Offset1 = 0;
            Line3Offset2 = 0;
            Line3Offset3 = 0;
            Line3Offset4 = 0;
            Line3Offset5 = 0;
            Line3Offset6 = 0;
            Line3Offset7 = 0;
            Line3Offset8 = 0;
            Line4Offset1 = 0;
            Line4Offset2 = 0;
            Line4Offset3 = 0;
            Line4Offset4 = 0;
            Line4Offset5 = 0;
            Line4Offset6 = 0;
            Line4Offset7 = 0;
            Line4Offset8 = 0;
            Line5Offset1 = 0;
            Line5Offset2 = 0;
            Line5Offset3 = 0;
            Line5Offset4 = 0;
            Line5Offset5 = 0;
            Line5Offset6 = 0;
            Line5Offset7 = 0;
            Line5Offset8 = 0;
            Line6Offset1 = 0;
            Line6Offset2 = 0;
            Line6Offset3 = 0;
            Line6Offset4 = 0;
            Line6Offset5 = 0;
            Line6Offset6 = 0;
            Line6Offset7 = 0;
            Line6Offset8 = 0;
            Line7Offset1 = 0;
            Line7Offset2 = 0;
            Line7Offset3 = 0;
            Line7Offset4 = 0;
            VertexOffsets = new List<ulong>();
            AssetOffsets = new List<ushort>();
            Names = new List<string>();
            List<Vertex> list = new List<Vertex>();
        }

        public ModelControl(BinaryReader binreader)
        {
            VertexOffsets = new List<ulong>();
            AssetOffsets = new List<ushort>();
            Names = new List<string>();
            Vertices = new List<Vertex>();
            Type = binreader.ReadUInt64();
            AssetID = binreader.ReadUInt64();
            Line1Offset1 = binreader.ReadUInt16();
            Line1Offset2 = binreader.ReadUInt16();
            Line1Offset3 = binreader.ReadUInt16();
            Line1Offset4 = binreader.ReadUInt16();
            Line1Offset5 = binreader.ReadUInt16();
            Line1Offset6 = binreader.ReadUInt16();
            Line1Offset7 = binreader.ReadUInt16();
            Line1Offset8 = binreader.ReadUInt16();
            if (Type != 7)
            {
                Line2Offset1 = binreader.ReadUInt16();
                Line2Offset2 = binreader.ReadUInt16();
                Line2Offset3 = binreader.ReadUInt16();
                Line2Offset4 = binreader.ReadUInt16();
                ModelIndexTableOffset = binreader.ReadUInt16();
                Line2Offset6 = binreader.ReadUInt16();
                Line2Offset7 = binreader.ReadUInt16();
                Line2Offset8 = binreader.ReadUInt16();
                if (Type == 9)
                {
                    AssetOffsets.Clear();
                    Line3Offset1 = binreader.ReadUInt16();
                    Line3Offset2 = binreader.ReadUInt16();
                    Line3Offset3 = binreader.ReadUInt16();
                    Line3Offset4 = binreader.ReadUInt16();
                    Line3Offset5 = binreader.ReadUInt16();
                    Line3Offset6 = binreader.ReadUInt16();
                    Line3Offset7 = binreader.ReadUInt16();
                    Line3Offset8 = binreader.ReadUInt16();
                    Line4Offset1 = binreader.ReadUInt16();
                    Line4Offset2 = binreader.ReadUInt16();
                    Line4Offset3 = binreader.ReadUInt16();
                    Line4Offset4 = binreader.ReadUInt16();
                    Line4Offset5 = binreader.ReadUInt16();
                    if (Line4Offset5 != 0)
                    {
                        AssetOffsets.Add(Line4Offset5);
                    }
                    Line4Offset6 = binreader.ReadUInt16();
                    Line4Offset7 = binreader.ReadUInt16();
                    if (Line4Offset7 != 0)
                    {
                        AssetOffsets.Add(Line4Offset7);
                    }
                    Line4Offset8 = binreader.ReadUInt16();
                    Line5Offset1 = binreader.ReadUInt16();
                    Line5Offset2 = binreader.ReadUInt16();
                    Line5Offset3 = binreader.ReadUInt16();
                    Line5Offset4 = binreader.ReadUInt16();
                    Line5Offset5 = binreader.ReadUInt16();
                    Line5Offset6 = binreader.ReadUInt16();
                    Line5Offset7 = binreader.ReadUInt16();
                    Line5Offset8 = binreader.ReadUInt16();
                    Line6Offset1 = binreader.ReadUInt16();
                    Line6Offset2 = binreader.ReadUInt16();
                    Line6Offset3 = binreader.ReadUInt16();
                    Line6Offset4 = binreader.ReadUInt16();
                    Line6Offset5 = binreader.ReadUInt16();
                    Line6Offset6 = binreader.ReadUInt16();
                    Line6Offset7 = binreader.ReadUInt16();
                    Line6Offset8 = binreader.ReadUInt16();
                }
            }
            Line7Offset1 = binreader.ReadUInt16();
            Line7Offset2 = binreader.ReadUInt16();
            Line7Offset3 = binreader.ReadUInt16();
            Line7Offset4 = binreader.ReadUInt16();
        }

        public void Process(BinaryReader binreader)
        {
            if (ModelIndexTableOffset != 0 && ModelIndexTableOffset < binreader.BaseStream.Length)
            {
                binreader.BaseStream.Position = ModelIndexTableOffset;
                VertexIndexTableEntries = binreader.ReadUInt64();
                VertexIndexTableOffset = binreader.ReadUInt64();
                binreader.BaseStream.Position = (long)VertexIndexTableOffset;
                VertexOffsets.Clear();
                for (int i = 0; i < (long)VertexIndexTableEntries; i++)
                {
                    VertexOffsets.Add(binreader.ReadUInt64());
                }
            }
        }

        public void ReadVertices(BinaryReader binreader)
        {
            Vertices.Clear();
            for (int i = 0; i < VertexOffsets.Count; i++)
            {
                binreader.BaseStream.Position = (long)VertexOffsets[i];
                Vertex vertex = new Vertex(binreader);
                vertex.Process(binreader);
                Vertices.Add(vertex);
            }
        }

        public string ReadString(BinaryReader binreader)
        {
            ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
            List<byte> list = new List<byte>();
            bool flag = false;
            while (!flag)
            {
                byte b = binreader.ReadByte();
                if (b != 0)
                {
                    list.Add(b);
                }
                else
                {
                    flag = true;
                }
            }
            return aSCIIEncoding.GetString(list.ToArray());
        }

    }

    public class Vertex
    {
        public ulong Type = 0uL;

        public uint Mask = 0u;

        public uint ID = 0u;

        public uint Line1Unknown0 = 0u;

        public float Line1Unknown1 = 0f;

        public float Line1Unknown2 = 0f;

        public float Line1Unknown3 = 0f;

        public float Line2Unknown0 = 0f;

        public float Line2Unknown1 = 0f;

        public float Line2Unknown2 = 0f;

        public float Line2Unknown3 = 0f;

        public float Line3Unknown0 = 0f;

        public float Line3Unknown1 = 0f;

        public uint Line3Unknown2 = 0u;

        public float Line3Unknown3 = 0f;

        public uint Line4Unknown0 = 0u;

        public uint Line4Unknown1 = 0u;

        public uint Line4Unknown2 = 0u;

        public uint Line4Unknown3 = 0u;

        public uint NameOffset = 0u;

        public string Name = "";

        public uint UnknownIndexOffset = 0u;

        public ulong UnknownCount = 0uL;

        public List<ulong> UnknownOffsets = new List<ulong>();

        public uint Unknown = 0u;

        public Vertex()
        {
            Type = 3uL;
            Mask = 0u;
            ID = 38u;
            Line1Unknown0 = 0u;
            Line1Unknown1 = 0f;
            Line1Unknown2 = 0f;
            Line1Unknown3 = 0f;
            Line2Unknown0 = 0f;
            Line2Unknown1 = 0f;
            Line2Unknown2 = 0f;
            Line2Unknown3 = 0f;
            Line3Unknown0 = 0f;
            Line3Unknown1 = 0f;
            Line3Unknown2 = 0u;
            Line3Unknown3 = 0f;
            Line4Unknown0 = 0u;
            Line4Unknown1 = 0u;
            Line4Unknown2 = 0u;
            Line4Unknown3 = 0u;
        }

        public Vertex(BinaryReader binreader)
        {
            Type = binreader.ReadUInt64();
            Mask = binreader.ReadUInt32();
            ID = binreader.ReadUInt32();
            if (Type != 5)
            {
                Line1Unknown0 = binreader.ReadUInt32();
                Line1Unknown1 = binreader.ReadSingle();
                Line1Unknown2 = binreader.ReadSingle();
                Line1Unknown3 = binreader.ReadSingle();
                Line2Unknown0 = binreader.ReadSingle();
                Line2Unknown1 = binreader.ReadSingle();
                Line2Unknown2 = binreader.ReadSingle();
                Line2Unknown3 = binreader.ReadSingle();
                Line3Unknown0 = binreader.ReadSingle();
                Line3Unknown1 = binreader.ReadSingle();
                Line3Unknown2 = binreader.ReadUInt32();
                Line3Unknown3 = binreader.ReadUInt32();
                Line4Unknown0 = binreader.ReadUInt32();
                Line4Unknown1 = binreader.ReadUInt32();
                Line4Unknown2 = binreader.ReadUInt32();
                Line4Unknown3 = binreader.ReadUInt32();
                NameOffset = Line3Unknown2;
                UnknownIndexOffset = Line4Unknown3;
            }
        }

        public string ReadString(BinaryReader binreader)
        {
            if (binreader.BaseStream.Position < binreader.BaseStream.Length)
            {
                ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
                List<byte> list = new List<byte>();
                bool flag = false;
                while (!flag)
                {
                    byte b = binreader.ReadByte();
                    if (b != 0)
                    {
                        list.Add(b);
                    }
                    else
                    {
                        flag = true;
                    }
                }
                return aSCIIEncoding.GetString(list.ToArray());
            }
            return string.Empty;
        }

        public void Process(BinaryReader binreader)
        {
            if (NameOffset != 0)
            {
                binreader.BaseStream.Position = NameOffset;
                Name = ReadString(binreader);
            }
            if (UnknownIndexOffset != 0)
            {
                binreader.BaseStream.Position = UnknownIndexOffset;
                if (binreader.BaseStream.Position < binreader.BaseStream.Length)
                {
                    UnknownCount = binreader.ReadUInt64();
                }
            }
        }
    }



}
