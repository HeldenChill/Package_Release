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
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/_Game/_Scenes/" + sceneName + ".unity");
            }
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