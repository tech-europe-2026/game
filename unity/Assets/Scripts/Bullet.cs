using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector2 vel;
    public bool reflected, homing, blue;
    int bounces;
    SpriteRenderer sr;
    float life;
    static readonly Collider2D[] hits = new Collider2D[6];

    public static Bullet Spawn(Vector2 pos, Vector2 vel, bool homing = false, bool blue = false)
    {
        var go = new GameObject("bullet");
        go.transform.position = pos;
        var b = go.AddComponent<Bullet>();
        b.vel = vel;
        b.homing = homing;
        b.blue = blue;
        Color core = blue ? new Color(.25f, .55f, 1f) : homing ? new Color(.85f, .35f, 1f) : (Color)Gfx.Coral;
        b.sr = Gfx.Quad(go.transform, Vector2.zero, Vector2.one * (blue ? .55f : .45f), core, 15, Gfx.Circle);
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 1.4f, new Color(core.r, core.g, core.b, .38f), 14, Gfx.Glow);
        if (GM.I != null) go.transform.SetParent(GM.I.LevelRoot, true);
        return b;
    }

    void Update()
    {
        life += Time.deltaTime;
        if (life > (homing ? 4.5f : blue ? 6f : 8f)) { Pop(); return; }
        var pl = GM.I != null ? GM.I.Player : null;
        if (homing && !reflected && pl != null && !pl.dead)
        {
            Vector2 want = (pl.rb.position - (Vector2)transform.position).normalized * vel.magnitude;
            vel = Vector3.RotateTowards(vel, want, 1.7f * Time.deltaTime, 0f);
        }
        Vector2 from = transform.position;
        transform.position += (Vector3)(vel * Time.deltaTime);
        int n = Physics2D.OverlapCircleNonAlloc(transform.position, .22f, hits);
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            var ball = h.GetComponent<Ball>();
            if (ball != null)
            {
                if (reflected) continue;
                if (ball.Parrying)
                {
                    reflected = true;
                    vel = ((Vector2)transform.position - ball.rb.position).normalized * vel.magnitude * 1.6f;
                    if (Mathf.Abs(vel.y) < Mathf.Abs(vel.x) * .3f) vel.y = 0;
                    sr.color = Gfx.Gold;
                    ball.OnParry(transform.position);
                    return;
                }
                if (blue) { ball.Knock(vel.normalized); Pop(); return; }
                ball.Hurt(transform.position);
                Pop();
                return;
            }
            if (h.isTrigger) continue;
            var t = h.GetComponent<Tile>();
            if (t != null && t.kind == TileKind.Door) continue;
            if (t != null && t.kind == TileKind.Crystal)
            {
                if (!reflected) continue;
                t.Break(vel);
            }
            if (h.GetComponent<Turret>() != null && !reflected) continue;
            if (blue && bounces < 4)
            {
                var hit = Physics2D.CircleCast(from - vel.normalized * .3f, .22f, vel.normalized, vel.magnitude * Time.deltaTime + .6f);
                if (hit.collider != null && hit.collider.GetComponent<Ball>() == null)
                {
                    bounces++;
                    vel = Vector2.Reflect(vel, hit.normal);
                    transform.position = hit.centroid + hit.normal * .05f;
                    Fx.Burst(transform.position, new Color(.5f, .75f, 1f), 5, 3f, .1f, 0f, .25f);
                    return;
                }
            }
            Pop();
            return;
        }
    }

    void Pop()
    {
        Fx.Burst(transform.position, reflected ? Gfx.Gold : blue ? new Color(.4f, .65f, 1f) : (Color)Gfx.Coral, 8, 4f, .15f, 0f, .35f);
        Destroy(gameObject);
    }
}
