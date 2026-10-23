using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARSolarSystem.UI;

namespace ARSolarSystem
{
    /// <summary>Owns plane taps, world-space placement, and explicit reposition/reset state.</summary>
    public sealed class SolarSystemARController : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameObject solarSystemPrefab;
        [SerializeField] private SolarSystemUIManager uiManager;
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>(8);
        private GameObject placedSystem;
        private bool awaitingPlacement;

        public event Action<SolarSystemBuilder> SystemPlaced;
        public bool IsPlaced => placedSystem != null;
        public bool HasReferences => raycastManager != null && arCamera != null && solarSystemPrefab != null && uiManager != null;

        public void Configure(ARRaycastManager raycasts, Camera camera, GameObject prefab, SolarSystemUIManager ui)
        {
            raycastManager = raycasts; arCamera = camera; solarSystemPrefab = prefab; uiManager = ui;
        }

        private void Start()
        {
            if (raycastManager == null || arCamera == null || solarSystemPrefab == null)
            {
                SetStatus("Setup incomplete: AR placement references are missing.");
                Debug.LogError("SolarSystemARController has missing AR references or Solar System prefab.", this);
                enabled = false;
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
#endif
            SetStatus("Move your phone slowly to find a horizontal surface.");
            OnSessionStateChanged(new ARSessionStateChangedEventArgs(ARSession.state));
        }

        private void OnEnable() => ARSession.stateChanged += OnSessionStateChanged;
        private void OnDisable() => ARSession.stateChanged -= OnSessionStateChanged;

        private void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            switch (args.state)
            {
                case ARSessionState.Unsupported: SetStatus("This device does not support ARCore."); break;
                case ARSessionState.NeedsInstall: SetStatus("Install or update Google Play Services for AR, then reopen the app."); break;
                case ARSessionState.Installing: SetStatus("Google Play Services for AR is installing…"); break;
                case ARSessionState.CheckingAvailability: SetStatus("Checking ARCore availability…"); break;
                case ARSessionState.SessionInitializing: SetStatus("AR is starting. Move your phone slowly to scan the area."); break;
                case ARSessionState.Ready:
                case ARSessionState.SessionTracking:
                    if (!IsPlaced) SetStatus("AR is ready. Scan for a level surface, then tap Place Solar System.");
                    break;
            }
        }

        private void Update()
        {
            if (!awaitingPlacement) return;
            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !IsPointerOverUi(touch.fingerId)) { TryPlace(touch.position); return; }
                }
                return;
            }
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi(-1)) TryPlace(Input.mousePosition);
        }

        private static bool IsPointerOverUi(int fingerId)
        {
            var events = EventSystem.current;
            return events != null && (fingerId < 0 ? events.IsPointerOverGameObject() : events.IsPointerOverGameObject(fingerId));
        }

        public void BeginPlacement()
        {
            awaitingPlacement = true;
            if (placedSystem != null) RemovePlacedSystem();
            SetStatus("Aim at a detected surface, then tap to place the Solar System.");
        }

        public void ResetPlacement()
        {
            awaitingPlacement = false;
            RemovePlacedSystem();
            SetStatus("Placement cleared. Tap Place Solar System to choose a new surface.");
        }

        private void TryPlace(Vector2 screenPosition)
        {
            if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                SetStatus("No detected surface at that point. Keep scanning and try again.");
                return;
            }

            var hit = hits[0];
            try
            {
                // Match the known-working AR_solarsystem2022 placement flow:
                // ARRaycastHit.pose is already in AR world space. Keep the
                // content at that world pose and out of the camera hierarchy.
                placedSystem = Instantiate(solarSystemPrefab, hit.pose.position, hit.pose.rotation);
                placedSystem.name = "Placed Solar System";
                awaitingPlacement = false;
                var builder = placedSystem.GetComponent<SolarSystemBuilder>();
                if (builder == null)
                {
                    SetStatus("Solar System prefab is missing its builder.");
                    Debug.LogError("Solar System prefab lacks SolarSystemBuilder.", this);
                    RemovePlacedSystem();
                    return;
                }
                SystemPlaced?.Invoke(builder);
                SetStatus("Solar System placed in AR world space. Select a planet to explore it.");
            }
            catch (Exception exception)
            {
                if (placedSystem != null) Destroy(placedSystem);
                placedSystem = null;
                SetStatus("AR placement failed. Restart placement and try again.");
                Debug.LogException(exception, this);
            }
        }

        private void RemovePlacedSystem()
        {
            if (placedSystem != null) Destroy(placedSystem);
            placedSystem = null;
        }

        private void SetStatus(string message)
        {
            if (uiManager != null) uiManager.SetStatus(message);
            else Debug.Log(message, this);
        }
    }
}
