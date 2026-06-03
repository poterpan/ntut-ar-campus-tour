using System;
using UnityEngine;

namespace NtutAR.Geo
{
    public enum AnchorResolveStatus { Pending, Success, Failed }

    public sealed class AnchorResolveResult
    {
        public string PoiId;
        public AnchorResolveStatus Status;
        public Transform Anchor;   // Success 時非 null
    }

    public interface IAnchorResolver
    {
        // 對一個 POI 發起解析;完成時回呼(可能非同步)
        void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone);
    }
}
