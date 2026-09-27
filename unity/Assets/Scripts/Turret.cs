using UnityEngine;

public class Turret : MonoBehaviour
{
    public Vector2 dir = Vector2.left;
    public float period = 2f, speed = 7f, delay;
    public SpriteRenderer eye;
    public Transform barrel;
    public bool blue;
    public float sweep, sweepSpeed = 1.2f, glide, glidePeriod = 9f;
    Vector2 baseDir, home;
    float t;

    void Start() { t = -delay; baseDir = dir; home = transform.position; }

    void Update()
    {
        t += Time.deltaTime;
        if (glide > 0) transform.position = home + Vector2.right * Mathf.Sin(Time.time * 2 * Mathf.PI / glidePeriod) * glide;
        if (barrel != null) barrel.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        if (sweep > 0)
        {
            dir = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * sweepSpeed) * sweep) * baseDir;
            if (eye != null && barrel == null) eye.transform.localPosition = dir * .18f;
        }
        if (eye != null) eye.transform.localScale = Vector3.one * (barrel != null ? .15f + Mathf.Pow(Mathf.Clamp01(t / period), 3) * .4f : .35f + Mathf.Clamp01(t / period) * .15f);
        if (t < period) return;
        t = 0;
        var b = Bullet.Spawn((Vector2)transform.position + dir * .75f, dir * speed, false, blue);
    }
}
