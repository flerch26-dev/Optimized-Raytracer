using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RayTracingCamera : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Color SkyColorHorizon;
    public Color SkyColorZenith;
    public Color GroundColor;
    public Vector3 SunPos;
    public float SunFocus;
    public float SunIntensity;

    [Header("Camera Settings")]
    public ComputeShader computeShader;
    Vector3 cameraPosition;
    Vector3 cameraRotation;
    public float mouseSensitivity = 2f;
    public float movementSpeed = 5f;

    public int fov = 90;
    public int maxBounceCount;
    public int numRaysPerPixel;
    public float divergeStrength;
    public RenderTexture renderTexture;

    [Header("Scene Settings")]
    public Scene[] scenes;
    public int currentSceneIndex;
    GameObject[] sceneModels;
    MeshInfo[] meshes;
    Triangle[] allTriangles;
    GPUNode[] allNodes;
    RayTracingMaterial[] materials;

    Matrix4x4 localToWorldMatrix;
    float aspectRatio;
    int imageWidth = 1920;
    int imageHeight = 1080;
    ComputeBuffer materialBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer nodeBuffer;
    ComputeBuffer meshBuffer;
    int numRenderedFrames = 0;
    int totalFramesSinceBeginning = 0;

    [Header("BVH Visualization")]
    public int BVHMaxDepth;
    public int depthVisualization;
    public Mesh cubeMesh;
    public Material drawMaterial;

    [Header("Video Settings")]
    List<Texture2D> allRenders = new List<Texture2D>();
    public bool makeVideo;
    public float currentTime;
    bool timerActive;
    public int totalFrames = 600;
    public int timeToWaitForEachFrame = 30;

    List<BVH> bvhs = new List<BVH>();

    void Start()
    {
        currentTime = 0;
        timerActive = true;

        renderTexture = new RenderTexture(imageWidth, imageHeight, 0, RenderTextureFormat.ARGBFloat);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        Cursor.lockState = CursorLockMode.Locked;
        aspectRatio = 16f / 9f;
        //imageHeight = (int)(imageWidth / aspectRatio);
        cameraPosition = transform.position;
        cameraRotation = transform.rotation.eulerAngles;

        List<Triangle> tris = new List<Triangle>();
        List<GPUNode> nodes = new List<GPUNode>();

        if (currentSceneIndex > scenes.Length - 1) currentSceneIndex = scenes.Length - 1;
        sceneModels = scenes[currentSceneIndex].sceneModels;
        meshes = new MeshInfo[sceneModels.Length];

        materials = new RayTracingMaterial[sceneModels.Length];

        for (int i = 0; i < sceneModels.Length; i++)
        {
            MeshInfo meshInfo;
            meshInfo.triangleOffset = tris.Count;
            meshInfo.nodeOffset = nodes.Count;
            meshInfo.materialIndex = i;
            meshInfo.worldToLocalMatrix = sceneModels[i].transform.worldToLocalMatrix;
            meshInfo.localToWorldMatrix = sceneModels[i].transform.localToWorldMatrix;
            meshes[i] = meshInfo;

            RTXMaterial rtxMat = sceneModels[i].GetComponent<RTXMaterial>();
            RayTracingMaterial mat;
            mat.flag = rtxMat.flag;
            mat.color = rtxMat.color;
            mat.emissionStrength = rtxMat.emissionStrength;
            mat.emissionColor = rtxMat.emissionColor;
            mat.smoothness = rtxMat.smoothness;
            mat.specularProbability = rtxMat.specularProbability;
            mat.specularColor = rtxMat.specularColor;
            mat.indexOfRefraction = rtxMat.indexOfRefraction;
            materials[i] = mat;

            Mesh mesh = sceneModels[i].GetComponent<MeshFilter>().sharedMesh;

            Vector3[] vertices = mesh.vertices;
            int[] meshTriangles = mesh.triangles;
            Vector3[] normals = mesh.normals;

            BVH bvh = new BVH(vertices, meshTriangles, BVHMaxDepth);
            bvhs.Add(bvh);
            //Debug.Log(bvh.allNodes.Count + ", " + bvh.allTriangles.Length);

            //allNodes = new GPUNode[bvh.allNodes.Count];
            for (int j = 0; j < bvh.allNodes.Count; j++)
            {
                GPUNode node = default(GPUNode);
                node.boundsMax = bvh.allBoundingBoxes[bvh.allNodes[j].boundingBoxIndex].max;
                node.boundsMin = bvh.allBoundingBoxes[bvh.allNodes[j].boundingBoxIndex].min;
                node.triangleCount = bvh.allNodes[j].triangleCount;
                node.index = node.triangleCount > 0 ? bvh.allNodes[j].triangleIndex : bvh.allNodes[j].childIndex;
                nodes.Add(node);
            }

            for (int triIdx = 0; triIdx < bvh.allTriangles.Length; triIdx++)
            {
                Triangle t = new Triangle();
                BVHTriangle bvht = bvh.allTriangles[triIdx];

                t.posA = bvht.posA;
                t.posB = bvht.posB;
                t.posC = bvht.posC;

                t.normalA = normals[bvht.indexA];
                t.normalB = normals[bvht.indexB];
                t.normalC = normals[bvht.indexC];

                t.materialIndex = i;

                tris.Add(t);
            }
        }
        allNodes = nodes.ToArray();
        allTriangles = tris.ToArray();
        InitBuffers();
    }

    void InitBuffers()
    {
        computeShader.SetFloat("fov", fov);
        computeShader.SetFloat("aspectRatio", aspectRatio);
        computeShader.SetInt("width", renderTexture.width);
        computeShader.SetInt("height", renderTexture.height);
        computeShader.SetFloat("divergeStrength", divergeStrength);

        if (materials.Length != 0)
        {
            if (materialBuffer != null) materialBuffer.Release();
            int materialStride = Marshal.SizeOf(typeof(RayTracingMaterial)); ; // RayTracingMaterial struct size
            materialBuffer = new ComputeBuffer(materials.Length, materialStride);
            materialBuffer.SetData(materials);
        }

        if (allTriangles.Length != 0)
        {
            if (triangleBuffer != null) triangleBuffer.Release();
            int triangleStride = Marshal.SizeOf(typeof(Triangle)); ; // Triangle struct size
            triangleBuffer = new ComputeBuffer(allTriangles.Length, triangleStride);
            triangleBuffer.SetData(allTriangles);
        }

        if (allNodes.Length != 0)
        {
            if (nodeBuffer != null) nodeBuffer.Release();
            int nodeStride = Marshal.SizeOf(typeof(GPUNode)); // Node struct size
            nodeBuffer = new ComputeBuffer(allNodes.Length, nodeStride);
            nodeBuffer.SetData(allNodes);
        }

        if (meshes.Length != 0)
        {
            if (meshBuffer != null) meshBuffer.Release();
            int meshInfoStride = Marshal.SizeOf(typeof(MeshInfo)); // Node struct size
            meshBuffer = new ComputeBuffer(meshes.Length, meshInfoStride);
            meshBuffer.SetData(meshes);
        }

        if (materials.Length != 0) computeShader.SetBuffer(0, "materials", materialBuffer);
        if (allTriangles.Length != 0) computeShader.SetBuffer(0, "triangles", triangleBuffer);
        if (allNodes.Length != 0) computeShader.SetBuffer(0, "nodes", nodeBuffer);
        if (allNodes.Length != 0) computeShader.SetInt("numNodes", allNodes.Length);
        if (allTriangles.Length != 0) computeShader.SetInt("numTris", allTriangles.Length);
        if (meshes.Length != 0) computeShader.SetBuffer(0, "meshes", meshBuffer);
        if (meshes.Length != 0) computeShader.SetInt("numMeshes", meshes.Length);

        computeShader.SetInt("maxBounceCount", maxBounceCount);
        computeShader.SetInt("numRaysPerPixel", numRaysPerPixel);
        computeShader.SetVector("SkyColorHorizon", SkyColorHorizon);
        computeShader.SetVector("GroundColor", GroundColor);
        computeShader.SetVector("SkyColorZenith", SkyColorZenith);
        computeShader.SetVector("SunPos", SunPos);
        computeShader.SetFloat("SunFocus", SunFocus);
        computeShader.SetFloat("SunIntensity", SunIntensity);
    }

    void DrawNodesAtDepth(Node node, int currentDepth, BVH bvh, int targetDepth)
    {
        if (node == null) return;

        if (currentDepth == targetDepth)
        {
            BoundingBox bounds = bvh.allBoundingBoxes[node.boundingBoxIndex];
            Color col = Color.HSVToRGB((currentDepth / 6f) % 1f, 1, 1);
            col.a = 0.3f;
            Gizmos.color = col;

            DrawBoundingBox(bounds, col, true);
            return; // Don’t go deeper
        }

        if (node.triangleCount == 0) // It's not a leaf → has children
        {
            DrawNodesAtDepth(bvh.allNodes[node.childIndex], currentDepth + 1, bvh, targetDepth);
            DrawNodesAtDepth(bvh.allNodes[node.childIndex + 1], currentDepth + 1, bvh, targetDepth);
        }
    }

    void DrawNodes(Node node, BVH bvh, int depth = 0)
    {
        if (node == null) return;

        if (depth == depthVisualization)
        {
            Color col = Color.HSVToRGB((depth / 6f) % 1f, 1, 1);
            col.a = 0.2f; // Transparent bounding box
            bool fill = true;
            DrawBoundingBox(bvh.allBoundingBoxes[node.boundingBoxIndex], col, fill);
            return;
        }

        DrawNodes(bvh.allNodes[node.childIndex], bvh, depth + 1);
        DrawNodes(bvh.allNodes[node.childIndex + 1], bvh, depth + 1);
    }

    void DrawBoundingBox(BoundingBox bounds, Color col, bool fill)
    {
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", col);

        Matrix4x4 matrix = Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size);

        Graphics.DrawMesh(cubeMesh, matrix, drawMaterial, 0, null, 0, props, false, false);
    }

    /*void OnRenderObject()
    {
        if (bvhs == null || bvhs.Count == 0) return;

        foreach (var bvh in bvhs)
        {
            if (bvh.allNodes == null || bvh.allNodes.Count == 0) continue;
            DrawNodesAtDepth(bvh.allNodes[0], 0, bvh, depthVisualization);
        }
    }*/

    void Update()
    {
        //HandleMouseLook();
        HandleMovement();

        if (numRaysPerPixel > 10) numRaysPerPixel = 10;

        // Use quaternion-based camera rotation
        Quaternion camRotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0);

        // Update Unity transform
        transform.rotation = camRotation;
        transform.position = cameraPosition;

        // Construct localToWorldMatrix for ray generation
        Vector3 forward = camRotation * Vector3.forward;
        Vector3 right = camRotation * Vector3.right;
        Vector3 up = camRotation * Vector3.up;

        localToWorldMatrix.SetColumn(0, right);
        localToWorldMatrix.SetColumn(1, up);
        localToWorldMatrix.SetColumn(2, forward);
        localToWorldMatrix.SetColumn(3, new Vector4(cameraPosition.x, cameraPosition.y, cameraPosition.z, 1));

        if (timerActive && makeVideo) currentTime = currentTime + Time.deltaTime;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        numRenderedFrames += 1;
        totalFramesSinceBeginning += 1;

        if (renderTexture == null || renderTexture.width != imageWidth || renderTexture.height != imageHeight)
        {
            if (renderTexture != null) renderTexture.Release();

            renderTexture = new RenderTexture(imageWidth, imageHeight, 0, RenderTextureFormat.ARGBFloat);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        computeShader.SetTexture(0, "Result", renderTexture);

        computeShader.SetVector("cameraPosition", cameraPosition);
        computeShader.SetMatrix("localToWorldMatrix", localToWorldMatrix);
        computeShader.SetInt("numRenderedFrames", numRenderedFrames);
        computeShader.SetInt("totalFrames", totalFramesSinceBeginning);

        int threadGroupsX = Mathf.CeilToInt(renderTexture.width / 32f);
        int threadGroupsY = Mathf.CeilToInt(renderTexture.height / 32f);
        computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(renderTexture, destination);
        Animate();
    }

    void Animate()
    {
        if (currentTime > timeToWaitForEachFrame && makeVideo)
        {
            numRenderedFrames = 0;
            allRenders.Add(SaveRender.toTexture2D(renderTexture));
            currentTime = 0;
            sceneModels[7].transform.Rotate(new Vector3(0, 0.6f, 0));
            UpdateModelMatrices(7);
            meshBuffer.SetData(meshes);
            computeShader.SetBuffer(0, "meshes", meshBuffer);

            if (allRenders.Count >= totalFrames)
            {
                timerActive = false;
                SaveRender.CreateOutputs(allRenders);
            }
        }
    }

    void UpdateModelMatrices(int index)
    {
        meshes[index].worldToLocalMatrix = sceneModels[index].transform.worldToLocalMatrix;
        meshes[index].localToWorldMatrix = sceneModels[index].transform.localToWorldMatrix;
    }

    void OnDestroy()
    {
        if (materialBuffer != null) materialBuffer.Release();
        if (nodeBuffer != null) nodeBuffer.Release();
        if (triangleBuffer != null) triangleBuffer.Release();
        if (meshBuffer != null) meshBuffer.Release();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f)
        {
            numRenderedFrames = 0;
        }

        cameraRotation.y += mouseX * Mathf.Deg2Rad;  // Yaw
        cameraRotation.x -= mouseY * Mathf.Deg2Rad;  // Pitch

        // Clamp vertical look (pitch)
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -90, 90);
    }

    void HandleMovement()
    {
        Quaternion camRotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0);

        Vector3 forward = camRotation * Vector3.forward;
        Vector3 right = camRotation * Vector3.right;
        Vector3 up = camRotation * Vector3.up;

        forward.Normalize();
        right.Normalize();
        up.Normalize();

        Vector3 velocity = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) velocity += forward;
        if (Input.GetKey(KeyCode.S)) velocity -= forward;
        if (Input.GetKey(KeyCode.A)) velocity -= right;
        if (Input.GetKey(KeyCode.D)) velocity += right;
        if (Input.GetKey(KeyCode.Q)) velocity -= up;
        if (Input.GetKey(KeyCode.E)) velocity += up;

        if (velocity != Vector3.zero)
        {
            velocity.Normalize();
            cameraPosition += velocity * movementSpeed * Time.deltaTime;
            numRenderedFrames = 0;
        }
    }

    [System.Serializable]
    public struct Scene
    {
        public string sceneName;
        public GameObject[] sceneModels;
    }
}