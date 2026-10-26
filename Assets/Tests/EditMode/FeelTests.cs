using NUnit.Framework;
using UnityEngine;

public class FeelTests
{
    [SetUp] public void SetUp() => Game.Reset();

    [TearDown]
    public void TearDown()
    {
        foreach (var f in Game.Fighters) Object.DestroyImmediate(f.gameObject);
        foreach (var p in Game.Props) Object.DestroyImmediate(p.gameObject);
        foreach (var l in Game.Lamps) Object.DestroyImmediate(l.gameObject);
        Game.Reset();
    }

    static void Run(int frames)
    {
        for (int i = 0; i < frames; i++) Game.Simulate();
    }

    [Test]
    public void JumpLandsAfterArc()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        p.Jump();
        int frames = 0;
        while (p.state == State.Jump && frames < 120) { Game.Simulate(); frames++; }

        int expected = Mathf.RoundToInt(2f * Feel.JumpVel / Feel.Gravity);
        Assert.That(frames, Is.InRange(expected - 2, expected + 2));
        Assert.AreEqual(0f, p.height);
    }

    [Test]
    public void JabHurtsSmashLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 100);

        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, e.state);

        Run(40);
        p.Attack(Move.Smash);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Launched, e.state);

        Run(Feel.SmashHitstop + 30);
        Assert.Greater(e.ground.x, 100f);
    }

    [Test]
    public void NoHitOutsideDepthBand()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f + Feel.DepthBand + 1f), 100);
        Assert.IsFalse(p.InReach(e, Feel.PunchReach));

        e.ground.y = -70f;
        Assert.IsTrue(p.InReach(e, Feel.PunchReach));
    }

    [Test]
    public void HitstopFreezesEveryone()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(200f, -70f), 100);
        p.Jump();
        e.Jump();
        Game.Simulate();
        float hp = p.height, he = e.height;

        Game.Freeze(5, 0);
        Run(5);
        Assert.AreEqual(hp, p.height);
        Assert.AreEqual(he, e.height);

        Run(1);
        Assert.Greater(p.height, hp);
        Assert.Greater(e.height, he);
    }

    [Test]
    public void HeroWinsASameFramePunch()
    {
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 100); // first in the list on purpose
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        Game.Hero = p;
        e.facing = -1;
        p.Attack(Move.Punch);
        e.Attack(Move.Punch);

        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, e.state);
        Assert.AreEqual(100, p.health, "no trade: being hit cancels the enemy's punch");
    }

    static Fighter Lasher(float x)
    {
        var w = Game.Spawn(1, Art.Lasher, new Vector2(x, -70f), 100);
        w.whip = true;
        w.facing = -1;
        return w;
    }

    [Test]
    public void WhipHitsFromBeyondPunchRange()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var w = Lasher(40f + 45f);
        Assert.Greater(45f, Feel.PunchReach);

        w.Attack(Move.Whip);
        Run(Feel.WhipStartup);
        Assert.AreEqual(State.Hurt, p.state);
    }

    [Test]
    public void PunchDuringWhipWindupLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var w = Lasher(55f);
        w.Attack(Move.Whip);
        Run(2);

        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Launched, w.state);
    }

    [Test]
    public void KickReachesFurtherThanPunch()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        float gap = (Feel.PunchReach + Feel.KickReach) / 2f; // too far for a punch, close enough for a kick
        var e = Game.Spawn(1, Art.GruntA, new Vector2(40f + gap, -70f), 100);
        Assert.Greater(gap, Feel.PunchReach);

        p.Attack(Move.Kick);
        Run(Feel.KickStartup);
        Assert.AreEqual(State.Hurt, e.state);
        Assert.AreEqual(100 - Feel.KickDamage, e.health);
    }

    [Test]
    public void JumpKickLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(60f, -70f), 100);
        p.Jump();
        Run(3);
        p.JumpKick();
        Run(2);
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void GrabHeadbuttsThenThrowsBehind()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(52f, -70f), 100);
        p.Grab(e);
        Assert.AreEqual(State.Held, e.state);

        p.Attack(Move.Knee);
        Run(Feel.KneeStartup + 30);
        Assert.AreEqual(State.Held, e.state, "a headbutt keeps the hold");
        Assert.AreEqual(100 - Feel.KneeDamage, e.health);

        p.Attack(Move.Throw);
        Run(Feel.ThrowStartup);
        Assert.AreEqual(State.Launched, e.state);
        Assert.Less(e.vel.x, 0f, "thrown over the shoulder, behind the player");
    }

    [Test]
    public void RunIsFasterThanWalkAndBicycleKickHitsSeveralTimes()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        p.moveInput = Vector2.right;
        p.StartRun(1);
        Run(10);
        Assert.AreEqual(State.Run, p.state);
        Assert.AreEqual(Feel.RunX, p.vel.x);
        Assert.Greater(Feel.RunX, Feel.WalkX);

        var e = Game.Spawn(1, Art.GruntA, new Vector2(p.ground.x + 20f, -70f), 20);
        float start = p.ground.x;
        p.Attack(Move.BicycleKick);
        Run(Feel.BicycleKickStartup);
        Assert.AreEqual(State.Hurt, e.state, "the first kicks only stagger");

        Run(Feel.BicycleKickActive + Feel.BicycleKickRecovery + 6 * Feel.Hitstop + Feel.SmashHitstop);
        Assert.Less(e.health, 20 - 2, "kicked again and again while flying");
        Assert.Greater(p.ground.x - start, 60f, "travels forward while kicking");
    }

    [Test]
    public void PoundBuriesThenEnemyPopsOut()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 100);
        p.Attack(Move.Pound);
        Run(Feel.PoundStartup);
        Assert.AreEqual(State.Buried, e.state);

        Run(Feel.BuriedFrames + Feel.SmashHitstop);
        Assert.AreEqual(State.GetUp, e.state);
    }

    [Test]
    public void ElbowDropHitsEnemyLyingDown()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 100);
        e.TakeHit(1, 1, true);
        while (e.state != State.Down) Game.Simulate();
        e.ground.x = 55f; // the launch carried them away; put them back in front, no longer sliding
        e.vel = Vector2.zero;
        Assert.IsTrue(p.IsDownedEnemyInReach(Feel.ElbowDropReach));

        p.Attack(Move.ElbowDrop);
        Run(Feel.ElbowDropStartup);
        Assert.AreEqual(99 - Feel.ElbowDropDamage, e.health, "99 left after the knockdown");
        Assert.AreEqual(State.Down, e.state);
    }

    [Test]
    public void LastHitIsAFinisherThatSendsThemFlying()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(65f, -70f), 1);
        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);

        Assert.AreEqual(State.Launched, e.state, "even a jab launches on the last hit");
        Assert.AreEqual(Feel.LaunchX * Feel.FinisherLaunch, e.vel.x, 0.01f);
    }

    [Test]
    public void FlyingKickCrossesTheScreenAndLaunchesEveryoneInThePath()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var near = Game.Spawn(1, Art.GruntA, new Vector2(70f, -70f), 100);
        var far = Game.Spawn(1, Art.GruntA, new Vector2(170f, -70f), 100);
        p.Attack(Move.FlyingKick);
        Run(Feel.FlyingKickStartup + Feel.FlyingKickActive + Feel.FlyingKickRecovery + 2 * Feel.SmashHitstop);

        Assert.Greater(p.ground.x - 40f, Feel.FlyingKickSpeed * Feel.FlyingKickActive * 0.8f);
        Assert.AreEqual(State.Launched, near.state);
        Assert.AreEqual(State.Launched, far.state);
    }

    [Test]
    public void ChairSwingLaunchesAndBreaksAfterThreeHits()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(45f, -70f));
        Assert.AreSame(chair, p.PropInReach());
        p.PickUp(chair);
        Assert.AreSame(chair, p.held);

        for (int i = 0; i < Feel.ChairHits; i++)
        {
            var e = Game.Spawn(1, Art.GruntA, new Vector2(p.ground.x + 30f, -70f), 100);
            p.Attack(Move.Swing);
            Run(Feel.SwingStartup);
            Assert.AreEqual(State.Launched, e.state);
            Assert.AreEqual(100 - Feel.HeavyPropDamage, e.health);
            Run(Feel.SwingActive + Feel.SwingRecovery + Feel.SmashHitstop);
        }
        Assert.IsNull(p.held, "the chair broke");
        Assert.AreEqual(0, Game.Props.Count);
    }

    [Test]
    public void ThrownBottleHitsAnEnemyAcrossTheRoom()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(140f, -70f), 100);
        var bottle = Game.SpawnProp(Prop.Kind.Bottle, new Vector2(40f, -70f));
        p.PickUp(bottle);
        p.Attack(Move.ThrowProp);
        Run(Feel.PropThrowStartup + 40);

        Assert.AreEqual(State.Launched, e.state);
        Assert.IsNull(p.held);
        Assert.AreEqual(0, Game.Props.Count, "the bottle smashed");
    }

    [Test]
    public void GettingHitDropsTheProp()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(40f, -70f));
        p.PickUp(chair);
        p.TakeHit(1, 1, false);
        Assert.IsNull(p.held);
        Assert.IsTrue(chair.Free);
    }

    [Test]
    public void PunchTipsTheTableAndTheBottleOnItSmashes()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(70f, -70f));
        var bottle = Game.SpawnProp(Prop.Kind.Bottle, new Vector2(72f, -70f), 26f);
        Assert.IsNull(p.PropInReach(), "tables can't be picked up; the bottle is out of pickup range");

        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.IsTrue(table.flipped);
        Assert.IsFalse(bottle.resting, "knocked off the table");

        Run(60);
        Assert.IsFalse(Game.Props.Contains(bottle), "smashed on the floor");
    }

    [Test]
    public void ThrownChairSmashesWhenItLands()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(40f, -70f));
        p.PickUp(chair);
        p.Attack(Move.ThrowProp);
        Run(Feel.PropThrowStartup + 60);
        Assert.AreEqual(0, Game.Props.Count);
        Assert.IsNull(p.held);
    }

    [Test]
    public void TippedTableHitAgainSlidesIntoAnEnemy()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(70f, -70f));
        var e = Game.Spawn(1, Art.GruntA, new Vector2(120f, -70f), 100);
        e.facing = -1;
        table.flipped = true;

        p.Attack(Move.Kick);
        Run(Feel.KickStartup + 30);
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void TablesBlockWalkingButCanBeJumpedOver()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(100f, -70f));
        p.moveInput = Vector2.right;
        Run(60);
        Assert.LessOrEqual(p.ground.x, table.ground.x - Feel.TableHalfWidth + 0.01f, "stopped at the table");

        p.Jump();
        Run(40);
        Assert.Greater(p.ground.x, table.ground.x, "cleared it with a jump");
    }

    [Test]
    public void TippedTableBreaksOnTheThirdHit()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(65f, -70f));
        table.flipped = true;
        for (int i = 0; i < Feel.TableHits; i++)
        {
            table.vel = Vector2.zero;
            table.ground.x = 65f; // back in reach after each slide
            p.Attack(Move.Punch);
            Run(Feel.PunchStartup + Feel.PunchActive + Feel.PunchRecovery + Feel.Hitstop);
        }
        Assert.IsFalse(Game.Props.Contains(table));
    }

    [Test]
    public void NoRunningWhileCarrying()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        p.PickUp(Game.SpawnProp(Prop.Kind.Chair, new Vector2(40f, -70f)));
        p.StartRun(1);
        Assert.AreNotEqual(State.Run, p.state);
    }

    [Test]
    public void ThrownChairIntoATableTipsItAndBreaks()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(40f, -70f));
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(110f, -70f));
        p.PickUp(chair);
        p.Attack(Move.ThrowProp);
        Run(Feel.PropThrowStartup + 30);
        Assert.IsTrue(table.flipped);
        Assert.IsFalse(Game.Props.Contains(chair));
    }

    [Test]
    public void TippingATableDoesNotHurtSomeoneStandingNextToIt()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(70f, -70f));
        var e = Game.Spawn(1, Art.GruntA, new Vector2(80f, -62f), 100);
        p.Attack(Move.Punch);
        Run(Feel.PunchStartup + 10);
        Assert.IsTrue(table.flipped);
        Assert.AreEqual(100, e.health);
    }

    [Test]
    public void ElbowDropThatKnocksOutIsAFinisher()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 4);
        e.TakeHit(1, 1, true);
        while (e.state != State.Down) Game.Simulate();
        e.ground.x = 55f;
        e.vel = Vector2.zero;

        p.Attack(Move.ElbowDrop);
        Run(Feel.ElbowDropStartup);
        Assert.IsFalse(e.Alive);
        Run(Feel.FinisherHitstop);
        Assert.IsTrue(e.state == State.Down || e.state == State.Dead, "stays down for good");
    }

    [Test]
    public void PatronsSitUntilTheFightStartsThenGetUp()
    {
        Game.Hero = Game.Spawn(0, Art.Hero, new Vector2(300f, -70f), 100);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(100f, -70f));
        var patron = Game.Seat(Art.GruntA, chair, 1, 3);
        Assert.IsNull(Game.Hero.PropInReach(), "nothing nearby");
        Run(200);
        Assert.AreEqual(State.Sit, patron.state);
        Assert.IsFalse(chair.Pickable, "someone is sitting on it");

        Game.Brawl = true;
        Run(160);
        Assert.AreNotEqual(State.Sit, patron.state);
        Assert.IsTrue(chair.Pickable, "free once they stand up");
    }

    [Test]
    public void RecolouredLooksKeepTheirSkin()
    {
        // the same frame in two looks: clothes and hair differ, skin pixels stay identical
        var a = Art.For(Art.GruntA).Idle[0];
        var b = Art.For(Art.GruntB).Idle[0];
        var pa = a.texture.GetPixels32();
        var pb = b.texture.GetPixels32();
        int skin = 0, changed = 0;
        for (int i = 0; i < pa.Length; i++)
        {
            bool isSkin = pa[i].r == 0xff && pa[i].g == 0xb1 && pa[i].b == 0x64;
            if (isSkin) { skin++; Assert.AreEqual(pa[i], pb[i]); }
            else if (!pa[i].Equals(pb[i])) changed++;
        }
        Assert.Greater(skin, 0);
        Assert.Greater(changed, 0, "outfit recoloured");
    }

    [Test]
    public void PoolTableIsHeavySolidAndShedsItsBalls()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var pool = Game.SpawnProp(Prop.Kind.PoolTable, new Vector2(120f, -70f));
        var ball = Game.SpawnProp(Prop.Kind.Ball, new Vector2(70f, -79f), 28f);
        Assert.IsFalse(pool.Pickable);

        p.moveInput = Vector2.right;
        Run(60);
        Assert.LessOrEqual(p.ground.x, pool.ground.x - Feel.PoolHalfWidth + 0.01f, "can't walk through it");

        p.moveInput = Vector2.zero;
        p.Attack(Move.Kick);
        Run(Feel.KickStartup + 2);
        Assert.AreEqual(120f, pool.ground.x, "doesn't budge");
        Assert.IsFalse(ball.resting, "the ball rolls off the felt");
    }

    [Test]
    public void CueThrustReachesFurtherThanAnything()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var cue = Game.SpawnProp(Prop.Kind.Cue, new Vector2(40f, -70f));
        var e = Game.Spawn(1, Art.GruntA, new Vector2(40f + Feel.CueReach - 4f, -70f), 100);
        Assert.Greater(Feel.CueReach - 4f, Feel.KickReach);
        p.PickUp(cue);
        p.Attack(Move.Swing);
        Run(Feel.SwingStartup);
        Assert.AreEqual(100 - Feel.LightPropDamage, e.health);
    }

    [Test]
    public void ThrownBallBouncesInsteadOfBreaking()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var ball = Game.SpawnProp(Prop.Kind.Ball, new Vector2(40f, -70f));
        p.PickUp(ball);
        p.Attack(Move.ThrowProp);
        Run(Feel.PropThrowStartup + 120);
        Assert.IsTrue(Game.Props.Contains(ball), "still there");
        Assert.AreEqual(0f, ball.height);
        Assert.Greater(ball.ground.x, 80f, "flew and rolled away");
    }

    static Fighter HeroUnderALamp(out Lamp lamp)
    {
        lamp = Lamp.Spawn(100f, -70f, Feel.LampBottom);
        var p = Game.Spawn(0, Art.Hero, new Vector2(100f, -82f), 100); // in front of where its pool table would be
        p.Jump();
        int frames = 0;
        while (p.state == State.Jump && frames++ < 60) Game.Simulate();
        return p;
    }

    [Test]
    public void JumpingIntoALampHangsFromIt()
    {
        var p = HeroUnderALamp(out var lamp);
        Assert.AreEqual(State.Hang, p.state);
        Assert.AreSame(p, lamp.hanger);
        Assert.AreEqual(Feel.LampBottom - Feel.HangReach, p.height, 0.01f);
    }

    [Test]
    public void PumpingSwingsTheLampAndTheHeroWithIt()
    {
        var p = HeroUnderALamp(out var lamp);
        p.moveInput = Vector2.right;
        Run(40);
        Assert.Greater(lamp.angle, 0.05f);
        Assert.AreEqual(lamp.SwingX, p.ground.x, 0.01f);
    }

    [Test]
    public void KickFromTheLampLaunches()
    {
        var p = HeroUnderALamp(out _);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(p.ground.x + 30f, -82f), 100);
        p.HangKick();
        Run(Feel.HangKickStartup + 1);
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void RippingTheLampGivesAWeaponAndLetGoKeepsItSwinging()
    {
        var p = HeroUnderALamp(out var lamp);
        p.moveInput = Vector2.right;
        Run(30);
        p.moveInput = Vector2.zero;
        p.LetGo();
        Assert.IsNull(lamp.hanger);
        float swing = Mathf.Abs(lamp.angle) + Mathf.Abs(lamp.angVel);
        Run(10);
        Assert.Greater(Mathf.Abs(lamp.angle) + Mathf.Abs(lamp.angVel), 0f, "still swinging");
        Assert.Greater(swing, 0f);

        var lamp2 = Lamp.Spawn(300f, -70f, Feel.LampBottom);
        var q = Game.Spawn(0, Art.Hero, new Vector2(300f, -82f), 100);
        q.Hang(lamp2);
        q.RipLamp();
        Assert.IsFalse(Game.Lamps.Contains(lamp2), "torn down");
        Assert.IsNotNull(q.held);
        Assert.AreEqual(Prop.Kind.Lamp, q.held.kind);
    }

    [Test]
    public void ElbowDropIgnoresAKnockedOutBody()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 1);
        e.TakeHit(1, 1, true);
        while (e.state != State.Down) Game.Simulate();
        e.ground.x = 55f;
        e.vel = Vector2.zero;
        Assert.IsFalse(p.IsDownedEnemyInReach(Feel.ElbowDropReach), "a corpse is not a target");
    }

    [Test]
    public void ReleasedEnemyCannotBeGrabbedOrHitAtOnce()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(52f, -70f), 100);
        p.Grab(e);
        Run(Feel.GrabFrames + 1);
        Assert.AreEqual(State.Hurt, e.state, "the hold ran out");
        Assert.IsFalse(e.CanBeHit, "a moment to break free");
        Run(Feel.BreakFreeInvuln);
        Assert.IsTrue(e.CanBeHit);
    }

    [Test]
    public void HeroCannotBeHitAgainRightAway()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        p.TakeHit(1, 1, false);
        Run(Feel.Hitstun);
        p.TakeHit(1, 1, false);
        Assert.AreEqual(99, p.health, "a crowd can't stunlock the hero");
        Run(Feel.HeroHurtInvuln);
        p.TakeHit(1, 1, false);
        Assert.AreEqual(98, p.health);
    }

    [Test]
    public void HittingABuriedEnemyKnocksThemOutFlying()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 100);
        p.Attack(Move.Pound);
        Run(Feel.PoundStartup);
        Assert.AreEqual(State.Buried, e.state);

        e.Bury(2, 1); // a second stomp doesn't keep them in the ground
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void EnemyBacksOffFacingTheHero()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        Game.Hero = p;
        var e = Game.Spawn(1, Art.GruntA, new Vector2(50f, -70f), 100);
        e.gameObject.AddComponent<Enemy>().Setup();
        Run(10);
        Assert.Greater(e.ground.x, 50f, "too close: steps back");
        Assert.AreEqual(-1, e.facing, "still facing the hero");
    }

    [Test]
    public void AtMostTwoEnemiesSwingAtOnce()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(200f, -70f), 1000);
        Game.Hero = p;
        foreach (float x in new[] { 140f, 260f, 150f, 250f }) // whips: long wind-ups would overlap without the cap
            Lasher(x).gameObject.AddComponent<Enemy>().Setup();
        int most = 0;
        for (int i = 0; i < 600; i++)
        {
            Game.Simulate();
            int n = Game.Fighters.FindAll(f => f.team == 1 && f.state == State.Attack).Count;
            most = Mathf.Max(most, n);
        }
        Assert.That(most, Is.InRange(1, Feel.MaxAttackers));
    }

    [Test]
    public void WaitingEnemiesStayOffTheHerosLane()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(400f, -50f), 1000);
        Game.Hero = p;
        foreach (float x in new[] { 300f, 500f, 320f, 480f, 340f })
            Game.Spawn(1, Art.GruntA, new Vector2(x, -50f), 100).gameObject.AddComponent<Enemy>().Setup();
        int most = 0;
        for (int i = 0; i < 600; i++)
        {
            Game.Simulate();
            if (i < 200) continue; // walking in
            int inFace = Game.Fighters.FindAll(f => f.team == 1 && Mathf.Abs(f.ground.x - p.ground.x) < Feel.PunchReach + 4f
                && Mathf.Abs(f.ground.y - p.ground.y) < Feel.DepthBand).Count;
            most = Mathf.Max(most, inFace);
        }
        Assert.That(most, Is.InRange(1, Feel.MaxAttackers), "only the ones having their turn come up to the hero");
    }

    [Test]
    public void LongCombosDoLessAndEndInAKnockdown()
    {
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 1000);
        for (int i = 0; i < Feel.ComboScaleFrom - 1; i++) e.TakeHit(10, 1, false);
        Assert.AreEqual(1000 - 10 * (Feel.ComboScaleFrom - 1), e.health, "full damage at first");
        int before = e.health;
        e.TakeHit(10, 1, false);
        Assert.AreEqual(before - 9, e.health, "then 10% less");

        while (e.comboHits < Feel.JuggleCap - 1) e.TakeHit(10, 1, false);
        Assert.AreEqual(State.Hurt, e.state);
        e.TakeHit(10, 1, false);
        Assert.AreEqual(State.Launched, e.state, "the combo is over: they fall");
    }

    [Test]
    public void PunchComboCountsOnlyHits()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.IsFalse(p.lastStrikeHit, "a whiff");
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 100);
        Run(20);
        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);
        Assert.IsTrue(p.lastStrikeHit);
    }
}
