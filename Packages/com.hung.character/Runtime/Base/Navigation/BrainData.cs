using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    using System;
    using Hung.Utilities.Timer;
    public class Trigger
    {
        bool value;
        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                TimerManager.Ins.WaitForFrame(1, () => this.value = false);
            }
        }
    }
    public class BrainData : Data
    {
        public bool Attack1 = false;
        public bool Attack2 = false;
        public bool Attack3 = false;
        public Trigger Jump = new Trigger();
        public bool Dash = false;
        public bool EquipItem = false;
        public Vector3 MoveDirection;
        public Vector2 MouseDeltaPosition;
        public Vector3 SeenEnemyPosition;
        public bool IsSeenEnemyCollider;
        public bool IsSeePosition;
        public bool IsRun = false;
        public bool IsCrouch = false;

        // ── Brain → FSM intent channel (additive, reference-compatible) ──────────────
        // Written by a Brain module each tick, read by future BrainGraph / logic.
        // Defaults keep characters without a Brain driver unaffected (HasMoveTarget=false).
        // PhaseId is generic int on purpose: shared by every character; game casts to its own enum.
        public Vector3 MoveTarget;
        public float MoveSpeed;
        public bool HasMoveTarget;
        public int PhaseId;

        // ── Arrival readback (Brain writes from NavMesh agent, FSM reads) ────────────
        public bool HasArrivedAtDestination;
        public float RemainingDistance;
    }
}
