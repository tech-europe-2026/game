using System.Collections.Generic;
using UnityEngine;

// Fluent, coordinate-based level construction: every piece is placed in world units.
public class LevelBuilder
{
    public Transform root;
    public Vector2 start;
    public Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
    public int orbs;

    static PhysicsMaterial2D platMat, railMat, padMat, boxMat, iceMat;

    static PhysicsMaterial2D Mat(ref PhysicsMaterial2D m, float friction, float bounce)
    {
        if (m == null) m = new PhysicsMaterial2D { friction = friction, bounciness = bounce };
        return m;
    }

    public LevelBuilder()
    {
        root = new GameObject("Level").transform;
    }

    void Grow(Vector2 p, float pad = 0f)
    {
        min = Vector2.Min(min, p - Vector2.one * pad);
        max = Vector2.Max(max, p + Vector2.one * pad);
    }

    GameObject Go(string name, Vector2 p, float angle = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = p;
        go.transform.rotation = Quaternion.Euler(0, 0, angle);
        return go;
    }

    static Tile AddTile(GameObject go, TileKind k)
    {
        var t = go.AddComponent<Tile>();
        t.kind = k;
        return t;
    }

    public LevelBuilder Start(float x, float y)
    {
        start = new Vector2(x, y);
        Grow(start, 2f);
        return this;
    }

