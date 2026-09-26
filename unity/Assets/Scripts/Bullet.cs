using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector2 vel;
    public bool reflected;
    SpriteRenderer sr;
    float life;
    static readonly Collider2D[] hits = new Collider2D[6];

    public static void Spawn(Vector2 pos, Vector2 vel)
    {
        var go = new GameObject("bullet");
        go.transform.position = pos;
        var b = go.AddComponent<Bullet>();
        b.vel = vel;
        b.sr = Gfx.Quad(go.transform, Vector2.zero, Vector2.one * .45f, Gfx.Coral, 15, Gfx.Circle);
        Gfx.Quad(go.transform, Vector2.zero, Vector2.one * 1.4f, new Color(1, .4f, .45f, .35f), 14, Gfx.Glow);
        if (GM.I != null) go.transform.SetParent(GM.I.LevelRoot, true);
    }

    void Update()
    {
        life += Time.deltaTime;
        if (life > 8f) { Destroy(gameObject); return; }
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
            Pop();
            return;
        }
    }

    void Pop()
    {
        Fx.Burst(transform.position, reflected ? Gfx.Gold : Gfx.Coral, 8, 4f, .15f, 0f, .35f);
        Destroy(gameObject);
    }
}
