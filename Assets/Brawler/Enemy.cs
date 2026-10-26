using UnityEngine;

// Walks into the hero's lane, stops at punching distance, punches on a cooldown.
// Patrons sit at their table until the hero starts the fight.
[RequireComponent(typeof(Fighter))]
public class Enemy : MonoBehaviour
{
    Fighter f;
    int cooldown, wakeDelay;

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

    // go around on the side we're already leaning to, unless the floor ends there
    float AroundSide(Prop table)
    {
        bool behind = f.ground.y >= table.ground.y;
        if (behind && table.ground.y + table.HalfDepth + 8f > Game.MaxDepth) behind = false;
        if (!behind && table.ground.y - table.HalfDepth - 8f < Game.MinDepth) behind = true;
        return behind ? 1f : -1f;
    }

    // teammates already swinging at the hero
    int Attackers()
    {
        int n = 0;
        foreach (var o in Game.Fighters) if (o.team == f.team && o.state == State.Attack) n++;
        return n;
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
        if (p == null || !p.Alive || !f.CanAct) return;

        Vector2 to = p.ground - f.ground;
        int side = to.x > 0f ? 1 : to.x < 0f ? -1 : f.facing; // hero exactly on top: keep facing
        // whip fighters hang back at whip range instead of walking in
        float reach = f.whip ? Feel.WhipReach : Feel.PunchReach;
        float wantX = to.x - side * reach * (f.whip ? 0.8f : 0.7f);
        f.moveInput = new Vector2(
            Mathf.Abs(wantX) > 2f ? Mathf.Sign(wantX) : 0f,
            Mathf.Abs(to.y) > 2f ? Mathf.Sign(to.y) : 0f);
        f.facing = side;
        if (TableAhead() is Prop table) f.moveInput.y = AroundSide(table); // step around it instead of pushing into it

        if (cooldown == 0 && f.InReach(p, reach) && Attackers() < Feel.MaxAttackers)
        {
            f.Attack(f.whip ? Move.Whip : Move.Punch);
            cooldown = (f.whip ? Feel.WhipCooldown : Feel.EnemyAttackCooldown) + Random.Range(0, 30);
        }
    }
}
