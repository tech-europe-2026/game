using UnityEngine;

public static class LevelBuilder
{
    public struct Result
    {
        public Transform root;
        public Vector2 start;
        public Vector2 min, max;
        public int coins;
    }

    static PhysicsMaterial2D groundMat, iceMat, padMat;

    static PhysicsMaterial2D Mat(ref PhysicsMaterial2D m, float friction, float bounce)
    {
        if (m == null) m = new PhysicsMaterial2D { friction = friction, bounciness = bounce };
        return m;
    }

    public static Result Build(string[] map)
    {
        var res = new Result { root = new GameObject("Level").transform };
        int h = map.Length;
        int w = 0;
        foreach (var r in map) w = Mathf.Max(w, r.Length);
        char At(int x, int y)
        {
            if (y < 0 || y >= h) return ' ';
            string row = map[h - 1 - y];
            return x >= 0 && x < row.Length ? row[x] : ' ';
        }
        res.min = new Vector2(0, 0);
        res.max = new Vector2(w - 1, h - 1);

        var solids = NewComposite(res.root, "Solids", Mat(ref groundMat, .6f, 0f));

        for (int y = 0; y < h; y++)
        {
            int x = 0;
            while (x < w)
            {
                char c = At(x, y);
                if (c == '#')
                {
                    int x0 = x;
                    while (At(x, y) == '#') x++;
                    AddRun(solids, x0, x - 1, y, At);
                    continue;
                }
                Vector2 p = new Vector2(x, y);
                switch (c)
                {
                    case 'S': res.start = p; break;
                    case '^': Spike(res.root, p); break;
                    case '~': Lava(res.root, p); break;
                    case 'C': Crate(res.root, p); break;
                    case 'X': Cracked(res.root, p); break;
                    case 'G': Glass(res.root, p); break;
                    case 'L': Laser(res.root, p, At); break;
                    case '*': Pickup(res.root, p, TileKind.Coin); res.coins++; break;
                    case 'H': Pickup(res.root, p, TileKind.Heal); break;
                    case 'F': Portal(res.root, p); break;
                    case 'B': Pad(res.root, p); break;
                    case 'P': Seesaw(res.root, p); break;
                    case 'O': Pendulum(res.root, p); break;
                    case 'D': Domino(res.root, p); break;
                }
                x++;
            }
        }
        return res;
    }

    static Transform NewComposite(Transform root, string name, PhysicsMaterial2D mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var comp = go.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
        comp.sharedMaterial = mat;
        return go.transform;
    }

    static void AddRun(Transform parent, int x0, int x1, int y, System.Func<int, int, char> at)
    {
        float cx = (x0 + x1) / 2f;
        int len = x1 - x0 + 1;
        var box = parent.gameObject.AddComponent<BoxCollider2D>();
        box.offset = new Vector2(cx, y);
        box.size = new Vector2(len, 1);
        box.usedByComposite = true;
        for (int x = x0; x <= x1; x++)
        {
            float shade = ((x * 7 + y * 13) % 5) * .012f;
            var col = Gfx.Ground + new Color(shade, shade, shade * 2, 0);
            Gfx.Quad(parent, new Vector2(x, y), Vector2.one, col, 0);
            if ((x * 31 + y * 17) % 4 == 0)
                Gfx.Quad(parent, new Vector2(x - .2f, y - .15f), new Vector2(.25f, .25f), new Color32(22, 32, 66, 255), 1);
            if (at(x, y + 1) != '#')
            {
                Gfx.Quad(parent, new Vector2(x, y + .44f), new Vector2(1, .12f), Gfx.GroundTop, 2);
                var g = Gfx.Quad(parent, new Vector2(x, y + .5f), new Vector2(1.6f, .8f), new Color(Gfx.GroundTop.r, Gfx.GroundTop.g, Gfx.GroundTop.b, .12f), 3, Gfx.Glow);
                g.transform.localScale = new Vector3(1.8f, .9f, 1);
            }
            if (at(x, y - 1) != '#')
                Gfx.Quad(parent, new Vector2(x, y - .46f), new Vector2(1, .08f), new Color32(18, 26, 56, 255), 2);
        }
    }

