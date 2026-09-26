using System.Collections.Generic;
using UnityEngine;

public static class Gfx
{
    public static readonly Color Bg = new Color32(11, 14, 38, 255);
    public static readonly Color Ground = new Color32(29, 43, 83, 255);
    public static readonly Color GroundTop = new Color32(41, 173, 255, 255);
    public static readonly Color Ice = new Color32(160, 230, 255, 255);
    public static readonly Color Crate = new Color32(171, 82, 54, 255);
    public static readonly Color CrateEdge = new Color32(255, 163, 0, 255);
    public static readonly Color Cracked = new Color32(95, 87, 79, 255);
    public static readonly Color Spike = new Color32(255, 0, 77, 255);
    public static readonly Color Lava = new Color32(255, 80, 20, 255);
    public static readonly Color Gold = new Color32(255, 236, 39, 255);
    public static readonly Color Green = new Color32(0, 228, 54, 255);
    public static readonly Color Pink = new Color32(255, 119, 168, 255);
    public static readonly Color Glass = new Color32(131, 118, 156, 255);

    static Sprite square, circle, glow, spike, ring;
    static readonly Dictionary<string, Sprite> ballSprites = new Dictionary<string, Sprite>();
    static Material spriteMat;

    public static Material SpriteMat
    {
        get
        {
            if (spriteMat == null) spriteMat = new Material(Shader.Find("Sprites/Default"));
            return spriteMat;
        }
    }

    public static Sprite Square => square != null ? square : square = Make(4, true, (x, y, n) => 1f);

    public static Sprite Circle => circle != null ? circle : circle = Make(64, false, (x, y, n) =>
    {
        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f));
        return Mathf.Clamp01(n / 2f - d);
    });

    public static Sprite Glow => glow != null ? glow : glow = Make(64, false, (x, y, n) =>
    {
        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
        return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
    });

    public static Sprite Ring => ring != null ? ring : ring = Make(64, false, (x, y, n) =>
    {
        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f));
        return Mathf.Clamp01(1.5f - Mathf.Abs(d - (n / 2f - 3f)));
    });

    public static Sprite SpikeSprite => spike != null ? spike : spike = Make(16, true, (x, y, n) =>
    {
        float half = (n - y) / 2f;
        return Mathf.Abs(x + .5f - n / 2f) < half ? 1f : 0f;
    });

    public static Sprite Ball(string state)
    {
        if (!ballSprites.TryGetValue(state, out var s))
        {
            s = Resources.Load<Sprite>("Sprites/" + state);
            ballSprites[state] = s;
        }
        return s;
    }

    static Sprite Make(int n, bool point, System.Func<int, int, int, float> alpha)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            filterMode = point ? FilterMode.Point : FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = new Color32(255, 255, 255, (byte)(alpha(x, y, n) * 255));
        t.SetPixels32(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n);
    }

    public static SpriteRenderer Quad(Transform parent, Vector2 localPos, Vector2 size, Color c, int order, Sprite s = null)
    {
        var go = new GameObject("q");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s != null ? s : Square;
        sr.color = c;
        sr.sortingOrder = order;
        return sr;
    }
}
