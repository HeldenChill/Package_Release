using System.Collections;
using System.Collections.Generic;


namespace Hung.UI
{
#if UNITY_EDITOR
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEditor.U2D;
#endif
    using System;
    using UnityEngine;
    using UnityEngine.U2D;
    using UnityEngine.UI;
    public class UISCanvas : UICanvas
    {
        public Action<UISCanvas> _OnOpen;
        public Action<UISCanvas> _OnClose;

        [Serializable]
        protected class Propertys
        {
            public bool SetActiveByAnim = true;
        }

        [SerializeField]
        protected UIAnim[] anims;
        [SerializeField]
        protected Button closeButton;
        [SerializeField]
        protected List<Button> closeBtns = new List<Button>();
        private readonly List<Button> allCloseButtons = new List<Button>();

        /// <summary>Authored controls that close this canvas, including the legacy button.</summary>
        public IReadOnlyList<Button> CloseButtons => allCloseButtons;
        [SerializeField]
        protected Propertys data;
        [SerializeField]
        protected UICanvasComponent[] canvasComponent;
        [SerializeField]
        protected SpriteAtlas spriteAtlas;

        protected int longestHideAnimId = 0;
        protected float longestHideAnimTime = 0;
        private Action pendingCloseCallback;
        private readonly HashSet<UIAnim> pendingHide = new HashSet<UIAnim>();
        private readonly HashSet<UIAnim> pendingShow = new HashSet<UIAnim>();
        private readonly Dictionary<UIAnim, Action<int, int>> exitHandlers = new Dictionary<UIAnim, Action<int, int>>();
        private bool isFullyShown;
        private CanvasGroup[] raycastBlockers;

        /// <summary>True after all participating SHOW effects finish on an active canvas.</summary>
        public bool IsFullyShown => isFullyShown && gameObject.activeInHierarchy;

        /// <summary>Raised once when the current SHOW operation reaches its settled state.</summary>
        public event Action FullyShown;

        protected override UITransition EnsureTransition()
        {
            UISCanvasTransition composite = null;
            foreach (UITransition transition in GetComponents<UITransition>())
            {
                if (transition is UIAnim) continue;
                if (transition is UISCanvasTransition knownComposite)
                {
                    composite = knownComposite;
                    continue;
                }
                return transition;
            }
            return composite != null ? composite : gameObject.AddComponent<UISCanvasTransition>();
        }

