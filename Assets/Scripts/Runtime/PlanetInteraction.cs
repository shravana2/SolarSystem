using UnityEngine;
using UnityEngine.EventSystems;
using ARSolarSystem.UI;

namespace ARSolarSystem
{
    /// <summary>Screen-space tap selection for planets; UI pointer targets are ignored.</summary>
    public sealed class PlanetInteraction : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private PlanetInfoPanel infoPanel;
        [SerializeField] private LayerMask selectableLayers = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public bool HasReferences => viewCamera != null && infoPanel != null;

        public void Configure(Camera camera, PlanetInfoPanel panel)
        {
            viewCamera = camera;
            infoPanel = panel;
        }

        private void Update()
        {
            if (viewCamera == null || infoPanel == null) return;

            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !IsPointerOverUi(touch.fingerId))
                        SelectAt(touch.position);
                }
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
                SelectAt(Input.mousePosition);
        }

        private bool IsPointerOverUi(int pointerId = -1)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            return pointerId < 0 ? eventSystem.IsPointerOverGameObject() : eventSystem.IsPointerOverGameObject(pointerId);
        }

        private void SelectAt(Vector2 screenPosition)
        {
            var ray = viewCamera.ScreenPointToRay(screenPosition);
            var count = Physics.RaycastNonAlloc(ray, hits, 1000f, selectableLayers, QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            Collider nearestCollider = null;
            for (var i = 0; i < count; i++)
            {
                if (hits[i].distance >= nearestDistance) continue;
                nearestCollider = hits[i].collider;
                nearestDistance = hits[i].distance;
            }
            var selected = nearestCollider != null ? nearestCollider.GetComponentInParent<PlanetSelectable>() : null;
            if (selected != null && selected.Data != null) infoPanel.Show(selected.Data);
        }
    }
}
