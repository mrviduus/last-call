using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SmokeTest
{
    // Unattended run in the bar: furniture in place, enemies walk in from an entrance and hit the idle hero,
    // with no errors logged.
    [UnityTest]
    public IEnumerator BarFurnitureAndEnemiesReachTheHero()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(27, Game.Props.Count, "stools, tables, chairs and bottles are in place");

        for (int i = 0; i < 1500; i++) yield return new WaitForFixedUpdate();
        Assert.IsTrue(Game.Fighters.Exists(f => f.team != 0), "enemies came in");
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
