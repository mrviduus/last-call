using UnityEngine;

// Walks into the hero's lane, stops at punching distance, punches on a cooldown.
[RequireComponent(typeof(Fighter))]
public class Enemy : MonoBehaviour
{
    Fighter f;
    int cooldown;

    void Awake()
    {
        f = GetComponent<Fighter>();
        f.Think = Think;
        f.walkX = Feel.EnemyWalkX;
        f.walkDepth = Feel.EnemyWalkX * 0.7f;
        cooldown = Random.Range(30, 90);
    }

    void Think()
    {
        var p = Game.Hero;
        f.moveInput = Vector2.zero;
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
        if (f.moveInput.x == 0f) f.facing = side;

        if (cooldown == 0 && f.InReach(p, reach))
        {
            f.Attack(f.whip ? Move.Whip : Move.Punch);
            cooldown = (f.whip ? Feel.WhipCooldown : Feel.EnemyAttackCooldown) + Random.Range(0, 30);
        }
    }
}
