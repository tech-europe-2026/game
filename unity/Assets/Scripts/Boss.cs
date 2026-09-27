using UnityEngine;

// Red rival ball for the duel level: rolls, jumps, shoots, dashes, grows and blinks.
public class Boss : MonoBehaviour
{
    public static Boss I;
    public const int MaxHearts = 4;
    const float G = 3f, R = .55f, RBig = .9f;

    public int hearts = MaxHearts;
    public bool dead;
    public Rigidbody2D rb;
    public Vector2 LastVel { get; private set; }
    public Vector2 arenaMin, arenaMax;
    public int tier;
    public float pitL, pitR;
    bool HasPit => pitR > pitL;
    bool OverPit(float x) => HasPit && x > pitL - .3f && x < pitR + .3f;

    CircleCollider2D col;
    SpriteRenderer body, glow;
    Transform spinT;
    float quakeT, spin, think = 2f, hurtT, dashT, growT, tellT, flashT, jumpCd;
    string flash = "idle", pending;
    bool grounded, diving;
    readonly Collider2D[] hits = new Collider2D[8];
    ContactFilter2D filter;

    public static Boss Create(Vector2 p, Transform parent)
    {
        var go = new GameObject("Boss");
        go.transform.SetParent(parent, false);
        go.transform.position = p;
        return go.AddComponent<Boss>();
    }

    void Awake()
    {
        I = this;
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = G;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.angularDrag = .5f;
        col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = R;
        col.sharedMaterial = new PhysicsMaterial2D { friction = .8f, bounciness = .15f };
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(Physics2D.AllLayers);
        spinT = new GameObject("spin").transform;
        spinT.SetParent(transform, false);
        body = spinT.gameObject.AddComponent<SpriteRenderer>();
        body.sortingOrder = 20;
        glow = Gfx.Quad(transform, Vector2.zero, Vector2.one * 2.8f, new Color(1f, .3f, .35f, .3f), 19, Gfx.Glow);
    }

    Ball Player => GM.I != null ? GM.I.Player : null;

    void Update()
    {
        if (dead) return;
        float dt = Time.deltaTime;
        hurtT -= dt; flashT -= dt; jumpCd -= dt;
        var p = Player;
        if (p == null || p.dead || !GM.I.Fighting) { Visual(); return; }

        if (tellT > 0)
        {
            tellT -= dt;
            if (tellT <= 0) Act(pending, p);
        }
        else
        {
            think -= dt;
            if (think <= 0) Choose(p);
        }
        if (growT > 0)
        {
            growT -= dt;
            if (growT <= 0) SetSize(false);
        }
        Visual();
    }

    void Choose(Ball p)
    {
        float d = Vector2.Distance(p.rb.position, rb.position);
        float r = Random.value;
        if (tier > 1)
        {
            if (hearts == 1 && r < .15f) pending = "blink";
            else if (d > 7f) pending = r < .3f ? "push" : r < .55f ? "rain" : r < .8f ? "homing" : "shoot";
            else pending = r < .25f ? "push" : r < .45f ? "burst" : r < .65f ? "dash" : r < .85f ? "quake" : "grow";
        }
        else if (tier > 0)
        {
            if (hearts == 1 && r < .2f) pending = "blink";
            else if (d > 7f) pending = r < .4f ? "homing" : r < .75f ? "shoot" : "blink";
            else pending = r < .3f ? "dash" : r < .55f ? "quake" : r < .8f ? "burst" : "grow";
        }
        else if (hearts == 1 && r < .25f) pending = "blink";
        else if (d > 7f) pending = r < .55f ? "shoot" : "blink";
        else pending = r < .45f ? "dash" : r < .7f ? "grow" : "shoot";
        if (pending == "grow" && growT > 0) pending = "dash";
        tellT = pending == "dash" || pending == "quake" || pending == "burst" ? .6f : .45f;
        string tell;
        switch (pending)
        {
            case "dash": case "burst": tell = "spin"; break;
            case "shoot": case "homing": case "push": tell = "parry"; break;
            case "rain": tell = "evolve"; break;
            case "grow": tell = "grow"; break;
            case "quake": tell = "bounce"; break;
            default: tell = "teleport"; break;
        }
        Flash(tell, tellT + .1f);
        Fx.Ring(rb.position, tier > 0 ? new Color(.8f, .4f, 1f) : (Color)Gfx.Coral, 1.3f);
        think = Random.Range(1.8f, 2.8f) - (MaxHearts - hearts) * .25f - tier * .2f;
    }

