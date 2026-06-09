using System.Collections.Generic;
using NUnit.Framework;
using NtutAR.Ui;

public class ExplorationServiceTests
{
    private sealed class InMemoryStore : IKeyValueStore
    {
        public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public string GetString(string key, string fallback) => Data.TryGetValue(key, out var v) ? v : fallback;
        public void SetString(string key, string value) => Data[key] = value;
        public int GetInt(string key, int fallback) => Data.TryGetValue(key, out var v) ? int.Parse(v) : fallback;
        public void SetInt(string key, int value) => Data[key] = value.ToString();
    }

    [Test]
    public void Unlock_PersistsAndRaisesEvent()
    {
        var store = new InMemoryStore();
        var svc = new ExplorationService(store);
        string unlocked = null;
        svc.StampUnlocked += id => unlocked = id;

        Assert.IsFalse(svc.IsUnlocked("p01"));
        svc.Unlock("p01");
        Assert.IsTrue(svc.IsUnlocked("p01"));
        Assert.AreEqual("p01", unlocked);
        Assert.AreEqual(1, svc.UnlockedCount);

        // 重建 service → 從 store 還原
        var svc2 = new ExplorationService(store);
        Assert.IsTrue(svc2.IsUnlocked("p01"));
    }

    [Test]
    public void Unlock_SameIdTwice_OnlyOnce()
    {
        var svc = new ExplorationService(new InMemoryStore());
        int events = 0;
        svc.StampUnlocked += _ => events++;
        svc.Unlock("p01");
        svc.Unlock("p01");
        Assert.AreEqual(1, events);
        Assert.AreEqual(1, svc.UnlockedCount);
    }

    [Test]
    public void FeedCount_IncrementsAndPersists()
    {
        var store = new InMemoryStore();
        var svc = new ExplorationService(store);
        int last = -1;
        svc.FeedCountChanged += c => last = c;
        svc.IncrementFeedCount();
        svc.IncrementFeedCount();
        Assert.AreEqual(2, svc.FeedCount);
        Assert.AreEqual(2, last);
        Assert.AreEqual(2, new ExplorationService(store).FeedCount);
    }
}
