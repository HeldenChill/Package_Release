using System;
using UnityEngine;

namespace Hung.Tool
{
    public enum StepKind
    {
        None = 0,

        // Basic flow
        CustomCode = 10,
        CallMethod = 11,
        Delay = 12,

        // UI flow
        OpenUI = 100,
        CloseSelf = 101,
        RefreshUI = 102,

        // Audio / feedback
        PlaySfx = 200,
        PlayVfx = 201,

        // Analytics
        AnalyticsTrack = 300,

        // Reward / economy
        ClaimCurrency = 400,
        ShowRewardAd = 401,
        BuyIapPack = 402,

        // Animation
        Tween = 500,
        RunActionList = 501,

        // Logic
        IfElse = 600,
    }

    [Serializable]
    public class FlowStep
    {
        [Tooltip("Loại hành động của step này.")]
        public StepKind kind = StepKind.None;

        [Tooltip("Dữ liệu đi kèm step. Có thể là JSON hoặc custom string.")]
        [TextArea(2, 6)]
        public string args;

        [Tooltip("Ghi chú cho designer/dev. Không dùng để generate logic.")]
        [TextArea(1, 3)]
        public string note;

        public bool enabled = true;

        public static FlowStep Todo(string message)
        {
            return new FlowStep
            {
                kind = StepKind.CustomCode,
                args = string.Empty,
                note = message,
                enabled = true
            };
        }

        public static FlowStep CustomCode(string codeOrDescription)
        {
            return new FlowStep
            {
                kind = StepKind.CustomCode,
                args = codeOrDescription,
                enabled = true
            };
        }

        public static FlowStep CallMethod(string methodName)
        {
            return new FlowStep
            {
                kind = StepKind.CallMethod,
                args = methodName,
                enabled = true
            };
        }

        public static FlowStep OpenUI(string uiName)
        {
            return new FlowStep
            {
                kind = StepKind.OpenUI,
                args = uiName,
                enabled = true
            };
        }

        public static FlowStep CloseSelf()
        {
            return new FlowStep
            {
                kind = StepKind.CloseSelf,
                args = string.Empty,
                enabled = true
            };
        }

        public static FlowStep PlaySfx(string sfxName)
        {
            return new FlowStep
            {
                kind = StepKind.PlaySfx,
                args = sfxName,
                enabled = true
            };
        }

        public static FlowStep Analytics(string eventName)
        {
            return new FlowStep
            {
                kind = StepKind.AnalyticsTrack,
                args = eventName,
                enabled = true
            };
        }
    }
    [Serializable]
    public class PlaySfxArgs
    {
        public string sfxName;
        public float delay;
        public float volume = 1f;
    }

    [Serializable]
    public class OpenUIArgs
    {
        public string uiName;
        public bool closeCurrent;
        public string paramJson;
    }

    [Serializable]
    public class AnalyticsTrackArgs
    {
        public string eventName;
        public string parameterJson;
    }

    [Serializable]
    public class DelayArgs
    {
        public float duration;
    }

    [Serializable]
    public class TweenArgs
    {
        public string targetField;
        public string tweenType;
        public float duration;
        public string value;
    }

    [Serializable]
    public class ClaimCurrencyArgs
    {
        public string currencyType;
        public int amount;
        public bool useMultiplier;
    }

    [Serializable]
    public class ShowRewardAdArgs
    {
        public string placement;
        public string rewardType;
        public int rewardAmount;
    }
}
