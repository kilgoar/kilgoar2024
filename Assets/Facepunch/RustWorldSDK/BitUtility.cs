using UnityEngine;

public class BitUtility
{
	private const float float2byte = 255;
	private const float byte2float = 1f / float2byte;

	public static byte Float2Byte(float f)
	{
		Union32 u = new Union32();
		u.f = f;
		u.b1 = 0;
		return (byte)(u.f * float2byte + 0.5f);
	}

	public static float Byte2Float(int b)
	{
		return b * byte2float;
	}
	
	// Encodes a normalized height (0–1) into a Color for red and blue monument texture
    public static Color EncodeHeight(float height)
    {
        // Clamp height to [0, 1]
        height = Mathf.Clamp01(height);

        // Convert to 16-bit short (inverse of Short2Float, assuming linear mapping)
        short shortValue = (short)(height * 65535f); // 0–65535 range

        // Split short into high and low bytes
        Union16 u = new Union16 { i = shortValue };
        byte r = u.b1; // High byte (red)
        byte b = u.b2; // Low byte (blue)

        // Convert bytes to normalized [0, 1] for Color
        return new Color(r / 255f, 0f, b / 255f, 1f); // Green = 0, Alpha = 1
    }

	public static float DecodeHeight(Color c)
	{
		// Convert the [0, 1] Color channels to [0, 255] Color32 channels
		byte r = Float2Byte(c.r);
		byte b = Float2Byte(c.b);

		// Combine red and blue into a short (mimicking DecodeShort)
		Union16 u = new Union16();
		u.b1 = r;
		u.b2 = b;
		short shortValue = u.i;

		// Convert the short to a float (mimicking Short2Float)
		return Short2Float(shortValue);
	}


    // Count the number of set bits in a 32-bit integer
    public static int CountSetBits(int value)
    {
        int count = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0) count++;
        }
        return count;
    }

    public static int ApplyTopologyAdditive(int existing, int newValue)
    {
        return existing | newValue;
    }



	public static float SampleAlphaBilinear(Texture2D texture, float u, float v)
	{
		if (texture == null) return 0f;

		int width = texture.width;
		int height = texture.height;

		float pixelX = u * (float)(width - 1);
		float pixelY = v * (float)(height - 1);

		int x0 = Mathf.Clamp((int)pixelX, 1, width - 2);
		int y0 = Mathf.Clamp((int)pixelY, 1, height - 2);
		int x1 = Mathf.Min(x0 + 1, width - 2);
		int y1 = Mathf.Min(y0 + 1, height - 2);

		float tx = pixelX - (float)x0;
		float ty = pixelY - (float)y0;

		float a00 = texture.GetPixel(x0, y0).a;
		float a10 = texture.GetPixel(x1, y0).a;
		float a01 = texture.GetPixel(x0, y1).a;
		float a11 = texture.GetPixel(x1, y1).a;

		float a0 = Mathf.Lerp(a00, a10, tx);
		float a1 = Mathf.Lerp(a01, a11, tx);
		return Mathf.Lerp(a0, a1, ty);
	}
	
	public static float SampleHeightBilinear(Texture2D texture, float u, float v)
	{
		if (texture == null) return 0f;

		int width = texture.width;
		int height = texture.height;

		// Map UV coordinates to pixel coordinates
		float pixelX = u * (float)(width - 1);
		float pixelY = v * (float)(height - 1);

		// Clamp to [1, width - 2] and [1, height - 2] to avoid edge pixels
		int x0 = Mathf.Clamp((int)pixelX, 1, width - 2);
		int y0 = Mathf.Clamp((int)pixelY, 1, height - 2);
		int x1 = Mathf.Min(x0 + 1, width - 2);
		int y1 = Mathf.Min(y0 + 1, height - 2);

		// Fractional parts for interpolation
		float tx = pixelX - (float)x0;
		float ty = pixelY - (float)y0;

		// Sample the four neighboring pixels and combine their heights
		Color c00 = texture.GetPixel(x0, y0);
		Color c10 = texture.GetPixel(x1, y0);
		Color c01 = texture.GetPixel(x0, y1);
		Color c11 = texture.GetPixel(x1, y1);

		float h00 = DecodeHeight(c00);
		float h10 = DecodeHeight(c10);
		float h01 = DecodeHeight(c01);
		float h11 = DecodeHeight(c11);

		// Bilinear interpolation
		float h0 = Mathf.Lerp(h00, h10, tx); // Interpolate along x (bottom edge)
		float h1 = Mathf.Lerp(h01, h11, tx); // Interpolate along x (top edge)
		return Mathf.Lerp(h0, h1, ty); // Interpolate along y
	}

    public static byte Bool2Byte(bool b)
    {
        return (b == true) ? Float2Byte(1f) : Float2Byte(0f);
    }

	private const float float2short = short.MaxValue - 1;
	private const float short2float = 1f / float2short;

	public static short Float2Short(float f)
	{
		return (short)(f * float2short + 0.5f);
	}

	public static float Short2Float(int b)
	{
		return b * short2float;
	}
	
	public static float Short2Float(short b)
    {
        return b * short2float;
    }

	public static Color32 EncodeFloat(float f)
	{
		Union32 u = new Union32();
		u.f = f;
		return new Color32(u.b1, u.b2, u.b3, u.b4);
	}

	public static float DecodeFloat(Color32 c)
	{
		Union32 u = new Union32();
		u.b1 = c.r;
		u.b2 = c.g;
		u.b3 = c.b;
		u.b4 = c.a;
		return u.f;
	}

	public static Color32 EncodeInt(int i)
	{
		Union32 u = new Union32();
		u.i = i;
		return new Color32(u.b1, u.b2, u.b3, u.b4);
	}

	public static int DecodeInt(Color32 c)
	{
		Union32 u = new Union32();
		u.b1 = c.r;
		u.b2 = c.g;
		u.b3 = c.b;
		u.b4 = c.a;
		return u.i;
	}

	public static Color32 EncodeShort(short i)
	{
		Union16 u = new Union16();
		u.i = i;
		return new Color32(u.b1, 0, u.b2, 1);
	}

	public static short DecodeShort(Color32 c)
	{
		Union16 u = new Union16();
		u.b1 = c.r;
		u.b2 = c.b;
		return u.i;
	}

	// Encode normal (result is in tangent space)
	public static Color EncodeNormal(Vector3 n)
	{
		n = (n + Vector3.one) * 0.5f; // [0, 1]
		return new Color(n.z, n.z, n.z, n.x);
	}

	// Decode normal (result is in world space)
	public static Vector3 DecodeNormal(Color c)
	{
		float nx = c.a * 2f - 1f;
		float nz = c.g * 2f - 1f;
		float ny = Mathf.Sqrt(1f - Mathf.Clamp01( nx * nx + nz * nz ));
		return new Vector3(nx, ny, nz);
	}

	public static Color32 EncodeVector(Vector4 v)
	{
		return new Color32(Float2Byte(v.x), Float2Byte(v.y), Float2Byte(v.z), Float2Byte(v.w));
	}

	public static Vector4 DecodeVector(Color32 c)
	{
		return new Vector4(Byte2Float(c.r), Byte2Float(c.g), Byte2Float(c.b), Byte2Float(c.a));
	}
}
