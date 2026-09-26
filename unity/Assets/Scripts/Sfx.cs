using System.Collections.Generic;
using UnityEngine;

public static class Sfx
{
    static AudioSource src;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    const int Rate = 22050;

    public static void Init(GameObject host)
    {
        src = host.AddComponent<AudioSource>();
        src.playOnAwake = false;
        Add("jump", .14f, t => Sq(t, Mathf.Lerp(300, 700, t / .14f)) * Env(t, .14f));
        Add("bounce", .2f, t => Sq(t, Mathf.Lerp(200, 900, t / .2f)) * Env(t, .2f));
        Add("land", .08f, t => Noise() * Env(t, .08f) * .6f);
        Add("dash", .25f, t => (Noise() * .5f + Sq(t, Mathf.Lerp(900, 200, t / .25f)) * .5f) * Env(t, .25f));
        Add("grow", .3f, t => Sq(t, Mathf.Lerp(120, 60, t / .3f)) * Env(t, .3f));
        Add("shrink", .2f, t => Sq(t, Mathf.Lerp(400, 800, t / .2f)) * Env(t, .2f));
        Add("teleport", .3f, t => Sq(t, 400 + 300 * Mathf.Sin(t * 120)) * Env(t, .3f));
        Add("freeze", .35f, t => Mathf.Sin(t * 2 * Mathf.PI * (1800 + Mathf.Sin(t * 60) * 400)) * Env(t, .35f) * .6f);
        Add("invis", .4f, t => Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(900, 300, t / .4f)) * Env(t, .4f) * .6f);
        Add("coin", .18f, t => Sq(t, t < .06f ? 988 : 1319) * Env(t, .18f) * .7f);
        Add("heal", .4f, t => Sq(t, t < .13f ? 523 : t < .26f ? 659 : 784) * Env(t, .4f) * .6f);
        Add("hurt", .3f, t => (Sq(t, Mathf.Lerp(300, 80, t / .3f)) * .6f + Noise() * .4f) * Env(t, .3f));
        Add("smash", .35f, t => Noise() * Env(t, .35f));
        Add("win", 1f, t => Sq(t, t < .15f ? 523 : t < .3f ? 659 : t < .45f ? 784 : 1047) * Env(t, 1f) * .6f);
    }

    static float Sq(float t, float f) => Mathf.Repeat(t * f, 1f) < .5f ? .35f : -.35f;
    static float Noise() => Random.value * 2f - 1f;
    static float Env(float t, float len) => Mathf.Clamp01(1f - t / len);

    static void Add(string name, float len, System.Func<float, float> f)
    {
        int n = Mathf.CeilToInt(len * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = f(i / (float)Rate) * .5f;
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        clips[name] = clip;
    }

    public static void Play(string name, float vol = 1f)
    {
        if (src != null && clips.TryGetValue(name, out var c))
        {
            src.pitch = Random.Range(.94f, 1.06f);
            src.PlayOneShot(c, vol);
        }
    }
}
