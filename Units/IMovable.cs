using UnityEngine;

namespace GameDevTV.RTS.Units
{
    public interface IMovable
    {
        void MoveTo(Vector3 position);
    }
}