using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Base
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;

    public class SceneHotkey : MonoBehaviour
    {
        public static void OpenScene(string sceneName)
        {
            string path = "Assets/_Game/_Scenes/" + sceneName + ".unity";
            if (!System.IO.File.Exists(path))
            {
                // Template layout missing: find a scene with the same file name anywhere under Assets.
                string fileName = System.IO.Path.GetFileName(sceneName);
                path = null;
                foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Scene", new[] { "Assets" }))
                {
                    string candidate = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(candidate) == fileName) { path = candidate; break; }
                }
                if (path == null)
                {
                    Debug.LogWarning($"[SceneHotkey] Scene not found: {fileName}.unity");
                    return;
                }
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        [MenuItem("Open Scene/GameScene")]
        public static void OpenSceneGameDemo()
        {
            OpenScene("Game/GameScene");
        }

        [MenuItem("Open Scene/MapEditor")]
        public static void OpenSceneMapEditor()
        {
            OpenScene("MapEditor");
        }
        [MenuItem("Open Scene/CryptoLoader")]
        public static void OpenSceneCryptoLoader()
        {
            OpenScene("Game/CryptoLoader");
        }
        [MenuItem("Open Scene/LoadStart")]
        public static void OpenSceneLoadStart()
        {
            OpenScene("Game/LoadStart");
        }
    }

#endif

}