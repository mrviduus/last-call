using System.Collections.Generic;
using UnityEngine;

// Fighters and the street: "Streets of Fight" by ansimuz, CC0 (see CREDITS.txt). Variants are tinted copies.
// Space bar stage, chair, bottle, whip and giant boot are drawn in code. 1 texel = 1 screen pixel.
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
    // the punk's sheets face left (he was drawn as an enemy); mirrored so every character faces right
    static Frames Punk(bool asVasyl)
    {
        var pivot = FeetPivot(Mirror(Load("Punk/idle")), FrameW);
        Sprite[] S(string sheet) => Sheet("Punk/" + sheet, pivot, asVasyl, mirror: true);
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

    static Sprite[] Sheet(string path, Vector2 pivot, bool asVasyl = false, bool mirror = false)
    {
        var tex = asVasyl ? RecolorAsVasyl(Load(path)) : Load(path);
        if (mirror) tex = Mirror(tex);
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

    // flips every 96 px frame left-right in place
    static Texture2D Mirror(Texture2D src)
    {
        var px = src.GetPixels32();
        var outPx = new Color32[px.Length];
        for (int y = 0; y < src.height; y++)
            for (int x = 0; x < src.width; x++)
            {
                int frame = x / FrameW * FrameW;
                outPx[y * src.width + x] = px[y * src.width + frame + FrameW - 1 - (x - frame)];
            }
        var tex = new Texture2D(src.width, src.height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(outPx);
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

    static Sprite streetScene, shadow, whip, spaceBar, chair, bottle;

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

    // space bar, our own design: window onto space with a planet, neon, a shelf of glowing bottles,
    // the counter, then a metal floor (the walkable area starts at row 112). Tiles horizontally.
    public static Sprite SpaceBar => spaceBar != null ? spaceBar : spaceBar = Paint(256, 240, (x, y) =>
    {
        if (y >= 8 && y < 64 && x >= 24 && x < 232)
        {
            if (y == 8 || y == 63 || x == 24 || x == 231 || x == 128) return C(0x5a6078); // window frame
            float dx = x - 190, dy = y - 70;
            if (dx * dx + dy * dy < 46 * 46) return Mix(0x2a8c8c, 0x0c3a4a, (y - 24) / 46f); // planet
            if (Speckle(x * 5, y * 3) && Speckle(x, y * 7)) return C(0xe8f0ff);             // stars
            return Mix(0x050514, 0x14102e, y / 64f);
        }
        if (y < 70)
        {
            if (x % 32 == 0 || y % 34 == 0) return C(0x10121c);                // wall panel seams
            return x % 32 == 2 && y % 17 == 3 ? C(0x3a3e52) : C(0x1c2030);     // rivets
        }
        if (y < 72) return x % 6 < 4 ? C(0xff4fd0) : C(0x401436);              // neon tube
        if (y < 92)
        {
            if (y == 91) return C(0x6a6e84);                                   // shelf
            int b = x / 9, bx = x % 9, top = 76 + b * 7 % 5;                   // bottles of different heights
            if (bx >= 2 && bx <= 6 && y >= top && (y >= top + 3 || bx == 4))
            {
                uint[] glass = { 0x3ae0a0, 0xe0a03a, 0xa03ae0, 0x3aa0e0 };
                return C(glass[b % 4]);
            }
            return C(0x181a28);
        }
        if (y < 96) return C(0xb0b4c8);                                         // counter top
        if (y < 108) return y == 104 ? C(0x3ae0ff) : C(0x2a2e44);              // counter front, cyan strip
        if (y < 112) return C(0x0c0e16);                                        // shadow under the counter
        if (y % 16 == 0 || (x + y / 16 * 8) % 48 == 0) return C(0x2a2c3c);     // floor plates
        return Speckle(x, y) ? C(0x40445a) : C(0x363a4e);
    }, Center);

    // bar stool: red cushions, metal legs and footrest; pivot at the feet
    public static Sprite Chair => chair != null ? chair : chair = Paint(20, 28, (x, y) =>
    {
        if (y < 4) return x >= 3 && x <= 16 ? C(0xc0303a) : Clear;               // backrest cushion
        if (y < 12) return x == 3 || x == 16 ? C(0x8a8ea4) : Clear;              // backrest posts
        if (y < 15) return C(0xc0303a);                                          // seat
        if (y == 15) return C(0x7a1c24);
        if (y == 22 && x >= 2 && x <= 17) return C(0x6a6e84);                    // footrest
        return x == 2 || x == 3 || x == 16 || x == 17 ? C(0x8a8ea4) : Clear;    // legs
    }, new Vector2(0.5f, 0f));

    // green glass bottle; pivot at the bottom
    public static Sprite Bottle => bottle != null ? bottle : bottle = Paint(6, 16, (x, y) =>
    {
        if (y < 5) return x == 2 || x == 3 ? C(0x2a8a4a) : Clear;                // neck
        return x == 1 ? C(0x9af0b0) : C(0x2a8a4a);                               // body with a highlight
    }, new Vector2(0.5f, 0f));
}
