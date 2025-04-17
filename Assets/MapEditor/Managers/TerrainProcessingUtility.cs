using UnityEngine;

public static class TerrainProcessingUtility
{
    public static void ComputeSignedDistanceField(int gridSize, byte threshold, byte[] inputMask, ref float[] distances)
    {
        int paddedSize = gridSize + 2;
        int[] edgeX = new int[paddedSize * paddedSize];
        int[] edgeY = new int[paddedSize * paddedSize];
        float[] tempDistances = new float[paddedSize * paddedSize];

        // Initialize padded arrays
        for (int i = 0; i < paddedSize * paddedSize; i++)
        {
            edgeX[i] = -1;
            edgeY[i] = -1;
            tempDistances[i] = float.PositiveInfinity;
        }

        // Seed edge points
        for (int y = 1; y < gridSize - 1; y++)
        {
            int inputBase = y * gridSize;
            int paddedBase = (y + 1) * paddedSize + 1;
            for (int x = 1; x < gridSize - 1; x++)
            {
                int inputIdx = inputBase + x;
                int paddedIdx = paddedBase + x;
                bool isAboveThreshold = inputMask[inputIdx] > threshold;
                if (isAboveThreshold &&
                    (inputMask[inputIdx - 1] > threshold != isAboveThreshold ||
                     inputMask[inputIdx + 1] > threshold != isAboveThreshold ||
                     inputMask[inputIdx - gridSize] > threshold != isAboveThreshold ||
                     inputMask[inputIdx + gridSize] > threshold != isAboveThreshold))
                {
                    edgeX[paddedIdx] = x + 1;
                    edgeY[paddedIdx] = y + 1;
                    tempDistances[paddedIdx] = 0f;
                }
            }
        }

        // Forward pass
        for (int y = 1; y < paddedSize - 1; y++)
        {
            int baseIdx = y * paddedSize;
            for (int x = 1; x < paddedSize - 1; x++)
            {
                int idx = baseIdx + x;
                float currentDist = tempDistances[idx];
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx - paddedSize - 1, x, y, 1.4142135f, ref currentDist);
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx - paddedSize, x, y, 1f, ref currentDist);
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx - paddedSize + 1, x, y, 1.4142135f, ref currentDist);
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx - 1, x, y, 1f, ref currentDist);
            }
        }

        // Backward pass
        for (int y = paddedSize - 2; y >= 1; y--)
        {
            int baseIdx = y * paddedSize;
            for (int x = paddedSize - 2; x >= 1; x--)
            {
                int idx = baseIdx + x;
                float currentDist = tempDistances[idx];
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx + 1, x, y, 1f, ref currentDist);
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx + paddedSize - 1, x, y, 1.4142135f, ref currentDist);
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx + paddedSize, x, y, 1f, ref currentDist);
                PropagateDistance(ref edgeX, ref edgeY, ref tempDistances, idx + paddedSize + 1, x, y, 1.4142135f, ref currentDist);
            }
        }

        // Output signed distances
        for (int y = 0; y < gridSize; y++)
        {
            int inputBase = y * gridSize;
            int paddedBase = (y + 1) * paddedSize + 1;
            for (int x = 0; x < gridSize; x++)
            {
                int idx = inputBase + x;
                int paddedIdx = paddedBase + x;
                distances[idx] = (inputMask[idx] > threshold) ? -tempDistances[paddedIdx] : tempDistances[paddedIdx];
            }
        }
    }

    public static void ApplyGaussianBlur(int gridSize, float[] data, int iterations = 1)
    {
        if (iterations <= 0) return;

        float[] tempData = new float[gridSize * gridSize];
        int maxIndex = gridSize - 1;
        float[] kernel = { 0.0625f, 0.25f, 0.375f, 0.25f, 0.0625f }; // Gaussian weights (5-tap)
        int[] offsets = { -2, -1, 0, 1, 2 }; // Neighbor offsets

        for (int iter = 0; iter < iterations; iter++)
        {
            // Horizontal pass
            for (int y = 0; y < gridSize; y++)
            {
                int baseIdx = y * gridSize;
                for (int x = 0; x < gridSize; x++)
                {
                    float sum = 0f;
                    for (int k = 0; k < 5; k++) // 5-tap kernel
                    {
                        int sampleX = Mathf.Clamp(x + offsets[k], 0, maxIndex);
                        sum += data[baseIdx + sampleX] * kernel[k];
                    }
                    tempData[baseIdx + x] = sum;
                }
            }

            // Vertical pass
            for (int y = 0; y < gridSize; y++)
            {
                int baseIdx = y * gridSize;
                for (int x = 0; x < gridSize; x++)
                {
                    float sum = 0f;
                    for (int k = 0; k < 5; k++)
                    {
                        int sampleY = Mathf.Clamp(y + offsets[k], 0, maxIndex);
                        sum += tempData[sampleY * gridSize + x] * kernel[k];
                    }
                    data[baseIdx + x] = sum;
                }
            }
        }
    }

    public static void ComputeNormals(int gridSize, float[] heights, ref Vector3[] normals)
    {
        // Interior normals using Sobel-like filter
        for (int y = 1; y < gridSize - 1; y++)
        {
            for (int x = 1; x < gridSize - 1; x++)
            {
                float center = GetValue(heights, gridSize, x, y);
                float topLeft = GetValue(heights, gridSize, x - 1, y - 1);
                float top = GetValue(heights, gridSize, x - 1, y);
                float topRight = GetValue(heights, gridSize, x - 1, y + 1);
                float left = GetValue(heights, gridSize, x, y - 1);
                float right = GetValue(heights, gridSize, x, y + 1);
                float bottomLeft = GetValue(heights, gridSize, x + 1, y - 1);
                float bottom = GetValue(heights, gridSize, x + 1, y);
                float bottomRight = GetValue(heights, gridSize, x + 1, y + 1);

                // Sobel-like gradient
                float dx = (bottomLeft + 2f * bottom + bottomRight) - (topLeft + 2f * top + topRight);
                float dy = (topRight + 2f * right + bottomRight) - (topLeft + 2f * left + bottomLeft);
                Vector2 gradient = new Vector2(-dx, -dy).normalized;
                normals[y * gridSize + x] = new Vector3(gradient.x, gradient.y, center);
            }
        }

        // Edges (extrapolate from interior)
        for (int x = 1; x < gridSize - 1; x++)
        {
            normals[x] = GetValue(normals, gridSize, x, 1); // Top edge
            normals[(gridSize - 1) * gridSize + x] = GetValue(normals, gridSize, x, gridSize - 2); // Bottom edge
        }
        for (int y = 0; y < gridSize; y++)
        {
            normals[y * gridSize] = GetValue(normals, gridSize, 1, y); // Left edge
            normals[y * gridSize + gridSize - 1] = GetValue(normals, gridSize, gridSize - 2, y); // Right edge
        }
    }

    private static float GetValue(float[] data, int gridSize, int x, int y)
    {
        x = Mathf.Clamp(x, 0, gridSize - 1);
        y = Mathf.Clamp(y, 0, gridSize - 1);
        return data[y * gridSize + x];
    }

    // Overload for Vector3 arrays
    private static Vector3 GetValue(Vector3[] data, int gridSize, int x, int y)
    {
        x = Mathf.Clamp(x, 0, gridSize - 1);
        y = Mathf.Clamp(y, 0, gridSize - 1);
        return data[y * gridSize + x];
    }

    // Helper for ComputeSignedDistanceField
    private static void PropagateDistance(ref int[] edgeX, ref int[] edgeY, ref float[] distances, int neighborIdx, int x, int y, float stepCost, ref float currentDist)
    {
        float neighborDist = distances[neighborIdx] + stepCost;
        if (neighborDist < currentDist)
        {
            int idx = y * (edgeX.Length / edgeY.Length) + x; // Assuming square grid
            edgeX[idx] = edgeX[neighborIdx];
            edgeY[idx] = edgeY[neighborIdx];
            distances[idx] = neighborDist;
            currentDist = neighborDist;
        }
    }

    private static float CalculateDistance(float dx, float dy)
    {
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}