    // Floating slab. x,y = centre.
    public GameObject Plat(float x, float y, float w, float h = .7f, float angle = 0f)
    {
        var go = Go("plat", new Vector2(x, y), angle);
        Gfx.Slab(go.transform, new Vector2(0, -.12f), new Vector2(w, h), Gfx.PlatShade, 1);
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(w, h), Gfx.Plat, 2);
        var c = go.AddComponent<BoxCollider2D>();
        c.edgeRadius = .1f;
        c.size = new Vector2(w - .2f, h - .2f);
        c.sharedMaterial = Mat(ref platMat, .7f, 0f);
        AddTile(go, TileKind.Solid);
        var q = Quaternion.Euler(0, 0, angle);
        Grow((Vector2)go.transform.position + (Vector2)(q * new Vector2(-w / 2, 0)), h);
        Grow((Vector2)go.transform.position + (Vector2)(q * new Vector2(w / 2, 0)), h);
        return go;
    }

    // Slab from (x0,y0) to (x1,y1) – handy for slopes.
    public GameObject Ramp(float x0, float y0, float x1, float y1, float h = .7f)
    {
        var d = new Vector2(x1 - x0, y1 - y0);
        return Plat((x0 + x1) / 2, (y0 + y1) / 2, d.magnitude + h * .5f, h, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    // Frictionless ice slab from (x0,y0) to (x1,y1); the ball slides in `dir` without steering.
    public GameObject Ice(float x0, float y0, float x1, float y1, int dir = 1, float h = .6f)
    {
        var d = new Vector2(x1 - x0, y1 - y0);
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, w = d.magnitude + h * .5f;
        var go = Go("ice", new Vector2((x0 + x1) / 2, (y0 + y1) / 2), ang);
        Gfx.Slab(go.transform, new Vector2(0, -.12f), new Vector2(w, h), new Color32(140, 200, 235, 255), 1);
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(w, h), new Color32(214, 242, 255, 255), 2);
        Gfx.Slab(go.transform, new Vector2(0, h * .22f), new Vector2(w - .4f, h * .16f), new Color(1, 1, 1, .85f), 3);
        var c = go.AddComponent<BoxCollider2D>();
        c.edgeRadius = .1f;
        c.size = new Vector2(w - .2f, h - .2f);
        c.sharedMaterial = Mat(ref iceMat, 0f, 0f);
        AddTile(go, TileKind.Ice).dir = dir;
        Grow(new Vector2(x0, y0), h);
        Grow(new Vector2(x1, y1), h);
        return go;
    }

    // Roller-coaster: a grind rail with support struts.
    public GameObject Coaster(params Vector2[] pts)
    {
        var go = Rail(true, pts);
        float last = float.MinValue;
        foreach (var p in go.GetComponent<EdgeCollider2D>().points)
        {
            if (p.x - last < 1.6f) continue;
            last = p.x;
            const float len = 4f;
            Gfx.Quad(go.transform, new Vector2(p.x, p.y - len / 2 - .12f), new Vector2(.09f, len), new Color(1, 1, 1, .5f), 2);
        }
        return go;
    }

    // Grind rail through control points (Catmull-Rom smoothed). One-way from above unless solid.
    static List<Vector2> Smooth(Vector2[] pts)
    {
        var smooth = new List<Vector2>();
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 p0 = pts[Mathf.Max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(pts.Length - 1, i + 2)];
            int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) * 2f));
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                smooth.Add(.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t));
            }
        }
        smooth.Add(pts[pts.Length - 1]);
        return smooth;
    }

    public GameObject Rail(bool oneWay, params Vector2[] pts)
    {
        var go = Go("rail", Vector2.zero);
        var smooth = Smooth(pts);
        foreach (var p in smooth) Grow(p, 1f);

        var e = go.AddComponent<EdgeCollider2D>();
        e.points = smooth.ToArray();
        e.edgeRadius = .08f;
        e.sharedMaterial = Mat(ref railMat, 0f, 0f);
        if (oneWay)
        {
            e.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.surfaceArc = 160f;
            eff.useOneWayGrouping = true;
        }
        AddTile(go, TileKind.Rail);

        var pts3 = new Vector3[smooth.Count];
        for (int i = 0; i < smooth.Count; i++) pts3[i] = smooth[i];
        Line(go, pts3, .26f, new Color(1f, .85f, .45f, .35f), 3);
        Line(go, pts3, .14f, Gfx.Gold, 4);
        for (int i = 0; i < pts.Length; i += Mathf.Max(1, pts.Length - 1))
            Gfx.Quad(go.transform, pts[i], Vector2.one * .32f, Gfx.Gold, 5, Gfx.Circle);
        return go;
    }

    // Short transparent glass tube the ball rolls through; pts trace its centreline.
    public GameObject Tube(params Vector2[] pts)
    {
        const float R = .78f;
        var go = Go("tube", Vector2.zero);
        var mid = Smooth(pts);
        int n = mid.Count;
        var lo = new Vector2[n];
        var hi = new Vector2[n];
        var mid3 = new Vector3[n];
        var shine = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 d = (mid[Mathf.Min(n - 1, i + 1)] - mid[Mathf.Max(0, i - 1)]).normalized;
            var nrm = new Vector2(-d.y, d.x);
            lo[i] = mid[i] - nrm * R;
            hi[i] = mid[i] + nrm * R;
            mid3[i] = mid[i];
            shine[i] = mid[i] + nrm * R * .55f;
            Grow(mid[i], 1.5f);
        }
        foreach (var wall in new[] { lo, hi })
        {
            var w = new GameObject("wall");
            w.transform.SetParent(go.transform, false);
            var e = w.AddComponent<EdgeCollider2D>();
            e.points = wall;
            e.edgeRadius = .05f;
            e.sharedMaterial = Mat(ref railMat, 0f, 0f);
            AddTile(w, TileKind.Tube);
            var w3 = new Vector3[n];
            for (int i = 0; i < n; i++) w3[i] = wall[i];
            Line(go, w3, .1f, new Color(1f, 1f, 1f, .75f), 26);
        }
        Line(go, mid3, R * 2f, new Color(.92f, .97f, 1f, .2f), 25);
        Line(go, shine, .08f, new Color(1f, 1f, 1f, .5f), 26);
        foreach (int i in new[] { 0, n - 1 })
            Line(go, new Vector3[] { lo[i], hi[i] }, .16f, new Color(1f, 1f, 1f, .85f), 27);
        return go;
    }

    static void Line(GameObject parent, Vector3[] pts, float width, Color c, int order)
    {
        var go = new GameObject("line");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = Gfx.SpriteMat;
        lr.useWorldSpace = true;
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = c;
        lr.numCapVertices = 6;
        lr.numCornerVertices = 4;
        lr.sortingOrder = order;
    }

    public GameObject Mover(float x, float y, float w, float dx, float dy, float period, float phase = 0f)
    {
        var go = Go("mover", new Vector2(x, y));
        Gfx.Slab(go.transform, new Vector2(0, -.12f), new Vector2(w, .6f), Gfx.PlatShade, 1);
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(w, .6f), Gfx.Plat, 2);
        Gfx.Quad(go.transform, Vector2.zero, new Vector2(.5f, .12f), Gfx.Cyan, 3, Gfx.Circle);
        var c = go.AddComponent<BoxCollider2D>();
        c.edgeRadius = .1f;
        c.size = new Vector2(w - .2f, .4f);
        c.sharedMaterial = Mat(ref platMat, .7f, 0f);
        var m = go.AddComponent<Mover>();
        m.a = new Vector2(x, y);
        m.b = new Vector2(x + dx, y + dy);
        m.period = period;
        m.phase = phase;
        AddTile(go, TileKind.Solid);
        Grow(m.a, 1f);
        Grow(m.b, 1f);
        return go;
    }

    public GameObject Spinner(float x, float y, float len, float speed, float angle = 0f)
    {
        var go = Go("spinner", new Vector2(x, y), angle);
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(len, .42f), Gfx.Coral, 7);
        Gfx.Quad(go.transform, Vector2.zero, new Vector2(len + 1.2f, 1.4f), new Color(1, .4f, .45f, .18f), 6, Gfx.Glow);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(len - .1f, .36f);
        go.AddComponent<Spinner>().speed = speed;
        AddTile(go, TileKind.Spinner);
        var hub = Go("hub", new Vector2(x, y));
        Gfx.Quad(hub.transform, Vector2.zero, Vector2.one * .5f, Color.white, 8, Gfx.Circle);
        Grow(new Vector2(x, y), len / 2);
        return go;
    }

    // Row of shards (spikes) centred at x, y = base; angle 0 points up.
    public void Shards(float x, float y, int count, float angle = 0f)
    {
        var q = Quaternion.Euler(0, 0, angle);
        for (int i = 0; i < count; i++)
        {
            Vector2 off = q * new Vector2((i - (count - 1) / 2f) * .7f, .3f);
            var go = Go("shard", new Vector2(x, y) + off, angle);
            Gfx.Quad(go.transform, Vector2.zero, new Vector2(.62f, .66f), Gfx.Coral, 4, Gfx.Tri);
            var pc = go.AddComponent<PolygonCollider2D>();
            pc.isTrigger = true;
            pc.points = new[] { new Vector2(-.25f, -.3f), new Vector2(.25f, -.3f), new Vector2(0, .28f) };
            AddTile(go, TileKind.Shard);
        }
    }

    public void Orb(float x, float y)
    {
        var go = Go("orb", new Vector2(x, y));
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 1.4f, new Color(1f, .85f, .4f, .45f), 8, Gfx.Glow);
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * .42f, Gfx.Gold, 9, Gfx.Circle);
        Gfx.Quad(go.transform, new Vector2(-.07f, .07f), Vector2.one * .14f, Color.white, 10, Gfx.Circle);
        var c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = .4f;
        AddTile(go, TileKind.Orb);
        orbs++;
    }

    public void Orbs(float x0, float y0, float x1, float y1, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? .5f : i / (float)(n - 1);
            Orb(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t));
        }
    }

    public void Check(float x, float y)
    {
        var go = Go("check", new Vector2(x, y));
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 2f, new Color(.3f, 1f, .75f, .35f), 8, Gfx.Glow);
        var t = AddTile(go, TileKind.Check);
        t.art = Gfx.Quad(go.transform, Vector2.zero, Vector2.one * .9f, Gfx.Mint, 9, Gfx.Ring);
        Gfx.Quad(go.transform, Vector2.zero, new Vector2(.42f, .12f), Gfx.Mint, 10);
        Gfx.Quad(go.transform, Vector2.zero, new Vector2(.12f, .42f), Gfx.Mint, 10);
        var c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = .6f;
    }

    public void Goal(float x, float y)
    {
        var go = Go("goal", new Vector2(x, y));
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 5f, new Color(1f, .85f, .5f, .45f), 7, Gfx.Glow);
        var t = AddTile(go, TileKind.Goal);
        t.art = Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 2.2f, Gfx.Gold, 9, Gfx.Ring);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            Gfx.Quad(t.art.transform, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .5f, Vector2.one * .06f, Color.white, 10, Gfx.Circle);
        }
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 1.5f, Color.white, 8, Gfx.Ring);
        var c = go.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = .8f;
        Grow(new Vector2(x, y), 2f);
    }

    // Launch pad; angle 0 launches straight up.
    public void Pad(float x, float y, float angle = 0f, float power = 20f)
    {
        var go = Go("pad", new Vector2(x, y), angle);
        Gfx.Quad(go.transform, new Vector2(0, .6f), new Vector2(2.4f, 2f), new Color(.3f, 1f, .75f, .3f), 1, Gfx.Glow);
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(1.5f, .4f), Gfx.Mint, 3);
        for (int i = 0; i < 2; i++)
            Gfx.Quad(go.transform, new Vector2(0, .45f + i * .3f), new Vector2(.5f, .18f), new Color(.3f, 1f, .75f, .8f - i * .3f), 3, Gfx.Tri);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(1.5f, .4f);
        c.sharedMaterial = Mat(ref padMat, .3f, 0f);
        var t = AddTile(go, TileKind.Pad);
        t.used = false;
        go.AddComponent<PadPower>().power = power;
    }

    // Vertical pulsing laser gate: red costs a heart, blue (push) only knocks the ball back.
    public void Gate(float x, float y0, float y1, float shift = 0f, bool push = false)
    {
        float h = y1 - y0;
        var go = Go("gate", new Vector2(x, (y0 + y1) / 2));
        Color col = push ? Gfx.Cyan : Gfx.Coral;
        var glow = Gfx.Quad(go.transform, Vector2.zero, new Vector2(1.2f, h + .6f), new Color(col.r, col.g, col.b, .25f), 5, Gfx.Glow);
        var t = AddTile(go, TileKind.Gate);
        t.push = push;
        t.glow = glow;
        t.gateShift = shift;
        t.art = Gfx.Quad(go.transform, Vector2.zero, new Vector2(.14f, h), col, 6);
        Gfx.Slab(go.transform, new Vector2(0, h / 2), new Vector2(.5f, .3f), Gfx.Ink, 7);
        Gfx.Slab(go.transform, new Vector2(0, -h / 2), new Vector2(.5f, .3f), Gfx.Ink, 7);
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(.3f, h);
    }

    public void Glass(float x, float y, float w, float h)
    {
        var go = Go("glass", new Vector2(x, y));
        var t = AddTile(go, TileKind.Glass);
        t.art = Gfx.Slab(go.transform, Vector2.zero, new Vector2(w, h), Gfx.Glass, 3);
        Gfx.Quad(go.transform, new Vector2(-w * .2f, h * .15f), new Vector2(.08f, h * .5f), new Color(1, 1, 1, .7f), 4);
        Gfx.Quad(go.transform, new Vector2(-w * .2f + .18f, h * .1f), new Vector2(.05f, h * .3f), new Color(1, 1, 1, .5f), 4);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(w, h);
        c.sharedMaterial = Mat(ref platMat, .7f, 0f);
    }

    public Tile Crystal(float x, float y)
    {
        var go = Go("crystal", new Vector2(x, y));
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 2.2f, new Color(.6f, .5f, 1f, .4f), 5, Gfx.Glow);
        var t = AddTile(go, TileKind.Crystal);
        t.art = Gfx.Quad(go.transform, Vector2.zero, Vector2.one * .75f, Gfx.Lilac, 6, Gfx.Rounded);
        go.AddComponent<CircleCollider2D>().radius = .45f;
        return t;
    }

    public Tile Door(float x, float y, float w, float h)
    {
        var go = Go("door", new Vector2(x, y));
        var t = AddTile(go, TileKind.Door);
        t.art = Gfx.Slab(go.transform, Vector2.zero, new Vector2(w, h), Gfx.Lilac, 3);
        for (float yy = -h / 2 + .6f; yy < h / 2 - .3f; yy += .8f)
            Gfx.Quad(go.transform, new Vector2(0, yy), new Vector2(w * .5f, .08f), new Color(1, 1, 1, .5f), 4);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(w, h);
        return t;
    }

    public void Turret(float x, float y, Vector2 dir, float period, float delay = 0f)
    {
        var go = Go("turret", new Vector2(x, y));
        Gfx.Slab(go.transform, new Vector2(0, -.1f), new Vector2(1.1f, 1.1f), Gfx.PlatShade, 4);
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(1.1f, 1.1f), Gfx.Plat, 5);
        var tu = go.AddComponent<Turret>();
        tu.dir = dir.normalized;
        tu.period = period;
        tu.delay = delay;
        tu.eye = Gfx.Quad(go.transform, dir.normalized * .18f, Vector2.one * .4f, Gfx.Coral, 6, Gfx.Circle);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(1.1f, 1.1f);
    }

    public void Box(float x, float y, float s = 1.2f)
    {
        var go = Go("box", new Vector2(x, y));
        Gfx.Slab(go.transform, Vector2.zero, new Vector2(s, s), new Color32(255, 214, 150, 255), 5);
        Gfx.Slab(go.transform, new Vector2(0, .08f), new Vector2(s - .25f, s - .35f), new Color32(255, 230, 185, 255), 6);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(s - .1f, s - .1f);
        c.edgeRadius = .05f;
        c.sharedMaterial = Mat(ref boxMat, .5f, 0f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 2.5f;
        rb.gravityScale = 3f;
    }

    public void Seesaw(float x, float y, float len)
    {
        var baseGo = Go("seesaw-base", new Vector2(x, y - .75f));
        Gfx.Quad(baseGo.transform, Vector2.zero, new Vector2(1.1f, 1.1f), Gfx.PlatShade, 2, Gfx.Tri);
        var plank = Go("seesaw", new Vector2(x, y));
        Gfx.Slab(plank.transform, new Vector2(0, -.08f), new Vector2(len, .45f), Gfx.PlatShade, 4);
        Gfx.Slab(plank.transform, Vector2.zero, new Vector2(len, .45f), Gfx.Plat, 5);
        Gfx.Quad(plank.transform, Vector2.zero, Vector2.one * .25f, Gfx.Cyan, 6, Gfx.Circle);
        var c = plank.AddComponent<BoxCollider2D>();
        c.size = new Vector2(len, .45f);
        c.sharedMaterial = Mat(ref platMat, .7f, 0f);
        var rb = plank.AddComponent<Rigidbody2D>();
        rb.mass = 3f;
        rb.gravityScale = 3f;
        var hinge = plank.AddComponent<HingeJoint2D>();
        hinge.autoConfigureConnectedAnchor = false;
        hinge.anchor = Vector2.zero;
        hinge.connectedAnchor = new Vector2(x, y);
        hinge.useLimits = true;
        hinge.limits = new JointAngleLimits2D { min = -25, max = 25 };
    }

    public global::Boss Rival(float x, float y, Vector2 arenaMin, Vector2 arenaMax)
    {
        var b = global::Boss.Create(new Vector2(x, y), root);
        b.arenaMin = arenaMin;
        b.arenaMax = arenaMax;
        return b;
    }

    public void Label(float x, float y, string text)
    {
        GM.AddSign(new Vector2(x, y), text);
    }
}
