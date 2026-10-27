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
        if (p == null || !p.Alive || !f.CanAct) { engaging = false; return; } // hurt or striking: the turn is over
        if (!engaging && cooldown == 0 && Engaged() < Feel.MaxAttackers) { engaging = true; giveUp = Feel.EngageGiveUp; }
        if (engaging && --giveUp <= 0) { engaging = false; cooldown = 30; } // couldn't get there: let someone else try

        Vector2 to = p.ground - f.ground;
        int side = to.x > 0f ? 1 : to.x < 0f ? -1 : f.facing; // hero exactly on top: keep facing
        // our turn: into the hero's lane at striking distance (whips stay at whip range);
        // otherwise wait on the ring, off the lane, drifting a little
        float reach = f.whip ? Feel.WhipReach : Feel.PunchReach;
        if (waitSide == 0 || (Game.Frame + (int)(pace * 40f)) % 60 == 0) waitSide = PickSide(-side);
        float wantX = engaging ? to.x - side * reach * (f.whip ? 0.8f : 0.7f)
            : to.x + waitSide * (ringX + Mathf.Sin((Game.Frame + pace * 40f) / 40f) * 8f);
        float laneY = p.ground.y + laneOffset;
        if (laneY > Game.MaxDepth || laneY < Game.MinDepth || InTable(f.ground.x + wantX, laneY))
            laneY = p.ground.y - laneOffset; // the floor ends there, or a table stands there: other side
        float wantY = engaging ? to.y : laneY - f.ground.y;
        f.moveInput = new Vector2(
            Mathf.Abs(wantX) > 2f ? Mathf.Sign(wantX) : 0f,
            Mathf.Abs(wantY) > 2f ? Mathf.Sign(wantY) : 0f);
        f.facing = side;
        if (TableAhead() is Prop table) f.moveInput.y = AroundSide(table); // step around it instead of pushing into it

        if (engaging && cooldown == 0 && f.InReach(p, reach))
        {
            engaging = false; // striking holds the turn until the blow is over
            f.Attack(f.whip ? Move.Whip : Move.Punch);
            cooldown = (f.whip ? Feel.WhipCooldown : Feel.EnemyAttackCooldown) + Random.Range(0, 30);
        }
    }
}
