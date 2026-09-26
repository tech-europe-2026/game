using UnityEngine;

public static class Palette
{
    public static readonly Color Background = Hex("0E1120");
    public static readonly Color Floor = Hex("1D2237");
    public static readonly Color FloorSide = Hex("121627");
    public static readonly Color WallTop = Hex("363D60");
    public static readonly Color WallSide = Hex("232843");
    public static readonly Color Hole = Hex("07080F");
    public static readonly Color Danger = Hex("FF4D6D");
    public static readonly Color Goal = Hex("34D399");
    public static readonly Color GoalSide = Hex("1E8C66");
    public static readonly Color Lift = Hex("A855F7");
    public static readonly Color Pulse = Hex("22C1EE");
    public static readonly Color Ice = Hex("C4ECFF");
    public static readonly Color Portal = Hex("FF7AB6");
    public static readonly Color Arrow = Hex("FF9F43");
    public static readonly Color Piece = Hex("FFD166");
    public static readonly Color PieceLight = Hex("FFEBB0");
    public static readonly Color Text = Hex("E7EAF6");
    public static readonly Color TextDim = Hex("8C93B3");
    public static readonly Color Accent = Hex("7C8CFF");
    public static readonly Color Panel = Hex("171B2E");
    public static readonly Color Button = Hex("252B45");

    static readonly Color[] platformTops = { Hex("1D2237"), Hex("4655C8"), Hex("5C6CEB"), Hex("7C8CFF") };

    public static Color PlatformTop(int height) => platformTops[Mathf.Clamp(height, 0, 3)];

    public static Color Side(Color top) => Color.Lerp(top, Color.black, 0.45f);

    public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
