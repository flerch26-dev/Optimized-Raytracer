using UnityEngine;
using System.Collections;

public class FPSCounter : MonoBehaviour
{
    public int fps;
    void Update()
    {
        fps = (int)(1.0f / Time.smoothDeltaTime);
        //GUI.Label(new Rect(0, 0, 100, 100), ((int)(1.0f / Time.smoothDeltaTime)).ToString());
    }
}

