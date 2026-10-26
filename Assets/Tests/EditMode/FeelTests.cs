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
        Game.Reset();
    }

    static void Run(int frames)
    {
        for (int i = 0; i < frames; i++) Game.Simulate();
    }

    [Test]
    public void JumpLandsAfterArc()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);

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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f + Feel.DepthBand + 1f), 10);
        Assert.IsFalse(p.InReach(e, Feel.PunchReach));

        e.ground.y = -70f;
        Assert.IsTrue(p.InReach(e, Feel.PunchReach));
    }

    [Test]
    public void HitstopFreezesEveryone()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(200f, -70f), 10);
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
    public void SamePunchFrameTrades()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);
        e.facing = -1;
        p.Attack(Move.Punch);
        e.Attack(Move.Punch);

        Run(Feel.PunchStartup);
        Assert.AreEqual(State.Hurt, p.state);
        Assert.AreEqual(State.Hurt, e.state);
    }

    static Fighter Lasher(float x)
    {
        var w = Game.Spawn(1, Art.Lasher, new Vector2(x, -70f), 10);
        w.whip = true;
        w.facing = -1;
        return w;
    }

    [Test]
    public void WhipHitsFromBeyondPunchRange()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var w = Lasher(40f + 45f);
        Assert.Greater(45f, Feel.PunchReach);

        w.Attack(Move.Whip);
        Run(Feel.WhipStartup);
        Assert.AreEqual(State.Hurt, p.state);
    }

    [Test]
    public void PunchDuringWhipWindupLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        float gap = (Feel.PunchReach + Feel.KickReach) / 2f; // too far for a punch, close enough for a kick
        var e = Game.Spawn(1, Art.GruntA, new Vector2(40f + gap, -70f), 10);
        Assert.Greater(gap, Feel.PunchReach);

        p.Attack(Move.Kick);
        Run(Feel.KickStartup);
        Assert.AreEqual(State.Hurt, e.state);
        Assert.AreEqual(8, e.health);
    }

    [Test]
    public void JumpKickLaunches()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(60f, -70f), 10);
        p.Jump();
        Run(3);
        p.JumpKick();
        Run(2);
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void GrabHeadbuttsThenThrowsBehind()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(52f, -70f), 10);
        p.Grab(e);
        Assert.AreEqual(State.Held, e.state);

        p.Attack(Move.Knee);
        Run(Feel.KneeStartup + 30);
        Assert.AreEqual(State.Held, e.state, "a headbutt keeps the hold");
        Assert.AreEqual(9, e.health);

        p.Attack(Move.Throw);
        Run(Feel.ThrowStartup);
        Assert.AreEqual(State.Launched, e.state);
        Assert.Less(e.vel.x, 0f, "thrown over the shoulder, behind the player");
    }

    [Test]
    public void RunIsFasterThanWalkAndBicycleKickHitsSeveralTimes()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);
        p.Attack(Move.Pound);
        Run(Feel.PoundStartup);
        Assert.AreEqual(State.Buried, e.state);

        Run(Feel.BuriedFrames + Feel.SmashHitstop);
        Assert.AreEqual(State.GetUp, e.state);
    }

    [Test]
    public void ElbowDropHitsEnemyLyingDown()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(55f, -70f), 10);
        e.TakeHit(1, 1, true);
        while (e.state != State.Down) Game.Simulate();
        e.ground.x = 55f; // the launch carried them away; put them back in front, no longer sliding
        e.vel = Vector2.zero;
        Assert.IsTrue(p.IsDownedEnemyInReach(Feel.ElbowDropReach));

        p.Attack(Move.ElbowDrop);
        Run(Feel.ElbowDropStartup);
        Assert.AreEqual(6, e.health, "9 left after the knockdown, the elbow does 3");
        Assert.AreEqual(State.Down, e.state);
    }

    [Test]
    public void LastHitIsAFinisherThatSendsThemFlying()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(65f, -70f), 1);
        p.Attack(Move.Punch);
        Run(Feel.PunchStartup);

        Assert.AreEqual(State.Launched, e.state, "even a jab launches on the last hit");
        Assert.AreEqual(Feel.LaunchX * Feel.FinisherLaunch, e.vel.x, 0.01f);
    }

    [Test]
    public void FlyingKickCrossesTheScreenAndLaunchesEveryoneInThePath()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var near = Game.Spawn(1, Art.GruntA, new Vector2(70f, -70f), 10);
        var far = Game.Spawn(1, Art.GruntA, new Vector2(170f, -70f), 10);
        p.Attack(Move.FlyingKick);
        Run(Feel.FlyingKickStartup + Feel.FlyingKickActive + Feel.FlyingKickRecovery + 2 * Feel.SmashHitstop);

        Assert.Greater(p.ground.x - 40f, Feel.FlyingKickSpeed * Feel.FlyingKickActive * 0.8f);
        Assert.AreEqual(State.Launched, near.state);
        Assert.AreEqual(State.Launched, far.state);
    }

    [Test]
    public void ChairSwingLaunchesAndBreaksAfterThreeHits()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(45f, -70f));
        Assert.AreSame(chair, p.PropInReach());
        p.PickUp(chair);
        Assert.AreSame(chair, p.held);

        for (int i = 0; i < Feel.ChairHits; i++)
        {
            var e = Game.Spawn(1, Art.GruntA, new Vector2(p.ground.x + 30f, -70f), 10);
            p.Attack(Move.Swing);
            Run(Feel.SwingStartup);
            Assert.AreEqual(State.Launched, e.state);
            Assert.AreEqual(7, e.health);
            Run(Feel.SwingActive + Feel.SwingRecovery + Feel.SmashHitstop);
        }
        Assert.IsNull(p.held, "the chair broke");
        Assert.AreEqual(0, Game.Props.Count);
    }

    [Test]
    public void ThrownBottleHitsAnEnemyAcrossTheRoom()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var e = Game.Spawn(1, Art.GruntA, new Vector2(140f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var chair = Game.SpawnProp(Prop.Kind.Chair, new Vector2(40f, -70f));
        p.PickUp(chair);
        p.TakeHit(1, 1, false);
        Assert.IsNull(p.held);
        Assert.IsTrue(chair.Free);
    }

    [Test]
    public void PunchTipsTheTableAndTheBottleOnItSmashes()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(70f, -70f));
        var e = Game.Spawn(1, Art.GruntA, new Vector2(120f, -70f), 10);
        e.facing = -1;
        table.flipped = true;

        p.Attack(Move.Kick);
        Run(Feel.KickStartup + 30);
        Assert.AreEqual(State.Launched, e.state);
    }

    [Test]
    public void TablesBlockWalkingButCanBeJumpedOver()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        p.PickUp(Game.SpawnProp(Prop.Kind.Chair, new Vector2(40f, -70f)));
        p.StartRun(1);
        Assert.AreNotEqual(State.Run, p.state);
    }

    [Test]
    public void ThrownChairIntoATableTipsItAndBreaks()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
        var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(70f, -70f));
        var e = Game.Spawn(1, Art.GruntA, new Vector2(80f, -62f), 10);
        p.Attack(Move.Punch);
        Run(Feel.PunchStartup + 10);
        Assert.IsTrue(table.flipped);
        Assert.AreEqual(10, e.health);
    }

    [Test]
    public void ElbowDropThatKnocksOutIsAFinisher()
    {
        var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 10);
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
}
