using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Camera cam;
    Rigidbody2D targetRb;
    Ball ball;
    Vector2 min, max;
    Vector3 pos;
    Vector2 look;
    Transform sky;
    SpriteRenderer skyTint, skySr;
    Transform[] deco;
    float[] depth;
    Vector2[] decoBase;
    float tint;
    const float BaseSize = 7f, WrapW = 70f, WrapH = 44f;

    public void SetSky(Color bottom, Color mid, Color top)
    {
        skySr.sprite = Gfx.VerticalGradient(bottom, mid, top);
        cam.backgroundColor = mid;
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = BaseSize;
        cam.backgroundColor = Gfx.SkyMid;
        cam.clearFlags = CameraClearFlags.SolidColor;

        var skyGo = new GameObject("Sky");
        sky = skyGo.transform;
        sky.SetParent(transform, false);
        sky.localPosition = new Vector3(0, 0, 20);
        var sr = skySr = skyGo.AddComponent<SpriteRenderer>();
        sr.sprite = Gfx.VerticalGradient(Gfx.SkyBottom, Gfx.SkyMid, Gfx.SkyTop);
        sr.sortingOrder = -100;
        skyTint = Gfx.Quad(sky, Vector2.zero, Vector2.one, new Color(.55f, .45f, 1f, 0f), -99);

        var bg = new GameObject("Clouds").transform;
        int clouds = 16, shapes = 22, n = clouds + shapes;
        deco = new Transform[n];
        depth = new float[n];
        decoBase = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            var t = new GameObject("deco").transform;
            t.SetParent(bg, false);
            decoBase[i] = new Vector2(Random.Range(-WrapW / 2, WrapW / 2), Random.Range(-WrapH / 2, WrapH / 2));
            if (i < clouds)
            {
                float d = Random.Range(.1f, .7f);
                depth[i] = d;
                float a = Mathf.Lerp(.35f, .85f, d), sc = Mathf.Lerp(1.2f, 2.6f, d);
                int puffs = Random.Range(4, 7);
                for (int k = 0; k < puffs; k++)
                {
                    float s = Random.Range(2.2f, 4f) * sc;
                    Gfx.Quad(t, new Vector2((k - puffs / 2f) * 1.1f * sc, Random.Range(-.3f, .5f) * sc), new Vector2(s * 1.3f, s), new Color(1, 1, 1, a * .7f), -60 + (int)(d * 10), Gfx.Glow);
                }
            }
            else
            {
                float d = Random.Range(.05f, .3f);
                depth[i] = d;
                var c = new Color(1, 1, 1, Mathf.Lerp(.2f, .45f, d));
                float s = Random.Range(.4f, 1.3f);
                switch (i % 3)
                {
                    case 0: Gfx.Quad(t, Vector2.zero, Vector2.one * s * 1.6f, c, -70, Gfx.Ring); break;
                    case 1: Gfx.Slab(t, Vector2.zero, new Vector2(s * 3f, s * .5f), c, -70); break;
                    default: Gfx.Quad(t, Vector2.zero, Vector2.one * s * .6f, c, -70, Gfx.Circle); break;
                }
                t.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
            }
            deco[i] = t;
        }
    }

    public void Follow(Ball b, Vector2 levelMin, Vector2 levelMax)
    {
        ball = b;
        targetRb = b.rb;
        min = levelMin;
        max = levelMax;
        pos = Clamp(targetRb.position);
        transform.position = pos;
    }

    Vector3 Clamp(Vector2 p)
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        float x = max.x - min.x < halfW * 2 ? (min.x + max.x) / 2 : Mathf.Clamp(p.x, min.x - 2f + halfW, max.x + 2f - halfW);
        float y = max.y - min.y < halfH * 2 ? (min.y + max.y) / 2 : Mathf.Clamp(p.y, min.y - 3f + halfH, max.y + 3f - halfH);
        return new Vector3(x, y, -10);
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        if (targetRb != null)
        {
            Vector2 v = targetRb.velocity;
            look = Vector2.Lerp(look, new Vector2(Mathf.Clamp(v.x * .3f, -3.5f, 3.5f), Mathf.Clamp(v.y * .12f, -2f, 2f)), 1f - Mathf.Exp(-dt * 2.5f));
            Vector3 goal = Clamp(targetRb.position + look + Vector2.up * ball.gravDir * 1.8f);
            pos = Vector3.Lerp(pos, goal, 1f - Mathf.Exp(-dt * 5f));
            float size = BaseSize + (ball.grown ? 1f : 0f) + Mathf.Clamp01((v.magnitude - 11f) / 10f) * 1.3f;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, 1f - Mathf.Exp(-dt * 2.5f));
            tint = Mathf.Lerp(tint, ball.gravDir < 0 ? .22f : 0f, 1f - Mathf.Exp(-dt * 4f));
        }
        float sh = Fx.Shake;
        Vector3 offset = sh > 0 ? (Vector3)(Random.insideUnitCircle * sh * .5f) : Vector3.zero;
        transform.position = pos + offset;
        transform.rotation = Quaternion.Euler(0, 0, sh > 0 ? Random.Range(-1f, 1f) * sh * 1.5f : 0);

        float hh = cam.orthographicSize * 2.4f;
        sky.localScale = new Vector3(hh * cam.aspect, hh / 256f, 1);
        skyTint.transform.localScale = new Vector3(1f, 256f, 1f);
        skyTint.color = new Color(.55f, .45f, 1f, tint);

        Vector2 c = pos;
        for (int i = 0; i < deco.Length; i++)
        {
            Vector2 p = decoBase[i] + c * (1f - depth[i]);
            p.x = c.x + Mathf.Repeat(p.x - c.x + WrapW / 2, WrapW) - WrapW / 2;
            p.y = c.y + Mathf.Repeat(p.y - c.y + WrapH / 2, WrapH) - WrapH / 2;
            deco[i].position = new Vector3(p.x, p.y, 5);
        }
    }
}