    void Act(string what, Ball p)
    {
        Vector2 to = (p.rb.position - rb.position).normalized;
        switch (what)
        {
            case "dash":
                dashT = .28f;
                rb.gravityScale = 0;
                rb.velocity = to * 14f;
                Flash("dash", .4f);
                Sfx.Play("dash", .8f);
                Fx.Burst(rb.position, Gfx.Coral, 14, 6f, .14f, 0f, .45f);
                break;
            case "shoot":
                float baseA = Mathf.Atan2(to.y, to.x);
                int n = hearts == 1 ? 3 : 2;
                for (int i = 0; i < n; i++)
                {
                    float a = baseA + (i - (n - 1) / 2f) * .28f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Bullet.Spawn(rb.position + d * (col.radius + .45f), d * 7f);
                }
                Sfx.Play("parry", .7f);
                break;
            case "push":
                float pa = Mathf.Atan2(to.y, to.x);
                for (int i = -2; i <= 2; i++)
                {
                    var d = new Vector2(Mathf.Cos(pa + i * .22f), Mathf.Sin(pa + i * .22f));
                    Bullet.Spawn(rb.position + d * (col.radius + .5f), d * 9f, false, true);
                }
                Sfx.Play("parry", .8f);
                break;
            case "rain":
                for (int i = 0; i < 7; i++)
                {
                    float rx = Mathf.Lerp(arenaMin.x + 1, arenaMax.x - 1, (i + Random.value * .6f) / 7f);
                    Bullet.Spawn(new Vector2(rx, arenaMax.y - .3f), new Vector2(0, -6.5f - Random.value * 2f));
                }
                Fx.AddShake(.2f);
                Sfx.Play("parry", .9f);
                break;
            case "homing":
                for (int i = -1; i <= 1; i += 2)
                    Bullet.Spawn(rb.position + new Vector2(i * (col.radius + .45f), .3f), new Vector2(i * 3f, 3f), true);
                Sfx.Play("parry", .7f);
                break;
            case "burst":
                float a0 = Random.value * Mathf.PI;
                for (int i = 0; i < 8; i++)
                {
                    float a = a0 + i * Mathf.PI / 4f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Bullet.Spawn(rb.position + d * (col.radius + .45f), d * 5f);
                }
                Fx.Ring(rb.position, Gfx.Coral, 2.2f);
                Sfx.Play("parry", .8f);
                break;
            case "quake":
                quakeT = 2f;
                diving = false;
                rb.velocity = new Vector2(to.x * 3f, 16f);
                Flash("bounce", .5f);
                Sfx.Play("jump", .8f);
                break;
            case "grow":
                SetSize(true);
                growT = 3f;
                Sfx.Play("grow");
                Fx.Ring(rb.position, Gfx.Coral, 2f);
                break;
            case "blink":
                float x = p.rb.position.x > (arenaMin.x + arenaMax.x) / 2 ? arenaMin.x + 2.5f : arenaMax.x - 2.5f;
                if (OverPit(x)) x = x < pitL ? pitL - 2f : pitR + 2f;
                var target = new Vector2(x, arenaMin.y + 2f);
                Fx.Burst(rb.position, Gfx.Coral, 16, 5f, .16f, 0f, .5f);
                rb.position = target;
                transform.position = target;
                rb.velocity = Vector2.zero;
                Fx.Ring(target, Color.white, 1.6f);
                Flash("teleport", .5f);
                Sfx.Play("teleport");
                pending = "shoot";
                tellT = .5f;
                break;
        }
    }

    void SetSize(bool big)
    {
        col.radius = big ? RBig : R;
        rb.mass = big ? 3f : 1f;
    }

