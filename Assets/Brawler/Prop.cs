using UnityEngine;

// Things in the bar: chairs, stools and bottles can be picked up, swung and thrown; tables are too heavy
// to lift but tip over when hit (spilling what stands on them) and then slide into people when hit again.
// Same ground plane as fighters: (x, depth) + height; stepped by Game at 60 Hz.
public class Prop : MonoBehaviour
{
    public enum Kind { Chair, Stool, Bottle, Table }

    public Kind kind;
    public Vector2 ground, vel;
    public float height, heightVel;
    public Fighter holder, thrower; // thrower: who threw it, or who sent a tipped table sliding
    public int hitsLeft;
    public bool resting;            // standing on a table or the counter, at its height
    public bool flipped;            // a table knocked onto its side
    public bool mirrored;           // drawn facing left (a chair turned towards its table)

    SpriteRenderer sr, shadow;

    public bool Free => holder == null && thrower == null;
    public bool Pickable => Free && kind != Kind.Table;
    public float Reach => kind == Kind.Bottle ? Feel.BottleReach : Feel.ChairReach;
    public int Damage => kind == Kind.Bottle ? 2 : 3;

    public void Init(Kind k, Vector2 pos, float onHeight, bool faceLeft)
    {
        kind = k;
        ground = pos;
        height = onHeight;
        resting = onHeight > 0f;
        mirrored = faceLeft;
        hitsLeft = k switch { Kind.Bottle => 1, Kind.Table => Feel.TableHits, _ => Feel.ChairHits };
        shadow = Child("Shadow", Art.Shadow);
        shadow.sortingOrder = -10000;
        sr = Child("Sprite", k switch
        {
            Kind.Chair => Art.Chair, Kind.Stool => Art.Stool, Kind.Bottle => Art.Bottle, _ => Art.Table,
        });
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
        resting = false;
        ground = by.ground;
        height = Feel.PropThrowHeight;
        vel = new Vector2(by.facing * Feel.PropThrowSpeed, 0f);
        heightVel = Feel.PropThrowUp;
    }

    public void Drop()
    {
        holder = null;
        height = 0f;
        resting = false;
    }

    // hit by a punch, kick or a swung prop; returns true if it reacted (counts as a hit)
    public bool Struck(int dir, Fighter by)
    {
        if (kind == Kind.Table && !flipped)
        {
            flipped = true;
            vel = new Vector2(dir * 2f, 0f);
            thrower = by;
            KnockOffWhatStandsOnMe(dir);
            return true;
        }
        if (kind == Kind.Table)
        {
            if (--hitsLeft <= 0) { Break(); return true; } // a beaten-up table falls apart
            vel = new Vector2(dir * Feel.TableSlide, 0f); // a tipped table slides into people
            thrower = by;
            return true;
        }
        if (resting)
        {
            resting = false; // bottle swept off the counter or a table: falls and smashes
            vel = new Vector2(dir * 1.5f, 0f);
            heightVel = 1.5f;
            return true;
        }
        return false;
    }

    void KnockOffWhatStandsOnMe(int dir)
    {
        foreach (var p in Game.Props)
            if (p != this && p.resting && Mathf.Abs(p.ground.x - ground.x) < 24f && Mathf.Abs(p.ground.y - ground.y) < Feel.DepthBand)
                p.Struck(dir, null);
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
        if (!Application.isPlaying) { DestroyImmediate(gameObject); return; } // edit-mode tests
        var color = kind switch
        {
            Kind.Bottle => new Color32(0x2a, 0x8a, 0x4a, 255),
            Kind.Table => new Color32(0x6a, 0x72, 0x90, 255),
            _ => new Color32(0xa0, 0x2a, 0x3a, 255),
        };
        for (int i = 0; i < 7; i++)
            Shard.Spawn(new Vector3(ground.x, ground.y + height + 6f, 0f), i % 3 == 0 ? new Color32(0x9a, 0xa2, 0xbc, 255) : color);
        Destroy(gameObject);
    }

