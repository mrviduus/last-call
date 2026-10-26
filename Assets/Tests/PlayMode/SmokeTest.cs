using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SmokeTest
{
    // Unattended run in the bar: furniture and seated couples in place; once the fight starts the patrons get up
    // and hit the idle hero, with no errors logged.
    [UnityTest]
    public IEnumerator BarFurnitureAndEnemiesReachTheHero()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(36, Game.Props.Count, "stools, tables, chairs, bottles, pool tables with balls and cues");
        Assert.AreEqual(8, Game.Fighters.FindAll(f => f.state == State.Sit).Count, "a couple at each round table and one at the counter");

        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
        Assert.AreEqual(8, Game.Fighters.FindAll(f => f.state == State.Sit).Count, "nobody gets up before the fight starts");

        Game.Brawl = true; // as if the hero threw the first punch
        for (int i = 0; i < 1500; i++) yield return new WaitForFixedUpdate();
        Assert.IsFalse(Game.Fighters.Exists(f => f.state == State.Sit), "the whole bar joined the fight");
        Assert.Less(Game.Hero.health, Game.Hero.maxHealth, "enemies reached and hit the hero");

        Camera.main.Render(); // batchmode has no end-of-frame, so draw the pixel screen by hand
        var tex = new Texture2D(Game.ScreenW, Game.ScreenH);
        RenderTexture.active = Game.PixelScreen;
        tex.ReadPixels(new Rect(0, 0, Game.ScreenW, Game.ScreenH), 0, 0);
        RenderTexture.active = null;
        Directory.CreateDirectory("Logs");
        File.WriteAllBytes("Logs/smoke.png", tex.EncodeToPNG());
        Object.Destroy(tex);
    }
}
