using System;
using GameDevTV.RTS.Units;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using GameDevTV.RTS.Events;
using GameDevTV.RTS.EventBus;



namespace Player.Move
{
    public class CameraMove : MonoBehaviour
    {
        [SerializeField] private CameraMoveConfig cameraMoveConfig;

        // Rigidbody is used for movement to respect physics and colliders
        [SerializeField] private Rigidbody cameraTarget;

        [SerializeField] private new Camera camera;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private LayerMask selectableUnitMask;
        [SerializeField] private LayerMask moveableUnitMask;
        [SerializeField] private RectTransform selectionBox;

        private CinemachineFollow cinemachineFollow;
        private float zoomStartTime;
        private float rotationStartTime;
        private float maxRotationAmount;
        private Vector3 startingFollowOffset;

        private ISelectable selectedUnit;
        private Vector2 startingMousePosition;

        // New variable to track the drag state
        private bool isDragging = false;

        private void Awake()
        {
            if (!cinemachineCamera.TryGetComponent(out cinemachineFollow))
            {
                Debug.LogError("No Cinemachine Follow found");
            }
            startingFollowOffset = cinemachineFollow.FollowOffset;
            maxRotationAmount = Math.Abs(cinemachineFollow.FollowOffset.z);

            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
        }
        //----------------------------------------------------
        private void OnDestroy()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
        }

        private void HandleUnitSelected(UnitSelectedEvent evt)
        {
            if (selectedUnit != null)
            {
                selectedUnit.DeSelect();
            }

            selectedUnit = evt.Unit;
        }

        //----------------------------------------------------

        private void FixedUpdate()
        {
            Zooming();
            Rotation();
            // Panning only when not dragging
            if (!isDragging)
            {
                Panning();
            }
        }

        // Use Update for input-based actions to avoid missing input frames
        private void Update()
        {
            HandleDragSelect();
            HandleClickSelection();
            RightClick();
        }

        // --------------------------------------------------------------------------
        // Drag Select Logic
        // --------------------------------------------------------------------------
        private void HandleDragSelect()
        {
            if (selectionBox == null) { return; }

            // Start drag
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Set the drag state to true
                isDragging = true;
                    
                    startingMousePosition = Mouse.current.position.ReadValue();
                    selectionBox.gameObject.SetActive(true);
                    // Fix: Reset the box's size and position on a new drag
                    selectionBox.sizeDelta = Vector2.zero;
                    selectionBox.anchoredPosition = startingMousePosition;
                }
                // Continue drag
                else if (Mouse.current.leftButton.isPressed)
                {
                    ResizeSelectionBox();
                }
                // End drag
                else if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    // Set the drag state to false
                    isDragging = false;
                    
                    selectionBox.gameObject.SetActive(false);
                    // Fix: Reset the box's size and position on drag end
                    selectionBox.sizeDelta = Vector2.zero;
                    selectionBox.anchoredPosition = startingMousePosition; // Or just Vector2.zero
                    // TODO: Add logic here to select units within the drag box.
                }
            }

        private void ResizeSelectionBox()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            float width = mousePosition.x - startingMousePosition.x;
            float height = mousePosition.y - startingMousePosition.y;

            selectionBox.anchoredPosition = startingMousePosition + new Vector2(width / 2, height / 2);
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        }

        // --------------------------------------------------------------------------
        // Click Selection Logic
        // --------------------------------------------------------------------------
        private void HandleClickSelection()
        {
            // Do not run this code if a drag is in progress (prevents conflicts)
            if (isDragging)
            {
                return;
            }

            // Only run on the frame the button is released
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Ray cameraRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                if (Physics.Raycast(cameraRay, out hit, float.MaxValue, selectableUnitMask))
                {
                    if (hit.collider.TryGetComponent(out ISelectable selectable))
                    {
                        selectedUnit?.DeSelect();
                        selectable.Select();
                        selectedUnit = selectable;
                    }
                }
                else
                {
                    selectedUnit?.DeSelect();
                    selectedUnit = null;
                }
            }
        }

        // --------------------------------------------------------------------------
        // Right-Click Logic
        // --------------------------------------------------------------------------
        private void RightClick()
        {
            if (selectedUnit == null || selectedUnit is not IMovable moveable) { return; }

            Ray cameraRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Mouse.current.rightButton.wasReleasedThisFrame && Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, moveableUnitMask))
            {
                moveable.MoveTo(hit.point);
            }
        }

        // --------------------------------------------------------------------------
        // Other Camera & Movement Methods
        // --------------------------------------------------------------------------
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

            // Fix: Only start panning if the mouse has moved significantly from the initial position.
            // This prevents the camera from moving if the mouse starts on the edge.
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