    static Tile TileGo(Transform root, string name, Vector2 p, TileKind kind)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = p;
        var t = go.AddComponent<Tile>();
        t.kind = kind;
        return t;
    }

    static void Spike(Transform root, Vector2 p)
    {
        var t = TileGo(root, "spike", p, TileKind.Spike);
        for (int i = 0; i < 2; i++)
            Gfx.Quad(t.transform, new Vector2(-.25f + i * .5f, -.2f), new Vector2(.5f, .6f), Gfx.Spike, 4, Gfx.SpikeSprite);
        Gfx.Quad(t.transform, new Vector2(0, -.1f), new Vector2(1.4f, 1f), new Color(1, 0, .3f, .15f), 3, Gfx.Glow);
        var c = t.gameObject.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(.8f, .45f);
        c.offset = new Vector2(0, -.25f);
    }

    static void Lava(Transform root, Vector2 p)
    {
        var t = TileGo(root, "lava", p, TileKind.Lava);
        t.art = Gfx.Quad(t.transform, Vector2.zero, Vector2.one, Gfx.Lava, 4);
        Gfx.Quad(t.transform, new Vector2(0, .5f), new Vector2(2f, 1.4f), new Color(1, .35f, 0, .2f), 3, Gfx.Glow);
        var c = t.gameObject.AddComponent<BoxCollider2D>();
        c.sharedMaterial = Mat(ref iceMat, 0f, 0f);
    }

    static void Crate(Transform root, Vector2 p)
    {
        var t = TileGo(root, "crate", p, TileKind.Crate);
        t.transform.localScale = Vector3.one * .96f;
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one, Gfx.CrateEdge, 4);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one * .78f, Gfx.Crate, 5);
        var d = Gfx.Quad(t.transform, Vector2.zero, new Vector2(1f, .14f), Gfx.CrateEdge, 6);
        d.transform.localRotation = Quaternion.Euler(0, 0, 45);
        t.gameObject.AddComponent<BoxCollider2D>().sharedMaterial = Mat(ref groundMat, .6f, 0f);
        var rb = t.gameObject.AddComponent<Rigidbody2D>();
        rb.mass = .8f;
        rb.gravityScale = 3f;
    }

    static void Cracked(Transform root, Vector2 p)
    {
        var t = TileGo(root, "cracked", p, TileKind.Cracked);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one, Gfx.Cracked, 4);
        Gfx.Quad(t.transform, new Vector2(-.1f, .1f), new Vector2(.08f, .7f), new Color32(40, 30, 30, 255), 5).transform.localRotation = Quaternion.Euler(0, 0, 25);
        Gfx.Quad(t.transform, new Vector2(.2f, -.2f), new Vector2(.4f, .07f), new Color32(40, 30, 30, 255), 5).transform.localRotation = Quaternion.Euler(0, 0, -20);
        Gfx.Quad(t.transform, new Vector2(0, .44f), new Vector2(1, .12f), Gfx.CrateEdge, 6);
        t.gameObject.AddComponent<BoxCollider2D>().sharedMaterial = Mat(ref groundMat, .6f, 0f);
    }

    static void Glass(Transform root, Vector2 p)
    {
        var t = TileGo(root, "glass", p, TileKind.Solid);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one, new Color(Gfx.Glass.r, Gfx.Glass.g, Gfx.Glass.b, .55f), 4);
        Gfx.Quad(t.transform, new Vector2(-.2f, .2f), new Vector2(.1f, .5f), new Color(1, 1, 1, .5f), 5).transform.localRotation = Quaternion.Euler(0, 0, -30);
        t.gameObject.AddComponent<BoxCollider2D>();
    }

    static void Laser(Transform root, Vector2 p, System.Func<int, int, char> at)
    {
        int len = 0;
        int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
        while (y - len >= 0 && at(x, y - len) != '#' && len < 30) len++;
        var t = TileGo(root, "laser", p, TileKind.Laser);
        float centerY = -(len - 1) / 2f;
        t.art = Gfx.Quad(t.transform, new Vector2(0, centerY), new Vector2(.22f, len), Gfx.Spike, 6);
        Gfx.Quad(t.transform, new Vector2(0, centerY), new Vector2(.08f, len), Color.white, 7);
        var glow = Gfx.Quad(t.transform, new Vector2(0, centerY), new Vector2(1.2f, len + 1), new Color(1, 0, .3f, .25f), 5, Gfx.Glow);
        glow.transform.localScale = new Vector3(1.2f, len + 1, 1);
        Gfx.Quad(t.transform, new Vector2(0, .3f), new Vector2(.7f, .4f), new Color32(95, 87, 79, 255), 8);
        var c = t.gameObject.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(.3f, len);
        c.offset = new Vector2(0, centerY);
    }

    static void Pickup(Transform root, Vector2 p, TileKind kind)
    {
        var t = TileGo(root, kind.ToString(), p, kind);
        if (kind == TileKind.Coin)
        {
            Gfx.Quad(t.transform, Vector2.zero, Vector2.one, Gfx.Gold, 10, Gfx.Circle);
            Gfx.Quad(t.transform, Vector2.zero, new Vector2(.35f, .6f), new Color32(255, 163, 0, 255), 11);
            Gfx.Quad(t.transform, Vector2.zero, Vector2.one * 3f, new Color(1, .9f, .2f, .25f), 9, Gfx.Glow);
        }
        else
        {
            Gfx.Quad(t.transform, Vector2.zero, Vector2.one * .8f, Gfx.Green, 10, Gfx.Circle);
            Gfx.Quad(t.transform, Vector2.zero, new Vector2(.5f, .14f), Color.white, 11);
            Gfx.Quad(t.transform, Vector2.zero, new Vector2(.14f, .5f), Color.white, 11);
            Gfx.Quad(t.transform, Vector2.zero, Vector2.one * 2.5f, new Color(0, 1, .3f, .3f), 9, Gfx.Glow);
        }
        var c = t.gameObject.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = .45f;
    }

    static void Portal(Transform root, Vector2 p)
    {
        var t = TileGo(root, "portal", p, TileKind.Portal);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one * 2.2f, new Color(1, .47f, .66f, .5f), 8, Gfx.Glow);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one, Gfx.Pink, 9, Gfx.Ring);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one * .7f, Gfx.Gold, 9, Gfx.Ring);
        Gfx.Quad(t.transform, Vector2.zero, Vector2.one * .35f, Color.white, 10, Gfx.Circle);
        Gfx.Quad(t.transform, new Vector2(.4f, 0), new Vector2(.12f, .12f), Color.white, 10);
        Gfx.Quad(t.transform, new Vector2(-.4f, 0), new Vector2(.12f, .12f), Color.white, 10);
        var c = t.gameObject.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = .4f;
    }

    static void Pad(Transform root, Vector2 p)
    {
        var t = TileGo(root, "pad", p, TileKind.Pad);
        Gfx.Quad(t.transform, new Vector2(0, -.3f), new Vector2(1, .4f), new Color32(95, 87, 79, 255), 4);
        t.art = Gfx.Quad(t.transform, new Vector2(0, -.02f), new Vector2(1, .18f), Gfx.Green, 5);
        Gfx.Quad(t.transform, new Vector2(0, .3f), new Vector2(2f, 1.4f), new Color(0, 1, .3f, .25f), 3, Gfx.Glow);
        var c = t.gameObject.AddComponent<BoxCollider2D>();
        c.size = new Vector2(1, .6f);
        c.offset = new Vector2(0, -.2f);
        c.sharedMaterial = Mat(ref padMat, .4f, 0f);
    }

    static void Seesaw(Transform root, Vector2 p)
    {
        var baseT = TileGo(root, "seesaw-base", p + Vector2.down * .9f, TileKind.Solid);
        Gfx.Quad(baseT.transform, Vector2.zero, new Vector2(.6f, .9f), new Color32(95, 87, 79, 255), 4, Gfx.SpikeSprite);
        var bc = baseT.gameObject.AddComponent<PolygonCollider2D>();
        bc.points = new[] { new Vector2(-.3f, -.45f), new Vector2(.3f, -.45f), new Vector2(0, .45f) };

        var plank = new GameObject("seesaw");
        plank.transform.SetParent(root, false);
        plank.transform.position = p;
        Gfx.Quad(plank.transform, Vector2.zero, new Vector2(7f, .35f), Gfx.CrateEdge, 5);
        Gfx.Quad(plank.transform, Vector2.zero, new Vector2(6.8f, .18f), Gfx.Crate, 6);
        var col = plank.AddComponent<BoxCollider2D>();
        col.size = new Vector2(7f, .35f);
        col.sharedMaterial = Mat(ref groundMat, .6f, 0f);
        var rb = plank.AddComponent<Rigidbody2D>();
        rb.mass = 2f;
        rb.gravityScale = 3f;
        var hinge = plank.AddComponent<HingeJoint2D>();
        hinge.autoConfigureConnectedAnchor = false;
        hinge.anchor = Vector2.zero;
        hinge.connectedAnchor = p;
        hinge.useLimits = true;
        hinge.limits = new JointAngleLimits2D { min = -22, max = 22 };
    }

    static void Pendulum(Transform root, Vector2 p)
    {
        var pivot = new GameObject("pivot");
        pivot.transform.SetParent(root, false);
        pivot.transform.position = p;
        Gfx.Quad(pivot.transform, Vector2.zero, Vector2.one * .5f, new Color32(95, 87, 79, 255), 6, Gfx.Circle);
        var prb = pivot.AddComponent<Rigidbody2D>();
        prb.bodyType = RigidbodyType2D.Static;

        const float length = 6f;
        var ball = new GameObject("wrecking-ball");
        ball.transform.SetParent(root, false);
        ball.transform.position = p + new Vector2(length * .7f, -length * .7f);
        Gfx.Quad(ball.transform, Vector2.zero, Vector2.one * 1.6f, new Color32(95, 87, 79, 255), 7, Gfx.Circle);
        Gfx.Quad(ball.transform, new Vector2(-.3f, .3f), Vector2.one * .4f, new Color32(194, 195, 199, 255), 8, Gfx.Circle);
        Gfx.Quad(ball.transform, Vector2.zero, Vector2.one * 3f, new Color(1, 0, .3f, .2f), 6, Gfx.Glow);
        var chain = ball.AddComponent<LineRenderer>();
        chain.material = Gfx.SpriteMat;
        chain.startColor = chain.endColor = new Color32(194, 195, 199, 255);
        chain.startWidth = chain.endWidth = .12f;
        chain.positionCount = 2;
        chain.sortingOrder = 5;
        ball.AddComponent<CircleCollider2D>().radius = .8f;
        var rb = ball.AddComponent<Rigidbody2D>();
        rb.mass = 10f;
        rb.gravityScale = 2f;
        rb.angularDrag = 0f;
        rb.drag = 0f;
        var j = ball.AddComponent<DistanceJoint2D>();
        j.connectedBody = prb;
        j.autoConfigureDistance = false;
        j.distance = length;
        j.maxDistanceOnly = false;
        ball.AddComponent<ChainLine>().pivot = pivot.transform;
    }

    static void Domino(Transform root, Vector2 p)
    {
        var go = new GameObject("domino");
        go.transform.SetParent(root, false);
        go.transform.position = p + Vector2.up * .75f;
        Gfx.Quad(go.transform, Vector2.zero, new Vector2(.35f, 2.5f), Gfx.Pink, 5);
        Gfx.Quad(go.transform, new Vector2(0, .6f), new Vector2(.15f, .15f), Color.white, 6);
        Gfx.Quad(go.transform, new Vector2(0, -.6f), new Vector2(.15f, .15f), Color.white, 6);
        go.AddComponent<BoxCollider2D>().size = new Vector2(.35f, 2.5f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = .4f;
        rb.gravityScale = 3f;
    }
}
