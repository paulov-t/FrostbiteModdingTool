/// Full Credits of this go to FrostyEditor by CadeEvs
///

using FMT.FileTools;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace FMT.Sound
{
    public static class EALayer3
    {
        public delegate void AudioCallback([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] short[] data, int count, StreamInfo info);

        [StructLayout(LayoutKind.Sequential)]
        public struct StreamInfo
        {
            public int streamIndex;
            public int numChannels;
            public int sampleRate;
        }

        [DllImport("../thirdparty/ealayer3.dll", EntryPoint = "Decode")]
        public static extern void Decode([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] buffer, int length, AudioCallback callback);
    }

    public static class Pcm16b
    {
        public static short[] Decode(byte[] soundBuffer)
        {
            using (NativeReader reader = new NativeReader(new MemoryStream(soundBuffer)))
            {
                ushort blockType = reader.ReadUShort();
                ushort blockSize = reader.ReadUShort(Endian.Big);
                byte compressionType = reader.ReadByte();

                int channelCount = (reader.ReadByte() >> 2) + 1;
                ushort sampleRate = reader.ReadUShort(Endian.Big);
                int totalSampleCount = reader.ReadInt(Endian.Big) & 0x00ffffff;

                List<short>[] channels = new List<short>[channelCount];
                for (int i = 0; i < channelCount; i++)
                    channels[i] = new List<short>();

                while (reader.Position <= reader.Length)
                {
                    blockType = reader.ReadUShort();
                    blockSize = reader.ReadUShort(Endian.Big);

                    if (blockType == 0x45)
                        break;

                    uint samples = reader.ReadUInt(Endian.Big);

                    for (int i = 0; i < samples; i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                            channels[j].Add(reader.ReadShort(Endian.Big));
                    }
                }

                short[] outBuffer = new short[channels[0].Count * channelCount];
                for (int i = 0; i < channels[0].Count; i++)
                {
                    for (int j = 0; j < channelCount; j++)
                    {
                        outBuffer[(i * channelCount) + j] = channels[j][i];
                    }
                }

                return outBuffer;
            }
        }
    }
    public static class XAS
    {
        public static short[] Decode(byte[] soundBuffer)
        {
            using (NativeReader reader = new NativeReader(new MemoryStream(soundBuffer)))
            {
                ushort blockType = reader.ReadUShort();
                ushort blockSize = reader.ReadUShort(Endian.Big);
                byte compressionType = reader.ReadByte();

                int channelCount = (reader.ReadByte() >> 2) + 1;
                ushort sampleRate = reader.ReadUShort(Endian.Big);
                int totalSampleCount = reader.ReadInt(Endian.Big) & 0x00ffffff;

                List<short>[] channels = new List<short>[channelCount];
                for (int i = 0; i < channelCount; i++)
                    channels[i] = new List<short>();

                while (reader.Position <= reader.Length)
                {
                    blockType = reader.ReadUShort();
                    blockSize = reader.ReadUShort(Endian.Big);

                    if (blockType == 0x45)
                        break;

                    uint samples = reader.ReadUInt(Endian.Big);

                    byte[] buffer = null;
                    short[] blockBuffer = new short[32];
                    int[] consts1 = new int[4] { 0, 240, 460, 392 };
                    int[] consts2 = new int[4] { 0, 0, -208, -220 };

                    for (int i = 0; i < (blockSize / 76 / channelCount); i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                        {
                            buffer = reader.ReadBytes(76);

                            for (int k = 0; k < 4; k++)
                            {
                                blockBuffer[0] = (short)(buffer[k * 4 + 0] & 0xF0 | buffer[k * 4 + 1] << 8);
                                blockBuffer[1] = (short)(buffer[k * 4 + 2] & 0xF0 | buffer[k * 4 + 3] << 8);

                                int index4 = (int)buffer[k * 4] & 0x0F;
                                int num10 = (int)buffer[k * 4 + 2] & 0x0F;
                                int index5 = 2;

                                while (index5 < 32)
                                {
                                    int num11 = ((int)buffer[12 + k + index5 * 2] & 240) >> 4;
                                    if (num11 > 7)
                                        num11 -= 16;

                                    int num12 = blockBuffer[index5 - 1] * consts1[index4] + blockBuffer[index5 - 2] * consts2[index4];

                                    blockBuffer[index5] = (short)(num12 + (num11 << 20 - num10) + 128 >> 8);
                                    if (blockBuffer[index5] > (int)short.MaxValue)
                                        blockBuffer[index5] = (int)short.MaxValue;
                                    else if (blockBuffer[index5] < (int)short.MinValue)
                                        blockBuffer[index5] = (int)short.MinValue;

                                    int num13 = (int)buffer[12 + k + index5 * 2] & 15;
                                    if (num13 > 7)
                                        num13 -= 16;

                                    int num14 = blockBuffer[index5] * consts1[index4] + blockBuffer[index5 - 1] * consts2[index4];

                                    blockBuffer[index5 + 1] = (short)(num14 + (num13 << 20 - num10) + 128 >> 8);
                                    if (blockBuffer[index5 + 1] > (int)short.MaxValue)
                                        blockBuffer[index5 + 1] = (int)short.MaxValue;
                                    else if (blockBuffer[index5 + 1] < (int)short.MinValue)
                                        blockBuffer[index5 + 1] = (int)short.MinValue;

                                    index5 += 2;
                                }

                                channels[j].AddRange(blockBuffer);
                            }

                            uint sampleSize = (samples < 128) ? samples : 128;
                            samples -= sampleSize;
                        }
                    }
                }

                short[] outBuffer = new short[channels[0].Count * channelCount];
                for (int i = 0; i < channels[0].Count; i++)
                {
                    for (int j = 0; j < channelCount; j++)
                    {
                        outBuffer[(i * channelCount) + j] = channels[j][i];
                    }
                }

                return outBuffer;
            }
        }
    }

    public class SoundDataTrack
    {
        public string Name { get; set; }
        public string Codec { get; set; }
        public double Duration { get; set; }
        public int SegmentCount { get; set; }
        public string Language { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public short[] Samples { get; set; }
        public uint LoopStart { get; set; }
        public uint LoopEnd { get; set; }

        public double Progress { get => progress; set { progress = value; NotifyPropertyChanged(); } }
        private double progress;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SoundWave : IDisposable
    {
        public SoundDataTrack track;
        public double Progress => (voice.State.SamplesPlayed - (loopPtr / track.ChannelCount) < 0) ? voice.State.SamplesPlayed / (double)(SampleCount / track.ChannelCount) : (voice.State.SamplesPlayed - (loopPtr / track.ChannelCount)) / (double)(SampleCount / track.ChannelCount);
        public long SampleCount => track.Samples.Length;

        private SourceVoice voice;
        private AudioBuffer buffer;
        private int bufferPtr;
        private long loopPtr;
        private long loopCount;

        public SoundWave(SoundDataTrack inTrack, AudioPlayer player)
        {
            track = inTrack;

            WaveFormatExtensible format = new WaveFormatExtensible(track.SampleRate, 16, track.ChannelCount);
            switch (track.ChannelCount)
            {
                case 2: format.ChannelMask = Speakers.FrontLeft | Speakers.FrontRight; break;
                case 4: format.ChannelMask = Speakers.FrontLeft | Speakers.FrontRight | Speakers.BackLeft | Speakers.BackRight; break;
                case 6: format.ChannelMask = Speakers.FrontLeft | Speakers.FrontRight | Speakers.FrontCenter | Speakers.LowFrequency | Speakers.BackLeft | Speakers.BackRight; break;
                default: format.ChannelMask = 0; break;
            }

            voice = new SourceVoice(player.AudioSystem, format, true);
            voice.SetOutputVoices(new VoiceSendDescriptor(player.OutputVoice));
            voice.BufferEnd += Voice_BufferEnd;

            Voice_BufferEnd(IntPtr.Zero);

            voice.Start();
        }

        private const int MAX_BUFFER_SIZE = 4096;
        private void Voice_BufferEnd(IntPtr obj)
        {
            if (bufferPtr < SampleCount)
            {
                int bufferSize = (SampleCount - bufferPtr > MAX_BUFFER_SIZE * track.ChannelCount) ? MAX_BUFFER_SIZE * track.ChannelCount : (int)(SampleCount - bufferPtr);
                DataStream DS = new DataStream(bufferSize * sizeof(short), true, true);
                buffer = new AudioBuffer
                {
                    Stream = DS,
                    AudioBytes = (int)DS.Length,
                    Flags = BufferFlags.None
                };

                // interleave channels
                while (DS.Position < DS.Length)
                {
                    DS.Write(track.Samples[bufferPtr]);
                    bufferPtr++;

                    if (track.LoopEnd != 0 && bufferPtr == track.LoopEnd)
                    {
                        loopPtr += bufferPtr - track.LoopStart;
                        loopCount++;

                        bufferPtr = (int)track.LoopStart;
                    }
                }

                voice.SubmitSourceBuffer(buffer, null);
            }
            else
            {
            }
        }

        public void Dispose()
        {
            voice.Stop();
            voice.DestroyVoice();
            voice.Dispose();
        }
    }

    public class AudioPlayer : IDisposable
    {
        public XAudio2 AudioSystem { get; }
        public MasteringVoice OutputVoice { get; }
        public double Progress => currentSound?.Progress ?? 0.0;
        public bool IsPlaying { get; private set; }

        private SoundWave currentSound;

        public AudioPlayer()
        {
            AudioSystem = new XAudio2();
            OutputVoice = new MasteringVoice(AudioSystem, 8);
        }

        public void PlaySound(SoundDataTrack track)
        {
            SoundDispose();

            currentSound = new SoundWave(track, this);
            IsPlaying = true;
        }

        public void SoundDispose()
        {
            IsPlaying = false;

            if (currentSound == null)
                return;

            var tmpSound = currentSound;
            currentSound = null;
            tmpSound.Dispose();
        }

        public void Dispose()
        {
            SoundDispose();

            OutputVoice.Dispose();
            AudioSystem.Dispose();
        }
    }
}