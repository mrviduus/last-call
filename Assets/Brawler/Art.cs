using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Original placeholder pixel art, built at startup. 1 char / 1 texel = 1 screen pixel.
public static class Art
{
    // palette keys: K outline, H hair, S skin, s skin shade, W eye, A arms/sleeves,
    // T shirt, t shirt shade, P pants, p pants shade, O boots/belt
    // VOLT: short blond hair, leather jacket over a white tee, jeans
    public const string Hero = "K1a1020 Hf0d070 Sf0b890 sc88860 Wffffff A3a2a26 Te8e8e8 tb8b8c0 P3050a0 p203878 O2a1a14";
    public const string GruntA = "K1a1020 H9030b0 Sd09070 sa06040 Wffffff Ad09070 Tc07820 t8a5414 P4a4a52 p34343a O202020";
    public const string GruntB = "K1a1020 Hd0d0d0 S8a5a40 s6a4028 Wffffff A8a5a40 T3a4a8a t283460 P6a3a2a p4a2818 O181818";
    // whip fighter: crimson ponytail, teal jumpsuit
    public const string Lasher = "K1a1020 Hd04060 Se0a080 sb07858 Wffffff A206060 T206060 t144444 P206060 p144444 O101010";

    public static readonly string[] PonytailHead =
    {
        "......KKKK......",
        ".....KHHHHK.....",
        "....KHHHHHHK....",
        "..HHKHHHHHHK....",
        ".HHHKSSSSKWK....",
        ".HH.KSSSSSSK....",
        ".....KSSSSK.....",
        "......KssK......",
    };

    public static readonly string[] PunkHead =
    {
        ".......HH.......",
        "......HHHH......",
        ".....KHHHHK.....",
        "....KSSSSSSK....",
        "....KSSSSKWK....",
        "....KSSSSSSK....",
        ".....KSSssK.....",
        "......KssK......",
    };

    public static readonly string[] BruiserHead =
    {
        "......KKKK......",
        ".....KSSSSK.....",
        "....KSSSSSSK....",
        "....KSSSSSSK....",
        "....KSSSKKWK....",
        "....KHSSSSSK....",
        ".....KHHHHK.....",
        "......KHHK......",
    };

    public static readonly string[] HeroHead =
    {
        "......KKKK......",
        ".....KHHHHK.....",
        "....KHHHHHHK....",
        "....KHHHHHHK....",
        "....KHSSSKWK....",
        "....KSSSSSSK....",
        ".....KSSSSK.....",
        "......KssK......",
    };

    static readonly string[] TorsoIdle =
    {
        "....KKTTTTKK....",
        "...KAKTTTTKAK...",
        "..KAAKTTTTKAAK..",
        "..KAAKtTTtKAAK..",
        "..KAAKtTTtKAAK..",
        "..KAAKTTTTKAAK..",
        "..KSSKTTTTKSSK..",
        "...KKKOOOOKKK...",
    };

    static readonly string[] TorsoPunch =
    {
        "....KKTTTTKK....",
        "...KAKTTTTKKKKK.",
        "..KAAKTTTTAAAASK",
        "..KAAKtTTtKKKKK.",
        "..KAAKtTTtK.....",
        "...KSKTTTTK.....",
        "...KKKTTTTK.....",
        "...KKKOOOOKK....",
    };

    static readonly string[] TorsoHurt =
    {
        "KSK.KKTTTTKK.KSK",
        "KAAKKTTTTTTKKAAK",
        ".KAAKTTTTTTKAAK.",
        "..KKKtTTTTtKKK..",
        "....KtTTTTtK....",
        "....KTTTTTTK....",
        "....KTTTTTTK....",
        "...KKKOOOOKKK...",
    };

    static readonly string[] LegsStand =
    {
        "....KPPPPPPPK...",
        "....KPPPPPPPK...",
        "....KPPKKKPPK...",
        "....KPPK.KPPK...",
        "....KPPK.KPPK...",
        "....KPPK.KPPK...",
        "....KPpK.KPpK...",
        "....KPpK.KPpK...",
        "....KPpK.KPpK...",
        "...KOOOK.KOOOOK.",
        "...KOOOK.KOOOOK.",
        "...KKKKK.KKKKKK.",
    };

    static readonly string[] LegsWalk =
    {
        "....KPPPPPPK....",
        "....KPPPPPPK....",
        "...KPPPKKPPPK...",
        "...KPPK..KPPK...",
        "..KPPK....KPPK..",
        "..KPpK....KPpK..",
        ".KPpK......KPpK.",
        ".KPpK......KPpK.",
        ".KPpK......KPpK.",
        "KOOOK......KOOOK",
        "KOOOK......KOOOK",
        "KKKKK......KKKKK",
    };

    static readonly string[] LegsAir =
    {
        "....KPPPPPPPK...",
        "....KPPPPPPPPK..",
        "....KPPKKPPPPPK.",
        "....KPpK.KKPPPK.",
        "....KPpK...KOOK.",
        "...KOOOK...KOOK.",
        "...KOOOK...KKKK.",
        "...KKKKK........",
        "................",
        "................",
        "................",
        "................",
    };

