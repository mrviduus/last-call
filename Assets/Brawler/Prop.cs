using System.Collections.Generic;
using UnityEngine;

// Things in the pub. Chairs, stools, bottles, cues and pool balls can be picked up, swung and thrown.
// Tables are too heavy to lift: they tip over when hit (spilling what stands on them) and slide into people
// when hit again. Pool tables don't move at all; hitting one knocks its balls and cue onto the floor.
// Gore leaves heads (pick up, throw, kick them), torn-off arms (a club) and halves of bodies (just lie there).
// Same ground plane as fighters: (x, depth) + height; stepped by Game at 60 Hz.
public class Prop : MonoBehaviour
{
    public enum Kind { Chair, Stool, Bottle, Table, PoolTable, Cue, Ball, Lamp, Head, Arm, Gib }

    public Kind kind;
    public Vector2 ground, vel;
    public float height, heightVel;
    public Fighter holder, thrower; // thrower: who threw it, or who sent a tipped table sliding
    public int hitsLeft;
    public bool resting;            // standing on a table, the counter or the felt, at its height
    public Fighter seatedBy;        // a chair with a patron on it
    public bool flipped;            // a table knocked onto its side
    public bool mirrored;           // drawn facing left (a chair turned towards its table)
    public bool lit;                // a bottle the hero threw: a molotov

    SpriteRenderer sr, shadow, flame;

    public bool Free => holder == null && thrower == null;
    public bool Heavy => kind == Kind.Table || kind == Kind.PoolTable;
    public bool Pickable => Free && !Heavy && kind != Kind.Gib && (seatedBy == null || seatedBy.state != State.Sit);
    public float Reach => kind switch { Kind.Cue => Feel.CueReach, Kind.Bottle or Kind.Ball or Kind.Head => Feel.BottleReach, _ => Feel.ChairReach };
    public int Damage => kind is Kind.Bottle or Kind.Ball or Kind.Cue or Kind.Head ? Feel.LightPropDamage : Feel.HeavyPropDamage;

    // furniture footprint on the floor: blocks walking (and thrown things), can be jumped over if low enough
    public bool Blocks => kind == Kind.PoolTable || kind == Kind.Table && vel == Vector2.zero;
    public float HalfWidth => kind == Kind.PoolTable ? Feel.PoolHalfWidth : Feel.TableHalfWidth;
    public float HalfDepth => kind == Kind.PoolTable ? Feel.PoolHalfDepth : Feel.TableHalfDepth;
    public float BlockHeight => kind == Kind.PoolTable ? Feel.PoolHeight : Feel.TableHeight;

    public void Init(Kind k, Vector2 pos, float onHeight, bool faceLeft)
    {
        kind = k;
        ground = pos;
        height = onHeight;
        resting = onHeight > 0f;
        mirrored = faceLeft;
        hitsLeft = k switch
        {
            Kind.Bottle => 1, Kind.Table => Feel.TableHits, Kind.Cue => Feel.CueHits, Kind.Lamp => Feel.LampHits,
            Kind.Head => Feel.HeadHits, Kind.Arm => Feel.ArmHits,
            Kind.Ball or Kind.PoolTable or Kind.Gib => int.MaxValue, _ => Feel.ChairHits,
        };
        shadow = Child("Shadow", Art.Shadow);
        shadow.sortingOrder = -10000;
        sr = Child("Sprite", k switch
        {
            Kind.Chair => Art.Chair, Kind.Stool => Art.Stool, Kind.Bottle => Art.Bottle, Kind.PoolTable => Art.PoolTable,
            Kind.Cue => Art.Cue, Kind.Ball => Art.Ball, Kind.Lamp => Art.LampShade, Kind.Arm => Art.Arm,
            Kind.Head or Kind.Gib => null, _ => Art.Table, // a head or a half body: Fling sets its picture
        });
        if (k == Kind.Bottle) // the rag in its neck, burning once Brick lights it
        {
            flame = Child("Flame", Art.Flame[0]);
            flame.transform.SetParent(sr.transform, false); // tumbles with the bottle
            flame.transform.localPosition = new Vector3(0f, 10f, 0f);
        }
    }

    public void Tint(Color c) => sr.color = c; // pool balls come in colours

    const int MaxParts = 12;
    internal static readonly Queue<Prop> parts = new Queue<Prop>();

