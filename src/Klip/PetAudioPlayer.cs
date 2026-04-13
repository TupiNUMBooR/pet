using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Klip;

public sealed class PetAudioPlayer : IDisposable
{
    private const string MeowResourceName = "Klip.assets.meow.ogg";
    private const string PanicResourceName = "Klip.assets.panic.ogg";
    private const string PurrResourceName = "Klip.assets.purr.ogg";

    private static readonly WaveFormat OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private readonly object gate = new();
    private readonly WaveOutEvent output;
    private readonly MixingSampleProvider mixer;

    private readonly CachedClip meowClip;
    private readonly CachedClip panicClip;
    private readonly CachedClip purrClip;

    private ISampleProvider? currentInput;
    private bool disposed;

    public PetAudioPlayer()
    {
        meowClip = LoadClip(MeowResourceName);
        panicClip = LoadClip(PanicResourceName);
        purrClip = LoadClip(PurrResourceName);

        mixer = new MixingSampleProvider(OutputFormat)
        {
            ReadFully = true
        };

        output = new WaveOutEvent
        {
            DesiredLatency = 60,
            NumberOfBuffers = 2
        };

        output.Init(mixer);
        output.Play();
    }

    public bool SoundsEnabled { get; set; } = true;

    public void PlayMeow()
    {
        Play(meowClip, 0.95f, 1.05f);
    }

    public void PlayPanic()
    {
        Play(panicClip, 0.98f, 1.08f);
    }

    public void PlayPurr()
    {
        Play(purrClip, 0.98f, 1.02f);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        output.Stop();
        output.Dispose();
    }

    private void Play(CachedClip clip, float minPitch, float maxPitch)
    {
        if (!SoundsEnabled) return;

        float pitch = RandomRange(minPitch, maxPitch);

        ISampleProvider input = new CachedClipSampleProvider(clip);

        if (Math.Abs(pitch - 1.0f) > 0.001f)
        {
            input = new SmbPitchShiftingSampleProvider(input)
            {
                PitchFactor = pitch
            };
        }

        lock (gate)
        {
            if (currentInput is not null)
            {
                mixer.RemoveMixerInput(currentInput);
            }

            mixer.AddMixerInput(input);
            currentInput = input;
        }
    }

    private static CachedClip LoadClip(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream resource = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource not found: {resourceName}");

        using VorbisWaveReader reader = new(resource);

        ISampleProvider provider = reader.ToSampleProvider();

        if (provider.WaveFormat.Channels == 1)
        {
            provider = new MonoToStereoSampleProvider(provider);
        }

        if (provider.WaveFormat.SampleRate != OutputFormat.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, OutputFormat.SampleRate);
        }

        List<float> samples = [];
        float[] buffer = new float[4096];

        while (true)
        {
            int read = provider.Read(buffer, 0, buffer.Length);

            if (read == 0)
            {
                break;
            }

            for (int i = 0; i < read; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        return new CachedClip(samples.ToArray(), OutputFormat);
    }

    private static float RandomRange(float min, float max)
    {
        return min + ((float)Random.Shared.NextDouble() * (max - min));
    }

    private sealed class CachedClip
    {
        public CachedClip(float[] samples, WaveFormat waveFormat)
        {
            Samples = samples;
            WaveFormat = waveFormat;
        }

        public float[] Samples { get; }
        public WaveFormat WaveFormat { get; }
    }

    private sealed class CachedClipSampleProvider : ISampleProvider
    {
        private readonly CachedClip clip;
        private int position;

        public CachedClipSampleProvider(CachedClip clip)
        {
            this.clip = clip;
        }

        public WaveFormat WaveFormat => clip.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int available = clip.Samples.Length - position;
            int toCopy = Math.Min(available, count);

            if (toCopy <= 0)
            {
                return 0;
            }

            Array.Copy(clip.Samples, position, buffer, offset, toCopy);
            position += toCopy;

            return toCopy;
        }
    }
}
