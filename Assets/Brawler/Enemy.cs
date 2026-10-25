using UnityEngine;

// Walks into the player's lane, stops at punching distance, punches on a cooldown.
[RequireComponent(typeof(Fighter))]
public class Enemy : MonoBehaviour
{
    const float Reach = 0.9f, AttackCooldown = 1.2f;

    Fighter f;
    float nextAttack;

    void Awake()
    {
        f = GetComponent<Fighter>();
        nextAttack = Time.time + Random.Range(0.5f, 1.5f);
    }

    void Update()
    {
        if (f.Dead)
        {
            f.Move(Vector2.zero);
            enabled = false; // Fighter keeps running so the knockback still plays
            Destroy(gameObject, 1f);
            return;
        }

        var p = Game.Hero;
        if (p.Dead) { f.Move(Vector2.zero); return; }

        Vector2 to = p.ground - f.ground;
        float wantX = to.x - Mathf.Sign(to.x) * Reach * 0.8f;
        var dir = new Vector2(
            Mathf.Abs(wantX) > 0.1f ? Mathf.Sign(wantX) : 0f,
            Mathf.Abs(to.y) > 0.1f ? Mathf.Sign(to.y) : 0f);
        f.Move(dir);
        if (dir.x == 0f && !f.Stunned) f.facing = to.x > 0f ? 1 : -1;

        if (Time.time > nextAttack && !f.Stunned && f.InReach(p, Reach))
        {
            nextAttack = Time.time + AttackCooldown;
            f.ShowFist(1f, 0.15f);
            p.TakeHit(1, f.facing, 3f);
        }
    }
}
