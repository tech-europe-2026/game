using UnityEngine;

public class Ball : MonoBehaviour
{
    public const float RNormal = .5f, RCrouch = .32f, RGrow = .85f;
    public const int MaxHearts = 3;
    public const float DashCd = .9f, TeleCd = 1.2f, ParryCd = .8f, CamoCd = 5f, FlipCd = .35f;
    public const float CamoTime = 2.6f, ParryTime = .38f, ClimbMax = 3f;
    const float G = 3f;

    public Rigidbody2D rb;
    CircleCollider2D col;
    Transform squashT, spinT;
    SpriteRenderer body, glow, shield;
    TrailRenderer trail;

    public int hearts = MaxHearts;
    public bool grown, crouching, dead, controlLocked, climbing, onRail, evolving;
    public float dashCd, teleCd, parryCd, camoCd, flipCd;
    public float camoT, parryT, stunT, dashT, flashT, healT, hurtInvT;
    public float climbStamina = ClimbMax;
    public int facing = 1;
    public float gravDir = 1f; // 1 = gravity down, -1 = gravity up
    public bool flipReady = true;
    public float iceT;
    Vector2 iceTan = Vector2.right;

    float targetRadius = RNormal, visRadius = RNormal;
    bool grounded;
    float coyote, jumpBuffer, railT;
    Vector2 squash = Vector2.one;
    Vector2 lastVel, climbNormal;
    string flashState = "bounce";
    float spinAngle;

    PhysicsMaterial2D normalMat, gripMat;
    readonly Collider2D[] hits = new Collider2D[10];
    ContactFilter2D solidFilter;

    public float Radius => col.radius;
    public bool Parrying => parryT > 0;
    public bool Camo => camoT > 0;
    Vector2 Up => new Vector2(0, gravDir);

    public static Ball Create(Vector2 pos)
    {
        var go = new GameObject("Ball");
        go.transform.position = pos;
        return go.AddComponent<Ball>();
    }

