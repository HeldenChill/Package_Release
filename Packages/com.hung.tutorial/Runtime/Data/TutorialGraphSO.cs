using System.Collections.Generic;
using System.Linq;
using Hung.Base;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialGraph", menuName = "ScriptableObjects/TutorialGraph")]
public class TutorialGraphSO : ScriptableObject
{
    [System.Serializable]
    public class TutorialStepNode
    {
        public int id;

        // Runtime identity copied from TutorialDataSO.
        // These fields must stay independent from editor layout so Follow Run is not broken
        // when nodes are moved manually or Auto Layout changes layer/slot.
        public int tutorialLevel = -1;
        public int tutorialStepIndex = -1;

        // Editor ordering/rank only. Do not use these fields for runtime mapping.
        public int layer;
        public int slot;

        public List<int> nextIds = new List<int>();

        public string conditionKey;
        public TUTORIAL_ACTION actionType;
        public TutorialDataSO.ActionData action;
        public float delay;
        public List<string> postProcessKeys = new List<string>();
        public List<string> otherActionKeys = new List<string>();

        // Editor-only layout data. These fields are intentionally kept in the asset so the graph
        // stays readable after closing/reopening Unity. They are not consumed by runtime tutorial flow.
        public bool useManualPosition;
        public Vector2 editorPosition;
    }

    public int tutorialLevel = -1;
    public List<TutorialStepNode> nodes = new List<TutorialStepNode>();
    public int nextNodeId = 0;
    public int startNodeId = -1;

    public TutorialStepNode GetNode(int id) => nodes.FirstOrDefault(n => n.id == id);

    public int AllocId()
    {
        while (nodes.Any(n => n.id == nextNodeId)) nextNodeId++;
        return nextNodeId++;
    }

    public int LayerCount => nodes.Count == 0 ? 0 : nodes.Max(n => n.layer) + 1;

    public IEnumerable<TutorialStepNode> NodesInLayer(int layer) =>
        nodes.Where(n => n.layer == layer).OrderBy(n => n.slot).ThenBy(n => n.id);
}
