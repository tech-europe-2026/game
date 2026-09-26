using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoBehaviour
{
    enum Mode { Title, Playing, LevelDone, Won }

    public static GM I;
    static readonly HashSet<string> unlocked = new HashSet<string>();
    static readonly List<(Vector2 p, string text)> signs = new List<(Vector2, string)>();

    public int orbs;
    public Vector2 checkpoint;
    public Transform LevelRoot => level != null ? level.root : null;

    Mode mode = Mode.Title;
    int levelIndex;
    Ball ball;
    CameraFollow camFollow;
    Camera cam;
    LevelBuilder level;
    float levelTime, totalTime;
    int deaths, totalDeaths, totalOrbs, totalOrbsMax;
    string newStates;
    float bannerT, uiT;

    public struct Ability
    {
        public string id, sprite, key, label;
        public Ability(string id, string sprite, string key, string label) { this.id = id; this.sprite = sprite; this.key = key; this.label = label; }
    }

    public static readonly Ability[] Abilities =
    {
        new Ability("jump", "bounce", "SPACE", "JUMP"),
        new Ability("crouch", "crouch", "S", "CROUCH"),
        new Ability("dash", "dash", "SHIFT", "DASH"),
        new Ability("reverse", "reverse", "E", "REVERSE"),
        new Ability("climb", "climb", "HOLD C", "CLIMB"),
        new Ability("grow", "grow", "G", "GROW"),
        new Ability("parry", "parry", "Q", "PARRY"),
        new Ability("teleport", "teleport", "T", "BLINK"),
        new Ability("camo", "camouflage", "V", "CAMO"),
    };

    static readonly string[] AllStates =
        { "idle", "spin", "bounce", "teleport", "grow", "crouch", "stun", "heal", "dash", "climb", "camouflage", "reverse", "parry", "evolve" };

    public static bool Has(string id) => unlocked.Contains(id);
    public static void AddSign(Vector2 p, string text) => signs.Add((p, text));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        new GameObject("GM").AddComponent<GM>();
    }

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Physics2D.gravity = new Vector2(0, -9.81f);
        gameObject.AddComponent<Fx>();
        Sfx.Init(gameObject);
        cam = Camera.main;
        if (cam == null)
        {
            var cgo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = cgo.AddComponent<Camera>();
            cgo.AddComponent<AudioListener>();
        }
        camFollow = cam.gameObject.AddComponent<CameraFollow>();
        LoadLevel(0);
        mode = Mode.Title;
        ball.controlLocked = true;
    }

    void LoadLevel(int idx)
    {
        levelIndex = idx;
        if (level != null) Destroy(level.root.gameObject);
        if (ball != null) Destroy(ball.gameObject);
        signs.Clear();
        var def = Levels.All[idx];
        level = new LevelBuilder();
        def.build(level);
        checkpoint = level.start;
        ball = Ball.Create(level.start);
        camFollow.Follow(ball, level.min, level.max);
        orbs = 0;
        deaths = 0;
        levelTime = 0;
        var fresh = new List<string>();
        foreach (var a in def.unlock.Split(','))
        {
            string id = a.Trim();
            if (id.Length > 0 && unlocked.Add(id)) fresh.Add(id);
        }
        newStates = string.Join(",", fresh);
        bannerT = 0;
    }

    void Update()
    {
        uiT += Time.unscaledDeltaTime;
        bannerT += Time.unscaledDeltaTime;
        switch (mode)
        {
            case Mode.Title:
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    mode = Mode.Playing;
                    ball.controlLocked = false;
                    bannerT = 0;
                    Sfx.Play("heal");
                }
                break;
            case Mode.Playing:
                levelTime += Time.deltaTime;
                if (!ball.dead)
                {
                    float y = ball.rb.position.y;
                    if (y < level.min.y - 9f || y > level.max.y + 9f) ball.Die();
                }
                if (Input.GetKeyDown(KeyCode.R)) LoadLevel(levelIndex);
                if (Input.GetKeyDown(KeyCode.N)) StartCoroutine(Advance(0f));
                break;
            case Mode.Won:
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    unlocked.Clear();
                    totalTime = 0; totalDeaths = 0; totalOrbs = 0; totalOrbsMax = 0;
                    LoadLevel(0);
                    mode = Mode.Playing;
                }
                break;
        }
    }

    public void OnBallDied()
    {
        deaths++;
        StartCoroutine(RespawnCo());
    }

    IEnumerator RespawnCo()
    {
        yield return new WaitForSeconds(.8f);
        if (ball != null && ball.dead) ball.Respawn(checkpoint + Vector2.up * .3f);
    }

    public void OnGoal(Vector2 p)
    {
        if (mode != Mode.Playing) return;
        StartCoroutine(GoalCo(p));
    }

    IEnumerator GoalCo(Vector2 p)
    {
        mode = Mode.LevelDone;
        ball.controlLocked = true;
        ball.Evolve();
        Sfx.Play("evolve");
        Vector3 start = ball.transform.position;
        for (float t = 0; t < .6f; t += Time.deltaTime)
        {
            float k = t / .6f;
            ball.transform.position = Vector3.Lerp(start, p, 1 - (1 - k) * (1 - k));
            yield return null;
        }
        Fx.Ring(p, Gfx.Gold, 3f);
        Fx.Ring(p, Color.white, 4.5f);
        Fx.Burst(p, Gfx.Gold, 40, 12f, .2f, 0f, 1f);
        Fx.Burst(p, Color.white, 30, 9f, .16f, 0f, 1f);
        Fx.AddShake(.4f);
        for (float t = 0; t < .9f; t += Time.deltaTime)
        {
            ball.transform.localScale = Vector3.one * (1f + Mathf.Sin(t / .9f * Mathf.PI) * .5f);
            yield return null;
        }
        totalTime += levelTime;
        totalDeaths += deaths;
        totalOrbs += orbs;
        totalOrbsMax += level.orbs;
        yield return Advance(1.4f);
    }

    IEnumerator Advance(float wait)
    {
        mode = Mode.LevelDone;
        yield return new WaitForSeconds(wait);
        if (levelIndex + 1 < Levels.All.Length)
        {
            LoadLevel(levelIndex + 1);
            mode = Mode.Playing;
            Sfx.Play("heal");
        }
        else
        {
            mode = Mode.Won;
            ball.controlLocked = true;
        }
    }

    // ---------- HUD ----------
    GUIStyle hero, h1, h2, body, keycap;
    static Texture2D white;

    void Styles()
    {
        if (hero != null) return;
        white = Texture2D.whiteTexture;
        hero = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        h1 = new GUIStyle(hero);
        h2 = new GUIStyle(hero) { fontStyle = FontStyle.Normal };
        body = new GUIStyle(hero) { fontStyle = FontStyle.Normal };
        keycap = new GUIStyle(hero);
    }

    static void Text(Rect r, string s, GUIStyle st, Color c, int size)
    {
        st.fontSize = Mathf.Max(8, size);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = old;
    }

    static void Pill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white, ScaleMode.StretchToFill, true, 0, c, 0, r.height / 2);
        GUI.color = old;
    }

    static void Icon(Rect r, string state, float alpha = 1f)
    {
        var s = Gfx.Ball(state);
        if (s == null) return;
        var old = GUI.color;
        GUI.color = new Color(1, 1, 1, alpha);
        GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
        GUI.color = old;
    }

    Ability Find(string id)
    {
        foreach (var a in Abilities) if (a.id == id) return a;
        return Abilities[0];
    }

    float Cooldown(string id)
    {
        switch (id)
        {
            case "dash": return Mathf.Clamp01(ball.dashCd / Ball.DashCd);
            case "teleport": return Mathf.Clamp01(ball.teleCd / Ball.TeleCd);
            case "parry": return Mathf.Clamp01(ball.parryCd / Ball.ParryCd);
            case "camo": return Mathf.Clamp01(ball.camoCd / Ball.CamoCd);
            case "reverse": return Mathf.Clamp01(ball.flipCd / Ball.FlipCd);
            case "climb": return 1f - Mathf.Clamp01(ball.climbStamina / Ball.ClimbMax);
            default: return 0f;
        }
    }

    void OnGUI()
    {
        Styles();
        float W = Screen.width, H = Screen.height, u = H / 720f;
        var ink = (Color)Gfx.Ink;

        if (mode != Mode.Title && cam != null)
        {
            foreach (var (p, text) in signs)
            {
                Vector3 sp = cam.WorldToScreenPoint(p);
                if (sp.x < -200 || sp.x > W + 200) continue;
                Text(new Rect(sp.x - 300 * u, H - sp.y - 20 * u, 600 * u, 40 * u), text, body, new Color(ink.r, ink.g, ink.b, .75f), (int)(20 * u));
            }
        }

        if (mode == Mode.Title) { DrawTitle(W, H, u, ink); return; }
        if (mode == Mode.Won) { DrawWon(W, H, u, ink); return; }

        // top-left: level + hearts + orbs
        var def = Levels.All[levelIndex];
        Pill(new Rect(24 * u, 22 * u, 330 * u, 56 * u), new Color(1, 1, 1, .7f));
        Text(new Rect(44 * u, 24 * u, 60 * u, 52 * u), (levelIndex + 1).ToString("00"), h1, ink, (int)(26 * u));
        Box(new Rect(100 * u, 36 * u, 2 * u, 28 * u), new Color(ink.r, ink.g, ink.b, .25f));
        for (int i = 0; i < Ball.MaxHearts; i++)
        {
            bool full = i < ball.hearts;
            Pill(new Rect((118 + i * 26) * u, 41 * u, 18 * u, 18 * u), full ? (Color)Gfx.Coral : new Color(ink.r, ink.g, ink.b, .15f));
        }
        Pill(new Rect(208 * u, 41 * u, 18 * u, 18 * u), Gfx.Gold);
        Text(new Rect(232 * u, 24 * u, 110 * u, 52 * u), orbs + " / " + level.orbs, h2, ink, (int)(22 * u));

        // top-right: time
        Text(new Rect(W - 260 * u, 22 * u, 236 * u, 30 * u), def.name, h1, ink, (int)(20 * u));
        Text(new Rect(W - 260 * u, 50 * u, 236 * u, 24 * u), levelTime.ToString("0.0") + "s   ·   " + deaths + " falls", body, new Color(ink.r, ink.g, ink.b, .6f), (int)(16 * u));

        // bottom: unlocked states
        var list = new List<Ability>();
        foreach (var a in Abilities) if (Has(a.id)) list.Add(a);
        float cw = 84 * u, gap = 10 * u, total = list.Count * cw + (list.Count - 1) * gap;
        float x0 = (W - total) / 2, y0 = H - 118 * u;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            var r = new Rect(x0 + i * (cw + gap), y0, cw, 100 * u);
            Pill(new Rect(r.x, r.y, r.width, r.height), new Color(1, 1, 1, .65f));
            float cd = Cooldown(a.id);
            Icon(new Rect(r.x + 20 * u, r.y + 10 * u, 44 * u, 44 * u), a.sprite, cd > 0 ? .4f : 1f);
            if (cd > 0) Box(new Rect(r.x + 18 * u, r.y + 58 * u, (cw - 36 * u) * (1 - cd), 3 * u), Gfx.Gold);
            Text(new Rect(r.x, r.y + 60 * u, cw, 18 * u), a.label, h1, ink, (int)(12 * u));
            Text(new Rect(r.x, r.y + 77 * u, cw, 18 * u), a.key, body, new Color(ink.r, ink.g, ink.b, .55f), (int)(11 * u));
        }

        // new-state banner
        if (!string.IsNullOrEmpty(newStates) && bannerT < 5.5f)
        {
            float a = Mathf.Clamp01(bannerT * 3f) * Mathf.Clamp01((5.5f - bannerT) * 2f);
            var ids = newStates.Split(',');
            float bw = 250 * u * ids.Length + 40 * u, bh = 150 * u;
            var br = new Rect((W - bw) / 2, 110 * u - (1 - a) * 20 * u, bw, bh);
            Pill(br, new Color(1, 1, 1, .82f * a));
            Text(new Rect(br.x, br.y + 8 * u, bw, 26 * u), "NEW STATE" + (ids.Length > 1 ? "S" : ""), body, new Color(ink.r, ink.g, ink.b, .55f * a), (int)(14 * u));
            for (int i = 0; i < ids.Length; i++)
            {
                var ab = Find(ids[i]);
                float cx = br.x + 20 * u + i * 250 * u;
                Icon(new Rect(cx + 14 * u, br.y + 40 * u, 84 * u, 84 * u), ab.sprite, a);
                Text(new Rect(cx + 104 * u, br.y + 50 * u, 140 * u, 34 * u), ab.label, h1, new Color(ink.r, ink.g, ink.b, a), (int)(24 * u));
                Text(new Rect(cx + 104 * u, br.y + 86 * u, 140 * u, 26 * u), ab.key, body, new Color(ink.r, ink.g, ink.b, .6f * a), (int)(16 * u));
            }
        }

        if (mode == Mode.LevelDone)
        {
            Text(new Rect(0, H * .3f, W, 80 * u), "EVOLVED", hero, Color.white, (int)(64 * u));
            Text(new Rect(0, H * .3f + 70 * u, W, 40 * u), orbs + " / " + level.orbs + " orbs   ·   " + levelTime.ToString("0.0") + "s", body, Color.white, (int)(22 * u));
        }
    }

    void DrawTitle(float W, float H, float u, Color ink)
    {
        Box(new Rect(0, 0, W, H), new Color(1, 1, 1, .35f));
        Text(new Rect(0, H * .16f, W, 100 * u), "BALL STATES", hero, ink, (int)(88 * u));
        Text(new Rect(0, H * .16f + 92 * u, W, 36 * u), "one ball  ·  fourteen states  ·  a sky full of physics", body, new Color(ink.r, ink.g, ink.b, .65f), (int)(22 * u));

        int n = AllStates.Length;
        float s = Mathf.Min(70 * u, (W - 80 * u) / n), total = s * n;
        int hi = (int)(uiT * 1.5f) % n;
        for (int i = 0; i < n; i++)
        {
            float bob = Mathf.Sin(uiT * 3f + i * .5f) * 4 * u;
            float sc = i == hi ? 1.25f : 1f;
            var r = new Rect((W - total) / 2 + i * s + s * (1 - sc) / 2, H * .5f - s / 2 + bob - (sc - 1) * s / 2, s * sc * .9f, s * sc * .9f);
            Icon(r, AllStates[i], i == hi ? 1f : .75f);
        }
        Text(new Rect(0, H * .5f + s * .75f, W, 30 * u), AllStates[hi].ToUpper(), h1, ink, (int)(18 * u));

        float p = .6f + Mathf.Sin(uiT * 4f) * .4f;
        Pill(new Rect(W / 2 - 170 * u, H * .72f, 340 * u, 58 * u), new Color(ink.r, ink.g, ink.b, .9f));
        Text(new Rect(W / 2 - 170 * u, H * .72f, 340 * u, 58 * u), "PRESS ANY KEY", h1, new Color(1, 1, 1, p), (int)(22 * u));
        Text(new Rect(0, H * .72f + 70 * u, W, 30 * u), "A / D roll   ·   SPACE jump   ·   R restart", body, new Color(ink.r, ink.g, ink.b, .6f), (int)(17 * u));
    }

    void DrawWon(float W, float H, float u, Color ink)
    {
        Box(new Rect(0, 0, W, H), new Color(1, 1, 1, .55f));
        float orbPct = totalOrbsMax > 0 ? totalOrbs / (float)totalOrbsMax : 1f;
        string rank = orbPct >= .95f && totalDeaths <= 2 ? "S" : orbPct >= .75f && totalDeaths <= 6 ? "A" : orbPct >= .5f ? "B" : "C";
        Icon(new Rect(W / 2 - 80 * u, H * .12f, 160 * u, 160 * u), "evolve");
        Text(new Rect(0, H * .12f + 170 * u, W, 80 * u), "FULLY EVOLVED", hero, ink, (int)(60 * u));
        Text(new Rect(0, H * .12f + 245 * u, W, 36 * u),
            totalOrbs + " / " + totalOrbsMax + " orbs   ·   " + totalDeaths + " falls   ·   " + totalTime.ToString("0.0") + "s",
            body, new Color(ink.r, ink.g, ink.b, .7f), (int)(22 * u));
        Text(new Rect(0, H * .12f + 290 * u, W, 120 * u), rank, hero, Gfx.Gold, (int)(110 * u));
        Text(new Rect(0, H * .88f, W, 30 * u), "press ENTER to fly again", body, new Color(ink.r, ink.g, ink.b, .6f), (int)(18 * u));
    }
}
