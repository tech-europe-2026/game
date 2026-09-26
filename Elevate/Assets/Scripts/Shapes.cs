using System;
using UnityEngine;

public static class Shapes
{
    const int Size = 128;

    static Sprite rounded, pill, circle, ring, chevron, star, shadow;

    public static Sprite Rounded => rounded ??= FromSdf((u, v) => RoundRect(u, v, 0.49f, 0.49f, 0.2f), 28);
    public static Sprite Pill => pill ??= FromSdf((u, v) => RoundRect(u, v, 0.49f, 0.49f, 0.49f), 63);
    public static Sprite Circle => circle ??= FromSdf((u, v) => Len(u - 0.5f, v - 0.5f) - 0.49f, 0);
    public static Sprite Ring => ring ??= FromSdf((u, v) => Mathf.Abs(Len(u - 0.5f, v - 0.5f) - 0.43f) - 0.06f, 0);
    public static Sprite Chevron => chevron ??= FromSdf((u, v) =>
        Mathf.Min(Segment(u, v, 0.32f, 0.2f, 0.66f, 0.5f), Segment(u, v, 0.66f, 0.5f, 0.32f, 0.8f)) - 0.085f, 0);
    public static Sprite Shadow => shadow ??= FromAlpha((u, v) =>
        1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.1f, RoundRect(u, v, 0.36f, 0.36f, 0.2f))));
    public static Sprite Star => star ??= FromAlpha(StarMask);

    static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

    static float RoundRect(float u, float v, float hw, float hh, float r)
    {
        float px = Mathf.Abs(u - 0.5f) - (hw - r);
        float py = Mathf.Abs(v - 0.5f) - (hh - r);
        float outside = Len(Mathf.Max(px, 0f), Mathf.Max(py, 0f));
        float inside = Mathf.Min(Mathf.Max(px, py), 0f);
        return outside + inside - r;
    }

    static float Segment(float u, float v, float ax, float ay, float bx, float by)
    {
        float pax = u - ax, pay = v - ay, bax = bx - ax, bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        return Len(pax - bax * h, pay - bay * h);
    }

    static float StarMask(float u, float v)
    {
        const int samples = 4;
        int hits = 0;
        for (int sy = 0; sy < samples; sy++)
        for (int sx = 0; sx < samples; sx++)
        {
            float x = u + (sx + 0.5f) / (samples * Size) - 0.5f / Size;
            float y = v + (sy + 0.5f) / (samples * Size) - 0.5f / Size;
            if (InsideStar(x - 0.5f, y - 0.52f)) hits++;
        }
        return hits / (float)(samples * samples);
    }

    static bool InsideStar(float x, float y)
    {
        bool inside = false;
        const int n = 10;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            StarPoint(i, out float xi, out float yi);
            StarPoint(j, out float xj, out float yj);
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
        }
        return inside;
    }

    static void StarPoint(int i, out float x, out float y)
    {
        float r = i % 2 == 0 ? 0.47f : 0.2f;
        float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
        x = Mathf.Cos(a) * r;
        y = Mathf.Sin(a) * r;
    }

    static Sprite FromSdf(Func<float, float, float> sdf, int border) =>
        FromAlpha((u, v) => Mathf.Clamp01(0.5f - sdf(u, v) * Size), border);

    static Sprite FromAlpha(Func<float, float, float> alpha) => FromAlpha(alpha, 0);

    static Sprite FromAlpha(Func<float, float, float> alpha, int border)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float a = alpha((x + 0.5f) / Size, (y + 0.5f) / Size);
            px[y * Size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}
