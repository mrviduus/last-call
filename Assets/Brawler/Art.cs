using System.Collections.Generic;
using UnityEngine;

// Fighters and the street: "Streets of Fight" by ansimuz, CC0 (see CREDITS.txt). Variants are tinted copies.
// Rooftop stage, whip and giant boot are drawn in code. 1 texel = 1 screen pixel.
public static class Art
{
    // a look is "<character>" or "<character>:<tint>"; characters are folders under Resources/Art
    public const string Hero = "Vasyl";          // the punk recoloured: blond hair, leather vest (until his own frames)
    public const string GruntA = "Girl";          // street fighter
    public const string GruntB = "Girl:c8a0ff";   // tougher fighter, violet tint
    public const string Lasher = "Girl:b0ffd8";   // whip fighter, teal tint

    public static Color Tint(string look)
    {
        int i = look.IndexOf(':');
        return i >= 0 && ColorUtility.TryParseHtmlString("#" + look.Substring(i + 1), out var c) ? c : Color.white;
    }

    public class Frames
    {
        public Sprite[] Idle, Walk, Throw, Spin, Sinking;
        public Sprite Punch1, Punch2, KickWind, Kick, KickRecover, JumpKick, Hurt, Fall, Down, GetUp, Crouch, Air, WindUp, Headbutt;
        public Vector2 KickTip; // toe of the kick frame, relative to the feet
    }

    const int FrameW = 96; // every sheet is a row of 96 px wide frames
    static readonly Dictionary<string, Frames> cache = new Dictionary<string, Frames>();
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static Frames For(string look)
    {
        string name = look.Split(':')[0];
        if (cache.TryGetValue(name, out var f)) return f;
        f = name == "Girl" ? Girl() : Punk(name == "Vasyl");
        f.Sinking = Sinking(f.Hurt);
        f.KickTip = Tip(f.Kick);
        return cache[name] = f;
    }

    // full move set
    static Frames Girl()
    {
        var pivot = FeetPivot(Load("Girl/idle"), FrameW);
        Sprite[] S(string sheet) => Sheet("Girl/" + sheet, pivot);
        var jab = S("jab"); var punch = S("punch"); var kick = S("kick"); var jump = S("jump");
        var jumpKick = S("jump_kick"); var dive = S("dive_kick"); var hurt = S("hurt");
        return new Frames
        {
            Idle = S("idle"), Walk = S("walk"),
            Punch1 = jab[1], Punch2 = punch[1], KickWind = kick[1], Kick = kick[3], KickRecover = kick[4], JumpKick = dive[3],
            Hurt = hurt[0], Fall = hurt[1], Down = LyingDown(hurt[1]),
            GetUp = jump[0], Crouch = jump[0], Air = jump[2],
            WindUp = punch[0], Headbutt = jab[2], Throw = new[] { punch[0], punch[2] },
            Spin = new[] { dive[1], jumpKick[1] },
        };
    }

    // only idle, walk, punch and hurt; the last hurt frame lies on the ground
    static Frames Punk(bool asVasyl)
    {
        var pivot = FeetPivot(Load("Punk/idle"), FrameW);
        Sprite[] S(string sheet) => Sheet("Punk/" + sheet, pivot, asVasyl);
        var idle = S("idle"); var punch = S("punch"); var hurt = S("hurt");
        return new Frames
        {
            Idle = idle, Walk = S("walk"),
            Punch1 = punch[2], Punch2 = punch[2], KickWind = punch[1], Kick = punch[2], KickRecover = idle[0], JumpKick = punch[2],
            Hurt = hurt[1], Fall = hurt[2], Down = hurt[3],
            GetUp = idle[0], Crouch = idle[0], Air = idle[0],
            WindUp = punch[1], Headbutt = punch[2], Throw = new[] { punch[1], punch[2] },
            Spin = new[] { punch[2], punch[2] },
        };
    }

    // the right-most opaque pixel of a frame (the toe of a kick), relative to the sprite's pivot
    static Vector2 Tip(Sprite s)
    {
        var r = s.rect;
        var px = s.texture.GetPixels32();
        for (int x = (int)r.width - 1; x >= 0; x--)
            for (int y = 0; y < (int)r.height; y++)
                if (px[((int)r.y + y) * s.texture.width + (int)r.x + x].a > 0)
                    return new Vector2(x, y) - s.pivot;
        return Vector2.zero;
    }

    static Texture2D Load(string path) => Resources.Load<Texture2D>("Art/" + path);

    static Sprite[] Sheet(string path, Vector2 pivot, bool asVasyl = false)
    {
        var tex = asVasyl ? RecolorAsVasyl(Load(path)) : Load(path);
        var frames = new Sprite[tex.width / FrameW];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(tex, new Rect(i * FrameW, 0, FrameW, tex.height), pivot, 1f, 0, SpriteMeshType.FullRect);
        return frames;
    }

