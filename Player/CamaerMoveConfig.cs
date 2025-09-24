using UnityEngine;

namespace Player.Move
{   
    [System.Serializable]
    public class CameraMoveConfig
    {   
        [field: SerializeField] public bool EnableEdgePan { get; private set; } = true;
        [field: SerializeField] public float MousePanSpeed { get; private set; } = 50f;
        [field: SerializeField] public float EdgePanSize { get; private set; } = 50;

        [field: SerializeField] public float KeyBoardPanSpeed { get; private set; } = 30f;
        
        [field: SerializeField] public float MinZoomDistance { get; private set; } = 7.5f;
        [field: SerializeField] public float ZoomSpeed { get; private set; } = 1f;

        [field: SerializeField] public float RotationSpeed { get; private set; } = 1f;
    }
}