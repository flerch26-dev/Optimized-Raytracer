using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct Sphere
{
    public Vector3 position;    // 12 bytes
    public float radius;        // 4 bytes
    public int materialIndex;   // 4 bytes
}

public struct MeshInfo
{
    public int nodeOffset;
    public int triangleOffset;
    public int materialIndex;
    public Matrix4x4 worldToLocalMatrix;
    public Matrix4x4 localToWorldMatrix;
}

public class BoundingBox
{
    public Vector3 min = Vector3.one * float.PositiveInfinity;
    public Vector3 max = Vector3.one * float.NegativeInfinity;
    public Vector3 center => (min + max) * 0.5f;
    public Vector3 size => max - min;

    public void GrowToInclude(Vector3 point)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }

    public void GrowToInclude(BVHTriangle triangle)
    {
        GrowToInclude(triangle.posA);
        GrowToInclude(triangle.posB);
        GrowToInclude(triangle.posC);
    }
}

public class Node
{
    //public BoundingBox bounds = new();
    //public List<BVHTriangle> triangles = new();
    public int boundingBoxIndex;
    public int triangleIndex;
    public int triangleCount;
    public int childIndex;
    public int isRootNode = 0;
}

[StructLayout(LayoutKind.Sequential)]
public struct GPUNode
{
    public Vector3 boundsMin;    // 12 bytes
    public Vector3 boundsMax;    // 12 bytes
    public int index;       // 4 bytes
    public int triangleCount;    // 4 bytes
}

[System.Serializable]
public struct RayTracingMaterial
{
    //0 means matte or metal; 1 means glass
    public int flag;
    public Color color;                // 16 bytes
    public float emissionStrength;     // 4 bytes
    public Color emissionColor;        // 16 bytes
    public float smoothness;           // 4 bytes
    public float specularProbability;  // 4 bytes
    public Color specularColor;        // 16 bytes
    public float indexOfRefraction;
}

public struct BVHTriangle
{
    public Vector3 posA;       // 16 bytes
    public Vector3 posB;       // 16 bytes
    public Vector3 posC;       // 16 bytes

    public Vector3 center;

    public int indexA;
    public int indexB;
    public int indexC;
}

public struct Triangle
{
    public Vector3 posA;       // 12 bytes
    public Vector3 posB;       // 12 bytes
    public Vector3 posC;       // 12 bytes

    public Vector3 normalA;    // 12 bytes
    public Vector3 normalB;    // 12 bytes
    public Vector3 normalC;    // 12 bytes

    public int materialIndex;  // 4 bytes
}