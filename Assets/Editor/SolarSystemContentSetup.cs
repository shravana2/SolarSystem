using System.IO;
using UnityEditor;
using UnityEngine;
using ARSolarSystem.Data;

namespace ARSolarSystem.Editor
{
    public static class SolarSystemContentSetup
    {
        private const string VisualAssetsPath = "Assets/Materials/SolarSystemVisualAssets.asset";
        private const string SolarSystemPrefabPath = "Assets/Prefabs/SolarSystem.prefab";
        private const string TextureFolder = "Assets/Textures/Procedural";

        public static void BuildSolarSystemPrefab()
        {
            EnsureFolder("Assets/Textures");
            EnsureFolder(TextureFolder);
            var visualAssets = BuildVisualAssets();
            var root = new GameObject("Solar System");
            var builder = root.AddComponent<SolarSystemBuilder>();
            builder.Configure(visualAssets, 0.1f, 0.25f);
            PrefabUtility.SaveAsPrefabAsset(root, SolarSystemPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {SolarSystemPrefabPath} with procedural maps, eight planets, eight orbits, and Saturn's ring mesh generator.");
        }

        public static bool ValidateSolarSystemContent()
        {
            var errors = 0;
            var visualAssets = AssetDatabase.LoadAssetAtPath<SolarSystemVisualAssets>(VisualAssetsPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SolarSystemPrefabPath);
            if (visualAssets == null) { Debug.LogError($"Missing generated visual catalog: {VisualAssetsPath}"); errors++; }
            if (prefab == null) { Debug.LogError($"Missing Solar System prefab: {SolarSystemPrefabPath}"); errors++; }
            else
            {
                var builder = prefab.GetComponent<SolarSystemBuilder>();
                if (builder == null) { Debug.LogError("Solar System prefab is missing SolarSystemBuilder."); errors++; }
                else if (builder.Visuals == null) { Debug.LogError("SolarSystemBuilder is missing its SolarSystemVisualAssets reference."); errors++; }
            }
            if (PlanetaryData.Planets.Count != 8) { Debug.LogError($"Expected eight planet records, found {PlanetaryData.Planets.Count}."); errors++; }
            for (var i = 0; i < PlanetaryData.Planets.Count; i++)
            {
                var body = PlanetaryData.Planets[i];
                if (body.DiameterKm <= 0 || body.MassKg <= 0 || body.MeanDistanceFromSunKm <= 0 || body.OrbitalPeriodDays <= 0 || body.SiderealRotationHours == 0)
                {
                    Debug.LogError($"Invalid scientific data for {body.DisplayName}."); errors++;
                }
                if (visualAssets != null && visualAssets.GetPlanetMaterial(body.Id) == null)
                {
                    Debug.LogError($"No material assigned for {body.DisplayName}."); errors++;
                }
            }
            if (visualAssets != null && (visualAssets.sunMaterial == null || visualAssets.orbitMaterial == null || visualAssets.saturnRingMaterial == null))
            {
                Debug.LogError("Generated visual catalog is missing the Sun, orbit, or Saturn ring material."); errors++;
            }
            if (errors == 0 && !ValidateBuiltHierarchy(visualAssets)) errors++;
            if (errors == 0) Debug.Log("Phase 2-4 validation passed: eight planet records and selectable bodies, visual materials, prefab generation, pause/resume motion, and independent size/distance scale checks succeeded.");
            return errors == 0;
        }

        private static bool ValidateBuiltHierarchy(SolarSystemVisualAssets visualAssets)
        {
            GameObject temporaryRoot = null;
            var hierarchyErrors = 0;
            try
            {
                temporaryRoot = new GameObject("Solar System Validation Temporary Root");
                var builder = temporaryRoot.AddComponent<SolarSystemBuilder>();
                builder.Configure(visualAssets, 0.1f, 0.25f);
                builder.Build();
                var contents = temporaryRoot.transform.Find("Solar System Contents");
                if (contents == null) { Debug.LogError("Builder did not create a Solar System Contents root."); return false; }
                if (contents.Find("Sun") == null) { Debug.LogError("Builder did not create the Sun."); hierarchyErrors++; }
                if (contents.childCount != 17) { Debug.LogError($"Builder created {contents.childCount} top-level objects; expected Sun, eight planets, and eight orbit paths (17 total)."); hierarchyErrors++; }
                var motion = contents.GetComponent<PlanetaryMotionController>();
                if (motion == null) { Debug.LogError("Builder did not create a PlanetaryMotionController."); hierarchyErrors++; }
                else if (motion.RegisteredBodyCount != 8) { Debug.LogError($"Motion controller registered {motion.RegisteredBodyCount} planets instead of eight."); hierarchyErrors++; }
                for (var i = 0; i < PlanetaryData.Planets.Count; i++)
                {
                    var name = PlanetaryData.Planets[i].DisplayName;
                    var pivot = contents.Find(name + " Orbit Pivot");
                    if (pivot == null || pivot.Find(name) == null || pivot.Find(name).GetComponent<PlanetSelectable>() == null)
                    { Debug.LogError($"Builder did not create a selectable {name} with an orbital pivot."); hierarchyErrors++; }
                }
                var saturnPivot = contents.Find("Saturn Orbit Pivot");
                var saturn = saturnPivot != null ? saturnPivot.Find("Saturn") : null;
                var rings = saturn != null ? saturn.Find("Saturn Rings") : null;
                if (rings == null || rings.GetComponent<MeshFilter>()?.sharedMesh == null || rings.GetComponent<MeshRenderer>() == null)
                { Debug.LogError("Builder did not create a rendered Saturn ring mesh."); hierarchyErrors++; }
                else if (rings.GetComponent<Collider>() != null)
                { Debug.LogError("Saturn rings should not have a collider that blocks taps."); hierarchyErrors++; }
                if (motion != null)
                {
                    var earthPivot = contents.Find("Earth Orbit Pivot");
                    var earthBody = earthPivot != null ? earthPivot.Find("Earth") : null;
                    motion.Pause();
                    if (earthPivot != null && earthBody != null)
                    {
                        var orbitBeforePause = earthPivot.localRotation;
                        var spinBeforePause = earthBody.localRotation;
                        motion.AdvanceMotion(1f);
                        if (Quaternion.Angle(orbitBeforePause, earthPivot.localRotation) > 0.001f || Quaternion.Angle(spinBeforePause, earthBody.localRotation) > 0.001f)
                        { Debug.LogError("Pausing did not stop both orbit and axial rotation."); hierarchyErrors++; }

                        motion.Resume();
                        motion.AdvanceMotion(1f);
                        if (Quaternion.Angle(orbitBeforePause, earthPivot.localRotation) < 0.001f || Quaternion.Angle(spinBeforePause, earthBody.localRotation) < 0.001f)
                        { Debug.LogError("Resuming did not restart both orbit and axial rotation."); hierarchyErrors++; }

                        motion.Pause();
                        var orbitAfterPause = earthPivot.localRotation;
                        var spinAfterPause = earthBody.localRotation;
                        motion.AdvanceMotion(1f);
                        if (Quaternion.Angle(orbitAfterPause, earthPivot.localRotation) > 0.001f || Quaternion.Angle(spinAfterPause, earthBody.localRotation) > 0.001f)
                        { Debug.LogError("A second pause did not stop both forms of motion."); hierarchyErrors++; }
                    }
                    else { Debug.LogError("Motion pause/resume validation could not find Earth transforms."); hierarchyErrors++; }
                }
                var earthBodyForScale = contents.Find("Earth Orbit Pivot/Earth");
                var initialBodyDiameter = earthBodyForScale != null ? earthBodyForScale.localScale.x : 0f;
                var sunTransformForScale = contents.Find("Sun");
                var initialSunDiameter = sunTransformForScale != null ? sunTransformForScale.localScale.x : 0f;
                if (sunTransformForScale == null || contents.localPosition.y < initialSunDiameter * 0.5f)
                { Debug.LogError("Sun should sit fully above the AR anchor plane."); hierarchyErrors++; }
                var initialOrbitRadius = contents.Find("Earth Orbit")?.GetComponent<LineRenderer>()?.GetPosition(0).magnitude ?? 0f;
                builder.SetScales(0.2f, 0.25f);
                if (sunTransformForScale == null || Mathf.Abs(sunTransformForScale.localScale.x - initialSunDiameter * 2f) > 0.00001f)
                { Debug.LogError("Changing Sun diameter did not resize the Sun."); hierarchyErrors++; }
                if (earthBodyForScale == null || Mathf.Abs(earthBodyForScale.localScale.x - initialBodyDiameter * 2f) > 0.00001f)
                { Debug.LogError("Changing Sun diameter did not proportionally scale Earth."); hierarchyErrors++; }
                var unchangedOrbitRadius = contents.Find("Earth Orbit")?.GetComponent<LineRenderer>()?.GetPosition(0).magnitude ?? 0f;
                if (Mathf.Abs(unchangedOrbitRadius - initialOrbitRadius) > 0.00001f)
                { Debug.LogError("Changing Sun diameter also changed orbital distance; scale controls must be independent."); hierarchyErrors++; }
                builder.SetScales(0.1f, 0.5f);
                var changedOrbitRadius = contents.Find("Earth Orbit")?.GetComponent<LineRenderer>()?.GetPosition(0).magnitude ?? 0f;
                if (Mathf.Abs(changedOrbitRadius - initialOrbitRadius * 2f) > 0.00001f)
                { Debug.LogError("Changing distance scale did not independently update Earth orbit spacing."); hierarchyErrors++; }
                if (earthBodyForScale != null && Mathf.Abs(earthBodyForScale.localScale.x - initialBodyDiameter) > 0.00001f)
                { Debug.LogError("Changing distance scale also changed planetary diameter; scale controls must be independent."); hierarchyErrors++; }
            }
            catch (System.Exception exception)
            {
                Debug.LogError("Solar System builder smoke validation failed: " + exception);
                hierarchyErrors++;
            }
            finally
            {
                if (temporaryRoot != null) Object.DestroyImmediate(temporaryRoot);
            }
            return hierarchyErrors == 0;
        }

        private static SolarSystemVisualAssets BuildVisualAssets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SolarSystemVisualAssets>(VisualAssetsPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SolarSystemVisualAssets>();
                AssetDatabase.CreateAsset(catalog, VisualAssetsPath);
            }

            catalog.sunMaterial = CreateBodyMaterial(PlanetId.Sun, true);
            catalog.orbitMaterial = LoadOrCreateMaterial("OrbitLines", "Sprites/Default");
            catalog.saturnRingMaterial = CreateRingMaterial();
            catalog.planetMaterials = new PlanetMaterialReference[PlanetaryData.Planets.Count];
            for (var i = 0; i < PlanetaryData.Planets.Count; i++)
            {
                var body = PlanetaryData.Planets[i];
                var material = CreateBodyMaterial(body.Id, false);
                catalog.planetMaterials[i] = new PlanetMaterialReference { id = body.Id, material = material };
            }
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static Material CreateBodyMaterial(PlanetId id, bool emissive)
        {
            var texturePath = $"{TextureFolder}/{id}Procedural.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = PlanetTextureGenerator.Generate(id);
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            else
            {
                var generated = PlanetTextureGenerator.Generate(id, texture.width, texture.height);
                texture.SetPixels(generated.GetPixels());
                texture.Apply(true, false);
                Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(texture);
            }

            var material = LoadOrCreateMaterial(id + "Material", "Standard");
            material.mainTexture = texture;
            material.color = Color.white;
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(1f, 0.32f, 0.035f) * 1.8f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateRingMaterial()
        {
            const string texturePath = TextureFolder + "/SaturnRings.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(128, 4, TextureFormat.RGBA32, false, false) { name = "Saturn Rings Procedural" };
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            var pixels = new Color[texture.width * texture.height];
            for (var y = 0; y < texture.height; y++)
            for (var x = 0; x < texture.width; x++)
            {
                var u = (float)x / (texture.width - 1);
                var gaps = Mathf.Abs(u - 0.47f) < 0.018f || Mathf.Abs(u - 0.76f) < 0.025f;
                var alpha = gaps ? 0.04f : (u < 0.12f ? 0.18f : 0.72f + 0.2f * Mathf.Sin(u * 180f));
                var color = Color.Lerp(new Color(0.38f, 0.28f, 0.17f), new Color(0.93f, 0.81f, 0.57f), Mathf.PerlinNoise(u * 29f, y * 0.17f));
                pixels[y * texture.width + x] = new Color(color.r, color.g, color.b, alpha);
            }
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);

            var material = LoadOrCreateMaterial("SaturnRingMaterial", "Standard");
            SetTransparent(material);
            material.mainTexture = texture;
            material.color = Color.white;
            if (material.HasProperty("_Cull")) material.SetInt("_Cull", 0);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            var path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find(shaderName) ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        private static void SetTransparent(Material material)
        {
            if (!material.HasProperty("_Mode")) return;
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
