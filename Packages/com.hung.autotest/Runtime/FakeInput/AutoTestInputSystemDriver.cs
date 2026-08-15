using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Hung.AutoTest
{
    /// <summary>
    /// Owns synthesis and cleanup of Input System keyboard/mouse gestures for AutoTest cases.
    /// Creates and owns virtual devices when none are supplied; never removes a pre-existing device.
    /// </summary>
    public sealed class AutoTestInputSystemDriver : IDisposable
    {
        private enum QueuedActionKind
        {
            Text,
            Escape,
            KeyDown,
            KeyUp,
            PointerMove,
            PointerDown,
            PointerUp,
            PointerClick
        }

        private readonly struct QueuedAction
        {
            public QueuedAction(QueuedActionKind kind, char text, Key key, Vector2 position, MouseButton button)
            {
                Kind = kind;
                Text = text;
                Key = key;
                Position = position;
                Button = button;
            }

            public QueuedActionKind Kind { get; }
            public char Text { get; }
            public Key Key { get; }
            public Vector2 Position { get; }
            public MouseButton Button { get; }
        }

        private readonly Queue<QueuedAction> queue = new Queue<QueuedAction>();
        private readonly HashSet<Key> pressedKeys = new HashSet<Key>();
        private bool leftButtonPressed;
        private bool rightButtonPressed;
        private bool middleButtonPressed;
        private readonly bool ownsKeyboard;
        private readonly bool ownsMouse;
        private bool disposed;

        public Keyboard CurrentKeyboard { get; }
        public Mouse CurrentMouse { get; }

        public AutoTestInputSystemDriver(Keyboard keyboard = null, Mouse mouse = null)
        {
            if (keyboard != null)
            {
                CurrentKeyboard = keyboard;
                ownsKeyboard = false;
            }
            else
            {
                CurrentKeyboard = InputSystem.AddDevice<Keyboard>();
                ownsKeyboard = true;
            }

            if (mouse != null)
            {
                CurrentMouse = mouse;
                ownsMouse = false;
            }
            else
            {
                CurrentMouse = InputSystem.AddDevice<Mouse>();
                ownsMouse = true;
            }
        }

        public void QueueText(string text)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(text))
                return;
            foreach (char c in text)
                queue.Enqueue(new QueuedAction(QueuedActionKind.Text, c, default, default, default));
        }

        public void QueueEscape()
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.Escape, default, Key.Escape, default, default));
        }

        public void QueueKeyDown(Key key)
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.KeyDown, default, key, default, default));
        }

        public void QueueKeyUp(Key key)
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.KeyUp, default, key, default, default));
        }

        public void QueuePointerMove(Vector2 screenPosition)
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.PointerMove, default, default, screenPosition, default));
        }

        public void QueuePointerDown(MouseButton button = MouseButton.Left)
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.PointerDown, default, default, default, button));
        }

        public void QueuePointerUp(MouseButton button = MouseButton.Left)
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.PointerUp, default, default, default, button));
        }

        public void QueuePointerClick(MouseButton button = MouseButton.Left)
        {
            ThrowIfDisposed();
            queue.Enqueue(new QueuedAction(QueuedActionKind.PointerClick, default, default, default, button));
        }

        public void PumpOneFrame()
        {
            ThrowIfDisposed();
            if (queue.Count == 0)
                return;

            QueuedAction action = queue.Dequeue();
            switch (action.Kind)
            {
                case QueuedActionKind.Text:
                    InputSystem.QueueTextEvent(CurrentKeyboard, action.Text);
                    break;
                case QueuedActionKind.Escape:
                    PressKey(Key.Escape);
                    InputSystem.Update();
                    ReleaseKey(Key.Escape);
                    break;
                case QueuedActionKind.KeyDown:
                    PressKey(action.Key);
                    break;
                case QueuedActionKind.KeyUp:
                    ReleaseKey(action.Key);
                    break;
                case QueuedActionKind.PointerMove:
                    InputSystem.QueueDeltaStateEvent(CurrentMouse.position, action.Position);
                    break;
                case QueuedActionKind.PointerDown:
                    PressButton(action.Button);
                    break;
                case QueuedActionKind.PointerUp:
                    ReleaseButton(action.Button);
                    break;
                case QueuedActionKind.PointerClick:
                    PressButton(action.Button);
                    InputSystem.Update();
                    ReleaseButton(action.Button);
                    break;
            }

            InputSystem.Update();
        }

        private void PressKey(Key key)
        {
            if (!pressedKeys.Add(key))
                return;
            CurrentKeyboard.CopyState(out KeyboardState state);
            state.Set(key, true);
            InputSystem.QueueStateEvent(CurrentKeyboard, state);
        }

        private void ReleaseKey(Key key)
        {
            if (!pressedKeys.Remove(key))
                return;
            CurrentKeyboard.CopyState(out KeyboardState state);
            state.Set(key, false);
            InputSystem.QueueStateEvent(CurrentKeyboard, state);
        }

        private void PressButton(MouseButton button)
        {
            if (!TrySetButtonState(button, true))
                return;
            CurrentMouse.CopyState(out MouseState state);
            InputSystem.QueueStateEvent(CurrentMouse, state.WithButton(button, true));
        }

        private void ReleaseButton(MouseButton button)
        {
            if (!TrySetButtonState(button, false))
                return;
            CurrentMouse.CopyState(out MouseState state);
            InputSystem.QueueStateEvent(CurrentMouse, state.WithButton(button, false));
        }

        private bool TrySetButtonState(MouseButton button, bool pressed)
        {
            switch (button)
            {
                case MouseButton.Left:
                    if (leftButtonPressed == pressed) return false;
                    leftButtonPressed = pressed;
                    return true;
                case MouseButton.Right:
                    if (rightButtonPressed == pressed) return false;
                    rightButtonPressed = pressed;
                    return true;
                case MouseButton.Middle:
                    if (middleButtonPressed == pressed) return false;
                    middleButtonPressed = pressed;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported mouse button.");
            }
        }

        public void Cleanup()
        {
            if (disposed)
                return;

            List<Key> keysToRelease = new List<Key>(pressedKeys);
            if (keysToRelease.Count > 0)
            {
                CurrentKeyboard.CopyState(out KeyboardState keyboardState);
                foreach (Key key in keysToRelease)
                    keyboardState.Set(key, false);
                InputSystem.QueueStateEvent(CurrentKeyboard, keyboardState);
            }
            pressedKeys.Clear();

            if (CurrentMouse != null && (leftButtonPressed || rightButtonPressed || middleButtonPressed))
            {
                CurrentMouse.CopyState(out MouseState mouseState);
                if (leftButtonPressed) mouseState = mouseState.WithButton(MouseButton.Left, false);
                if (rightButtonPressed) mouseState = mouseState.WithButton(MouseButton.Right, false);
                if (middleButtonPressed) mouseState = mouseState.WithButton(MouseButton.Middle, false);
                InputSystem.QueueStateEvent(CurrentMouse, mouseState);
            }
            leftButtonPressed = false;
            rightButtonPressed = false;
            middleButtonPressed = false;

            InputSystem.Update();
            queue.Clear();

            if (ownsKeyboard && CurrentKeyboard != null && CurrentKeyboard.added)
                InputSystem.RemoveDevice(CurrentKeyboard);
            if (ownsMouse && CurrentMouse != null && CurrentMouse.added)
                InputSystem.RemoveDevice(CurrentMouse);

            disposed = true;
        }

        public void Dispose() => Cleanup();

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(AutoTestInputSystemDriver));
        }
    }
}
