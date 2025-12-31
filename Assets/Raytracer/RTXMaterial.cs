using UnityEngine;
using System.Collections;

public class RTXMaterial : MonoBehaviour
{
    public int flag;
    public Color color;                // 16 bytes
    public float emissionStrength;     // 4 bytes
    public Color emissionColor;        // 16 bytes
    public float smoothness;           // 4 bytes
    public float specularProbability;  // 4 bytes
    public Color specularColor;        // 16 bytes
    public float indexOfRefraction;

    private void OnValidate()
    {
        Material mat = transform.GetComponent<Renderer>().sharedMaterial;
        mat.color = color;
    }
}

