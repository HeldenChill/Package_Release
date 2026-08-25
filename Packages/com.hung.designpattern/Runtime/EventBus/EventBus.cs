using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hung.DesignPattern
{
    public interface IEvent { }

    public static class EventBus<T> where T : IEvent
    {
        static readonly HashSet<IEventBinding<T>> bindings = new();

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
}
