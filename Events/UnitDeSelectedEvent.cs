using System;
using GameDevTV.RTS.EventBus;
using GameDevTV.RTS.Units;

namespace GameDevTV.RTS.Events
{
    public struct UnitDeSelectedEvent : IEvent
    {
        public ISelectable Unit { get; private set; }

        public UnitDeSelectedEvent(ISelectable unit)
        {
            Unit = unit;
        }
    }
}
