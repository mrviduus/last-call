using UnityEngine;

// Things lying around a stage that can be picked up, swung and thrown (chairs, bottles).
// Same ground plane as fighters: (x, depth) + height; stepped by Game at 60 Hz.
public class Prop : MonoBehaviour
{
    public enum Kind { Chair, Bottle }

    public Kind kind;
    public Vector2 ground, vel;
    public float height, heightVel;
    public Fighter holder, thrower;
    public int hitsLeft;

    SpriteRenderer sr, shadow;

    public bool Free => holder == null && thrower == null;
    public float Reach => kind == Kind.Chair ? Feel.ChairReach : Feel.BottleReach;
    public int Damage => kind == Kind.Chair ? 3 : 2;

    public void Init(Kind k, Vector2 pos)
    {
        kind = k;
        ground = pos;
        hitsLeft = k == Kind.Chair ? Feel.ChairHits : 1;
        shadow = Child("Shadow", Art.Shadow);
        shadow.sortingOrder = -10000;
        sr = Child("Sprite", k == Kind.Chair ? Art.Chair : Art.Bottle);
    }

    SpriteRenderer Child(string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        return r;
    }

    public void Throw(Fighter by)
    {
        holder = null;
        thrower = by;
        ground = by.ground;
        height = Feel.PropThrowHeight;
        vel = new Vector2(by.facing * Feel.PropThrowSpeed, 0f);
        heightVel = Feel.PropThrowUp;
    }

    public void Drop()
    {
        holder = null;
        height = 0f;
    }

    // one swing landed; a worn-out prop breaks
    public void UseUp()
    {
        if (--hitsLeft <= 0) Break();
    }

    public void Break()
    {
        if (holder != null) holder.held = null;
        Game.Props.Remove(this);
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject); // edit-mode tests
    }

    public void Step()
    {
        if (holder != null)
        {
            ground = holder.ground;
            height = holder.height;
            return;
        }
        if (thrower == null) return;

        ground += vel;
        heightVel -= Feel.Gravity;
        height += heightVel;
        foreach (var f in Game.Fighters)
            if (f.team != thrower.team && f.CanBeHit
                && Mathf.Abs(f.ground.x - ground.x) < 16f
                && Mathf.Abs(f.ground.y - ground.y) < Feel.DepthBand
                && height - f.height < Feel.HeightBand + 20f)
            {
                f.TakeHit(Damage, vel.x > 0f ? 1 : -1, true);
                Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
                Break();
                return;
            }
        if (height > 0f) return;
        if (kind == Kind.Bottle) { Break(); return; } // smashes on the floor
        height = 0f;
        vel = Vector2.zero;
        thrower = null;
    }

    void LateUpdate()
    {
        transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);
        float h = Mathf.Round(height);
        var pose = sr.transform;
        if (holder != null)
        {
            // raised overhead, swung down in front while attacking
            bool swinging = holder.state == State.Attack && holder.move == Move.Swing && holder.stateFrames > Feel.SwingStartup;
            int face = holder.facing;
            pose.localPosition = swinging ? new Vector3(face * 28f, h + 26f, 0f) : new Vector3(face * 4f, h + 58f, 0f);
            pose.localRotation = Quaternion.Euler(0f, 0f, swinging ? -70f * face : 0f);
        }
        else
        {
            pose.localPosition = new Vector3(0f, h, 0f);
            pose.localRotation = Quaternion.Euler(0f, 0f, thrower != null ? Game.Frame * 25f : 0f); // tumbles in the air
        }
        shadow.enabled = holder == null;
        int order = -Mathf.RoundToInt(ground.y) * 2;
        sr.sortingOrder = holder != null ? order + 1 : order;
    }
}
