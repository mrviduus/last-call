using UnityEngine;

namespace Brawler
{
    // 2.5D belt-scroller body. Logic runs in Step() at a fixed 60 Hz, in pixels and frames.
    // Ground plane = (x, depth); height = jump height. Screen y = depth + height.
    public class Fighter : MonoBehaviour
    {
        const float Width = 28f; // body width for bodies hitting bodies

        public int team;
        public string look; // for the HUD name
        public int maxHealth = 5;
        public float walkX = Feel.WalkX, walkDepth = Feel.WalkDepth;
        public System.Action Think; // brain (Player or Enemy), runs before each tick

        public State state;
        public int stateFrames;
        public Vector2 ground, vel, moveInput;
        public float height, heightVel;
        public int health, facing = 1;
        public int recoverable; // health spent on the special, won back by landing hits, lost when hit (Streets of Rage 4)
        public Move move;
        public bool whip; // enemy type: long reach, slow telegraphed wind-up
        public float mass = 1f; // heavier flies less far from the same blow and shoves others aside
        public bool armored;    // the boss: light blows hurt but don't stagger him
        public bool drunk;      // sways as he walks (every enemy's blow lands on whoever is in front, mates too)
        public float size = 1f; // drawn this much bigger (the boss); reach stays the same
        public bool faceMove = true; // walking turns us; enemies turn to the hero themselves
        public Fighter grabbed;
        public Prop held; // chair or bottle in hand
        public Lamp hangingOn;
        public bool headless, halved, armless; // gore done to us
        int bleed;      // frames left of bleeding from a wound
        int sinkFrames; // killed while buried: how far we've been hammered on through the floor
        bool sunk;      // ...all the way, gone
        int hangKick; // frames left of a kick from the lamp

        int flashFrames, invulnFrames;
        public int comboHits;        // hits taken in the current combo (since we last stood free)
        public bool lastStrikeHit;   // our last blow connected: the punch combo goes on only on hits
        bool bounced, strikePending, airKick;
        public bool punchAlt; // the other hand: the hero's 2nd punch of the combo (set by Player), enemies alternate
        bool splatted;     // hit the end wall on this flight already
        int wallBounce;    // frames left bouncing off the wall, open for one more hit
        int slipCooldown;  // frames before a puddle can take our feet again
        int stun;          // hurt lasts at least this long (doubled over after a low blow)
        public int burning; // frames left on fire
        public int dirtyCooldown; // frames before J+L works again
        bool caughtFromAnother; // set alight by a burning body: doesn't pass it on (no chain reactions through a crowd)
        int stumble;        // frames left lurching forward after a blow that hit only air
        int burnClock;      // frames since catching fire: a burn every BurnTick of them
        bool charred;       // burnt to death: drawn black
        Fighter elbowTarget; // the body an elbow drop is steering onto
        readonly System.Collections.Generic.List<Fighter> floorHits = new System.Collections.Generic.List<Fighter>(); // bodies on the floor this move already kicked
        public bool Girl => Art.IsGirl(look);
        SpriteRenderer body, shadow, whipLine;
        SpriteRenderer[] dizzy; // stars round the head while doubled over
        SpriteRenderer[] burnFx; // flames on the body while burning
        public float lift, tilt; // held up by the hair: drawn this much higher, tipped this many degrees
        Material normalMat;
        int landSquash, jumpStretch; // frames left of squashing on landing / stretching on take-off
        Art.Frames art;

        int Startup => move switch
        {
            Move.Kick => Feel.KickStartup, Move.BodyKick => Feel.BodyKickStartup, Move.Roundhouse => Feel.RoundhouseStartup, Move.Sweep => Feel.SweepStartup,
            Move.FlyingKick => Feel.FlyingKickStartup, Move.BicycleKick => Feel.BicycleKickStartup,
            Move.Pound => Feel.PoundStartup, Move.ElbowDrop => Feel.ElbowDropStartup, Move.Knee or Move.LowBlow => Feel.KneeStartup,
            Move.Throw => Feel.ThrowStartup, Move.HairSlam => Feel.HairSlamStartup, Move.Rip => Feel.RipStartup, Move.Whip => Feel.WhipStartup,
            Move.Swing => Feel.SwingStartup, Move.ThrowProp => Feel.PropThrowStartup, Move.Special => Feel.SpecialStartup,
            _ => team == 0 ? Feel.PunchStartup : Feel.EnemyPunchStartup,
        };
        int Active => move switch
        {
            Move.Kick => Feel.KickActive, Move.BodyKick => Feel.BodyKickActive, Move.Roundhouse => Feel.RoundhouseActive, Move.Sweep => Feel.SweepActive,
            Move.FlyingKick => Feel.FlyingKickActive, Move.BicycleKick => Feel.BicycleKickActive, Move.Pound => Feel.PoundActive,
            Move.Swing => Feel.SwingActive, Move.Whip => Feel.WhipActive, Move.Special => Feel.SpecialActive, _ => Feel.PunchActive,
        };
        int Recovery => move switch
        {
            Move.Smash => Feel.SmashRecovery, Move.Kick => Feel.KickRecovery,
            Move.BodyKick => Feel.BodyKickRecovery, Move.Roundhouse => Feel.RoundhouseRecovery, Move.Sweep => Feel.SweepRecovery,
            Move.FlyingKick => Feel.FlyingKickRecovery, Move.BicycleKick => Feel.BicycleKickRecovery, Move.Pound => Feel.PoundRecovery,
            Move.ElbowDrop => Feel.ElbowDropRecovery, Move.Knee or Move.LowBlow => Feel.KneeRecovery,
            Move.Throw or Move.Rip or Move.HairSlam => Feel.ThrowRecovery, Move.Whip => Feel.WhipRecovery,
            Move.Swing => Feel.SwingRecovery, Move.ThrowProp => Feel.PropThrowRecovery, Move.Special => Feel.SpecialRecovery,
            _ => Feel.PunchRecovery,
        };
        float Reach => move switch
        {
            Move.Smash => Feel.SmashReach, Move.Kick => Feel.KickReach, Move.Special => Feel.SpecialReach,
            Move.BodyKick => Feel.BodyKickReach, Move.Roundhouse => Feel.RoundhouseReach, Move.Sweep => Feel.SweepReach,
            Move.JumpKick => Feel.JumpKickReach, Move.FlyingKick => Feel.FlyingKickReach, Move.BicycleKick => Feel.BicycleKickReach, Move.Pound => Feel.PoundReach,
            Move.ElbowDrop => Feel.ElbowDropReach, Move.HangKick => Feel.HangKickReach,
            Move.Whip => Feel.WhipReach, Move.Swing when held != null => held.Reach, _ => Feel.PunchReach,
        };
        float Push => move switch
        {
            Move.Kick => Feel.KickPush, Move.BodyKick => Feel.BodyKickPush, Move.Whip => Feel.WhipPush,
            Move.BicycleKick => Feel.BicycleKickSpeed * 1.6f, // carries them along in front of the kicks
            _ => Feel.JabPush,
        };
        int Damage => move switch
        {
            Move.Smash => Feel.SmashDamage, Move.Kick => Feel.KickDamage,
            Move.BodyKick => Feel.BodyKickDamage, Move.Roundhouse => Feel.RoundhouseDamage, Move.Sweep => Feel.SweepDamage, Move.JumpKick or Move.HangKick => Feel.JumpKickDamage,
            Move.FlyingKick => Feel.FlyingKickDamage, Move.BicycleKick => Feel.BicycleKickDamage, Move.Pound => Feel.PoundDamage,
            Move.ElbowDrop => Feel.ElbowDropDamage, Move.Whip => Feel.WhipDamage, Move.Special => Feel.SpecialDamage, Move.Swing when held != null => held.Damage,
            _ => team == 0 ? Feel.PunchDamage : armored ? Feel.EnemyPunchDamage * 2 : Feel.EnemyPunchDamage,
        };
        bool Launches => move is Move.Smash or Move.Roundhouse or Move.JumpKick or Move.FlyingKick or Move.HangKick or Move.Special
            || move == Move.BicycleKick && stateFrames >= Startup + Active - Feel.BicycleHitGap // only its last kick
            || move == Move.Swing && held != null && held.kind is Prop.Kind.Chair or Prop.Kind.Lamp;
        bool WindingUp => state == State.Attack && stateFrames < Startup;

