using GameDevTV.RTS.Units;

namespace GameDevTV.RTS.EventBus
{
    public struct UnitSelectedEvent : IEvent
    {
        public ISelectable Unit { get; private set; }
        public UnitSelectedEvent(ISelectable unit)
        {
            this.Unit = unit;
        }
    }
}