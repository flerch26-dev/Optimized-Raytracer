using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/*run this code in the terminal to get the video:
/opt/homebrew/bin/ffmpeg -y -framerate 30 -pattern_type glob -i "/Users/florianlerch/Desktop/Renders/*.png" -c:v libx264 -pix_fmt yuv420p ~/Desktop/test.mp4
*/

public static class SaveRender
{
    public static void SaveRenderTexture(Texture2D  tex, int index)
    {
        Debug.Log("Converted texture");

        string path = "/Users/florianlerch/Desktop/Renders/frame_" + (index + 1).ToString("D4") + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("Saved to: " + path);
    }

    public static Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        RenderTexture currentActiveRT = RenderTexture.active;

        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentActiveRT; // restore
        return tex;
    }

    public static void CreateOutputs(List<Texture2D> renders)
    {
        for (int i = 0; i < renders.Count; i++)
        {
            SaveRenderTexture(renders[i], i);
        }

        #if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
        #else
            Application.Quit();
        #endif
    }
}
