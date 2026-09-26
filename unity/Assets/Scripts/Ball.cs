using UnityEngine;

public class Ball : MonoBehaviour
{
    public const float RNormal = .6f, RCrouch = .36f, RGrow = 1.05f;
    public const int MaxHearts = 3;
    public const float DashCd = 1.1f, TeleCd = 1.4f, FreezeCd = 4f, InvisCd = 6f;
    public const float FreezeTime = 3f, InvisTime = 2.6f;

    public Rigidbody2D rb;
    CircleCollider2D col;
    Transform squashT, spinT;
    SpriteRenderer body, glow;
    TrailRenderer trail;

    public int hearts = MaxHearts;
    public bool grown, crouching, dead, controlLocked;
    public float dashCd, teleCd, freezeCd, invisCd;
    public float frozenT, invisT, stunT, dashT, flashT, healT, hurtInvT;
    public int facing = 1;

    float targetRadius = RNormal, visRadius = RNormal;
    bool grounded;
    float coyote, jumpBuffer;
    Vector2 squash = Vector2.one;
    Vector2 lastVel;
    string flashState = "bounce";
    float spinAngle;

    PhysicsMaterial2D normalMat, iceMat;
    readonly Collider2D[] hits = new Collider2D[8];
    ContactFilter2D solidFilter;

    public float Radius => col.radius;

    public static Ball Create(Vector2 pos)
    {
        var go = new GameObject("Ball");
        go.transform.position = pos;
        var b = go.AddComponent<Ball>();
        return b;
    }

