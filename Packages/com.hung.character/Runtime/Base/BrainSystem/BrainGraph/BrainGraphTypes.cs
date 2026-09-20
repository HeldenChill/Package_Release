using System;
using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    public enum BrainGraphNodeKind
    {
        Root = 0,
        Sequence = 10,
        Selector = 20,
        Parallel = 30,

        Perception = 100,
        SeeSomething = 110,
        HearSomething = 120,
        DetectWorldStateChange = 130,
        KeepTrackOfTarget = 140,
        KeepTrackOfPosition = 150,
        LoseTrackOfSeeTarget = 160,

        MoveToTarget = 200,
        AwareAround = 210,
        LookAround = 220,
        LookAroundPosition = 230,
        StopMove = 240,

        Condition = 300,
        SetBrainFlag = 310,
        DebugLog = 900,
        Comment = 999
    }

    public enum BrainGraphStatus
    {
        None = 0,
        Running = 1,
        Success = 2,
        Failure = 3
    }

    public enum BrainGraphTargetSource
    {
        SeenEnemyPosition = 0,
        MoveTarget = 1,
        LastSoundPosition = 2,
        ManualPosition = 3,
        CharacterForward = 4
    }

    public enum BrainGraphConditionKind
    {
        Always = 0,
        HasSeenTarget = 10,
        HasSound = 20,
        HasWorldStateChange = 30,
        HasMoveTarget = 40,
        HasArrived = 50,
        IsRunning = 60,
        IsCrouching = 70,
        CustomPhaseIdEquals = 100
    }

    [Serializable]
    public sealed class BrainGraphNode
    {
        public int id;
        public string title = "Node";
        public BrainGraphNodeKind kind = BrainGraphNodeKind.Comment;
        public Vector2 editorPosition;
        public Vector2 editorSize = new Vector2(230f, 118f);

        [TextArea(2, 5)] public string note;
        public bool disabled;

        // Generic config used by the built-in runtime nodes.
        public BrainGraphTargetSource targetSource = BrainGraphTargetSource.SeenEnemyPosition;
        public BrainGraphConditionKind condition = BrainGraphConditionKind.Always;
        public bool invertCondition;
        public Vector3 vectorValue;
        public float floatValue = 1f;
        public int intValue;
        public string stringValue;

        // Output flag config for SetBrainFlag and quick debugging.
        public bool writeMoveTarget;
        public bool writeSeePosition;
        public bool writeRun;
        public bool writeCrouch;
        public bool clearMoveOnEnd = true;
    }

    [Serializable]
    public sealed class BrainGraphEdge
    {
        public int fromNodeId;
        public int toNodeId;
        public int order;
        public string label;
        public bool disabled;
    }

    [Serializable]
    public sealed class BrainGraphRuntimeDebugState
    {
        public int currentNodeId = -1;
        public int activeLeafId = -1;
        public int previousNodeId = -1;
        public int lastSuccessNodeId = -1;
        public int lastFailureNodeId = -1;
        public double lastTickTime;
        public int tickVersion;
        public string lastMessage;
        public BrainGraphStatus lastStatus = BrainGraphStatus.None;
    }
}
