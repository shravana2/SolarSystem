using UnityEngine;
using ARSolarSystem.Data;

namespace ARSolarSystem
{
    public sealed class PlanetSelectable : MonoBehaviour
    {
        public PlanetaryBodyData Data { get; private set; }

        public void Configure(PlanetaryBodyData data)
        {
            Data = data;
        }
    }
}
