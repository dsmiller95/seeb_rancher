﻿using UnityEngine;
using System.Collections;
using System;
using Dman.Tiling;
using Dman.Tiling.SquareCoords;
using Dman.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.UI.Manipulators.Scripts
{
    /// <summary>
    /// provider for drag-selecting
    /// </summary>
    public class DragAreaSelector : MonoBehaviour
    {

        public Func<GameObject, bool> validSelection = (x) => true;
        public RaycastGroup targetCastGroup;

        public event Action<IEnumerable<GameObject>> OnObjectSetIntersected;

        private bool dragging = false;
        private UniversalCoordinate originDragPoint = default;
        private UniversalCoordinateRange lastDragRange = default;
        public GameObject dragAreaRenderer;

        // Update is called once per frame
        void Update()
        {
            var hoveredCoordinate = GetHoveredCoordinate();

            if (!Input.GetMouseButton(0))
            {
                if (dragging)
                {
                    if (hoveredCoordinate != null)
                    {
                        var currentDraggingPoint = hoveredCoordinate.Value;
                        lastDragRange = UniversalCoordinateRange.From(RectCoordinateRange.FromCoordsInclusive(currentDraggingPoint.squareDataView, originDragPoint.squareDataView), originDragPoint.CoordinatePlaneID);
                    }
                    Debug.Log("Apply at range:");
                    Debug.Log(lastDragRange);

                    dragging = false;
                    originDragPoint = default;
                    dragAreaRenderer.SetActive(false);
                }
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (hoveredCoordinate == null)
                {
                    return;
                }
                dragging = true;
                originDragPoint = hoveredCoordinate.Value;
            }

            if (dragging)
            {
                if (Input.GetMouseButton(0) && hoveredCoordinate != null)
                {
                    var currentDraggingPoint = hoveredCoordinate.Value;
                    lastDragRange = UniversalCoordinateRange.From(RectCoordinateRange.FromCoordsInclusive(currentDraggingPoint.squareDataView, originDragPoint.squareDataView), originDragPoint.CoordinatePlaneID);

                    var coords = lastDragRange.BoundingPolygon();
                    var vMin = coords.OrderBy(x => x.x + x.y).First();
                    var vMax = coords.OrderByDescending(x => x.x + x.y).First();
                    var vAvg = (vMin + vMax) / 2;
                    dragAreaRenderer.SetActive(true);
                    dragAreaRenderer.transform.localScale = new Vector3(vMax.x - vMin.x, 1, vMax.y - vMin.y);
                    dragAreaRenderer.transform.position = new Vector3(vAvg.x, dragAreaRenderer.transform.position.y, vAvg.y);
                }
            }
        }
        private UniversalCoordinate? GetHoveredCoordinate()
        {
            var mouseOvered = targetCastGroup.CurrentlyHitObject;
            var hoveredGameObject = mouseOvered.HasValue ? mouseOvered.Value.collider.gameObject : null;
            return hoveredGameObject?.GetComponentInParent<TileMember>().CoordinatePosition;
        }

    }
}