        public bool CanAct => state == State.Idle || state == State.Walk || state == State.Run;
        public bool Alive => health > 0;
        public bool Solid => Alive && height <= 0f && state != State.Launched && state != State.Down && state != State.Sit && state != State.Hang;
        public bool CanBeHit => Alive && invulnFrames == 0
            && (state != State.Launched || wallBounce > 0) && state != State.Down && state != State.GetUp;

        public void Init(int team, string look, Vector2 start)
        {
            this.team = team;
            this.look = look;
            ground = start;
            health = maxHealth;
            art = Art.For(look);
            if (look != Art.Hero && !Girl) size = Feel.GuyScale; // the CC0 guys are drawn smaller than the toad
            shadow = MakeSprite("Shadow", Art.Shadow);
            shadow.sortingOrder = -10000; // under every body
            body = MakeSprite("Body", art.Idle[0]);
            normalMat = body.sharedMaterial;
            whipLine = MakeSprite("Whip", Art.Whip);
            dizzy = new[] { MakeSprite("Dizzy", Art.Spark[0]), MakeSprite("Dizzy", Art.Spark[0]) };
            burnFx = new[] { MakeSprite("Burn", Art.Flame[0]), MakeSprite("Burn", Art.Flame[1]), MakeSprite("Burn", Art.Flame[2]) };
        }

        SpriteRenderer MakeSprite(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return sr;
        }

        // ground attacks; knee and throw only while holding someone
        public void Attack(Move m)
        {
            bool holding = state == State.Grab && m is Move.Knee or Move.Throw or Move.Rip or Move.LowBlow or Move.HairSlam;
            if (!CanAct && !holding) return;
            move = m;
            if (team != 0) punchAlt = !punchAlt;
            if (m != Move.BicycleKick && m != Move.FlyingKick) vel = Vector2.zero; // running kicks keep the momentum
            if (m == Move.ElbowDrop)
            {
                heightVel = Feel.ElbowDropHop;
                elbowTarget = DownedEnemyInReach(Feel.ElbowDropHoming, Feel.ElbowDropHomingDepth);
            }
            floorHits.Clear();
            Enter(State.Attack);
            Sfx.Play("whoosh", team == 0 ? 0.5f : 0.3f);
        }

        // J+L: grabs whoever is right in front (or the one already held) and does the dirty one:
        // a low blow on a guy, a hair slam on a girl. Not the boss: he doesn't let anyone that close.
        public bool DirtyMove()
        {
            Fighter target = state == State.Grab && grabbed != null && grabbed.state == State.Held ? grabbed : null;
            bool starting = CanAct || state == State.Attack && stateFrames <= Feel.SpecialLeniency; // J or L just pressed: cancel it
            if (target == null && starting)
                foreach (var t in Game.Fighters)
                    if (t.team != team && !t.armored && t.state != State.Sit && t.state != State.Held && InReach(t, Feel.PunchReach))
                    {
                        target = t;
                        break;
                    }
            if (target == null || dirtyCooldown > 0) return false;
            dirtyCooldown = Feel.DirtyCooldown;
            strikePending = false; // the cancelled jab or kick never lands
            if (state != State.Grab) Grab(target);
            Attack(target.Girl ? Move.HairSlam : Move.LowBlow);
            return true;
        }

        // desperation move (J+K): a spinning kick all round that can't be interrupted, even from hitstun;
        // costs health, which comes back by landing hits before getting hit again
        public bool Special()
        {
            bool from = CanAct || state == State.Hurt || state == State.Jump || state == State.Attack && move == Move.Punch;
            if (!from || health <= Feel.SpecialCost) return false;
            if (held != null) { held.Drop(); held = null; }
            health -= Feel.SpecialCost;
            recoverable += Feel.SpecialCost;
            move = Move.Special;
            vel = Vector2.zero;
            height = heightVel = 0f; // on the ground: J+K a moment apart may have started a jump first
            airKick = false;
            Enter(State.Attack);
            invulnFrames = Feel.SpecialStartup + Feel.SpecialActive;
            Sfx.Play("whoosh", 0.8f);
            return true;
        }

        public bool IsDownedEnemyInReach(float reach, float depth = Feel.DepthBand) => DownedEnemyInReach(reach, depth) != null;

        // the nearest living enemy lying on the floor ahead
        Fighter DownedEnemyInReach(float reach, float depth)
        {
            Fighter best = null;
            foreach (var t in Game.Fighters)
            {
                float dx = (t.ground.x - ground.x) * facing;
                if (t.team != team && t.Alive && t.state == State.Down && dx > -4f && dx < reach
                    && Mathf.Abs(t.ground.y - ground.y) < depth
                    && (best == null || dx < (best.ground.x - ground.x) * facing))
                    best = t;
            }
            return best;
        }

        public void StartRun(int dir)
        {
            if (!CanAct || held != null) return; // no running with a chair in your hands
            facing = dir;
            Enter(State.Run);
        }

