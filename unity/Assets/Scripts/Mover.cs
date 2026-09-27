using UnityEngine;

public class Mover : MonoBehaviour
{
    public Vector2 a, b;
    public float period = 4f, phase;
    public Vector2 vel;
    Rigidbody2D rb;

    void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.useFullKinematicContacts = true;
    }

    void FixedUpdate()
    {
        float k = (1 - Mathf.Cos((Time.fixedTime / period + phase) * Mathf.PI * 2f)) * .5f;
        var next = Vector2.Lerp(a, b, k);
        vel = (next - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(next);
    }
}