    void FixedUpdate()
    {
        if (dead) return;
        float fdt = Time.fixedDeltaTime;
        grounded = Physics2D.OverlapCircle(rb.position + Vector2.down * col.radius * .55f, col.radius * .6f, filter, hits) > 1;
        var p = Player;
        if (HasPit && rb.position.y < arenaMin.y - .8f && tellT <= 0)
        {
            quakeT = dashT = 0;
            diving = false;
            rb.gravityScale = G;
            Act("blink", p);
            LastVel = rb.velocity;
            return;
        }
        if (quakeT > 0)
        {
            quakeT -= fdt;
            if (!diving && rb.velocity.y < 1f) { diving = true; rb.velocity = new Vector2(0, -22f); Flash("spin", .6f); }
        }
        else if (dashT > 0)
        {
            dashT -= fdt;
            if (dashT <= 0) rb.gravityScale = G;
        }
        else if (p != null && !p.dead && GM.I.Fighting && hurtT < .6f && tellT <= 0)
        {
            float dx = p.rb.position.x - rb.position.x;
            float dir = Mathf.Abs(dx) > .6f ? Mathf.Sign(dx) : 0f;
            // keep a little distance while the player is dashing at us
            if (p.dashT > 0 && Mathf.Abs(dx) < 4f) dir = -Mathf.Sign(dx);
            if (HasPit && grounded && OverPit(rb.position.x + dir * 1.3f) && !OverPit(rb.position.x))
            {
                bool across = (p.rb.position.x - (pitL + pitR) / 2) * (rb.position.x - (pitL + pitR) / 2) < 0;
                if (across && jumpCd <= 0) { rb.velocity = new Vector2(dir * 8.5f, 12.5f); jumpCd = .9f; Flash("bounce", .3f); }
                else { dir = 0; rb.velocity = new Vector2(rb.velocity.x * .8f, rb.velocity.y); }
            }
            float max = growT > 0 ? 5.5f : 7f;
            if (dir != 0 && (Mathf.Abs(rb.velocity.x) < max || Mathf.Sign(rb.velocity.x) != dir))
                rb.AddForce(new Vector2(dir * 30f * rb.mass, 0));
            bool wantJump = (p.rb.position.y > rb.position.y + 1.5f && Mathf.Abs(dx) < 5f) || Random.value < .004f;
            if (grounded && jumpCd <= 0 && wantJump)
            {
                rb.velocity = new Vector2(rb.velocity.x, 12.5f);
                jumpCd = .9f;
                Flash("bounce", .3f);
            }
        }
        LastVel = rb.velocity;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        var p = c.collider.GetComponent<Ball>();
        if (p == null && diving && !dead)
        {
            diving = false;
            quakeT = 0;
            float fy = rb.position.y - col.radius + .35f;
            Bullet.Spawn(new Vector2(rb.position.x - col.radius - .5f, fy), new Vector2(-8f, 0));
            Bullet.Spawn(new Vector2(rb.position.x + col.radius + .5f, fy), new Vector2(8f, 0));
            Fx.AddShake(.45f);
            Fx.Ring(rb.position, Gfx.Coral, 3f);
            Sfx.Play("land", 1f);
            return;
        }
        if (p == null || dead || p.dead) return;
        Vector2 toP = (p.rb.position - rb.position).normalized;
        float mine = Vector2.Dot(LastVel, toP) * rb.mass;
        float theirs = Vector2.Dot(p.LastVel, -toP) * p.rb.mass;
        if (theirs > 5f && theirs > mine + 1f) Hurt(p.rb.position);
        else if (mine > 5f && mine > theirs + 1f) p.Hurt(rb.position);
        else
        {
            rb.velocity = -toP * 6f + Vector2.up * 3f;
            p.rb.velocity = toP * 6f + Vector2.up * 3f;
            Sfx.Play("land", .6f);
        }
    }

    public void Hurt(Vector2 from)
    {
        if (dead || hurtT > 0) return;
        hearts--;
        hurtT = 1.4f;
        tellT = 0;
        quakeT = 0;
        diving = false;
        dashT = 0;
        rb.gravityScale = G;
        Vector2 away = (rb.position - from).normalized;
        rb.velocity = away * 11f + Vector2.up * 6f;
        Fx.AddShake(.5f);
        Fx.HitStop(.1f);
        Fx.Burst(rb.position, Gfx.Coral, 22, 9f, .18f);
        Sfx.Play("hurt");
        Flash("stun", .6f);
        think = 1.2f;
        if (hearts <= 0)
        {
            dead = true;
            rb.simulated = false;
            body.enabled = glow.enabled = false;
            Fx.Burst(rb.position, Gfx.Coral, 40, 12f, .22f);
            Fx.Burst(rb.position, Color.white, 30, 9f, .16f);
            Fx.Ring(rb.position, Gfx.Coral, 4f);
            Fx.AddShake(.8f);
            GM.I.OnBossDefeated(rb.position);
        }
    }

    void Flash(string s, float t) { flash = s; flashT = t; }

    void Visual()
    {
        string st = hurtT > .5f ? "stun" : flashT > 0 ? flash : growT > 0 ? "grow" : "idle";
        body.sprite = Gfx.Ball("red_" + st) ?? Gfx.Ball("red_idle");
        float d = col.radius * 2f;
        spinT.localScale = new Vector3(d, d, 1);
        spin -= rb.velocity.x / col.radius * Mathf.Rad2Deg * Time.deltaTime;
        spinT.localRotation = Quaternion.Euler(0, 0, st == "stun" ? 0 : spin);
        transform.rotation = Quaternion.identity;
        var c = Color.white;
        if (hurtT > 0 && Mathf.Repeat(Time.time * 12f, 1f) < .5f) c.a = .45f;
        body.color = c;
        float tell = tellT > 0 ? .35f + Mathf.Sin(Time.time * 30f) * .2f : .25f;
        glow.color = tier > 0 ? new Color(.85f, .3f, 1f, tell + .1f) : new Color(1f, .3f, .35f, tell);
        glow.transform.localScale = Vector3.one * (2.8f * col.radius / R);
    }
}
