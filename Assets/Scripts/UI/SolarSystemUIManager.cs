using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ARSolarSystem;

namespace ARSolarSystem.UI
{
    /// <summary>Wires the generated mobile controls to AR placement, model scale, and animation.</summary>
    public sealed class SolarSystemUIManager : MonoBehaviour
    {
        [SerializeField] private SolarSystemARController arController;
        [SerializeField] private Button placeButton, resetPlacementButton, pauseButton, resetSettingsButton, applySunSizeButton;
        [SerializeField] private TMP_InputField sunDiameterInput;
        [SerializeField] private Slider distanceScaleSlider;
        [SerializeField] private TMP_Text distanceScaleLabel, pauseLabel, statusLabel, instructionsLabel;
        [SerializeField] private Dropdown animationModeDropdown;
        [SerializeField] private PlanetInfoPanel planetInfoPanel;
        private SolarSystemBuilder builder;
        private SolarSystemUserSettings settings;

        public bool IsConfigured => arController != null && placeButton != null && resetPlacementButton != null && pauseButton != null &&
            resetSettingsButton != null && applySunSizeButton != null && sunDiameterInput != null && distanceScaleSlider != null &&
            animationModeDropdown != null && distanceScaleLabel != null && pauseLabel != null && statusLabel != null && instructionsLabel != null && planetInfoPanel != null;

        public void Configure(SolarSystemARController ar, Button place, Button resetPlacement, Button pause, Button resetSettings,
            Button applySunSize, TMP_InputField sunInput, Slider distanceSlider, TMP_Text distanceLabel, TMP_Text pauseText,
            TMP_Text status, TMP_Text instructions, Dropdown modeDropdown, PlanetInfoPanel infoPanel)
        {
            arController = ar; placeButton = place; resetPlacementButton = resetPlacement; pauseButton = pause;
            resetSettingsButton = resetSettings; applySunSizeButton = applySunSize; sunDiameterInput = sunInput;
            distanceScaleSlider = distanceSlider; distanceScaleLabel = distanceLabel; pauseLabel = pauseText;
            statusLabel = status; instructionsLabel = instructions; animationModeDropdown = modeDropdown;
            planetInfoPanel = infoPanel;
            Bind();
        }

        private void OnEnable() { Bind(); }
        private void Start()
        {
            settings = SolarSystemUserSettings.Load();
            SyncSettingsToControls();
        }

        private void Bind()
        {
            BindButton(placeButton, BeginPlacement);
            BindButton(resetPlacementButton, ResetPlacement);
            BindButton(pauseButton, TogglePause);
            BindButton(resetSettingsButton, ResetSettings);
            BindButton(applySunSizeButton, ApplyScales);
            if (distanceScaleSlider != null)
            {
                distanceScaleSlider.onValueChanged.RemoveListener(OnDistanceChanged);
                distanceScaleSlider.onValueChanged.AddListener(OnDistanceChanged);
            }
            if (animationModeDropdown != null)
            {
                animationModeDropdown.onValueChanged.RemoveListener(OnModeChanged);
                animationModeDropdown.onValueChanged.AddListener(OnModeChanged);
            }
            if (arController != null)
            {
                arController.SystemPlaced -= RegisterSystem;
                arController.SystemPlaced += RegisterSystem;
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action); button.onClick.AddListener(action);
        }

        public void SetStatus(string text) { if (statusLabel != null) statusLabel.text = text; }
        private void BeginPlacement() { planetInfoPanel?.Hide(); if (arController != null) arController.BeginPlacement(); }
        private void ResetPlacement() { planetInfoPanel?.Hide(); if (arController != null) arController.ResetPlacement(); builder = null; }

        private void RegisterSystem(SolarSystemBuilder value)
        {
            builder = value;
            if (settings == null) settings = SolarSystemUserSettings.Load();
            ApplyScales();
            if (builder.Motion != null) ApplyMotionSettings();
            else { builder.Built -= OnBuilderBuilt; builder.Built += OnBuilderBuilt; }
        }

        private void OnBuilderBuilt(SolarSystemBuilder value)
        {
            if (value != builder) return;
            value.Built -= OnBuilderBuilt;
            ApplyMotionSettings();
        }

