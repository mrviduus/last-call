using System.Collections.Generic;
using UnityEngine;

// Fighters and the street use CC0 art by Chasersgaming (see CREDITS.txt); fighters are palette-swapped per role.
// Rooftop stage, shadow and whip are drawn in code. 1 texel = 1 screen pixel.
public static class Art
{
    // palette swap keys: H hair, T shirt, t shirt shade, P pants, p pants shade, S skin, s skin shade
    public const string Hero = "Hf0d070 T4a3a32 t2e241f";                                    // VOLT: blond, leather jacket
    public const string GruntA = "H9030b0 Tc07820 t8a5414 P4a4a52 p34343a";                  // punk
    public const string GruntB = "Hd0d0d0 T3a4a8a t283460 P6a3a2a p4a2818 Sc08860 s8a5030";  // bruiser
    public const string Lasher = "Hd04060 T208080 t145454 P1a6a6a p104848";                  // whip fighter

    // which palette key each colour of the source sprites belongs to (0xRRGGBB)
    static readonly Dictionary<int, char> SourceKeys = new Dictionary<int, char>
    {
        [0x550000] = 'H', [0x5a1a00] = 'H',
        [0xffffff] = 'T', [0xaaaaaa] = 't',
        [0x0055aa] = 'P', [0x0000aa] = 'p',
        [0xffaa55] = 'S', [0xff5500] = 's',
    };

    public class Frames
    {
        public Sprite[] Idle, Walk;
        public Sprite Punch1, Punch2, Kick, Hurt, Fall, GetUp, Crouch, WindUp;
    }

    static readonly Dictionary<string, Frames> cache = new Dictionary<string, Frames>();
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static Frames For(string palette)
    {
        if (cache.TryGetValue(palette, out var f)) return f;
        var pal = ParsePalette(palette);
        f = new Frames
        {
            Idle = Strip("Idle", 4, pal),
            Walk = Strip("Walk", 4, pal),
            Punch1 = Strip("Punch1", 1, pal)[0],
            Punch2 = Strip("Punch2", 1, pal)[0],
            Kick = Strip("Kick", 1, pal)[0],
            Hurt = Strip("Hurt", 1, pal)[0],
            Fall = Strip("Fall", 1, pal, 0.5f)[0],
            GetUp = Strip("GetUp", 1, pal)[0],
            Crouch = Strip("Crouch", 1, pal)[0],
            WindUp = Strip("HeadButt", 2, pal)[0],
        };
        return cache[palette] = f;
    }

    static Dictionary<char, Color32> ParsePalette(string palette)
    {
        var pal = new Dictionary<char, Color32>();
        foreach (var entry in palette.Split(' '))
            if (ColorUtility.TryParseHtmlString("#" + entry.Substring(1), out var c)) pal[entry[0]] = c;
        return pal;
    }

    // equal-width frames from a horizontal strip in Resources/Art/Fighter, recoloured.
    // Frames face right; wide frames keep the body on the left with the limb reaching right.
    static Sprite[] Strip(string name, int count, Dictionary<char, Color32> pal, float? pivotX = null)
    {
        var src = Resources.Load<Texture2D>("Art/Fighter/" + name);
        var px = src.GetPixels32();
        for (int i = 0; i < px.Length; i++)
            if (SourceKeys.TryGetValue(px[i].r << 16 | px[i].g << 8 | px[i].b, out char k) && pal.TryGetValue(k, out var c))
                px[i] = new Color32(c.r, c.g, c.b, px[i].a);
        var tex = new Texture2D(src.width, src.height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();

        int w = src.width / count;
        var pivot = new Vector2(pivotX ?? (w > 16 ? 8f / w : 0.5f), 0f);
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = Sprite.Create(tex, new Rect(i * w, 0, w, src.height), pivot, 1f, 0, SpriteMeshType.FullRect);
        return frames;
    }

    static Sprite FromResource(string path)
    {
        var tex = Resources.Load<Texture2D>(path);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Center, 1f, 0, SpriteMeshType.FullRect);
    }

