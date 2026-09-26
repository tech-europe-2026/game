using UnityEngine;

public class Mover : MonoBehaviour
{
    public Vector2 a, b;
    public float period = 4f, phase;
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
        rb.MovePosition(Vector2.Lerp(a, b, k));
    }
}