        // a patron sitting at a table (or up on a bar stool: seatHeight), facing it, until the bar fight starts
        public void Sit(int faceDir, float seatHeight = 0f)
        {
            facing = faceDir;
            vel = Vector2.zero;
            height = seatHeight;
            heightVel = 0f;
            Enter(State.Sit);
        }

        public void StandUp()
        {
            if (state == State.Sit) Enter(State.Idle);
        }

        // jumped into a lamp: hang from it by the hands
        public void Hang(Lamp lamp)
        {
            hangingOn = lamp;
            lamp.hanger = this;
            vel = Vector2.zero;
            heightVel = 0f;
            ground.x = lamp.SwingX; // hands on the rim right away
            height = lamp.bottom - Feel.HangReach;
            hangKick = 0;
            Enter(State.Hang);
        }

        public void HangKick()
        {
            if (state != State.Hang || hangKick > 0) return;
            hangKick = Feel.HangKickFrames;
            Sfx.Play("whoosh", 0.5f);
        }

        // let go: flung off in the direction of the swing
        public void LetGo()
        {
            if (hangingOn == null) return;
            vel = new Vector2(Mathf.Clamp(hangingOn.angVel * Lamp.Length, -5f, 5f), 0f);
            hangingOn.hanger = null;
            hangingOn = null;
            heightVel = 0f; // falling, not rising: a rising jump would catch the same lamp again next frame
            airKick = false;
            Enter(State.Jump);
        }

        // pull the lamp off its cord and drop down holding it
        public void RipLamp()
        {
            var lamp = hangingOn;
            if (lamp == null) return;
            LetGo();
            vel = Vector2.zero;
            lamp.Rip();
            Sfx.Play("glass", 0.5f);
            var shade = Game.SpawnProp(Prop.Kind.Lamp, ground);
            held?.Drop(); // whatever we carried up falls
            held = shade;
            shade.holder = this;
        }

        public void Jump()
        {
            if (!CanAct) return;
            heightVel = Feel.JumpVel;
            airKick = false;
            jumpStretch = Feel.SquashFrames;
            Enter(State.Jump);
        }

        public void JumpKick()
        {
            if (state != State.Jump || airKick) return;
            airKick = true;
            move = Move.JumpKick;
        }

        public void PickUp(Prop p)
        {
            if (!CanAct || held != null || !p.Pickable) return;
            held = p;
            p.holder = this;
            p.resting = false;
        }

        // a free prop on the floor right in front or underneath
        public Prop PropInReach()
        {
            foreach (var p in Game.Props)
                if (p.Pickable && Mathf.Abs(p.ground.x - ground.x) < Feel.PickupRange
                    && Mathf.Abs(p.ground.y - ground.y) < Feel.DepthBand)
                    return p;
            return null;
        }

        public bool EnemyInReach(float reach)
        {
            foreach (var t in Game.Fighters)
                if (t.team != team && InReach(t, reach)) return true;
            return false;
        }

        public void Grab(Fighter t)
        {
            grabbed = t;
            facing = t.ground.x > ground.x ? 1 : -1;
            vel = Vector2.zero;
            Enter(State.Grab);
            t.Enter(State.Held);
            t.vel = Vector2.zero;
            t.facing = -facing;
            HoldVictim();
        }

        public void TakeHit(int damage, int dir, bool launch, float push = Feel.JabPush)
        {
            if (!CanBeHit) return;
            if (armored && !launch && health > damage && comboHits < Feel.JuggleCap)
            {
                Damaged(damage); // shrugs it off: no stagger, his own blow still comes
                return;
            }
            strikePending = false; // hit first: our own blow never lands
            recoverable = 0;
            bool countered = move == Move.Whip && WindingUp; // punished mid wind-up: knocked flying
            Release();
            if (hangingOn != null) { hangingOn.hanger = null; hangingOn = null; } // knocked off the lamp
            if (held != null) { held.Drop(); held = null; }
            Damaged(damage);
            facing = -dir;
            // a long enough combo knocks them down whatever the last hit was: no endless juggles
            if (launch || countered || !Alive || height > 0f || state == State.Buried || comboHits >= Feel.JuggleCap)
            {
                Enter(State.Launched);
                bounced = splatted = false;
                wallBounce = 0;
                vel = new Vector2(dir * Feel.LaunchX / mass, 0f);
                heightVel = Feel.LaunchUp / Mathf.Sqrt(mass);
            }
            else
            {
                stun = state == State.Hurt ? Mathf.Max(0, stun - stateFrames) : 0; // still doubled over: stays so
                Enter(State.Hurt);
                vel = new Vector2(dir * push / mass, 0f);
                if (team == 0) invulnFrames = Feel.HeroHurtInvuln;
            }
        }

        // slammed into the end wall: bounces back off it, open for one more hit on the way
        void WallSplat()
        {
            splatted = true;
            vel.x *= -0.5f;
            heightVel = Mathf.Max(heightVel, 2f);
            Damaged(Feel.WallSplatDamage);
            if (comboHits < Feel.JuggleCap) wallBounce = Feel.WallBounceFrames;
            Fx.Dust(ground);
            Sfx.Play("thud");
            Game.Freeze(Feel.Hitstop, Feel.SmashShake);
        }

        // set alight (a molotov's flames, or one in the face); standing in the fire keeps it going
        public void Ignite(bool fromAnother = false)
        {
            if (!Alive || state == State.Sit) return;
            if (burning == 0) { Sfx.Play("fire", 0.6f); burnClock = 0; caughtFromAnother = fromAnother; }
            else if (!fromAnother) caughtFromAnother = false; // the real flames again: can spread it now
            burning = Feel.BurnFrames;
        }

        // the fire eats at us; burnt to death, we drop where we stand, black
        void Burn()
        {
            health -= Feel.BurnDamage;
            flashFrames = 4;
            if (team != 0) { Game.LastHit = this; Game.Scored(); }
            if (Alive) return;
            charred = true;
            burning = 0;
            Release();
            if (held != null) { held.Drop(); held = null; }
            if (state is State.Launched or State.Down or State.Dead or State.Buried or State.Headless) return; // already falling or lying
            Enter(State.Launched);
            bounced = true; // straight down, no bounce
            vel = Vector2.zero;
            heightVel = 1.5f;
            Sfx.Play("thud", 0.8f);
        }

        // kicked while lying on the floor: bounced along it, then down again
        void KickedOnTheFloor(int dir)
        {
            Enter(State.Launched);
            bounced = true; // lands flat, no second bounce
            vel = new Vector2(dir * 1.5f, 0f);
            heightVel = 2f;
            if (!Alive) Fx.Blood(ground, 4f, 20, dir, 3f);
        }

