using System.Collections.Generic;
using UnityEngine;

public static class Gfx
{
    public static readonly Color SkyTop = new Color32(120, 170, 245, 255);
    public static readonly Color SkyMid = new Color32(186, 214, 255, 255);
    public static readonly Color SkyBottom = new Color32(255, 222, 214, 255);
    public static readonly Color Plat = new Color32(252, 252, 255, 255);
    public static readonly Color PlatShade = new Color32(196, 208, 238, 255);
    public static readonly Color Ink = new Color32(30, 40, 78, 255);
    public static readonly Color Gold = new Color32(236, 190, 84, 255);
    public static readonly Color Coral = new Color32(255, 96, 110, 255);
    public static readonly Color Mint = new Color32(72, 222, 176, 255);
    public static readonly Color Lilac = new Color32(150, 128, 255, 255);
    public static readonly Color Cyan = new Color32(90, 205, 255, 255);
    public static readonly Color Glass = new Color32(170, 225, 255, 150);

    static Sprite square, circle, glow, ring, tri, rounded, heart;
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

    public static Sprite Heart => heart != null ? heart : heart = Make(128, (x, y, n) =>
    {
        float best = 0f;
        for (int s = 0; s < 4; s++)
        {
            float px = ((x + (s & 1) * .5f + .25f) / n - .5f) * 2.5f, py = ((y + (s >> 1) * .5f + .25f) / n - .45f) * 2.5f;
            float a = px * px + py * py - 1f;
            if (a * a * a - px * px * py * py * py <= 0f) best += .25f;
        }
        return best;
    });

    public static Sprite Square => square != null ? square : square = Make(4, (x, y, n) => 1f);

    public static Sprite Circle => circle != null ? circle : circle = Make(128, (x, y, n) =>
    {
        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f));
        return Mathf.Clamp01(n / 2f - d);
    });

    public static Sprite Glow => glow != null ? glow : glow = Make(64, (x, y, n) =>
    {
        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
        return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
    });

    public static Sprite Ring => ring != null ? ring : ring = Make(128, (x, y, n) =>
    {
        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f));
        return Mathf.Clamp01(3f - Mathf.Abs(d - (n / 2f - 4f)));
    });

    // upward-pointing triangle filling the unit square
    public static Sprite Tri => tri != null ? tri : tri = Make(64, (x, y, n) =>
    {
        float half = (n - y) / 2f;
        return Mathf.Clamp01(half - Mathf.Abs(x + .5f - n / 2f) + .5f);
    });

    // 9-sliced rounded rectangle, 1 unit = 64px, corner radius .25 units
    public static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            const int n = 64, r = 16;
            var t = NewTex(n);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, r, n - r), cy = Mathf.Clamp(y + .5f, r, n - r);
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d + .5f) * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            rounded = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return rounded;
        }
    }

    public static Sprite Ball(string state)
    {
        if (!ballSprites.TryGetValue(state, out var s))
        {
            s = Resources.Load<Sprite>("Sprites/" + state);
            ballSprites[state] = s;
        }
        return s;
    }

    static Texture2D NewTex(int n) => new Texture2D(n, n, TextureFormat.RGBA32, false)
    {
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp
    };

    static Sprite Make(int n, System.Func<int, int, int, float> alpha)
    {
        var t = NewTex(n);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y, n)) * 255));
        t.SetPixels32(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n);
    }

    public static Sprite VerticalGradient(Color bottom, Color mid, Color top)
    {
        var t = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < 256; y++)
        {
            float k = y / 255f;
            t.SetPixel(0, y, k < .5f ? Color.Lerp(bottom, mid, k * 2f) : Color.Lerp(mid, top, (k - .5f) * 2f));
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 1, 256), new Vector2(.5f, .5f), 1);
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

    public static SpriteRenderer Slab(Transform parent, Vector2 localPos, Vector2 size, Color c, int order)
    {
        var go = new GameObject("slab");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Rounded;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = c;
        sr.sortingOrder = order;
        return sr;
    }
}
