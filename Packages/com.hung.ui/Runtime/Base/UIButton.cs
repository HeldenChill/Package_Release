using System.Collections;
using System.Collections.Generic;


namespace Hung.UI
{
    using Hung.Base;
    using UnityEngine;
    using UnityEngine.UI;
    using System;
    using TMPro;

    public class UIButton : MonoBehaviour
	{

		public Action<int> _OnClick;
		public enum STATE
		{
			DISABLE = 0,
			OPENING = 1,
			SELECTING = 2,
			CLOSING = 3,
		}

		//Index follow State
		[SerializeField]
		protected int indexID;
		[SerializeField]
		protected Button button;
		[SerializeField]
		protected Transform tf;
		[SerializeField]
		protected TMP_Text textButton;
		[SerializeField]
		protected SFX_TYPE sfxType = SFX_TYPE.CLICK;
		[SerializeField]
		protected UIButtonComponent[] buttonComponents;
		protected STATE state;
		public int IndexID => indexID;
		public STATE State => state;
		/// <summary>Serialized target transform; <see cref="RectTf"/> casts it.</summary>
		public Transform Tf => tf;
		public RectTransform RectTf => (RectTransform)tf;

		/// <summary>Process-wide default for click SFX. A game whose buttons are silent sets it false at composition.</summary>
		public static bool DefaultClickSfx = true;
		protected virtual bool PlaysClickSfx => DefaultClickSfx;
		protected virtual void Awake()
		{
			button.onClick.AddListener(OnClick);
		}
		protected virtual void OnDestroy()
		{
			button.onClick.RemoveListener(OnClick);
		}
		public virtual void SetData(string text)
		{
			if (textButton != null)
				textButton.text = text;
		}
		public virtual void SetState(STATE state)
		{
			this.state = state;
			for (int i = 0; i < buttonComponents.Length; i++)
			{
				buttonComponents[i].SetState(state);
			}
		}
		public virtual void SetInteractable(bool state)
		{
			button.interactable = state;
		}
		public virtual T GetButtonComponent<T>() where T : UIButtonComponent
		{
			for (int i = 0; i < buttonComponents.Length; i++)
			{
				if (buttonComponents[i] is T)
					return (T)buttonComponents[i];
			}
			return null;
		}
		protected virtual void OnClick()
		{
			if (PlaysClickSfx) Locator.Audio?.PlaySfx(sfxType);
			_OnClick?.Invoke(indexID);
		}

	}
}