    public void Step()
    {
        if (holder != null)
        {
            ground = holder.ground;
            height = holder.height;
            return;
        }
        if (resting) return;

        bool airborne = height > 0f || heightVel > 0f;
        ground += vel;
        ground.x = Mathf.Clamp(ground.x, Game.MinX, Game.MaxX);
        ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
        if (thrower != null && (HitSomeone() || HitTable())) return;

        if (airborne)
        {
            heightVel -= Feel.Gravity;
            height += heightVel;
            if (height > 0f) return;
            height = 0f;
            heightVel = 0f;
            if (kind == Kind.Bottle || thrower != null) { Break(); return; } // bottles shatter, thrown chairs smash
            vel = Vector2.zero;
            return;
        }
        vel = Vector2.MoveTowards(vel, Vector2.zero, Feel.Friction); // a sliding table slows down
        if (vel == Vector2.zero && kind == Kind.Table) thrower = null;
    }

    bool HitSomeone()
    {
        if (kind == Kind.Table && Mathf.Abs(vel.x) < 3f) return false; // only a table sent sliding hurts, not one tipping over
        foreach (var f in Game.Fighters)
            if (f.team != thrower.team && f.CanBeHit
                && Mathf.Abs(f.ground.x - ground.x) < 16f
                && Mathf.Abs(f.ground.y - ground.y) < Feel.DepthBand
                && height - f.height < Feel.HeightBand + 20f)
            {
                f.TakeHit(Damage, vel.x > 0f ? 1 : -1, true);
                Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
                if (kind == Kind.Table) vel *= 0.5f;
                else Break();
                return true;
            }
        return false;
    }

    // a thrown chair or bottle that flies into a table knocks it over and breaks
    bool HitTable()
    {
        if (kind == Kind.Table || height > Feel.TableHeight + 10f) return false;
        Prop table = Game.Props.Find(t => t.kind == Kind.Table && t.holder == null
            && Mathf.Abs(t.ground.x - ground.x) < Feel.TableHalfWidth
            && Mathf.Abs(t.ground.y - ground.y) < Feel.TableHalfDepth + 4f);
        if (table == null) return false;
        table.Struck(vel.x > 0f ? 1 : -1, thrower);
        Break();
        return true;
    }

    void LateUpdate()
    {
        transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);
        float h = Mathf.Round(height);
        var pose = sr.transform;
        if (kind == Kind.Table) sr.sprite = flipped ? Art.TableFlipped : Art.Table;
        sr.flipX = holder == null && mirrored;
        if (holder != null)
        {
            // carried in front at the fists, swung out further while attacking
            bool swinging = holder.state == State.Attack && holder.move == Move.Swing && holder.stateFrames > Feel.SwingStartup;
            int face = holder.facing;
            pose.localPosition = swinging ? new Vector3(face * 28f, h + 26f, 0f) : new Vector3(face * 16f, h + 24f, 0f);
            pose.localRotation = Quaternion.Euler(0f, 0f, swinging ? -70f * face : 0f);
        }
        else
        {
            pose.localPosition = new Vector3(0f, h, 0f);
            pose.localRotation = Quaternion.Euler(0f, 0f, thrower != null && kind != Kind.Table ? Game.Frame * 25f : 0f); // tumbles in the air
        }
        shadow.enabled = holder == null && !resting;
        int order = -Mathf.RoundToInt(ground.y) * 2;
        sr.sortingOrder = holder != null || resting ? order + 1 : order; // on top of the table it stands on
    }
}

// a bit of glass or wood flying off a broken prop; cosmetic only
public class Shard : MonoBehaviour
{
    Vector2 vel;
    int life = 40;

    public static void Spawn(Vector3 at, Color32 color)
    {
        var sr = new GameObject("Shard").AddComponent<SpriteRenderer>();
        sr.sprite = Art.Shard;
        sr.color = color;
        sr.sortingOrder = 20000;
        sr.transform.position = at;
        var s = sr.gameObject.AddComponent<Shard>();
        s.vel = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(1.5f, 4f));
    }

    void FixedUpdate()
    {
        vel.y -= Feel.Gravity;
        transform.position += (Vector3)vel;
        if (--life <= 0) Destroy(gameObject);
    }
}
