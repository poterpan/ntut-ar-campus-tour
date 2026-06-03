using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NtutAR.Geo.Tests
{
    public class AnchorRegistryTests
    {
        private sealed class FakeResolver : IAnchorResolver
        {
            public readonly List<string> Requested = new List<string>();
            private Action<AnchorResolveResult> _cb;

            public void Resolve(NtutAR.Poi.Poi poi, Action<AnchorResolveResult> onDone)
            {
                Requested.Add(poi.id);
                _cb = onDone;
            }

            public void Complete(string poiId, bool success, Transform anchor)
            {
                _cb(new AnchorResolveResult
                {
                    PoiId = poiId,
                    Status = success ? AnchorResolveStatus.Success : AnchorResolveStatus.Failed,
                    Anchor = anchor
                });
            }
        }

        private static List<NtutAR.Poi.Poi> Pois(params string[] ids)
        {
            var list = new List<NtutAR.Poi.Poi>();
            foreach (var id in ids)
                list.Add(new NtutAR.Poi.Poi { id = id, name = id, anchorType = "Terrain" });
            return list;
        }

        [Test]
        public void ResolveAll_DispatchesOncePerPoi()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            reg.ResolveAll(Pois("p01", "p02"));
            Assert.AreEqual(new[] { "p01", "p02" }, r.Requested.ToArray());
        }

        [Test]
        public void Success_RegistersAnchor()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            var go = new GameObject("a");
            try
            {
                reg.ResolveAll(Pois("p01"));
                r.Complete("p01", true, go.transform);
                Assert.AreSame(go.transform, reg.GetAnchor("p01"));
                Assert.AreEqual(1, reg.ResolvedCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Failure_NoAnchor()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            reg.ResolveAll(Pois("p01"));
            r.Complete("p01", false, null);
            Assert.IsNull(reg.GetAnchor("p01"));
            Assert.AreEqual(0, reg.ResolvedCount);
        }

        [Test]
        public void ResolveAll_SkipsInFlightAndResolved()
        {
            var r = new FakeResolver();
            var reg = new AnchorRegistry(r);
            reg.ResolveAll(Pois("p01"));
            reg.ResolveAll(Pois("p01"));
            Assert.AreEqual(1, r.Requested.Count);
        }

        [Test]
        public void GetAnchor_Unknown_ReturnsNull()
        {
            var reg = new AnchorRegistry(new FakeResolver());
            Assert.IsNull(reg.GetAnchor("nope"));
        }
    }
}