    // a body part torn off and sent flying (thrower: a kicked head hurts whoever it hits); past MaxParts the oldest goes
    public static Prop Fling(Kind kind, Sprite sprite, Vector2 ground, float height, Vector2 vel, float up, bool faceLeft, Fighter thrower = null)
    {
        var p = Game.SpawnProp(kind, ground, 0f, faceLeft);
        if (sprite != null) p.sr.sprite = sprite;
        p.height = height;
        p.vel = vel;
        p.heightVel = up;
        p.thrower = thrower;
        parts.Enqueue(p);
        while (parts.Count > MaxParts)
        {
            var old = parts.Dequeue();
            if (old == null || old.holder != null) continue; // already broken, or a club in someone's hands
            Game.Props.Remove(old);
            if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
        }
        return p;
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
        lit = kind == Kind.Bottle && by.team == 0; // Brick lights the rag first
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
        if (kind == Kind.PoolTable)
        {
            KnockOffWhatStandsOnMe(dir); // too heavy to budge; balls and cue roll off the felt
            return true;
        }
        if (kind == Kind.Table && !flipped)
        {
            flipped = true;
            Sfx.Play("wood", 0.6f);
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
            resting = false; // swept off the counter, a table or the felt: falls to the floor
            vel = new Vector2(dir * 1.5f, 0f);
            heightVel = 1.5f;
            return true;
        }
        if (kind is Kind.Head or Kind.Ball && height <= 0f && by != null)
        {
            vel = new Vector2(dir * Feel.PuntSpeed, 0f); // kicked along the floor like a football, at whoever is there
            heightVel = Feel.PuntUp;
            thrower = by;
            return true;
        }
        return false;
    }

    void KnockOffWhatStandsOnMe(int dir)
    {
        foreach (var p in Game.Props)
            if (p != this && p.resting && Mathf.Abs(p.ground.x - ground.x) < HalfWidth + 4f
                && Mathf.Abs(p.ground.y - ground.y) < Feel.DepthBand)
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
        bool flesh = kind is Kind.Head or Kind.Arm or Kind.Gib;
        Sfx.Play(kind is Kind.Bottle or Kind.Lamp ? "glass" : flesh ? "splat" : "wood");
        if (kind == Kind.Bottle && lit) Fire.Spawn(ground); // a molotov: the floor burns
        else if (kind == Kind.Bottle && !resting) Puddle.Spawn(ground); // what was in it goes on the floor
        if (!Application.isPlaying) { DestroyImmediate(gameObject); return; } // edit-mode tests
        if (flesh) Fx.Blood(ground, height + 4f, 20, 0f, 3f);
        var color = kind switch
        {
            Kind.Bottle => new Color32(0x4a, 0x2a, 0x10, 255),   // brown glass
            Kind.Chair or Kind.Lamp => new Color32(0x17, 0x58, 0x2f, 255), // green cushion or shade
            Kind.Stool => new Color32(0xa0, 0x2a, 0x28, 255),    // red leather
            Kind.Head or Kind.Arm or Kind.Gib => new Color32(0xe8, 0xb0, 0x80, 255), // skin and bone
            _ => new Color32(0x9a, 0x5e, 0x2e, 255),             // wood
        };
        for (int i = 0; i < 7; i++)
            Shard.Spawn(new Vector3(ground.x, ground.y + height + 6f, 0f), i % 3 == 0 ? new Color32(0x5a, 0x32, 0x18, 255) : color);
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
        if (resting || kind == Kind.PoolTable) return;

        bool airborne = height > 0f || heightVel > 0f;
        if (airborne && kind is Kind.Head or Kind.Arm or Kind.Gib && Game.Frame % 3 == 0)
            Fx.Blood(ground, height, 1, 0f, 0.5f); // drips as it flies
        ground += vel;
        ground.x = Mathf.Clamp(ground.x, Game.MinX, Game.MaxX);
        ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
        if (thrower != null && (HitSomeone() || HitTable())) return;

        if (airborne)
        {
            float impact = heightVel - Feel.Gravity;
            heightVel = impact;
            height += heightVel;
            if (height > 0f) return;
            height = 0f;
            heightVel = 0f;
            if (kind is Kind.Ball or Kind.Head)
            {
                Sfx.Play(kind == Kind.Head ? "splat" : "ball", Mathf.Clamp01(-impact / 4f) * (kind == Kind.Head ? 0.4f : 1f));
                if (impact < -1.5f) heightVel = -impact * 0.45f; // bounces, smaller each time
                else thrower = null;                             // then rolls
                vel *= 0.7f;
                return;
            }
            if (kind == Kind.Bottle || thrower != null) { Break(); return; } // bottles shatter, thrown things smash
            vel = Vector2.zero;
            return;
        }
        // tables slide to a stop, balls roll a long way
        vel = Vector2.MoveTowards(vel, Vector2.zero, kind == Kind.Ball ? Feel.Friction * 0.15f : Feel.Friction);
        if (vel == Vector2.zero && kind == Kind.Table) thrower = null;
    }

