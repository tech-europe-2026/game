using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    const float ViewH = 224f;
    const float Dt = 1f / 60f;

    enum State { Title, Playing, Dying, Results, GameOver }

    class Ent
    {
        public string type;
        public Vector2 p, v, home;
        public float a, b, t;
        public int state, life, cooldown;
        public bool alive = true;
        public SpriteRenderer sr;
    }

    class Fx
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public int rate, t;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
    }

    LevelData level;
    World world;
    Player player;
    State state;
    Camera cam;
    RenderTexture rt;
    Transform worldRoot, entRoot, hud;
    AudioSource music, sfx;

    Sprite[] kitFeet, kitBall, enemySpr, ringSpr, springSpr, dashSpr, checkSpr, goalSpr, boomSpr, dustSpr;
    Sprite spikeSpr, logoSpr, iconSpr;
    SpriteRenderer playerSr;
    readonly SpriteRenderer[] ghosts = new SpriteRenderer[3];
    readonly List<(Vector3 pos, Quaternion rot, Sprite spr, bool flip)> trail = new List<(Vector3, Quaternion, Sprite, bool)>();

    readonly List<Ent> ents = new List<Ent>();
    readonly List<Ent> loose = new List<Ent>();
    readonly List<Fx> fxs = new List<Fx>();

    class Layer { public SpriteRenderer[] tiles; public SpriteRenderer fill; public float fx, fy, baseY, h; }
    readonly List<Layer> layers = new List<Layer>();

    Vector2 camPos;
    float viewW = 398f;
    int tick, score, rings, lives, stateTimer, enemyChain, nextLifeRings;
    float time, checkTime;
    Vector2 checkpoint;
    Ent goal;
    bool jumpLatch;
    int animT;
    float animFrame;
    int titleCardT;
    int timeBonus, ringBonus, tallyTotal;
    bool tallyDone;

    BitmapText tScoreL, tTimeL, tRingsL, tScore, tTime, tRings, tLives;
    SpriteRenderer lifeIcon;
    BitmapText cardA, cardB, cardC, msgBig, msgA, msgB, msgC, msgD, msgE, msgF, msgG, press, controls;
    SpriteRenderer logo, cardBar;

    void Awake()
    {
        Application.targetFrameRate = 60;
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(c.gameObject);

        var clearCam = new GameObject("ClearCam").AddComponent<Camera>();
        clearCam.cullingMask = 0;
        clearCam.clearFlags = CameraClearFlags.SolidColor;
        clearCam.backgroundColor = Color.black;
        clearCam.depth = -10;

        cam = new GameObject("PixelCam").AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ViewH / 2;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(90, 160, 240, 255);
        cam.depth = 0;
        cam.transform.position = new Vector3(0, 0, -10);
        EnsureRT();

        var au = new GameObject("Audio");
        au.AddComponent<AudioListener>();
        music = au.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = 0.55f;
        sfx = au.AddComponent<AudioSource>();
        sfx.volume = 0.8f;

        LoadSprites();
        level = LevelData.Load("level1");
        world = new World(level);
        worldRoot = new GameObject("World").transform;
        BuildTerrain();
        BuildParallax();
        BuildHud();

        player = new Player(world, level.loops, Vector2.zero) { Sfx = Play };
        playerSr = Art.Make("Player", worldRoot, kitFeet[0], 10);
        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i] = Art.Make("Ghost", worldRoot, null, 9);
            ghosts[i].color = new Color(1f, 0.55f, 0.35f, 0.45f - i * 0.12f);
        }
        EnterTitle();
    }

    void EnsureRT()
    {
        float aspect = Mathf.Clamp((float)Screen.width / Mathf.Max(1, Screen.height), 1.25f, 2.2f);
        int w = Mathf.RoundToInt(ViewH * aspect / 2f) * 2;
        if (rt != null && rt.width == w) return;
        if (rt != null) { cam.targetTexture = null; rt.Release(); }
        rt = new RenderTexture(w, (int)ViewH, 16) { filterMode = FilterMode.Point };
        rt.Create();
        cam.targetTexture = rt;
        viewW = w;
        if (hud != null) LayoutHud();
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || rt == null) return;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), rt, ScaleMode.ScaleToFit, false);
    }

    void LoadSprites()
    {
        kitFeet = Art.Sheet("kit", 56, 56, new Vector2(0.5f, 2f / 56f), 38);
        kitBall = Art.Sheet("kit", 56, 56, new Vector2(0.5f, 20.5f / 56f), 38);
        var enemiesBottom = Art.Sheet("enemies", 40, 32, new Vector2(0.5f, 0f));
        var enemiesMid = Art.Sheet("enemies", 40, 32, new Vector2(0.5f, 0.5f));
        enemySpr = new[] { enemiesBottom[0], enemiesBottom[1], enemiesBottom[2], enemiesBottom[3], enemiesMid[4], enemiesMid[5], enemiesMid[6], enemiesMid[7] };
        ringSpr = Art.Sheet("rings", 16, 16, new Vector2(0.5f, 0.5f));
        springSpr = Art.Sheet("springs", 32, 32, new Vector2(0.5f, 0f));
        dashSpr = Art.Sheet("dash", 40, 12, new Vector2(0.5f, 0f));
        checkSpr = Art.Sheet("check", 16, 48, new Vector2(0.5f, 0f));
        goalSpr = Art.Sheet("goal", 48, 56, new Vector2(0.5f, 0f));
        boomSpr = Art.Sheet("boom", 32, 32, new Vector2(0.5f, 0.5f));
        dustSpr = Art.Sheet("dust", 16, 16, new Vector2(0.5f, 0.5f));
        spikeSpr = Art.Single("Sprites/spikes", new Vector2(0.5f, 0f));
        logoSpr = Art.Single("Sprites/logo", new Vector2(0.5f, 0.5f));
        iconSpr = Art.Single("Sprites/kit_icon", new Vector2(0f, 0f));
    }

    void BuildTerrain()
    {
        foreach (var c in level.chunks)
        {
            var sr = Art.Make(c.file, worldRoot, Art.Single("Terrain/" + c.file, Vector2.zero), 0);
            sr.transform.position = new Vector3(c.x, c.y, 0);
        }
    }

    void BuildParallax()
    {
        AddLayer("bg_sky", 0.02f, 0f, -8f, -100, new Color32(212, 240, 255, 255));
        AddLayer("bg_far", 0.08f, 0.05f, 24f, -90, new Color32(116, 192, 132, 255));
        AddLayer("bg_mid", 0.22f, 0.12f, -86f, -80, new Color32(12, 58, 26, 255));
        AddLayer("bg_near", 0.4f, 0.2f, -56f, -70, new Color32(6, 32, 12, 255));
    }

    void AddLayer(string name, float fx, float fy, float baseY, int order, Color fillCol)
    {
        var spr = Art.Single("Sprites/" + name, Vector2.zero);
        var l = new Layer { fx = fx, fy = fy, baseY = baseY, h = spr.rect.height, tiles = new SpriteRenderer[3] };
        for (int i = 0; i < 3; i++) l.tiles[i] = Art.Make(name, cam.transform, spr, order);
        l.fill = Art.Rect(name + "_fill", cam.transform, fillCol, 0, 0, 1, 1, order);
        layers.Add(l);
    }

    void UpdateParallax()
    {
        float halfH = ViewH / 2;
        foreach (var l in layers)
        {
            float off = Mathf.Repeat(camPos.x * l.fx, 512f);
            float shift = Mathf.Clamp(-camPos.y * l.fy, -60f, 60f);
            float by = -halfH + l.baseY + shift;
            float left = -viewW / 2 - off;
            for (int i = 0; i < 3; i++)
                l.tiles[i].transform.localPosition = new Vector3(Mathf.Round(left + i * 512f), Mathf.Round(by), 10);
            float fillTop = by + (l.fx < 0.05f ? l.h : 2f);
            if (l.fx < 0.05f)
            {
                l.fill.transform.localPosition = new Vector3(-viewW / 2 - 4, Mathf.Round(fillTop), 10);
                l.fill.transform.localScale = new Vector3(viewW + 8, 200, 1);
            }
            else
            {
                l.fill.transform.localPosition = new Vector3(-viewW / 2 - 4, -halfH - 200, 10);
                l.fill.transform.localScale = new Vector3(viewW + 8, Mathf.Max(0, Mathf.Round(by) + halfH + 202), 1);
            }
        }
    }

    // ------------------------------------------------------------------ HUD
    void BuildHud()
    {
        hud = new GameObject("HUD").transform;
        hud.SetParent(cam.transform, false);
        hud.localPosition = new Vector3(0, 0, 10);
        tScoreL = new BitmapText(hud, "font_gold", 200); tScoreL.Set("SCORE");
        tTimeL = new BitmapText(hud, "font_gold", 200); tTimeL.Set("TIME");
        tRingsL = new BitmapText(hud, "font_gold", 200); tRingsL.Set("RINGS");
        tScore = new BitmapText(hud, "font_small", 200, 1);
        tTime = new BitmapText(hud, "font_small", 200, 1);
        tRings = new BitmapText(hud, "font_small", 200, 1);
        tLives = new BitmapText(hud, "font_small", 200);
        lifeIcon = Art.Make("LifeIcon", hud, iconSpr, 200);

        cardBar = Art.Rect("CardBar", hud, new Color32(24, 70, 200, 235), 0, 0, 1, 1, 190);
        cardA = new BitmapText(hud, "font_big", 201, 1);
        cardB = new BitmapText(hud, "font_big", 201, 1);
        cardC = new BitmapText(hud, "font_gold", 201, 1);
        logo = Art.Make("Logo", hud, logoSpr, 201);
        msgBig = new BitmapText(hud, "font_big", 201, 0.5f);
        msgA = new BitmapText(hud, "font_big", 201, 0.5f);
        msgB = new BitmapText(hud, "font_gold", 201, 0f);
        msgC = new BitmapText(hud, "font_small", 201, 1f);
        msgD = new BitmapText(hud, "font_gold", 201, 0f);
        msgE = new BitmapText(hud, "font_small", 201, 1f);
        msgF = new BitmapText(hud, "font_gold", 201, 0f);
        msgG = new BitmapText(hud, "font_small", 201, 1f);
        press = new BitmapText(hud, "font_small", 201, 0.5f);
        controls = new BitmapText(hud, "font_small", 201, 0.5f);
        LayoutHud();
    }

    void LayoutHud()
    {
        float l = -viewW / 2 + 10, top = ViewH / 2 - 6;
        tScoreL.SetPos(l, top); tScore.SetPos(l + 100, top);
        tTimeL.SetPos(l, top - 13); tTime.SetPos(l + 100, top - 13);
        tRingsL.SetPos(l, top - 26); tRings.SetPos(l + 100, top - 26);
        lifeIcon.transform.localPosition = new Vector3(Mathf.Round(l), -ViewH / 2 + 8, 0);
        tLives.SetPos(l + 32, -ViewH / 2 + 20);
        logo.transform.localPosition = new Vector3(0, 32, 0);
        msgBig.SetPos(0, 72);
        msgA.SetPos(0, 44);
        msgB.SetPos(-120, 4); msgC.SetPos(120, 4);
        msgD.SetPos(-120, -16); msgE.SetPos(120, -16);
        msgF.SetPos(-120, -44); msgG.SetPos(120, -44);
        press.SetPos(0, -60);
        controls.SetPos(0, -86);
    }

    void ShowHud(bool on)
    {
        foreach (var t in new[] { tScoreL, tTimeL, tRingsL, tScore, tTime, tRings, tLives }) t.SetActive(on);
        lifeIcon.enabled = on;
    }

    void HideMessages()
    {
        foreach (var t in new[] { cardA, cardB, cardC, msgBig, msgA, msgB, msgC, msgD, msgE, msgF, msgG, press, controls }) t.SetActive(false);
        logo.enabled = false;
        cardBar.enabled = false;
    }

    // ------------------------------------------------------------------ entities
    void BuildEntities()
    {
        if (entRoot != null) Destroy(entRoot.gameObject);
        entRoot = new GameObject("Entities").transform;
        entRoot.SetParent(worldRoot, false);
        ents.Clear();
        loose.Clear();
        fxs.Clear();
        goal = null;
        foreach (var o in level.objects)
        {
            var e = new Ent { type = o.type, p = new Vector2(o.x, o.y), home = new Vector2(o.x, o.y), a = o.a, b = o.b };
            switch (o.type)
            {
                case "start":
                    checkpoint = e.p;
                    continue;
                case "ramp":
                    continue;
                case "ring":
                    e.sr = Art.Make("Ring", entRoot, ringSpr[0], 6);
                    break;
                case "walker":
                    e.sr = Art.Make("Beetle", entRoot, enemySpr[0], 7);
                    e.v = new Vector2(-0.6f, 0);
                    break;
                case "flyer":
                    e.sr = Art.Make("Wasp", entRoot, enemySpr[4], 7);
                    break;
                case "spring":
                    e.sr = Art.Make("Spring", entRoot, springSpr[o.b >= 12 ? 2 : 0], 5);
                    e.sr.transform.rotation = Quaternion.Euler(0, 0, o.a * Mathf.Rad2Deg);
                    break;
                case "dash":
                    e.sr = Art.Make("Dash", entRoot, dashSpr[0], 4);
                    if (o.a < 0) e.sr.flipX = true;
                    break;
                case "spikes":
                {
                    int n = Mathf.Max(1, Mathf.RoundToInt(o.a / 16f));
                    e.sr = Art.Make("Spikes", entRoot, spikeSpr, 5);
                    for (int i = 1; i < n; i++)
                    {
                        var s2 = Art.Make("Spike", e.sr.transform, spikeSpr, 5);
                        s2.transform.localPosition = new Vector3(i * 16, 0, 0);
                    }
                    e.sr.transform.position = new Vector3(o.x - (n - 1) * 8, o.y, 0);
                    ents.Add(e);
                    continue;
                }
                case "check":
                    e.sr = Art.Make("Check", entRoot, checkSpr[0], 4);
                    e.p = new Vector2(o.x, world.GroundBelow(o.x, o.y + 40) is float gy && gy > -9999 ? gy : o.y);
                    break;
                case "goal":
                    e.sr = Art.Make("Goal", entRoot, goalSpr[0], 4);
                    goal = e;
                    break;
                default:
                    continue;
            }
            e.sr.transform.position = new Vector3(e.p.x, e.p.y, 0);
            ents.Add(e);
        }
    }

    void Spawn(Sprite[] frames, Vector2 p, int rate, int order = 12)
    {
        var sr = Art.Make("Fx", entRoot, frames[0], order);
        sr.transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0);
        fxs.Add(new Fx { sr = sr, frames = frames, rate = rate });
    }

    void ScatterRings()
    {
        int n = Mathf.Min(rings, 32);
        float angle = 101.25f * Mathf.Deg2Rad, speed = 4f;
        bool flip = false;
        for (int i = 0; i < n; i++)
        {
            var e = new Ent { type = "loose", p = player.pos, life = 256 };
            e.v = new Vector2(Mathf.Cos(angle) * speed * (flip ? -1 : 1), Mathf.Sin(angle) * speed);
            if (flip) angle += 22.5f * Mathf.Deg2Rad;
            flip = !flip;
            if (i == 15) { speed = 2f; angle = 101.25f * Mathf.Deg2Rad; }
            e.sr = Art.Make("Loose", entRoot, ringSpr[0], 6);
            loose.Add(e);
        }
        rings = 0;
    }

    void Play(string name)
    {
        var c = Art.Clip(name);
        if (c != null) sfx.PlayOneShot(c);
    }

    // ------------------------------------------------------------------ flow
    void EnterTitle()
    {
        state = State.Title;
        BuildEntities();
        player.Spawn(checkpoint);
        playerSr.enabled = false;
        foreach (var g in ghosts) g.enabled = false;
        camPos = new Vector2(400, 60);
        ShowHud(false);
        HideMessages();
        logo.enabled = true;
        press.SetActive(true);
        press.Set("PRESS ENTER");
        controls.SetActive(true);
        controls.Set("ARROWS MOVE  Z JUMP  DOWN ROLL");
        music.Stop();
    }

    void StartGame()
    {
        score = 0;
        lives = 3;
        time = 0;
        checkTime = 0;
        nextLifeRings = 100;
        BuildEntities();
        Respawn();
        Play("select");
        music.clip = Art.Clip("theme");
        music.loop = true;
        music.Play();
    }

    void Respawn()
    {
        state = State.Playing;
        rings = 0;
        time = checkTime;
        enemyChain = 0;
        player.Spawn(checkpoint);
        playerSr.enabled = true;
        camPos = player.pos + new Vector2(0, 16);
        ShowHud(true);
        HideMessages();
        titleCardT = 150;
        cardBar.enabled = cardA.Root.gameObject.activeSelf || true;
        cardA.SetActive(true); cardA.Set("VERDANT");
        cardB.SetActive(true); cardB.Set("GROVE");
        cardC.SetActive(true); cardC.Set("ZONE  ACT 1");
    }

    void Hurt(float fromX)
    {
        if (player.invuln > 0 || player.dead || player.finished) return;
        if (rings > 0)
        {
            ScatterRings();
            player.Hurt(fromX);
            Play("lose");
        }
        else Kill();
    }

    void Kill()
    {
        if (player.dead) return;
        player.Die();
        Play("hurt");
        state = State.Dying;
        stateTimer = 110;
        music.Stop();
    }

    void Finish()
    {
        player.finished = true;
        goal.state = 1;
        goal.t = 0;
        stateTimer = 0;
        Play("check");
        int s = Mathf.FloorToInt(time);
        timeBonus = s < 30 ? 50000 : s < 45 ? 10000 : s < 60 ? 5000 : s < 90 ? 4000 : s < 120 ? 3000 : s < 180 ? 2000 : s < 240 ? 1000 : 100;
        ringBonus = rings * 100;
        tallyTotal = 0;
        tallyDone = false;
    }

    // ------------------------------------------------------------------ loop
    void Update()
    {
        EnsureRT();
        if (JumpDown()) jumpLatch = true;
        if (Confirm()) confirmLatch = true;
        int steps = 0;
        accum += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        while (accum >= Dt && steps < 5)
        {
            accum -= Dt;
            steps++;
            Tick();
            jumpLatch = false;
            confirmLatch = false;
        }
        Render();
    }

    float accum;

    static bool JumpDown() =>
        Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.C);

    static bool JumpHeld() =>
        Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.Space) ||
        Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.C);

    static bool Confirm() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);

    PadInput ReadPad()
    {
        return new PadInput
        {
            left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
            right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D),
            up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
            down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S),
            jumpHeld = JumpHeld(),
            jumpPressed = jumpLatch,
        };
    }

    bool confirmLatch;

    void Tick()
    {
        tick++;
        bool confirm = confirmLatch;
        switch (state)
        {
            case State.Title:
                camPos.x += 1.5f;
                if (camPos.x > level.width - 400) camPos.x = 400;
                camPos.y = 60 + Mathf.Sin(tick * 0.01f) * 10;
                if (confirm) StartGame();
                break;
            case State.Playing:
                PlayTick();
                break;
            case State.Dying:
                player.Step(default);
                if (--stateTimer <= 0)
                {
                    lives--;
                    if (lives <= 0)
                    {
                        state = State.GameOver;
                        HideMessages();
                        msgBig.SetActive(true);
                        msgBig.Set("GAME OVER");
                        msgBig.SetPos(0, 14);
                        press.SetActive(true);
                        press.Set("PRESS ENTER");
                        stateTimer = 60;
                    }
                    else
                    {
                        Respawn();
                        music.Play();
                    }
                }
                break;
            case State.Results:
                PlayTick();
                ResultsTick(confirm);
                break;
            case State.GameOver:
                if (stateTimer > 0) stateTimer--;
                else if (confirm) { msgBig.SetPos(0, 72); EnterTitle(); }
                break;
        }
        UpdateFx();
    }

    void PlayTick()
    {
        var pad = ReadPad();
        if (titleCardT > 90) { pad = default; }
        if (titleCardT > 0) titleCardT--;
        player.Step(pad);
        if (!player.finished) time += Dt;
        if (player.grounded) enemyChain = 0;

        if (player.skidding && tick % 4 == 0) { Spawn(dustSpr, player.Feet, 4); if (tick % 16 == 0) Play("skid"); }
        if (player.spindash && tick % 6 == 0) Spawn(dustSpr, player.Feet + new Vector2(-player.facing * 10, 4), 4);

        UpdateEntities();
        UpdateLoose();

        if (player.pos.y < level.yMin + 60 && !player.dead) Kill();
        if (!player.finished && goal != null && player.pos.x >= goal.p.x) Finish();

        if (player.finished && state == State.Playing)
        {
            stateTimer++;
            if (stateTimer == 70) { music.Stop(); music.clip = Art.Clip("clear"); music.loop = false; music.Play(); }
            if (stateTimer >= 150) EnterResults();
        }
        UpdateCamera();
    }

    void EnterResults()
    {
        state = State.Results;
        stateTimer = 0;
        HideMessages();
        msgBig.SetActive(true); msgBig.Set("KIT GOT");
        msgA.SetActive(true); msgA.Set("THROUGH ACT 1");
        msgB.SetActive(true); msgB.Set("TIME BONUS");
        msgC.SetActive(true);
        msgD.SetActive(true); msgD.Set("RING BONUS");
        msgE.SetActive(true);
        msgF.SetActive(true); msgF.Set("TOTAL");
        msgG.SetActive(true);
    }

    void ResultsTick(bool confirm)
    {
        stateTimer++;
        if (stateTimer > 90 && !tallyDone)
        {
            int moved = 0;
            int d = Mathf.Min(timeBonus, 200); timeBonus -= d; moved += d;
            d = Mathf.Min(ringBonus, 200); ringBonus -= d; moved += d;
            score += moved;
            tallyTotal += moved;
            if (moved > 0 && tick % 4 == 0) Play("select");
            if (moved == 0) { tallyDone = true; Play("ring"); stateTimer = 90; press.SetActive(true); press.Set("PRESS ENTER"); }
        }
        msgC.Set(timeBonus.ToString());
        msgE.Set(ringBonus.ToString());
        msgG.Set(tallyTotal.ToString());
        if (tallyDone) press.SetActive((tick / 30) % 2 == 0);
        if (tallyDone && confirm) EnterTitle();
    }

    void UpdateEntities()
    {
        var pp = player.pos;
        foreach (var e in ents)
        {
            if (!e.alive) continue;
            if (e.cooldown > 0) e.cooldown--;
            if (Mathf.Abs(e.p.x - camPos.x) > 700) continue;
            switch (e.type)
            {
                case "ring":
                    if (e.state == 0 && !player.dead && (e.p - pp).sqrMagnitude < 18 * 18) CollectRing(e);
                    break;
                case "walker":
                {
                    e.p.x += e.v.x;
                    if (e.p.x < e.a) { e.p.x = e.a; e.v.x = 0.6f; }
                    if (e.p.x > e.b) { e.p.x = e.b; e.v.x = -0.6f; }
                    float gy = world.GroundBelow(e.p.x, e.p.y + 24);
                    if (gy > -9999) e.p.y = gy;
                    EnemyContact(e, e.p + new Vector2(0, 14));
                    break;
                }
                case "flyer":
                    e.t += 1;
                    e.p = e.home + new Vector2(Mathf.Sin(e.t * 0.012f) * 60f, Mathf.Sin(e.t * 0.05f) * e.b * 0.5f);
                    EnemyContact(e, e.p);
                    break;
                case "spring":
                {
                    var dir = new Vector2(-Mathf.Sin(e.a), Mathf.Cos(e.a));
                    var c = e.p + dir * 10f;
                    if (e.cooldown == 0 && !player.dead && (pp - c).sqrMagnitude < 22 * 22)
                    {
                        player.pos = c + dir * (Player.R + 6f);
                        player.Bounce(dir, e.b, Mathf.Abs(dir.x) > 0.1f);
                        e.cooldown = 20;
                        e.t = 12;
                        Play("spring");
                    }
                    if (e.t > 0) e.t--;
                    break;
                }
                case "dash":
                    if (e.cooldown == 0 && (player.grounded || player.loopIdx >= 0) && Mathf.Abs(player.Feet.x - e.p.x) < 22 && Mathf.Abs(player.Feet.y - e.p.y) < 14)
                    {
                        player.DashPad(e.a >= 0 ? 1 : -1);
                        e.cooldown = 20;
                        Play("dash");
                    }
                    break;
                case "spikes":
                {
                    float hw = Mathf.Max(8, e.a / 2f);
                    var f = player.Feet;
                    if (Mathf.Abs(f.x - e.p.x) < hw + 4 && f.y < e.p.y + 18 && pp.y > e.p.y - 4) Hurt(e.p.x);
                    break;
                }
                case "check":
                    if (e.state == 0 && Mathf.Abs(pp.x - e.p.x) < 16 && pp.y > e.p.y - 8 && pp.y < e.p.y + 70)
                    {
                        e.state = 1;
                        checkpoint = e.p;
                        checkTime = time;
                        Play("check");
                    }
                    break;
                case "goal":
                    if (e.state == 1) e.t++;
                    break;
            }
        }
    }

    void CollectRing(Ent e)
    {
        e.state = 1;
        e.t = 0;
        rings++;
        Play("ring");
        if (rings >= nextLifeRings) { lives++; nextLifeRings += 100; Play("check"); }
    }

    void EnemyContact(Ent e, Vector2 center)
    {
        if (player.dead || (player.pos - center).sqrMagnitude > 24 * 24) return;
        if (player.Attacking || player.invuln > 60)
        {
            if (!player.Attacking) return;
            e.alive = false;
            e.sr.enabled = false;
            Spawn(boomSpr, center, 4);
            Play("boom");
            enemyChain++;
            score += enemyChain <= 1 ? 100 : enemyChain == 2 ? 200 : enemyChain == 3 ? 500 : 1000;
            player.EnemyBounce();
        }
        else Hurt(center.x);
    }

    void UpdateLoose()
    {
        for (int i = loose.Count - 1; i >= 0; i--)
        {
            var e = loose[i];
            e.life--;
            e.v.y -= 0.09375f;
            e.p += e.v;
            float gy = world.GroundBelow(e.p.x, e.p.y + 8);
            if (e.v.y < 0 && gy > -9999 && e.p.y - 8 < gy) { e.p.y = gy + 8; e.v.y = -e.v.y * 0.75f; }
            bool collect = e.life < 256 - 64 && !player.dead && (e.p - player.pos).sqrMagnitude < 18 * 18;
            if (collect || e.life <= 0)
            {
                if (collect) { rings++; Play("ring"); Spawn(ringSpr[6..10], e.p, 3); }
                Destroy(e.sr.gameObject);
                loose.RemoveAt(i);
            }
        }
    }

    void UpdateFx()
    {
        for (int i = fxs.Count - 1; i >= 0; i--)
        {
            var f = fxs[i];
            f.t++;
            int k = f.t / f.rate;
            if (k >= f.frames.Length) { Destroy(f.sr.gameObject); fxs.RemoveAt(i); continue; }
            f.sr.sprite = f.frames[k];
        }
    }

    void UpdateCamera()
    {
        if (player.dead) return;
        var v = player.Velocity;
        float tx = player.pos.x + Mathf.Clamp(v.x * 10f, -70f, 70f);
        float dx = tx - camPos.x;
        camPos.x += Mathf.Clamp(dx * 0.12f, -24f, 24f);
        float ty = player.pos.y + 16f;
        if (player.grounded || player.loopIdx >= 0) camPos.y += Mathf.Clamp((ty - camPos.y) * 0.15f, -16f, 16f);
        else
        {
            if (ty > camPos.y + 32) camPos.y = Mathf.Lerp(camPos.y, ty - 32, 0.3f);
            if (ty < camPos.y - 40) camPos.y = Mathf.Lerp(camPos.y, ty + 40, 0.3f);
        }
        ClampCamera();
    }

    void ClampCamera()
    {
        camPos.x = Mathf.Clamp(camPos.x, viewW / 2, level.width - viewW / 2);
        camPos.y = Mathf.Clamp(camPos.y, -300f, level.yMax - ViewH / 2);
    }

    // ------------------------------------------------------------------ render
    void Render()
    {
        if (state == State.Title) ClampCamera();
        cam.transform.position = new Vector3(Mathf.Round(camPos.x), Mathf.Round(camPos.y), -10);
        UpdateParallax();
        int[] ringSeq = { 0, 0, 0, 1, 2, 3, 4, 5 };
        int ringFrame = ringSeq[(tick / 4) % ringSeq.Length];
        foreach (var e in ents)
        {
            if (!e.alive || e.sr == null) continue;
            switch (e.type)
            {
                case "ring":
                    if (e.state == 0) e.sr.sprite = ringSpr[ringFrame];
                    else
                    {
                        int k = (int)(e.t++ / 3);
                        if (k >= 4) { e.sr.enabled = false; e.alive = false; }
                        else e.sr.sprite = ringSpr[6 + k];
                    }
                    break;
                case "walker":
                    e.sr.sprite = enemySpr[(tick / 8) % 4];
                    e.sr.flipX = e.v.x > 0;
                    break;
                case "flyer":
                    e.sr.sprite = enemySpr[4 + (tick / 3) % 4];
                    e.sr.flipX = Mathf.Cos(e.t * 0.012f) > 0;
                    break;
                case "spring":
                    e.sr.sprite = springSpr[(e.b >= 12 ? 2 : 0) + (e.t > 0 ? 1 : 0)];
                    break;
                case "dash":
                    e.sr.sprite = dashSpr[(tick / 4) % 2];
                    break;
                case "check":
                    e.sr.sprite = checkSpr[e.state];
                    break;
                case "goal":
                    if (e.state == 1)
                    {
                        int[] seq = { 0, 1, 2, 3, 4, 3, 2, 1 };
                        int k = (int)(e.t / 3);
                        e.sr.sprite = goalSpr[k < 64 ? seq[k % seq.Length] : 4];
                    }
                    break;
            }
            if (e.type != "spikes") e.sr.transform.position = new Vector3(Mathf.Round(e.p.x), Mathf.Round(e.p.y), 0);
        }
        foreach (var e in loose)
        {
            e.sr.sprite = ringSpr[(tick / 3) % 6];
            e.sr.enabled = e.life > 64 || (tick / 3) % 2 == 0;
            e.sr.transform.position = new Vector3(Mathf.Round(e.p.x), Mathf.Round(e.p.y), 0);
        }
        if (state != State.Title) RenderPlayer();
        RenderHud();
    }

    void RenderPlayer()
    {
        var p = player;
        Sprite spr;
        bool useBall = false;
        float speed = p.Speed;
        bool onGround = p.grounded || p.loopIdx >= 0;
        animT++;
        if (p.dead || p.hurt) spr = kitFeet[34];
        else if (p.trickTimer > 0) spr = kitFeet[35];
        else if (p.spindash) { spr = kitBall[28 + (animT / 2) % 4]; useBall = true; }
        else if (p.ball || p.rolling)
        {
            animFrame += Mathf.Max(0.25f, speed / 6f);
            spr = kitBall[20 + (int)animFrame % 8];
            useBall = true;
        }
        else if (p.springing && p.vel.y > 0) spr = kitFeet[33];
        else if (p.crouch) spr = kitFeet[37];
        else if (p.lookUp) spr = kitFeet[36];
        else if (p.skidding) spr = kitFeet[32];
        else if (onGround && speed < 0.05f) spr = kitFeet[(animT / 8) % 180 < 2 ? 1 : 0];
        else
        {
            animFrame += Mathf.Max(0.12f, speed / 7f);
            if (speed < 3f) spr = kitFeet[2 + (int)animFrame % 8];
            else if (speed < 6f) spr = kitFeet[10 + (int)animFrame % 6];
            else spr = kitFeet[16 + (int)animFrame % 4];
        }
        playerSr.sprite = spr;
        bool flip = p.facing < 0;
        if (onGround && Mathf.Abs(p.gs) > 0.5f && !p.skidding) flip = p.gs < 0;
        playerSr.flipX = flip;
        float ang = Mathf.Atan2(-p.up.x, p.up.y) * Mathf.Rad2Deg;
        Quaternion rot;
        Vector2 at;
        if (useBall || p.dead) { rot = Quaternion.identity; at = p.pos; }
        else
        {
            if (!onGround && !p.springing) ang = Mathf.Abs(ang) < 5 ? 0 : ang;
            if (onGround && Mathf.Abs(ang) < 20 && speed < 3f) ang = 0;
            rot = Quaternion.Euler(0, 0, ang);
            at = p.pos - p.up * Player.R;
        }
        var pos3 = new Vector3(Mathf.Round(at.x), Mathf.Round(at.y), 0);
        playerSr.transform.SetPositionAndRotation(pos3, rot);
        playerSr.enabled = p.invuln == 0 || p.hurt || (tick / 3) % 2 == 0;

        if (tick % 2 == 0)
        {
            trail.Insert(0, (pos3, rot, spr, flip));
            if (trail.Count > 12) trail.RemoveAt(trail.Count - 1);
        }
        bool showGhosts = (p.boost || p.Speed > 9f) && !p.dead;
        for (int i = 0; i < ghosts.Length; i++)
        {
            int idx = 2 + i * 3;
            ghosts[i].enabled = showGhosts && trail.Count > idx;
            if (!ghosts[i].enabled) continue;
            var g = trail[idx];
            ghosts[i].sprite = g.spr;
            ghosts[i].flipX = g.flip;
            ghosts[i].transform.SetPositionAndRotation(g.pos, g.rot);
        }
    }

    void RenderHud()
    {
        bool title = state == State.Title;
        if (title)
        {
            press.SetActive((tick / 30) % 2 == 0);
            return;
        }
        tScore.Set(score.ToString());
        int cs = Mathf.FloorToInt(time * 100f);
        tTime.Set($"{cs / 6000}:{(cs / 100) % 60:00}:{cs % 100:00}");
        tRings.Set(rings.ToString());
        tRingsL.SetColor(rings == 0 && (tick / 16) % 2 == 0 ? new Color(1f, 0.3f, 0.3f) : Color.white);
        tLives.Set("x " + Mathf.Max(0, lives));

        if (titleCardT > 0 && state == State.Playing)
        {
            float k = titleCardT > 120 ? (150 - titleCardT) / 30f : titleCardT < 30 ? titleCardT / 30f : 1f;
            k = 1 - Mathf.Pow(1 - k, 3);
            float xOff = Mathf.Lerp(viewW, 0, k);
            cardBar.enabled = true;
            cardBar.transform.localPosition = new Vector3(Mathf.Round(-viewW / 2 - xOff), -8, 0);
            cardBar.transform.localScale = new Vector3(viewW, 76, 1);
            cardA.SetPos(viewW / 2 - 30 + xOff, 60);
            cardB.SetPos(viewW / 2 - 30 + xOff * 1.3f, 32);
            cardC.SetPos(viewW / 2 - 30 + xOff * 1.6f, 2);
        }
        else if (state == State.Playing && cardBar.enabled)
        {
            cardBar.enabled = false;
            cardA.SetActive(false); cardB.SetActive(false); cardC.SetActive(false);
        }
    }
}