    void Awake()
    {
        normalMat = new PhysicsMaterial2D { friction = .8f, bounciness = .15f };
        iceMat = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 1f;
        rb.angularDrag = .5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = RNormal;
        col.sharedMaterial = normalMat;

        solidFilter = new ContactFilter2D { useTriggers = false };
        solidFilter.SetLayerMask(Physics2D.AllLayers);

        var sq = new GameObject("squash");
        squashT = sq.transform;
        squashT.SetParent(transform, false);
        var sp = new GameObject("spin");
        spinT = sp.transform;
        spinT.SetParent(squashT, false);
        body = sp.AddComponent<SpriteRenderer>();
        body.sortingOrder = 20;
        glow = Gfx.Quad(squashT, Vector2.zero, Vector2.one * 3f, new Color(.2f, .6f, 1f, .25f), 19, Gfx.Glow);

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.material = Gfx.SpriteMat;
        trail.time = .25f;
        trail.minVertexDistance = .05f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        trail.widthMultiplier = 1f;
        trail.sortingOrder = 18;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(.16f, .68f, 1f), 0), new GradientColorKey(new Color(1f, .47f, .66f), 1) },
            new[] { new GradientAlphaKey(.6f, 0), new GradientAlphaKey(0, 1) });
        trail.colorGradient = grad;
    }

    void Update()
    {
        if (dead) { UpdateVisual(); return; }
        float dt = Time.deltaTime;
        dashCd -= dt; teleCd -= dt; freezeCd -= dt; invisCd -= dt;
        stunT -= dt; flashT -= dt; healT -= dt; hurtInvT -= dt;
        jumpBuffer -= dt;
        if (invisT > 0) invisT -= dt;
        if (frozenT > 0)
        {
            frozenT -= dt;
            if (frozenT <= 0) Unfreeze();
        }

        if (!controlLocked && stunT <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                jumpBuffer = .12f;
            if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow)) && rb.velocity.y > 3f && dashT <= 0)
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * .55f);

            bool wantCrouch = GM.Has("crouch") && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && !grown && frozenT <= 0;
            if (wantCrouch && !crouching) { crouching = true; SetRadius(RCrouch); }
            else if (!wantCrouch && crouching && RoomFor(RNormal)) { crouching = false; SetRadius(RNormal); }

            if (GM.Has("dash") && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K)) && dashCd <= 0 && frozenT <= 0) Dash();
            if (GM.Has("grow") && Input.GetKeyDown(KeyCode.G) && frozenT <= 0) ToggleGrow();
            if (GM.Has("teleport") && Input.GetKeyDown(KeyCode.T) && teleCd <= 0) Teleport();
            if (GM.Has("freeze") && Input.GetKeyDown(KeyCode.F) && freezeCd <= 0 && frozenT <= 0) Freeze();
            if (GM.Has("invis") && Input.GetKeyDown(KeyCode.V) && invisCd <= 0) Vanish();
        }
        UpdateVisual();
    }

    float InputX()
    {
        if (controlLocked || stunT > 0) return 0;
        float x = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1;
        return x;
    }

    void FixedUpdate()
    {
        if (dead) return;
        float r = col.radius;
        int n = Physics2D.OverlapCircle(rb.position + Vector2.down * (r * .55f), r * .6f, solidFilter, hits);
        grounded = false;
        for (int i = 0; i < n; i++)
            if (hits[i] != col && hits[i].attachedRigidbody != rb) { grounded = true; break; }
        coyote = grounded ? .1f : coyote - Time.fixedDeltaTime;

        float x = InputX();
        if (x != 0) facing = x > 0 ? 1 : -1;

        if (dashT > 0)
        {
            dashT -= Time.fixedDeltaTime;
            if (dashT <= 0) rb.gravityScale = 3f;
        }
        else
        {
            float maxSpeed = grown ? 7f : crouching ? 6f : 9.5f;
            float accel = grounded ? 38f : 22f;
            if (frozenT > 0) accel *= .35f;
            float vx = rb.velocity.x;
            if (x != 0 && (Mathf.Abs(vx) < maxSpeed || Mathf.Sign(vx) != x))
                rb.AddForce(new Vector2(x * accel * rb.mass, 0));
            else if (x == 0 && grounded && frozenT <= 0)
                rb.velocity = new Vector2(Mathf.MoveTowards(vx, 0, 14f * Time.fixedDeltaTime), rb.velocity.y);
            if (frozenT <= 0)
                rb.AddTorque(-x * 6f * rb.mass * r);
        }

        if (jumpBuffer > 0 && coyote > 0 && frozenT <= 0) Jump();
        lastVel = rb.velocity;
    }

    void Jump()
    {
        float v = grown ? 10f : crouching ? 10.5f : 12.5f;
        rb.velocity = new Vector2(rb.velocity.x, v);
        jumpBuffer = 0;
        coyote = 0;
        squash = new Vector2(.7f, 1.35f);
        Sfx.Play("jump", .6f);
        Fx.Burst(rb.position + Vector2.down * col.radius, new Color(.8f, .85f, 1f), 6, 3f, .15f, 2f, .3f);
    }

    void Dash()
    {
        dashT = .22f;
        dashCd = DashCd;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(facing * 19f, 0f);
        flashState = "spin";
        flashT = .35f;
        squash = new Vector2(1.4f, .75f);
        Sfx.Play("dash");
        Fx.Ring(rb.position, Gfx.Gold, 1.6f);
        Fx.Burst(rb.position, Gfx.Gold, 12, 6f, .2f, 0f, .4f);
        Fx.AddShake(.15f);
    }

    void ToggleGrow()
    {
        if (!grown)
        {
            if (!RoomFor(RGrow)) { Fx.Burst(rb.position, Color.gray, 5, 2f); return; }
            grown = true;
            crouching = false;
            rb.mass = 5f;
            SetRadius(RGrow);
            Sfx.Play("grow");
            Fx.Ring(rb.position, Gfx.Gold, 2.2f);
            Fx.AddShake(.2f);
        }
        else
        {
            grown = false;
            rb.mass = 1f;
            SetRadius(RNormal);
            Sfx.Play("shrink");
            Fx.Ring(rb.position, Gfx.GroundTop, 1.2f);
        }
    }

    bool RoomFor(float radius)
    {
        float dr = radius - col.radius;
        if (dr <= 0) return true;
        Vector2 probe = rb.position + Vector2.up * (dr + .02f);
        int n = Physics2D.OverlapCircle(probe, radius * .95f, solidFilter, hits);
        for (int i = 0; i < n; i++)
        {
            var hrb = hits[i].attachedRigidbody;
            if (hits[i] != col && (hrb == null || hrb.bodyType == RigidbodyType2D.Static)) return false;
        }
        rb.position = probe;
        return true;
    }

    void SetRadius(float r)
    {
        targetRadius = r;
        col.radius = r;
    }

    void Teleport()
    {
        Vector2 dir = new Vector2(InputX(), 0);
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir.y = 1;
        if (dir == Vector2.zero) dir = new Vector2(facing, 0);
        dir.Normalize();
        const float range = 5f;
        for (float d = range; d >= 1f; d -= .25f)
        {
            Vector2 target = rb.position + dir * d;
            if (Physics2D.OverlapCircle(target, col.radius * .9f, solidFilter, hits) == 0)
            {
                Fx.Burst(rb.position, new Color32(29, 43, 83, 255), 16, 5f, .2f, 0f, .5f);
                Fx.Ring(rb.position, Gfx.GroundTop, 1.2f);
                rb.position = target;
                transform.position = target;
                trail.Clear();
                Fx.Ring(target, Gfx.Pink, 1.8f);
                Fx.Burst(target, Gfx.Pink, 16, 6f, .2f, 0f, .5f);
                flashState = "teleport";
                flashT = .35f;
                teleCd = TeleCd;
                Sfx.Play("teleport");
                return;
            }
        }
        Fx.Burst(rb.position, Color.gray, 5, 2f);
    }

    void Freeze()
    {
        frozenT = FreezeTime;
        freezeCd = FreezeCd;
        col.sharedMaterial = iceMat;
        rb.freezeRotation = true;
        rb.angularVelocity = 0;
        Sfx.Play("freeze");
        Fx.Burst(rb.position, Gfx.Ice, 20, 6f, .18f, 4f, .5f);
        Fx.Ring(rb.position, Gfx.Ice, 1.8f);
    }

    void Unfreeze()
    {
        frozenT = 0;
        col.sharedMaterial = normalMat;
        rb.freezeRotation = false;
        Fx.Burst(rb.position, Gfx.Ice, 14, 5f, .15f, 10f, .5f);
    }

    void Vanish()
    {
        invisT = InvisTime;
        invisCd = InvisCd;
        Sfx.Play("invis");
        Fx.Burst(rb.position, new Color(.8f, .8f, 1f, .6f), 16, 4f, .2f, 0f, .6f);
    }

    public void Hurt(Vector2 from)
    {
        if (dead || hurtInvT > 0) return;
        hearts--;
        hurtInvT = 1.2f;
        stunT = .45f;
        Vector2 away = (rb.position - from).normalized;
        rb.velocity = new Vector2(away.x * 7f + (away.x == 0 ? -facing * 5 : 0), 11f);
        Fx.AddShake(.5f);
        Fx.HitStop(.08f);
        Fx.Burst(rb.position, Gfx.Spike, 16, 8f);
        Sfx.Play("hurt");
        if (hearts <= 0) Die();
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        body.enabled = false;
        glow.enabled = false;
        rb.simulated = false;
        trail.emitting = false;
        Fx.Burst(rb.position, new Color32(29, 43, 83, 255), 30, 12f, .25f);
        Fx.Burst(rb.position, Gfx.Gold, 20, 10f, .2f);
        Fx.AddShake(.8f);
        Fx.HitStop(.12f);
        Sfx.Play("hurt");
        GM.I.OnBallDied();
    }

    public void Respawn(Vector2 p)
    {
        dead = false;
        hearts = MaxHearts;
        grown = crouching = false;
        rb.mass = 1f;
        rb.gravityScale = 3f;
        SetRadius(RNormal);
        visRadius = RNormal;
        Unfreeze();
        invisT = dashT = stunT = 0;
        hurtInvT = 1f;
        rb.simulated = true;
        rb.position = p;
        transform.position = p;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        body.enabled = glow.enabled = true;
        trail.Clear();
        trail.emitting = true;
        flashState = "heal";
        flashT = .6f;
        Fx.Ring(p, Gfx.Green, 2f);
    }

    public void Heal()
    {
        hearts = MaxHearts;
        healT = .8f;
        Sfx.Play("heal");
        Fx.Burst(rb.position, Gfx.Green, 18, 5f, .2f, -4f, .8f);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (dead) return;
        float impact = c.relativeVelocity.magnitude;
        var tile = c.collider.GetComponent<Tile>();
        if (tile != null)
        {
            if (tile.kind == TileKind.Lava && frozenT <= 0) { Die(); return; }
            if (tile.kind == TileKind.Cracked && (dashT > 0 || (grown && impact > 5f)))
            {
                tile.Break(lastVel);
                rb.velocity = lastVel * (grown ? .8f : .9f);
                return;
            }
            if (tile.kind == TileKind.Pad && c.GetContact(0).normal.y > .5f)
            {
                rb.velocity = new Vector2(rb.velocity.x, grown ? 17f : 20f);
                squash = new Vector2(.6f, 1.5f);
                flashState = "bounce";
                flashT = .3f;
                Sfx.Play("bounce");
                Fx.Burst(tile.transform.position, Gfx.Green, 14, 7f, .18f);
                Fx.Ring(tile.transform.position, Gfx.Green, 1.5f);
                return;
            }
        }
        if (impact > 7f)
        {
            float k = Mathf.InverseLerp(7f, 22f, impact);
            Fx.AddShake(Mathf.Lerp(.08f, .4f, k) * (grown ? 2f : 1f));
            Vector2 cp = c.GetContact(0).point;
            Fx.Burst(cp, new Color(.8f, .85f, 1f), 6 + (int)(k * 10), 3f + k * 5f, .15f, 6f, .35f);
            if (impact > 11f) { flashState = "bounce"; flashT = .18f; }
            squash = new Vector2(1f + k * .5f, 1f - k * .4f);
            Sfx.Play("land", .5f + k * .5f);
            if (grown && impact > 9f) Fx.HitStop(.04f);
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (dead || frozenT > 0) return;
        var tile = c.collider.GetComponent<Tile>();
        if (tile != null && tile.kind == TileKind.Lava) Die();
    }

    void OnTriggerEnter2D(Collider2D other) => HandleTrigger(other);
    void OnTriggerStay2D(Collider2D other)
    {
        var t = other.GetComponent<Tile>();
        if (t != null && (t.kind == TileKind.Spike || t.kind == TileKind.Laser)) HandleTrigger(other);
    }

    void HandleTrigger(Collider2D other)
    {
        if (dead) return;
        var t = other.GetComponent<Tile>();
        if (t == null || t.used) return;
        switch (t.kind)
        {
            case TileKind.Spike: Hurt(other.transform.position + Vector3.down); break;
            case TileKind.Laser: if (invisT <= 0) Die(); break;
            case TileKind.Coin:
                t.used = true;
                GM.I.coins++;
                Sfx.Play("coin", .7f);
                Fx.Burst(t.transform.position, Gfx.Gold, 10, 5f, .15f, 0f, .4f);
                Destroy(t.gameObject);
                break;
            case TileKind.Heal:
                t.used = true;
                Heal();
                GM.I.checkpoint = t.transform.position;
                Destroy(t.gameObject);
                break;
            case TileKind.Portal:
                t.used = true;
                GM.I.OnPortal(t.transform.position);
                break;
        }
    }

    string State()
    {
        if (frozenT > 0) return "freeze";
        if (invisT > 0) return "invisibility";
        if (stunT > 0 || (hurtInvT > .6f && !dead)) return "stun";
        if (flashT > 0) return flashState;
        if (healT > 0) return "heal";
        if (grown) return "grow";
        if (crouching) return "crouch";
        return "idle";
    }

    void UpdateVisual()
    {
        float dt = Time.deltaTime;
        visRadius = Mathf.Lerp(visRadius, targetRadius, 1f - Mathf.Exp(-dt * 18f));
        string st = State();
        body.sprite = Gfx.Ball(st);

        squash = Vector2.Lerp(squash, Vector2.one, 1f - Mathf.Exp(-dt * 12f));
        Vector2 v = rb.velocity;
        float stretch = Mathf.Clamp01((v.magnitude - 8f) / 20f) * .25f;
        Vector2 s = squash;
        if (stretch > 0 && st != "freeze")
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            squashT.localRotation = Quaternion.Euler(0, 0, ang);
            s = new Vector2(s.x * (1 + stretch), s.y * (1 - stretch * .6f));
            spinT.localRotation = Quaternion.Euler(0, 0, -ang);
        }
        else
        {
            squashT.localRotation = Quaternion.identity;
            spinT.localRotation = Quaternion.identity;
        }
        float d = visRadius * 2f;
        squashT.localScale = new Vector3(s.x * d, s.y * d, 1);
        transform.rotation = Quaternion.identity;

        bool rolls = st == "idle" || st == "crouch" || st == "grow" || st == "spin" || st == "bounce";
        if (rolls)
        {
            spinAngle -= v.x / Mathf.Max(.3f, visRadius) * Mathf.Rad2Deg * dt;
            if (st == "spin") spinAngle -= 1440f * dt;
            spinT.localRotation *= Quaternion.Euler(0, 0, spinAngle);
        }

        Color c = Color.white;
        if (invisT > 0) c.a = .35f + Mathf.Sin(Time.time * 20f) * .1f;
        if (hurtInvT > 0 && Mathf.Repeat(Time.time * 12f, 1f) < .5f) c.a *= .4f;
        body.color = c;

        Color gc;
        switch (st)
        {
            case "spin": gc = new Color(1f, .85f, .1f, .45f); break;
            case "freeze": gc = new Color(.6f, .9f, 1f, .5f); break;
            case "invisibility": gc = new Color(.8f, .8f, 1f, .08f); break;
            case "heal": gc = new Color(0f, 1f, .3f, .45f); break;
            case "stun": gc = new Color(1f, 0f, .3f, .4f); break;
            case "grow": gc = new Color(1f, .75f, .1f, .3f); break;
            default: gc = new Color(.2f, .6f, 1f, .22f); break;
        }
        glow.color = gc;
        trail.widthMultiplier = visRadius * 1.4f;
        trail.emitting = !dead && invisT <= 0 && v.magnitude > 3f;
    }
}
