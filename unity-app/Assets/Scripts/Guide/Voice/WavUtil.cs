using System;
using UnityEngine;

namespace NtutAR.Guide.Voice
{
    /// <summary>
    /// WAV(PCM)解碼 / 編碼 + 開頭提示音能量裁剪(純函式,方便單元測試)。
    /// GLM TTS 免費帳號的音檔開頭帶服務端提示音(鈴聲 + 兩聲輕叮),無 API 參數可關,
    /// 用能量偵測找到「語音真正開始」的位置裁掉前面。
    /// </summary>
    public static class WavUtil
    {
        /// <summary>
        /// 解碼標準 PCM WAV bytes。回傳是否成功;samples 為 [-1,1] 浮點(交錯多聲道)。
        /// 支援 8/16 bit;其餘 bit depth 回 false。
        /// </summary>
        public static bool TryDecode(byte[] wav, out float[] samples, out int channels, out int sampleRate)
        {
            samples = Array.Empty<float>();
            channels = 1;
            sampleRate = 24000;
            if (wav == null || wav.Length < 44) return false;

            // RIFF / WAVE 標頭
            if (wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F') return false;
            if (wav[8] != 'W' || wav[9] != 'A' || wav[10] != 'V' || wav[11] != 'E') return false;

            int bitsPerSample = 16;
            int dataOffset = -1, dataSize = 0;

            // 逐 chunk 掃描(fmt/data 之間可能夾 LIST/fact 等)
            int p = 12;
            while (p + 8 <= wav.Length)
            {
                string id = new string(new[] { (char)wav[p], (char)wav[p + 1], (char)wav[p + 2], (char)wav[p + 3] });
                int size = BitConverter.ToInt32(wav, p + 4);
                int body = p + 8;
                if (size < 0 || body + size > wav.Length) size = wav.Length - body;

                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(wav, body + 2);
                    sampleRate = BitConverter.ToInt32(wav, body + 4);
                    bitsPerSample = BitConverter.ToInt16(wav, body + 14);
                }
                else if (id == "data")
                {
                    dataOffset = body;
                    dataSize = size;
                    break;
                }

                p = body + size + (size & 1);   // chunk 補齊到偶數
            }

            if (dataOffset < 0 || channels <= 0) return false;

            if (bitsPerSample == 16)
            {
                int count = dataSize / 2;
                samples = new float[count];
                for (int i = 0; i < count; i++)
                {
                    short s = BitConverter.ToInt16(wav, dataOffset + i * 2);
                    samples[i] = s / 32768f;
                }
                return true;
            }
            if (bitsPerSample == 8)
            {
                int count = dataSize;
                samples = new float[count];
                for (int i = 0; i < count; i++)
                    samples[i] = (wav[dataOffset + i] - 128) / 128f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 找出「語音真正開始」的取樣索引(交錯樣本的全域索引,已對齊聲道)。
        /// 作法:每 windowMs 算一格 RMS,找到「連續 requiredWindows 格都超過 rmsThreshold」的第一格,
        /// 再往前留 prerollMs。找不到語音就回 0(整段播放,不裁)。
        /// rmsThreshold 預設 0.045 ≈ 16-bit 振幅 1500(語音 ~0.08+,叮聲 ~0.03)。
        /// </summary>
        public static int FindSpeechStartSample(
            float[] samples, int channels, int sampleRate,
            float rmsThreshold = 0.045f, int windowMs = 100, int requiredWindows = 2, int prerollMs = 50)
        {
            if (samples == null || samples.Length == 0 || channels <= 0 || sampleRate <= 0) return 0;

            int frameCount = samples.Length / channels;             // 每聲道的樣本數
            int windowFrames = Mathf.Max(1, sampleRate * windowMs / 1000);
            int consecutive = 0;

            for (int w = 0; w * windowFrames < frameCount; w++)
            {
                int startFrame = w * windowFrames;
                int endFrame = Mathf.Min(startFrame + windowFrames, frameCount);

                double sumSq = 0;
                int n = 0;
                for (int f = startFrame; f < endFrame; f++)
                {
                    float v = samples[f * channels];               // 取第 0 聲道判定能量
                    sumSq += (double)v * v;
                    n++;
                }
                float rms = n > 0 ? Mathf.Sqrt((float)(sumSq / n)) : 0f;

                if (rms > rmsThreshold)
                {
                    consecutive++;
                    if (consecutive >= requiredWindows)
                    {
                        int speechStartFrame = (w - requiredWindows + 1) * windowFrames;
                        int prerollFrames = sampleRate * prerollMs / 1000;
                        int startFrameClamped = Mathf.Max(0, speechStartFrame - prerollFrames);
                        return startFrameClamped * channels;
                    }
                }
                else
                {
                    consecutive = 0;
                }
            }
            return 0;
        }

        /// <summary>把 [-1,1] 浮點(單聲道)編成 16-bit PCM WAV bytes(供 STT 上傳)。</summary>
        public static byte[] EncodeWav16(float[] mono, int sampleRate)
        {
            mono ??= Array.Empty<float>();
            const int channels = 1;
            const int bits = 16;
            int dataBytes = mono.Length * 2;
            int byteRate = sampleRate * channels * bits / 8;

            var buffer = new byte[44 + dataBytes];
            int o = 0;
            void PutStr(string s) { foreach (char c in s) buffer[o++] = (byte)c; }
            void PutInt(int v) { buffer[o++] = (byte)v; buffer[o++] = (byte)(v >> 8); buffer[o++] = (byte)(v >> 16); buffer[o++] = (byte)(v >> 24); }
            void PutShort(short v) { buffer[o++] = (byte)v; buffer[o++] = (byte)(v >> 8); }

            PutStr("RIFF"); PutInt(36 + dataBytes); PutStr("WAVE");
            PutStr("fmt "); PutInt(16); PutShort(1); PutShort(channels);
            PutInt(sampleRate); PutInt(byteRate); PutShort((short)(channels * bits / 8)); PutShort(bits);
            PutStr("data"); PutInt(dataBytes);

            for (int i = 0; i < mono.Length; i++)
            {
                short s = (short)Mathf.Clamp(mono[i] * 32767f, -32768f, 32767f);
                buffer[o++] = (byte)s;
                buffer[o++] = (byte)(s >> 8);
            }
            return buffer;
        }
    }
}
