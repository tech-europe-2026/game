using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoBehaviour
{
    enum Mode { Title, Playing, LevelDone, Won }

    public static GM I;
    static readonly HashSet<string> unlocked = new HashSet<string>(), seen = new HashSet<string>();
    static readonly List<(Vector2 p, string text)> signs = new List<(Vector2, string)>();

    public int orbs;
    public Vector2 checkpoint;
    public Transform LevelRoot => level != null ? level.root : null;
    public Ball Player => ball;
    public bool Fighting => mode == Mode.Playing && Levels.All[levelIndex].boss;

    Mode mode = Mode.Title;
    int levelIndex;
    Ball ball;
    CameraFollow camFollow;
    Camera cam;
    LevelBuilder level;
    float levelTime, totalTime;
    int deaths, totalDeaths, totalOrbs, totalOrbsMax;
    string newStates;
    float bannerT, uiT, menuT;
    int sel;
    string doneTitle = "EVOLVED", doneSub;

    public struct Ability
    {
        public string id, sprite, key, label;
        public Ability(string id, string sprite, string key, string label) { this.id = id; this.sprite = sprite; this.key = key; this.label = label; }
    }

    public static readonly Ability[] Abilities =
    {
        new Ability("jump", "bounce", "SPACE", "JUMP"),
        new Ability("crouch", "crouch", "S", "CROUCH"),
        new Ability("dash", "dash", "D", "DASH"),
        new Ability("slam", "spin", "F", "SLAM"),
        new Ability("hover", "freeze", "Q", "HOVER"),
        new Ability("reverse", "reverse", "E", "REVERSE"),
        new Ability("climb", "climb", "HOLD C", "CLIMB"),
        new Ability("grow", "grow", "G", "GROW"),
        new Ability("parry", "parry", "Q", "PARRY"),
        new Ability("teleport", "teleport", "T", "BLINK"),
        new Ability("camo", "camouflage", "V", "CAMO"),
    };

    static readonly string[] AllStates =
        { "idle", "spin", "bounce", "teleport", "grow", "crouch", "stun", "heal", "dash", "freeze", "climb", "camouflage", "reverse", "parry", "evolve" };

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
        gameObject.AddComponent<Controls>();
        gameObject.AddComponent<Fx>();
        Sfx.Init(gameObject);
        Music.Init(gameObject);
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
        camFollow.fixedView = Levels.All[idx].boss;
        if (def.boss) camFollow.Follow(ball, def.camMin, def.camMax);
        else camFollow.Follow(ball, level.min, level.max);
        camFollow.SetSky(def.bottom, def.mid, def.top);
        orbs = 0;
        deaths = 0;
        levelTime = 0;
        var fresh = new List<string>();
        unlocked.Clear();
        foreach (var a in def.unlock.Split(','))
        {
            string id = a.Trim();
            if (id.Length == 0) continue;
            unlocked.Add(id);
            if (seen.Add(id)) fresh.Add(id);
        }
        newStates = string.Join(",", fresh);
        bannerT = 0;
        Music.Play(idx);
    }

    void StartLevel(int idx)
    {
        unlocked.Clear();
        totalTime = 0; totalDeaths = 0; totalOrbs = 0; totalOrbsMax = 0;
        LoadLevel(idx);
        mode = Mode.Playing;
        ball.controlLocked = false;
        bannerT = 0;
        Sfx.Play("heal");
    }

    void ToMenu()
    {
        LoadLevel(0);
        mode = Mode.Title;
        ball.controlLocked = true;
        menuT = 0;
    }

    static Vector2 MouseGui => new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

    Rect CardRect(int i)
    {
        float W = Screen.width, H = Screen.height, u = H / 720f;
        int n = 5, row = i / n, col = i % n;
        float cw = Mathf.Min(190 * u, (W - 60 * u) / n - 14 * u), gap = 14 * u, total = n * cw + (n - 1) * gap;
        return new Rect((W - total) / 2 + col * (cw + gap), H * .46f + row * 124 * u, cw, 110 * u);
    }

    Rect MusicRect { get { float u = Screen.height / 720f; return new Rect(Screen.width - 160 * u, 98 * u, 76 * u, 40 * u); } }
    Rect MenuRect { get { float u = Screen.height / 720f; return new Rect(Screen.width - 244 * u, 98 * u, 76 * u, 40 * u); } }

    void Update()
    {
        uiT += Time.unscaledDeltaTime;
        menuT += Time.unscaledDeltaTime;
        Music.Tick(Time.unscaledDeltaTime);
        bool click = Input.GetMouseButtonDown(0) && !Controls.Portrait;
        if (Input.GetKeyDown(KeyCode.M) || (click && MusicRect.Contains(MouseGui))) { Music.On = !Music.On; click = false; }
        Controls.I.Layout(unlocked);
        Controls.I.gameplay = mode == Mode.Playing && !Controls.Portrait;
        bannerT += Time.unscaledDeltaTime;
        switch (mode)
        {
            case Mode.Title:
                if (Controls.Portrait || menuT < .3f) break;
                for (int i = 0; i < Levels.All.Length; i++)
                {
                    if (Input.GetKeyDown(i < 9 ? KeyCode.Alpha1 + i : KeyCode.Alpha0) || Input.GetKeyDown(i < 9 ? KeyCode.Keypad1 + i : KeyCode.Keypad0)) { StartLevel(i); return; }
                    if (click && CardRect(i).Contains(MouseGui)) { StartLevel(i); return; }
                }
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow)) sel = (sel + 1) % Levels.All.Length;
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow)) sel = (sel + Levels.All.Length - 1) % Levels.All.Length;
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) StartLevel(sel);
                break;
            case Mode.Playing:
                levelTime += Time.deltaTime;
                if (!ball.dead)
                {
                    float y = ball.rb.position.y;
                    if (y < level.min.y - 9f || y > level.max.y + 9f) ball.Die();
                }
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.L) || (click && MenuRect.Contains(MouseGui))) { ToMenu(); break; }
                if (Input.GetKeyDown(KeyCode.R) || Controls.Pressed("restart")) LoadLevel(levelIndex);
                if (Input.GetKeyDown(KeyCode.F9)) SkipAhead();
                if (Input.GetKeyDown(KeyCode.N)) StartCoroutine(Advance(0f));
                break;
            case Mode.Won:
                if (menuT > 1f && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || click)) ToMenu();
                break;
        }
    }

    public void OnBallDied()
    {
        deaths++;
        if (Levels.All[levelIndex].boss) StartCoroutine(DuelLostCo());
        else StartCoroutine(RespawnCo());
    }

    IEnumerator DuelLostCo()
    {
        mode = Mode.LevelDone;
        doneTitle = "RED WINS";
        doneSub = levelIndex == 4 ? "try again · dash into it from the side" : "try again · slam it from above, jump the shockwaves";
        yield return new WaitForSeconds(2.2f);
        doneTitle = "EVOLVED";
        doneSub = null;
        LoadLevel(levelIndex);
        mode = Mode.Playing;
    }

    public void OnBossDefeated(Vector2 p)
    {
        if (mode != Mode.Playing) return;
        StartCoroutine(BossWonCo());
    }

    IEnumerator BossWonCo()
    {
        mode = Mode.LevelDone;
        ball.controlLocked = true;
        doneTitle = "CHAMPION";
        doneSub = "the red ball is beaten";
        Sfx.Play("evolve");
        yield return new WaitForSeconds(2.4f);
        doneTitle = "EVOLVED";
        doneSub = null;
        totalTime += levelTime;
        totalDeaths += deaths;
        totalOrbs += orbs;
        totalOrbsMax += level.orbs;
        if (levelIndex + 1 < Levels.All.Length) yield return Advance(0f);
        else
        {
            mode = Mode.Won;
            menuT = 0;
        }
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
            menuT = 0;
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

    void SkipAhead()
    {
        float bx = ball.rb.position.x, best = float.MaxValue;
        Vector2 target = Vector2.zero;
        foreach (var t in level.root.GetComponentsInChildren<Tile>())
        {
            if (t.kind != TileKind.Check && t.kind != TileKind.Goal) continue;
            float x = t.transform.position.x;
            if (x > bx + 1f && x < best) { best = x; target = t.transform.position; }
        }
        if (best < float.MaxValue) ball.Respawn(target + Vector2.left * 1.5f + Vector2.up);
    }

    float Cooldown(string id)
    {
        switch (id)
        {
            case "dash": return Mathf.Clamp01(ball.dashCd / Ball.DashCd);
            case "slam": return Mathf.Clamp01(ball.slamCd / Ball.SlamCd);
            case "hover": return Mathf.Clamp01(ball.hoverCd / Ball.HoverCd);
            case "teleport": return Mathf.Clamp01(ball.teleCd / Ball.TeleCd);
            case "parry": return Mathf.Clamp01(ball.parryCd / Ball.ParryCd);
            case "camo": return Mathf.Clamp01(ball.camoCd / Ball.CamoCd);
            case "reverse": return ball.flipReady ? Mathf.Clamp01(ball.flipCd / Ball.FlipCd) : 1f;
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
                var parts = text.Split('|');
                string shown = Controls.Touch && parts.Length > 1 ? parts[1] : parts[0];
                Text(new Rect(sp.x - 300 * u, H - sp.y - 20 * u, 600 * u, 40 * u), shown, body, new Color(ink.r, ink.g, ink.b, .75f), (int)(20 * u));
            }
        }

        if (Controls.Portrait) { DrawRotate(W, H, ink); return; }
        if (mode == Mode.Title) { DrawTitle(W, H, u, ink); DrawMusic(u, ink); return; }
        if (mode == Mode.Won) { DrawWon(W, H, u, ink); return; }
        DrawMusic(u, ink);
        Pill(MenuRect, new Color(1, 1, 1, .6f));
        Text(MenuRect, "MENU", h1, ink, (int)(14 * u));

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

        if (def.boss && Boss.I != null)
        {
            var br = new Rect(W / 2 - 110 * u, 22 * u, 220 * u, 56 * u);
            Pill(br, new Color(1, 1, 1, .7f));
            Text(new Rect(br.x + 14 * u, br.y, 70 * u, br.height), "RED", h1, Gfx.Coral, (int)(22 * u));
            for (int i = 0; i < Boss.MaxHearts; i++)
                Pill(new Rect(br.x + (100 + i * 32) * u, br.y + 18 * u, 20 * u, 20 * u), i < Boss.I.hearts ? (Color)Gfx.Coral : new Color(ink.r, ink.g, ink.b, .15f));
        }

        if (Controls.Touch) DrawTouch(u, ink);

        // bottom: unlocked states
        var list = new List<Ability>();
        foreach (var a in Abilities) if (Has(a.id)) list.Add(a);
        float cw = 84 * u, gap = 10 * u, total = list.Count * cw + (list.Count - 1) * gap;
        float x0 = (W - total) / 2, y0 = H - 118 * u;
        for (int i = 0; i < list.Count && !Controls.Touch; i++)
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
                Text(new Rect(cx + 104 * u, br.y + 86 * u, 140 * u, 26 * u), Controls.Touch ? TouchHint(ab.id) : ab.key, body, new Color(ink.r, ink.g, ink.b, .6f * a), (int)(16 * u));
            }
        }

        if (mode == Mode.LevelDone)
        {
            Text(new Rect(0, H * .3f, W, 80 * u), doneTitle, hero, Color.white, (int)(64 * u));
            Text(new Rect(0, H * .3f + 70 * u, W, 40 * u), doneSub ?? orbs + " / " + level.orbs + " orbs   ·   " + levelTime.ToString("0.0") + "s", body, Color.white, (int)(22 * u));
        }
    }

    static readonly (string sprite, string label)[] TitleForms =
    {
        ("idle", "ROLL"), ("bounce", "JUMP"), ("dash", "DASH"), ("crouch", "CROUCH"), ("grow", "GROW"),
        ("reverse", "REVERSE"), ("spin", "SLAM"), ("freeze", "HOVER"), ("heal", "HEAL"), ("evolve", "EVOLVE"),
    };

    void DrawTitle(float W, float H, float u, Color ink)
    {
        Box(new Rect(0, 0, W, H), new Color(1, 1, 1, .72f));
        Text(new Rect(0, H * .12f, W, 100 * u), "SKYROLL", hero, ink, (int)(92 * u));
        Text(new Rect(0, H * .12f + 96 * u, W, 30 * u), "roll  ·  jump  ·  fly", body, new Color(ink.r, ink.g, ink.b, .5f), (int)(20 * u));
        float bob = Mathf.Sin(uiT * 2.5f) * 6 * u;
        int n = TitleForms.Length, cur = (int)(uiT / 3f) % n, prev = (cur + n - 1) % n;
        float k = uiT < 3f ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((uiT % 3f) / .6f));
        float pop = 1f + (1f - k) * .15f;
        var ir = new Rect(W / 2 - 40 * u * pop, H * .305f + bob - 40 * u * (pop - 1), 80 * u * pop, 80 * u * pop);
        if (k < 1f) Icon(ir, TitleForms[prev].sprite, 1f - k);
        Icon(ir, TitleForms[cur].sprite, k);
        if (k < 1f) Text(new Rect(0, H * .305f + 82 * u, W, 24 * u), TitleForms[prev].label, h1, new Color(ink.r, ink.g, ink.b, .55f * (1f - k)), (int)(15 * u));
        Text(new Rect(0, H * .305f + 82 * u, W, 24 * u), TitleForms[cur].label, h1, new Color(ink.r, ink.g, ink.b, .55f * k), (int)(15 * u));
        var mp = MouseGui;
        for (int i = 0; i < Levels.All.Length; i++)
        {
            var d = Levels.All[i];
            var cr = CardRect(i);
            bool on = i == sel || (!Controls.Touch && cr.Contains(mp));
            if (on) cr = new Rect(cr.x - 3 * u, cr.y - 5 * u, cr.width + 6 * u, cr.height + 6 * u);
            Pill(cr, on ? ink : new Color(1, 1, 1, .9f));
            var tc = on ? Color.white : ink;
            Text(new Rect(cr.x, cr.y + 20 * u, cr.width, 40 * u), (i + 1).ToString(), hero, tc, (int)(30 * u));
            Text(new Rect(cr.x, cr.y + 62 * u, cr.width, 26 * u), d.name, h1, new Color(tc.r, tc.g, tc.b, .85f), (int)(13 * u));
        }
        Text(new Rect(0, H - 52 * u, W, 30 * u), Controls.Touch ? "tap a level" : "click a level  ·  1-9, 0", body, new Color(ink.r, ink.g, ink.b, .45f), (int)(16 * u));
    }

    void DrawMusic(float u, Color ink)
    {
        var r = MusicRect;
        Pill(r, new Color(1, 1, 1, Music.On ? .75f : .45f));
        Text(r, Music.On ? "MUSIC" : "MUTED", h1, new Color(ink.r, ink.g, ink.b, Music.On ? 1f : .5f), (int)(14 * u));
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
        Text(new Rect(0, H * .88f, W, 30 * u), Controls.Touch ? "tap for the level menu" : "press ENTER for the level menu", body, new Color(ink.r, ink.g, ink.b, .6f), (int)(18 * u));
    }

    static string TouchHint(string id)
    {
        switch (id)
        {
            case "crouch": return "HOLD";
            case "climb": return "HOLD";
            default: return "TAP";
        }
    }

    void DrawRotate(float W, float H, Color ink)
    {
        float u = Mathf.Min(W, H) / 400f;
        Box(new Rect(0, 0, W, H), new Color(1, 1, 1, .8f));
        var c = new Vector2(W / 2, H * .38f);
        float t = Mathf.Repeat(uiT, 2.4f), ang = Mathf.SmoothStep(0, -90, Mathf.Clamp01((t - .5f) / .8f));
        var old = GUI.matrix;
        GUIUtility.RotateAroundPivot(ang, c);
        Pill(new Rect(c.x - 40 * u, c.y - 70 * u, 80 * u, 140 * u), ink);
        Pill(new Rect(c.x - 34 * u, c.y - 62 * u, 68 * u, 124 * u), new Color(.75f, .85f, 1f));
        GUI.matrix = old;
        Icon(new Rect(c.x - 18 * u, c.y - 18 * u, 36 * u, 36 * u), "idle");
        Text(new Rect(0, H * .62f, W, 50 * u), "TURN SIDEWAYS", hero, ink, (int)(30 * u));
        Text(new Rect(0, H * .62f + 44 * u, W, 30 * u), "Skyroll plays in landscape", body, new Color(ink.r, ink.g, ink.b, .55f), (int)(16 * u));
    }

    static void DrawArrow(Vector2 c, float r, int dir, bool down, float u, Color ink)
    {
        float rr = r * (down ? .94f : 1f);
        Pill(new Rect(c.x - rr, c.y - rr, rr * 2, rr * 2), down ? new Color(1, 1, 1, .92f) : new Color(1, 1, 1, .6f));
        var col = new Color(ink.r, ink.g, ink.b, down ? 1f : .8f);
        float len = r * .62f, th = 11 * u;
        var tip = c + new Vector2(dir * r * .2f, 0);
        var old = GUI.matrix;
        for (int s = -1; s <= 1; s += 2)
        {
            GUI.matrix = old;
            GUIUtility.RotateAroundPivot(dir * s * -45f, tip);
            Pill(new Rect(dir > 0 ? tip.x - len + th / 2 : tip.x - th / 2, tip.y - th / 2, len, th), col);
        }
        GUI.matrix = old;
    }

    void DrawTouch(float u, Color ink)
    {
        var c = Controls.I;
        DrawArrow(c.leftPad, c.padRadius, -1, Controls.Held("left"), u, ink);
        DrawArrow(c.rightPad, c.padRadius, 1, Controls.Held("right"), u, ink);
        foreach (var b in c.buttons)
        {
            bool down = Controls.Held(b.id);
            float r = b.radius * (down ? .92f : 1f);
            var rect = new Rect(b.center.x - r, b.center.y - r, r * 2, r * 2);
            if (b.id == "restart")
            {
                Pill(rect, new Color(1, 1, 1, .6f));
                Text(rect, "R", h1, ink, (int)(20 * u));
                continue;
            }
            float cd = b.id == "jump" ? 0f : Cooldown(b.id);
            Pill(rect, down ? new Color(1, 1, 1, .9f) : new Color(.93f, .95f, 1f, .62f));
            var ab = Find(b.id);
            float ir = r * .6f;
            Icon(new Rect(b.center.x - ir, b.center.y - ir - r * .18f, ir * 2, ir * 2), ab.sprite, cd > 0 ? .35f : 1f);
            if (cd > 0) Box(new Rect(b.center.x - r * .5f, b.center.y + r * .82f, r * (1 - cd), 3 * u), Gfx.Gold);
            Text(new Rect(rect.x - 10 * u, b.center.y + r * .42f, rect.width + 20 * u, 20 * u), ab.label, h1, ink, (int)((b.id == "jump" ? 15 : 11) * u));
        }
    }
}
