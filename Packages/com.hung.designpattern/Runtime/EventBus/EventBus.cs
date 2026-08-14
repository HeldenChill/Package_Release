using System.Collections.Generic;
using UnityEngine;

namespace Hung.DesignPattern
{
    public interface IEvent { }

    public static class EventBus<T> where T : IEvent
    {
        static readonly HashSet<IEventBinding<T>> bindings = new();

        public static void Subscribe(EventBinding<T> binding) => bindings.Add(binding);
        public static void Unsubscribe(EventBinding<T> binding) => bindings.Remove(binding);
        public static void Raise(T @event)
        {
            foreach (var binding in bindings)
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
