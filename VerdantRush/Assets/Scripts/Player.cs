using UnityEngine;

public struct PadInput
{
    public bool left, right, up, down, jumpHeld, jumpPressed;
}

/// <summary>Momentum platformer physics on arbitrary polyline terrain (units: pixels, frames at 60 Hz).</summary>
public class Player
{
    public const float R = 14f;
    const float Acc = 0.07f, Dec = 0.5f, Frc = 0.06f, Top = 6.5f, BoostTop = 9f, MaxSpeed = 16f;
    const float AirAcc = 0.14f, Jump = 6.8f, Grav = 0.21875f, HurtGrav = 0.1875f;
    const float Slope = 0.125f, RollUp = 0.078125f, RollDown = 0.3125f, RollFrc = 0.025f, RollDec = 0.125f;

    public Vector2 pos, vel, up = Vector2.up;
    public float gs;
    public bool grounded, rolling, ball, jumping, springing, hurt, dead, crouch, lookUp, spindash, skidding, finished;
    public int facing = 1, controlLock, invuln, trickTimer;
    public bool trickReady, boost;
    public float spinRev, boostCharge;
    public int loopIdx = -1;
    float theta;
    readonly World world;
    readonly LoopData[] loops;
    public System.Action<string> Sfx;

    public Player(World w, LoopData[] loops, Vector2 start)
    {
        world = w;
        this.loops = loops;
        Spawn(start);
    }

    public void Spawn(Vector2 feet)
    {
        pos = feet + Vector2.up * (R + 1);
        vel = Vector2.zero;
        gs = 0;
        up = Vector2.up;
        grounded = false;
        rolling = ball = jumping = springing = hurt = dead = crouch = spindash = finished = false;
        loopIdx = -1;
        facing = 1;
        controlLock = 0;
        invuln = 0;
        boost = false;
        boostCharge = 0;
    }

    public Vector2 Tangent => new Vector2(up.y, -up.x);
    public float Speed => grounded || loopIdx >= 0 ? Mathf.Abs(gs) : vel.magnitude;
    public bool Attacking => ball || rolling || spindash || boost;
    public Vector2 Velocity => grounded || loopIdx >= 0 ? Tangent * gs : vel;

    public void Step(PadInput pin)
    {
        if (finished) { pin = default; pin.right = false; }
        if (controlLock > 0) controlLock--;
        if (invuln > 0) invuln--;
        if (trickTimer > 0) trickTimer--;
        if (dead)
        {
            vel.y -= Grav;
            pos += vel;
            return;
        }
        skidding = false;
        if (loopIdx >= 0) LoopStep(pin);
        else if (grounded) GroundStep(pin);
        else AirStep(pin);

        float top = boost ? BoostTop : Top;
        if ((grounded || loopIdx >= 0) && Mathf.Abs(gs) >= Top - 0.3f && !finished)
        {
            boostCharge += 1f / 60f;
            if (boostCharge > 2.2f && !boost) { boost = true; Sfx?.Invoke("boost"); }
        }
        else if (grounded || loopIdx >= 0)
        {
            boostCharge = 0;
            if (Mathf.Abs(gs) < 4f) boost = false;
        }
        _ = top;
    }

    void ApplyGroundInput(PadInput pin, Vector2 t)
    {
        float top = boost ? BoostTop : Top;
        if (Mathf.Abs(t.y) > 0.05f)
        {
            float s = Slope;
            if (rolling) s = Mathf.Sign(gs) == Mathf.Sign(t.y) ? RollUp : RollDown;
            if (!(Mathf.Abs(gs) < 0.05f && Mathf.Abs(t.y) < 0.35f && !rolling)) gs -= s * t.y;
        }
        if (rolling)
        {
            gs -= Mathf.Min(Mathf.Abs(gs), RollFrc) * Mathf.Sign(gs);
            if (pin.left && gs > 0) gs -= RollDec;
            if (pin.right && gs < 0) gs += RollDec;
            if (Mathf.Abs(gs) < 0.5f && loopIdx < 0) rolling = false;
        }
        else if (controlLock == 0)
        {
            if (pin.left)
            {
                if (gs > 0) { gs -= Dec; if (gs > 3f) skidding = true; if (gs <= 0) gs = -0.5f; }
                else if (gs > -top) gs = Mathf.Max(gs - Acc, -top);
                if (gs <= 0) facing = -1;
            }
            else if (pin.right)
            {
                if (gs < 0) { gs += Dec; if (gs < -3f) skidding = true; if (gs >= 0) gs = 0.5f; }
                else if (gs < top) gs = Mathf.Min(gs + Acc, top);
                if (gs >= 0) facing = 1;
            }
            else gs -= Mathf.Min(Mathf.Abs(gs), Frc) * Mathf.Sign(gs);
        }
        else gs -= Mathf.Min(Mathf.Abs(gs), Frc) * Mathf.Sign(gs);
        gs = Mathf.Clamp(gs, -MaxSpeed, MaxSpeed);
    }