    static readonly string[] BigFist =
    {
        "...KKKKKK...",
        "..KSSSSSSK..",
        ".KSSKSSKSSK.",
        "KSSSKSSKSSSK",
        "KSSSSSSSSSSK",
        "KSSSSSSSSSSK",
        "KsSSSSSSSSsK",
        ".KsSSSSSSsK.",
        "..KssssssK..",
        "...KKKKKK...",
    };

    public class Frames
    {
        public Sprite Stand, Walk, Punch, Hurt, Air, Fist;
    }

    static readonly Dictionary<string, Frames> cache = new Dictionary<string, Frames>();
    static readonly Vector2 Feet = new Vector2(0.5f, 0f), Center = new Vector2(0.5f, 0.5f);

    public static Frames For(string palette, string[] head)
    {
        string key = palette + string.Concat(head);
        if (cache.TryGetValue(key, out var f)) return f;
        var pal = ParsePalette(palette);
        f = new Frames
        {
            Stand = FromRows(Join(head, TorsoIdle, LegsStand), pal, Feet),
            Walk = FromRows(Join(head, TorsoIdle, LegsWalk), pal, Feet),
            Punch = FromRows(Join(head, TorsoPunch, LegsStand), pal, Feet),
            Hurt = FromRows(Join(head, TorsoHurt, LegsStand), pal, Feet),
            Air = FromRows(Join(head, TorsoIdle, LegsAir), pal, Feet),
            Fist = FromRows(BigFist, pal, Center),
        };
        return cache[key] = f;
    }

    static string[] Join(params string[][] parts) => parts.SelectMany(p => p).ToArray();

    static Dictionary<char, Color32> ParsePalette(string palette)
    {
        var pal = new Dictionary<char, Color32>();
        foreach (var entry in palette.Split(' '))
            if (ColorUtility.TryParseHtmlString("#" + entry.Substring(1), out var c)) pal[entry[0]] = c;
        return pal;
    }

    static Sprite FromRows(string[] rows, Dictionary<char, Color32> pal, Vector2 pivot)
    {
        int w = rows.Max(r => r.Length);
        return Paint(w, rows.Length, (x, y) =>
            x < rows[y].Length && pal.TryGetValue(rows[y][x], out var c) ? c : Clear, pivot);
    }

    // f(x, y) with y counted from the top, like the ASCII rows
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
    static bool Speckle(int x, int y) => ((x * 73856093) ^ (y * 19349663)) % 7 == 0;

    static Sprite shadow, bricks, street, window, litWindow, whip, nightSky, skyline, roof, neon, tank;

    static Color32 Mix(uint a, uint b, float t) => Color32.Lerp(C(a), C(b), t);

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

    public static Sprite Shadow => shadow != null ? shadow : shadow = Paint(18, 6, (x, y) =>
    {
        float dx = (x - 8.5f) / 9f, dy = (y - 2.5f) / 3f;
        return dx * dx + dy * dy <= 1f ? C(0x000000, 90) : Clear;
    }, Center);

    public static Sprite Bricks => bricks != null ? bricks : bricks = Paint(32, 16, (x, y) =>
    {
        int row = y / 8, bx = x + row % 2 * 8;
        if (y % 8 == 7 || bx % 16 == 15) return C(0x3a2a30);
        if (y % 8 == 6) return C(0x5e2c26);
        return (bx / 16 + row * 3) % 3 == 0 ? C(0x7a3a30) : C(0x8a4434);
    }, Center);

    // sidewalk on top, curb, then road with lane dashes; tiles horizontally
    public static Sprite Street => street != null ? street : street = Paint(32, 84, (x, y) =>
    {
        if (y < 3) return C(0x302830);
        if (y < 26) return x == 0 || y == 14 ? C(0x6a6a72) : Speckle(x, y) ? C(0x8a8a92) : C(0x82828a);
        if (y < 30) return C(0xa8a8b0);
        if (y < 32) return C(0x404048);
        if ((y == 55 || y == 56) && x < 16) return C(0xc8b040);
        return Speckle(x, y) ? C(0x34343c) : C(0x2c2c34);
    }, Center);

    public static Sprite Window(bool lit)
    {
        if (lit && litWindow != null) return litWindow;
        if (!lit && window != null) return window;
        var s = Paint(24, 32, (x, y) =>
        {
            if (x < 2 || x > 21 || y < 2 || y > 29 || x == 11 || x == 12 || y == 15 || y == 16) return C(0x2a2028);
            if (lit) return (x + y) % 9 < 2 ? C(0xfff0a0) : C(0xe0b050);
            return (x + y) % 9 < 2 ? C(0x9ab8e0) : C(0x4a6898);
        }, Center);
        return lit ? litWindow = s : window = s;
    }
}