        // swept: feet taken out, flat on the floor where we stand (the boss is too heavy to sweep)
        public void Trip(int damage, int dir)
        {
            if (!CanBeHit) return;
            if (armored && health > damage) { Damaged(damage); return; }
            strikePending = false;
            Release();
            if (held != null) { held.Drop(); held = null; }
            Damaged(damage);
            facing = -dir;
            Enter(State.Launched);
            bounced = true; // flat on the floor, no bounce
            splatted = false;
            wallBounce = 0;
            vel = new Vector2(dir * 1f, 0f);
            heightVel = 2.5f;
        }

        // feet out from under us on a spilled drink: down without a blow
        public void Slip()
        {
            if (slipCooldown > 0 || !Alive || invulnFrames > 0) return;
            slipCooldown = Feel.SlipCooldown;
            Release();
            if (held != null) { held.Drop(); held = null; }
            Enter(State.Launched);
            bounced = true; // flat on the floor, no bounce
            splatted = false;
            wallBounce = 0;
            vel = new Vector2(facing * 1.5f, 0f);
            heightVel = 2.5f;
            Sfx.Play("whoosh", 0.6f);
        }

        // hammer stomp: driven into the ground up to the chest; a finishing blow knocks them flying instead
        public void Bury(int damage, int dir)
        {
            if (!CanBeHit) return;
            if (state == State.Buried) { TakeHit(damage, dir, true); return; } // once per bury
            Release();
            Damaged(damage);
            vel = Vector2.zero;
            Enter(State.Buried);
            if (Alive) return;
            Fx.Blood(ground, 4f, 30, 0f, 4f); // a killing stomp: hammered on, right through the floor
            Sfx.Play("splat");
            Sfx.Play("crunch", 0.8f);
        }

        // ---- gore: what the hero's killing blows do ----
        Sprite[] Sinking => headless ? art.HeadlessSinking : art.Sinking;
        float Neck => height + Feel.NeckHeight - (state == State.Buried ? Feel.BurySink : 0f);

        // the head comes off and flies; the body stands a moment with blood fountaining from the neck, then drops
        public void Decapitate(int dir, float speed, Fighter kicker = null)
        {
            headless = true;
            Prop.Fling(Prop.Kind.Head, art.Head, ground, Neck + 7f, new Vector2(dir * speed, 0f), Feel.HeadFlyUp, facing < 0, kicker);
            Fx.Blood(ground, Neck, 25, dir, 3f);
            Sfx.Play("splat");
            Sfx.Play("crunch", 0.8f);
            if (state == State.Buried) return; // stuck in the floor: sinks on through it, headless
            vel = Vector2.zero;
            heightVel = 0f;
            Enter(State.Headless);
        }

        // a headbutt in the face: the head bursts
        public void BurstHead()
        {
            headless = true;
            Fx.Blood(ground, Neck + 6f, 45, 0f, 4.5f);
            for (int i = 0; i < 8; i++) // bits of skull
                Shard.Spawn(new Vector3(ground.x, ground.y + Neck + 6f, 0f), i % 2 == 0 ? new Color32(0xf0, 0xe8, 0xd0, 255) : new Color32(0xd0, 0x70, 0x70, 255));
            Sfx.Play("splat");
            Sfx.Play("crunch");
            vel = Vector2.zero;
            heightVel = 0f;
            Enter(State.Headless);
        }

        // an elbow drop finishing a body on the floor: torn in two, one half flies off
        public void TearInHalf(int dir)
        {
            halved = true;
            Prop.Fling(Prop.Kind.Gib, art.HalfFlies, ground, 4f, new Vector2(dir * 3f, 0f), 4f, facing < 0);
            Fx.Blood(ground, 6f, 40, dir, 3.5f);
            bleed = Feel.BleedFrames;
            Sfx.Play("splat");
            Sfx.Play("crunch");
        }

        // pulled off in a hold: the wound bleeds, the arm is a club now
        public Prop RipArm(int dir)
        {
            armless = true;
            bleed = Feel.BleedFrames;
            Fx.Blood(ground, height + Feel.ShoulderHeight, 30, dir, 3.5f);
            Sfx.Play("crunch");
            Sfx.Play("splat");
            return Prop.Fling(Prop.Kind.Arm, null, ground, height + Feel.ShoulderHeight, Vector2.zero, 0f, facing > 0);
        }

        // every blow lands through here: from the ComboScaleFrom-th hit of a combo on, each does 10% less (down to half)
        void Damaged(int damage)
        {
            comboHits++;
            float scale = Mathf.Clamp(1f - 0.1f * (comboHits - Feel.ComboScaleFrom + 1), Feel.ComboScaleMin, 1f);
            int dealt = Mathf.Max(1, Mathf.RoundToInt(damage * scale));
            health -= dealt;
            flashFrames = 8;
            Fx.Spark(new Vector2(ground.x, ground.y + height + 34f), dealt >= Feel.SmashDamage);
            Fx.Blood(ground, height + 38f, dealt / 4, 0f, 1.5f); // every blow draws blood, harder ones more
            if (team != 0) { Game.LastHit = this; Game.Scored(); } // its health bar shows in the HUD
            else Game.HeroCombo = 0;
        }

        public bool InReach(Fighter t, float reach, float heightBand = Feel.HeightBand, bool allRound = false)
        {
            float dx = (t.ground.x - ground.x) * facing;
            if (allRound) dx = Mathf.Abs(dx);
            return t.CanBeHit && dx > -4f && dx < reach
                && Mathf.Abs(t.ground.y - ground.y) < Feel.DepthBand
                && Mathf.Abs(t.height - height) < heightBand;
        }

        // the lash sheet: frames 0-5 wind up, 6-7 the crack, 8-14 the whip coming back
        int LashFrame()
        {
            if (state != State.Attack) return 0;
            if (stateFrames < Startup) return stateFrames * 6 / Startup;
            if (stateFrames < Startup + Feel.WhipActive) return 6 + (stateFrames - Startup) * 2 / Feel.WhipActive;
            return Mathf.Min(14, 8 + (stateFrames - Startup - Feel.WhipActive) * 7 / Feel.WhipRecovery);
        }

        public void Step()
        {
            Think?.Invoke();
            Tick();
        }

