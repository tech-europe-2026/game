using UnityEngine;

public class ChainLine : MonoBehaviour
{
    public Transform pivot;
    LineRenderer line;
    Rigidbody2D rb;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        Vector2 toPivot = (Vector2)pivot.position - rb.position;
        Vector2 tangent = new Vector2(-toPivot.y, toPivot.x).normalized;
        float along = Vector2.Dot(rb.velocity, tangent);
        if (Mathf.Abs(along) < 7f)
            rb.AddForce(tangent * Mathf.Sign(along == 0 ? 1 : along) * 4f * rb.mass);
    }

    void LateUpdate()
    {
        line.SetPosition(0, pivot.position);
        line.SetPosition(1, transform.position);
    }
}
