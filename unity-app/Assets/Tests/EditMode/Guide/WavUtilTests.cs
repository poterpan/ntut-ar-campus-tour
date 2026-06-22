using NUnit.Framework;
using NtutAR.Guide.Voice;

namespace NtutAR.Guide.Tests
{
    public class WavUtilTests
    {
        [Test]
        public void EncodeDecode_Roundtrip_PreservesSamplesAndFormat()
        {
            var src = new float[] { 0f, 0.5f, -0.5f, 0.25f, -0.99f, 0.99f };
            const int rate = 24000;

            byte[] wav = WavUtil.EncodeWav16(src, rate);
            bool ok = WavUtil.TryDecode(wav, out float[] decoded, out int channels, out int sampleRate);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, channels);
            Assert.AreEqual(rate, sampleRate);
            Assert.AreEqual(src.Length, decoded.Length);
            for (int i = 0; i < src.Length; i++)
                Assert.AreEqual(src[i], decoded[i], 0.001f, $"sample {i}");
        }

        [Test]
        public void TryDecode_GarbageBytes_ReturnsFalse()
        {
            Assert.IsFalse(WavUtil.TryDecode(new byte[] { 1, 2, 3 }, out _, out _, out _));
        }

        [Test]
        public void FindSpeechStart_TrimsLowEnergyPrefix()
        {
            const int rate = 24000;
            int windowFrames = rate * 100 / 1000;   // 2400
            int lowWindows = 5;

            // 前 5 格(0.5s)低能量(模擬叮聲,< 門檻),之後語音(> 門檻)
            var samples = new float[lowWindows * windowFrames + 4 * windowFrames];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = i < lowWindows * windowFrames ? 0.02f : 0.1f;

            int start = WavUtil.FindSpeechStartSample(samples, 1, rate);

            // 語音 onset 在 frame 12000;連續兩格判定後往前留 50ms(1200)→ 10800
            Assert.AreEqual(10800, start);
        }

        [Test]
        public void FindSpeechStart_AllSilent_ReturnsZero()
        {
            var samples = new float[24000];   // 全靜音
            Assert.AreEqual(0, WavUtil.FindSpeechStartSample(samples, 1, 24000));
        }
    }
}
