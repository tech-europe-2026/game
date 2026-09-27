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
        { "dash", new[] { KeyCode.D } },
        { "slam", new[] { KeyCode.F } },
        { "hover", new[] { KeyCode.Q } },
        { "grow", new[] { KeyCode.G } },
        { "teleport", new[] { KeyCode.T } },
        { "parry", new[] { KeyCode.J } },
        { "camo", new[] { KeyCode.V } },
        { "phase", new[] { KeyCode.V } },
        { "reverse", new[] { KeyCode.E } },
        { "climb", new[] { KeyCode.C, KeyCode.L } },
    };

    public static readonly string[] TouchButtons = { "crouch", "dash", "reverse", "slam", "hover", "climb", "grow", "parry", "phase", "teleport", "camo" };

    public struct Button
    {
        public string id;
        public Vector2 center;
        public float radius;
    }

    public readonly List<Button> buttons = new List<Button>();
    public Vector2 leftPad, rightPad;
    public float padRadius;
    public bool gameplay;

    readonly HashSet<string> held = new HashSet<string>(), prevHeld = new HashSet<string>();

    public float Unit => Mathf.Min(Screen.height, Screen.width) / 720f;

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
            if (Input.GetKey(KeyCode.LeftArrow)) x -= 1;
            if (Input.GetKey(KeyCode.RightArrow)) x += 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1;
            if (I != null)
            {
                if (I.held.Contains("left")) x -= 1;
                if (I.held.Contains("right")) x += 1;
                if (I.held.Contains("climb")) y += 1;
                if (I.held.Contains("crouch")) y -= 1;
            }
            return new Vector2(Mathf.Clamp(x, -1, 1), Mathf.Clamp(y, -1, 1));
        }
    }

    public static bool Crouch => Move.y < -.6f;

    public void Layout(ICollection<string> unlocked)
    {
        buttons.Clear();
        float u = Unit, W = Screen.width, H = Screen.height;
        padRadius = 82 * u;
        leftPad = new Vector2(120 * u, H - 120 * u);
        rightPad = new Vector2(310 * u, H - 120 * u);
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
        if (!Touch || !gameplay) return;

        float split = (leftPad.x + rightPad.x) / 2;
        for (int t = 0; t < Input.touchCount; t++)
        {
            var touch = Input.GetTouch(t);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;
            var p = new Vector2(touch.position.x, Screen.height - touch.position.y);

            if (p.x < Screen.width * .4f && p.y > Screen.height * .35f)
            {
                held.Add(p.x < split ? "left" : "right");
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
    }
}
