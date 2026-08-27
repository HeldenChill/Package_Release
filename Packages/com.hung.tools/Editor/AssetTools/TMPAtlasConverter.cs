using System.IO;
using TMPro;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class TMPAtlasConverter
{
    [MenuItem("Tools/Universal/Art/UI/Create TMP Sprite Asset From Atlas")]
    static void Create()
    {
        SpriteAtlas atlas = Selection.activeObject as SpriteAtlas;

        Sprite[] sprites = new Sprite[atlas.spriteCount];
        atlas.GetSprites(sprites);

        TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

        foreach (var s in sprites)
        {
            Debug.Log(s.name);
        }
    }
    [MenuItem("Tools/Universal/Art/UI/Export Sprite Atlas")]
    static void Export()
    {
        SpriteAtlas atlas = Selection.activeObject as SpriteAtlas;

        if (atlas == null)
        {
            Debug.LogError("Select a SpriteAtlas first.");
            return;
        }

        Sprite[] sprites = new Sprite[atlas.spriteCount];
        atlas.GetSprites(sprites);
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        Texture2D tex = ToReadableRGBA32(sprites[0].texture);

        if (tex == null)
        {
            Debug.LogError("Atlas not packed yet. Click Pack Preview first.");
            return;
        }

        byte[] png = tex.EncodeToPNG();

        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string folder = System.IO.Path.GetDirectoryName(assetPath);
        string path = Path.Combine(folder, atlas.name + ".png");
        File.WriteAllBytes(path, png);

        Debug.Log("Atlas exported to: " + path);
    }
    static Texture2D ToReadableRGBA32(Texture source)
    {
        int w = source.width;
        int h = source.height;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;

        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply(false, false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }
}
#endif
