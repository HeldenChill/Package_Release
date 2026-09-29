using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hung.DesignPattern
{
    public interface IEvent { }

    public static class EventBus<T> where T : IEvent
    {
        static readonly HashSet<IEventBinding<T>> bindings = new();

        // Every closed EventBus<T> registers its reset once, on first touch, so
        // EventBusRegistry can clear all of them without knowing the event types.
        static EventBus() => EventBusRegistry.Register(() => bindings.Clear());

        public static void Subscribe(EventBinding<T> binding) => bindings.Add(binding);
        public static void Unsubscribe(EventBinding<T> binding) => bindings.Remove(binding);

        /// <summary>
        /// Invokes every current subscriber. Handlers may safely Subscribe or Unsubscribe
        /// during the raise: the binding set is snapshotted first, so mutations apply to
        /// the NEXT raise. A binding added mid-raise does not receive the in-flight event;
        /// a binding removed mid-raise still receives it.
        /// </summary>
        public static void Raise(T @event)
        {
            // Snapshot before iterating. Enumerating the live set let any handler that
            // mutated its own bus throw out of MoveNext - which the per-iteration catch
            // below cannot intercept - silently skipping every later binding.
            foreach (var binding in bindings.ToArray())
            {
                try
                {
                    binding.OnEvent.Invoke(@event);
                    binding.OnEventNoArgs.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }

    /// <summary>
    /// Clears every EventBus binding set when play mode starts. With Enter Play Mode Options
    /// disabling domain reload, the static sets survive stopping play: a subscriber whose
    /// owner was destroyed without unsubscribing (play stopped mid-phase) keeps receiving
    /// events in the next session and touches destroyed objects.
    /// </summary>
    public static class EventBusRegistry
    {
        static readonly List<System.Action> resets = new();

        internal static void Register(System.Action reset) => resets.Add(reset);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAllBindings()
        {
            foreach (var reset in resets) reset();
        }
    }
}