        protected virtual void Start()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i]._OnAnimEnter += OnAnimEnter;
                UIAnim effect = anims[i];
                Action<int, int> handler = (id, anim) =>
                {
                    OnAnimExit(id, anim);
                    OnEffectExit(effect, anim);
                };
                exitHandlers[effect] = handler;
                effect._OnAnimExit += handler;
                for (int j = 0; j < anims[i].Datas.Count; j++)
                {
                    UIAnim.Propertys Data = anims[i].Datas[j];
                    if (Data != null && Data.Id == UIAnim.ANIM.HIDE)
                    {
                        if (Data.Time > longestHideAnimTime)
                        {
                            longestHideAnimTime = Data.Time;
                            longestHideAnimId = anims[i].GetHashCode();
                        }
                        break;
                    }
                }
            }
            allCloseButtons.Clear();
            if (closeButton != null) allCloseButtons.Add(closeButton);
            if (closeBtns != null)
            {
                foreach (Button button in closeBtns)
                    if (button != null && !allCloseButtons.Contains(button)) allCloseButtons.Add(button);
            }
            foreach (Button button in allCloseButtons) button.onClick.AddListener(Close);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i]._OnAnimEnter -= OnAnimEnter;
                if (exitHandlers.TryGetValue(anims[i], out Action<int, int> handler))
                    anims[i]._OnAnimExit -= handler;
            }
            exitHandlers.Clear();
            foreach (Button button in allCloseButtons)
                if (button != null) button.onClick.RemoveListener(Close);
        }

        protected override void OnOpen(object param)
        {
            _OnOpen?.Invoke(this);
        }

        protected override void OnActivated() => Show();

        public override void Show()
        {
            SetRaycastBlocking(true);
            isFullyShown = false;
            pendingShow.Clear();
            foreach (UIAnim effect in anims)
            {
                if (effect == null || !ShouldAnimateOnShow(effect) || effect.Datas == null) continue;
                foreach (UIAnim.Propertys property in effect.Datas)
                {
                    if (property == null || property.Id != UIAnim.ANIM.SHOW) continue;
                    pendingShow.Add(effect);
                    break;
                }
            }
            for (int i = 0; i < anims.Length; i++)
            {
                if (anims[i] != null && ShouldAnimateOnShow(anims[i])) anims[i].Play(UIAnim.ANIM.SHOW);
            }
            for (int i = 0; i < canvasComponent.Length; i++)
            {
                canvasComponent[i].Show();
            }
            pendingShow.RemoveWhere(effect => !effect.IsPending(UIAnim.ANIM.SHOW));
            if (pendingShow.Count == 0) MarkFullyShown();
        }

        public override void Hide()
        {
            SetRaycastBlocking(false);
            isFullyShown = false;
            pendingShow.Clear();
            for (int i = 0; i < anims.Length; i++)
            {
                if (anims[i] != null && ShouldAnimateOnHide(anims[i])) anims[i].Play(UIAnim.ANIM.HIDE);
            }
            for (int i = 0; i < canvasComponent.Length; i++)
            {
                canvasComponent[i].Hide();
            }
        }
        protected override void OnClose()
        {
            _OnClose?.Invoke(this);
        }

        public void HideForClose(Action onComplete)
        {
            pendingCloseCallback = onComplete;
            pendingHide.Clear();
            foreach (UIAnim effect in anims)
            {
                if (effect == null || !ShouldAnimateOnHide(effect) || effect.Datas == null) continue;
                foreach (UIAnim.Propertys property in effect.Datas)
                {
                    if (property == null || property.Id != UIAnim.ANIM.HIDE) continue;
                    pendingHide.Add(effect);
                    break;
                }
            }
            Hide();
            pendingHide.RemoveWhere(effect => !effect.IsPending(UIAnim.ANIM.HIDE));
            if (pendingHide.Count == 0) CompleteClose();
        }

        public void StopAllAnims()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].Interrupt();
            }
            pendingHide.Clear();
            pendingShow.Clear();
            isFullyShown = false;
            pendingCloseCallback = null;
        }

        private void OnEffectExit(UIAnim effect, int anim)
        {
            if (anim == (int)UIAnim.ANIM.SHOW && pendingShow.Remove(effect) && pendingShow.Count == 0)
                MarkFullyShown();
            if (anim != (int)UIAnim.ANIM.HIDE || !pendingHide.Remove(effect)) return;
            if (pendingHide.Count == 0) CompleteClose();
        }

        private void MarkFullyShown()
        {
            if (isFullyShown) return;
            isFullyShown = true;
            FullyShown?.Invoke();
        }

        private void CompleteClose()
        {
            Action callback = pendingCloseCallback;
            pendingCloseCallback = null;
            callback?.Invoke();
        }
        /// <summary>Allow a canvas subclass to skip a SHOW effect for this operation.</summary>
        protected virtual bool ShouldAnimateOnShow(UIAnim anim) => true;

        /// <summary>Allow a canvas subclass to skip a HIDE effect for this operation.</summary>
        protected virtual bool ShouldAnimateOnHide(UIAnim anim) => true;

        private void SetRaycastBlocking(bool blocking)
        {
            if (raycastBlockers == null)
            {
                var authored = new List<CanvasGroup>();
                foreach (CanvasGroup group in GetComponentsInChildren<CanvasGroup>(true))
                    if (group != null && group.blocksRaycasts) authored.Add(group);
                raycastBlockers = authored.ToArray();
            }
            foreach (CanvasGroup group in raycastBlockers)
                if (group != null) group.blocksRaycasts = blocking;
        }
        protected virtual void OnAnimEnter(int id, int anim)
        {
            if (data.SetActiveByAnim)
            {
                switch (anim)
                {
                    case (int)UIAnim.ANIM.SHOW:
                        if (!gameObject.activeInHierarchy)
                        {
                            gameObject.SetActive(true);
                        }
                        break;
                }

            }

        }
        protected virtual void OnAnimExit(int id, int anim)
        {
            // Completion is coordinated by OnEffectExit for every participating component.
        }
        public virtual T GetCanvasComponent<T>() where T : UICanvasComponent
        {
            for (int i = 0; i < canvasComponent.Length; i++)
            {
                if (canvasComponent[i] is T)
                {
                    return (T)canvasComponent[i];
                }
            }
            return null;
        }
#if UNITY_EDITOR
        [Button(ButtonSizes.Large), HorizontalGroup("Func", 0.3f)]
        public virtual void AddAtlasSprite()
        {
            if (spriteAtlas != null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image image in images)
                {
                    if (image.sprite == null) continue;
                    AtlasSprite atlasSprite = image.gameObject.GetComponent<AtlasSprite>();
                    if (atlasSprite == null)
                        atlasSprite = image.gameObject.AddComponent<AtlasSprite>();
                    atlasSprite.atlas = spriteAtlas;
                }
            }
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Func", 0.4f)]
        public virtual void UpdateAtlasSpriteData()
        {
            List<Sprite> packedSprites = new List<Sprite>();

            UnityEngine.Object[] objects = spriteAtlas.GetPackables();
            for (int i = 0; i < objects.Length; i++)
            {
                packedSprites.Add(objects[i] as Sprite);
            }

            List<Sprite> addSprite = new List<Sprite>();
            if (spriteAtlas != null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image image in images)
                {
                    if (image.sprite == null) continue;
                    if (!packedSprites.Contains(image.sprite))
                    {
                        packedSprites.Add(image.sprite);
                        addSprite.Add(image.sprite);
                    }
                }
            }

            if (addSprite.Count == 0) return;
            SpriteAtlasAsset atlasAsset = SpriteAtlasAsset.Load(AssetDatabase.GetAssetPath(spriteAtlas));
            List<UnityEngine.Object> save = new List<UnityEngine.Object>();
            for (int i = 0; i < addSprite.Count; i++)
            {
                save.Add(addSprite[i]);
            }

            atlasAsset.Add(save.ToArray());
            SpriteAtlasAsset.Save(atlasAsset, AssetDatabase.GetAssetPath(spriteAtlas));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Func", 0.3f)]
        public virtual void GetAllAnim()
        {
            anims = GetComponentsInChildren<UIAnim>(true);
        }
#endif

    }
}
