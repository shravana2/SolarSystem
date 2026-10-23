using UnityEngine;

namespace ARSolarSystem
{
    /// <summary>Small persistent user preference set; physical model scale remains configurable independently.</summary>
    public sealed class SolarSystemUserSettings
    {
        private const string SunKey = "arss.sunDiameterMeters";
        private const string DistanceKey = "arss.metersPerAu";
        private const string ModeKey = "arss.animationMode";
        public float SunDiameterMeters { get; private set; } = 0.1f;
        public float MetersPerAu { get; private set; } = 0.25f;
        public SolarSystemAnimationMode Mode { get; private set; } = SolarSystemAnimationMode.Educational;

        public static SolarSystemUserSettings Load() => new SolarSystemUserSettings
        {
            SunDiameterMeters = PlayerPrefs.GetFloat(SunKey, 0.1f),
            MetersPerAu = PlayerPrefs.GetFloat(DistanceKey, 0.25f),
            Mode = (SolarSystemAnimationMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, 1)
        };

        public void Set(float sunDiameter, float metersPerAu, SolarSystemAnimationMode mode)
        {
            if (float.IsNaN(sunDiameter) || float.IsInfinity(sunDiameter)) sunDiameter = 0.1f;
            if (float.IsNaN(metersPerAu) || float.IsInfinity(metersPerAu)) metersPerAu = 0.25f;
            SunDiameterMeters = Mathf.Clamp(sunDiameter, 0.05f, 100f);
            MetersPerAu = Mathf.Clamp(metersPerAu, 0.001f, 20f);
            Mode = mode;
            PlayerPrefs.SetFloat(SunKey, SunDiameterMeters);
            PlayerPrefs.SetFloat(DistanceKey, MetersPerAu);
            PlayerPrefs.SetInt(ModeKey, (int)Mode);
            PlayerPrefs.Save();
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(SunKey); PlayerPrefs.DeleteKey(DistanceKey); PlayerPrefs.DeleteKey(ModeKey);
            PlayerPrefs.Save();
            SunDiameterMeters = 0.1f; MetersPerAu = 0.25f; Mode = SolarSystemAnimationMode.Educational;
        }
    }
}
