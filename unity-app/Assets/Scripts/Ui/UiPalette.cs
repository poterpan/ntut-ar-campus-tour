using UnityEngine;

namespace NtutAR.Ui
{
    /// <summary>設計 tokens — 見 docs/superpowers/specs/2026-06-10-ui-ux-design.md §3</summary>
    public static class UiPalette
    {
        public static readonly Color GlassFill   = new Color(1f, 0.988f, 0.961f, 0.75f); // #FFFCF5 @ 75%
        public static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.6f);
        public static readonly Color TextMain    = new Color32(0x5D, 0x40, 0x37, 0xFF);
        public static readonly Color TextSub     = new Color32(0xA1, 0x88, 0x7F, 0xFF);
        public static readonly Color ButtonGreen = new Color32(0xAE, 0xD5, 0x81, 0xFF);
        public static readonly Color AccentOrange = new Color32(0xF5, 0x7C, 0x00, 0xFF);
        public static readonly Color WarmBgTop   = new Color32(0xF7, 0xF1, 0xE5, 0xFF);
        public static readonly Color WarmBgBottom = new Color32(0xED, 0xDF, 0xC8, 0xFF);
        public static readonly Color CardWhite   = new Color(1f, 1f, 1f, 0.75f);
    }
}