        private void ApplyMotionSettings()
        {
            if (builder != null && builder.Motion != null)
            {
                builder.Motion.SetMode(settings.Mode);
                builder.Motion.SetPaused(false);
            }
            RefreshPauseLabel();
        }

        private void ApplyScales()
        {
            if (settings == null) settings = SolarSystemUserSettings.Load();
            var currentSun = ParseSunDiameter(settings.SunDiameterMeters);
            var distance = distanceScaleSlider != null ? distanceScaleSlider.value : settings.MetersPerAu;
            settings.Set(currentSun, distance, ReadMode());
            if (builder != null) builder.SetScales(settings.SunDiameterMeters, settings.MetersPerAu);
            if (sunDiameterInput != null) sunDiameterInput.text = settings.SunDiameterMeters.ToString("0.##", CultureInfo.InvariantCulture);
            UpdateDistanceLabel(settings.MetersPerAu);
        }

        private float ParseSunDiameter(float fallback)
        {
            if (sunDiameterInput != null && float.TryParse(sunDiameterInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !float.IsNaN(value) && !float.IsInfinity(value))
                return Mathf.Clamp(value, 0.05f, 100f);
            SetStatus("Enter a Sun diameter in meters (0.05 to 100).");
            return fallback;
        }

        private void OnDistanceChanged(float value)
        {
            if (settings == null) settings = SolarSystemUserSettings.Load();
            settings.Set(settings.SunDiameterMeters, value, ReadMode());
            if (builder != null) builder.SetScales(settings.SunDiameterMeters, settings.MetersPerAu);
            UpdateDistanceLabel(value);
        }

        private void UpdateDistanceLabel(float value)
        {
            if (distanceScaleLabel != null) distanceScaleLabel.text = "Distance scale: " + value.ToString("0.###", CultureInfo.InvariantCulture) + " m / AU";
        }

        private SolarSystemAnimationMode ReadMode() => animationModeDropdown != null && animationModeDropdown.value == 1
            ? SolarSystemAnimationMode.RelativePeriods : SolarSystemAnimationMode.Educational;

        private void OnModeChanged(int index)
        {
            if (settings == null) settings = SolarSystemUserSettings.Load();
            settings.Set(settings.SunDiameterMeters, distanceScaleSlider != null ? distanceScaleSlider.value : settings.MetersPerAu,
                index == 1 ? SolarSystemAnimationMode.RelativePeriods : SolarSystemAnimationMode.Educational);
            if (builder != null && builder.Motion != null) builder.Motion.SetMode(settings.Mode);
        }

        private void TogglePause()
        {
            if (builder == null || builder.Motion == null) { SetStatus("Place the Solar System before changing animation."); return; }
            builder.Motion.TogglePaused(); RefreshPauseLabel();
        }

        private void RefreshPauseLabel()
        {
            if (pauseLabel != null) pauseLabel.text = builder != null && builder.Motion != null && !builder.Motion.IsPaused ? "Pause" : "Resume";
        }

        private void ResetSettings()
        {
            if (settings == null) settings = SolarSystemUserSettings.Load();
            settings.Reset(); SyncSettingsToControls(); ApplyScales();
            if (builder != null && builder.Motion != null) builder.Motion.SetMode(settings.Mode);
            SetStatus("Scale and animation settings reset to defaults.");
        }

        private void SyncSettingsToControls()
        {
            if (settings == null) settings = SolarSystemUserSettings.Load();
            if (sunDiameterInput != null) sunDiameterInput.text = settings.SunDiameterMeters.ToString("0.##", CultureInfo.InvariantCulture);
            if (distanceScaleSlider != null) distanceScaleSlider.SetValueWithoutNotify(Mathf.Clamp(distanceScaleSlider.value, distanceScaleSlider.minValue, distanceScaleSlider.maxValue));
            if (distanceScaleSlider != null) distanceScaleSlider.SetValueWithoutNotify(settings.MetersPerAu);
            if (animationModeDropdown != null) animationModeDropdown.SetValueWithoutNotify((int)settings.Mode);
            UpdateDistanceLabel(settings.MetersPerAu); RefreshPauseLabel();
        }
    }
}
