using System.Collections.Generic;
using UnityEngine;

public class FogBank : MonoBehaviour
{
    public Vector2 revealMin, revealMax;
    public readonly List<SpriteRenderer> parts = new List<SpriteRenderer>();
    float a = 1f;

    void Update()
    {
        var b = GM.I != null ? GM.I.Player : null;
        bool inside = b != null && !b.dead && b.rb.position.x > revealMin.x && b.rb.position.x < revealMax.x && b.rb.position.y > revealMin.y && b.rb.position.y < revealMax.y;
        a = Mathf.MoveTowards(a, inside ? 0f : 1f, Time.deltaTime * (inside ? 1.5f : .8f));
        foreach (var p in parts) { var c = p.color; c.a = a; p.color = c; }
    }
}
