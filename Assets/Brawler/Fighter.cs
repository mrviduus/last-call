using UnityEngine;

// 2.5D belt-scroller body: position on the ground plane (x, depth) + jump height.
// Screen y = depth + height, so walking "up" moves into the screen.
public class Fighter : MonoBehaviour
{
    public float speed = 4f, depthSpeed = 2.5f, jumpSpeed = 10f, gravity = 30f;
    public int maxHealth = 5;

    public Vector2 ground;
    public float height;
    public int health;
    public int facing = 1;
    public float stunTime;

    Vector2 moveInput, knockVel;
    float heightVel, flashTime, fistTime;
    SpriteRenderer body, shadow, fist;
    Color color;
    readonly Vector2 size = new Vector2(0.8f, 1.6f);

    public bool Grounded => height <= 0f;
    public bool Stunned => stunTime > 0f;
    public bool Dead => health <= 0;

    public void Init(Color c, Vector2 start)
    {
        color = c;
        ground = start;
        health = maxHealth;
        shadow = MakeSprite("Shadow", new Color(0f, 0f, 0f, 0.35f), new Vector2(0.9f, 0.25f));
        body = MakeSprite("Body", c, size);
        fist = MakeSprite("Fist", Color.Lerp(c, Color.black, 0.3f), Vector2.one * 0.35f);
    }

    SpriteRenderer MakeSprite(string name, Color c, Vector2 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Game.Square;
        sr.color = c;
        return sr;
    }

    public void Move(Vector2 dir) => moveInput = dir;

    public void Jump()
    {
        if (Grounded && !Stunned && !Dead) heightVel = jumpSpeed;
    }

    public void ShowFist(float scale, float time)
    {
        fist.transform.localScale = Vector3.one * 0.35f * scale;
        fistTime = time;
    }

    public void TakeHit(int damage, int fromDir, float force)
    {
        if (Dead) return;
        health -= damage;
        stunTime = 0.35f;
        flashTime = 0.1f;
        facing = -fromDir;
        knockVel = new Vector2(fromDir * force, 0f);
        if (force >= 8f) heightVel = 7f; // big hits launch
    }

    public bool InReach(Fighter other, float range)
    {
        float dx = (other.ground.x - ground.x) * facing;
        return !other.Dead && dx > -0.2f && dx < range
            && Mathf.Abs(other.ground.y - ground.y) < 0.35f
            && Mathf.Abs(other.height - height) < 1f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stunTime -= dt;
        flashTime -= dt;
        fistTime -= dt;

        bool canMove = !Stunned && !Dead;
        if (canMove && moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
        Vector2 vel = canMove ? new Vector2(moveInput.x * speed, moveInput.y * depthSpeed) : Vector2.zero;
        ground += (vel + knockVel) * dt;
        ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
        knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 25f * dt);

        heightVel -= gravity * dt;
        height += heightVel * dt;
        if (height <= 0f) { height = 0f; heightVel = 0f; }

        transform.position = new Vector3(ground.x, ground.y, 0f);
        body.transform.localPosition = new Vector3(0f, height + size.y / 2f, 0f);
        body.color = flashTime > 0f ? Color.white : Dead ? Color.gray : color;
        fist.enabled = fistTime > 0f;
        float fistX = facing * (size.x + fist.transform.localScale.x) / 2f;
        fist.transform.localPosition = new Vector3(fistX, height + size.y * 0.65f, 0f);

        // nearer to the camera (lower depth) draws on top
        int order = -Mathf.RoundToInt(ground.y * 100f);
        shadow.sortingOrder = -10000;
        body.sortingOrder = order;
        fist.sortingOrder = order + 1;
    }
}
