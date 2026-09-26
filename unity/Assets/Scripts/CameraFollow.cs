using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    Camera cam;
    Transform target;
    Rigidbody2D targetRb;
    Vector2 min, max;
    Vector3 pos;
    Vector2 look;
    Transform[] stars;
    float[] depth;
    Vector2[] starBase;
    const float BaseSize = 7.5f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = BaseSize;
        cam.backgroundColor = Gfx.Bg;
        cam.clearFlags = CameraClearFlags.SolidColor;
        var bg = new GameObject("Background").transform;
        int n = 140;
        stars = new Transform[n];
        depth = new float[n];
        starBase = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float d = Random.Range(.05f, .6f);
            depth[i] = d;
            starBase[i] = new Vector2(Random.Range(-30f, 30f), Random.Range(-20f, 20f));
            bool blob = i < 10;
            Color c = blob
                ? (i % 2 == 0 ? new Color(.49f, .15f, .33f, .18f) : new Color(.11f, .17f, .45f, .25f))
                : new Color(1f, 1f, 1f, Mathf.Lerp(.15f, .7f, d));
            float size = blob ? Random.Range(8f, 16f) : Mathf.Lerp(.05f, .16f, d);
            var sr = Gfx.Quad(bg, starBase[i], Vector2.one * size, c, blob ? -20 : -10, blob ? Gfx.Glow : null);
            if (blob) depth[i] = Random.Range(.05f, .15f);
            stars[i] = sr.transform;
        }
    }

    public void Follow(Rigidbody2D rb, Vector2 levelMin, Vector2 levelMax)
    {
        targetRb = rb;
        target = rb.transform;
        min = levelMin;
        max = levelMax;
        pos = Clamp(rb.position);
        pos.z = -10;
        transform.position = pos;
    }

    Vector3 Clamp(Vector2 p)
    {
        float halfW = cam.orthographicSize * cam.aspect;
        float x = max.x - min.x < halfW * 2 ? (min.x + max.x) / 2 : Mathf.Clamp(p.x, min.x - .5f + halfW, max.x + .5f - halfW);
        float y = Mathf.Max(p.y, min.y - 1.5f + cam.orthographicSize);
        return new Vector3(x, y, -10);
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        if (target != null)
        {
            Vector2 v = targetRb.velocity;
            look = Vector2.Lerp(look, new Vector2(Mathf.Clamp(v.x * .35f, -3.5f, 3.5f), Mathf.Clamp(v.y * .12f, -2f, 1.5f)), 1f - Mathf.Exp(-dt * 3f));
            Vector3 goal = Clamp((Vector2)target.position + look + Vector2.up * 1.5f);
            pos = Vector3.Lerp(pos, goal, 1f - Mathf.Exp(-dt * 6f));
            var ball = target.GetComponent<Ball>();
            float size = BaseSize + (ball != null && ball.grown ? 1.2f : 0f) + Mathf.Clamp01((v.magnitude - 12f) / 10f) * 1.2f;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, 1f - Mathf.Exp(-dt * 3f));
        }
        float sh = Fx.Shake;
        Vector3 offset = sh > 0 ? (Vector3)(Random.insideUnitCircle * sh * .6f) : Vector3.zero;
        transform.position = pos + offset;
        transform.rotation = Quaternion.Euler(0, 0, sh > 0 ? Random.Range(-1f, 1f) * sh * 2f : 0);

        Vector2 c = pos;
        for (int i = 0; i < stars.Length; i++)
        {
            Vector2 p = starBase[i] + c * (1f - depth[i]);
            p.x = c.x + Mathf.Repeat(p.x - c.x + 30f, 60f) - 30f;
            p.y = c.y + Mathf.Repeat(p.y - c.y + 20f, 40f) - 20f;
            stars[i].position = p;
        }
    }
}
