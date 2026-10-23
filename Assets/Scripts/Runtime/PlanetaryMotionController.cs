using System.Collections.Generic;
using UnityEngine;
using ARSolarSystem.Data;

namespace ARSolarSystem
{
    public enum SolarSystemAnimationMode
    {
        Educational,
        RelativePeriods
    }

    /// <summary>Updates registered orbital pivots and axial bodies using frame time and one pause state.</summary>
    public sealed class PlanetaryMotionController : MonoBehaviour
    {
        private struct MotionEntry
        {
            public PlanetaryBodyData data;
            public Transform orbitalPivot;
            public Transform axialBody;
        }

        [SerializeField] private SolarSystemAnimationMode mode = SolarSystemAnimationMode.Educational;
        [SerializeField] private bool paused;
        [SerializeField, Min(0.1f)] private float relativeEarthOrbitDegreesPerSecond = 2f;
        private readonly List<MotionEntry> entries = new List<MotionEntry>(8);

        public bool IsPaused => paused;
        public SolarSystemAnimationMode Mode => mode;
        public int RegisteredBodyCount => entries.Count;

        public void Register(PlanetaryBodyData data, Transform orbitalPivot, Transform axialBody)
        {
            if (data == null || orbitalPivot == null || axialBody == null) return;
            entries.Add(new MotionEntry { data = data, orbitalPivot = orbitalPivot, axialBody = axialBody });
        }

        public void SetPaused(bool value) => paused = value;
        public void Pause() => SetPaused(true);
        public void Resume() => SetPaused(false);
        public void TogglePaused() => SetPaused(!paused);
        public void SetMode(SolarSystemAnimationMode value) => mode = value;

        private void Update()
        {
            AdvanceMotion(Time.deltaTime);
        }

        public void AdvanceMotion(float delta)
        {
            if (paused || entries.Count == 0) return;
            if (delta <= 0f) return;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.orbitalPivot == null || entry.axialBody == null) continue;
                entry.orbitalPivot.Rotate(Vector3.up, OrbitDegreesPerSecond(entry.data) * delta, Space.Self);
                entry.axialBody.Rotate(Vector3.up, RotationDegreesPerSecond(entry.data) * delta, Space.Self);
            }
        }

        private float OrbitDegreesPerSecond(PlanetaryBodyData data)
        {
            if (mode == SolarSystemAnimationMode.RelativePeriods)
                return relativeEarthOrbitDegreesPerSecond * (365.2f / (float)data.OrbitalPeriodDays);

            // Educational mode keeps all eight orbits perceptible, so its rates are intentionally not period-accurate.
            return Mathf.Clamp(6f * Mathf.Sqrt(365.2f / (float)data.OrbitalPeriodDays), 2.5f, 18f);
        }

        private float RotationDegreesPerSecond(PlanetaryBodyData data)
        {
            var sign = Mathf.Sign((float)data.SiderealRotationHours);
            if (mode == SolarSystemAnimationMode.RelativePeriods)
                return sign * 20f * (23.9f / Mathf.Abs((float)data.SiderealRotationHours));

            // Educational speeds retain rotation direction but are accelerated and clamped for visibility.
            return sign * Mathf.Clamp(24f * (23.9f / Mathf.Abs((float)data.SiderealRotationHours)), 4f, 60f);
        }
    }
}