    void GroundStep(PadInput pin)
    {
        var t = Tangent;
        lookUp = false;
        if (!rolling && Mathf.Abs(gs) < 0.5f && pin.down && !finished) crouch = true;
        if (crouch)
        {
            gs = 0;
            if (pin.jumpPressed)
            {
                spindash = true;
                spinRev = Mathf.Min(spinRev + 2f, 8f);
                Sfx?.Invoke("rev");
            }
            spinRev -= spinRev / 32f;
            if (!pin.down)
            {
                crouch = false;
                if (spindash)
                {
                    gs = facing * (8f + Mathf.Floor(spinRev) / 2f);
                    rolling = true;
                    spindash = false;
                    spinRev = 0;
                    Sfx?.Invoke("release");
                }
            }
            if (crouch) { MoveGround(); return; }
        }
        if (pin.jumpPressed && !finished)
        {
            DoJump(t);
            return;
        }
        if (Mathf.Abs(gs) < 0.05f && pin.up && !finished) lookUp = true;
        ApplyGroundInput(pin, t);
        if (!rolling && pin.down && Mathf.Abs(gs) > 1f && !finished) { rolling = true; Sfx?.Invoke("dash"); }
        MoveGround();
        if (grounded && up.y < 0.3f && Mathf.Abs(gs) < 2.5f)
        {
            vel = Tangent * gs;
            grounded = false;
            controlLock = 30;
        }
    }

    void DoJump(Vector2 t)
    {
        vel = t * gs + up * Jump;
        grounded = false;
        loopIdx = -1;
        ball = true;
        jumping = true;
        rolling = false;
        crouch = false;
        trickReady = false;
        Sfx?.Invoke("jump");
    }

