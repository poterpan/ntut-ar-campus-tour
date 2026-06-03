using UnityEngine;
using NtutAR.Poi;

namespace NtutAR.Geo
{
    public sealed class GeospatialAnchorManager : MonoBehaviour, NtutAR.Guide.IPoiAnchorProvider
    {
        [SerializeField] private PoiService _poiService;
        [SerializeField] private MonoBehaviour _resolverBehaviour;   // IAnchorResolver(Mock 或 ArCore)

        private AnchorRegistry _registry;

        private void Awake()
        {
            var resolver = _resolverBehaviour as IAnchorResolver;
            _registry = new AnchorRegistry(resolver);
        }

        // 由 ArLocalizationController 在定位完成後呼叫
        public void ResolveAllPois()
        {
            if (_poiService != null)
                _registry.ResolveAll(_poiService.All);
        }

        public Transform GetAnchor(string poiId)
        {
            return _registry != null ? _registry.GetAnchor(poiId) : null;
        }
    }
}
