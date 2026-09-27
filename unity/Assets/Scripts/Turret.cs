using UnityEngine;

public class Turret : MonoBehaviour
{
    public Vector2 dir = Vector2.left;
    public float period = 2f, speed = 7f, delay;
    public SpriteRenderer eye;
    public bool blue;
    public float sweep, sweepSpeed = 1.2f;
    Vector2 baseDir;
    float t;

    void Start() { t = -delay; baseDir = dir; }

    void Update()
    {
        t += Time.deltaTime;
        if (sweep > 0)
        {
            dir = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * sweepSpeed) * sweep) * baseDir;
            if (eye != null) eye.transform.localPosition = dir * .18f;
        }
        if (eye != null) eye.transform.localScale = Vector3.one * (.35f + Mathf.Clamp01(t / period) * .15f);
        if (t < period) return;
        t = 0;
        var b = Bullet.Spawn((Vector2)transform.position + dir * .75f, dir * speed, false, blue);
    }
}