    void MoveGround()
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(gs) / 4f));
        for (int i = 0; i < n; i++)
        {
            var t = Tangent;
            var prev = pos;
            pos += t * (gs / n);
            if (world.Resolve(ref pos, prev, R - 4f, false, up, 0.5f, out var wn))
            {
                if (Vector2.Dot(t * Mathf.Sign(gs), wn) < -0.3f) gs = 0;
            }
            if (world.FindGround(pos, up, R, 5f + Mathf.Abs(gs), out var np, out var nn))
            {
                pos = np;
                up = nn;
            }
            else
            {
                grounded = false;
                vel = t * gs;
                if (rolling) { ball = true; rolling = false; }
                trickReady = vel.magnitude > 6f;
                return;
            }
            if (gs > 3f && loops != null)
            {
                for (int k = 0; k < loops.Length; k++)
                {
                    var L = loops[k];
                    float bottom = L.cy - L.r;
                    if (prev.x < L.cx && pos.x >= L.cx && Mathf.Abs(pos.y - R - bottom) < 10f)
                    {
                        loopIdx = k;
                        theta = -Mathf.PI / 2 + (pos.x - L.cx) / (L.r - R);
                        return;
                    }
                }
            }
        }
    }

    void LoopStep(PadInput pin)
    {
        var L = loops[loopIdx];
        float rc = L.r - R;
        var t = Tangent;
        if (pin.jumpPressed) { DoJump(t); return; }
        ApplyGroundInput(pin, t);
        theta += gs / rc;
        var c = new Vector2(L.cx, L.cy);
        if (theta >= 1.5f * Mathf.PI)
        {
            loopIdx = -1;
            pos = new Vector2(L.cx + (theta - 1.5f * Mathf.PI) * rc, L.cy - L.r + R);
            up = Vector2.up;
            return;
        }
        if (theta < -0.5f * Mathf.PI)
        {
            loopIdx = -1;
            pos = new Vector2(L.cx + (theta + 0.5f * Mathf.PI) * rc, L.cy - L.r + R);
            up = Vector2.up;
            return;
        }
        var dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
        pos = c + dir * rc;
        up = -dir;
        if (Mathf.Abs(gs) < 2.5f && up.y < 0.3f)
        {
            vel = Tangent * gs;
            loopIdx = -1;
            grounded = false;
            controlLock = 30;
        }
    }

    void AirStep(PadInput pin)
    {
        float top = boost ? BoostTop : Top;
        if (!hurt && controlLock == 0 && !finished)
        {
            if (pin.left && vel.x > -top) vel.x = Mathf.Max(vel.x - AirAcc, -top);
            if (pin.right && vel.x < top) vel.x = Mathf.Min(vel.x + AirAcc, top);
        }
        if (trickReady && pin.jumpPressed && !hurt && !finished)
        {
            trickReady = false;
            trickTimer = 36;
            float dir = pin.left ? -1 : 1;
            if (pin.up) vel = new Vector2(vel.x * 0.6f, Mathf.Max(vel.y, 0) + 5f);
            else vel = new Vector2(vel.x + dir * 3f, Mathf.Max(vel.y, 1.5f));
            springing = false;
            ball = false;
            Sfx?.Invoke("trick");
        }
        if (jumping && !pin.jumpHeld && vel.y > 4f) vel.y = 4f;
        if (vel.y > 0 && vel.y < 4f && Mathf.Abs(vel.x) >= 0.125f) vel.x -= vel.x / 32f / 4f;
        vel.y -= hurt ? HurtGrav : Grav;
        vel.y = Mathf.Max(vel.y, -14f);
        up = Vector2.Lerp(up, Vector2.up, 0.12f).normalized;
        MoveAir();
    }

    void MoveAir()
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(vel.magnitude / 4f));
        for (int i = 0; i < n; i++)
        {
            var prev = pos;
            pos += vel / n;
            if (!world.Resolve(ref pos, prev, R, vel.y <= 0, Vector2.up, 2f, out var hn)) continue;
            if (hn.y > 0.55f && Vector2.Dot(vel, hn) < 0)
            {
                Land(hn);
                return;
            }
            float into = Vector2.Dot(vel, hn);
            if (into < 0) vel -= hn * into;
        }
    }

    void Land(Vector2 n)
    {
        up = n;
        gs = Vector2.Dot(vel, Tangent);
        grounded = true;
        ball = jumping = springing = false;
        trickReady = false;
        trickTimer = 0;
        if (hurt) { hurt = false; gs = 0; }
        if (world.FindGround(pos, up, R, 8f, out var np, out var nn)) { pos = np; up = nn; }
    }

    public void Bounce(Vector2 dir, float power, bool lockControl)
    {
        loopIdx = -1;
        grounded = false;
        vel = dir * power;
        if (Mathf.Abs(dir.x) > 0.3f) facing = dir.x > 0 ? 1 : -1;
        ball = jumping = rolling = crouch = spindash = false;
        springing = true;
        trickReady = true;
        hurt = false;
        if (lockControl) controlLock = 16;
    }

    public void DashPad(float dir)
    {
        if (loopIdx < 0 && !grounded) return;
        gs = dir * Mathf.Max(Mathf.Abs(gs), 11f);
        facing = dir > 0 ? 1 : -1;
        controlLock = 12;
    }

    public void Hurt(float fromX)
    {
        float dir = pos.x < fromX ? -1 : 1;
        loopIdx = -1;
        grounded = false;
        vel = new Vector2(2f * dir, 4f);
        hurt = true;
        ball = jumping = rolling = springing = crouch = spindash = boost = false;
        boostCharge = 0;
        invuln = 120;
    }

    public void Die()
    {
        dead = true;
        loopIdx = -1;
        grounded = false;
        vel = new Vector2(0, 7f);
        up = Vector2.up;
        ball = rolling = springing = hurt = false;
    }

    public void EnemyBounce()
    {
        if (!grounded && loopIdx < 0 && vel.y < 0) vel.y = Mathf.Max(-vel.y, 4f);
    }

    public Vector2 Feet => pos - up * R;
}
