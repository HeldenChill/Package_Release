using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hung.Base
{
    using Hung.DesignPattern;
    [DefaultExecutionOrder(-10)]
    public class SceneGameManager : Singleton<SceneGameManager>, IProgress
    {
        public Action<float> _OnLoadingProgress { get; set; }
        public event Action<int, float> OnLoadingScene;
        public event Action<int> OnSceneLoaded;

        private int _id = 0;

        public int Id => _id;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void LoadingSceneAsync(int id)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(id));
        }
        public void LoadingSceneAsync(string sceneName)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        }

        private IEnumerator LoadSceneAsyncCoroutine(int id)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(id);
            _id = id;
            while (!asyncLoad.isDone)
            {
                float loadingPercentage = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                OnLoadingScene?.Invoke(_id, loadingPercentage);
                _OnLoadingProgress?.Invoke(loadingPercentage);
                // Update your loading UI with the percentage
                yield return null;
            }
            // Scene is fully loaded; hide loading UI
            OnSceneLoaded?.Invoke(_id);
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            _id = SceneManager.GetSceneByName(sceneName).buildIndex;
            while (!asyncLoad.isDone)
            {
                float loadingPercentage = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                OnLoadingScene?.Invoke(_id, loadingPercentage);
                _OnLoadingProgress?.Invoke(loadingPercentage);
                // Update your loading UI with the percentage
                yield return null;
            }
            // Scene is fully loaded; hide loading UI
            OnSceneLoaded?.Invoke(_id);
        }
    }
}