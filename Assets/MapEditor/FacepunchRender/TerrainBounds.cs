using System;
using UnityEngine;

public class TerrainBounds
{
    public int xMin;
    public int yMin;
    public int xMax;
    public int yMax;
    private int defaultValue;

    public TerrainBounds(int defaultValue = 0)
    {
        this.defaultValue = defaultValue;
        Clear();
    }

    public TerrainBounds(int xMin, int yMin, int xMax, int yMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
        this.defaultValue = 0;
    }

    public void Clear()
    {
        xMin = yMin = defaultValue;
        xMax = yMax = 0;
    }

    public void Expand(int amount)
    {
        xMin -= amount;
        yMin -= amount;
        xMax += amount;
        yMax += amount;
    }

    public void Clamp(int minValue, int maxValue)
    {
        xMin = Mathf.Clamp(xMin, minValue, maxValue);
        yMin = Mathf.Clamp(yMin, minValue, maxValue);
        xMax = Mathf.Clamp(xMax, minValue, maxValue);
        yMax = Mathf.Clamp(yMax, minValue, maxValue);
    }

    // Added from IIBAEFGCCFH.KLOOMFJEEBJ
    public void IncludeRect(RectInt rect)
    {
        if (rect.xMin < xMin) xMin = rect.xMin;
        if (rect.yMin < yMin) yMin = rect.yMin;
        if (rect.xMax > xMax) xMax = rect.xMax;
        if (rect.yMax > yMax) yMax = rect.yMax;
    }

    public int Width => xMax - xMin;
    public int Height => yMax - yMin;

    public bool Contains(Vector2 point)
    {
        return point.x >= xMin && point.y >= yMin && point.x <= xMax && point.y <= yMax;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is TerrainBounds other)) return false;
        return other.xMin == xMin && other.yMin == yMin && other.xMax == xMax && other.yMax == yMax;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + xMin.GetHashCode();
            hash = hash * 23 + yMin.GetHashCode();
            hash = hash * 23 + xMax.GetHashCode();
            hash = hash * 23 + yMax.GetHashCode();
            return hash;
        }
    }

    public override string ToString()
    {
        return $"X: {xMin} Z: {yMin} XX: {xMax} ZZ: {yMax}";
    }

    public static TerrainBounds Empty = new TerrainBounds(0, 0, 0, 0);
}