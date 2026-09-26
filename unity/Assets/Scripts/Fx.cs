using System.Collections.Generic;
using UnityEngine;

public class Fx : MonoBehaviour
{
    class P
    {
        public SpriteRenderer sr;
        public Vector2 v;
        public float life, max, size, grav, spin;
        public Color c;
    }

    static Fx inst;
    readonly List<P> live = new List<P>();
    readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
    float shake;
    float hitstop;

    public static float Shake => inst != null ? inst.shake : 0f;

    void Awake() { inst = this; }

    public static void Burst(Vector2 pos, Color c, int count, float speed, float size = .18f, float grav = 12f, float life = .6f)
    {
        if (inst == null) return;
        for (int i = 0; i < count; i++)
        {
            var sr = inst.pool.Count > 0 ? inst.pool.Pop() : inst.NewSr();
            sr.gameObject.SetActive(true);
            sr.transform.position = pos;
            var p = new P
            {
                sr = sr,
                v = Random.insideUnitCircle.normalized * speed * Random.Range(.3f, 1f),
                life = 0,
                max = life * Random.Range(.6f, 1.2f),
                size = size * Random.Range(.6f, 1.4f),
                grav = grav,
                spin = Random.Range(-720f, 720f),
                c = c
            };
            sr.color = c;
            inst.live.Add(p);
        }
    }

    public static void Ring(Vector2 pos, Color c, float radius)
    {
        if (inst == null) return;
        inst.StartCoroutine(inst.RingCo(pos, c, radius));
    }

    System.Collections.IEnumerator RingCo(Vector2 pos, Color c, float radius)
    {
        var sr = Gfx.Quad(transform, pos, Vector2.one, c, 50, Gfx.Ring);
        for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
        {
            float k = t / .35f;
            sr.transform.localScale = Vector3.one * Mathf.Lerp(.2f, radius * 2f, 1 - (1 - k) * (1 - k));
            sr.color = new Color(c.r, c.g, c.b, 1 - k);
            yield return null;
        }
        Destroy(sr.gameObject);
    }

    public static void AddShake(float amount)
    {
        if (inst != null) inst.shake = Mathf.Max(inst.shake, amount);
    }

    public static void HitStop(float seconds)
    {
        if (inst != null) inst.hitstop = Mathf.Max(inst.hitstop, seconds);
    }

    SpriteRenderer NewSr()
    {
        var sr = Gfx.Quad(transform, Vector2.zero, Vector2.one, Color.white, 40);
        return sr;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        shake = Mathf.MoveTowards(shake, 0, dt * 2.5f);
        if (hitstop > 0)
        {
            hitstop -= dt;
            Time.timeScale = hitstop > 0 ? .05f : 1f;
        }
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.life += dt;
            if (p.life >= p.max)
            {
                p.sr.gameObject.SetActive(false);
                pool.Push(p.sr);
                live.RemoveAt(i);
                continue;
            }
            p.v.y -= p.grav * dt;
            p.v *= 1f - dt * 1.5f;
            var t = p.sr.transform;
            t.position += (Vector3)(p.v * dt);
            t.Rotate(0, 0, p.spin * dt);
            float k = 1 - p.life / p.max;
            t.localScale = Vector3.one * p.size * k;
            p.sr.color = new Color(p.c.r, p.c.g, p.c.b, k);
        }
    }
}
