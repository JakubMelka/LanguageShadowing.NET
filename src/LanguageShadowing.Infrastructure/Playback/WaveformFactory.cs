// MIT License
//
// Copyright (c) 2026 Jakub Melka and Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Buffers.Binary;
using LanguageShadowing.Core.Models;

namespace LanguageShadowing.Infrastructure.Playback;

/// <summary>
/// Creates waveform previews either from decoded WAV payloads or from estimated segment metadata.
/// </summary>
public sealed class WaveformFactory
{
    /// <summary>
    /// The default number of visual samples used by the waveform control.
    /// </summary>
    public const int SampleCount = 144;

    /// <summary>
    /// Creates an estimated waveform when real audio samples are unavailable.
    /// </summary>
    public WaveformData CreateEstimated(IReadOnlyList<SpeechSegment> segments, int sampleCount)
    {
        if (segments.Count == 0)
        {
            return WaveformData.Empty;
        }

        var samples = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var segment = segments[i % segments.Count];
            var normalizedLength = Math.Clamp(segment.Duration.TotalSeconds / 2.5, 0.15, 1.0);
            var modulation = 0.15 + 0.85 * Math.Abs(MathF.Sin((i + 1) * 0.43f));
            samples[i] = (float)(normalizedLength * modulation);
        }

        return new WaveformData(samples, true);
    }

    /// <summary>
    /// Attempts to derive waveform data and duration from a 16-bit PCM WAV payload.
    /// </summary>
    public bool TryCreateFromWave(byte[] audioBytes, int sampleCount, out WaveformData waveform, out TimeSpan duration)
    {
        waveform = WaveformData.Empty;
        duration = TimeSpan.Zero;

        if (audioBytes.Length < 44 || !Matches(audioBytes, 0, "RIFF") || !Matches(audioBytes, 8, "WAVE"))
        {
            return false;
        }

        var channelCount = BinaryPrimitives.ReadInt16LittleEndian(audioBytes.AsSpan(22, 2));
        var sampleRate = BinaryPrimitives.ReadInt32LittleEndian(audioBytes.AsSpan(24, 4));
        var bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(audioBytes.AsSpan(34, 2));
        var dataOffset = FindChunk(audioBytes, "data");
        if (dataOffset < 0 || bitsPerSample != 16 || channelCount <= 0 || sampleRate <= 0)
        {
            return false;
        }

        var dataSize = BinaryPrimitives.ReadInt32LittleEndian(audioBytes.AsSpan(dataOffset + 4, 4));
        var pcmStart = dataOffset + 8;
        if (pcmStart + dataSize > audioBytes.Length || dataSize <= 0)
        {
            return false;
        }

        var blockAlign = channelCount * (bitsPerSample / 8);
        var totalSamples = dataSize / blockAlign;
        if (totalSamples <= 0)
        {
            return false;
        }

        duration = TimeSpan.FromSeconds(totalSamples / (double)sampleRate);
        var samples = new float[sampleCount];
        var samplesPerBucket = Math.Max(1, totalSamples / sampleCount);

        for (var bucket = 0; bucket < sampleCount; bucket++)
        {
            var startSample = bucket * samplesPerBucket;
            var endSample = bucket == sampleCount - 1 ? totalSamples : Math.Min(totalSamples, startSample + samplesPerBucket);
            short peak = 0;

            for (var sampleIndex = startSample; sampleIndex < endSample; sampleIndex++)
            {
                var byteIndex = pcmStart + sampleIndex * blockAlign;
                var value = BinaryPrimitives.ReadInt16LittleEndian(audioBytes.AsSpan(byteIndex, 2));
                peak = (short)Math.Max(peak, Math.Abs(value));
            }

            samples[bucket] = peak / (float)short.MaxValue;
        }

        waveform = new WaveformData(samples, false);
        return true;
    }

    private static bool Matches(byte[] source, int offset, string marker)
    {
        return marker.Select((character, index) => source[offset + index] == character).All(match => match);
    }

    private static int FindChunk(byte[] source, string chunk)
    {
        for (var i = 12; i <= source.Length - 8; i++)
        {
            if (Matches(source, i, chunk))
            {
                return i;
            }
        }

        return -1;
    }
}