    // the punk's reds, by where they are: hair at the top of the body, vest below it
    static readonly Dictionary<int, Color32> BlondHair = new Dictionary<int, Color32>
    {
        [0xce2038] = C(0xf0d070), [0xed4924] = C(0xfff0a0), [0x8a0b41] = C(0xb08830),
    };
    static readonly Dictionary<int, Color32> LeatherVest = new Dictionary<int, Color32>
    {
        [0xce2038] = C(0x4a3a32), [0xed4924] = C(0x6a5248), [0x8a0b41] = C(0x2a201c),
    };
    const int HairRows = 10; // rows from the top of each frame's body that are hair

    static Texture2D RecolorAsVasyl(Texture2D src)
    {
        var px = src.GetPixels32();
        for (int f = 0; f < src.width / FrameW; f++)
        {
            int top = -1; // highest opaque row of this frame (rows count up from the bottom)
            for (int y = src.height - 1; y >= 0 && top < 0; y--)
                for (int x = f * FrameW; x < (f + 1) * FrameW; x++)
                    if (px[y * src.width + x].a > 0) { top = y; break; }
            for (int y = 0; y < src.height; y++)
                for (int x = f * FrameW; x < (f + 1) * FrameW; x++)
                {
                    ref var c = ref px[y * src.width + x];
                    var map = y > top - HairRows ? BlondHair : LeatherVest;
                    if (c.a > 0 && map.TryGetValue(c.r << 16 | c.g << 8 | c.b, out var to)) c = to;
                }
        }
        var tex = new Texture2D(src.width, src.height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // pivot under the feet: lowest opaque row, centred on the body (first frame of the sheet)
    static Vector2 FeetPivot(Texture2D tex, int frameW)
    {
        var px = tex.GetPixels32();
        int minX = frameW, maxX = -1, bottom = tex.height;
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < frameW; x++)
                if (px[y * tex.width + x].a > 0)
                {
                    bottom = Mathf.Min(bottom, y);
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                }
        return new Vector2((minX + maxX + 1) / 2f / frameW, bottom / (float)tex.height);
    }

    // a frame turned 90 degrees counter-clockwise: lying on the back, head behind (no lying frame in the sheet)
    static Sprite LyingDown(Sprite standing)
    {
        var r = standing.rect;
        int w = (int)r.width, h = (int)r.height;
        var src = standing.texture.GetPixels((int)r.x, (int)r.y, w, h);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[x * h + (h - 1 - y)] = src[y * w + x];
        var tex = new Texture2D(h, w) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, h, w), FeetPivot(tex, h), 1f, 0, SpriteMeshType.FullRect);
    }

    // the hurt pose with its bottom rows cut off, deeper each frame: a body hammered into the ground
    static Sprite[] Sinking(Sprite hurt)
    {
        var frames = new Sprite[(int)(Feel.BurySink / 3f) + 1];
        var r = hurt.rect;
        var pivot = new Vector2(hurt.pivot.x / r.width, 0f);
        for (int i = 0; i < frames.Length; i++)
        {
            float cut = i * 3f;
            frames[i] = Sprite.Create(hurt.texture, new Rect(r.x, r.y + cut, r.width, r.height - cut),
                pivot, 1f, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }

    static Sprite FromResource(string path)
    {
        var tex = Load(path);
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

    static Sprite bigBoot;

    // oversized boot for finishing kicks: shaft on the left, toe pointing right, pivot at the ankle
    public static Sprite BigBoot => bigBoot != null ? bigBoot : bigBoot = Paint(24, 14, (x, y) =>
    {
        bool inShaft = x < 10 && y < 8;
        bool inFoot = y >= 6 && !(x > 20 && y < 9); // rounded toe
        if (!inShaft && !inFoot) return Clear;
        bool edge = x == 0 || y == 0 && x < 10 || x == 23 || y == 13 || x == 10 && y < 6 || x > 20 && y == 9;
        if (edge) return C(0x1a1020);
        if (y >= 11) return C(0x3a3a44);                                          // sole
        if ((y == 7 || y == 9) && x >= 3 && x <= 15 && x % 3 == 0) return C(0xffffff); // laces
        return y < 3 || x < 2 ? C(0x801c14) : C(0xd03020);                        // shaded red leather
    }, new Vector2(0f, 0.5f));


    // 1200x240 street: buildings on top, sidewalk and road below
    public static Sprite StreetScene => streetScene != null ? streetScene : streetScene = FromResource("Street/stage");

    public static Sprite Shadow => shadow != null ? shadow : shadow = FromResource("shadow");

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
    public static Sprite Roof => roof != null ? roof : roof = Paint(32, 132, (x, y) =>
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
