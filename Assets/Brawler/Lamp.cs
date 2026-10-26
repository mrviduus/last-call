using UnityEngine;

// A lamp hanging over a pool table: a pendulum from the ceiling. Jump into it to hang on, pump left/right
// to swing, kick from it; rip it down to use as a weapon. It keeps swinging for a while after you let go.
public class Lamp : MonoBehaviour
{
    public const float Length = 90f;   // ceiling to shade rim (pool_lamp.png); the sprite pivots at the ceiling

    public float x, depth, bottom;      // rest position: rim height above the floor at this depth
    public float angle, angVel;         // radians, positive swings the rim to the right
    public Fighter hanger;

    public float SwingX => x + Mathf.Sin(angle) * Length;

    public static Lamp Spawn(float x, float depth, float bottom)
    {
        var l = new GameObject("PoolLamp").AddComponent<Lamp>();
        l.x = x;
        l.depth = depth;
        l.bottom = bottom;
        var sr = l.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Art.PoolLamp;
        sr.sortingOrder = -Mathf.RoundToInt(depth) * 2 + 2; // just in front of its pool table
        Game.Lamps.Add(l);
        return l;
    }

    // whoever hangs on it pushes left or right to build up the swing
    public void Pump(float input) => angVel += input * Feel.LampPump;

    public void Step()
    {
        angVel += -Feel.LampGravity * Mathf.Sin(angle);
        angVel *= Feel.LampDamping;
        angle += angVel;
        if (Mathf.Abs(angle) > Feel.LampMaxAngle)
        {
            angle = Mathf.Sign(angle) * Feel.LampMaxAngle; // the cord snaps taut at the end of the swing
            angVel *= -0.3f;
        }
    }

    public void Rip()
    {
        Game.Lamps.Remove(this);
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject); // edit-mode tests
    }

    void LateUpdate()
    {
        transform.position = new Vector3(x, depth + bottom + Length, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
    }
}
