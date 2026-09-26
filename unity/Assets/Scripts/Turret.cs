using UnityEngine;

public class Turret : MonoBehaviour
{
    public Vector2 dir = Vector2.left;
    public float period = 2f, speed = 7f, delay;
    public SpriteRenderer eye;
    float t;

    void Start() => t = -delay;

    void Update()
    {
        t += Time.deltaTime;
        if (eye != null) eye.transform.localScale = Vector3.one * (.35f + Mathf.Clamp01(t / period) * .15f);
        if (t < period) return;
        t = 0;
        Bullet.Spawn((Vector2)transform.position + dir * .7f, dir * speed);
    }
}
