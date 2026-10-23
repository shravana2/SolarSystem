using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.Management;
using ARSolarSystem.UI;
using ARSolarSystem;

namespace ARSolarSystem.Editor
{
    public static class ARSolarSystemPhase1Setup
    {
        private const string ScenePath = "Assets/Scenes/ARMain.unity";
        private const string PlanePrefabPath = "Assets/Prefabs/DetectedPlane.prefab";
        private const string MaterialPath = "Assets/Materials/DetectedPlane.mat";

        [MenuItem("Tools/AR Solar System/Build or Repair Project")]
        public static void BuildOrRepair()
        {
            EnsureFolders();
            EnsureTextMeshProResources();
            var planeMaterial = LoadOrCreatePlaneMaterial();
            var planePrefab = CreateOrUpdatePlanePrefab(planeMaterial);
            SolarSystemContentSetup.BuildSolarSystemPrefab();
            CreateOrUpdateScene(planePrefab);
            ConfigurePlayerSettings();
            ConfigureAndroidXRLoader();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("AR Solar System project setup completed. Open Assets/Scenes/ARMain.unity.");
        }

        [MenuItem("Tools/AR Solar System/Apply Reference AR Camera Setup")]
        public static void ApplyReferenceARCameraSetup()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForRepair = !scene.IsValid() || !scene.isLoaded;
            if (openedForRepair) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            var origin = FindInScene<XROrigin>(scene);
            if (origin == null || origin.Camera == null)
            {
                Debug.LogError("Could not repair AR camera hierarchy: XR Origin or its Camera reference is missing.");
                if (openedForRepair && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            var oldOffset = origin.CameraFloorOffsetObject;
            var camera = origin.Camera;
            camera.transform.SetParent(origin.transform, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            camera.transform.localScale = Vector3.one;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            ConfigureTrackedPoseDriver(camera);
            origin.CameraFloorOffsetObject = null;
            origin.CameraYOffset = 0f;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
            if (oldOffset != null && oldOffset.transform != origin.transform && oldOffset.transform.childCount == 0)
                UnityEngine.Object.DestroyImmediate(oldOffset.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedForRepair && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Applied the reference AR camera hierarchy to ARMain without rebuilding the UI. Rebuild the Android APK to include the change.");
        }

        [MenuItem("Tools/AR Solar System/Validate Project")]
        public static void ValidateProject()
        {
            var errors = 0;
            if (!File.Exists("Packages/manifest.json")) { Debug.LogError("Missing Packages/manifest.json"); errors++; }
            if (!ValidateAndroidXRLoader()) errors++;
            if (!ValidateAndroidGraphicsApis()) errors++;
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene == null) { Debug.LogError($"Missing scene: {ScenePath}. Run Build or Repair Project."); errors++; }
            else
            {
                var previous = SceneManager.GetActiveScene();
                var target = SceneManager.GetSceneByPath(ScenePath);
                var openedForValidation = !target.IsValid() || !target.isLoaded;
                if (openedForValidation) target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                CheckInScene<ARSession>(target, "AR Session", ref errors);
                CheckInScene<XROrigin>(target, "XR Origin", ref errors);
                CheckInScene<ARPlaneManager>(target, "AR Plane Manager", ref errors);
                CheckInScene<ARRaycastManager>(target, "AR Raycast Manager", ref errors);
                CheckInScene<ARAnchorManager>(target, "AR Anchor Manager", ref errors);
                CheckInScene<SolarSystemARController>(target, "Solar System AR Controller", ref errors);
                CheckInScene<SolarSystemUIManager>(target, "Solar System UI Manager", ref errors);
                CheckInScene<PlanetInteraction>(target, "Planet Interaction", ref errors);
                CheckInScene<PlanetInfoPanel>(target, "Planet Info Panel", ref errors);
                CheckInScene<Canvas>(target, "UI Canvas", ref errors);
                CheckInScene<EventSystem>(target, "UI EventSystem", ref errors);
                var origin = FindInScene<XROrigin>(target);
                if (origin != null && origin.Camera != null && origin.Camera.transform.parent != origin.transform)
                {
                    Debug.LogError("AR Camera must be a direct child of XR Origin (matching the working AR_solarsystem2022 project). Run Build or Repair Project.");
                    errors++;
                }
                if (origin != null && origin.CameraFloorOffsetObject != null)
                {
                    Debug.LogError("XR Origin Camera Floor Offset Object must be empty for this mobile AR setup. Run Build or Repair Project.");
                    errors++;
                }
                if (origin != null && Mathf.Abs(origin.CameraYOffset) > 0.001f)
                {
                    Debug.LogError("XR Origin Camera Y Offset must be zero for mobile AR. Run Apply Reference AR Camera Setup.");
                    errors++;
                }
                if (origin != null && origin.Camera != null && !HasConfiguredTrackedPoseDriver(origin.Camera))
                {
                    Debug.LogError("AR Camera is missing a configured Tracked Pose Driver. Without camera pose updates, AR content can appear fixed to the screen. Run Apply Reference AR Camera Setup.");
                    errors++;
                }
                var planetInteraction = FindInScene<PlanetInteraction>(target);
                if (planetInteraction != null && !planetInteraction.HasReferences) { Debug.LogError("Planet Interaction camera or information panel reference is missing."); errors++; }
                var infoPanel = FindInScene<PlanetInfoPanel>(target);
                if (infoPanel != null && !infoPanel.IsConfigured) { Debug.LogError("Planet Info Panel is missing one or more UI references."); errors++; }
                var arController = FindInScene<SolarSystemARController>(target);
                if (arController != null && !arController.HasReferences) { Debug.LogError("Solar System AR Controller is missing raycast, anchor, camera, prefab, or UI reference."); errors++; }
                var uiManager = FindInScene<SolarSystemUIManager>(target);
                if (uiManager != null && !uiManager.IsConfigured) { Debug.LogError("Solar System UI Manager is missing one or more control references."); errors++; }
                if (openedForValidation)
                {
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    // Never close the Editor's only loaded scene.
                    if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(target, true);
                }
            }
            if (!SolarSystemContentSetup.ValidateSolarSystemContent()) errors++;
            if (errors == 0) Debug.Log("Project validation passed: Android ARCore loader and OpenGLES3 graphics API, AR scene and placement references, generated controls, Solar System content, selection, animation, and independent scale smoke checks succeeded. Runtime AR behavior still needs Play mode/device testing.");
            else Debug.LogError($"Project validation found {errors} issue(s).");
        }

        private static void CheckInScene<T>(Scene scene, string label, ref int errors) where T : Component
        {
            if (FindInScene<T>(scene) != null) return;
            Debug.LogError($"Scene is missing {label}.");
            errors++;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static void EnsureFolders()
        {
            foreach (var folder in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Materials", "Assets/Scripts", "Assets/Scripts/UI", "Assets/Documentation" })
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                    var name = Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(parent, name);
                }
        }

        private static Material LoadOrCreatePlaneMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("Legacy Shaders/Transparent/Diffuse") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "DetectedPlane" };
            if (material.HasProperty("_Color")) material.color = new Color(0.15f, 0.8f, 1f, 0.22f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static GameObject CreateOrUpdatePlanePrefab(Material material)
        {
            // Rebuild this generated prefab from a clean object so an older or
            // partially-created prefab cannot retain a broken component state.
            var root = new GameObject("DetectedPlane");
            root.AddComponent<MeshFilter>();
            var meshRenderer = root.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            root.AddComponent<ARPlane>();
            root.AddComponent<ARPlaneMeshVisualizer>();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PlanePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateOrUpdateScene(GameObject planePrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sessionObject = new GameObject("AR Session");
            sessionObject.AddComponent<ARSession>();

            var originObject = new GameObject("XR Origin");
            var origin = originObject.AddComponent<XROrigin>();
            originObject.AddComponent<ARInputManager>();
            var planeManager = originObject.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            originObject.AddComponent<ARRaycastManager>();
            originObject.AddComponent<ARAnchorManager>();

            var cameraObject = new GameObject("AR Camera", typeof(Camera), typeof(AudioListener), typeof(ARCameraManager), typeof(ARCameraBackground));
            cameraObject.tag = "MainCamera";
            // Match the known-working AR_solarsystem2022 hierarchy: AR camera
            // is directly under XR Origin, with no device-height offset object.
            cameraObject.transform.SetParent(originObject.transform, false);
            // The model is deliberately tabletop-sized; Unity's default 0.3 m
            // near plane can clip nearby orbit paths and small bodies.
            var arCamera = cameraObject.GetComponent<Camera>();
            arCamera.nearClipPlane = 0.05f;
            arCamera.farClipPlane = 100f;
            ConfigureTrackedPoseDriver(arCamera);
            origin.Camera = arCamera;
            origin.CameraFloorOffsetObject = null;
            origin.CameraYOffset = 0f;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;

            var infoPanel = CreatePlanetInfoUI();
            var interactionObject = new GameObject("Planet Interaction");
            interactionObject.AddComponent<PlanetInteraction>().Configure(origin.Camera, infoPanel);

            var solarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SolarSystem.prefab");
            var canvasObject = infoPanel.transform.root.gameObject;
            var uiManager = canvasObject.AddComponent<SolarSystemUIManager>();
            var controllerObject = new GameObject("Solar System AR Controller");
            var arController = controllerObject.AddComponent<SolarSystemARController>();
            var controls = CreateMainControlsUI(canvasObject.transform);
            uiManager.Configure(arController, controls.place, controls.reset, controls.pause, controls.resetSettings,
                controls.applySize, controls.sunInput, controls.distanceSlider, controls.distanceLabel, controls.pauseLabel,
                controls.status, controls.instructions, controls.mode, infoPanel);
                arController.Configure(originObject.GetComponent<ARRaycastManager>(), origin.Camera, solarPrefab, uiManager);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigureTrackedPoseDriver(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            var driver = camera.GetComponent<TrackedPoseDriver>();
            if (driver == null) driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.ignoreTrackingState = false;
            driver.positionInput = new InputActionProperty(new InputAction(
                "AR Camera Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction(
                "AR Camera Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            driver.trackingStateInput = new InputActionProperty(new InputAction(
                "AR Camera Tracking State", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));
            EditorUtility.SetDirty(driver);
        }

        private static bool HasConfiguredTrackedPoseDriver(Camera camera)
        {
            var driver = camera != null ? camera.GetComponent<TrackedPoseDriver>() : null;
            return driver != null && driver.trackingType == TrackedPoseDriver.TrackingType.RotationAndPosition &&
                HasBinding(driver.positionInput, "<XRHMD>/centerEyePosition") &&
                HasBinding(driver.rotationInput, "<XRHMD>/centerEyeRotation") &&
                HasBinding(driver.trackingStateInput, "<XRHMD>/trackingState");
        }

        private static bool HasBinding(InputActionProperty property, string expectedPath)
        {
            var action = property.action;
            if (action == null || action.bindings.Count == 0) return false;
            for (var i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].path == expectedPath) return true;
            return false;
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.arsolarsystem.explorer");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            EditorUserBuildSettings.androidBuildType = AndroidBuildType.Debug;
        }

        private static bool ValidateAndroidGraphicsApis()
        {
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (apis.Length == 1 && apis[0] == GraphicsDeviceType.OpenGLES3) return true;
            Debug.LogError("Android graphics APIs must be OpenGLES3 only for this ARCore configuration. Run Build or Repair Project.");
            return false;
        }

        private static void ConfigureAndroidXRLoader()
        {
            const string settingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
            const string loaderPath = "Assets/XR/Loaders/ARCoreLoader.asset";
            var targetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsPath);
            if (targetSettings == null)
            {
                targetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(targetSettings, settingsPath);
            }

            var androidSettings = targetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (androidSettings == null)
            {
                targetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
                androidSettings = targetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            }
            if (androidSettings.Manager == null)
                targetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var manager = androidSettings.Manager;
            var arCoreLoader = AssetDatabase.LoadAssetAtPath<ARCoreLoader>(loaderPath);
            if (arCoreLoader == null)
                throw new FileNotFoundException($"ARCore loader asset is missing at {loaderPath}. Confirm ARCore XR Plugin 5.0.6 is resolved.");

            if (!manager.activeLoaders.Any(loader => loader is ARCoreLoader) && !manager.TryAddLoader(arCoreLoader))
                throw new InvalidOperationException("XR Plug-in Management could not add the ARCore loader to Android settings.");

            androidSettings.InitManagerOnStart = true;
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, targetSettings, true);
            EditorUtility.SetDirty(targetSettings);
            EditorUtility.SetDirty(androidSettings);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
            Debug.Log("Configured ARCore XR loader for Android startup.");
        }

        private static bool ValidateAndroidXRLoader()
        {
            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var targetSettings) || targetSettings == null)
            {
                Debug.LogError("XR Plug-in Management has no general settings asset registered. Run Build or Repair Project.");
                return false;
            }

            var androidSettings = targetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            var manager = androidSettings != null ? androidSettings.Manager : null;
            if (manager == null || !manager.activeLoaders.Any(loader => loader is ARCoreLoader))
            {
                Debug.LogError("ARCore is not enabled as an Android XR loader. Run Build or Repair Project before building the APK.");
                return false;
            }
            if (!androidSettings.InitManagerOnStart)
            {
                Debug.LogError("XR Manager is not set to initialize on startup for Android.");
                return false;
            }
            return true;
        }

