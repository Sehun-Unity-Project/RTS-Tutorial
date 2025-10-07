using System;
using GameDevTV.RTS.Units;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using GameDevTV.RTS.Events;
using GameDevTV.RTS.EventBus;
using System.Collections.Generic;
using System.Linq; // List.ToList()


namespace Player.Move
{
    public class CameraMove : MonoBehaviour
    {
        [SerializeField] private CameraMoveConfig cameraMoveConfig;
        [SerializeField] private Rigidbody cameraTarget;
        [SerializeField] private new Camera camera;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        // [SerializeField] private LayerMask selectableUnitMask;
        // [SerializeField] private LayerMask moveableUnitMask;
        // [SerializeField] private RectTransform selectionBox;

        private CinemachineFollow cinemachineFollow;
        private float zoomStartTime;
        private float rotationStartTime;
        private float maxRotationAmount;
        private Vector3 startingFollowOffset;
        private Vector2 startingMousePosition;
        private bool isDragging = false; 
        

        private void Awake()
        {
            if (!cinemachineCamera.TryGetComponent(out cinemachineFollow))
            {
                Debug.LogError("No Cinemachine Follow found");
            }
            startingFollowOffset = cinemachineFollow.FollowOffset;
            maxRotationAmount = Math.Abs(cinemachineFollow.FollowOffset.z);
        }

        private void FixedUpdate()
        {
            Zooming();
            Rotation();
            if (!isDragging) Panning(); 
        }

        private void Panning()
        {
            Vector2 moveAmount = KeyBoardPanning();
            moveAmount += MousePanning();

            cameraTarget.linearVelocity = new Vector3(Math.Min(moveAmount.x, 30), 0, moveAmount.y);
        }

        Vector2 MousePanning()
        {
            Vector2 moveAmount = Vector2.zero;
            if (!cameraMoveConfig.EnableEdgePan)
            {
                return moveAmount;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (mousePosition == Vector2.zero) 
            {
                return moveAmount;
            }

            if (mousePosition.x <= cameraMoveConfig.EdgePanSize)
            {
                moveAmount.x -= cameraMoveConfig.MousePanSpeed;
            }
            else if (mousePosition.x >= screenWidth - cameraMoveConfig.EdgePanSize)
            {
                moveAmount.x += cameraMoveConfig.MousePanSpeed;
            }

            if (mousePosition.y <= cameraMoveConfig.EdgePanSize)
            {
                moveAmount.y -= cameraMoveConfig.MousePanSpeed;
            }
            else if (mousePosition.y >= screenHeight - cameraMoveConfig.EdgePanSize)
            {
                moveAmount.y += cameraMoveConfig.MousePanSpeed;
            }

            return moveAmount;
        }
        
        Vector2 KeyBoardPanning()
        {
            Vector2 moveAmount = Vector2.zero;
            if (Keyboard.current.upArrowKey.isPressed)
            {
                moveAmount.y += cameraMoveConfig.KeyBoardPanSpeed;
            }
            if (Keyboard.current.downArrowKey.isPressed)
            {
                moveAmount.y -= cameraMoveConfig.KeyBoardPanSpeed;
            }
            if (Keyboard.current.leftArrowKey.isPressed)
            {
                moveAmount.x -= cameraMoveConfig.KeyBoardPanSpeed;
            }
            if (Keyboard.current.rightArrowKey.isPressed)
            {
                moveAmount.x += cameraMoveConfig.KeyBoardPanSpeed;
            }
            return moveAmount;
        }

        private void Rotation()
        {
            if (SouldSetRotationStartTime())
            {
                rotationStartTime = Time.time;
            }

            float rotationTime = Mathf.Clamp01((Time.time - rotationStartTime) * cameraMoveConfig.RotationSpeed);
            Vector3 targetFollowOffset;

            if (Keyboard.current.pageDownKey.isPressed)
            {
                targetFollowOffset = new Vector3(maxRotationAmount, cinemachineFollow.FollowOffset.y, 0);
            }
            else if (Keyboard.current.pageUpKey.isPressed)
            {
                targetFollowOffset = new Vector3(-maxRotationAmount, cinemachineFollow.FollowOffset.y, 0);
            }
            else
            {
                targetFollowOffset = new Vector3(
                    startingFollowOffset.x,
                    cinemachineFollow.FollowOffset.y,
                    startingFollowOffset.z
                );
            }

            cinemachineFollow.FollowOffset = Vector3.Slerp(
                cinemachineFollow.FollowOffset,
                targetFollowOffset,
                rotationTime
            );
        }

        private void Zooming()
        {
            if (EndKeyIsPressed())
            {
                zoomStartTime = Time.time;
            }

            Vector3 targetFollowOffset;
            float zoomTime = Mathf.Clamp01((Time.time - zoomStartTime) * cameraMoveConfig.ZoomSpeed);

            if (Keyboard.current.endKey.isPressed)
            {
                targetFollowOffset = new Vector3(
                    cinemachineFollow.FollowOffset.x,
                    cameraMoveConfig.MinZoomDistance,
                    cinemachineFollow.FollowOffset.z
                );
            }
            else
            {
                targetFollowOffset = new Vector3(
                    cinemachineFollow.FollowOffset.x,
                    startingFollowOffset.y,
                    cinemachineFollow.FollowOffset.z
                );
            }

            cinemachineFollow.FollowOffset = Vector3.Slerp(
                cinemachineFollow.FollowOffset,
                targetFollowOffset,
                zoomTime
            );
        }

        private bool EndKeyIsPressed()
        {
            return Keyboard.current.endKey.wasPressedThisFrame || Keyboard.current.endKey.wasReleasedThisFrame;
        }

        private bool SouldSetRotationStartTime()
        {
            return Keyboard.current.pageDownKey.wasReleasedThisFrame
                || Keyboard.current.pageUpKey.wasReleasedThisFrame
                || Keyboard.current.pageDownKey.wasPressedThisFrame
                || Keyboard.current.pageUpKey.wasPressedThisFrame;
        }
    }
}