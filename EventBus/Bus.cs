using System;
using GameDevTV.RTS.Events;

namespace GameDevTV.RTS.EventBus
{
    public static class Bus<E> where E : IEvent
    {
        public delegate void Event(E args);
        public static event Event OnEvent; // only the bus can invoke now

        public static void Raise(E evt) => OnEvent?.Invoke(evt);

        internal static void RaiseEvent(E unitSelectedEvent)
        {
            OnEvent?.Invoke(unitSelectedEvent);
        }
    }
}