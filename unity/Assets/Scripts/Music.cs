using System.Collections.Generic;
using UnityEngine;

// Calm procedural background loops: soft pad + bass + bell arpeggio, one mood per level.
public static class Music
{
    const int Rate = 22050;
    static AudioSource src;
    static readonly Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();
    static int current = -1;
    static float target = .32f;

    public static bool On
    {
        get => PlayerPrefs.GetInt("music", 1) == 1;
        set { PlayerPrefs.SetInt("music", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // chords as semitone offsets from A4 (440 Hz); tempo in seconds per chord
    static readonly int[][][] Songs =
    {
        new[] { new[] { -9, -5, -2, 3 }, new[] { -12, -9, -5, 0 }, new[] { -16, -12, -9, -4 }, new[] { -14, -10, -7, -2 } }, // C  Am  F  G
        new[] { new[] { -7, -3, 0, 4 }, new[] { -10, -7, -3, 0 }, new[] { -14, -10, -7, -3 }, new[] { -12, -8, -5, -1 } },  // Dmaj7 Bm7 Gmaj7 A
        new[] { new[] { -5, -2, 2, 7 }, new[] { -9, -5, -2, 2 }, new[] { -14, -10, -7, -2 }, new[] { -7, -3, 0, 5 } },     // Em C G D (airy)
        new[] { new[] { -16, -12, -9, -4 }, new[] { -19, -16, -12, -7 }, new[] { -11, -7, -4, 1 }, new[] { -9, -5, -2, 3 } }, // F Dm Bb C
        new[] { new[] { -12, -9, -5, 0 }, new[] { -16, -12, -9, -4 }, new[] { -19, -16, -12, -7 }, new[] { -17, -13, -10, -5 } }, // Am F Dm E
        new[] { new[] { -14, -10, -7, -2 }, new[] { -9, -5, -2, 3 }, new[] { -12, -9, -5, 0 }, new[] { -16, -12, -9, -4 } }, // G C Am F
        new[] { new[] { -7, -4, 0, 3 }, new[] { -11, -7, -4, 1 }, new[] { -14, -10, -7, -2 }, new[] { -9, -5, -2, 3 } },   // Bm Bb G C (dreamy)
        new[] { new[] { -5, -1, 2, 7 }, new[] { -9, -5, -2, 2 }, new[] { -2, 2, 5, 10 }, new[] { -7, -3, 0, 5 } },         // E C A D
        new[] { new[] { -10, -6, -3, 2 }, new[] { -14, -10, -7, -2 }, new[] { -9, -5, -2, 3 }, new[] { -12, -8, -5, 0 } }, // B7-ish G C A
        new[] { new[] { -14, -11, -7, -2 }, new[] { -18, -14, -11, -6 }, new[] { -21, -18, -14, -9 }, new[] { -19, -15, -12, -7 } }, // Gm Eb C D
        new[] { new[] { -9, -5, -2, 2 }, new[] { -12, -8, -5, 0 }, new[] { -7, -3, 0, 4 }, new[] { -14, -10, -7, -2 } },
        new[] { new[] { -16, -12, -9, -4 }, new[] { -11, -7, -4, 1 }, new[] { -14, -11, -7, -2 }, new[] { -9, -6, -2, 3 } },
        new[] { new[] { -12, -8, -5, -1 }, new[] { -10, -7, -3, 2 }, new[] { -15, -12, -8, -3 }, new[] { -17, -13, -10, -5 } },
        new[] { new[] { -7, -3, 0, 5 }, new[] { -10, -6, -3, 2 }, new[] { -5, -1, 2, 7 }, new[] { -12, -9, -5, 0 } },
        new[] { new[] { -15, -12, -8, -3 }, new[] { -19, -15, -12, -7 }, new[] { -17, -14, -10, -5 }, new[] { -20, -17, -13, -8 } },
    };
    static readonly float[] ChordLen = { 3.4f, 3.8f, 4f, 3.2f, 2.6f, 3.6f, 4.2f, 3f, 2.8f, 2.4f, 3.3f, 3.9f, 3.1f, 2.9f, 2.2f };
    static readonly int[] Arp = { 0, 2, 1, 3, 2, 1, 3, 2 };

    public static void Init(GameObject host)
    {
        src = host.AddComponent<AudioSource>();
        src.loop = true;
        src.volume = 0;
        src.playOnAwake = false;
    }

    public static void Play(int level)
    {
        if (src == null) return;
        level = Mathf.Clamp(level, 0, Songs.Length - 1);
        if (current == level && src.isPlaying) return;
        current = level;
        if (!cache.TryGetValue(level, out var clip)) cache[level] = clip = Render(level);
        src.clip = clip;
        src.volume = 0;
        src.Play();
    }

    public static void Tick(float dt)
    {
        if (src == null) return;
        float want = On ? target : 0f;
        src.volume = Mathf.MoveTowards(src.volume, want, dt * .25f);
    }

    static float Hz(int semi) => 440f * Mathf.Pow(2f, semi / 12f);

    static AudioClip Render(int level)
    {
        var song = Songs[level];
        float cl = ChordLen[level];
        int total = Mathf.CeilToInt(song.Length * cl * Rate);
        var buf = new float[total];
        bool boss = level == 4 || level == 9 || level == 14;
        for (int c = 0; c < song.Length; c++)
        {
            int start = Mathf.RoundToInt(c * cl * Rate);
            var ch = song[c];
            // pad: slow attack/release, overlapping into next chord for a seamless loop
            foreach (int n in ch)
                Voice(buf, start, cl + 1.2f, Hz(n), .055f, 1.1f, 1.2f, true);
            Voice(buf, start, cl + .8f, Hz(ch[0] - 12), .09f, .3f, .9f, false);
            // bells
            int steps = boss ? 8 : 4;
            float step = cl / steps;
            for (int s = 0; s < steps; s++)
            {
                int note = ch[Arp[s % Arp.Length]] + (level == 2 || level == 6 ? 24 : 12);
                Bell(buf, start + Mathf.RoundToInt(s * step * Rate), Hz(note), boss ? .045f : .06f, 1.6f);
            }
        }
        float peak = .001f;
        foreach (var v in buf) peak = Mathf.Max(peak, Mathf.Abs(v));
        float k = .8f / peak;
        for (int i = 0; i < total; i++) buf[i] *= k;
        var clip = AudioClip.Create("music" + level, total, 1, Rate, false);
        clip.SetData(buf, 0);
        return clip;
    }

    static void Voice(float[] buf, int start, float len, float f, float amp, float atk, float rel, bool detune)
    {
        int n = Mathf.CeilToInt(len * Rate), total = buf.Length;
        float w = 2 * Mathf.PI * f / Rate, w2 = w * 1.004f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Clamp01(t / atk) * Mathf.Clamp01((len - t) / rel);
            env = env * env * (3 - 2 * env);
            float v = Mathf.Sin(w * i) + (detune ? Mathf.Sin(w2 * i) * .6f + Mathf.Sin(w * 2 * i) * .12f : 0f);
            buf[(start + i) % total] += v * amp * env;
        }
    }

    static void Bell(float[] buf, int start, float f, float amp, float len)
    {
        int n = Mathf.CeilToInt(len * Rate), total = buf.Length;
        float w = 2 * Mathf.PI * f / Rate;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 3.2f);
            float v = Mathf.Sin(w * i) + Mathf.Sin(w * 3.01f * i) * .12f;
            buf[(start + i) % total] += v * amp * env;
        }
    }
}
