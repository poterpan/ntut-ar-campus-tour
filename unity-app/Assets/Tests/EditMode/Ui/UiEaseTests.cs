using NUnit.Framework;
using NtutAR.Ui;

public class UiEaseTests
{
    [Test]
    public void OutCubic_Endpoints()
    {
        Assert.That(UiEase.OutCubic(0f), Is.EqualTo(0f).Within(1e-5f));
        Assert.That(UiEase.OutCubic(1f), Is.EqualTo(1f).Within(1e-5f));
    }

    [Test]
    public void OutCubic_FastStart()
    {
        // t=0.5 時應已超過 0.8(快進慢出)
        Assert.Greater(UiEase.OutCubic(0.5f), 0.8f);
    }

    [Test]
    public void OutBack_Overshoots()
    {
        // OutBack 中段會超過 1
        Assert.Greater(UiEase.OutBack(0.7f), 1f);
        Assert.That(UiEase.OutBack(1f), Is.EqualTo(1f).Within(1e-4f));
    }
}
