using System.Collections.Generic;
using NUnit.Framework;

namespace NtutAR.Poi.Tests
{
    public class PoiRepositoryTests
    {
        private static List<Poi> Sample() => new List<Poi>
        {
            new Poi { id = "p01", name = "Gate", lat = 25.0436, lng = 121.5332 },
            new Poi { id = "p08", name = "ChemBldg", lat = 25.0437, lng = 121.5344 },
        };

        [Test]
        public void TryGetById_Hit_ReturnsTrue()
        {
            var repo = new PoiRepository(Sample());
            Assert.IsTrue(repo.TryGetById("p08", out var poi));
            Assert.AreEqual("ChemBldg", poi.name);
        }

        [Test]
        public void TryGetById_Miss_ReturnsFalse()
        {
            var repo = new PoiRepository(Sample());
            Assert.IsFalse(repo.TryGetById("p99", out _));
        }

        [Test]
        public void GetNearest_ReturnsClosest()
        {
            var repo = new PoiRepository(Sample());
            var near = repo.GetNearest(25.0436, 121.5332); // 幾乎在 p01
            Assert.IsTrue(near.HasValue);
            Assert.AreEqual("p01", near.Value.id);
        }

        [Test]
        public void GetNearest_EmptyList_ReturnsNull()
        {
            var repo = new PoiRepository(new List<Poi>());
            Assert.IsFalse(repo.GetNearest(0, 0).HasValue);
        }
    }
}
