using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ARSolarSystem.Data;

namespace ARSolarSystem.UI
{
    public sealed class PlanetInfoPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button closeButton;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public bool IsConfigured => panelRoot != null && titleText != null && detailsText != null && descriptionText != null && closeButton != null &&
                                    titleText.font != null && detailsText.font != null && descriptionText.font != null;

        private void OnEnable()
        {
            if (closeButton == null) return;
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        public void Configure(GameObject panel, TMP_Text title, TMP_Text details, TMP_Text description, Button close)
        {
            panelRoot = panel;
            titleText = title;
            detailsText = details;
            descriptionText = description;
            closeButton = close;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
            Hide();
        }

        public void Show(PlanetaryBodyData data)
        {
            if (data == null || panelRoot == null || titleText == null || detailsText == null || descriptionText == null)
            {
                Debug.LogWarning("Planet information panel is not fully configured.", this);
                return;
            }

            titleText.text = data.DisplayName;
            var au = data.MeanDistanceFromSunKm / 149_600_000.0;
            var rotation = Mathf.Abs((float)data.SiderealRotationHours).ToString("0.##", CultureInfo.InvariantCulture) + " hours";
            if (data.SiderealRotationHours < 0) rotation += " (retrograde)";
            var moonText = data.ConfirmedMoonCount.HasValue
                ? data.ConfirmedMoonCount.Value.ToString(CultureInfo.InvariantCulture) + " confirmed (NASA source: 18 Mar 2025)"
                : "Not listed";
            detailsText.text =
                $"Diameter: {data.DiameterKm.ToString("N0", CultureInfo.InvariantCulture)} km (equatorial)\n" +
                $"Mass: {data.MassKg.ToString("0.###E+0", CultureInfo.InvariantCulture)} kg\n" +
                $"Mean distance: {data.MeanDistanceFromSunKm.ToString("N0", CultureInfo.InvariantCulture)} km ({au.ToString("0.000", CultureInfo.InvariantCulture)} AU)\n" +
                $"Orbital period: {data.OrbitalPeriodDays.ToString("N1", CultureInfo.InvariantCulture)} days\n" +
                $"Sidereal rotation: {rotation}\n" +
                $"Known moons: {moonText}";
            descriptionText.text = data.Description;
            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
