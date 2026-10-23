using UnityEngine;
using ARSolarSystem.Data;
using System.Collections.Generic;
using System;

namespace ARSolarSystem
{
    /// <summary>Builds a complete, procedural-fallback Solar System below one root transform.</summary>
    public sealed class SolarSystemBuilder : MonoBehaviour
    {
        [SerializeField] private SolarSystemVisualAssets visuals;
        [SerializeField, Min(0.05f)] private float defaultSunDiameterMeters = 0.1f;
        [SerializeField, Min(0.001f)] private float defaultMetersPerAstronomicalUnit = 0.25f;
        [SerializeField, Min(24)] private int orbitSegments = 96;

        private Transform contents;
        private Transform sunTransform;
        private Light sunLight;
        private readonly List<ScaledPlanet> scaledPlanets = new List<ScaledPlanet>(8);
        private struct ScaledPlanet { public PlanetaryBodyData data; public Transform pivot; public Transform body; public LineRenderer orbit; }
        private static Mesh saturnRingMesh;

        // Thin, sub-millimetre lines disappear on phone displays, especially for
        // the outer orbits. Keep a readable minimum and let distant paths grow
        // slightly so they remain rasterized at normal viewing distances.
        private float OrbitLineWidth(float radius)
        {
            return Mathf.Max(0.004f, defaultSunDiameterMeters * 0.02f, radius * 0.002f);
        }

        public SolarSystemVisualAssets Visuals => visuals;
        public float DefaultSunDiameterMeters => defaultSunDiameterMeters;
        public float DefaultMetersPerAstronomicalUnit => defaultMetersPerAstronomicalUnit;
        public PlanetaryMotionController Motion { get; private set; }
        public event Action<SolarSystemBuilder> Built;

        private void Start()
        {
            if (visuals == null)
            {
                Debug.LogError("SolarSystemBuilder has no SolarSystemVisualAssets assigned.", this);
                return;
            }
            Build();
        }

        public void Configure(SolarSystemVisualAssets visualAssets, float sunDiameterMeters, float metersPerAu)
        {
            visuals = visualAssets;
            defaultSunDiameterMeters = Mathf.Max(0.05f, sunDiameterMeters);
            defaultMetersPerAstronomicalUnit = Mathf.Max(0.001f, metersPerAu);
        }

        public void Build()
        {
            if (visuals == null) throw new MissingReferenceException("Assign SolarSystemVisualAssets before building the model.");
            if (contents != null)
            {
                Destroy(contents.gameObject);
                contents = null;
            }

            var sunDiameter = defaultSunDiameterMeters;
            scaledPlanets.Clear();
            contents = new GameObject("Solar System Contents").transform;
            contents.SetParent(transform, false);
            contents.localPosition = Vector3.up * (sunDiameter * 0.5f + 0.01f);
            Motion = contents.gameObject.AddComponent<PlanetaryMotionController>();

            var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = "Sun";
            sun.transform.SetParent(contents, false);
            sun.transform.localScale = Vector3.one * sunDiameter;
            sunTransform = sun.transform;
            AssignMaterial(sun, visuals.sunMaterial, "Sun");
            var sunLight = sun.AddComponent<Light>();
            this.sunLight = sunLight;
            sunLight.type = LightType.Point;
            sunLight.color = new Color(1f, 0.82f, 0.58f);
            sunLight.intensity = 1.2f;
            sunLight.range = Mathf.Max(10f, defaultMetersPerAstronomicalUnit * 40f);
            sunLight.shadows = LightShadows.None;

            for (var i = 0; i < PlanetaryData.Planets.Count; i++)
            {
                var data = PlanetaryData.Planets[i];
                var orbitRadius = (float)(data.MeanDistanceFromSunKm / 149_600_000.0) * defaultMetersPerAstronomicalUnit;
                var orbit = CreateOrbit(data, orbitRadius);

                var pivot = new GameObject(data.DisplayName + " Orbit Pivot").transform;
                pivot.SetParent(contents, false);
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = data.DisplayName;
                body.transform.SetParent(pivot, false);
                body.transform.localPosition = Vector3.right * orbitRadius;
                var diameter = sunDiameter * (float)(data.DiameterKm / PlanetaryData.SunDiameterKm);
                body.transform.localScale = Vector3.one * diameter;
                AssignMaterial(body, visuals.GetPlanetMaterial(data.Id), data.DisplayName);
                // Preserve tiny displayed diameters while giving finger taps a practical target.
                var sphereCollider = body.GetComponent<SphereCollider>();
                sphereCollider.radius = Mathf.Max(0.5f, 0.05f / Mathf.Max(diameter, 0.0001f));
                body.AddComponent<PlanetSelectable>().Configure(data);
                Motion.Register(data, pivot, body.transform);
                scaledPlanets.Add(new ScaledPlanet { data = data, pivot = pivot, body = body.transform, orbit = orbit });

                if (data.Id == PlanetId.Saturn && visuals.saturnRingMaterial != null)
                    CreateSaturnRings(body.transform, visuals.saturnRingMaterial);
            }
            Built?.Invoke(this);
        }

