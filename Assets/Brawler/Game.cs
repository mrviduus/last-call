using System.Collections.Generic;
using UnityEngine;

// Boots itself in any scene: 60 Hz fixed loop, 256x224 pixel screen, floor, player, enemy waves.
public class Game : MonoBehaviour
{
    public const int ScreenW = 256, ScreenH = 224;
    public const float MinX = 8f, MinDepth = -100f, MaxDepth = -40f;

    public static readonly List<Fighter> Fighters = new List<Fighter>();
    public static Fighter Hero;
    public static int Frame;
    public static bool RestartRequested;
    public static RenderTexture PixelScreen;
    static int freezeFrames, shakeFrames;

    static Sprite square;
    public static Sprite Square => square != null ? square : square = MakeSquare();

    Camera cam;
    float camX = ScreenW / 2f;
    int wave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
    }

    static Sprite MakeSquare()
    {
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    public static void Reset()
    {
        Fighters.Clear();
        Hero = null;
        Frame = freezeFrames = shakeFrames = 0;
        RestartRequested = false;
    }

    public static void Freeze(int frames, int shake)
    {
        freezeFrames = Mathf.Max(freezeFrames, frames);
        shakeFrames = Mathf.Max(shakeFrames, shake);
    }

    // one 60 Hz tick of the whole fight; hitstop skips it
    public static void Simulate()
    {
        Frame++;
        if (shakeFrames > 0) shakeFrames--;
        if (freezeFrames > 0) { freezeFrames--; return; }
        for (int i = 0; i < Fighters.Count; i++) Fighters[i].Step();
    }

    public static Fighter Spawn(int team, Color c, Vector2 pos, int hp)
    {
        var f = new GameObject(team == 0 ? "Player" : "Enemy").AddComponent<Fighter>();
        f.maxHealth = hp;
        f.Init(team, c, pos);
        Fighters.Add(f);
        return f;
    }

    void Awake()
    {
        Time.fixedDeltaTime = 1f / 60f;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        PixelScreen = new RenderTexture(ScreenW, ScreenH, 16) { filterMode = FilterMode.Point };
        cam = Camera.main;
        if (cam == null) cam = new GameObject("Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ScreenH / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.1f, 0.25f);
        cam.targetTexture = PixelScreen;

        // the window itself needs a camera to clear it; this one draws nothing
        var bars = new GameObject("Letterbox").AddComponent<Camera>();
        bars.cullingMask = 0;
        bars.clearFlags = CameraClearFlags.SolidColor;
        bars.backgroundColor = Color.black;
        bars.depth = -100;

        MakeRect(new Vector2(1900f, -74f), new Vector2(4200f, 84f), new Color(0.3f, 0.25f, 0.2f), -20000);
        MakeRect(new Vector2(1900f, 0f), new Vector2(4200f, 64f), new Color(0.22f, 0.18f, 0.3f), -20000);
        for (int x = 0; x < 4000; x += 64) // markers so scrolling is visible
            MakeRect(new Vector2(x, -74f), new Vector2(2f, 84f), new Color(0.25f, 0.2f, 0.15f), -19999);

        Restart();
    }

    static void MakeRect(Vector2 pos, Vector2 scale, Color c, int order)
    {
        var sr = new GameObject("Rect").AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        sr.color = c;
        sr.sortingOrder = order;
        sr.transform.position = pos;
        sr.transform.localScale = scale;
    }

    void Restart()
    {
        foreach (var f in Fighters) if (f != null) Destroy(f.gameObject);
        Reset();
        wave = 0;
        camX = ScreenW / 2f;
        Hero = Spawn(0, new Color(0.3f, 0.8f, 0.3f), new Vector2(40f, -70f), 10);
        Hero.gameObject.AddComponent<Player>();
    }

    void SpawnWave()
    {
        wave++;
        for (int i = 0; i < Mathf.Min(1 + wave, 5); i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            var pos = new Vector2(camX + side * (ScreenW / 2f + 20f), Random.Range(MinDepth, MaxDepth));
            var e = Spawn(1, Color.HSVToRGB(Random.Range(0f, 0.1f), 0.7f, 0.8f), pos, 3);
            e.gameObject.AddComponent<Enemy>();
        }
    }

    void FixedUpdate()
    {
        if (RestartRequested) Restart();
        Simulate();

        for (int i = Fighters.Count - 1; i >= 0; i--)
        {
            var f = Fighters[i];
            if (f.team != 0 && f.state == State.Dead && f.stateFrames > Feel.CorpseFrames)
            {
                Fighters.RemoveAt(i);
                Destroy(f.gameObject);
            }
        }
        if (Hero.Alive && !Fighters.Exists(f => f.team != 0)) SpawnWave();

        camX = Mathf.Max(ScreenW / 2f, camX + (Hero.ground.x - camX) * 0.1f);
    }

    void LateUpdate()
    {
        float shake = shakeFrames > 0 ? (Frame % 2 == 0 ? 2f : -2f) : 0f;
        cam.transform.position = new Vector3(Mathf.Round(camX), shake, -10f);
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            int scale = Mathf.Max(1, Mathf.Min(Screen.width / ScreenW, Screen.height / ScreenH));
            int w = ScreenW * scale, h = ScreenH * scale;
            GUI.DrawTexture(new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h), PixelScreen);
        }
        GUI.Label(new Rect(10, 10, 400, 25), $"HP {Mathf.Max(Hero.health, 0)}   Wave {wave}");
        if (Hero.state == State.Dead) GUI.Label(new Rect(10, 35, 400, 25), "GAME OVER - press J to restart");
    }
}
