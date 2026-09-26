using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float speed = 90f;
    Rigidbody2D rb;

    void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.useFullKinematicContacts = true;
    }

    void FixedUpdate() => rb.MoveRotation(rb.rotation + speed * Time.fixedDeltaTime);
}