        private static void EnsureTextMeshProResources()
        {
            const string fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) != null) return;
            const string packagePath = "Library/PackageCache/com.unity.textmeshpro@3.0.6/Package Resources/TMP Essential Resources.unitypackage";
            if (!File.Exists(packagePath))
            {
                Debug.LogError("TextMeshPro is installed but its Essential Resources package is missing from Library/PackageCache.");
                return;
            }
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();
        }

        private static PlanetInfoPanel CreatePlanetInfoUI()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("AR Solar System UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Planet Info Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = Vector2.zero;
            panelRect.anchoredPosition = new Vector2(24f, 24f);
            panelRect.sizeDelta = new Vector2(620f, 470f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.94f);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var title = CreatePanelText("Planet Name", panel.transform, "Planet", font, 40f, FontStyles.Bold, new Vector2(24f, -18f), new Vector2(-92f, -72f));
            var details = CreatePanelText("Planet Details", panel.transform, string.Empty, font, 22f, FontStyles.Normal, new Vector2(24f, -88f), new Vector2(-24f, -278f));
            var description = CreatePanelText("Planet Description", panel.transform, string.Empty, font, 23f, FontStyles.Italic, new Vector2(24f, -294f), new Vector2(-24f, -446f));
            details.enableWordWrapping = false;
            description.enableWordWrapping = true;

            var closeObject = new GameObject("Close Information", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(panel.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            closeRect.sizeDelta = new Vector2(60f, 60f);
            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = new Color(0.12f, 0.27f, 0.42f, 1f);
            var closeButton = closeObject.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabel = CreatePanelText("Close Label", closeObject.transform, "×", font, 40f, FontStyles.Bold, Vector2.zero, Vector2.zero);
            var labelRect = closeLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            closeLabel.alignment = TextAlignmentOptions.Center;

            var infoPanel = panel.AddComponent<PlanetInfoPanel>();
            infoPanel.Configure(panel, title, details, description, closeButton);
            return infoPanel;
        }

        private sealed class MainControls
        {
            public Button place, reset, pause, resetSettings, applySize;
            public TMP_InputField sunInput;
            public Slider distanceSlider;
            public TMP_Text distanceLabel, pauseLabel, status, instructions;
            public Dropdown mode;
        }

        private static MainControls CreateMainControlsUI(Transform canvas)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var panelObject = new GameObject("Main Controls", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas, false);
            var panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-20f, -20f);
            panel.sizeDelta = new Vector2(470f, 820f);
            panelObject.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.94f);
            var output = new MainControls();
            output.instructions = CreatePanelText("Placement Instructions", panelObject.transform,
                "Scan a level surface, then place your model.", font, 24f, FontStyles.Bold, new Vector2(20, -16), new Vector2(-20, -74));
            output.status = CreatePanelText("AR Status", panelObject.transform, "Starting AR…", font, 20f, FontStyles.Normal,
                new Vector2(20, -80), new Vector2(-20, -138));
            output.place = CreateControlButton("Place Solar System", panelObject.transform, font, new Vector2(20, -150), new Vector2(-20, -212));
            output.reset = CreateControlButton("Reset Placement", panelObject.transform, font, new Vector2(20, -220), new Vector2(-20, -282));
            output.pause = CreateControlButton("Resume", panelObject.transform, font, new Vector2(20, -290), new Vector2(-20, -352));
            output.pauseLabel = output.pause.GetComponentInChildren<TMP_Text>();
            CreatePanelText("Sun Diameter Label", panelObject.transform, "Sun diameter (meters)", font, 19f, FontStyles.Normal,
                new Vector2(20, -362), new Vector2(-20, -402));
            output.sunInput = CreateSunInput(panelObject.transform, font, new Vector2(20, -406), new Vector2(-150, -462));
            output.applySize = CreateRightAlignedControlButton("Apply Size", panelObject.transform, font, new Vector2(-20, -406), new Vector2(120, 56));
            CreatePanelText("Mode Label", panelObject.transform, "Animation mode", font, 19f, FontStyles.Normal,
                new Vector2(20, -470), new Vector2(-20, -508));
            output.mode = CreateModeDropdown(panelObject.transform, font, new Vector2(20, -512), new Vector2(-20, -570));
            output.distanceLabel = CreatePanelText("Distance Scale Label", panelObject.transform, "Distance scale: 0.25 m / AU", font, 19f, FontStyles.Normal,
                new Vector2(20, -578), new Vector2(-20, -620));
            output.distanceSlider = CreateDistanceSlider(panelObject.transform, new Vector2(20, -632), new Vector2(-20, -670));
            output.resetSettings = CreateControlButton("Reset Settings", panelObject.transform, font, new Vector2(20, -678), new Vector2(-20, -740));
            CreatePanelText("Scale Note", panelObject.transform,
                "Body sizes follow diameter ratios. Orbit distance is separate; outer planets can be beyond view. This is not a complete scale model.",
                font, 16f, FontStyles.Italic, new Vector2(20, -748), new Vector2(-20, -812));
            return output;
        }

        private static Button CreateControlButton(string label, Transform parent, TMP_FontAsset font, Vector2 topLeft, Vector2 bottomRight)
        {
            var item = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y); rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
            var image = item.GetComponent<Image>(); image.color = new Color(.12f, .28f, .43f, 1);
            var button = item.GetComponent<Button>(); button.targetGraphic = image;
            var text = CreatePanelText("Label", item.transform, label, font, 21f, FontStyles.Bold, Vector2.zero, Vector2.zero);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(6, 2); text.rectTransform.offsetMax = new Vector2(-6, -2);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static Button CreateRightAlignedControlButton(string label, Transform parent, TMP_FontAsset font, Vector2 anchoredPosition, Vector2 size)
        {
            var item = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = item.GetComponent<Image>(); image.color = new Color(.12f, .28f, .43f, 1);
            var button = item.GetComponent<Button>(); button.targetGraphic = image;
            var text = CreatePanelText("Label", item.transform, label, font, 19f, FontStyles.Bold, Vector2.zero, Vector2.zero);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(4, 2); text.rectTransform.offsetMax = new Vector2(-4, -2);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TMP_InputField CreateSunInput(Transform parent, TMP_FontAsset font, Vector2 topLeft, Vector2 bottomRight)
        {
            var root = new GameObject("Sun Diameter Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y); rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
            var image = root.GetComponent<Image>(); image.color = new Color(.08f, .12f, .18f, 1);
            var text = CreatePanelText("Value", root.transform, "0.1", font, 21, FontStyles.Normal, new Vector2(10, -5), new Vector2(-10, -5));
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.rectTransform.offsetMin = new Vector2(10, 4); text.rectTransform.offsetMax = new Vector2(-10, -4); text.alignment = TextAlignmentOptions.MidlineLeft;
            var input = root.GetComponent<TMP_InputField>(); input.textComponent = text; input.targetGraphic = image; input.contentType = TMP_InputField.ContentType.DecimalNumber;
            return input;
        }

        private static Slider CreateDistanceSlider(Transform parent, Vector2 topLeft, Vector2 bottomRight)
        {
            var root = new GameObject("Distance Scale Slider", typeof(RectTransform), typeof(Slider)); root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y); rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); background.transform.SetParent(root.transform, false);
            var bgRect = background.GetComponent<RectTransform>(); bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.offsetMin = new Vector2(8, 14); bgRect.offsetMax = new Vector2(-8, -14);
            background.GetComponent<Image>().color = new Color(.12f, .15f, .2f, 1);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(background.transform, false);
            var fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(1, 1); fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(.15f, .62f, .9f, 1);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(root.transform, false);
            var handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(36, 36);
            var slider = root.GetComponent<Slider>(); slider.minValue = .05f; slider.maxValue = 2f; slider.wholeNumbers = false;
            slider.fillRect = fillRect; slider.handleRect = handleRect; slider.targetGraphic = handle.GetComponent<Image>(); slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Dropdown CreateModeDropdown(Transform parent, TMP_FontAsset font, Vector2 topLeft, Vector2 bottomRight)
        {
            var root = new GameObject("Animation Mode Selector", typeof(RectTransform), typeof(Image), typeof(Dropdown)); root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y); rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
            var image = root.GetComponent<Image>(); image.color = new Color(.08f, .12f, .18f, 1);
            var caption = CreateDropdownText("Caption", root.transform, "Educational", 20);
            caption.rectTransform.anchorMin = Vector2.zero; caption.rectTransform.anchorMax = Vector2.one; caption.rectTransform.offsetMin = new Vector2(12, 4); caption.rectTransform.offsetMax = new Vector2(-38, -4); caption.alignment = TextAnchor.MiddleLeft;
            var arrow = CreatePanelText("Arrow", root.transform, "▼", font, 18, FontStyles.Bold, new Vector2(-35, -4), new Vector2(-6, -4));
            arrow.rectTransform.anchorMin = Vector2.zero; arrow.rectTransform.anchorMax = Vector2.one; arrow.rectTransform.offsetMin = new Vector2(-38, 4); arrow.rectTransform.offsetMax = new Vector2(-6, -4); arrow.alignment = TextAlignmentOptions.Center;
            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect)); template.transform.SetParent(root.transform, false);
            var tr = template.GetComponent<RectTransform>(); tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 0); tr.pivot = new Vector2(.5f, 1); tr.anchoredPosition = new Vector2(0, -2); tr.sizeDelta = new Vector2(0, 100);
            template.GetComponent<Image>().color = new Color(.06f, .09f, .14f, 1);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D)); viewport.transform.SetParent(template.transform, false);
            var vr = viewport.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = Vector2.zero; vr.offsetMax = Vector2.zero;
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)); content.transform.SetParent(viewport.transform, false);
            var cr = content.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f, 1); cr.sizeDelta = new Vector2(0, 100);
            var item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle)); item.transform.SetParent(content.transform, false);
            var ir = item.GetComponent<RectTransform>(); ir.sizeDelta = new Vector2(0, 50);
            var itemImage = item.GetComponent<Image>(); itemImage.color = new Color(.08f, .12f, .18f, 1);
            var itemText = CreateDropdownText("Item Label", item.transform, "Option", 18);
            itemText.rectTransform.anchorMin = Vector2.zero; itemText.rectTransform.anchorMax = Vector2.one; itemText.rectTransform.offsetMin = new Vector2(10, 4); itemText.rectTransform.offsetMax = new Vector2(-10, -4); itemText.alignment = TextAnchor.MiddleLeft;
            itemText.color = new Color(.91f, .95f, 1f, 1f);
            var toggle = item.GetComponent<Toggle>(); toggle.targetGraphic = itemImage; toggle.graphic = itemImage;
            var scroll = template.GetComponent<ScrollRect>(); scroll.viewport = vr; scroll.content = cr; scroll.horizontal = false;
            template.SetActive(false);
            var dropdown = root.GetComponent<Dropdown>(); dropdown.targetGraphic = image; dropdown.template = tr; dropdown.captionText = caption; dropdown.itemText = itemText;
            dropdown.options.Clear(); dropdown.options.Add(new Dropdown.OptionData("Educational")); dropdown.options.Add(new Dropdown.OptionData("Relative periods"));
            dropdown.value = 0; dropdown.RefreshShownValue();
            return dropdown;
        }

        private static Text CreateDropdownText(string name, Transform parent, string value, int size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = new Color(.91f, .95f, 1f, 1f);
            text.raycastTarget = true;
            return text;
        }

        private static TextMeshProUGUI CreatePanelText(string name, Transform parent, string value, TMP_FontAsset font,
            float size, FontStyles style, Vector2 topLeftOffset, Vector2 bottomRightOffset)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(topLeftOffset.x, bottomRightOffset.y);
            rect.offsetMax = new Vector2(bottomRightOffset.x, topLeftOffset.y);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontSizeMin = Mathf.Max(14f, size - 5f);
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.color = new Color(0.91f, 0.95f, 1f, 1f);
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = true;
            return text;
        }
    }
}
