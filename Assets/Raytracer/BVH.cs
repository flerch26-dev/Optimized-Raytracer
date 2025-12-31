using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

public class BVH
{
    public Node root;
    public List<Node> allNodes = new();
    public BVHTriangle[] allTriangles;
    public List<BoundingBox> allBoundingBoxes = new();

    public BVH(Vector3[] vertices, int[] triangleIndices, int maxDepth)
    {
        BoundingBox bounds = new();

        foreach (Vector3 vert in vertices)
        {
            bounds.GrowToInclude(vert);
        }

        allBoundingBoxes.Add(bounds);
        allTriangles = new BVHTriangle[triangleIndices.Length / 3];

        for (int i = 0; i < triangleIndices.Length; i += 3)
        {
            Vector3 a = vertices[triangleIndices[i + 0]];
            Vector3 b = vertices[triangleIndices[i + 1]];
            Vector3 c = vertices[triangleIndices[i + 2]];

            BVHTriangle tri;
            tri.posA = a;
            tri.posB = b;
            tri.posC = c;
            tri.center = (a + b + c) / 3f;
            tri.indexA = triangleIndices[i];
            tri.indexB = triangleIndices[i + 1];
            tri.indexC = triangleIndices[i + 2];

            allTriangles[i / 3] = tri;
        }

        root = new Node() { boundingBoxIndex = 0, childIndex = 0 };
        root.isRootNode = 1;
        root.triangleIndex = 0;
        root.triangleCount = allTriangles.Length;
        allNodes.Add(root);
        Split(root);
    }

    public void Split(Node parent, int depth = 0)
    {
        /*if (depth == maxDepth)
        {
            parent.childIndex = 0;
            return;
        }

        (int splitAxis, float splitPos, float cost) = ChooseSplit(parent);
        if (cost >= NodeCost(allBoundingBoxes[parent.boundingBoxIndex].size, parent.triangleCount)) return;

        Node childA = new() { triangleIndex = parent.triangleIndex };
        Node childB = new() { triangleIndex = parent.triangleIndex };

        for (int i = parent.triangleIndex; i < parent.triangleIndex + parent.triangleCount; i++)
        {
            bool isSideA = allTriangles[i].center[splitAxis] < splitPos;
            Node child = isSideA ? childA : childB;
            allBoundingBoxes[child.boundingBoxIndex].GrowToInclude(allTriangles[i]);
            child.triangleCount++;

            if (isSideA)
            {
                int swap = child.triangleIndex + child.triangleCount - 1;
                (allTriangles[i], allTriangles[swap]) = (allTriangles[swap], allTriangles[i]);
                childB.triangleIndex++;
            }
        }

        parent.childIndex = allNodes.Count;
        allNodes.Add(childA);
        allNodes.Add(childB);

        Split(childA, depth + 1);
        Split(childB, depth + 1);*/

        Vector3 size = allBoundingBoxes[parent.boundingBoxIndex].size;
        int splitAxis = size.x > Mathf.Max(size.y, size.z) ? 0 : size.y > size.z ? 1 : 2;
        float splitPos = allBoundingBoxes[parent.boundingBoxIndex].center[splitAxis];

        Node childA = new Node();
        Node childB = new Node();

        int start = parent.triangleIndex;
        int end = start + parent.triangleCount;

        int mid = start;

        for (int i = start; i < end; i++)
        {
            if (allTriangles[i].center[splitAxis] < splitPos)
            {
                (allTriangles[i], allTriangles[mid]) = (allTriangles[mid], allTriangles[i]);
                mid++;
            }
        }

        childA.triangleIndex = start;
        childA.triangleCount = mid - start;

        childB.triangleIndex = mid;
        childB.triangleCount = end - mid;

        BoundingBox childABoundingBox = new();
        BoundingBox childBBoundingBox = new();

        for (int i = childA.triangleIndex; i < childA.triangleIndex + childA.triangleCount; i++)
            childABoundingBox.GrowToInclude(allTriangles[i]);

        for (int i = childB.triangleIndex; i < childB.triangleIndex + childB.triangleCount; i++)
            childBBoundingBox.GrowToInclude(allTriangles[i]);

        allBoundingBoxes.Add(childABoundingBox);
        childA.boundingBoxIndex = allBoundingBoxes.Count - 1;
        allBoundingBoxes.Add(childBBoundingBox);
        childB.boundingBoxIndex = allBoundingBoxes.Count - 1;

        if ((childA.triangleCount > 0 && childB.triangleCount > 0))
        {
            parent.childIndex = allNodes.Count;
            parent.triangleCount = 0;
            allNodes.Add(childA);
            allNodes.Add(childB);

            Split(childA, depth + 1);
            Split(childB, depth + 1);
        }
        else
        {
            return;
            //parent.childIndex = 0;
        }
    }

    (int axis, float pos, float cost) ChooseSplit(Node node)
    {
        const int numTestsPerAxis = 5;
        float bestCost = float.MaxValue;
        float bestPos = 0;
        int bestAxis = 0;

        for (int axis = 0; axis < 3; axis++)
        {
            float boundsStart = allBoundingBoxes[node.boundingBoxIndex].min[axis];
            float boundsEnd = allBoundingBoxes[node.boundingBoxIndex].max[axis];

            for (int i = 0; i < numTestsPerAxis; i++)
            {
                float splitT = (i + 1) / (numTestsPerAxis + 1f);
                float pos = boundsStart + (boundsEnd - boundsStart) * splitT;
                float cost = EvaluateSplit(node, axis, pos);

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPos = pos;
                    bestAxis = axis;
                }
            }
        }

        return (bestAxis, bestPos, bestCost);
    }

    float EvaluateSplit(Node node, int splitAxis, float splitPos)
    {
        BoundingBox boundsA = new();
        BoundingBox boundsB = new();
        int numInA = 0;
        int numInB = 0;

        for (int i = node.triangleIndex; i < node.triangleIndex + node.triangleCount; i++)
        {
            BVHTriangle tri = allTriangles[i];
            if (tri.center[splitAxis] < splitPos)
            {
                boundsA.GrowToInclude(tri);
                numInA++;
            }
            else
            {
                boundsB.GrowToInclude(tri);
                numInB++;
            }
        }

        return NodeCost(boundsA.size, numInA) + NodeCost(boundsB.size, numInB);
    }

    static float NodeCost(Vector3 size, int numTriangles)
    {
        float halfArea = size.x * (size.y + size.z) + size.y * size.z;
        return halfArea * numTriangles;
    }
}
