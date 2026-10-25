using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// WASD move, K / Space jump, J punch. Third punch of a combo = big smash.
[RequireComponent(typeof(Fighter))]
public class Player : MonoBehaviour
{
    const float ComboWindow = 0.45f, AttackCooldown = 0.2f, Reach = 1.1f;

    Fighter f;
    int comboStep;
    float lastAttack = -1f;

    void Awake() => f = GetComponent<Fighter>();

    void Update()
    {
        if (f.Dead)
        {
            if (AttackPressed()) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        f.Move(ReadMove());
        if (JumpPressed()) f.Jump();
        if (AttackPressed() && !f.Stunned && Time.time - lastAttack > AttackCooldown) Attack();
    }

    void Attack()
    {
        comboStep = Time.time - lastAttack < ComboWindow ? comboStep + 1 : 1;
        lastAttack = Time.time;
        bool smash = comboStep >= 3;
        if (smash) comboStep = 0;

        f.ShowFist(smash ? 3f : 1f, 0.15f);
        foreach (var e in Game.Enemies)
            if (f.InReach(e, smash ? Reach * 1.5f : Reach))
                e.TakeHit(smash ? 2 : 1, f.facing, smash ? 12f : 2f);
    }

#if ENABLE_INPUT_SYSTEM
    static Vector2 ReadMove()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
        float y = (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
        return new Vector2(x, y);
    }

    static bool JumpPressed() =>
        Keyboard.current != null && (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);

    static bool AttackPressed() => Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame;
#else
    static Vector2 ReadMove() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    static bool JumpPressed() => Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Space);
    static bool AttackPressed() => Input.GetKeyDown(KeyCode.J);
#endif
}
