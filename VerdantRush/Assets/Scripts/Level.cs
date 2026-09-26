using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class ChunkData { public string file; public float x, y, w, h; }
[Serializable] public class PathData { public float[] pts; public bool oneWay; public int loop; }
[Serializable] public class LoopData { public float cx, cy, r; }
[Serializable] public class ObjData { public string type; public float x, y, a, b; }

[Serializable]
public class LevelData
{
    public float width, yMin, yMax;
    public ChunkData[] chunks;
    public PathData[] paths;
    public LoopData[] loops;
    public ObjData[] objects;

    public static LevelData Load(string name)
    {
        var ta = Resources.Load<TextAsset>(name);
        return JsonUtility.FromJson<LevelData>(ta.text);
    }
}

public struct Seg
{
    public Vector2 a, b, n;
    public float len;
    public bool oneWay;
}

/// <summary>Static collision geometry: polyline segments bucketed by x.</summary>
public class World
{
    const float Bucket = 64f;
    public readonly List<Seg> segs = new List<Seg>();
    readonly Dictionary<int, List<int>> buckets = new Dictionary<int, List<int>>();
    readonly List<int> query = new List<int>();
    int[] stamp;
    int stampId;

    public World(LevelData level)
    {
        foreach (var p in level.paths)
        {
            if (p.loop >= 0) continue;
            for (int i = 0; i + 3 < p.pts.Length; i += 2)
            {
                var a = new Vector2(p.pts[i], p.pts[i + 1]);
                var b = new Vector2(p.pts[i + 2], p.pts[i + 3]);
                var d = b - a;
                float len = d.magnitude;
                if (len < 0.01f) continue;
                var n = new Vector2(-d.y, d.x) / len;
                Add(new Seg { a = a, b = b, n = n, len = len, oneWay = p.oneWay });
            }
        }
        stamp = new int[segs.Count];
    }

    void Add(Seg s)
    {
        int id = segs.Count;
        segs.Add(s);
        int b0 = Mathf.FloorToInt(Mathf.Min(s.a.x, s.b.x) / Bucket);
        int b1 = Mathf.FloorToInt(Mathf.Max(s.a.x, s.b.x) / Bucket);
        for (int k = b0; k <= b1; k++)
        {
            if (!buckets.TryGetValue(k, out var l)) buckets[k] = l = new List<int>();
            l.Add(id);
        }
    }

    public List<int> Query(float x0, float x1)
    {
        query.Clear();
        stampId++;
        int b0 = Mathf.FloorToInt(x0 / Bucket), b1 = Mathf.FloorToInt(x1 / Bucket);
        for (int k = b0; k <= b1; k++)
        {
            if (!buckets.TryGetValue(k, out var l)) continue;
            foreach (int id in l)
            {
                if (stamp[id] == stampId) continue;
                stamp[id] = stampId;
                query.Add(id);
            }
        }
        return query;
    }

    public static Vector2 Closest(in Seg s, Vector2 p)
    {
        var d = s.b - s.a;
        float t = Mathf.Clamp01(Vector2.Dot(p - s.a, d) / (s.len * s.len));
        return s.a + d * t;
    }

    /// <summary>Surface under a grounded circle: projection must lie inside a segment whose normal faces 'up'.</summary>
    public bool FindGround(Vector2 c, Vector2 up, float r, float snap, out Vector2 pos, out Vector2 normal)
    {
        pos = c; normal = up;
        float best = float.MaxValue;
        bool found = false;
        var q = Query(c.x - r - snap - 8, c.x + r + snap + 8);
        foreach (int id in q)
        {
            var s = segs[id];
            if (Vector2.Dot(s.n, up) < 0.5f) continue;
            var d = s.b - s.a;
            float along = Vector2.Dot(c - s.a, d) / s.len;
            if (along < -2f || along > s.len + 2f) continue;
            float sd = Vector2.Dot(c - s.a, s.n);
            if (sd < (s.oneWay ? r - 6f : -6f) || sd > r + snap) continue;
            float score = Mathf.Abs(sd - r);
            if (score < best)
            {
                best = score;
                found = true;
                float t = Mathf.Clamp(along, 0, s.len);
                pos = s.a + d / s.len * t + s.n * r;
                normal = s.n;
            }
        }
        return found;
    }

    /// <summary>Pushes a circle out of solid geometry. Returns the combined contact normal.</summary>
    public bool Resolve(ref Vector2 c, Vector2 prev, float r, bool falling, Vector2 upFilter, float filterMax, out Vector2 normal)
    {
        normal = Vector2.zero;
        bool hit = false;
        for (int iter = 0; iter < 2; iter++)
        {
            var q = Query(c.x - r - 4, c.x + r + 4);
            foreach (int id in q)
            {
                var s = segs[id];
                if (filterMax < 1f && Vector2.Dot(s.n, upFilter) >= filterMax) continue;
                var cp = Closest(s, c);
                var diff = c - cp;
                float dist = diff.magnitude;
                if (dist >= r) continue;
                Vector2 dir;
                if (s.oneWay)
                {
                    if (!falling || prev.y - r < cp.y - 4f) continue;
                    dir = s.n;
                    if (Vector2.Dot(c - s.a, s.n) < -r) continue;
                    c = new Vector2(c.x, cp.y + r);
                }
                else
                {
                    float sd = Vector2.Dot(diff, s.n);
                    if (sd < 0)
                    {
                        if (Vector2.Dot(prev - Closest(s, prev), s.n) < 0) continue;
                        dir = s.n;
                    }
                    else dir = dist > 1e-4f ? diff / dist : s.n;
                    c = cp + dir * r;
                }
                normal += dir;
                hit = true;
            }
        }
        if (hit) normal = normal.normalized;
        return hit;
    }

    /// <summary>Highest walkable surface at x at or below y.</summary>
    public float GroundBelow(float x, float y)
    {
        float best = float.MinValue;
        foreach (int id in Query(x - 1, x + 1))
        {
            var s = segs[id];
            if (s.n.y < 0.3f) continue;
            float x0 = Mathf.Min(s.a.x, s.b.x), x1 = Mathf.Max(s.a.x, s.b.x);
            if (x < x0 || x > x1 || x1 - x0 < 0.01f) continue;
            float gy = Mathf.Lerp(s.a.y, s.b.y, (x - s.a.x) / (s.b.x - s.a.x));
            if (gy <= y + 8 && gy > best) best = gy;
        }
        return best;
    }
}
