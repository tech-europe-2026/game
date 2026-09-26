using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Controls : MonoBehaviour
{
    public static Controls I;
    public static bool Touch { get; private set; }
    public static bool Portrait => Touch && Screen.height > Screen.width;

    static readonly Dictionary<string, KeyCode[]> keys = new Dictionary<string, KeyCode[]>
    {
        { "jump", new[] { KeyCode.Space, KeyCode.W, KeyCode.UpArrow } },
        { "dash", new[] { KeyCode.LeftShift, KeyCode.RightShift } },
        { "grow", new[] { KeyCode.G } },
        { "teleport", new[] { KeyCode.T } },
        { "parry", new[] { KeyCode.Q, KeyCode.J } },
        { "camo", new[] { KeyCode.V } },
        { "reverse", new[] { KeyCode.E } },
        { "climb", new[] { KeyCode.C, KeyCode.L } },
    };

    public static readonly string[] TouchButtons = { "dash", "reverse", "climb", "grow", "parry", "teleport", "camo" };

    public struct Button
    {
        public string id;
        public Vector2 center;
        public float radius;
    }

    public readonly List<Button> buttons = new List<Button>();
    public Vector2 stickOrigin, stickPos;
    public bool stickActive;
    public bool gameplay;

    readonly HashSet<string> held = new HashSet<string>(), prevHeld = new HashSet<string>();
    int stickFinger = -1;
    Vector2 stickVec;

    public float Unit => Mathf.Min(Screen.height, Screen.width) / 720f;
    public float StickRadius => 70f * Unit;

    void Awake()
    {
        I = this;
        Input.simulateMouseWithTouches = true;
        Touch = Application.isMobilePlatform;
    }

    public static bool Held(string id)
    {
        if (I != null && I.held.Contains(id)) return true;
        if (!keys.TryGetValue(id, out var ks)) return false;
        foreach (var k in ks) if (Input.GetKey(k)) return true;
        return false;
    }

    public static bool Pressed(string id)
    {
        if (I != null && I.held.Contains(id) && !I.prevHeld.Contains(id)) return true;
        if (!keys.TryGetValue(id, out var ks)) return false;
        foreach (var k in ks) if (Input.GetKeyDown(k)) return true;
        return false;
    }

    public static bool Released(string id)
    {
        if (I != null && !I.held.Contains(id) && I.prevHeld.Contains(id)) return true;
        if (!keys.TryGetValue(id, out var ks)) return false;
        foreach (var k in ks) if (Input.GetKeyUp(k)) return true;
        return false;
    }

    public static Vector2 Move
    {
        get
        {
            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1;
            if (I != null && I.stickActive)
            {
                x = Mathf.Clamp(x + I.stickVec.x, -1, 1);
                y = Mathf.Clamp(y + I.stickVec.y, -1, 1);
            }
            return new Vector2(x, y);
        }
    }

    public static bool Crouch => Move.y < -.6f;

    public void Layout(ICollection<string> unlocked)
    {
        buttons.Clear();
        float u = Unit, W = Screen.width, H = Screen.height;
        var jump = new Vector2(W - 130 * u, H - 130 * u);
        buttons.Add(new Button { id = "jump", center = jump, radius = 78 * u });
        var list = new List<string>();
        foreach (var id in TouchButtons) if (unlocked.Contains(id)) list.Add(id);
        int inner = Mathf.Min(3, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            bool ring1 = i < inner;
            int n = ring1 ? inner : list.Count - inner, k = ring1 ? i : i - inner;
            float r = (ring1 ? 165 : 265) * u;
            float a0 = 180f, a1 = 90f;
            float a = n == 1 ? 135f : Mathf.Lerp(a0, a1, k / (float)(n - 1));
            if (ring1 && n == 2) a = k == 0 ? 165f : 105f;
            var c = jump + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), -Mathf.Sin(a * Mathf.Deg2Rad)) * r;
            buttons.Add(new Button { id = list[i], center = c, radius = 48 * u });
        }
        buttons.Add(new Button { id = "restart", center = new Vector2(W - 48 * u, 120 * u), radius = 26 * u });
    }

    void Update()
    {
        prevHeld.Clear();
        foreach (var h in held) prevHeld.Add(h);
        held.Clear();
        if (Input.touchCount > 0) Touch = true;
        if (!Touch || !gameplay) { stickActive = false; stickFinger = -1; stickVec = Vector2.zero; return; }

        bool stickSeen = false;
        for (int t = 0; t < Input.touchCount; t++)
        {
            var touch = Input.GetTouch(t);
            var p = new Vector2(touch.position.x, Screen.height - touch.position.y);
            bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;

            if (touch.fingerId == stickFinger)
            {
                if (ended) continue;
                stickSeen = true;
                Vector2 d = p - stickOrigin;
                float max = StickRadius;
                if (d.magnitude > max)
                {
                    stickOrigin += d.normalized * (d.magnitude - max);
                    d = d.normalized * max;
                }
                stickPos = stickOrigin + d;
                Vector2 v = new Vector2(d.x, -d.y) / max;
                float ax = Mathf.Abs(v.x) < .18f ? 0 : Mathf.Sign(v.x) * Mathf.Clamp01((Mathf.Abs(v.x) - .18f) / .55f);
                stickVec = new Vector2(ax, v.y);
                continue;
            }
            if (ended) continue;

            if (p.x < Screen.width * .42f)
            {
                if (stickFinger < 0 && touch.phase == TouchPhase.Began)
                {
                    stickFinger = touch.fingerId;
                    stickOrigin = stickPos = p;
                    stickVec = Vector2.zero;
                    stickSeen = true;
                }
                continue;
            }

            float best = float.MaxValue;
            string hit = null;
            foreach (var b in buttons)
            {
                float d = Vector2.Distance(p, b.center);
                if (d < b.radius * 1.25f && d < best) { best = d; hit = b.id; }
            }
            if (hit != null) held.Add(hit);
        }
        if (!stickSeen) { stickFinger = -1; stickVec = Vector2.zero; }
        stickActive = stickFinger >= 0;
    }
}