        public void SetScales(float sunDiameterMeters, float metersPerAu)
        {
            if (float.IsNaN(sunDiameterMeters) || float.IsInfinity(sunDiameterMeters)) sunDiameterMeters = defaultSunDiameterMeters;
            if (float.IsNaN(metersPerAu) || float.IsInfinity(metersPerAu)) metersPerAu = defaultMetersPerAstronomicalUnit;
            defaultSunDiameterMeters = Mathf.Clamp(sunDiameterMeters, 0.05f, 100f);
            defaultMetersPerAstronomicalUnit = Mathf.Clamp(metersPerAu, 0.001f, 20f);
            if (sunTransform == null) return;
            if (contents != null) contents.localPosition = Vector3.up * (defaultSunDiameterMeters * 0.5f + 0.01f);
            sunTransform.localScale = Vector3.one * defaultSunDiameterMeters;
            if (sunLight != null) sunLight.range = Mathf.Max(10f, defaultMetersPerAstronomicalUnit * 40f);
            for (var i = 0; i < scaledPlanets.Count; i++)
            {
                var item = scaledPlanets[i];
                var radius = (float)(item.data.MeanDistanceFromSunKm / 149_600_000.0) * defaultMetersPerAstronomicalUnit;
                var diameter = defaultSunDiameterMeters * (float)(item.data.DiameterKm / PlanetaryData.SunDiameterKm);
                item.body.localScale = Vector3.one * diameter;
                item.body.localPosition = Vector3.right * radius;
                item.orbit.startWidth = item.orbit.endWidth = OrbitLineWidth(radius);
                for (var p = 0; p < orbitSegments; p++)
                {
                    var angle = p * Mathf.PI * 2f / orbitSegments;
                    item.orbit.SetPosition(p, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }
            }
        }

        private LineRenderer CreateOrbit(PlanetaryBodyData data, float radius)
        {
            var orbit = new GameObject(data.DisplayName + " Orbit");
            orbit.transform.SetParent(contents, false);
            var line = orbit.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = orbitSegments;
            line.startWidth = line.endWidth = OrbitLineWidth(radius);
            line.sharedMaterial = visuals.orbitMaterial;
            line.startColor = line.endColor = new Color(0.72f, 0.82f, 1f, 0.48f);
            line.numCapVertices = 0;
            for (var i = 0; i < orbitSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / orbitSegments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return line;
        }

        private static void CreateSaturnRings(Transform planet, Material material)
        {
            var ring = new GameObject("Saturn Rings");
            ring.transform.SetParent(planet, false);
            ring.transform.localRotation = Quaternion.Euler(26.7f, 0f, 0f);
            ring.transform.localScale = Vector3.one; // The planet parent already carries the body diameter scale.
            var filter = ring.AddComponent<MeshFilter>();
            var renderer = ring.AddComponent<MeshRenderer>();
            if (saturnRingMesh == null) saturnRingMesh = CreateRingMesh(0.62f, 1.2f, 128);
            filter.sharedMesh = saturnRingMesh;
            renderer.sharedMaterial = material;
        }

        private static Mesh CreateRingMesh(float innerRadius, float outerRadius, int segments)
        {
            var mesh = new Mesh { name = "Saturn Ring Mesh" };
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var indices = new int[segments * 6];
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * innerRadius;
                vertices[i * 2 + 1] = direction * outerRadius;
                uvs[i * 2] = new Vector2(0f, 0f);
                uvs[i * 2 + 1] = new Vector2(1f, 0f);
                if (i == segments) continue;
                var offset = i * 6;
                var inner = i * 2;
                indices[offset] = inner; indices[offset + 1] = inner + 1; indices[offset + 2] = inner + 2;
                indices[offset + 3] = inner + 1; indices[offset + 4] = inner + 3; indices[offset + 5] = inner + 2;
            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AssignMaterial(GameObject body, Material material, string label)
        {
            var renderer = body.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            else Debug.LogWarning($"No generated material assigned for {label}; Unity's default material will be used.", body);
        }
    }
}
