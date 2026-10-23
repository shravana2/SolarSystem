using System;
using UnityEngine;
using ARSolarSystem.Data;

namespace ARSolarSystem
{
    [Serializable]
    public sealed class PlanetMaterialReference
    {
        public PlanetId id;
        public Material material;
    }

    [CreateAssetMenu(menuName = "AR Solar System/Visual Assets", fileName = "SolarSystemVisualAssets")]
    public sealed class SolarSystemVisualAssets : ScriptableObject
    {
        public Material sunMaterial;
        public Material orbitMaterial;
        public Material saturnRingMaterial;
        public PlanetMaterialReference[] planetMaterials = Array.Empty<PlanetMaterialReference>();

        public Material GetPlanetMaterial(PlanetId id)
        {
            if (planetMaterials == null) return null;
            for (var i = 0; i < planetMaterials.Length; i++)
                if (planetMaterials[i].id == id) return planetMaterials[i].material;
            return null;
        }
    }
}
