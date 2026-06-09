using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>PlayerPrefs 的可測 seam。</summary>
    public interface IKeyValueStore
    {
        string GetString(string key, string fallback);
        void SetString(string key, string value);
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
    }

    public sealed class PlayerPrefsStore : IKeyValueStore
    {
        public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
        public void SetString(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
        public void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); }
    }
}
