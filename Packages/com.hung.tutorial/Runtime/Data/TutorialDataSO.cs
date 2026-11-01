using System;
using System.Collections.Generic;
using Hung.Base;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;


[CreateAssetMenu(fileName = "Tutorial", menuName = "ScriptableObjects/Tutorial")]
public class TutorialDataSO : SerializedScriptableObject
{
    [Serializable]
    public class ActionData
    {
        public string Name;
        public float Delay = 0;
        public List<string> PostProcessActions = new List<string>();
        public TUTORIAL_ACTION Type;
        [ShowIf("Type", TUTORIAL_ACTION.HAND)]
        public HAND_ACTION Action;
        [ShowIf("Type", TUTORIAL_ACTION.HAND)]
        public Ease Ease;
        [ShowIf("Type", TUTORIAL_ACTION.SHOW_VIDEO)]
        public string Description;
        [ShowIf("Type", TUTORIAL_ACTION.CALL_OUT)]
        public Vector2 Padding;
        [ShowIf("Type", TUTORIAL_ACTION.CALL_OUT)]
        public Vector2 Offset;
        [ShowIf("Type", TUTORIAL_ACTION.SHOW_CONTENT)]
        [SerializeField] private ItemId contentItemId;
        public ItemId ContentItemId
        {
            get
            {
                return contentItemId;
            }
        }
        [ShowIf("Type", TUTORIAL_ACTION.SHOW_CONTENT_STRING)]
        public string ContentString;
        [ShowIf("Type", TUTORIAL_ACTION.BRANCH)]
        public List<string> Conditions;
        [ShowIf("Type", TUTORIAL_ACTION.BRANCH)]
        public List<int> AdvanceSteps;
        [HideInInspector]
        public Vector3 Pos1;
        [HideInInspector]
        public Vector3 Pos2;
        [HideInInspector]
        public Transform CallOutRectTf;
        [HideInInspector]
        public Image CallOutFrame;
        [HideInInspector]
        public bool IsUI = true;
        public List<string> OtherActions = new List<string>();

    }
    [Serializable]
    public class LoadingData
    {
        public TUTORIAL_LOADING Type;
        [ShowIf("Type", TUTORIAL_LOADING.PREPARE_VIDEO)]
        public VideoClip Video;
        [ShowIf("Type", TUTORIAL_LOADING.PREPARE_VIDEO)]
        public string VideoName;
    }
    [Serializable]
    public class ConditionAction
    {
        [TableColumnWidth(100)]
        public string Condition;
        public ActionData Action;
    }

    [Serializable]
    public class TutorialData
    {
        
        public Dictionary<int, List<LoadingData>> LoadingDatas;
        [TableList(ShowIndexLabels = true)]
        [ListDrawerSettings(DraggableItems = true)]
        public List<ConditionAction> ConditionActions;
    }
    [Serializable]
    public class IndexData<T>
    {
        public int Index;
        public List<T> Value;
    }

    [SerializeField]
    protected Dictionary<int, TutorialData> tutorialDatas;
    public Dictionary<int, TutorialData> TutorialDatas => tutorialDatas;
    [SerializeField]
    protected Dictionary<int, List<IndexData<Vector2Int>>> exceptPositions;
    public Dictionary<int, List<IndexData<Vector2Int>>> ExceptPositions => exceptPositions;
    [SerializeField]
    protected Dictionary<int, List<IndexData<GameObject>>> tutorialPets;
    public Dictionary<int, List<IndexData<GameObject>>> TutorialPets => tutorialPets;
}

