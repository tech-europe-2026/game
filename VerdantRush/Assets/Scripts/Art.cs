using System.Collections.Generic;
using UnityEngine;

public static class Art
{
    static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static Sprite white;

    public static Texture2D Tex(string name)
    {
        if (!tex.TryGetValue(name, out var t))
        {
            t = Resources.Load<Texture2D>(name);
            if (t == null) Debug.LogError("Missing texture " + name);
            else { t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; }
            tex[name] = t;
        }
        return t;
    }

    public static Sprite[] Sheet(string name, int fw, int fh, Vector2 pivot, int count = -1)
    {
        var t = Tex("Sprites/" + name);
        int cols = t.width / fw, rows = t.height / fh;
        if (count < 0) count = cols * rows;
        var list = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            int c = i % cols, r = i / cols;
            var rect = new Rect(c * fw, t.height - (r + 1) * fh, fw, fh);
            list[i] = Sprite.Create(t, rect, pivot, 1f, 0, SpriteMeshType.FullRect);
        }
        return list;
    }

    public static Sprite Single(string path, Vector2 pivot)
    {
        var t = Tex(path);
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, 1f, 0, SpriteMeshType.FullRect);
    }

    public static Sprite White
    {
        get
        {
            if (white == null)
            {
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                t.SetPixels32(px);
                t.Apply();
                white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0, 0), 4f);
            }
            return white;
        }
    }

    public static AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out var c))
        {
            c = Resources.Load<AudioClip>("Audio/" + name);
            clips[name] = c;
        }
        return c;
    }

    public static SpriteRenderer Make(string name, Transform parent, Sprite s, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }

    public static SpriteRenderer Rect(string name, Transform parent, Color col, float x, float y, float w, float h, int order)
    {
        var sr = Make(name, parent, White, order);
        sr.color = col;
        sr.transform.localPosition = new Vector3(x, y, 0);
        sr.transform.localScale = new Vector3(w, h, 1);
        return sr;
    }
}

/// <summary>Pixel font rendered with pooled sprite renderers.</summary>
public class BitmapText
{
    public const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ:.!'-/x ";
    static readonly Dictionary<string, Sprite[]> fonts = new Dictionary<string, Sprite[]>();
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    readonly Transform root;
    readonly Sprite[] glyphs;
    readonly int advance, order;
    string current;
    public float align; // 0 left, 0.5 center, 1 right

    public BitmapText(Transform parent, string font, int order, float align = 0)
    {
        if (!fonts.TryGetValue(font, out glyphs))
        {
            bool big = font == "font_big";
            glyphs = Art.Sheet(font, big ? 14 : 9, big ? 18 : 11, new Vector2(0, 1));
            fonts[font] = glyphs;
        }
        advance = font == "font_big" ? 13 : 7;
        this.order = order;
        this.align = align;
        root = new GameObject("Text").transform;
        root.SetParent(parent, false);
    }

    public Transform Root => root;

    public void SetPos(float x, float y) => root.localPosition = new Vector3(Mathf.Round(x), Mathf.Round(y), 0);
    public void SetActive(bool on) => root.gameObject.SetActive(on);

    public void SetColor(Color c)
    {
        foreach (var sr in pool) sr.color = c;
    }

    public void Set(string s)
    {
        if (s == current) return;
        current = s;
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++) if (chars[i] != 'x') chars[i] = char.ToUpperInvariant(chars[i]);
        s = new string(chars);
        while (pool.Count < s.Length) pool.Add(Art.Make("g", root, null, order));
        float width = s.Length * advance;
        float x0 = Mathf.Round(-width * align);
        for (int i = 0; i < pool.Count; i++)
        {
            var sr = pool[i];
            if (i >= s.Length) { sr.enabled = false; continue; }
            int idx = Chars.IndexOf(s[i]);
            if (idx < 0) idx = Chars.Length - 1;
            sr.enabled = s[i] != ' ';
            sr.sprite = glyphs[idx];
            sr.transform.localPosition = new Vector3(x0 + i * advance, 0, 0);
        }
    }

}
