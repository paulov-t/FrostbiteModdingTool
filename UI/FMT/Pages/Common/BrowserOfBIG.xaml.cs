using AvalonDock.Layout;
using CSharpImageLibrary;
using FIFAModdingUI.Pages.Common;
using FMT.FileTools;
using FrostySdk;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for BrowserOfBIG.xaml
    /// </summary>
    public partial class BrowserOfBIG : UserControl
    {
        public BrowserOfBIG()
        {
            InitializeComponent();
        }


        public class BigFileEntry
        {
            public string DisplayName => Name + " (" + Type + ")";

            public string Name { get; set; }

            public byte[] Hash { get; set; }

            public uint Offset { get; set; }

            private uint size;

            public uint Size
            {
                get { return ModifiedEntry != null ? (uint)ModifiedEntry.Data.Length : size; }
                set { size = value; }
            }


            private byte[] data;

            public byte[] Data
            {
                get { return ModifiedEntry != null ? ModifiedEntry.Data : data; }
                set { data = value; }
            }


            public string Type { get; set; }

            public bool IsModified => ModifiedEntry != null;

            public class ModifiedBigFileEntry
            {
                public byte[] Data { get; set; }

                public int Size => Data.Length;

                public ModifiedBigFileEntry(byte[] inData)
                {
                    Data = inData;
                }
            }

            public ModifiedBigFileEntry ModifiedEntry { get; set; }

            public BigFileEntry(string inName, uint inOffset, uint inSize, byte[] inData, string inType = "MISC")
            {
                Name = inName;
                Offset = inOffset;
                Size = inSize;
                Data = inData;
                Type = inType;
            }
        }

        public AssetEntry AssetEntry;

        public BigFileEntry SelectedEntry => (BigFileEntry)lb.SelectedItem;

        public string SelectedEntryText { get; set; }
        public Browser ParentBrowser { get; internal set; }

        public void LoadBig()
        {
            bigBrowserDocumentsPane.Children.Clear();

            using FileReader nativeReader = new FileReader(AssetManager.Instance.GetCustomAsset("legacy", AssetEntry));
            uint num = nativeReader.ReadUInt32LittleEndian();
            bool compressedFlag = num == 877087042;
            if (num != 1179076930 && num != 877087042)
            {
                return;
            }
            nativeReader.ReadUInt32LittleEndian();
            uint num2 = nativeReader.ReadUInt32BigEndian();
            nativeReader.ReadUInt32BigEndian();
            List<BigFileEntry> list = new List<BigFileEntry>();
            string inType = "DAT";
            for (uint num3 = 0u; num3 < num2; num3++)
            {
                uint offset = nativeReader.ReadUInt32BigEndian();
                uint size = nativeReader.ReadUInt32BigEndian();
                string text = nativeReader.ReadNullTerminatedString();
                if (size == 0)
                {
                    if (text == "sg1")
                    {
                        inType = "DDS";
                    }
                    else if (text == "sg2")
                    {
                        inType = "APT";
                    }
                    continue;
                }
                long position = nativeReader.Position;
                nativeReader.Position = offset;
                //byte[] data = (compressedFlag ? ArrayPool<byte>.Shared.Rent((int)size) : new byte[size]);
                byte[] data = new byte[size];
                byte[] decompressedData;
                try
                {
                    data = nativeReader.ReadBytes((int)size);
                    decompressedData = (compressedFlag ? DecompressEAHD(data, (int)size) : data);
                }
                finally
                {
                }
                nativeReader.Position = position;
                list.Add(new BigFileEntry(text, offset, size, decompressedData, inType));
            }
            lb.ItemsSource = list;
        }

        public void LoadNonBig()
        {
            bigBrowserDocumentsPane.Children.Clear();


            using FileReader reader = new FileReader(AssetManager.Instance.GetCustomAsset("legacy", AssetEntry));
            if (reader.ReadUInt32LittleEndian() != 1095124802)
            {
                return;
            }
            reader.ReadUInt32LittleEndian();
            reader.ReadUInt32LittleEndian();
            uint num6 = reader.ReadUInt32LittleEndian();
            reader.ReadUInt32LittleEndian();
            reader.ReadUInt32LittleEndian();
            reader.ReadUInt32LittleEndian();
            reader.ReadUInt32LittleEndian();
            byte[] array2 = reader.ReadBytes(6);
            reader.ReadUInt16LittleEndian();
            uint num7 = reader.ReadUInt32LittleEndian();
            reader.Position += 20L;
            List<uint> fileTypes = new List<uint>();
            for (uint num8 = 0u; num8 < num7; num8++)
            {
                fileTypes.Add(reader.ReadUInt32LittleEndian());
            }
            List<BigFileEntry> list3 = new List<BigFileEntry>();
            for (uint num9 = 0u; num9 < num6; num9++)
            {
                uint num10 = ReadVarInt(reader, array2[0]);
                int index = (int)ReadVarInt(reader, array2[1]);
                byte[] hash = reader.ReadBytes(array2[2]);
                uint offset = ReadVarInt(reader, array2[3]) * 8;
                uint size = ReadVarInt(reader, array2[4]);
                uint decompressedSize = ReadVarInt(reader, array2[5]) + size;
                long position = reader.Position;
                reader.Position = offset;
                byte[] data = reader.ReadBytes((int)size);
                if (num10 != 0)
                {
                    data = Utils.DecompressZLib(data, (int)decompressedSize);
                }
                reader.Position = position;
                List<BigFileEntry> list4 = list3;
                string inName = BitConverter.ToString(hash);
                uint inOffset = offset;
                uint inSize = size;
                byte[] inData = data;
                BigFileEntry bigFileEntry = new BigFileEntry(inName, inOffset, inSize, inData, fileTypes[index] switch
                {
                    642327507u => "DDS",
                    642327506u => "UNK",
                    642327510u => "APT",
                    642327509u => "APTD",
                    2414196441u => "DAT",
                    _ => "DAT",
                });
                bigFileEntry.Hash = hash;
                list4.Add(bigFileEntry);
            }
            list3.Sort((BigFileEntry a, BigFileEntry b) => a.Offset.CompareTo(b.Offset));
            lb.ItemsSource = list3;
        }

        private byte[] DecompressEAHD(byte[] buffer, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            using FileReader reader = new FileReader(new MemoryStream(buffer, 0, count));
            if (reader.ReadUInt16LittleEndian() != 64272)
            {
                return buffer;
            }
            byte[] decompressedData = new byte[ReadVarInt(reader, 3, Endian.Big)];
            int num = 0;
            while (reader.Position < reader.Length)
            {
                byte b = reader.ReadByte();
                int num3 = 0;
                int num4 = 0;
                int num5 = 0;
                if (b < 128)
                {
                    int num6 = reader.ReadByte();
                    num3 = b & 3;
                    num4 = ((b & 0x1C) >> 2) + 3;
                    num5 = ((b & 0x60) << 3) + num6 + 1;
                }
                else if (b < 192)
                {
                    byte num10 = reader.ReadByte();
                    int num7 = reader.ReadByte();
                    num3 = ((num10 & 0xC0) >> 6) & 3;
                    num4 = (b & 0x3F) + 4;
                    num5 = ((num10 & 0x3F) << 8) + num7 + 1;
                }
                else if (b >= 224)
                {
                    num3 = ((b >= 252) ? (b & 3) : (((b & 0x1F) << 2) + 4));
                }
                else
                {
                    int num8 = reader.ReadByte();
                    int num9 = reader.ReadByte();
                    int num2 = reader.ReadByte();
                    num3 = b & 3;
                    num4 = ((b & 0xC) << 6) + num2 + 5;
                    num5 = ((b & 0x10) << 12) + (num8 << 8) + num9 + 1;
                }
                for (int i = 0; i < num3; i++)
                {
                    decompressedData[num++] = reader.ReadByte();
                }
                num5 = num - num5;
                for (int j = 0; j < num4; j++)
                {
                    decompressedData[num++] = decompressedData[num5 + j];
                }
            }
            return decompressedData;
        }

        private uint ReadVarInt(FileReader reader, int size, Endian endian = Endian.Little)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size", size, "Size cannot be negative.");
            }
            var buffer = reader.ReadBytes(size);
            uint num = 0u;
            if (endian == Endian.Little)
            {
                for (int i = 0; i < size; i++)
                {
                    num |= (uint)(buffer[i] << i * 8);
                }
            }
            else
            {
                for (int j = 0; j < size; j++)
                {
                    num |= (uint)(buffer[j] << (size - j - 1) * 8);
                }
            }
            return num;
        }

        private void WriteVarInt(FileWriter writer, int size, uint value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size", size, "Size cannot be negative.");
            }
            Span<byte> buffer = stackalloc byte[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = (byte)(value >> i * 8);
            }
            writer.WriteBytes(buffer.ToArray());
        }

        //private async void ExportAll_Click(object sender, RoutedEventArgs e)
        //{

        //}

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            await ExportAsync().ConfigureAwait(continueOnCapturedContext: true);
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            await ImportAsync().ConfigureAwait(continueOnCapturedContext: true);
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            BigFileEntry entry = lb.SelectedItem as BigFileEntry;
            if (entry == null)
            {
                return;
            }

            entry.ModifiedEntry = null;
            AssetManager.Instance.Logger.Log($"{entry.Name} has been reverted");

            bigBrowserDocumentsPane.Children.Clear();

        }

        private async Task ExportAsync(BigFileEntry entry, string file)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            string directory = System.IO.Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string extension = System.IO.Path.GetExtension(file);
            if (entry.Type == "DDS")
            {
                if (extension.Equals(".DDS", StringComparison.OrdinalIgnoreCase))
                {
                    await File.WriteAllBytesAsync(file, entry.Data).ConfigureAwait(continueOnCapturedContext: false);
                }
                else
                {
                    ImageEngineImage originalImage = new ImageEngineImage(entry.Data);

                    var imageBytes = originalImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.PNG)
                        , MipHandling.KeepTopOnly
                    , removeAlpha: false);

                    await File.WriteAllBytesAsync(file, imageBytes);
                }
            }
            else
            {
                await File.WriteAllBytesAsync(file, entry.Data).ConfigureAwait(continueOnCapturedContext: false);
            }
            AssetManager.Instance.Logger.Log($"{entry.Name} has been exported to {file}");
        }

        private async Task ExportAsync(IEnumerable<BigFileEntry> entries, string path, bool hierarchicalExport)
        {
            if (entries == null)
            {
                throw new ArgumentNullException("entries");
            }
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            foreach (BigFileEntry entry in entries)
            {
                string entryName = (hierarchicalExport ? entry.Name : entry.Name.Replace('\\', '_').Replace('/', '_'));
                string file = System.IO.Path.Combine(path, entryName + "." + entry.Type);
                await ExportAsync(entry, file).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private async Task ImportAsync(BigFileEntry entry, string file)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            string extension = System.IO.Path.GetExtension(file)!.ToLowerInvariant();
            byte[] data = await File.ReadAllBytesAsync(file);
            if (entry.Type == "DDS")
            {
                await Task.Run(() =>
                {
                    if (extension != ".dds")
                    {
                        ImageEngineImage originalImage = new ImageEngineImage(entry.Data);

                        ImageEngineImage imageEngineImage = new ImageEngineImage(file);

                        //                  TextureUtils.TextureImportOptions options = default(TextureUtils.TextureImportOptions);
                        //options.type = TextureType.TT_2d;
                        //                  options.format = TextureUtils.ToShaderFormat("BC3A_UNORM", false);
                        //options.generateMipmaps = false;
                        //                  options.mipmapsFilter = 0;
                        //                  options.resizeTexture = true;
                        //                  options.resizeFilter = 0;
                        //options.resizeHeight = originalImage.Height;
                        //                  options.resizeWidth = originalImage.Width;

                        //TextureUtils.BlobData pOutData = default(TextureUtils.BlobData);
                        //                  TextureUtils.ConvertImageToDDS(data, data.Length, TextureUtils.ImageFormat.PNG, options, ref pOutData);

                        //data = pOutData.Data;
                        data = imageEngineImage.Save(
                            new ImageFormats.ImageEngineFormatDetails(
                                originalImage.Format
                                , CSharpImageLibrary.Headers.DDS_Header.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB)
                            , MipHandling.Default
                            , removeAlpha: false);
                    }
                });
            }

            //entry.Data = data;
            //entry.Size = (uint)data.Length;
            //entry.IsModified = true;

            entry.ModifiedEntry = new BigFileEntry.ModifiedBigFileEntry(data);
            AssetManager.Instance.Logger.Log($"{file} has been imported into {entry.Name}");
            Reconstruct(AssetEntry);

            bigBrowserDocumentsPane.Children.Clear();

        }

        private void Reconstruct(AssetEntry assetEntry)
        {
            if (assetEntry == null)
            {
                throw new ArgumentNullException("assetEntry");
            }
            List<BigFileEntry> entries = (List<BigFileEntry>)lb.ItemsSource;
            if (assetEntry.Type == "BIG")
            {
                ReconstructBIG(assetEntry, entries);
            }
            else
            {
                ReconstructAST(assetEntry, entries);
            }
        }

        private void ReconstructBIG(AssetEntry assetEntry, IEnumerable<BigFileEntry> entries)
        {
            if (assetEntry == null)
            {
                throw new ArgumentNullException("assetEntry");
            }
            if (entries == null)
            {
                throw new ArgumentNullException("entries");
            }
            MemoryStream memoryStream = new MemoryStream();
            List<uint> list = new List<uint>();
            uint num = 0u;
            using FileWriter nativeWriter = new FileWriter(memoryStream);
            string b = "DAT";
            foreach (BigFileEntry item in entries)
            {
                list.Add((uint)nativeWriter.BaseStream.Position);
                nativeWriter.WriteBytes(item.Data);
                while (nativeWriter.BaseStream.Position % 64 != 0L)
                {
                    nativeWriter.BaseStream.Position++;
                }
                num += (uint)(8 + item.Name.Length + 1);
                if (item.Type != b)
                {
                    b = item.Type;
                    num += 12;
                }
            }
            byte[] array = memoryStream.ToArray();
            using FileReader nativeReader = new FileReader(AssetManager.Instance.GetCustomAsset("legacy", assetEntry));
            using FileWriter nativeWriter2 = new FileWriter(new MemoryStream());
            uint value = nativeReader.ReadUInt32LittleEndian();
            uint num2 = nativeReader.ReadUInt32LittleEndian();
            uint value2 = nativeReader.ReadUInt32BigEndian();
            nativeReader.ReadUInt32BigEndian();
            for (num2 = num + 8; (num2 + 16) % 64u != 0; num2++)
            {
            }
            uint num3 = num2 + 16;
            num2 += (uint)array.Length;
            nativeWriter2.WriteUInt32LittleEndian(value);
            nativeWriter2.WriteUInt32LittleEndian(num2);
            nativeWriter2.WriteUInt32BigEndian(value2);
            nativeWriter2.WriteUInt32BigEndian(num + 16 + 8);
            int index = 0;
            string text = "DAT";
            foreach (BigFileEntry item2 in lb.Items)
            {
                if (item2.Type != text)
                {
                    text = item2.Type;
                    string str = "";
                    if (text == "DDS")
                    {
                        str = "sg1";
                    }
                    else if (text == "APT")
                    {
                        str = "sg2";
                    }
                    nativeWriter2.WriteUInt32BigEndian(list[index] + num3);
                    nativeWriter2.WriteInt32LittleEndian(0);
                    nativeWriter2.WriteNullTerminatedString(str);
                }
                nativeWriter2.WriteUInt32BigEndian(list[index++] + num3);
                nativeWriter2.WriteInt32BigEndian(item2.Data.Length);
                nativeWriter2.WriteNullTerminatedString(item2.Name);
            }
            nativeWriter2.WriteBytes(new byte[8] { 76, 50, 56, 54, 21, 5, 0, 0 });
            nativeWriter2.WritePadding(64);
            nativeWriter2.WriteBytes(array);
            AssetManager.Instance.ModifyLegacyAsset(assetEntry.Name, ((MemoryStream)nativeWriter2.BaseStream).ToArray(), false);
            AssetManager.Instance.Logger.Log($"{AssetEntry.Name} has been Reconstructed");

        }

        private void ReconstructAST(AssetEntry assetEntry, IEnumerable<BigFileEntry> entries)
        {
            if (assetEntry == null)
            {
                throw new ArgumentNullException("assetEntry");
            }
            if (entries == null)
            {
                throw new ArgumentNullException("entries");
            }
            MemoryStream memoryStream = new MemoryStream();
            List<uint> list = new List<uint>();
            using (FileWriter nativeWriter = new FileWriter(memoryStream))
            {
                foreach (BigFileEntry item2 in entries)
                {
                    list.Add((uint)nativeWriter.BaseStream.Position);
                    nativeWriter.WriteBytes(item2.Data);
                    while (nativeWriter.BaseStream.Position % 8 != 0L)
                    {
                        nativeWriter.BaseStream.Position++;
                    }
                }
            }
            using FileReader nativeReader = new FileReader(AssetManager.Instance.GetCustomAsset("legacy", assetEntry));
            using FileWriter nativeWriter2 = new FileWriter(new MemoryStream());
            uint value = nativeReader.ReadUInt32LittleEndian();
            uint value2 = nativeReader.ReadUInt32LittleEndian();
            nativeReader.ReadUInt32LittleEndian();
            nativeReader.ReadUInt32LittleEndian();
            uint num = nativeReader.ReadUInt32LittleEndian();
            uint value3 = nativeReader.ReadUInt32LittleEndian();
            uint num2 = nativeReader.ReadUInt32LittleEndian();
            uint value4 = nativeReader.ReadUInt32LittleEndian();
            byte[] array = nativeReader.ReadBytes(6);
            ushort value5 = nativeReader.ReadUInt16LittleEndian();
            uint num3 = nativeReader.ReadUInt32LittleEndian();
            nativeReader.Position += 20L;
            List<uint> list2 = new List<uint>();
            for (uint num4 = 0u; num4 < num3; num4++)
            {
                list2.Add(nativeReader.ReadUInt32LittleEndian());
            }
            array[3] = 4;
            array[4] = 4;
            array[5] = 4;
            num2 = (uint)((array[0] + array[1] + array[2] + array[3] + array[4] + array[5]) * lb.Items.Count + num3 * 4);
            nativeWriter2.WriteUInt32LittleEndian(value);
            nativeWriter2.WriteUInt32LittleEndian(value2);
            nativeWriter2.WriteUInt32LittleEndian((uint)(lb.Items.Count + 1));
            nativeWriter2.WriteUInt32LittleEndian((uint)lb.Items.Count);
            nativeWriter2.WriteUInt32LittleEndian(num);
            nativeWriter2.WriteUInt32LittleEndian(value3);
            nativeWriter2.WriteUInt32LittleEndian(num2);
            nativeWriter2.WriteUInt32LittleEndian(value4);
            nativeWriter2.WriteBytes(array);
            nativeWriter2.WriteUInt16LittleEndian(value5);
            nativeWriter2.WriteUInt32LittleEndian(num3);
            for (int i = 0; i < 5; i++)
            {
                nativeWriter2.WriteUInt32LittleEndian(0u);
            }
            for (int j = 0; j < num3; j++)
            {
                nativeWriter2.WriteUInt32LittleEndian(list2[j]);
            }
            int num5 = 0;
            uint num6;
            for (num6 = num + num2; num6 % 8u != 0; num6++)
            {
            }
            foreach (BigFileEntry item3 in lb.Items)
            {
                uint item = 0u;
                switch (item3.Type)
                {
                    case "DDS":
                        item = 642327507u;
                        break;
                    case "UNK":
                        item = 642327506u;
                        break;
                    case "APT":
                        item = 642327510u;
                        break;
                    case "APTD":
                        item = 642327509u;
                        break;
                    case "DAT":
                        item = 2414196441u;
                        break;
                }
                WriteVarInt(nativeWriter2, array[0], 0u);
                WriteVarInt(nativeWriter2, array[1], (uint)list2.IndexOf(item));
                nativeWriter2.WriteBytes(item3.Hash);
                WriteVarInt(nativeWriter2, array[3], (list[num5++] + num6) / 8u);
                WriteVarInt(nativeWriter2, array[4], (uint)item3.Data.Length);
                WriteVarInt(nativeWriter2, array[5], 0u);
            }
            while (nativeWriter2.BaseStream.Position < num6)
            {
                nativeWriter2.Write((byte)0);
            }
            nativeWriter2.WriteBytes(memoryStream.ToArray());
            AssetManager.Instance.ModifyLegacyAsset(assetEntry.Name, ((MemoryStream)nativeWriter2.BaseStream).ToArray(), false);
        }

        private void UpdateSelectedEntryDataAsText()
        {
            if (SelectedEntry == null || SelectedEntry.Data == null)
            {
                SelectedEntryText = null;
                return;
            }
            string name = SelectedEntry.Name;
            byte[] data = SelectedEntry.Data;
            if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) || (data[0] == 60 && data[1] == 83 && data[2] == 67))
            {
                SelectedEntryText = Encoding.UTF8.GetString(data);
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            string text = "";
            string text2 = "";
            for (int i = 0; i < data.Length; i++)
            {
                if (i != 0 && i % 16 == 0)
                {
                    stringBuilder.Append(text).AppendLine(text2);
                    text = "";
                    text2 = "";
                }
                text = text + data[i].ToString("X2") + " ";
                text2 += (char)((char.IsLetterOrDigit((char)data[i]) || char.IsSymbol((char)data[i])) ? data[i] : 46);
            }
            stringBuilder.Append(text.PadRight(48)).AppendLine(text2);
            SelectedEntryText = stringBuilder.ToString();
        }

        private async Task ExportAsync()
        {
            object selectedItem = lb.SelectedItem;
            BigFileEntry entry = selectedItem as BigFileEntry;
            if (entry == null)
            {
                return;
            }
            string filter = entry.Type + " (*." + entry.Type.ToLower() + ")|*." + entry.Type;
            if (entry.Type == "DDS")
            {
                filter = "PNG (*.png)|*.png|" + filter;
            }
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = entry.Name.Replace('\\', '_').Replace('/', '_'),
                AddExtension = true,
                Title = "Export File"
            };
            if (dialog.ShowDialog() == true)
            {
                string filename = dialog.FileName;
                string extension = System.IO.Path.GetExtension(filename);
                string filterExtension = filter.Split('|')[dialog.FilterIndex * 2 - 1];
                if (string.IsNullOrEmpty(extension) || (filterExtension.Equals("*.DDS", StringComparison.OrdinalIgnoreCase) && !extension.Equals("." + entry.Type, StringComparison.Ordinal)))
                {
                    filename = filename + "." + entry.Type;
                }

                await ExportAsync(entry, filename);
            }
        }

        private async Task ImportAsync()
        {
            BigFileEntry entry = lb.SelectedItem as BigFileEntry;
            if (entry == null)
            {
                return;
            }
            string filter = entry.Type + " (*." + entry.Type.ToLower() + ")|*." + entry.Type;
            if (entry.Type == "DDS")
            {
                filter = "All Images|*.png;*.dds";
            }
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter,
                FileName = entry.Name.Replace('\\', '_').Replace('/', '_'),
                Title = "Import File"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await ImportAsync(entry, dialog.FileName);
                }
                catch (InvalidDataException)
                {
                }
            }

            //if(ParentBrowser != null)
            //         {
            //	ParentBrowser.UpdateAssetListView();
            //         }
        }

        private void lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb.SelectedItem != null)
            {
                //var bigFileEntry = lb.SelectedItem as BigFileEntry;

                // ---- ----------
                // stop duplicates
                bigBrowserDocumentsPane.AllowDuplicateContent = false;
                foreach (var child in bigBrowserDocumentsPane.Children)
                {
                    if (child.Title == SelectedEntry.DisplayName)
                    {
                        bigBrowserDocumentsPane.SelectedContentIndex = bigBrowserDocumentsPane.Children.IndexOf(child);
                        return;
                    }
                }
                // ---- ----------

                var newLayoutDoc = new LayoutDocument();
                newLayoutDoc.Title = SelectedEntry.DisplayName;
                if (SelectedEntry.Type == "DDS")
                {
                    System.Windows.Controls.Image imageEditor = new System.Windows.Controls.Image();
                    ImageEngineImage imageEngineImage = new ImageEngineImage(SelectedEntry.Data);
                    var iData = imageEngineImage.Save(new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.BMP), MipHandling.KeepTopOnly, removeAlpha: false);
                    imageEditor.Source = LoadImage(iData);
                    newLayoutDoc.Content = imageEditor;
                }
                //else if (SelectedEntry.Type == "APT")
                //{
                //	APTEditor aptEditor = new APTEditor(this);
                //	newLayoutDoc.Content = aptEditor;
                //}
                else
                {
                    WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
                    hexEditor.Stream = new MemoryStream(SelectedEntry.Data);
                    newLayoutDoc.Content = hexEditor;
                    hexEditor.BytesModified += HexEditor_BytesModified;

                    //APT aptTest = new APT();
                    //aptTest.Read(new MemoryStream(SelectedEntry.Data));
                }
                bigBrowserDocumentsPane.Children.Insert(0, newLayoutDoc);
                bigBrowserDocumentsPane.SelectedContentIndex = 0;


            }
        }

        private void HexEditor_BytesModified(object sender, WpfHexaEditor.Core.EventArguments.ByteEventArgs e)
        {
            var bigFileEntry = lb.SelectedItem as BigFileEntry;

            var hexEditor = sender as WpfHexaEditor.HexEditor;
            if (hexEditor != null)
            {
                //bigFileEntry.Data = hexEditor.GetAllBytes(true);
                //bigFileEntry.Size = (uint)bigFileEntry.Data.Length;
                //bigFileEntry.IsModified = true;
                bigFileEntry.ModifiedEntry = new BigFileEntry.ModifiedBigFileEntry(hexEditor.GetAllBytes(true));
                Reconstruct(AssetEntry);
                //if (ParentBrowser != null)
                //{
                //	ParentBrowser.UpdateAssetListView();
                //}
            }
        }

        private static System.Windows.Media.Imaging.BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new System.Windows.Media.Imaging.BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
}
