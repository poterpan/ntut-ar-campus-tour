using UnityEngine;

namespace NtutAR.Guide.Mocks
{
    public sealed class MockPoiAnchorProvider : MonoBehaviour, IPoiAnchorProvider
    {
        [SerializeField] private Transform _anchorPoint;   // 場景放一個點(鏡頭前)

        public Transform GetAnchor(string poiId) => _anchorPoint;
    }
}
