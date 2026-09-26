using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoBehaviour
{
    enum Mode { Title, Playing, LevelDone, Won }

    public static GM I;
    static readonly HashSet<string> unlocked = new HashSet<string>();

    public int coins;
    public Vector2 checkpoint;

    Mode mode = Mode.Title;
    int levelIndex;
    Ball ball;
    CameraFollow camFollow;
    LevelBuilder.Result level;
    float levelTime, totalTime;
    int deaths, totalDeaths, totalCoins, totalCoinsMax;
    string toast;
    float toastT;
    float titleT;

    static readonly (string id, string sprite, string key, string label)[] Abilities =
    {
        ("jump", "bounce", "SPACE", "BOUNCE"),
        ("crouch", "crouch", "S", "CROUCH"),
        ("dash", "spin", "SHIFT", "SPIN"),
        ("grow", "grow", "G", "GROW"),
        ("teleport", "teleport", "T", "BLINK"),
        ("freeze", "freeze", "F", "FREEZE"),
        ("invis", "invisibility", "V", "VANISH"),
    };

    public static bool Has(string id) => unlocked.Contains(id);

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
        var cam = Camera.main;
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
        if (level.root != null) Destroy(level.root.gameObject);
        if (ball != null) Destroy(ball.gameObject);
        var def = Levels.All[idx];
        level = LevelBuilder.Build(def.map);
        checkpoint = level.start;
        ball = Ball.Create(level.start);
        camFollow.Follow(ball.rb, level.min, level.max);
        coins = 0;
        deaths = 0;
        levelTime = 0;
        foreach (var a in def.unlock.Split(','))
            unlocked.Add(a.Trim());
        ShowToast(def.name + "\n" + def.hint);
    }

    void ShowToast(string s)
    {
        toast = s;
        toastT = 5f;
    }

    void Update()
    {
        titleT += Time.unscaledDeltaTime;
        toastT -= Time.unscaledDeltaTime;
        switch (mode)
        {
            case Mode.Title:
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    mode = Mode.Playing;
                    ball.controlLocked = false;
                    Sfx.Play("heal");
                    ShowToast(Levels.All[levelIndex].name + "\n" + Levels.All[levelIndex].hint);
                }
                break;
            case Mode.Playing:
                levelTime += Time.deltaTime;
                if (!ball.dead && ball.rb.position.y < level.min.y - 6f) ball.Die();
                if (Input.GetKeyDown(KeyCode.R)) LoadLevel(levelIndex);
                break;
            case Mode.Won:
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
                {
                    unlocked.Clear();
                    totalTime = 0; totalDeaths = 0; totalCoins = 0; totalCoinsMax = 0;
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
        yield return new WaitForSeconds(.9f);
        ball.Respawn(checkpoint + Vector2.up * .3f);
    }

    public void OnPortal(Vector2 p)
    {
        if (mode != Mode.Playing) return;
        StartCoroutine(PortalCo(p));
    }

    IEnumerator PortalCo(Vector2 p)
    {
        mode = Mode.LevelDone;
        ball.controlLocked = true;
        ball.rb.simulated = false;
        Sfx.Play("win");
        Fx.Ring(p, Gfx.Pink, 3f);
        Fx.Burst(p, Gfx.Gold, 40, 12f, .25f, 0f, 1f);
        Fx.Burst(p, Gfx.Pink, 40, 9f, .2f, 0f, 1f);
        Vector3 start = ball.transform.position;
        for (float t = 0; t < .8f; t += Time.deltaTime)
        {
            float k = t / .8f;
            ball.transform.position = Vector3.Lerp(start, p, k * k);
            ball.transform.localScale = Vector3.one * (1 - k);
            yield return null;
        }
        ball.transform.localScale = Vector3.zero;
        totalTime += levelTime;
        totalDeaths += deaths;
        totalCoins += coins;
        totalCoinsMax += level.coins;
        yield return new WaitForSeconds(2.2f);
        if (levelIndex + 1 < Levels.All.Length)
        {
            LoadLevel(levelIndex + 1);
            mode = Mode.Playing;
            Sfx.Play("heal");
        }
        else
        {
            mode = Mode.Won;
        }
    }

    // ---------- HUD ----------
    GUIStyle big, mid, small, center;

    void Styles()
    {
        if (big != null) return;
        big = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        mid = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        small = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        center = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
    }

    static void Shadowed(Rect r, string s, GUIStyle st, Color c)
    {
        var old = GUI.color;
        GUI.color = new Color(0, 0, 0, c.a * .8f);
        GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), s, st);
        GUI.color = c;
        GUI.Label(r, s, st);
        GUI.color = old;
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void SpriteIcon(Rect r, Sprite s, Color tint)
    {
        if (s == null) return;
        var old = GUI.color;
        GUI.color = tint;
        var tr = s.textureRect;
        var tex = s.texture;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        float aspect = tr.width / tr.height;
        Rect dst = aspect > 1 ? new Rect(r.x, r.y + (r.height - r.width / aspect) / 2, r.width, r.width / aspect)
                              : new Rect(r.x + (r.width - r.height * aspect) / 2, r.y, r.height * aspect, r.height);
        GUI.DrawTextureWithTexCoords(dst, tex, uv);
        GUI.color = old;
    }

    void OnGUI()
    {
        Styles();
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float W = Screen.width / scale, H = 720f;

        if (mode == Mode.Title)
        {
            Box(new Rect(0, 0, W, H), new Color(0.04f, 0.05f, 0.15f, .75f));
            float bob = Mathf.Sin(titleT * 3f) * 10f;
            Shadowed(new Rect(0, 120 + bob, W, 100), "BALL STATES", big, Gfx.Gold);
            Shadowed(new Rect(0, 210, W, 40), "one ball. ten states. pure physics.", small, new Color(.8f, .85f, 1f));
            string[] cyc = { "idle", "spin", "bounce", "teleport", "grow", "crouch", "stun", "heal", "invisibility", "freeze" };
            string st = cyc[(int)(titleT * 1.5f) % cyc.Length];
            SpriteIcon(new Rect(W / 2 - 90, 270, 180, 180), Gfx.Ball(st), Color.white);
            Shadowed(new Rect(0, 455, W, 30), st.ToUpper(), small, Gfx.Pink);
            if (Mathf.Repeat(titleT, 1f) < .7f)
                Shadowed(new Rect(0, 530, W, 50), "CLICK / PRESS ANY KEY", mid, Color.white);
            Shadowed(new Rect(0, 620, W, 30), "A/D roll  -  SPACE jump  -  R restart level", small, new Color(.6f, .65f, .8f));
            return;
        }

        // hearts
        for (int i = 0; i < Ball.MaxHearts; i++)
        {
            bool full = ball != null && i < ball.hearts;
            var old = GUI.color;
            GUI.color = full ? Gfx.Spike : new Color(1, 1, 1, .2f);
            GUI.DrawTexture(new Rect(24 + i * 44, 22, 36, 36), Gfx.Circle.texture);
            GUI.color = old;
        }
        Shadowed(new Rect(170, 18, 400, 44), "COINS " + coins + "/" + level.coins, center, Gfx.Gold);
        Shadowed(new Rect(W - 420, 18, 400, 44), Levels.All[levelIndex].name + "   " + levelTime.ToString("0.0") + "s", new GUIStyle(center) { alignment = TextAnchor.MiddleRight }, Color.white);
        Shadowed(new Rect(W - 420, 52, 400, 30), "LEVEL " + (levelIndex + 1) + "/" + Levels.All.Length + "   deaths " + deaths, new GUIStyle(small) { alignment = TextAnchor.MiddleRight }, new Color(.7f, .75f, .9f));

        // ability bar
        var list = new List<int>();
        for (int i = 0; i < Abilities.Length; i++) if (Has(Abilities[i].id)) list.Add(i);
        float size = 78, gap = 12;
        float total = list.Count * size + (list.Count - 1) * gap;
        float x0 = W / 2 - total / 2, y0 = H - size - 40;
        foreach (int i in list)
        {
            var a = Abilities[i];
            float cd = 0, cdMax = 1;
            bool active = false;
            if (ball != null)
            {
                switch (a.id)
                {
                    case "dash": cd = ball.dashCd; cdMax = Ball.DashCd; active = ball.dashCd > Ball.DashCd - .3f; break;
                    case "teleport": cd = ball.teleCd; cdMax = Ball.TeleCd; active = ball.teleCd > Ball.TeleCd - .3f; break;
                    case "freeze": cd = ball.freezeCd; cdMax = Ball.FreezeCd; active = ball.frozenT > 0; break;
                    case "invis": cd = ball.invisCd; cdMax = Ball.InvisCd; active = ball.invisT > 0; break;
                    case "grow": active = ball.grown; break;
                    case "crouch": active = ball.crouching; break;
                }
            }
            var r = new Rect(x0, y0, size, size);
            Box(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), active ? Gfx.Gold : new Color(.16f, .68f, 1f, .6f));
            Box(r, new Color(.05f, .07f, .18f, .92f));
            SpriteIcon(new Rect(r.x + 8, r.y + 6, size - 16, size - 16), Gfx.Ball(a.sprite), Color.white);
            if (cd > 0)
                Box(new Rect(r.x, r.y + r.height * (1 - cd / cdMax), r.width, r.height * (cd / cdMax)), new Color(0, 0, 0, .65f));
            Shadowed(new Rect(r.x - 10, r.y + size + 2, size + 20, 22), a.key, small, Color.white);
            Shadowed(new Rect(r.x - 10, r.y - 26, size + 20, 22), a.label, new GUIStyle(small) { fontSize = 14 }, new Color(.7f, .8f, 1f));
            x0 += size + gap;
        }

        if (toastT > 0 && mode == Mode.Playing)
        {
            float a = Mathf.Clamp01(toastT);
            var lines = toast.Split('\n');
            Box(new Rect(W / 2 - 440, 100, 880, 100), new Color(.05f, .07f, .18f, .8f * a));
            Shadowed(new Rect(0, 108, W, 50), lines[0], mid, new Color(Gfx.Gold.r, Gfx.Gold.g, Gfx.Gold.b, a));
            if (lines.Length > 1) Shadowed(new Rect(0, 155, W, 36), lines[1], small, new Color(1, 1, 1, a));
        }

        if (mode == Mode.LevelDone)
        {
            Shadowed(new Rect(0, 250, W, 100), "LEVEL CLEAR!", big, Gfx.Gold);
            Shadowed(new Rect(0, 350, W, 40), $"{levelTime:0.0}s    coins {coins}/{level.coins}    deaths {deaths}", mid, Color.white);
        }
        if (mode == Mode.Won)
        {
            Box(new Rect(0, 0, W, H), new Color(0.04f, 0.05f, 0.15f, .8f));
            Shadowed(new Rect(0, 130, W, 100), "YOU WIN!", big, Gfx.Gold);
            SpriteIcon(new Rect(W / 2 - 80, 240, 160, 160), Gfx.Ball("heal"), Color.white);
            float coinPct = totalCoinsMax > 0 ? totalCoins / (float)totalCoinsMax : 0;
            string rank = coinPct > .9f && totalDeaths == 0 ? "S" : coinPct > .7f && totalDeaths < 4 ? "A" : coinPct > .4f ? "B" : "C";
            Shadowed(new Rect(0, 420, W, 40), $"time {totalTime:0.0}s    coins {totalCoins}/{totalCoinsMax}    deaths {totalDeaths}", mid, Color.white);
            Shadowed(new Rect(0, 470, W, 90), "RANK " + rank, big, Gfx.Pink);
            Shadowed(new Rect(0, 590, W, 40), "press R to play again", small, new Color(.7f, .75f, .9f));
        }
    }
}
