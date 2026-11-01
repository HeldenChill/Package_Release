# Hung AI (`com.hung.ai`)

Reusable behavior-tree AI for Unity. No dependency on any Hung character framework.

## Concepts

| Type | Role |
|---|---|
| `AIGraphAsset` | ScriptableObject graph: nodes (`[SerializeReference]`), root id, blackboard keys |
| `AIAgent` | Component that runs a graph on a GameObject (auto tick or host tick) |
| `Blackboard` | Per-agent typed values for the graph's keys; per-agent overrides on `AIAgent` |
| `IAIMover` | Movement abstraction used by Move To / Patrol / Stop Move |
| `NavMeshMover` | Optional `IAIMover` in `Hung.AI.NavMesh` |

## Node lifecycle

`OnStart` once on entry → `OnUpdate` each tick while Running → `OnEnd(status)` once. When interrupted, `OnAbort` runs, then `OnEnd(Aborted)`. Put cleanup in `OnEnd`.

## Conditional aborts

Set `AbortType` on a condition:
- `Self` — inside a Sequence, re-checked while later children run; false aborts them.
- `LowerPriority` — as a Selector child (or first conditions of a Sequence child), re-checked while a lower child runs; true aborts the lower child and switches.
- `Both`.

## Writing a node

```csharp
[System.Serializable, Hung.AI.AINode("Custom/Shout", Description = "Logs a shout.")]
public sealed class ShoutNode : Hung.AI.ActionNode
{
    [UnityEngine.SerializeField] string text = "Hey!";
    protected override Hung.AI.NodeStatus OnUpdate(Hung.AI.AIContext context)
    {
        UnityEngine.Debug.Log(text, context.Owner);
        return Hung.AI.NodeStatus.Success;
    }
}
```

Any assembly can add nodes. Renaming or moving a node class needs `[UnityEngine.Scripting.APIUpdating.MovedFrom(...)]` or saved graphs lose that node.

## Host-driven ticking

```csharp
agent.AutoTick = false;
agent.Mover = myMover;
agent.Initialize();
agent.Tick(Time.deltaTime);
```

`Pause()` aborts the active branch and stops the mover so a scripted sequence can take the body; `Resume()` restarts from the root.

## Graph editor

Open it by double-clicking an AI Graph asset, with **Open Graph Editor** on the asset or `AIAgent` inspector, or from **Tools > Character > AI Graph Editor**.

| Action | How |
|---|---|
| Create a node | Space, right-click > Create Node, or drag an edge onto empty space |
| Connect | Drag from a node's bottom port to another node's top port. Child order = left to right |
| Root | The first node created is the root; right-click > Set as Root to change it |
| Copy / cut / paste / duplicate / delete | Ctrl+C / Ctrl+X / Ctrl+V / Ctrl+D / Delete |
| Undo / redo | Ctrl+Z / Ctrl+Y — every edit is undoable |
| Frame | F frames the selection, A frames everything |
| Group, sticky note, align | Right-click menu |
| Blackboard | Left panel: add (+), rename, retype, delete keys, set defaults. Drag a key's handle onto a key field in the inspector |
| Node fields | Right panel shows the selected node's own fields |
| Problems | Red badge on a node (hover it), and the status text in the toolbar |
| Missing node class | Red "Missing" node; restore the class or use Remove Missing Nodes |

## Live debugging

Enter Play mode, then pick the agent in the window's **Debug Agent** field (or select its GameObject). Running nodes and edges are highlighted; nodes that just finished flash green (success), red (failure) or grey (aborted). The blackboard panel switches to live values. The `AIAgent` inspector also shows live values and per-agent overrides.

## Not in this version

Character bridge (Phase 3), HFSM state node, Shared/Global blackboard scopes (reserved), event-driven aborts, subgraphs, breakpoints.
