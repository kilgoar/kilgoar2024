using UnityEngine;
using System; 

public struct TextureSampler
{
    public int Width;

    public int Height;

    public Color32[] Pixels;

    public TextureSampler(Texture2D texture)
    {
        if (texture != null)
        {
            Width = texture.width;
            Height = texture.height;
            Pixels = texture.GetPixels32();
        }
        else
        {
            Width = 0;
            Height = 0;
            Pixels = null;
        }
    }

    private Color32 GetPixel(int x, int y)
    {
        if (Pixels == null || x < 0 || x >= Width || y < 0 || y >= Height)
            return new Color32(0, 0, 0, 0);
        return Pixels[y * Width + x];
    }

    public float SampleFloatShort(float u, float v)
    {
        return SampleBilinear(u, v, color => BitUtility.Short2Float(BitUtility.DecodeShort(color)));
    }

    public float SampleFloat(float u, float v)
    {
        return SampleBilinear(u, v, BitUtility.DecodeFloat);
    }

    public int SampleIntNearest(float u, float v)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(u * (Width - 1)), 0, Width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(v * (Height - 1)), 0, Height - 1);
        return BitUtility.DecodeInt(GetPixel(x, y));
    }

	public Vector3 SampleNormal(float u, float v)
	{
		return SampleBilinear(u, v, color => BitUtility.DecodeNormal(color));
	}

    public Vector4 SampleVector4(float u, float v)
    {
        return SampleBilinear(u, v, BitUtility.DecodeVector);
    }

    public Color32 SampleColor(float u, float v)
    {
        float x = u * (Width - 1);
        float y = v * (Height - 1);
        int x0 = Mathf.Clamp((int)x, 0, Width - 1);
        int y0 = Mathf.Clamp((int)y, 0, Height - 1);
        int x1 = Mathf.Min(x0 + 1, Width - 1);
        int y1 = Mathf.Min(y0 + 1, Height - 1);

        Color a = GetPixel(x0, y0);
        Color b = GetPixel(x1, y0);
        Color c = GetPixel(x0, y1);
        Color d = GetPixel(x1, y1);

        float tx = x - x0;
        float ty = y - y0;

        Color ab = Color.Lerp(a, b, tx);
        Color cd = Color.Lerp(c, d, tx);
        return Color.Lerp(ab, cd, ty);
    }

    private T SampleBilinear<T>(float u, float v, Func<Color32, T> decode)
    {
        float x = u * (Width - 1);
        float y = v * (Height - 1);
        int x0 = Mathf.Clamp((int)x, 0, Width - 1);
        int y0 = Mathf.Clamp((int)y, 0, Height - 1);
        int x1 = Mathf.Min(x0 + 1, Width - 1);
        int y1 = Mathf.Min(y0 + 1, Height - 1);

        T a = decode(GetPixel(x0, y0));
        T b = decode(GetPixel(x1, y0));
        T c = decode(GetPixel(x0, y1));
        T d = decode(GetPixel(x1, y1));

        float tx = x - x0;
        float ty = y - y0;

        return Lerp(Lerp(a, b, tx), Lerp(c, d, tx), ty);
    }

    private static T Lerp<T>(T a, T b, float t)
    {
        if (typeof(T) == typeof(float))
            return (T)(object)Mathf.Lerp((float)(object)a, (float)(object)b, t);
        if (typeof(T) == typeof(Vector3))
            return (T)(object)Vector3.Lerp((Vector3)(object)a, (Vector3)(object)b, t);
        if (typeof(T) == typeof(Vector4))
            return (T)(object)Vector4.Lerp((Vector4)(object)a, (Vector4)(object)b, t);
        throw new NotSupportedException($"Type {typeof(T)} not supported for interpolation.");
    }

 
    public float GetHeight(int x, int y)
    {
        return BitUtility.Short2Float(BitUtility.DecodeShort(GetPixel(x, y)));
    }

    public Vector3 GetNormal(int x, int y)
    {
        return BitUtility.DecodeNormal(GetPixel(x, y));
    }

    public int GetInt(int x, int y)
    {
        return BitUtility.DecodeInt(GetPixel(x, y));
    }
}