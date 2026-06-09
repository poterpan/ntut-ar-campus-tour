using NUnit.Framework;
using NtutAR.Ui;
using UnityEngine;

public class GeoMapProjectorTests
{
    // 北科校園概略範圍(西北角 / 東南角)
    private static readonly GeoRect Campus = new GeoRect(25.0445, 121.5330, 25.0405, 121.5375);

    [Test]
    public void Corners_MapToUvCorners()
    {
        Vector2 nw = GeoMapProjector.ToUv(25.0445, 121.5330, Campus);
        Vector2 se = GeoMapProjector.ToUv(25.0405, 121.5375, Campus);
        Assert.That(nw.x, Is.EqualTo(0f).Within(1e-4f));
        Assert.That(nw.y, Is.EqualTo(1f).Within(1e-4f));   // 北 = 上
        Assert.That(se.x, Is.EqualTo(1f).Within(1e-4f));
        Assert.That(se.y, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void Center_MapsToHalf()
    {
        Vector2 uv = GeoMapProjector.ToUv(25.0425, 121.53525, Campus);
        Assert.That(uv.x, Is.EqualTo(0.5f).Within(1e-3f));
        Assert.That(uv.y, Is.EqualTo(0.5f).Within(1e-3f));
    }

    [Test]
    public void OutOfBounds_Clamped()
    {
        Vector2 uv = GeoMapProjector.ToUv(26.0, 122.0, Campus);
        Assert.That(uv.x, Is.EqualTo(1f).Within(1e-4f));
        Assert.That(uv.y, Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void Distance_ZeroForSamePoint()
    {
        Assert.That(GeoMapProjector.DistanceMeters(25.0425, 121.535, 25.0425, 121.535),
            Is.EqualTo(0).Within(0.01));
    }

    [Test]
    public void Distance_KnownValue()
    {
        // 緯度差 0.001 度 ≈ 111.2m
        double d = GeoMapProjector.DistanceMeters(25.0420, 121.535, 25.0430, 121.535);
        Assert.That(d, Is.EqualTo(111.2).Within(1.5));
    }
}