    // f(x, y) with y counted from the top
    public static Sprite Paint(int w, int h, System.Func<int, int, Color32> f, Vector2 pivot)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[(h - 1 - y) * w + x] = f(x, y);
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 1f, 0, SpriteMeshType.FullRect);
    }

    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static Color32 C(uint rgb, byte a = 255) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);
    static Color32 Mix(uint a, uint b, float t) => Color32.Lerp(C(a), C(b), t);
    static bool Speckle(int x, int y) => ((x * 73856093) ^ (y * 19349663)) % 7 == 0;

    static Sprite streetScene, shadow, whip, nightSky, skyline, roof, neon, tank;

    // 784x192 street: buildings on top, sidewalk and road along the bottom 64 px
    public static Sprite StreetScene => streetScene != null ? streetScene : streetScene = FromResource("Art/Street/panorama");

    public static Sprite Shadow => shadow != null ? shadow : shadow = Paint(18, 6, (x, y) =>
    {
        float dx = (x - 8.5f) / 9f, dy = (y - 2.5f) / 3f;
        return dx * dx + dy * dy <= 1f ? C(0x000000, 90) : Clear;
    }, Center);

    // pivot at the handle end; flip by negative x scale
    public static Sprite Whip => whip != null ? whip : whip = Paint(44, 3, (x, y) =>
        x >= 40 ? C(0xe0c080) : y == 1 ? C(0x6a3a1c) : Clear, new Vector2(0f, 0.5f));

    public static Sprite NightSky => nightSky != null ? nightSky : nightSky = Paint(32, 144, (x, y) =>
        y < 100 && Speckle(x * 3 + 1, y * 5) && Speckle(x + 7, y) ? C(0xe8e8ff) : Mix(0x0c0c24, 0x3a1a4a, y / 143f), Center);

    // distant buildings with a few lit windows; transparent sky above them
    public static Sprite Skyline => skyline != null ? skyline : skyline = Paint(128, 72, (x, y) =>
    {
        int height = 24 + x / 16 * 37 % 5 * 9;
        if (72 - y > height) return Clear;
        if (x % 16 == 0) return C(0x120e26);
        return x % 4 == 2 && y % 6 == 3 && (x * 7 + y * 3) % 5 == 0 ? C(0xf0d070) : C(0x1c1636);
    }, Center);

    // concrete ledge at the far edge, then tar and gravel
    public static Sprite Roof => roof != null ? roof : roof = Paint(32, 84, (x, y) =>
    {
        if (y < 2) return C(0x9a9aa8);
        if (y < 6) return C(0x6a6a78);
        if (y < 8) return C(0x24202a);
        if (x == 31) return C(0x2e2a34);
        return Speckle(x, y) ? C(0x4a4652) : C(0x3a3640);
    }, Center);

    public static Sprite Neon => neon != null ? neon : neon = Paint(56, 22, (x, y) =>
    {
        bool frame = x == 3 || x == 52 || y == 3 || y == 18;
        bool squiggle = x > 6 && x < 50 && y >= 8 && y <= 13 && x % 8 < 4 && (y == 8 || y == 13 || x % 8 == 0);
        if ((frame && x >= 3 && x <= 52 && y >= 3 && y <= 18) || squiggle) return C(0xff4fd0);
        return x < 2 || x > 53 || y < 2 || y > 19 ? C(0x101018) : C(0x1a1424);
    }, Center);

    public static Sprite Tank => tank != null ? tank : tank = Paint(28, 44, (x, y) =>
    {
        if (y < 4) return x > 3 && x < 24 ? C(0x3a2a24) : Clear;
        if (y < 30) return x == 0 || x == 27 ? C(0x1a1020) : y % 9 == 0 ? C(0x2a2a2a) : C(0x6a4a30);
        return x < 4 || x > 23 || x == 13 || x == 14 ? C(0x2a2a30) : Clear;
    }, Center);
}
