using UnityEngine;

public enum State { Idle, Walk, Jump, Attack, Hurt, Launched, Down, GetUp, Dead }

// 2.5D belt-scroller body. Logic runs in Step() at a fixed 60 Hz, in pixels and frames.
// Ground plane = (x, depth); height = jump height. Screen y = depth + height.
public class Fighter : MonoBehaviour
{
    const float Width = 16f;

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
    bool bounced, strikePending;
    SpriteRenderer body, shadow, fist;
    Art.Frames art;

    public bool CanAct => state == State.Idle || state == State.Walk;
    public bool Alive => health > 0;
    public bool Solid => Alive && height <= 0f && state != State.Launched && state != State.Down;
    public bool CanBeHit => Alive && invulnFrames == 0
        && state != State.Launched && state != State.Down && state != State.GetUp;

    public void Init(int team, string palette, string[] head, Vector2 start)
    {
        this.team = team;
        ground = start;
        health = maxHealth;
        art = Art.For(palette, head);
        shadow = MakeSprite("Shadow", Art.Shadow);
        shadow.sortingOrder = -10000; // under every body
        body = MakeSprite("Body", art.Stand);
        fist = MakeSprite("Fist", art.Fist);
        fist.transform.localScale = new Vector3(2f, 2f, 1f);
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
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
                if (stateFrames == Feel.PunchStartup) strikePending = true;
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

    // called by Game after every fighter has stepped, so same-frame punches trade
    public void ResolveStrike()
    {
        if (!strikePending) return;
        strikePending = false;
        Strike();
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
                && Mathf.Abs(o.ground.y - ground.y) < Feel.DepthBand
                && Mathf.Abs(o.height - height) < Feel.HeightBand)
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

        float h = Mathf.Round(height);
        bool punching = state == State.Attack && stateFrames >= Feel.PunchStartup
            && stateFrames < Feel.PunchStartup + Feel.PunchActive + 6;
        body.sprite = state switch
        {
            State.Walk => Game.Frame / 8 % 2 == 0 ? art.Walk : art.Stand,
            State.Jump or State.GetUp => art.Air,
            State.Attack => punching ? art.Punch : art.Stand,
            State.Hurt or State.Launched => art.Hurt,
            _ => art.Stand,
        };
        body.flipX = facing < 0;

        // lying = sprite rotated 90 degrees around its feet, head pointing backwards
        bool lying = state == State.Down || state == State.Dead;
        body.transform.localPosition = lying ? new Vector3(16f * facing, 8f, 0f) : new Vector3(0f, h, 0f);
        float tilt = lying ? 90f : state == State.Launched ? 30f : 0f;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, tilt * facing);
        body.enabled = invulnFrames == 0 || invulnFrames / 3 % 2 == 0;
        body.color = flashFrames > 0 && flashFrames / 2 % 2 == 0 ? new Color(1f, 0.45f, 0.45f)
            : Alive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

        fist.enabled = smash && punching;
        fist.flipX = facing < 0;
        fist.transform.localPosition = new Vector3(facing * 18f, h + 17f, 0f);

        // nearer to the camera (lower depth) draws on top; shadows under everything
        int order = -Mathf.RoundToInt(ground.y) * 2;
        body.sortingOrder = order;
        fist.sortingOrder = order + 1;
    }
}
