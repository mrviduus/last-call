using UnityEngine;

public enum State { Idle, Walk, Jump, Attack, Hurt, Launched, Down, GetUp, Dead }

// 2.5D belt-scroller body. Logic runs in Step() at a fixed 60 Hz, in pixels and frames.
// Ground plane = (x, depth); height = jump height. Screen y = depth + height.
public class Fighter : MonoBehaviour
{
    const float Width = 16f, Height = 32f, LyingHeight = 10f;

    public int team;
    public int maxHealth = 5;
    public float walkX = Feel.WalkX, walkDepth = Feel.WalkDepth;
    public System.Action Think; // brain (Player or Enemy), runs before each tick

    public State state;
    public int stateFrames;
    public Vector2 ground, vel, moveInput;
    public float height, heightVel;
    public int health, facing = 1;
    public bool smash;

    int flashFrames, invulnFrames;
    bool bounced;
    SpriteRenderer body, shadow, fist;
    Color color;

    public bool CanAct => state == State.Idle || state == State.Walk;
    public bool Alive => health > 0;
    public bool CanBeHit => Alive && invulnFrames == 0
        && state != State.Launched && state != State.Down && state != State.GetUp;

    public void Init(int team, Color c, Vector2 start)
    {
        this.team = team;
        color = c;
        ground = start;
        health = maxHealth;
        shadow = MakeSprite("Shadow", new Color(0f, 0f, 0f, 0.35f), new Vector2(18f, 5f));
        body = MakeSprite("Body", c, new Vector2(Width, Height));
        fist = MakeSprite("Fist", Color.Lerp(c, Color.black, 0.3f), Vector2.one);
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

    public void Attack(bool isSmash)
    {
        if (!CanAct) return;
        smash = isSmash;
        vel = Vector2.zero;
        Enter(State.Attack);
    }

    public void Jump()
    {
        if (!CanAct) return;
        heightVel = Feel.JumpVel;
        Enter(State.Jump);
    }

    public void TakeHit(int damage, int dir, bool launch)
    {
        if (!CanBeHit) return;
        health -= damage;
        flashFrames = 8;
        facing = -dir;
        if (launch || !Alive || height > 0f)
        {
            Enter(State.Launched);
            bounced = false;
            vel = new Vector2(dir * Feel.LaunchX, 0f);
            heightVel = Feel.LaunchUp;
        }
        else
        {
            Enter(State.Hurt);
            vel = new Vector2(dir * Feel.JabPush, 0f);
        }
    }

    public bool InReach(Fighter t, float reach)
    {
        float dx = (t.ground.x - ground.x) * facing;
        return t.CanBeHit && dx > -4f && dx < reach
            && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand
            && Mathf.Abs(t.height - height) < Feel.HeightBand;
    }

    public void Step()
    {
        Think?.Invoke();
        Tick();
    }

    void Tick()
    {
        stateFrames++;
        if (flashFrames > 0) flashFrames--;
        if (invulnFrames > 0) invulnFrames--;

        bool landed = ApplyPhysics();
        switch (state)
        {
            case State.Idle:
            case State.Walk:
                vel = new Vector2(moveInput.x * walkX, moveInput.y * walkDepth);
                if (moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
                state = vel == Vector2.zero ? State.Idle : State.Walk;
                break;
            case State.Jump:
                if (landed) Enter(State.Idle);
                break;
            case State.Attack:
                if (stateFrames == Feel.PunchStartup) Strike();
                int recovery = smash ? Feel.SmashRecovery : Feel.PunchRecovery;
                if (stateFrames >= Feel.PunchStartup + Feel.PunchActive + recovery) Enter(State.Idle);
                break;
            case State.Hurt:
                if (stateFrames >= Feel.Hitstun) Enter(State.Idle);
                break;
            case State.Launched:
                HitBodiesInPath();
                if (landed && bounced) Enter(State.Down);
                else if (landed) { bounced = true; heightVel = Feel.BounceUp; vel.x *= 0.5f; }
                break;
            case State.Down:
                if (stateFrames >= Feel.DownFrames) Enter(Alive ? State.GetUp : State.Dead);
                break;
            case State.GetUp:
                if (stateFrames >= Feel.GetUpFrames) { Enter(State.Idle); invulnFrames = Feel.GetUpInvuln; }
                break;
        }
    }

    // returns true on the frame the body touches the ground
    bool ApplyPhysics()
    {
        bool grounded = height <= 0f && heightVel <= 0f;
        if (grounded && !CanAct) vel.x = Mathf.MoveTowards(vel.x, 0f, Feel.Friction);
        ground += vel;
        ground.x = Mathf.Max(ground.x, Game.MinX);
        ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
        if (grounded) return false;

        heightVel -= Feel.Gravity;
        height += heightVel;
        if (height > 0f) return false;
        height = 0f;
        heightVel = 0f;
        return true;
    }

    void Strike()
    {
        bool hit = false;
        foreach (var t in Game.Fighters)
            if (t.team != team && InReach(t, smash ? Feel.SmashReach : Feel.PunchReach))
            {
                t.TakeHit(smash ? 2 : 1, facing, smash);
                hit = true;
            }
        if (hit) Game.Freeze(smash ? Feel.SmashHitstop : Feel.Hitstop, smash ? Feel.SmashShake : 0);
    }

    // a body flying fast knocks down its own teammates
    void HitBodiesInPath()
    {
        if (Mathf.Abs(vel.x) < Feel.BodyHitSpeed) return;
        int dir = vel.x > 0f ? 1 : -1;
        foreach (var o in Game.Fighters)
            if (o != this && o.team == team && o.CanBeHit
                && Mathf.Abs(o.ground.x - ground.x) < Width
                && Mathf.Abs(o.ground.y - ground.y) < Feel.DepthBand)
                o.TakeHit(1, dir, true);
    }

    void Enter(State s)
    {
        state = s;
        stateFrames = 0;
    }

    void LateUpdate()
    {
        if (body == null) return;
        transform.position = new Vector3(Mathf.Round(ground.x), Mathf.Round(ground.y), 0f);

        bool lying = state == State.Down || state == State.Dead;
        var size = lying ? new Vector2(Height, LyingHeight) : new Vector2(Width, Height);
        float h = Mathf.Round(height);
        body.transform.localScale = size;
        body.transform.localPosition = new Vector3(0f, h + size.y / 2f, 0f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, state == State.Launched ? 30f * facing : 0f);
        body.enabled = invulnFrames == 0 || invulnFrames / 3 % 2 == 0;
        body.color = flashFrames > 0 && flashFrames / 2 % 2 == 0 ? Color.white : Alive ? color : Color.gray;

        fist.enabled = state == State.Attack && stateFrames >= Feel.PunchStartup
            && stateFrames < Feel.PunchStartup + Feel.PunchActive + 4;
        float fistSize = smash ? 16f : 6f;
        fist.transform.localScale = new Vector3(fistSize, fistSize, 1f);
        fist.transform.localPosition = new Vector3(facing * (Width + fistSize) / 2f, h + Height * 0.65f, 0f);

        // nearer to the camera (lower depth) draws on top; shadows under everything
        int order = -Mathf.RoundToInt(ground.y) * 2;
        body.sortingOrder = order;
        fist.sortingOrder = order + 1;
        shadow.sortingOrder = -10000;
    }
}