        void Tick()
        {
            stateFrames++;
            if (state != State.Held && state != State.Launched) lift = tilt = 0f; // let go by the hair, or landed
            if (flashFrames > 0) flashFrames--;
            if (landSquash > 0) landSquash--;
            if (bleed > 0 && --bleed % 5 == 0) Fx.Blood(ground, state is State.Down or State.Dead ? 4f : height + Feel.ShoulderHeight, 1, 0f, 0.3f);
            if (jumpStretch > 0) jumpStretch--;
            if (invulnFrames > 0) invulnFrames--;
            if (dirtyCooldown > 0) dirtyCooldown--;

            bool landed = ApplyPhysics();
            bool landedFromJump = landed && state == State.Jump;
            switch (state)
            {
                case State.Idle:
                case State.Walk:
                    comboHits = 0;
                    float carry = held != null ? Feel.CarrySpeed : 1f; // props are heavy
                    vel = new Vector2(moveInput.x * walkX, moveInput.y * walkDepth) * carry;
                    if (faceMove && moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
                    state = vel == Vector2.zero ? State.Idle : State.Walk;
                    break;
                case State.Run:
                    // keeps running while the direction is held; letting go or turning stops it
                    vel = new Vector2(facing * Feel.RunX, moveInput.y * walkDepth);
                    if (moveInput.x * facing <= 0f) Enter(State.Idle);
                    if (stateFrames % 10 == 0) Fx.Dust(ground - new Vector2(facing * 8f, 0f)); // kicked up at the heels
                    break;
                case State.Jump:
                    if (airKick) strikePending = true; // hits everyone it passes through
                    if (team == 0 && heightVel > 0f && !airKick && LampToGrab() is Lamp lamp) Hang(lamp);
                    if (landed) { Enter(State.Idle); landSquash = Feel.SquashFrames; Fx.Dust(ground); }
                    break;
                case State.Attack:
                    if (grabbed != null) HoldVictim();
                    if (grabbed != null && move == Move.HairSlam && WindingUp) // up by the hair, tipping over
                    {
                        float t = (float)stateFrames / Startup;
                        grabbed.lift = Mathf.Sin(t * Mathf.PI / 2f) * Feel.HairSlamLift;
                        grabbed.tilt = -60f * t * facing;
                    }
                    if (stateFrames == Startup) strikePending = true;
                    if (move == Move.ElbowDrop && elbowTarget != null && WindingUp)
                    {
                        // steers through the air to land elbow-first right on the body, which stays down until it lands
                        if (elbowTarget.state == State.Down) elbowTarget.stateFrames = Mathf.Min(elbowTarget.stateFrames, Feel.DownFrames - 1);
                        var aim = elbowTarget.ground - new Vector2(facing * 12f, 0f);
                        vel = (aim - ground) / Mathf.Max(1, Startup - stateFrames);
                    }
                    if (move == Move.ElbowDrop && stateFrames == Startup) vel = Vector2.zero;
                    if (move == Move.FlyingKick && stateFrames >= Startup && stateFrames < Startup + Active)
                    {
                        // flies straight across the screen, feet first, just above the ground
                        vel = new Vector2(facing * Feel.FlyingKickSpeed, 0f);
                        height = Feel.FlyingKickHeight;
                        heightVel = 0f;
                        strikePending = true;
                        if (stateFrames % Feel.GhostGap == 0) Fx.Ghost(body); // a trail behind the kick
                    }
                    if (move == Move.BicycleKick && stateFrames >= Startup && stateFrames < Startup + Active)
                    {
                        // flies forward low, kicking left-right-left: a separate hit every BicycleHitGap frames
                        vel = new Vector2(facing * Feel.BicycleKickSpeed, 0f);
                        height = Feel.BicycleKickHeight;
                        heightVel = 0f;
                        if (stateFrames % Feel.GhostGap == 0) Fx.Ghost(body);
                        if ((stateFrames - Startup) % Feel.BicycleHitGap == 0) strikePending = true;
                    }
                    if (move == Move.Special && stateFrames >= Startup && stateFrames < Startup + Active)
                    {
                        strikePending = true; // hits everyone around, each once (they fly off untouchable)
                        if (stateFrames % Feel.GhostGap == 0) Fx.Ghost(body);
                    }
                    if (stateFrames >= Startup + Active + Recovery)
                        Enter(grabbed != null && grabbed.state == State.Held ? State.Grab : State.Idle);
                    break;
                case State.Grab:
                    if (grabbed == null || grabbed.state != State.Held || stateFrames >= Feel.GrabFrames) Release();
                    else HoldVictim();
                    break;
                case State.Held:
                    vel = Vector2.zero; // the holder positions us
                    break;
                case State.Sit:
                    vel = Vector2.zero;
                    break;
                case State.Hang:
                    // swinging from the lamp: pump with left/right, kick with J/L
                    hangingOn.Pump(moveInput.x);
                    ground.x = hangingOn.SwingX;
                    height = hangingOn.bottom - Feel.HangReach;
                    if (moveInput.x != 0f) facing = moveInput.x > 0f ? 1 : -1;
                    if (hangKick > 0 && --hangKick == Feel.HangKickFrames - Feel.HangKickStartup)
                    {
                        move = Move.HangKick;
                        strikePending = true;
                    }
                    break;
                case State.Buried:
                    vel = Vector2.zero;
                    if (Alive && stateFrames >= Feel.BuriedFrames) Enter(State.GetUp); // pops out
                    if (!Alive && ++sinkFrames / 2 + Art.BuriedFrame >= Sinking.Length) { sunk = true; Enter(State.Dead); }
                    break;
                case State.Headless:
                    // stands a moment, a fountain from the neck, then drops
                    if (stateFrames % 2 == 0 && stateFrames < Feel.HeadlessFrames - 10) Fx.Blood(ground, Neck, 2, 0f, 6f);
                    if (stateFrames >= Feel.HeadlessFrames) { Enter(State.Down); Sfx.Play("thud", 0.8f); Fx.Dust(ground); bleed = Feel.BleedFrames; }
                    break;
                case State.Hurt:
                    if (stateFrames >= Mathf.Max(Feel.Hitstun, stun)) { stun = 0; Enter(State.Idle); }
                    break;
                case State.Launched:
                    HitBodiesInPath();
                    if (wallBounce > 0) wallBounce--;
                    else if (!splatted && Mathf.Abs(vel.x) >= Feel.WallSplatSpeed
                             && (ground.x <= Game.MinX && vel.x < 0f || ground.x >= Game.MaxX && vel.x > 0f)) WallSplat();
                    if (landed && bounced) { Enter(State.Down); Sfx.Play("thud", 0.8f); Fx.Dust(ground); }
                    else if (landed) { bounced = true; heightVel = Feel.BounceUp; vel.x *= 0.5f; Fx.Dust(ground); }
                    break;
                case State.Down:
                    if (stateFrames >= Feel.DownFrames) Enter(Alive ? State.GetUp : State.Dead);
                    break;
                case State.GetUp:
                    if (stateFrames >= Feel.GetUpFrames) { Enter(State.Idle); if (team == 0) invulnFrames = Feel.GetUpInvuln; }
                    break;
            }

            // fire: standing in the flames sets us alight; burning hurts every BurnTick frames
            if (Alive && height <= 0f && state != State.Sit && Fire.Under(ground)) Ignite();
            if (burning > 0)
            {
                burning--;
                if (++burnClock % Feel.BurnTick == 0 && Alive) Burn();
                // flailing about, we set alight whoever we touch, and they won't thank us for it; only one step from the
                // flames themselves, and never Brick (he burns only from the floor or his own bottles)
                if (burnClock % 10 == 0 && !caughtFromAnother)
                    foreach (var o in Game.Fighters)
                        if (o != this && o.team != 0 && o.burning == 0 && o.Alive && o.state != State.Sit
                            && Mathf.Abs(o.ground.x - ground.x) < Feel.BodyGap + 4f && Mathf.Abs(o.ground.y - ground.y) < Feel.DepthBand
                            && Mathf.Abs(o.height - height) < Feel.HeightBand)
                        {
                            o.Ignite(fromAnother: true);
                            if (o.team == team) o.GetComponent<Enemy>()?.HitByMate(this);
                        }
            }

            // a spilled drink: running, landing or being knocked across it takes your feet; walking carefully is safe,
            // except for the patrons, who are past walking carefully
            if (stumble > 0) stumble--;
            if (slipCooldown > 0) slipCooldown--;
            else if (Puddle.Under(ground) && (state is State.Run or State.Hurt || landedFromJump
                     || team != 0 && state == State.Walk && Random.value < Feel.EnemySlipChance)) Slip();
        }

        Lamp LampToGrab()
        {
            foreach (var l in Game.Lamps)
                if (l.hanger == null && Mathf.Abs(l.SwingX - ground.x) < Feel.LampGrabX
                    && Mathf.Abs(l.depth - ground.y) < Feel.LampGrabDepth
                    && height >= l.bottom - Feel.HangReach - 6f)
                    return l;
            return null;
        }

        void HoldVictim() => grabbed.ground = new Vector2(ground.x + facing * Feel.BodyGap, ground.y);

        // lets go of the held fighter (if any); they stagger free
        void Release()
        {
            if (grabbed != null && grabbed.state == State.Held) { grabbed.Enter(State.Hurt); grabbed.invulnFrames = Feel.BreakFreeInvuln; }
            grabbed = null;
            if (state == State.Grab) Enter(State.Idle);
        }

        // returns true on the frame the body touches the ground
        bool ApplyPhysics()
        {
            if (state == State.Sit || state == State.Hang) return false; // held up by a seat, or hanging from a lamp
            bool grounded = height <= 0f && heightVel <= 0f;
            if (grounded && !CanAct) vel.x = Mathf.MoveTowards(vel.x, 0f, Feel.Friction);
            ground += vel;
            ground.x = Mathf.Clamp(ground.x, Game.MinX, Game.MaxX);
            ground.y = Mathf.Clamp(ground.y, Game.MinDepth, Game.MaxDepth);
            if (grounded) return false;

            heightVel -= Feel.Gravity;
            height += heightVel;
            if (height > 0f) return false;
            height = 0f;
            heightVel = 0f;
            return true;
        }

        // called by Game after every fighter has stepped; the hero's first, so the hero wins a same-frame tie
        public void ResolveStrike()
        {
            if (!strikePending) return;
            strikePending = false;
            if (move is Move.Knee or Move.Throw or Move.Rip or Move.LowBlow or Move.HairSlam) StrikeHeld();
            else if (move == Move.ThrowProp) { held?.Throw(this); held = null; }
            else Strike();
        }

        void Strike()
        {
            if (move == Move.ElbowDrop) { StrikeDowned(); return; }
            bool hit = false, solidHit = false, finished = false;
            int landed = 0;
            bool allRound = move == Move.Special;
            float heightBand = move is Move.JumpKick or Move.FlyingKick or Move.BicycleKick or Move.HangKick ? Feel.JumpKickHeight : Feel.HeightBand;
            foreach (var t in Game.Fighters)
                if ((t.team != team || team != 0 && t != this) && t.state != State.Held && InReach(t, Reach, heightBand, allRound)) // enemies hit mates in the way
                {
                    if (move == Move.Pound) t.Bury(Damage, facing);
                    else if (move is Move.Kick or Move.BodyKick or Move.Roundhouse && team == 0 && t.state == State.Buried) t.Punt(facing, this);
                    else if (move == Move.Sweep) t.Trip(Damage, facing);
                    else t.TakeHit(Damage, allRound ? (t.ground.x >= ground.x ? 1 : -1) : facing, Launches, Push);
                    if (t.team == team) t.GetComponent<Enemy>()?.HitByMate(this); // may start a fight among themselves
                    if (team == 0 && !t.Alive) { Finish(t); Gore(t); finished = true; }
                    hit = solidHit = true;
                    landed++;
                }
            if (team != 0 && !hit && move is Move.Punch or Move.Whip) // swung at air, the hero stepped aside: it carries on into a mate behind
            {
                foreach (var t in Game.Fighters)
                    if (t != this && t.team == team && t.state != State.Held && InReach(t, Reach + Feel.WhiffCarry, heightBand))
                    {
                        t.TakeHit(Damage, facing, Launches, Push);
                        t.GetComponent<Enemy>()?.HitByMate(this);
                        hit = solidHit = true;
                        landed++;
                        break;
                    }
                vel = new Vector2(facing * 2f, 0f); // overcommitted: stumbles on after the miss
                stumble = 14;
            }
            if (move is Move.FlyingKick or Move.BicycleKick) // flying low over bodies on the floor still kicks them, once each
                foreach (var t in Game.Fighters)
                {
                    float dx = (t.ground.x - ground.x) * facing;
                    if (t.team == team || !t.Alive || t.state != State.Down || floorHits.Contains(t) || dx <= -4f || dx >= Reach
                        || Mathf.Abs(t.ground.y - ground.y) >= Feel.DepthBand) continue;
                    floorHits.Add(t);
                    t.Damaged(Damage);
                    t.KickedOnTheFloor(facing);
                    hit = solidHit = true;
                    landed++;
                }
            // tables tip over and slide, bottles get swept off the counter (backwards: a bottle may break)
            for (int i = Game.Props.Count - 1; i >= 0; i--)
            {
                var p = Game.Props[i];
                float ahead = (p.ground.x - ground.x) * facing;
                if (p.holder == null && p.thrower == null && ahead > -4f && ahead < Reach
                    && Mathf.Abs(p.ground.y - ground.y) < Feel.DepthBand)
                {
                    bool table = p.Heavy; // tables and pool tables are solid hits, loose bottles are not
                    if (p.Struck(facing, this)) { hit = true; solidHit |= table; }
                }
            }
            lastStrikeHit = hit;
            if (landed > 0 && recoverable > 0 && move != Move.Special) // hits win back the special's cost
            {
                int back = Mathf.Min(recoverable, landed * Feel.RecoverPerHit);
                health += back;
                recoverable -= back;
            }
            if (solidHit && move == Move.Swing) held?.UseUp(); // wears out on people and tables, not on loose bottles
            if (hit) Sfx.Play(finished || Launches ? "hit_heavy" : "hit");
            if (!hit || finished) return; // a finisher already set its own freeze
            bool big = Launches || move == Move.Pound;
            Game.Freeze(big ? Feel.SmashHitstop : Feel.Hitstop, big ? Feel.SmashShake : 0);
        }

        // a buried enemy kicked in the face: the head sails off like a football, hitting whoever is in the way
        public void Punt(int dir, Fighter kicker)
        {
            if (!CanBeHit) return;
            Damaged(health);
            health = Mathf.Min(health, 0); // no combo scaling saves you from this
            Decapitate(dir, Feel.PuntSpeed * 1.5f, kicker);
        }

        // what a killing blow from the hero does to the body
        void Gore(Fighter t)
        {
            if (t.headless || t.state == State.Buried) return; // punted, or hammered through the floor
            bool heavy = move is Move.Kick or Move.BodyKick or Move.Roundhouse or Move.Smash or Move.JumpKick or Move.FlyingKick or Move.HangKick
                || move == Move.Swing && held != null && held.kind != Prop.Kind.Bottle;
            if (heavy) { t.Decapitate(facing, Feel.HeadFlySpeed); return; }
            Fx.Blood(t.ground, t.height + 36f, 20, facing, 3f); // flies off bleeding
            t.bleed = Feel.BleedFrames;
            Sfx.Play("splat", 0.7f);
        }

        // the player's last hit on an enemy: long freeze, flash, shake, sent flying far
        void Finish(Fighter t)
        {
            t.vel *= Feel.FinisherLaunch;
            t.heightVel *= Feel.FinisherLaunch;
            Game.Freeze(Feel.FinisherHitstop, Feel.FinisherShake);
            Game.Flash(Feel.FinisherFlash);
            if (!Game.Fighters.Exists(o => o.team != team && o.Alive && o.state != State.Sit)) Game.SlowMo(Feel.LastKillSlowMo); // the last one: slow motion
        }

        // wrestling elbow drop: lands elbow-first on enemies lying on the ground (doesn't keep them down longer);
        // when it knocks one out it is the finisher (long freeze, flash, shake)
        void StrikeDowned()
        {
            bool hit = false, finished = false;
            foreach (var t in Game.Fighters)
            {
                float dx = (t.ground.x - ground.x) * facing;
                if (t.team == team || !t.Alive || t.state != State.Down || dx <= -4f || dx >= Reach
                    || Mathf.Abs(t.ground.y - ground.y) >= Feel.DepthBand) continue;
                t.Damaged(Damage);
                hit = true;
                if (!t.Alive) { finished = true; t.TearInHalf(facing); }
            }
            if (finished)
            {
                Game.Freeze(Feel.FinisherHitstop, Feel.FinisherShake);
                Game.Flash(Feel.FinisherFlash);
                if (!Game.Fighters.Exists(o => o.team != team && o.Alive && o.state != State.Sit)) Game.SlowMo(Feel.LastKillSlowMo);
            }
            else if (hit) Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
            if (hit) Sfx.Play("hit_heavy");
        }

        // knee: damage without letting go (the last point knocks them flying); throw: over the shoulder
        void StrikeHeld()
        {
            if (grabbed == null || grabbed.state != State.Held) return;
            var g = grabbed;
            if (move == Move.Knee && g.health > Feel.KneeDamage)
            {
                g.Damaged(Feel.KneeDamage);
                Sfx.Play("hit");
                Game.Freeze(Feel.Hitstop, 0);
                return;
            }
            grabbed = null;
            g.Enter(State.Hurt); // makes them hittable again
            if (move == Move.LowBlow) // let go, and he doubles over: open to anything for a while
            {
                g.TakeHit(Feel.LowBlowDamage, facing, false, 0f);
                Fx.Spark(new Vector2(g.ground.x, g.ground.y + 24f), true);
                if (g.state == State.Hurt) g.stun = Feel.LowBlowStun;
                Sfx.Play("hit_heavy");
                Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
                return;
            }
            if (move == Move.HairSlam) // over by the hair and face-first into the floor, right in front: elbow-drop range
            {
                float lifted = g.lift;
                g.TakeHit(Feel.HairSlamDamage, facing, true);
                g.vel = new Vector2(facing * 1f, 0f);
                g.height = lifted;   // from up there, straight down face-first
                g.heightVel = -2.5f;
                g.lift = 0f;
                g.tilt = -90f * facing;
                g.bounced = true; // flat on the floor, no bounce
                if (!g.Alive) Fx.Blood(g.ground, 4f, 30, 0f, 3f);
                Sfx.Play("hit_heavy");
                Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
                return;
            }
            if (move == Move.Rip)
            {
                g.TakeHit(Feel.ArmRipDamage, facing, true);
                if (held == null) { held = g.RipArm(facing); held.holder = this; }
                Game.Freeze(Feel.FinisherHitstop, Feel.FinisherShake);
                return;
            }
            bool thrown = move == Move.Throw;
            g.TakeHit(thrown ? Feel.ThrowDamage : Feel.KneeDamage, thrown ? -facing : facing, true);
            if (team == 0 && !g.Alive && !thrown) g.BurstHead(); // the last headbutt
            Sfx.Play("hit_heavy");
            Game.Freeze(Feel.SmashHitstop, Feel.SmashShake);
        }

        // a body flying fast knocks down its own teammates
        void HitBodiesInPath()
        {
            if (Mathf.Abs(vel.x) < Feel.BodyHitSpeed) return;
            int dir = vel.x > 0f ? 1 : -1;
            foreach (var o in Game.Fighters)
                if (o != this && o.team == team && o.CanBeHit && o.state != State.Held
                    && Mathf.Abs(o.ground.x - ground.x) < Width
                    && Mathf.Abs(o.ground.y - ground.y) < Feel.DepthBand
                    && Mathf.Abs(o.height - height) < Feel.HeightBand)
                {
                    o.TakeHit(Feel.BodyHitDamage, dir, true);
                    o.GetComponent<Enemy>()?.HitByMate(this); // bowled over by a mate: blames him
                }
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
            bool striking = state == State.Attack && stateFrames >= Startup && stateFrames < Startup + Active + 6;
            body.sprite = state switch
            {
                State.Idle => art.Idle[Game.Frame / 10 % art.Idle.Length],
                State.Walk => art.Walk[Game.Frame / 5 % art.Walk.Length],
                State.Run => art.Walk[Game.Frame / 3 % art.Walk.Length], // walk cycle, played fast
                State.Jump => airKick ? art.JumpKick : art.Air,
                State.GetUp => art.GetUp,
                State.Grab => art.Hold,
                State.Held => art.Hurt,
                State.Sit => art.Sit,
                State.Hang => hangKick > 0 ? art.JumpKick : art.ElbowRise, // arms up on the lamp, or kicking
                State.Buried => Sinking[Alive ? Mathf.Min(stateFrames, Art.BuriedFrame) // hammered in
                    : Mathf.Min(Art.BuriedFrame + sinkFrames / 2, Sinking.Length - 1)], // ...and on through the floor
                State.Headless => art.HeadlessHurt,
                State.Attack => move switch
                {
                    Move.Whip when art.Lash != null => art.Lash[LashFrame()],
                    Move.Whip => WindingUp ? art.WindUp : striking ? art.Punch2 : art.Idle[0],
                    Move.Smash or Move.BodyKick or Move.Pound => WindingUp ? art.KickWind : striking ? art.Kick : art.KickRecover,
                    Move.Kick => WindingUp ? art.KickWind : striking ? art.LowKick : art.KickRecover,
                    Move.Roundhouse => WindingUp ? art.Chamber : striking ? art.HighKick : art.KickRecover,
                    Move.Sweep => striking ? art.SweepKick : art.Crouch,
                    Move.FlyingKick => striking ? art.JumpKick : art.Crouch,
                    Move.Swing => striking ? art.Punch2 : art.WindUp,
                    Move.ThrowProp => striking ? art.Punch2 : art.WindUp,
                    Move.BicycleKick => striking ? art.Bicycle[stateFrames / 3 % 2] : art.KickWind, // left-right-left
                    Move.ElbowDrop => heightVel > 0f ? art.ElbowRise : art.ElbowDrop, // up with the elbow raised, down flat
                    Move.Knee => striking ? art.Headbutt : art.Hold,
                    Move.Throw or Move.Rip or Move.HairSlam => art.Throw[WindingUp ? 0 : 1],
                    Move.LowBlow => striking ? art.KickWind : art.Hold, // still holding on, then the knee comes up
                    Move.Special => striking ? art.Kick : art.KickWind,
                    _ => striking ? (punchAlt ? art.Punch2 : art.Punch1) : WindingUp && team != 0 ? art.WindUp : art.Idle[0],
                },
                State.Hurt => stun > 0 ? art.Doubled : art.Hurt,
                State.Launched => art.Fall,
                _ => halved ? art.HalfStays : headless ? art.HeadlessDown : art.Down, // down, dead
            };
            bool spinning = state == State.Attack && move == Move.Special && striking;
            bool turnedAway = state == State.Attack && move == Move.Roundhouse && WindingUp && stateFrames >= Startup / 2;
            body.flipX = spinning ? stateFrames / 4 % 2 == 0 : turnedAway ? facing > 0 : facing < 0; // the kick turns round and round
            // shakes in place through the hitstop; leans back winding up, lunges into the blow
            float jolt = Game.Frozen && flashFrames > 0 ? (Game.Frame % 2 == 0 ? Feel.HitJolt : -Feel.HitJolt) : 0f;
            float lunge = state != State.Attack || move is Move.FlyingKick or Move.BicycleKick or Move.ElbowDrop or Move.Throw or Move.HairSlam or Move.Special ? 0f
                : WindingUp ? -Feel.WindUpLean : striking ? (move is Move.Punch or Move.Knee ? Feel.JabLunge : Feel.HeavyLunge) : 0f;
            if (stumble > 0) lunge = Feel.HeavyLunge; // lurching after the miss, pitched forward
            body.transform.localPosition = new Vector3(jolt + lunge * facing, h + Mathf.Round(lift), 0f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, tilt - (stumble > 0 ? 12f * facing : 0f));
            // two stars circling the bowed head while doubled over
            for (int i = 0; i < dizzy.Length; i++)
            {
                dizzy[i].enabled = state == State.Hurt && stun > 0;
                float a = Game.Frame * 0.15f + i * Mathf.PI;
                dizzy[i].transform.localPosition = new Vector3(Mathf.Round(facing * 12f + Mathf.Cos(a) * 9f), Mathf.Round(h + 36f + Mathf.Sin(a) * 3f), 0f);
            }
            body.transform.localScale = size * (landSquash > 0 ? new Vector3(1.12f, 0.88f, 1f)
                : jumpStretch > 0 ? new Vector3(0.9f, 1.1f, 1f) : Vector3.one);
            shadow.enabled = state != State.Buried && !sunk;
            body.enabled = !sunk && (invulnFrames == 0 || spinning || invulnFrames / 3 % 2 == 0);
            bool telegraph = team != 0 && WindingUp && stateFrames / 3 % 2 == 0; // every enemy blow flashes as it winds up
            bool white = flashFrames > 5; // solid white the moment a blow lands, then red
            body.sharedMaterial = white ? Art.Silhouette : normalMat;
            body.color = white ? Color.white
                : flashFrames > 0 && flashFrames / 2 % 2 == 0 ? new Color(1f, 0.45f, 0.45f)
                : telegraph ? new Color(1f, 1f, 0.45f)
                : charred ? new Color(0.22f, 0.17f, 0.15f)
                : Alive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

            whipLine.enabled = move == Move.Whip && striking && art.Lash == null; // the lash frames have their own whip
            whipLine.transform.localScale = new Vector3(facing * 1.8f, 1f, 1f);
            whipLine.transform.localPosition = new Vector3(facing * 20f, h + 38f, 0f);

            // nearer to the camera (lower depth) draws on top; shadows under everything
            int order = -Mathf.RoundToInt(ground.y) * 2;
            body.sortingOrder = state == State.Sit ? order + 1 : order; // in front of the chair
            whipLine.sortingOrder = order + 1;
            foreach (var star in dizzy) star.sortingOrder = order + 1;
            // flames on the body: up the chest and head standing, along the body lying down
            bool lying = state is State.Down or State.Dead or State.Launched;
            for (int i = 0; i < burnFx.Length; i++)
            {
                burnFx[i].enabled = burning > 0;
                burnFx[i].sprite = Art.Flame[(Game.Frame / 4 + i) % 3];
                burnFx[i].transform.localPosition = lying ? new Vector3((i - 1) * 12f, h + 2f, 0f)
                    : new Vector3((i - 1) * 6f, h + 20f + i % 2 * 14f, 0f);
                burnFx[i].sortingOrder = order + 1;
            }
        }
    }
}