    void Awake()
    {
        normalMat = new PhysicsMaterial2D { friction = .8f, bounciness = .12f };
        gripMat = new PhysicsMaterial2D { friction = 1f, bounciness = 0f };
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = G;
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
        glow = Gfx.Quad(squashT, Vector2.zero, Vector2.one * 2.6f, new Color(.3f, .5f, 1f, .2f), 19, Gfx.Glow);
        shield = Gfx.Quad(squashT, Vector2.zero, Vector2.one * 1.5f, new Color(1, 1, 1, 0), 21, Gfx.Ring);

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.material = Gfx.SpriteMat;
        trail.time = .22f;
        trail.minVertexDistance = .05f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        trail.sortingOrder = 18;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.55f, .7f, 1f), 1) },
            new[] { new GradientAlphaKey(.7f, 0), new GradientAlphaKey(0, 1) });
        trail.colorGradient = grad;
    }


    void Update()
    {
        if (dead || evolving) { UpdateVisual(); return; }
        float dt = Time.deltaTime;
        dashCd -= dt; teleCd -= dt; parryCd -= dt; camoCd -= dt; flipCd -= dt;
        stunT -= dt; flashT -= dt; healT -= dt; hurtInvT -= dt; parryT -= dt; railT -= dt;
        jumpBuffer -= dt;
        if (camoT > 0) camoT -= dt;

        if (!controlLocked && stunT <= 0 && iceT <= 0)
        {
            if (Controls.Pressed("jump")) jumpBuffer = .12f;
            if (Controls.Released("jump")
                && rb.velocity.y * gravDir < -3f && dashT <= 0 && !climbing)
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * .55f);

            bool wantCrouch = GM.Has("crouch") && Controls.Crouch && !grown && !climbing;
            if (wantCrouch && !crouching) { crouching = true; SetRadius(RCrouch); Sfx.Play("shrink", .4f); }
            else if (!wantCrouch && crouching && RoomFor(RNormal)) { crouching = false; SetRadius(RNormal); }

            if (GM.Has("dash") && Controls.Pressed("dash") && dashCd <= 0) Dash();
            if (GM.Has("grow") && Controls.Pressed("grow")) ToggleGrow();
            if (GM.Has("teleport") && Controls.Pressed("teleport") && teleCd <= 0) Teleport();
            if (GM.Has("parry") && Controls.Pressed("parry") && parryCd <= 0) Parry();
            if (GM.Has("camo") && Controls.Pressed("camo") && camoCd <= 0) Camouflage();
            if (GM.Has("reverse") && Controls.Pressed("reverse") && flipCd <= 0) Flip();
        }
        UpdateVisual();
    }

    Vector2 InputDir()
    {
        if (controlLocked || stunT > 0 || iceT > 0) return Vector2.zero;
        return Controls.Move;
    }

    void FixedUpdate()
    {
        if (dead || evolving) return;
        float fdt = Time.fixedDeltaTime;
        float r = col.radius;
        int n = Physics2D.OverlapCircle(rb.position - Up * (r * .55f), r * .6f, solidFilter, hits);
        grounded = false;
        for (int i = 0; i < n; i++)
            if (hits[i] != col && hits[i].attachedRigidbody != rb) { grounded = true; break; }
        coyote = grounded ? .1f : coyote - fdt;
        if (grounded && !climbing) climbStamina = Mathf.MoveTowards(climbStamina, ClimbMax, fdt * 2f);
        onRail = railT > 0;
        iceT -= fdt;
        if (grounded || climbing) flipReady = true;

        Vector2 inp = InputDir();
        if (inp.x != 0) facing = inp.x > 0 ? 1 : -1;

        UpdateClimb(inp, fdt);

        if (dashT > 0)
        {
            dashT -= fdt;
            if (dashT <= 0) rb.gravityScale = G * gravDir;
        }
        else if (iceT > 0)
        {
            float along = Vector2.Dot(rb.velocity, iceTan);
            rb.velocity += iceTan * (Mathf.Max(along, 9f) - along);
            rb.angularVelocity = 0;
            if (Random.value < .3f) Fx.Burst(rb.position - Up * r, new Color(.8f, .95f, 1f), 1, 2f, .1f, 0f, .3f);
        }
        else if (!climbing)
        {
            float maxSpeed = grown ? 7.5f : crouching ? 7f : 10f;
            if (onRail) maxSpeed = 16f;
            float accel = grounded ? 40f : 24f;
            float vx = rb.velocity.x;
            if (inp.x != 0 && (Mathf.Abs(vx) < maxSpeed || Mathf.Sign(vx) != Mathf.Sign(inp.x)))
                rb.AddForce(new Vector2(inp.x * accel * rb.mass, 0));
            else if (inp.x == 0 && grounded && !onRail)
                rb.velocity = new Vector2(Mathf.MoveTowards(vx, 0, 14f * fdt), rb.velocity.y);
            rb.AddTorque(-inp.x * gravDir * 5f * rb.mass * r);
        }

        if (jumpBuffer > 0 && (coyote > 0 || climbing)) Jump();
        lastVel = rb.velocity;
    }

    void UpdateClimb(Vector2 inp, float fdt)
    {
        bool want = GM.Has("climb") && Controls.Held("climb") && !controlLocked && stunT <= 0 && climbStamina > 0 && dashT <= 0;
        Vector2 normal = Vector2.zero;
        if (want)
        {
            float best = float.MaxValue;
            int n = Physics2D.OverlapCircle(rb.position, col.radius + .2f, solidFilter, hits);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h == col || h.attachedRigidbody == rb) continue;
                var hrb = h.attachedRigidbody;
                if (hrb != null && hrb.bodyType == RigidbodyType2D.Dynamic) continue;
                Vector2 cp = h.ClosestPoint(rb.position);
                float d = Vector2.Distance(cp, rb.position);
                if (d < best && d > 1e-4f) { best = d; normal = (rb.position - cp) / d; }
            }
        }
        if (normal == Vector2.zero)
        {
            if (climbing)
            {
                climbing = false;
                rb.gravityScale = G * gravDir;
                col.sharedMaterial = normalMat;
            }
            return;
        }
        if (!climbing)
        {
            climbing = true;
            rb.gravityScale = 0f;
            col.sharedMaterial = gripMat;
            Sfx.Play("climb", .6f);
            Fx.Ring(rb.position, Gfx.Gold, 1f);
        }
        climbNormal = normal;
        climbStamina -= fdt;
        Vector2 tangent = new Vector2(-normal.y, normal.x);
        float along = Vector2.Dot(inp, tangent);
        if (Mathf.Abs(along) < .1f && inp != Vector2.zero)
            along = Mathf.Sign(Vector2.Dot(inp, tangent) + 1e-3f) * inp.magnitude * .7f;
        Vector2 target = tangent * along * 6.5f - normal * 1.5f;
        rb.velocity = Vector2.Lerp(rb.velocity, target, 1f - Mathf.Exp(-fdt * 12f));
        rb.angularVelocity = -Vector2.Dot(rb.velocity, tangent) / col.radius * Mathf.Rad2Deg * Mathf.Sign(normal.y + normal.x * .01f);
        if (climbStamina <= 0) Fx.Burst(rb.position, Color.white, 6, 3f);
    }

    void Jump()
    {
        float v = grown ? 10.5f : crouching ? 11f : 12.8f;
        if (climbing)
        {
            Vector2 dir = (climbNormal + Up * .6f).normalized;
            rb.velocity = dir * 12f;
            climbing = false;
            rb.gravityScale = G * gravDir;
            col.sharedMaterial = normalMat;
            climbStamina -= .4f;
        }
        else rb.velocity = new Vector2(rb.velocity.x, v * gravDir);
        jumpBuffer = 0;
        coyote = 0;
        squash = new Vector2(.75f, 1.3f);
        Sfx.Play("jump", .5f);
        Fx.Burst(rb.position - Up * col.radius, Color.white, 6, 3f, .15f, 0f, .3f);
    }

    void Dash()
    {
        Vector2 d = InputDir();
        if (d == Vector2.zero) d = new Vector2(facing, 0);
        d.Normalize();
        dashT = .2f;
        dashCd = DashCd;
        rb.gravityScale = 0f;
        rb.velocity = d * 20f;
        if (climbing) { climbing = false; col.sharedMaterial = normalMat; }
        Flash("dash", .35f);
        squash = new Vector2(1.35f, .78f);
        Sfx.Play("dash");
        Fx.Ring(rb.position, Gfx.Gold, 1.4f);
        Fx.Burst(rb.position, Gfx.Gold, 14, 6f, .14f, 0f, .45f);
        Fx.AddShake(.12f);
    }

    void ToggleGrow()
    {
        if (!grown)
        {
            if (!RoomFor(RGrow)) { Fx.Burst(rb.position, Color.gray, 5, 2f); return; }
            grown = true;
            crouching = false;
            rb.mass = 4f;
            SetRadius(RGrow);
            Sfx.Play("grow");
            Fx.Ring(rb.position, Gfx.Gold, 2f);
            Fx.AddShake(.2f);
        }
        else
        {
            grown = false;
            rb.mass = 1f;
            SetRadius(RNormal);
            Sfx.Play("shrink");
            Fx.Ring(rb.position, Color.white, 1.2f);
        }
    }

    bool RoomFor(float radius)
    {
        float dr = radius - col.radius;
        if (dr <= 0) return true;
        Vector2 probe = rb.position + Up * (dr + .02f);
        int n = Physics2D.OverlapCircle(probe, radius * .95f, solidFilter, hits);
        for (int i = 0; i < n; i++)
        {
            var hrb = hits[i].attachedRigidbody;
            if (hits[i] != col && (hrb == null || hrb.bodyType != RigidbodyType2D.Dynamic)) return false;
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
        Vector2 dir = InputDir();
        if (dir == Vector2.zero) dir = new Vector2(facing, 0);
        dir.Normalize();
        const float range = 4.5f;
        for (float d = range; d >= 1f; d -= .25f)
        {
            Vector2 target = rb.position + dir * d;
            bool blocked = false;
            int n = Physics2D.OverlapCircle(target, col.radius * .95f, solidFilter, hits);
            for (int i = 0; i < n; i++) if (hits[i] != col) { blocked = true; break; }
            if (blocked) continue;
            Fx.Burst(rb.position, Gfx.Cyan, 18, 5f, .16f, 0f, .5f);
            Fx.Ring(rb.position, Gfx.Cyan, 1.1f);
            rb.position = target;
            transform.position = target;
            trail.Clear();
            Fx.Ring(target, Color.white, 1.6f);
            Fx.Burst(target, Gfx.Cyan, 18, 6f, .16f, 0f, .5f);
            Flash("teleport", .4f);
            teleCd = TeleCd;
            Sfx.Play("teleport");
            return;
        }
        Fx.Burst(rb.position, Color.gray, 5, 2f);
    }

    void Parry()
    {
        parryT = ParryTime;
        parryCd = ParryCd;
        Sfx.Play("parry", .7f);
        Fx.Ring(rb.position, Color.white, 1.3f);
    }

    public void OnParry(Vector2 at)
    {
        parryT = Mathf.Max(parryT, .15f);
        Flash("parry", .4f);
        Sfx.Play("deflect");
        Fx.Ring(at, Gfx.Gold, 1.4f);
        Fx.Burst(at, Color.white, 14, 7f, .14f, 0f, .35f);
        Fx.HitStop(.06f);
        Fx.AddShake(.2f);
    }

    void Camouflage()
    {
        camoT = CamoTime;
        camoCd = CamoCd;
        Sfx.Play("invis");
        Fx.Burst(rb.position, new Color(1, 1, 1, .7f), 16, 4f, .18f, 0f, .6f);
    }

    void Flip()
    {
        if (!flipReady) { Fx.Burst(rb.position, Color.gray, 5, 2f); return; }
        flipReady = false;
        gravDir = -gravDir;
        flipCd = FlipCd;
        if (dashT <= 0 && !climbing) rb.gravityScale = G * gravDir;
        Flash("reverse", .5f);
        Sfx.Play("flip");
        Fx.Ring(rb.position, Gfx.Lilac, 1.8f);
        Fx.Burst(rb.position, Gfx.Lilac, 14, 5f, .16f, 0f, .5f);
        squash = new Vector2(1.25f, .8f);
    }

    void Flash(string state, float t)
    {
        flashState = state;
        flashT = t;
    }

    public void Hurt(Vector2 from)
    {
        if (dead || evolving || hurtInvT > 0) return;
        hearts--;
        hurtInvT = 1.1f;
        stunT = .4f;
        Vector2 away = (rb.position - from).normalized;
        if (away == Vector2.zero) away = new Vector2(-facing, 0);
        rb.velocity = away * 8f + Up * 6f;
        Fx.AddShake(.45f);
        Fx.HitStop(.08f);
        Fx.Burst(rb.position, Gfx.Coral, 16, 8f, .16f);
        Sfx.Play("hurt");
        if (hearts <= 0) Die();
    }

    public void Die()
    {
        if (dead || evolving) return;
        dead = true;
        body.enabled = glow.enabled = shield.enabled = false;
        rb.simulated = false;
        trail.emitting = false;
        Fx.Burst(rb.position, Gfx.Ink, 26, 11f, .2f);
        Fx.Burst(rb.position, Gfx.Gold, 18, 9f, .16f);
        Fx.AddShake(.7f);
        Fx.HitStop(.1f);
        Sfx.Play("hurt");
        GM.I.OnBallDied();
    }

    public void Respawn(Vector2 p)
    {
        dead = false;
        hearts = MaxHearts;
        grown = crouching = climbing = false;
        gravDir = 1f;
        flipReady = true;
        iceT = 0;
        rb.mass = 1f;
        rb.gravityScale = G;
        col.sharedMaterial = normalMat;
        SetRadius(RNormal);
        visRadius = RNormal;
        camoT = dashT = stunT = parryT = 0;
        climbStamina = ClimbMax;
        hurtInvT = 1f;
        rb.simulated = true;
        rb.position = p;
        transform.position = p;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        body.enabled = glow.enabled = shield.enabled = true;
        trail.Clear();
        trail.emitting = true;
        Flash("heal", .6f);
        Fx.Ring(p, Gfx.Mint, 2f);
    }

    public void Heal()
    {
        hearts = MaxHearts;
        healT = .9f;
        Sfx.Play("heal");
        Fx.Burst(rb.position, Gfx.Mint, 18, 5f, .16f, -4f, .8f);
        Fx.Ring(rb.position, Gfx.Mint, 1.8f);
    }

    public void Evolve()
    {
        evolving = true;
        rb.simulated = false;
        trail.emitting = false;
    }

    void Bounce(Vector2 normal, float speed)
    {
        rb.velocity = normal * speed;
        squash = new Vector2(1.3f, .75f);
        Fx.AddShake(.15f);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (dead || evolving) return;
        float impact = c.relativeVelocity.magnitude;
        Vector2 normal = c.GetContact(0).normal;
        var tile = c.collider.GetComponent<Tile>();
        if (tile != null)
        {
            switch (tile.kind)
            {
                case TileKind.Spinner:
                    if (Parrying) { OnParry(c.GetContact(0).point); Bounce(normal, 15f); }
                    else Hurt(c.GetContact(0).point);
                    return;
                case TileKind.Glass:
                    if (dashT > 0 || (grown && impact > 6f))
                    {
                        tile.Break(lastVel);
                        rb.velocity = lastVel * .85f;
                        return;
                    }
                    break;
                case TileKind.Pad:
                    if (Vector2.Dot(normal, tile.transform.up) > .5f)
                    {
                        float power = tile.GetComponent<PadPower>().power * (grown ? .85f : 1f);
                        Vector2 up = tile.transform.up;
                        rb.velocity = up * power + Vector2.Dot(rb.velocity, new Vector2(up.y, -up.x)) * new Vector2(up.y, -up.x) * .6f;
                        squash = new Vector2(.65f, 1.45f);
                        Flash("bounce", .35f);
                        Sfx.Play("bounce");
                        Fx.Burst(tile.transform.position, Gfx.Mint, 14, 7f, .16f);
                        Fx.Ring(tile.transform.position, Gfx.Mint, 1.4f);
                        return;
                    }
                    break;
                case TileKind.Ice:
                    IceContact(tile, normal);
                    break;
                case TileKind.Tube:
                    if (railT <= 0) Sfx.Play("rail", .3f);
                    railT = .15f;
                    break;
                case TileKind.Rail:
                    if (railT <= 0)
                    {
                        Sfx.Play("rail", .5f);
                        Fx.Burst(c.GetContact(0).point, Gfx.Gold, 10, 4f, .12f, 6f, .3f);
                    }
                    railT = .15f;
                    break;
            }
        }
        if (impact > 7f)
        {
            float k = Mathf.InverseLerp(7f, 22f, impact);
            Fx.AddShake(Mathf.Lerp(.06f, .35f, k) * (grown ? 2f : 1f));
            Vector2 cp = c.GetContact(0).point;
            Fx.Burst(cp, Color.white, 6 + (int)(k * 10), 3f + k * 5f, .14f, 6f, .35f);
            if (impact > 11f) Flash("bounce", .2f);
            squash = new Vector2(1f + k * .45f, 1f - k * .35f);
            Sfx.Play("land", .4f + k * .5f);
            if (grown && impact > 9f) Fx.HitStop(.04f);
        }
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (dead) return;
        var tile = c.collider.GetComponent<Tile>();
        if (tile == null) return;
        if (tile.kind == TileKind.Rail || tile.kind == TileKind.Tube)
        {
            bool tube = tile.kind == TileKind.Tube;
            railT = .15f;
            var cp = c.GetContact(0);
            Vector2 tangent = new Vector2(-cp.normal.y, cp.normal.x);
            float s = Vector2.Dot(rb.velocity, tangent);
            if (Mathf.Abs(s) > 1f) rb.AddForce(tangent * Mathf.Sign(s) * (tube ? 13f : 9f) * rb.mass);
            if (Random.value < .35f) Fx.Burst(cp.point, tube ? Color.white : Gfx.Gold, 1, 3f, .1f, 8f, .25f);
        }
        else if (tile.kind == TileKind.Ice) IceContact(tile, c.GetContact(0).normal);
        else if (tile.kind == TileKind.Spinner && !Parrying) Hurt(c.GetContact(0).point);
    }

    void IceContact(Tile tile, Vector2 normal)
    {
        if (Vector2.Dot(normal, Up) < .5f) return;
        Vector2 t = new Vector2(normal.y, -normal.x);
        if (t.x * tile.dir < 0) t = -t;
        iceTan = t;
        if (iceT <= 0)
        {
            Sfx.Play("land", .5f);
            Fx.Burst(rb.position, new Color(.85f, .96f, 1f), 12, 4f, .14f, 0f, .4f);
            if (crouching && RoomFor(RNormal)) { crouching = false; SetRadius(RNormal); }
        }
        iceT = .15f;
    }

    void OnTriggerEnter2D(Collider2D other) => HandleTrigger(other);
    void OnTriggerStay2D(Collider2D other)
    {
        var t = other.GetComponent<Tile>();
        if (t != null && (t.kind == TileKind.Shard || t.kind == TileKind.Gate)) HandleTrigger(other);
    }

    void HandleTrigger(Collider2D other)
    {
        if (dead || evolving) return;
        var t = other.GetComponent<Tile>();
        if (t == null || t.used) return;
        switch (t.kind)
        {
            case TileKind.Shard:
                if (Parrying) { OnParry(rb.position); Bounce(((Vector2)(rb.position - (Vector2)other.transform.position)).normalized, 13f); }
                else Hurt(other.transform.position);
                break;
            case TileKind.Gate:
                if (!Camo)
                {
                    Hurt(new Vector2(other.transform.position.x, rb.position.y));
                }
                break;
            case TileKind.Orb:
                t.used = true;
                GM.I.orbs++;
                Sfx.Play("coin", .6f);
                Fx.Burst(t.transform.position, Gfx.Gold, 10, 5f, .13f, 0f, .4f);
                Fx.Ring(t.transform.position, Gfx.Gold, .8f);
                Destroy(t.gameObject);
                break;
            case TileKind.Check:
                t.used = true;
                Heal();
                GM.I.checkpoint = t.transform.position;
                Destroy(t.gameObject);
                break;
            case TileKind.Goal:
                t.used = true;
                GM.I.OnGoal(t.transform.position);
                break;
        }
    }

    string State()
    {
        if (evolving) return "evolve";
        if (stunT > 0 || (hurtInvT > .6f && !dead)) return "stun";
        if (iceT > 0) return "freeze";
        if (Parrying) return "parry";
        if (Camo) return "camouflage";
        if (flashT > 0) return flashState;
        if (climbing) return "climb";
        if (healT > 0) return "heal";
        if (onRail && rb.velocity.magnitude > 6f) return "spin";
        if (grown) return "grow";
        if (crouching) return "crouch";
        if (gravDir < 0) return "reverse";
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
        float stretch = Mathf.Clamp01((v.magnitude - 9f) / 20f) * .22f;
        Vector2 s = squash;
        float ang = 0;
        if (stretch > 0)
        {
            ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            s = new Vector2(s.x * (1 + stretch), s.y * (1 - stretch * .6f));
        }
        squashT.localRotation = Quaternion.Euler(0, 0, ang);
        float d = visRadius * 2f;
        squashT.localScale = new Vector3(s.x * d, s.y * d, 1);
        transform.rotation = Quaternion.identity;

        bool rolls = st != "evolve" && st != "stun" && st != "parry" && st != "crouch" && st != "freeze";
        if (rolls && !dead)
        {
            spinAngle -= v.x * gravDir / Mathf.Max(.3f, visRadius) * Mathf.Rad2Deg * dt;
            if (st == "spin") spinAngle -= 900f * dt * Mathf.Sign(v.x);
        }
        spinT.localRotation = Quaternion.Euler(0, 0, (rolls ? spinAngle : 0) - ang);

        Color c = Color.white;
        if (Camo) c.a = .35f + Mathf.Sin(Time.time * 18f) * .08f;
        if (hurtInvT > 0 && !dead && Mathf.Repeat(Time.time * 12f, 1f) < .5f) c.a *= .45f;
        body.color = c;

        Color gc;
        switch (st)
        {
            case "dash": case "spin": gc = new Color(1f, .8f, .3f, .45f); break;
            case "freeze": gc = new Color(.6f, .9f, 1f, .5f); break;
            case "heal": gc = new Color(.2f, 1f, .6f, .5f); break;
            case "stun": gc = new Color(1f, .3f, .35f, .45f); break;
            case "reverse": gc = new Color(.6f, .5f, 1f, .4f); break;
            case "teleport": gc = new Color(.4f, .85f, 1f, .5f); break;
            case "evolve": gc = new Color(1f, .85f, .4f, .7f); break;
            case "camouflage": gc = new Color(1f, 1f, 1f, .05f); break;
            default: gc = new Color(.35f, .5f, 1f, .18f); break;
        }
        glow.color = gc;
        float sa = Parrying ? Mathf.Clamp01(parryT / ParryTime) : 0f;
        shield.color = new Color(1f, 1f, 1f, sa * .9f);
        shield.transform.localScale = Vector3.one * (1.25f + (1 - sa) * .4f);
        trail.widthMultiplier = visRadius * 1.3f;
        trail.emitting = !dead && !evolving && !Camo && v.magnitude > 4f;
    }
}
