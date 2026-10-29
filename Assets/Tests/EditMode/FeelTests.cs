using NUnit.Framework;
using UnityEngine;

namespace Brawler.Tests
{
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
        public void KickStringEndsInALaunchingRoundhouse()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Game.Spawn(1, Art.GruntA, new Vector2(80f, -70f), 100);
            p.Attack(Move.BodyKick);
            Run(Feel.BodyKickStartup);
            Assert.AreEqual(State.Hurt, e.state, "the body kick staggers");
            Assert.AreEqual(100 - Feel.BodyKickDamage, e.health);
            Run(60);
            e.ground = new Vector2(80f, -70f);
            p.Attack(Move.Roundhouse);
            Run(Feel.RoundhouseStartup);
            Assert.AreEqual(State.Launched, e.state, "the roundhouse sends them flying");
        }

        [Test]
        public void SweepDropsThemWhereTheyStand()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Game.Spawn(1, Art.GruntA, new Vector2(75f, -70f), 100);
            p.Attack(Move.Sweep);
            Run(Feel.SweepStartup);
            Assert.AreEqual(State.Launched, e.state);
            Assert.AreEqual(100 - Feel.SweepDamage, e.health);
            int frames = 0;
            while (e.state != State.Down && frames++ < 120) Game.Simulate();
            Assert.AreEqual(State.Down, e.state);
            Assert.Less(Mathf.Abs(e.ground.x - 75f), 20f, "down on the spot, not knocked across the room");
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
        public void BrickHasOwnPosesForEveryMove()
        {
            // no stand-ins: each of these used to show the idle frame or the punch
            var a = Art.For(Art.Hero);
            Assert.AreNotSame(a.Punch1, a.Punch2, "combo alternates two punches");
            foreach (var s in new[] { a.Air, a.Crouch, a.GetUp }) Assert.AreNotSame(a.Idle[0], s);
            foreach (var s in new[] { a.Hold, a.Headbutt, a.Throw[0], a.Throw[1] }) Assert.AreNotSame(a.Punch1, s);
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
                bool isSkin = pa[i].a > 0 && pa[i].r > pa[i].g && pa[i].g > pa[i].b && pa[i].r - pa[i].b > 60; // warm
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
            Assert.AreNotEqual(State.Hang, p.state, "let go means off, not caught again by the same lamp");

            var lamp2 = Lamp.Spawn(300f, -70f, Feel.LampBottom);
            var q = Game.Spawn(0, Art.Hero, new Vector2(300f, -82f), 100);
            var bottle = Game.SpawnProp(Prop.Kind.Bottle, q.ground);
            q.PickUp(bottle);
            q.Hang(lamp2);
            q.RipLamp();
            Assert.IsFalse(Game.Lamps.Contains(lamp2), "torn down");
            Assert.IsNotNull(q.held);
            Assert.AreEqual(Prop.Kind.Lamp, q.held.kind);
            Assert.IsNull(bottle.holder, "the bottle carried up is dropped, not stuck to the hero");
        }

        [Test]
        public void FighterCorneredByATableStaysInTheBar()
        {
            var table = Game.SpawnProp(Prop.Kind.Table, new Vector2(Game.MinX + 4f, -60f));
            var e = Game.Spawn(1, Art.GruntA, new Vector2(Game.MinX, -60f), 40);
            Run(2);
            Assert.GreaterOrEqual(e.ground.x, Game.MinX);
            Assert.IsTrue(Mathf.Abs(e.ground.y - table.ground.y) >= Feel.TableHalfDepth - 0.01f, "pushed out sideways");
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
            Random.InitState(1); // a missed whip can hit a mate, who may swing back out of turn: a dice roll
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
            foreach (float x in new[] { 300f, 500f, 320f, 480f, 340f }) // punchers: the girls' whips strike from further out
                Game.Spawn(1, Art.Guy, new Vector2(x, -50f), 100).gameObject.AddComponent<Enemy>().Setup();
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

        [Test]
        public void SpecialHitsBothSidesUntouchableAndCostsHealth()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(200f, -70f), 100);
            Game.Hero = p;
            var front = Game.Spawn(1, Art.GruntA, new Vector2(225f, -70f), 100);
            var back = Game.Spawn(1, Art.GruntA, new Vector2(175f, -70f), 100);
            back.facing = 1;
            Assert.IsTrue(p.Special());
            Assert.AreEqual(100 - Feel.SpecialCost, p.health);
            Assert.AreEqual(Feel.SpecialCost, p.recoverable);

            back.Attack(Move.Punch);
            Run(Feel.SpecialStartup + 2);
            Assert.AreEqual(State.Launched, front.state);
            Assert.AreEqual(State.Launched, back.state);
            Assert.Less(back.vel.x, 0f, "the one behind flies off backwards");
            Assert.AreEqual(100 - Feel.SpecialCost, p.health, "untouchable while spinning");
        }

        [Test]
        public void SpecialCostIsWonBackByHitsAndLostWhenHit()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(200f, -70f), 100);
            Game.Hero = p;
            p.Special();
            Run(Feel.SpecialStartup + Feel.SpecialActive + Feel.SpecialRecovery + 1);

            var e = Game.Spawn(1, Art.GruntA, new Vector2(215f, -70f), 100);
            p.Attack(Move.Punch);
            Run(Feel.PunchStartup);
            Assert.AreEqual(100 - Feel.SpecialCost + Feel.RecoverPerHit, p.health);
            Assert.AreEqual(Feel.SpecialCost - Feel.RecoverPerHit, p.recoverable);

            p.TakeHit(1, -1, false);
            Assert.AreEqual(0, p.recoverable, "hit before winning it all back: the rest is gone");
        }

        [Test]
        public void NoSpecialOnTheLastBitOfHealth()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(200f, -70f), Feel.SpecialCost);
            Assert.IsFalse(p.Special());
            Assert.AreEqual(Feel.SpecialCost, p.health);
        }

        [Test]
        public void ACrowdSpreadsToBothSidesOfTheHero()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(500f, -50f), 1000);
            Game.Hero = p;
            foreach (float x in new[] { 300f, 320f, 340f, 360f, 380f, 400f })
                Game.Spawn(1, Art.GruntA, new Vector2(x, -50f), 100).gameObject.AddComponent<Enemy>().Setup();
            Run(600);
            int right = Game.Fighters.FindAll(f => f.team == 1 && f.ground.x > p.ground.x).Count;
            Assert.GreaterOrEqual(right, 2, "they all came from the left; some walk round to the right");
        }

        static Fighter Victim(float x, int hp)
        {
            var e = Game.Spawn(1, Art.GruntA, new Vector2(x, -70f), hp);
            e.facing = -1;
            return e;
        }

        [Test]
        public void KillingKickTakesTheHeadOff()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Victim(75f, 5);
            p.Attack(Move.Kick);
            Run(Feel.KickStartup);
            Assert.IsTrue(e.headless);
            Assert.AreEqual(State.Headless, e.state, "the body stays standing a moment");
            var head = Game.Props.Find(q => q.kind == Prop.Kind.Head);
            Assert.IsNotNull(head);
            Run(Feel.FinisherHitstop + 20);
            Assert.Greater(head.ground.x, 75f, "the head flies off the way it was kicked");
            Run(Feel.HeadlessFrames);
            Assert.AreEqual(State.Down, e.state, "then it drops");
        }

        [Test]
        public void KillingStompHammersThemThroughTheFloor()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Victim(55f, 5);
            p.Attack(Move.Pound);
            Run(Feel.PoundStartup);
            Assert.AreEqual(State.Buried, e.state);
            Assert.IsFalse(e.Alive);
            Run(Feel.FinisherHitstop + 200);
            Assert.AreEqual(State.Dead, e.state, "gone through the floor");
        }

        [Test]
        public void KickingSomeoneBuriedPuntsTheirHead()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Victim(70f, 100);
            e.Bury(10, 1);
            p.Attack(Move.Kick);
            Run(Feel.KickStartup);
            Assert.IsFalse(e.Alive);
            Assert.IsTrue(e.headless);
            var head = Game.Props.Find(q => q.kind == Prop.Kind.Head);
            Assert.AreSame(p, head.thrower, "a kicked head hits whoever is in the way");
        }

        [Test]
        public void LInAHoldRipsAnArmOffToClubWith()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Victim(52f, 100);
            p.Grab(e);
            p.Attack(Move.Rip);
            Run(Feel.RipStartup);
            Assert.IsTrue(e.armless);
            Assert.AreEqual(State.Launched, e.state);
            Assert.AreEqual(100 - Feel.ArmRipDamage, e.health);
            Assert.IsNotNull(p.held);
            Assert.AreEqual(Prop.Kind.Arm, p.held.kind);
        }

        [Test]
        public void KillingElbowDropTearsTheBodyInTwo()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Victim(55f, 5);
            e.TakeHit(1, 1, true);
            while (e.state != State.Down) Game.Simulate();
            e.ground.x = 55f;
            e.vel = Vector2.zero;
            p.Attack(Move.ElbowDrop);
            Run(Feel.ElbowDropStartup);
            Assert.IsTrue(e.halved);
            Assert.IsNotNull(Game.Props.Find(q => q.kind == Prop.Kind.Gib), "the other half");
        }

        [Test]
        public void LastHeadbuttBurstsTheHead()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(40f, -70f), 100);
            var e = Victim(52f, Feel.KneeDamage);
            p.Grab(e);
            p.Attack(Move.Knee);
            Run(Feel.KneeStartup);
            Assert.IsFalse(e.Alive);
            Assert.IsTrue(e.headless);
            Assert.AreEqual(State.Headless, e.state);
        }

        [Test]
        public void ALaunchedBodyBouncesOffTheEndWall()
        {
            var e = Game.Spawn(1, Art.GruntA, new Vector2(Game.MaxX - 10f, -50f), 40);
            e.TakeHit(1, 1, true);
            int frames = 0;
            while (e.vel.x > 0f && frames++ < 30) Game.Simulate();
            Assert.Less(e.vel.x, 0f, "bounced back off the wall");
            Assert.AreEqual(40 - 1 - Feel.WallSplatDamage, e.health);
            Assert.IsTrue(e.CanBeHit, "open for one more hit off the wall");
        }

        [Test]
        public void TheHeavyBossFliesLessFarAndShrugsOffJabs()
        {
            var a = Game.Spawn(1, Art.GruntA, new Vector2(300f, -20f), 100);
            var b = Game.Spawn(1, Art.Boss, new Vector2(300f, -90f), 100);
            b.mass = Feel.BossMass;
            a.TakeHit(1, 1, true);
            b.TakeHit(1, 1, true);
            Run(60);
            Assert.Less(b.ground.x - 300f, (a.ground.x - 300f) * 0.75f);

            var p = Game.Spawn(0, Art.Hero, new Vector2(600f, -50f), 100);
            var boss = Game.Spawn(1, Art.Boss, new Vector2(620f, -50f), Feel.BossHealth);
            boss.armored = true;
            boss.facing = -1;
            p.Attack(Move.Punch);
            Run(20);
            Assert.Less(boss.health, Feel.BossHealth, "the jab hurts");
            Assert.AreNotEqual(State.Hurt, boss.state, "but doesn't stagger him");
        }

        [Test]
        public void TheBossThrowsTheHero()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            var boss = Game.Spawn(1, Art.Boss, new Vector2(320f, -50f), Feel.BossHealth);
            boss.Grab(p);
            Assert.AreEqual(State.Held, p.state);
            boss.Attack(Move.Throw);
            Run(30);
            Assert.Less(p.health, 100);
            Assert.IsTrue(p.state is State.Launched or State.Down);
        }

        [Test]
        public void TheDrunkPunchesHisOwnMate()
        {
            var d = Game.Spawn(1, Art.Drunk, new Vector2(200f, -70f), Feel.DrunkHealth);
            d.drunk = true;
            d.facing = 1;
            var mate = Game.Spawn(1, Art.GruntA, new Vector2(224f, -70f), 30);
            d.Attack(Move.Punch);
            Run(30);
            Assert.Less(mate.health, 30);
            Assert.AreEqual(Feel.DrunkHealth, d.health, "not himself");
        }

        [Test]
        public void ABrokenBottleLeavesAPuddleThatTripsARunner()
        {
            Game.SpawnProp(Prop.Kind.Bottle, new Vector2(300f, -50f)).Break();
            Assert.IsTrue(Puddle.Under(new Vector2(300f, -50f)));
            var p = Game.Spawn(0, Art.Hero, new Vector2(240f, -50f), 100);
            p.StartRun(1);
            p.moveInput = Vector2.right;
            int frames = 0;
            while (p.state == State.Run && frames++ < 60) Game.Simulate();
            Assert.AreEqual(State.Launched, p.state, "slipped");
            Assert.AreEqual(100, p.health, "a slip doesn't hurt");
        }

        [Test]
        public void AnEnemyIsOpenTheMomentItStandsUp()
        {
            var e = Game.Spawn(1, Art.GruntA, new Vector2(300f, -50f), 100);
            e.TakeHit(1, 1, true);
            while (e.state != State.Down) Game.Simulate();
            Run(Feel.DownFrames - 1);
            Assert.AreEqual(State.Down, e.state, "still lying there");
            while (e.state != State.Idle) Game.Simulate();
            Assert.IsTrue(e.CanBeHit, "no blinking invulnerability for enemies");
        }

        [Test]
        public void ALowBlowDoublesAGuyOver()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            var e = Game.Spawn(1, Art.Guy, new Vector2(324f, -50f), 40);
            p.Grab(e);
            p.Attack(Move.LowBlow);
            Run(Feel.KneeStartup + 1);
            Assert.AreEqual(40 - Feel.LowBlowDamage, e.health);
            Run(Feel.LowBlowStun - 10);
            Assert.AreEqual(State.Hurt, e.state, "still doubled over");
            p.Attack(Move.Punch);
            Run(10);
            Assert.AreEqual(State.Hurt, e.state, "a jab doesn't end it");
            Run(Feel.LowBlowStun);
            Assert.AreEqual(State.Idle, e.state, "straightens up in the end");
        }

        [Test]
        public void AHairSlamPutsAGirlDownInFront()
        {
            Assert.IsTrue(Art.IsGirl(Art.GruntA));
            Assert.IsFalse(Art.IsGirl(Art.Guy));
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            var e = Game.Spawn(1, Art.GruntA, new Vector2(324f, -50f), 100);
            p.Grab(e);
            p.Attack(Move.HairSlam);
            int frames = 0;
            while (e.state != State.Down && frames++ < 60) Game.Simulate();
            Assert.AreEqual(State.Down, e.state);
            Assert.AreEqual(100 - Feel.HairSlamDamage, e.health);
            Assert.Less(Mathf.Abs(e.ground.x - p.ground.x), Feel.ElbowDropReach, "right in front: elbow-drop range");
        }

        [Test]
        public void TheDirtyMoveNeedsNoGrabFirst()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            var guy = Game.Spawn(1, Art.Guy, new Vector2(330f, -50f), 40);
            Assert.IsTrue(p.DirtyMove(), "an idle guy right in front");
            Run(Feel.KneeStartup + 2);
            Assert.AreEqual(40 - Feel.LowBlowDamage, guy.health);
            Assert.AreEqual(State.Hurt, guy.state);
            Run(Feel.KneeRecovery + 4);
            Assert.IsFalse(p.DirtyMove(), "cooling down");
            Run(Feel.DirtyCooldown);
            Assert.IsTrue(p.DirtyMove(), "ready again");

            var boss = Game.Spawn(1, Art.Boss, new Vector2(600f, -50f), Feel.BossHealth);
            boss.armored = true;
            var q = Game.Spawn(0, Art.Hero, new Vector2(570f, -50f), 100);
            Assert.IsFalse(q.DirtyMove(), "not on the boss");
            Assert.IsFalse(Game.Spawn(0, Art.Hero, new Vector2(900f, -50f), 100).DirtyMove(), "nobody in reach");
        }

        [Test]
        public void ABottleBrickThrowsIsAMolotov()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            var bottle = Game.SpawnProp(Prop.Kind.Bottle, p.ground);
            p.PickUp(bottle);
            p.Attack(Move.ThrowProp);
            int frames = 0;
            while (Game.Props.Contains(bottle) && frames++ < 120) Game.Simulate();
            Assert.IsTrue(bottle == null || !Game.Props.Contains(bottle), "broke on landing");
            Assert.AreEqual(1, Fire.Count, "the floor burns where it broke");

            var e = Game.Spawn(0, Art.Hero, new Vector2(300f, -90f), 30); // enemy bottles hit Brick's side
            var q = Game.Spawn(1, Art.Chucker, new Vector2(200f, -90f), 30);
            var enemyBottle = Game.SpawnProp(Prop.Kind.Bottle, q.ground);
            q.PickUp(enemyBottle);
            q.Attack(Move.ThrowProp);
            Run(120);
            Assert.AreEqual(1, Fire.Count, "an enemy's bottle is just beer");
            Assert.Less(e.health, 30, "but it still hurts");
        }

        [Test]
        public void StandingInFireBurnsAndBurntToDeathIsDown()
        {
            var e = Game.Spawn(1, Art.Guy, new Vector2(400f, -50f), Feel.BurnDamage * 2);
            Fire.Spawn(e.ground);
            Run(1);
            Assert.Greater(e.burning, 0, "caught fire");
            int frames = 0;
            while (e.Alive && frames++ < Feel.BurnTick * 3) Game.Simulate();
            Assert.IsFalse(e.Alive, "burnt to death");
            Run(30);
            Assert.IsTrue(e.state is State.Down or State.Dead, "drops where he stood");
        }

        static Fighter LyingEnemy(Vector2 at, int hp)
        {
            var e = Game.Spawn(1, Art.Guy, at, hp);
            e.TakeHit(1, 1, true);
            while (e.state != State.Down) Game.Simulate();
            e.ground = at;
            e.vel = Vector2.zero;
            return e;
        }

        [Test]
        public void TheElbowDropSteersOntoABodyFurtherAway()
        {
            var e = LyingEnemy(new Vector2(370f, -35f), 100);
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            Assert.IsTrue(p.IsDownedEnemyInReach(Feel.ElbowDropHoming, Feel.ElbowDropHomingDepth), "70 px ahead, 15 off the lane");
            int hp = e.health;
            p.Attack(Move.ElbowDrop);
            Run(Feel.ElbowDropStartup + 2);
            Assert.Less(e.health, hp, "landed on him");
        }

        [Test]
        public void AFlyingKickHitsBodiesOnTheFloorOnce()
        {
            var e = LyingEnemy(new Vector2(380f, -50f), 100);
            var p = Game.Spawn(0, Art.Hero, new Vector2(300f, -50f), 100);
            int hp = e.health;
            p.Attack(Move.FlyingKick);
            Run(Feel.FlyingKickStartup + Feel.FlyingKickActive);
            Assert.AreEqual(hp - Feel.FlyingKickDamage, e.health, "kicked once on the way over, not every frame");
        }

        [Test]
        public void AnEnemyHitByAMateMayTurnOnThem()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(800f, -50f), 1000);
            Game.Hero = p;
            var a = Game.Spawn(1, Art.Guy, new Vector2(300f, -50f), 100);
            var b = Game.Spawn(1, Art.GruntA, new Vector2(330f, -50f), 100);
            a.facing = 1;
            a.Attack(Move.Punch);
            Run(20);
            Assert.Less(b.health, 100, "an enemy's punch lands on a mate in the way");

            var be = b.gameObject.AddComponent<Enemy>();
            be.Setup();
            Random.InitState(1);
            for (int i = 0; i < 5; i++) be.HitByMate(a); // at 60% one of five sticks
            int hp = a.health;
            Run(300);
            Assert.Less(a.health, hp, "she went after him, not the hero");
        }

        [Test]
        public void ADodgedBlowCarriesOnIntoAMateBehind()
        {
            var a = Game.Spawn(1, Art.Guy, new Vector2(300f, -50f), 100);
            var b = Game.Spawn(1, Art.GruntA, new Vector2(360f, -50f), 100); // 60 px: past a jab's reach, the hero just stepped out of it
            a.facing = 1;
            a.Attack(Move.Punch);
            Run(20);
            Assert.Less(b.health, 100);
        }

        [Test]
        public void BowlingAMateOverOrSettingHimAlightStartsAGrudge()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(800f, -50f), 1000);
            Game.Hero = p;
            bool blamed = false;
            for (int i = 0; i < 5 && !blamed; i++) // 60% each time
            {
                float x = 100f + i * 150f;
                var flier = Game.Spawn(1, Art.Guy, new Vector2(x, -90f), 100);
                var standing = Game.Spawn(1, Art.GruntA, new Vector2(x + 40f, -90f), 100);
                var brain = standing.gameObject.AddComponent<Enemy>();
                brain.Setup();
                flier.TakeHit(1, 1, true); // sent flying into her
                Run(30);
                Assert.Less(standing.health, 100, "bowled over");
                blamed = brain.HasGrudge;
            }
            Assert.IsTrue(blamed, "blames the mate who flew into her");

            var burner = Game.Spawn(1, Art.Guy, new Vector2(500f, -50f), 100);
            var next = Game.Spawn(1, Art.GruntB, new Vector2(520f, -50f), 100);
            burner.Ignite();
            Run(12);
            Assert.Greater(next.burning, 0, "fire spreads to whoever a burning body touches");
        }

        [Test]
        public void OldTomPicksFightsWithHisMates()
        {
            var p = Game.Spawn(0, Art.Hero, new Vector2(900f, -50f), 100000);
            Game.Hero = p;
            var tom = Game.Spawn(1, Art.Drunk, new Vector2(300f, -50f), 1000);
            tom.drunk = true;
            var brain = tom.gameObject.AddComponent<Enemy>();
            brain.Setup();
            Game.Spawn(1, Art.Guy, new Vector2(380f, -60f), 1000);
            Random.InitState(5);
            int frames = 0;
            while (!brain.HasGrudge && frames++ < 60 * 60) Game.Simulate();
            Assert.IsTrue(brain.HasGrudge, "within a minute he takes offence at someone");
        }

        [Test]
        public void FireSpreadsOneStepAndNeverToBrick()
        {
            var a = Game.Spawn(1, Art.Guy, new Vector2(300f, -50f), 1000);
            var b = Game.Spawn(1, Art.GruntA, new Vector2(320f, -50f), 1000);
            var c = Game.Spawn(1, Art.GruntB, new Vector2(340f, -50f), 1000);
            var p = Game.Spawn(0, Art.Hero, new Vector2(280f, -50f), 1000);
            a.Ignite();
            Run(25);
            Assert.Greater(b.burning, 0, "caught from a");
            Assert.AreEqual(0, c.burning, "b caught it from a body: doesn't pass it on");
            Assert.AreEqual(0, p.burning, "Brick doesn't catch it from bodies");
        }
    }
}
