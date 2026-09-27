using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoBehaviour
{
    enum Mode { Title, Playing, LevelDone, Won, Lost }

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
    int sel = -1;
    bool picking;
    string doneTitle = "LEVEL CLEAR", doneSub;

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
        new Ability("parry", "parry", "J", "SHIELD"),
        new Ability("phase", "camouflage", "V", "PHASE"),
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
        Ball.squeezeZone = default;
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
        picking = true;
        sel = -1;
        ball.controlLocked = true;
        menuT = 0;
    }

    static Vector2 MouseGui => new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

    const int Cols = 5;
    float scroll, dragY, dragMoved;

    Rect ListClip { get { float u = Screen.height / 720f; return new Rect(0, 150 * u, Screen.width, Screen.height - 200 * u); } }

    Rect CardRect(int i)
    {
        float W = Screen.width, H = Screen.height, u = H / 720f;
        int row = i / Cols, col = i % Cols;
        float gap = 18 * u, cw = Mathf.Min(200 * u, (W - 80 * u - (Cols - 1) * gap) / Cols), ch = 150 * u, total = Cols * cw + (Cols - 1) * gap;
        return new Rect((W - total) / 2 + col * (cw + gap), 170 * u + row * (ch + gap) - scroll, cw, ch);
    }

    float MaxScroll
    {
        get
        {
            float u = Screen.height / 720f, gap = 18 * u, ch = 150 * u;
            int rows = (Levels.All.Length + Cols - 1) / Cols;
            var clip = ListClip;
            return Mathf.Max(0, 20 * u + rows * (ch + gap) + 20 * u - clip.height);
        }
    }

    void ScrollTo(int i)
    {
        var c = CardRect(i); var clip = ListClip;
        if (c.y < clip.y + 10) scroll -= clip.y + 10 - c.y;
        if (c.yMax > clip.yMax - 10) scroll += c.yMax - (clip.yMax - 10);
    }

    Rect PlayRect { get { float u = Screen.height / 720f; return new Rect(Screen.width / 2 - 130 * u, Screen.height * .74f, 260 * u, 76 * u); } }
    Rect BackRect { get { float u = Screen.height / 720f; return new Rect(40 * u, 44 * u, 110 * u, 44 * u); } }

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
                if (!picking)
                {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || (click && PlayRect.Contains(MouseGui))) { picking = true; sel = -1; menuT = 0; }
                    break;
                }
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace) || (click && BackRect.Contains(MouseGui))) { picking = false; menuT = 0; break; }
                {
                    float su = Screen.height / 720f;
                    scroll -= Input.mouseScrollDelta.y * 50 * su;
                    if (Input.GetMouseButtonDown(0)) { dragY = Input.mousePosition.y; dragMoved = 0; }
                    else if (Input.GetMouseButton(0))
                    {
                        float dy = Input.mousePosition.y - dragY;
                        scroll += dy; dragMoved += Mathf.Abs(dy); dragY = Input.mousePosition.y;
                    }
                    scroll = Mathf.Clamp(scroll, 0, MaxScroll);
                }
                bool tapUp = Input.GetMouseButtonUp(0) && dragMoved < 12 && !Controls.Portrait && ListClip.Contains(MouseGui);
                for (int i = 0; i < Levels.All.Length; i++)
                {
                    if (i < 10 && (Input.GetKeyDown(i < 9 ? KeyCode.Alpha1 + i : KeyCode.Alpha0) || Input.GetKeyDown(i < 9 ? KeyCode.Keypad1 + i : KeyCode.Keypad0))) { StartLevel(i); return; }
                    if (tapUp && CardRect(i).Contains(MouseGui)) { StartLevel(i); return; }
                }
                int nl = Levels.All.Length, ps = sel;
                if (Input.GetKeyDown(KeyCode.RightArrow)) sel = sel < 0 ? 0 : (sel + 1) % nl;
                if (Input.GetKeyDown(KeyCode.LeftArrow)) sel = sel < 0 ? 0 : (sel + nl - 1) % nl;
                if (Input.GetKeyDown(KeyCode.DownArrow)) sel = sel < 0 ? 0 : Mathf.Min(nl - 1, sel + Cols);
                if (Input.GetKeyDown(KeyCode.UpArrow)) sel = sel < 0 ? 0 : Mathf.Max(0, sel - Cols);
                if (sel != ps && sel >= 0) { ScrollTo(sel); scroll = Mathf.Clamp(scroll, 0, MaxScroll); }
                if (sel >= 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) StartLevel(sel);
                break;
            case Mode.Playing:
                levelTime += Time.deltaTime;
                if (!ball.dead)
                {
                    float y = ball.rb.position.y;
                    if (Levels.All[levelIndex].pit && y < level.min.y - 2f) ball.PitFall(Levels.All[levelIndex].camMin + new Vector2(3f, 2f));
                    else if (y < level.min.y - 9f || y > level.max.y + 9f) ball.Die();
                }
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.L) || (click && MenuRect.Contains(MouseGui))) { ToMenu(); break; }
                if (Input.GetKeyDown(KeyCode.R) || Controls.Pressed("restart")) LoadLevel(levelIndex);
                if (Input.GetKeyDown(KeyCode.F9)) SkipAhead();
                if (Input.GetKeyDown(KeyCode.N)) StartCoroutine(Advance(0f));
                break;
            case Mode.Lost:
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R) || Controls.Pressed("restart") || (click && ReplayRect.Contains(MouseGui))) Replay();
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.L) || (click && QuitRect.Contains(MouseGui))) { doneSub = null; ToMenu(); }
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
        doneSub = levelIndex == 4 ? "try again · dash into it from the side" : levelIndex == 9 ? "try again · slam it from above, jump the shockwaves" : "try again · shield the blue shots, stay off the edge";
        yield return new WaitForSeconds(1.2f);
        mode = Mode.Lost;
    }

    Rect ModalRect { get { float u = Screen.height / 720f; return new Rect(Screen.width / 2 - 250 * u, Screen.height / 2 - 150 * u, 500 * u, 300 * u); } }
    Rect ReplayRect { get { float u = Screen.height / 720f; var m = ModalRect; return new Rect(m.x + 40 * u, m.yMax - 100 * u, 200 * u, 64 * u); } }
    Rect QuitRect { get { float u = Screen.height / 720f; var m = ModalRect; return new Rect(m.xMax - 240 * u, m.yMax - 100 * u, 200 * u, 64 * u); } }

    void Replay()
    {
        doneTitle = "LEVEL CLEAR";
        doneSub = null;
        LoadLevel(levelIndex);
        mode = Mode.Playing;
    }

    void DrawLost(float W, float H, float u, Color ink)
    {
        Box(new Rect(0, 0, W, H), new Color(ink.r, ink.g, ink.b, .45f));
        var m = ModalRect;
        Round(new Rect(m.x, m.y + 10 * u, m.width, m.height), new Color(0, 0, 0, .15f), 30 * u);
        Round(m, new Color(1, 1, 1, .97f), 30 * u);
        Text(new Rect(m.x, m.y + 30 * u, m.width, 70 * u), "RED WINS", hero, Gfx.Coral, (int)(52 * u));
        Text(new Rect(m.x + 30 * u, m.y + 100 * u, m.width - 60 * u, 60 * u), doneSub, body, new Color(ink.r, ink.g, ink.b, .65f), (int)(17 * u));
        var mp = MouseGui;
        foreach (var (r, label, main) in new[] { (ReplayRect, "REPLAY", true), (QuitRect, "MENU", false) })
        {
            bool hov = !Controls.Touch && r.Contains(mp);
            Pill(r, main ? (hov ? Gfx.Gold : ink) : new Color(ink.r, ink.g, ink.b, hov ? .22f : .1f));
            Text(r, label, hero, main ? Color.white : ink, (int)(26 * u));
        }
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
        doneTitle = "LEVEL CLEAR";
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
        var bold = Resources.Load<Font>("Fonts/Poppins-Bold");
        var semi = Resources.Load<Font>("Fonts/Poppins-SemiBold");
        GUI.skin.font = semi;
        hero = new GUIStyle(GUI.skin.label) { font = bold, alignment = TextAnchor.MiddleCenter };
        h1 = new GUIStyle(hero);
        h2 = new GUIStyle(hero) { font = semi };
        body = new GUIStyle(hero) { font = semi };
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

    void HeartIcon(Rect r, bool full, Color ink, int i)
    {
        var tex = Gfx.Heart.texture;
        var old = GUI.color;
        if (full)
        {
            float beat = 1f + Mathf.Max(0, Mathf.Sin(uiT * 4f - i * .6f)) * .07f;
            r = new Rect(r.center.x - r.width * beat / 2, r.center.y - r.height * beat / 2, r.width * beat, r.height * beat);
            GUI.color = new Color(0, 0, 0, .12f);
            GUI.DrawTexture(new Rect(r.x, r.y + 2, r.width, r.height), tex, ScaleMode.ScaleToFit);
            GUI.color = Gfx.Coral;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            GUI.color = new Color(1, 1, 1, .45f);
            GUI.DrawTexture(new Rect(r.x + r.width * .2f, r.y + r.height * .2f, r.width * .22f, r.height * .18f), Gfx.Circle.texture, ScaleMode.StretchToFill);
        }
        else
        {
            GUI.color = new Color(ink.r, ink.g, ink.b, .15f);
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
        }
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
            case "phase": return Mathf.Clamp01(ball.phaseCd / Ball.PhaseCd);
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
        Pill(new Rect(24 * u, 22 * u, 380 * u, 56 * u), new Color(1, 1, 1, .72f));
        Text(new Rect(40 * u, 25 * u, 60 * u, 52 * u), (levelIndex + 1).ToString("00"), hero, ink, (int)(26 * u));
        Box(new Rect(104 * u, 36 * u, 2 * u, 28 * u), new Color(ink.r, ink.g, ink.b, .2f));
        for (int i = 0; i < Ball.MaxHearts; i++)
            HeartIcon(new Rect((120 + i * 30) * u, 38 * u, 24 * u, 24 * u), i < ball.hearts, ink, i);
        Box(new Rect(248 * u, 36 * u, 2 * u, 28 * u), new Color(ink.r, ink.g, ink.b, .2f));
        Pill(new Rect(264 * u, 40 * u, 20 * u, 20 * u), Gfx.Gold);
        Pill(new Rect(269 * u, 44 * u, 6 * u, 6 * u), new Color(1, 1, 1, .8f));
        Text(new Rect(292 * u, 25 * u, 100 * u, 52 * u), orbs + " / " + level.orbs, h1, ink, (int)(22 * u));

        // top-right: time
        Text(new Rect(W - 260 * u, 22 * u, 236 * u, 30 * u), def.name, h1, ink, (int)(20 * u));
        Text(new Rect(W - 260 * u, 50 * u, 236 * u, 24 * u), levelTime.ToString("0.0") + "s   ·   " + deaths + " falls", body, new Color(ink.r, ink.g, ink.b, .6f), (int)(16 * u));

        if (def.boss && Boss.I != null)
        {
            var br = new Rect(W / 2 - 110 * u, 22 * u, 220 * u, 56 * u);
            Pill(br, new Color(1, 1, 1, .7f));
            Text(new Rect(br.x + 14 * u, br.y, 70 * u, br.height), "RED", h1, Gfx.Coral, (int)(22 * u));
            for (int i = 0; i < Boss.MaxHearts; i++)
                HeartIcon(new Rect(br.x + (96 + i * 30) * u, br.y + 16 * u, 24 * u, 24 * u), i < Boss.I.hearts, ink, i);
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

        if (mode == Mode.Lost) { DrawLost(W, H, u, ink); return; }
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

    static void Round(Rect r, Color c, float rad)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white, ScaleMode.StretchToFill, true, 0, c, 0, rad);
        GUI.color = old;
    }

    void DrawTitle(float W, float H, float u, Color ink)
    {
        if (picking) { DrawLevels(W, H, u, ink); return; }
        Box(new Rect(0, 0, W, H), new Color(1, 1, 1, .35f));
        var soft = new Color(ink.r, ink.g, ink.b, .5f);
        Text(new Rect(0, H * .08f, W, 110 * u), "SKYROLL", hero, ink, (int)(104 * u));
        Text(new Rect(0, H * .08f + 104 * u, W, 30 * u), "ROLL  ·  JUMP  ·  EVOLVE", h1, soft, (int)(17 * u));

        var c = new Vector2(W / 2, H * .47f + Mathf.Sin(uiT * 2.2f) * 7 * u);
        float pulse = 1f + Mathf.Sin(uiT * 3f) * .03f;
        for (int i = 3; i >= 1; i--)
        {
            float rr = (78 + i * 26) * u * pulse;
            Pill(new Rect(c.x - rr, c.y - rr, rr * 2, rr * 2), new Color(1, 1, 1, .12f + (3 - i) * .06f));
        }
        int n = TitleForms.Length, cur = (int)(uiT / 3f) % n, prev = (cur + n - 1) % n;
        float k = uiT < 3f ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((uiT % 3f) / .6f));
        float sz = 150 * u * (1f + (1f - k) * .12f);
        var ir = new Rect(c.x - sz / 2, c.y - sz / 2, sz, sz);
        if (k < 1f) Icon(ir, TitleForms[prev].sprite, 1f - k);
        Icon(ir, TitleForms[cur].sprite, k);

        var tag = new Rect(W / 2 - 90 * u, H * .47f + 118 * u, 180 * u, 38 * u);
        Pill(tag, new Color(ink.r, ink.g, ink.b, .9f));
        if (k < 1f) Text(tag, TitleForms[prev].label, h1, new Color(1, 1, 1, 1f - k), (int)(17 * u));
        Text(tag, TitleForms[cur].label, h1, new Color(1, 1, 1, k), (int)(17 * u));
        for (int i = 0; i < n; i++)
        {
            float dx = (i - (n - 1) / 2f) * 14 * u, d = i == cur ? 8 * u : 5 * u;
            Pill(new Rect(W / 2 + dx - d / 2, tag.yMax + 14 * u - d / 2, d, d), new Color(ink.r, ink.g, ink.b, i == cur ? .8f : .25f));
        }

        var pr = PlayRect;
        bool hov = !Controls.Touch && pr.Contains(MouseGui);
        float grow = (hov ? 6 : 0) * u + Mathf.Sin(uiT * 4f) * 2 * u;
        pr = new Rect(pr.x - grow, pr.y - grow / 2, pr.width + grow * 2, pr.height + grow);
        Pill(new Rect(pr.x, pr.y + 6 * u, pr.width, pr.height), new Color(0, 0, 0, .12f));
        Pill(pr, hov ? Gfx.Gold : ink);
        Text(pr, "PLAY", hero, Color.white, (int)(34 * u));
        Text(new Rect(0, H - 44 * u, W, 26 * u), Controls.Touch ? "tap play" : "press ENTER", body, new Color(ink.r, ink.g, ink.b, .4f), (int)(15 * u));
    }

    void DrawLevels(float W, float H, float u, Color ink)
    {
        Box(new Rect(0, 0, W, H), new Color(1, 1, 1, .45f));
        var br = BackRect;
        Pill(br, new Color(1, 1, 1, .8f));
        Text(br, "‹  BACK", h1, ink, (int)(15 * u));
        Text(new Rect(0, 56 * u, W, 60 * u), "CHOOSE A LEVEL", hero, ink, (int)(40 * u));
        Text(new Rect(0, 112 * u, W, 26 * u), Levels.All.Length + " worlds in the sky", body, new Color(ink.r, ink.g, ink.b, .5f), (int)(16 * u));
        var mp = MouseGui;
        var clip = ListClip;
        float ms = MaxScroll;
        if (ms > 0)
        {
            float th = clip.height - 40 * u, bh = th * clip.height / (clip.height + ms);
            Round(new Rect(W - 22 * u, clip.y + 20 * u, 6 * u, th), new Color(ink.r, ink.g, ink.b, .1f), 3 * u);
            Round(new Rect(W - 22 * u, clip.y + 20 * u + (th - bh) * scroll / ms, 6 * u, bh), new Color(ink.r, ink.g, ink.b, .45f), 3 * u);
        }
        GUI.BeginGroup(clip);
        for (int i = 0; i < Levels.All.Length; i++)
        {
            var d = Levels.All[i];
            var cr = CardRect(i);
            bool on = i == sel || (!Controls.Touch && clip.Contains(mp) && cr.Contains(mp));
            cr.y -= clip.y;
            if (cr.yMax < -20 * u || cr.y > clip.height + 20 * u) continue;
            float appear = Mathf.Clamp01((menuT - i * .025f) / .25f);
            cr.y += (1f - appear) * 20 * u - (on ? 6 * u : 0);
            float rad = 22 * u;
            Round(new Rect(cr.x, cr.y + 8 * u, cr.width, cr.height), new Color(0, 0, 0, (on ? .16f : .07f) * appear), rad);
            Round(cr, new Color(1, 1, 1, .92f * appear), rad);
            var art = new Rect(cr.x + 8 * u, cr.y + 8 * u, cr.width - 16 * u, cr.height * .52f);
            Round(art, new Color(d.top.r, d.top.g, d.top.b, appear), rad - 8 * u);
            Round(new Rect(art.x, art.y + art.height * .45f, art.width, art.height * .55f), new Color(d.mid.r, d.mid.g, d.mid.b, appear), rad - 8 * u);
            Round(new Rect(art.x, art.y + art.height * .78f, art.width, art.height * .22f), new Color(d.bottom.r, d.bottom.g, d.bottom.b, appear), rad - 8 * u);
            float bs = art.height * .62f;
            Icon(new Rect(art.center.x - bs / 2, art.center.y - bs / 2 + Mathf.Sin(uiT * 3 + i) * (on ? 3 : 0) * u, bs, bs), d.boss ? "stun" : TitleForms[i % TitleForms.Length].sprite, appear);
            if (d.boss)
            {
                var bt = new Rect(art.xMax - 58 * u, art.y + 8 * u, 50 * u, 20 * u);
                Pill(bt, new Color(1f, .3f, .35f, appear));
                Text(bt, "BOSS", h1, new Color(1, 1, 1, appear), (int)(11 * u));
            }
            Text(new Rect(cr.x + 14 * u, art.yMax + 8 * u, 40 * u, 24 * u), (i + 1).ToString("00"), hero, new Color(Gfx.Gold.r, Gfx.Gold.g, Gfx.Gold.b, appear), (int)(18 * u));
            h1.alignment = TextAnchor.MiddleLeft;
            Text(new Rect(cr.x + 52 * u, art.yMax + 8 * u, cr.width - 58 * u, 24 * u), d.name, h1, new Color(ink.r, ink.g, ink.b, appear), (int)(13 * u));
            h1.alignment = TextAnchor.MiddleCenter;
            Round(new Rect(cr.x + 14 * u, cr.yMax - 16 * u, (on ? cr.width - 28 * u : 26 * u), 4 * u), new Color(ink.r, ink.g, ink.b, (on ? .8f : .2f) * appear), 2 * u);
        }
        GUI.EndGroup();
        Text(new Rect(0, H - 40 * u, W, 26 * u), Controls.Touch ? "swipe to scroll  ·  tap a level" : "click a level  ·  scroll for more  ·  arrows + ENTER  ·  ESC back", body, new Color(ink.r, ink.g, ink.b, .4f), (int)(15 * u));
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
        Text(new Rect(0, H * .12f + 170 * u, W, 80 * u), "SKY CONQUERED", hero, ink, (int)(60 * u));
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
