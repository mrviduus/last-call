using UnityEngine;

// Waits its turn on a ring around the hero, off the hero's lane; when one of the MaxAttackers turns is free,
// walks into the lane, strikes once, then backs off to the ring again.
// Patrons sit at their table until the hero starts the fight.
[RequireComponent(typeof(Fighter))]
public class Enemy : MonoBehaviour
{
    Fighter f;
    int cooldown, wakeDelay, giveUp;
    bool engaging;     // our turn: allowed into the hero's lane to strike
    float laneOffset;  // where we wait: this far in front of or behind the hero's lane
    float ringX;       // ...and this far off the hero (x)
    int waitSide;      // ...on this side of the hero (-1 left, 1 right; 0 = not picked yet)
    float pace;        // phase of the slow drift while waiting, so the ring never stands still
    public bool chucker; // throws bottles from a distance instead of queueing for a melee turn
    int hold;            // the boss: frames left holding the hero up before the throw
    Fighter grudge;      // a mate who hit us: settling that comes first
    int grudgeFrames;
    SpriteRenderer anger; // a red flash over the head while holding a grudge
    public bool HasGrudge => grudge != null;

    // called right after AddComponent (also works in edit-mode tests, where Awake doesn't run);
    // wakeDelay: frames a seated patron waits after the fight starts before getting up
    public void Setup(int wakeDelay = 0)
    {
        f = GetComponent<Fighter>();
        f.Think = Think;
        f.walkX = Feel.EnemyWalkX;
        f.faceMove = false; // backs off facing the hero
        f.walkDepth = Feel.EnemyWalkX * 0.7f;
        cooldown = Random.Range(30, 90);
        this.wakeDelay = wakeDelay;
        laneOffset = (Random.value < 0.5f ? -1f : 1f) * Random.Range(Feel.RingDepth * 0.6f, Feel.RingDepth * 1.6f);
        ringX = Random.Range(Feel.RingX - 10f, Feel.RingX + 40f);
        pace = Random.Range(0f, 100f);
    }

    // a standing table right in the way of where we're walking
    Prop TableAhead()
    {
        if (f.moveInput.x == 0f) return null;
        foreach (var t in Game.Props)
        {
            float dx = (t.ground.x - f.ground.x) * Mathf.Sign(f.moveInput.x);
            if (t.Blocks && dx > 0f && dx < t.HalfWidth + 24f && Mathf.Abs(t.ground.y - f.ground.y) < t.HalfDepth + 6f)
                return t;
        }
        return null;
    }

    // keeps its distance in the hero's lane and throws bottles along it
    void Chuck(Fighter p)
    {
        Vector2 to = p.ground - f.ground;
        int side = to.x > 0f ? 1 : -1;
        float standX = p.ground.x - side * Feel.ChuckerRange;
        if (standX < Game.MinX + 10f || standX > Game.MaxX - 10f) standX = p.ground.x + side * Feel.ChuckerRange; // no room: the other side
        float wantX = standX - f.ground.x;
        f.moveInput = new Vector2(Mathf.Abs(wantX) > 8f ? Mathf.Sign(wantX) : 0f, Mathf.Abs(to.y) > 2f ? Mathf.Sign(to.y) : 0f);
        f.facing = side;
        if (TableAhead() is Prop table) f.moveInput.y = AroundSide(table);
        if (cooldown == 0 && Mathf.Abs(to.y) < Feel.DepthBand && Mathf.Abs(to.x) > 60f && Mathf.Abs(to.x) < Feel.ChuckerRange + 25f)
        {
            f.PickUp(Game.SpawnProp(Prop.Kind.Bottle, f.ground)); // one more from the crate
            f.Attack(Move.ThrowProp);
            cooldown = Feel.ChuckerCooldown + Random.Range(0, 40);
        }
    }

    static bool InTable(float x, float y)
    {
        foreach (var t in Game.Props)
            if (t.Blocks && Mathf.Abs(t.ground.x - x) < t.HalfWidth + 4f && Mathf.Abs(t.ground.y - y) < t.HalfDepth + 4f)
                return true;
        return false;
    }

    // go around on the side we're already leaning to, unless the floor ends there
    float AroundSide(Prop table)
    {
        bool behind = f.ground.y >= table.ground.y;
        if (behind && table.ground.y + table.HalfDepth + 8f > Game.MaxDepth) behind = false;
        if (!behind && table.ground.y - table.HalfDepth - 8f < Game.MinDepth) behind = true;
        return behind ? 1f : -1f;
    }

    // teammates having their turn: walking in to strike, or striking
    int Engaged()
    {
        int n = 0;
        foreach (var o in Game.Fighters)
            if (o != f && o.team == f.team && (o.state == State.Attack || o.GetComponent<Enemy>() is Enemy e && e.engaging)) n++;
        return n;
    }

    // wait on the less crowded side of the hero, so a crowd surrounds the hero instead of queueing on one side
    int PickSide(int mine)
    {
        int here = 0, there = 0;
        foreach (var o in Game.Fighters)
            if (o != f && o.team == f.team && o.Alive && o.state != State.Sit)
                if ((o.ground.x < Game.Hero.ground.x ? -1 : 1) == mine) here++; else there++;
        int pick = here > there + 1 ? -mine : mine;
        float x = Game.Hero.ground.x + pick * ringX;
        return x < Game.MinX + 10f || x > Game.MaxX - 10f ? -pick : pick; // no room by the wall: the other side
    }

