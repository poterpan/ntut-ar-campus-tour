using System;
using UnityEngine;

namespace NtutAR.Geo
{
    // 編輯器/Play 用:把 anchor 放相機前,依 POI 順序橫向錯開
    public sealed class MockAnchorResolver : MonoBehaviour, IAnchorResolver
    {
        [SerializeField] private Camera _camera;
        private int _index;

        public void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
        {
            var cam = _camera != null ? _camera : Camera.main;
            var go = new GameObject("MockAnchor_" + poi.id);
            var basePos = cam != null ? cam.transform.position + cam.transform.forward * 3f : Vector3.forward * 3f;
            go.transform.position = basePos + new Vector3(_index * 2f, 0f, 0f);
            go.transform.rotation = Quaternion.identity;
            _index++;
            onDone(new AnchorResolveResult { PoiId = poi.id, Status = AnchorResolveStatus.Success, Anchor = go.transform });
        }
    }
}
