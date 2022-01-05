using UnityEngine;
public static class ExtensionMethod
{
    public static Texture2D toTexture2D(this RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        var old_rt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        // Debug.Log(tex.GetPixel(200,200)[0]);

        RenderTexture.active = old_rt;
        return tex;
    }

     public static void DrawLine(this Texture2D tex, Vector2 p1, Vector2 p2, Color col)
 {
     Vector2 t = p1;
     float frac = 1/Mathf.Sqrt (Mathf.Pow (p2.x - p1.x, 2) + Mathf.Pow (p2.y - p1.y, 2));
     float ctr = 0;
     
     while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y) {
         t = Vector2.Lerp(p1, p2, ctr);
         ctr += frac;
         tex.SetPixel((int)t.x, (int)t.y, col);
     }
 }
}