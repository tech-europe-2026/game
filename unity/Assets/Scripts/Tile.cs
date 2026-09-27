using UnityEngine;

public enum TileKind { Solid, Rail, Shard, Spinner, Gate, Orb, Check, Goal, Pad, Glass, Crystal, Door, Ice, Tube, Tramp, GravZone, Loop }

public class Tile : MonoBehaviour
{
    public TileKind kind;
    public bool used;
    public SpriteRenderer art;
    public Tile linked;
    public int dir = 1;
    public float loopR;
    public SpriteRenderer glow;
    public bool push;
    public float gateOn = 1.4f, gateOff = 1.3f, gateShift;
    float GateClock => Mathf.Repeat(Time.time + gateShift, gateOn + gateOff);
    public bool GateLive => GateClock < gateOn;
    Vector3 basePos;
    float phase;

    void Start()
    {
        basePos = transform.localPosition;
        phase = Random.value * 10f;
    }

    void Update()
    {
        float t = Time.time + phase;
        switch (kind)
        {
            case TileKind.Orb:
                transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * 2.4f) * .12f;
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 5f) * .06f);
                break;
            case TileKind.Check:
                transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * 1.6f) * .1f;
                if (art != null) art.transform.Rotate(0, 0, 60f * Time.deltaTime);
                break;
            case TileKind.Goal:
                if (art != null) art.transform.Rotate(0, 0, -50f * Time.deltaTime);
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.5f) * .04f);
                break;
            case TileKind.Gate:
                if (art != null)
                {
                    float clk = GateClock, a;
                    if (clk < gateOn) a = .7f + Mathf.Sin(t * 24f) * .2f;
                    else if (clk > gateOn + gateOff - .4f) a = Mathf.Sin(t * 40f) > 0 ? .45f : .1f;
                    else a = .08f;
                    var c = art.color; c.a = a; art.color = c;
                    if (glow != null) { var g = glow.color; g.a = clk < gateOn ? .25f : .03f; glow.color = g; }
                }
                break;
            case TileKind.Crystal:
                if (art != null) art.transform.localRotation = Quaternion.Euler(0, 0, 45f + Mathf.Sin(t * 2f) * 8f);
                break;
        }
    }

    public void Break(Vector2 hitVel)
    {
        if (used) return;
        used = true;
        Vector2 p = transform.position;
        Color c = kind == TileKind.Crystal ? Gfx.Lilac : Gfx.Glass;
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("shard");
            go.transform.position = p + Random.insideUnitCircle * .6f;
            go.transform.localScale = Vector3.one * Random.Range(.2f, .45f);
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Gfx.Tri;
            sr.color = new Color(c.r, c.g, c.b, .9f);
            sr.sortingOrder = 6;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.velocity = hitVel * .4f + Random.insideUnitCircle * 6f;
            rb.angularVelocity = Random.Range(-700f, 700f);
            go.layer = 2;
            Destroy(go, 2f);
        }
        Fx.Burst(p, c, 20, 9f, .2f, 6f, .6f);
        Fx.Ring(p, Color.white, 1.6f);
        Fx.AddShake(.3f);
        Fx.HitStop(.05f);
        Sfx.Play("smash");
        if (linked != null) linked.Open();
        Destroy(gameObject);
    }

    public void Open()
    {
        if (used) return;
        used = true;
        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
        Sfx.Play("heal", .6f);
        Fx.Burst(transform.position, Gfx.Lilac, 24, 6f, .2f, 0f, .7f);
        StartCoroutine(Fade());
    }

    System.Collections.IEnumerator Fade()
    {
        var srs = GetComponentsInChildren<SpriteRenderer>();
        Vector3 s = transform.localScale;
        for (float t = 0; t < .5f; t += Time.deltaTime)
        {
            float k = t / .5f;
            transform.localScale = new Vector3(s.x * (1 - k), s.y, 1);
            foreach (var sr in srs) { var c = sr.color; c.a = 1 - k; sr.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }
}
