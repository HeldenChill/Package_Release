using System;
using System.Collections.Generic;

namespace Hung.UI
{
    public class UIBackStack
    {
        private readonly Dictionary<UICanvas, Action> backActions = new();
        private readonly List<UICanvas> stack = new();

        public UICanvas Top => stack.Count > 0 ? stack[^1] : null;

        public IReadOnlyList<UICanvas> Canvases => stack;

        public void Push(UICanvas canvas, Action backAction)
        {
            if (!stack.Contains(canvas)) stack.Add(canvas);
            if (backAction != null) backActions[canvas] = backAction;
        }

        public void Remove(UICanvas canvas)
        {
            stack.Remove(canvas);
            backActions.Remove(canvas);
        }

        public void InvokeBack()
        {
            UICanvas top = Top;
            if (top == null) return;
            if (backActions.TryGetValue(top, out Action action)) action?.Invoke();
        }

        public void Clear()
        {
            stack.Clear();
            backActions.Clear();
        }
    }
}
