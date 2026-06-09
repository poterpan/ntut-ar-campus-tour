using System;
using System.Collections.Generic;
using System.Linq;

namespace NtutAR.Ui
{
    /// <summary>探索進度:POI 集章 + 餵貓計數。純 C#,由 HudController 持有單一實例。</summary>
    public sealed class ExplorationService
    {
        private const string StampsKey = "ntut.stamps";     // csv of poi ids
        private const string FeedKey = "ntut.feedCount";

        private readonly IKeyValueStore _store;
        private readonly HashSet<string> _unlocked;

        public event Action<string> StampUnlocked;
        public event Action<int> FeedCountChanged;

        public int UnlockedCount => _unlocked.Count;
        public int FeedCount { get; private set; }

        public ExplorationService(IKeyValueStore store)
        {
            _store = store;
            string csv = store.GetString(StampsKey, "");
            _unlocked = new HashSet<string>(csv.Split(',').Where(s => s.Length > 0));
            FeedCount = store.GetInt(FeedKey, 0);
        }

        public bool IsUnlocked(string poiId) => _unlocked.Contains(poiId);

        public void Unlock(string poiId)
        {
            if (!_unlocked.Add(poiId)) return;
            _store.SetString(StampsKey, string.Join(",", _unlocked));
            StampUnlocked?.Invoke(poiId);
        }

        public void IncrementFeedCount()
        {
            FeedCount++;
            _store.SetInt(FeedKey, FeedCount);
            FeedCountChanged?.Invoke(FeedCount);
        }
    }
}
