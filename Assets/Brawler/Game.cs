using System.Collections.Generic;
using UnityEngine;

// Boots itself in any scene: camera, floor, player, endless enemy waves.
public class Game : MonoBehaviour
{
    public const float MinDepth = -3f, MaxDepth = 0f;

    public static Sprite Square;
    public static Fighter Hero;
    public static readonly List<Fighter> Enemies = new List<Fighter>();

    Camera cam;
    int wave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => new GameObject("Game").AddComponent<Game>();

    void Awake()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        cam = Camera.main;
        if (cam == null) cam = new GameObject("Camera").AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 3.5f;
        cam.backgroundColor = new Color(0.15f, 0.1f, 0.25f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        MakeRect(new Vector2(500f, -1.5f), new Vector2(2000f, 3.6f), new Color(0.3f, 0.25f, 0.2f), -20000);
        for (int x = -40; x < 1000; x += 4) // markers so scrolling is visible
            MakeRect(new Vector2(x, -1.5f), new Vector2(0.1f, 3.6f), new Color(0.25f, 0.2f, 0.15f), -19999);

        Enemies.Clear();
        Hero = Spawn("Player", new Color(0.3f, 0.8f, 0.3f), new Vector2(0f, -1.5f), 10, 4f);
        Hero.gameObject.AddComponent<Player>();
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

    static Fighter Spawn(string name, Color c, Vector2 pos, int hp, float speed)
    {
        var f = new GameObject(name).AddComponent<Fighter>();
        f.maxHealth = hp;
        f.speed = speed;
        f.Init(c, pos);
        return f;
    }

    void SpawnWave()
    {
        wave++;
        for (int i = 0; i < Mathf.Min(1 + wave, 6); i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            var pos = new Vector2(Hero.ground.x + side * Random.Range(6f, 9f), Random.Range(MinDepth, MaxDepth));
            var e = Spawn("Enemy", Color.HSVToRGB(Random.Range(0f, 0.1f), 0.7f, 0.8f), pos, 3, 2.5f);
            e.gameObject.AddComponent<Enemy>();
            Enemies.Add(e);
        }
    }

    void Update()
    {
        Enemies.RemoveAll(e => e == null);
        if (Enemies.Count == 0 && !Hero.Dead) SpawnWave();

        float x = Mathf.Lerp(cam.transform.position.x, Hero.ground.x, 5f * Time.deltaTime);
        cam.transform.position = new Vector3(x, -0.5f, -10f);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 25), $"HP {Mathf.Max(Hero.health, 0)}   Wave {wave}");
        if (Hero.Dead) GUI.Label(new Rect(10, 35, 400, 25), "GAME OVER - press J to restart");
    }
}
