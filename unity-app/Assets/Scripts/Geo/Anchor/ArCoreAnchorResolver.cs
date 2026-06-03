using System;
using System.Collections;
using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARFoundation;

namespace NtutAR.Geo
{
    // 實機用:依 anchorType 經 ARCore Extensions 解析 Geospatial / Terrain anchor
    public sealed class ArCoreAnchorResolver : MonoBehaviour, IAnchorResolver
    {
        [SerializeField] private ARAnchorManager _anchorManager;

        public void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
        {
            StartCoroutine(ResolveRoutine(poi, onDone));
        }

        private IEnumerator ResolveRoutine(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
        {
            var rot = Quaternion.identity;

            if (poi.AnchorType == NtutAR.Poi.PoiAnchorType.Geospatial)
            {
                var anchor = _anchorManager.AddAnchor(poi.lat, poi.lng, poi.altitude, rot);
                onDone(Result(poi.id, anchor != null, anchor != null ? anchor.transform : null));
                yield break;
            }

            // Terrain(預設):altitude 由地形解析
            var promise = _anchorManager.ResolveAnchorOnTerrainAsync(poi.lat, poi.lng, 0, rot);
            yield return promise;
            var res = promise.Result;
            bool ok = res.TerrainAnchorState == TerrainAnchorState.Success && res.Anchor != null;
            onDone(Result(poi.id, ok, ok ? res.Anchor.transform : null));
        }

        private static AnchorResolveResult Result(string id, bool ok, Transform t)
        {
            return new AnchorResolveResult
            {
                PoiId = id,
                Status = ok ? AnchorResolveStatus.Success : AnchorResolveStatus.Failed,
                Anchor = t
            };
        }
    }
}