    bool HitSomeone()
    {
        if (kind == Kind.Table && Mathf.Abs(vel.x) < 3f) return false; // only a table sent sliding hurts, not one tipping over
        foreach (var f in Game.Fighters)
            if (f.team != thrower.team && f.CanBeHit && f.state != State.Held
                && Mathf.Abs(f.ground.x - ground.x) < 16f
                && Mathf.Abs(f.ground.y - ground.y) < Feel.DepthBand
                && Mathf.Abs(height - f.height) < Feel.HeightBand + 20f)
            {
                f.TakeHit(Damage, vel.x > 0f ? 1 : -1, true);
                if (lit) f.Ignite(); // a molotov in the face
                Sfx.Play("hit_heavy");
                Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
                if (kind == Kind.Table) vel *= 0.5f;
                else if (kind is Kind.Ball or Kind.Head) BounceBack();
                else Break();
                return true;
            }
        return false;
    }

    // a thrown thing that flies into a table or pool table: knocks it over (or its balls off) and breaks,
    // a ball bounces back instead
    bool HitTable()
    {
        if (Heavy) return false;
        Prop table = Game.Props.Find(t => t.Blocks && t.holder == null && height <= t.BlockHeight + 10f
            && Mathf.Abs(t.ground.x - ground.x) < t.HalfWidth
            && Mathf.Abs(t.ground.y - ground.y) < t.HalfDepth + 4f);
        if (table == null) return false;
        table.Struck(vel.x > 0f ? 1 : -1, thrower);
        if (kind is Kind.Ball or Kind.Head) BounceBack();
        else Break();
        return true;
    }

    void BounceBack()
    {
        vel = new Vector2(-vel.x * 0.3f, 0f);
        heightVel = 2f;
        thrower = null;
    }

    void LateUpdate()
    {
        transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);
        float h = Mathf.Round(height);
        var pose = sr.transform;
        if (kind == Kind.Table) sr.sprite = flipped ? Art.TableFlipped : Art.Table;
        sr.flipX = holder == null ? mirrored : holder.facing < 0;
        if (holder != null)
        {
            // carried in front at the fists, swung out further while attacking; a cue is held level and thrust
            bool swinging = holder.state == State.Attack && holder.move == Move.Swing && holder.stateFrames > Feel.SwingStartup;
            int face = holder.facing;
            if (kind == Kind.Cue)
            {
                pose.localPosition = new Vector3(face * (swinging ? 40f : 22f), h + 28f, 0f);
                pose.localRotation = Quaternion.identity;
            }
            else
            {
                pose.localPosition = swinging ? new Vector3(face * 28f, h + 26f, 0f) : new Vector3(face * 16f, h + 24f, 0f);
                pose.localRotation = Quaternion.Euler(0f, 0f, swinging ? -70f * face : 0f);
            }
        }
        else
        {
            pose.localPosition = new Vector3(0f, h, 0f);
            bool tumbling = (thrower != null || kind is Kind.Head or Kind.Gib && height > 0f) && !Heavy && kind != Kind.Ball;
            pose.localRotation = Quaternion.Euler(0f, 0f, tumbling ? Game.Frame * 25f : 0f);
        }
        shadow.enabled = holder == null && !resting && kind != Kind.PoolTable;
        if (flame != null) // the rag catches as Brick winds up to throw, and burns all the way
        {
            flame.enabled = lit || holder != null && holder.team == 0 && holder.state == State.Attack && holder.move == Move.ThrowProp;
            flame.sprite = Art.Flame[Game.Frame / 4 % 3];
            flame.sortingOrder = sr.sortingOrder + 1;
        }
        int order = -Mathf.RoundToInt(ground.y) * 2;
        sr.sortingOrder = holder != null || resting ? order + 1 : order; // on top of what it stands on
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
