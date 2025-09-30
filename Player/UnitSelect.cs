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
    public class UnitSelect : MonoBehaviour
    {
        [SerializeField] private LayerMask selectableUnitMask;
        [SerializeField] private LayerMask moveableUnitMask;
        [SerializeField] private RectTransform selectionBox;
        [SerializeField] private new Camera camera;

        // 유닛 관리를 위한 컬렉션
        private HashSet<AbstractUnit> aliveUnits = new(100);
        private HashSet<AbstractUnit> addedUnits = new(24);
        private List<ISelectable> selectedUnits = new(12);

        // 이벤트 버스 핸들러
        private void HandleUnitSelected(UnitSelectedEvent evt) => selectedUnits.Add(evt.Unit);
        private void HandleUnitDeselected(UnitDeSelectedEvent evt) => selectedUnits.Remove(evt.Unit);
        private void HandleUnitSpawn(UnitSpawnEvent evt) => aliveUnits.Add(evt.Unit);

        private Vector2 startingMousePosition;

    
        // =========================================================================
        // UNITY LIFECYCLE & EVENTS
        // =========================================================================

        private void Awake()
        {
            Bus<UnitSelectedEvent>.OnEvent += HandleUnitSelected;
            Bus<UnitDeSelectedEvent>.OnEvent += HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent += HandleUnitSpawn;
        }

        [Obsolete]
        private void Start()
        {
            // [초기화 수정] 씬에 이미 배치된 유닛을 수동으로 찾아 추가합니다.
            if (aliveUnits.Count == 0)
            {
                AbstractUnit[] allUnitsInScene = FindObjectsOfType<AbstractUnit>();

                if (allUnitsInScene.Length > 0)
                {
                    foreach (var unit in allUnitsInScene)
                    {
                        aliveUnits.Add(unit);
                    }
                    Debug.Log($"[Init Fix] Manually added {aliveUnits.Count} units to aliveUnits list for selection.");
                }
            }
        }

        private void OnDestroy()
        {
            Bus<UnitSelectedEvent>.OnEvent -= HandleUnitSelected;
            Bus<UnitDeSelectedEvent>.OnEvent -= HandleUnitDeselected;
            Bus<UnitSpawnEvent>.OnEvent -= HandleUnitSpawn;
        }

        private void Update()
        {
            HandleDragSelect();
            HandleClickSelection();
            RightClick();
        }

        // =========================================================================
        // DRAG SELECT LOGIC (상세 디버그 추가)
        // =========================================================================

        private void HandleDragSelect()
        {
            if (selectionBox == null) { return; }

            // Start drag
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                startingMousePosition = Mouse.current.position.ReadValue();
                selectionBox.gameObject.SetActive(true);

                selectionBox.sizeDelta = Vector2.zero;
                selectionBox.anchoredPosition = startingMousePosition;

            }
            // Continue drag
            else if (Mouse.current.leftButton.isPressed)
            {
                addedUnits.Clear();
                if (!Keyboard.current.shiftKey.isPressed)
                {
                  Debug.Log("DeSelecting");
                  DeselectAllUnits();
                }

                Bounds selectionBount = ResizeSelectionBox();

                if (aliveUnits.Count == 0)
                {
                    Debug.LogWarning("DEBUG 2: aliveUnits list is EMPTY! No units to select.");
                }

                foreach (AbstractUnit unit in aliveUnits)
                {
                    Vector2 unitPosition = camera.WorldToScreenPoint(unit.transform.position);

                    if (selectionBount.Contains(unitPosition))
                    {
                        addedUnits.Add(unit);
                    }
                }
            }

            // End drag
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                foreach (AbstractUnit unit in addedUnits)
                {
                    if (unit is ISelectable selectable)
                    {
                        unit.Select();
                        selectedUnits.Add(unit);
                    }
                }

                addedUnits.Clear();

                selectionBox.gameObject.SetActive(false);
                selectionBox.sizeDelta = Vector2.zero;
                selectionBox.anchoredPosition = startingMousePosition;
            }
        }

        private Bounds ResizeSelectionBox()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            float width = mousePosition.x - startingMousePosition.x;
            float height = mousePosition.y - startingMousePosition.y;

            // UI 시각적 표시 업데이트
            selectionBox.anchoredPosition = startingMousePosition + new Vector2(width / 2, height / 2);
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));

            // Bounds 계산 (raw screen space 사용)
            float minX = Mathf.Min(startingMousePosition.x, mousePosition.x);
            float maxX = Mathf.Max(startingMousePosition.x, mousePosition.x);
            float minY = Mathf.Min(startingMousePosition.y, mousePosition.y);
            float maxY = Mathf.Max(startingMousePosition.y, mousePosition.y);

            // Z=0을 중심으로 하는 스크린 좌표 Bounds
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 1f);

            return new Bounds(center, size);
        }

        // =========================================================================
        // CLICK & DESELECTION LOGIC
        // =========================================================================

        private void HandleClickSelection()
        {
            // 드래그 중이 아니며, 마우스 버튼을 뗀 순간에만 실행합니다.
            if (Mouse.current.leftButton.isPressed)
            {

                Ray cameraRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                // 광선 투사가 유닛을 맞춘 경우
                if (Physics.Raycast(cameraRay, out hit, float.MaxValue, selectableUnitMask))
                {
                    if (hit.collider.TryGetComponent(out ISelectable selectable))
                    {
                        selectable.Select();
                        selectedUnits.Add(selectable);
                    }
                }
                // 유닛을 맞추지 못한 경우 (빈 공간 클릭)
                else if (!Keyboard.current.anyKey.isPressed)
                {
                    Debug.Log("DeSelecting 2");
                    DeselectAllUnits();
                }
            }
        }

        private void DeselectAllUnits()
        {
            if (selectedUnits.Count == 0) return;

            List<ISelectable> unitsToDeselect = selectedUnits.ToList();

            foreach (var unit in unitsToDeselect)
            {
                unit.DeSelect();
            }

            selectedUnits.Clear();
        }

        // =========================================================================
        // RIGHT-CLICK & MOVEMENT
        // =========================================================================

        private void RightClick()
        {
            if (selectedUnits.Count == 0) { return; }

            Ray cameraRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Mouse.current.rightButton.wasReleasedThisFrame
                && Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue))
            {
                foreach (ISelectable selectable in selectedUnits)
                {
                    if (selectable is IMovable moveable)
                    {
                        moveable.MoveTo(hit.point);
                    }
                }
            }
        }

        // =========================================================================
        // CAMERA MOVEMENT & UTILITIES
        // =========================================================================

    }
}