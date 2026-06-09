using UnityEngine;
using UnityEngine.UI;
using NtutAR.Poi;
using NtutAR.Cat;

namespace NtutAR.Ui
{
    /// <summary>UI 場景接線中樞:HUD 按鈕 ↔ 抽屜/手帳,餵貓事件 → ExplorationService。
    /// 掛在場景的 UiSystem 物件上,Inspector 接好引用。</summary>
    public sealed class UiRoot : MonoBehaviour
    {
        [SerializeField] private PoiService _poiService;
        [SerializeField] private HudController _hud;
        [SerializeField] private PoiDrawerPanel _drawer;
        [SerializeField] private HandbookPanel _handbook;
        [SerializeField] private Button _drawerHandle;
        [SerializeField] private Button _handbookButton;
        [SerializeField] private CatSummonController _catSummon;

        private void Start()
        {
            _drawerHandle.onClick.AddListener(() => _drawer.Toggle(_poiService, _hud.Exploration));
            _hud.GeoUpdated += _drawer.UpdateGeo;
            _handbookButton.onClick.AddListener(() => _handbook.Open(_poiService, _hud.Exploration));
            if (_catSummon != null)
                _catSummon.CatFed += () => _hud.Exploration.IncrementFeedCount();
        }
    }
}
