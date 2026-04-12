namespace StreamSchedule.Data;

public struct Color(float r = 0, float g = 0, float b = 0, float a = 0)
{
    public float r = r;
    public float g = g;
    public float b = b;
    public string name = "";
    
    public static readonly Color Gray = new Color() { r = 0.5f, g = 0.5f, b = 0.5f};

    public static Color FromHex(string? hex)
    {
        hex = hex?.Replace("#", string.Empty);
        if (string.IsNullOrWhiteSpace(hex)) return Gray;
        return new Color() { r = Convert.ToInt32(hex[..2], 16) / 255f, g = Convert.ToInt32(hex[2..4], 16) / 255f, b = Convert.ToInt32(hex[4..6], 16) / 255f};
    }

    public string ToHex() => $"{(int)(r * 255):X}{(int)(g * 255):X}{(int)(b * 255):X}";

    public static float DistanceSquared(Color left, Color right) => (left.r - right.r) * (left.r - right.r) + (left.g - right.g) * (left.g - right.g) + (left.b - right.b) * (left.b - right.b);
    public static float Distance(Color left, Color right) => MathF.Sqrt(DistanceSquared(left, right));

    public static float operator ^(Color left, Color right) { return left.r * right.r + left.g * right.g + left.b * right.b;}
    
    public static Color operator +(Color left, Color right) { return new Color() { r = left.r + right.r, g = left.g + right.g, b = left.b + right.b }; }
    public static Color operator +(Color left, int right) { return new Color() { r = left.r + right, g = left.g + right, b = left.b + right }; }
    public static Color operator +(int left, Color right) { return new Color() { r = left + right.r, g = left + right.g, b = left + right.b }; }
    public static Color operator +(Color left, float right) { return new Color() { r = left.r + right, g = left.g + right, b = left.b + right }; }
    public static Color operator +(float left, Color right) { return new Color() { r = left + right.r, g = left + right.g, b = left + right.b }; }

    public static Color operator *(Color left, Color right) { return new Color() { r = left.r * right.r, g = left.g * right.g, b = left.b * right.b }; }
    public static Color operator *(Color left, int right) { return new Color() { r = left.r * right, g = left.g * right, b = left.b * right }; }
    public static Color operator *(int left, Color right) { return new Color() { r = left * right.r, g = left * right.g, b = left * right.b }; }
    public static Color operator *(Color left, float right) { return new Color() { r = left.r * right, g = left.g * right, b = left.b * right }; }
    public static Color operator *(float left, Color right) { return new Color() { r = left * right.r, g = left * right.g, b = left * right.b }; }

    public static Color operator -(Color left, Color right) { return new Color() { r = left.r - right.r, g = left.g - right.g, b = left.b - right.b }; }
    public static Color operator -(Color left, int right) { return new Color() { r = left.r - right, g = left.g - right, b = left.b - right }; }
    public static Color operator -(int left, Color right) { return new Color() { r = left - right.r, g = left - right.g, b = left - right.b }; }
    public static Color operator -(Color left, float right) { return new Color() { r = left.r - right, g = left.g - right, b = left.b - right }; }
    public static Color operator -(float left, Color right) { return new Color() { r = left - right.r, g = left - right.g, b = left - right.b }; }

    public static Color operator /(Color left, Color right) { return new Color() { r = right.r == 0 ? 0 : left.r / right.r, g = right.g == 0 ? 0 : left.g / right.g, b = right.b == 0 ? 0 : left.b / right.b }; }
    public static Color operator /(Color left, int right) { return new Color() { r = right == 0 ? 0 : left.r / right, g = right == 0 ? 0 : left.g / right, b = right == 0 ? 0 : left.b / right }; }
    public static Color operator /(int left, Color right) { return new Color() { r = right.r == 0 ? 0 : left / right.r, g = right.g == 0 ? 0 : left / right.g, b = right.b == 0 ? 0 : left / right.b }; }
    public static Color operator /(Color left, float right) { return new Color() { r = right == 0 ? 0 : left.r / right, g = right == 0 ? 0 : left.g / right, b = right == 0 ? 0 : left.b / right }; }
    public static Color operator /(float left, Color right) { return new Color() { r = right.r == 0 ? 0 : left / right.r, g = right.g == 0 ? 0 : left / right.g, b = right.b == 0 ? 0 : left / right.b }; }

}
