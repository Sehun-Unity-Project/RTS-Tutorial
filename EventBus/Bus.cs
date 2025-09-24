namespace GameDevTV.RTS.EventBus
{
    public static class Bus<E> where E : IEvent
    {   
        public delegate void Event(E args);
        public static Event OnEvent;
    }
}