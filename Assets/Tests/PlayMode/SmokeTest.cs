using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SmokeTest
{
    // Unattended run: the bar loads with the hero by the counter and its chairs and bottles, no errors logged.
    [UnityTest]
    public IEnumerator BarLoadsWithHeroAndProps()
    {
        if (Object.FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();

        Assert.IsTrue(Game.Hero.Alive);
        Assert.IsFalse(Game.Fighters.Exists(f => f.team != 0), "no enemies in this step");
        Assert.AreEqual(27, Game.Props.Count, "stools, tables, chairs and bottles are in place");

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