    void Think()
    {
        var p = Game.Hero;
        f.moveInput = Vector2.zero;
        if (f.state == State.Sit)
        {
            if (Game.Brawl && --wakeDelay <= 0) f.StandUp(); // one by one, the bar joins the fight
            return;
        }
        if (cooldown > 0) cooldown--;
        if (f.state == State.Grab && f.grabbed == p) // the boss has the hero: holds him up a moment, then throws
        {
            if (--hold <= 0) f.Attack(Move.Throw);
            return;
        }
        if (p == null || !p.Alive || !f.CanAct) { engaging = false; return; } // hurt or striking: the turn is over
        if (f.burning > 0) // on fire: runs about flailing, no thought of fighting
        {
            engaging = false;
            float t = Game.Frame + pace * 40f;
            f.moveInput = new Vector2(Mathf.Sign(Mathf.Sin(t / 15f)), Mathf.Sin(t / 9f));
            return;
        }
        if (grudge != null && (--grudgeFrames <= 0 || !grudge.Alive)) grudge = null; // settled (waits for them to get up)
        if (f.drunk && grudge == null && Random.value < Feel.DrunkPickFight) PickFight(); // a drunk takes offence at nothing
        if (chucker && grudge == null) { Chuck(p); return; }
        if (grudge == null && !engaging && cooldown == 0 && Engaged() < (Game.Wave >= Feel.LateWave || Game.Midnight ? Feel.MaxAttackersLate : Feel.MaxAttackers)) { engaging = true; giveUp = Feel.EngageGiveUp; }
        if (engaging && --giveUp <= 0) { engaging = false; cooldown = 30; } // couldn't get there: let someone else try

        // a grudge goes straight at the mate who hit us, no waiting for a turn
        var target = grudge != null ? grudge : p;
        bool going = engaging || grudge != null;
        Vector2 to = target.ground - f.ground;
        int side = to.x > 0f ? 1 : to.x < 0f ? -1 : f.facing; // target exactly on top: keep facing
        // our turn: into the hero's lane at striking distance (whips stay at whip range);
        // otherwise wait on the ring, off the lane, drifting a little
        float reach = f.whip ? Feel.WhipReach : Feel.PunchReach;
        if (waitSide == 0 || (Game.Frame + (int)(pace * 40f)) % 60 == 0) waitSide = PickSide(-side);
        float wantX = going ? to.x - side * reach * (f.whip ? 0.8f : 0.7f)
            : to.x + waitSide * (ringX + Mathf.Sin((Game.Frame + pace * 40f) / 40f) * 8f);
        float laneY = p.ground.y + laneOffset;
        if (laneY > Game.MaxDepth || laneY < Game.MinDepth || InTable(f.ground.x + wantX, laneY))
            laneY = p.ground.y - laneOffset; // the floor ends there, or a table stands there: other side
        float wantY = going ? to.y : laneY - f.ground.y;
        f.moveInput = new Vector2(
            Mathf.Abs(wantX) > 2f ? Mathf.Sign(wantX) : 0f,
            Mathf.Abs(wantY) > 2f ? Mathf.Sign(wantY) : 0f);
        if (f.drunk) f.moveInput.y = Mathf.Clamp(f.moveInput.y + Mathf.Sin((Game.Frame + pace * 40f) / 25f) * Feel.DrunkSway, -1f, 1f);
        f.facing = side;
        if (TableAhead() is Prop table) f.moveInput.y = AroundSide(table); // step around it instead of pushing into it

        if (going && cooldown == 0 && f.InReach(target, reach))
        {
            engaging = false; // striking holds the turn until the blow is over
            if (f.armored && target == p && p.CanBeHit && Random.value < Feel.BossGrabChance)
            {
                f.Grab(p);
                hold = Feel.BossHold;
            }
            else f.Attack(f.whip ? Move.Whip : Move.Punch);
            cooldown = (f.whip ? Feel.WhipCooldown : Feel.EnemyAttackCooldown) + Random.Range(0, 30);
        }
    }

    // hit by one of our own: sometimes we take it personally (the boss never does)
    public void HitByMate(Fighter mate)
    {
        if (f.armored || mate == f || Random.value > Feel.GrudgeChance) return;
        StartGrudge(mate);
    }

    // OLD TOM decides the nearest mate looked at him funny
    void PickFight()
    {
        Fighter nearest = null;
        foreach (var o in Game.Fighters)
            if (o != f && o.team == f.team && o.Alive && o.Solid && Mathf.Abs(o.ground.x - f.ground.x) < Feel.DrunkPickRange
                && (nearest == null || Vector2.Distance(o.ground, f.ground) < Vector2.Distance(nearest.ground, f.ground)))
                nearest = o;
        if (nearest != null) StartGrudge(nearest);
    }

    void StartGrudge(Fighter mate)
    {
        grudge = mate;
        grudgeFrames = Feel.GrudgeFrames;
        engaging = false;
        if (anger == null)
        {
            anger = new GameObject("Anger").AddComponent<SpriteRenderer>();
            anger.transform.SetParent(transform, false);
            anger.sprite = Art.Spark[1];
            anger.color = new Color(1f, 0.25f, 0.2f);
        }
    }

    void LateUpdate()
    {
        if (anger == null) return;
        anger.enabled = grudge != null && Game.Frame / 8 % 2 == 0; // blinks over the head
        anger.transform.localPosition = new Vector3(0f, f.height + 70f, 0f);
        anger.sortingOrder = 29000;
    }
}
