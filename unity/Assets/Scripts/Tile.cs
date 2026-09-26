using UnityEngine;

public enum TileKind { Solid, Spike, Lava, Laser, Coin, Heal, Portal, Cracked, Pad, Crate }

public class Tile : MonoBehaviour
{
    public TileKind kind;
    public bool used;
    public SpriteRenderer art;
    Vector3 basePos;
    float phase;

    void Start()
    {
        basePos = transform.position;
        phase = Random.value * 10f;
    }

    void Update()
    {
        float t = Time.time + phase;
        switch (kind)
        {
            case TileKind.Coin:
                transform.position = basePos + Vector3.up * Mathf.Sin(t * 3f) * .12f;
                transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 2.5f)) * .5f + .1f, .6f, 1);
                break;
            case TileKind.Heal:
                transform.position = basePos + Vector3.up * Mathf.Sin(t * 2f) * .1f;
                transform.localScale = Vector3.one * (.8f + Mathf.Sin(t * 4f) * .06f);
                break;
            case TileKind.Portal:
                transform.Rotate(0, 0, 90f * Time.deltaTime);
                transform.localScale = Vector3.one * (2.2f + Mathf.Sin(t * 3f) * .15f);
                break;
            case TileKind.Laser:
                if (art != null)
                {
                    var c = art.color;
                    c.a = .65f + Mathf.Sin(t * 30f) * .25f;
                    art.color = c;
                }
                break;
            case TileKind.Lava:
                if (art != null)
                    art.color = Color.Lerp(Gfx.Lava, Gfx.Spike, (Mathf.Sin(t * 2f + basePos.x) + 1) * .5f);
                break;
        }
    }

    public void Break(Vector2 hitVel)
    {
        if (used) return;
        used = true;
        Vector2 p = transform.position;
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("debris");
            go.transform.position = p + new Vector2((i % 2) - .5f, (i / 2) - .5f) * .5f;
            go.transform.localScale = Vector3.one * .45f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Gfx.Square;
            sr.color = Gfx.Cracked;
            sr.sortingOrder = 5;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.velocity = hitVel * .5f + Random.insideUnitCircle * 5f;
            rb.angularVelocity = Random.Range(-600f, 600f);
            go.AddComponent<BoxCollider2D>();
            go.layer = 2;
            Destroy(go, 2.5f);
        }
        Fx.Burst(p, Gfx.Cracked, 18, 9f, .22f);
        Fx.AddShake(.35f);
        Fx.HitStop(.06f);
        Sfx.Play("smash");
        Destroy(gameObject);
    }
}
