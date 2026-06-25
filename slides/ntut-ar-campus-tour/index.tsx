import type { DesignSystem, Page, SlideMeta, SlideTransition } from '@open-slide/core';
// NOTE: this deck lives in the NTUT project and is symlinked into the hub, so Vite
// resolves modules from the project (where @open-slide/core isn't installed). Only
// `import type` (erased at build) is safe here — runtime helpers are implemented locally.

import appIcon from './assets/app-icon.png';
import gate from './assets/gate.jpg';
import architecture from './assets/architecture.png';
import geospatial from './assets/geospatial.png';
import proximity from './assets/proximity.png';
import cicd from './assets/cicd.png';
import ci from './assets/ci.png';
import testflight from './assets/testflight.png';
import npcConcept from './assets/npc-concept.png';
import npcRig from './assets/npc-rig.png';
import npcStates from './assets/npc-states.png';
import pipeline from './assets/pipeline.png';
import rlStates from './assets/rl-states.png';
import qloop from './assets/qloop.png';
import uiOnboarding from './assets/ui-onboarding.png';
import uiHud from './assets/ui-hud.png';
import uiPoi from './assets/ui-poi.png';
import uiHandbook from './assets/ui-handbook.png';
import appDialogue from './assets/app-dialogue.png';
import appCat from './assets/app-cat.png';

export const design: DesignSystem = {
  palette: { bg: '#F6F4EF', text: '#21252F', accent: '#E07B00' },
  fonts: {
    display: '"PingFang TC", "Heiti TC", system-ui, -apple-system, "Segoe UI", sans-serif',
    body: '"PingFang TC", "Heiti TC", system-ui, -apple-system, "Segoe UI", sans-serif',
  },
  typeScale: { hero: 132, body: 36 },
  radius: 16,
};

// extra tokens outside the DesignSystem shape
const GREEN = '#4F9A64';
const MUTED = '#6F7480';
const LINE = '#E2DACB';
const CARD = '#FFFFFF';
const INK = 'var(--osd-bg)';
const TXT = 'var(--osd-text)';
const AMBER = 'var(--osd-accent)';
const DISPLAY = 'var(--osd-font-display)';

const fill = { width: '100%', height: '100%', boxSizing: 'border-box', fontFamily: 'var(--osd-font-body)' } as const;

// house transition — quiet 6px rise
export const transition: SlideTransition = {
  duration: 200,
  exit: { duration: 140, easing: 'cubic-bezier(0.4,0,1,1)', keyframes: [{ opacity: 1, transform: 'translateY(0)' }, { opacity: 0, transform: 'translateY(-4px)' }] },
  enter: { duration: 200, delay: 80, easing: 'cubic-bezier(0,0,0.2,1)', keyframes: [{ opacity: 0, transform: 'translateY(6px)' }, { opacity: 1, transform: 'translateY(0)' }] },
};

// ---------- shared helpers ----------
const Footer = () => (
  <div style={{ position: 'absolute', left: 120, right: 120, bottom: 40, display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 22, color: MUTED, borderTop: `1px solid ${LINE}`, paddingTop: 14 }}>
    <span>北科 AR 校園導覽 — LLM 導遊與 AI 互動體驗</span>
    <span>NTUT · 2026</span>
  </div>
);

const Shell = ({ eyebrow, title, children, accent = AMBER }: { eyebrow?: string; title?: string; children?: any; accent?: string }) => (
  <div style={{ ...fill, background: INK, color: TXT, padding: '84px 120px 110px', position: 'relative', display: 'flex', flexDirection: 'column' }}>
    {eyebrow && <div style={{ color: accent, fontSize: 24, fontWeight: 700, letterSpacing: '0.18em', marginBottom: 14 }}>{eyebrow}</div>}
    {title && <h2 style={{ fontFamily: DISPLAY, fontSize: 62, fontWeight: 800, margin: 0, lineHeight: 1.14 }}>{title}</h2>}
    <div style={{ marginTop: 40, flex: 1, minHeight: 0 }}>{children}</div>
    <Footer />
  </div>
);

const Figure = ({ src, maxH = 600, white = true }: { src: string; maxH?: number; white?: boolean }) => (
  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
    <img src={src} style={{ maxWidth: '100%', maxHeight: maxH, objectFit: 'contain', borderRadius: 14, border: `1px solid ${LINE}`, boxShadow: '0 12px 36px rgba(40,40,60,0.10)', ...(white ? { background: '#fff', padding: 20 } : {}), boxSizing: 'border-box' }} />
  </div>
);

const SectionDivider = ({ num, who, title }: { num: string; who: string; title: string }) => (
  <div style={{ ...fill, background: INK, color: TXT, display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '0 140px', position: 'relative', overflow: 'hidden' }}>
    <div style={{ position: 'absolute', top: 10, right: 90, fontSize: 360, fontWeight: 900, color: AMBER, opacity: 0.1, lineHeight: 1, fontFamily: DISPLAY }}>{num}</div>
    <div style={{ width: 96, height: 8, background: AMBER, borderRadius: 4, marginBottom: 34 }} />
    <div style={{ fontSize: 30, color: AMBER, letterSpacing: '0.2em', fontWeight: 700 }}>{`SECTION ${num}`}</div>
    <div style={{ fontFamily: DISPLAY, fontSize: 76, fontWeight: 900, lineHeight: 1.18, marginTop: 20, maxWidth: 1480 }}>{title}</div>
    <div style={{ fontSize: 40, color: MUTED, marginTop: 26 }}>報告人　<span style={{ color: TXT, fontWeight: 700 }}>{who}</span></div>
  </div>
);

// ---------- repeated visual elements (explicit instances, not map) ----------
const Scenario = ({ k, t, d }: { k: string; t: string; d: string }) => (
  <div style={{ flex: 1, background: CARD, border: `1px solid ${LINE}`, borderRadius: 18, padding: '40px 36px' }}>
    <div style={{ fontSize: 56, fontWeight: 900, color: AMBER, fontFamily: DISPLAY }}>{k}</div>
    <div style={{ fontSize: 38, fontWeight: 700, marginTop: 18 }}>{t}</div>
    <div style={{ fontSize: 28, color: MUTED, marginTop: 14, lineHeight: 1.5 }}>{d}</div>
  </div>
);

const Stat = ({ v, l, c = AMBER }: { v: string; l: string; c?: string }) => (
  <div style={{ flex: 1, background: CARD, border: `1px solid ${LINE}`, borderRadius: 18, padding: '32px 28px', textAlign: 'center' }}>
    <div style={{ fontSize: 60, fontWeight: 900, color: c, fontFamily: DISPLAY }}>{v}</div>
    <div style={{ fontSize: 26, color: MUTED, marginTop: 10 }}>{l}</div>
  </div>
);

const Poi = ({ n, name, d }: { n: string; name: string; d: string }) => (
  <div style={{ display: 'flex', gap: 22, alignItems: 'baseline', padding: '14px 0', borderBottom: `1px solid ${LINE}` }}>
    <span style={{ fontSize: 26, fontWeight: 800, color: AMBER, fontFamily: DISPLAY, minWidth: 56 }}>{n}</span>
    <span style={{ fontSize: 34, fontWeight: 700, minWidth: 230 }}>{name}</span>
    <span style={{ fontSize: 26, color: MUTED }}>{d}</span>
  </div>
);

const Pill = ({ children, c = GREEN }: { children: any; c?: string }) => (
  <span style={{ display: 'inline-block', background: 'rgba(0,0,0,0.035)', border: `1px solid ${c}`, color: c, borderRadius: 999, padding: '8px 22px', fontSize: 26, marginRight: 14 }}>{children}</span>
);

const VCard = ({ t, d, c = AMBER }: { t: string; d: string; c?: string }) => (
  <div style={{ flex: 1, background: CARD, border: `1px solid ${LINE}`, borderRadius: 18, padding: '34px 32px' }}>
    <div style={{ fontSize: 34, fontWeight: 800, color: c }}>{t}</div>
    <div style={{ fontSize: 27, color: MUTED, marginTop: 16, lineHeight: 1.6 }}>{d}</div>
  </div>
);

const Done = ({ t, d }: { t: string; d: string }) => (
  <div style={{ flex: 1, background: CARD, border: `1px solid ${LINE}`, borderRadius: 18, padding: '32px 30px' }}>
    <div style={{ fontSize: 34, fontWeight: 800 }}><span style={{ color: GREEN, marginRight: 12 }}>✓</span>{t}</div>
    <div style={{ fontSize: 26, color: MUTED, marginTop: 14, lineHeight: 1.55 }}>{d}</div>
  </div>
);

const bullet = { fontSize: 36, lineHeight: 1.7, margin: 0, paddingLeft: 8 } as const;

// ===================== PAGES =====================

const Cover: Page = () => (
  <div style={{ ...fill, background: INK, color: TXT, display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '0 140px', position: 'relative' }}>
    <div style={{ position: 'absolute', top: 90, right: 140, display: 'flex', alignItems: 'center', gap: 22 }}>
      <img src={appIcon} style={{ width: 116, height: 116, borderRadius: 26 }} />
    </div>
    <div style={{ color: AMBER, fontSize: 26, fontWeight: 700, letterSpacing: '0.22em' }}>電腦圖學與擴增實境 · 期末專題</div>
    <h1 style={{ fontFamily: DISPLAY, fontSize: 'var(--osd-size-hero)', fontWeight: 900, margin: '28px 0 0', lineHeight: 1.06 }}>北科 AR 校園導覽</h1>
    <div style={{ fontSize: 56, fontWeight: 700, color: TXT, marginTop: 10 }}>LLM 導遊與 AI 互動體驗</div>
    <div style={{ fontSize: 34, color: MUTED, marginTop: 34, maxWidth: 1300, lineHeight: 1.5 }}>結合 Geospatial AR、大型語言模型(LLM)與強化學習的校園導覽應用</div>
    <div style={{ display: 'flex', gap: 16, marginTop: 70, flexWrap: 'wrap' }}>
      <Pill c={AMBER}>潘柏嘉</Pill><Pill c={AMBER}>簡妤真</Pill><Pill c={AMBER}>張凱琳</Pill><Pill c={AMBER}>蔡宗育</Pill>
    </div>
    <div style={{ fontSize: 26, color: MUTED, marginTop: 40 }}>NTUT 創新 AI 所　·　2026 年 6 月</div>
  </div>
);

const Div1: Page = () => <SectionDivider num="01" who="潘柏嘉" title="專案概述 · 系統架構 · AR 定位 · 工程整合" />;

const Motivation: Page = () => (
  <Shell eyebrow="WHY — 動機" title="校園導覽的三個痛點">
    <div style={{ display: 'flex', gap: 32, height: 420 }}>
      <Scenario k="01" t="新生迷路" d="剛入學對校園不熟,密集的教學大樓常找不到目的地。" />
      <Scenario k="02" t="訪客與招生" d="校慶、招生說明會,外賓需要自助、即時、有溫度的導覽。" />
      <Scenario k="03" t="校園探索" d="一般使用者想用更有趣、遊戲化的方式認識校園。" />
    </div>
    <div style={{ marginTop: 44, fontSize: 36, lineHeight: 1.6 }}>
      傳統 GPS 在都市高樓間誤差 <span style={{ color: AMBER, fontWeight: 800 }}>5–15 公尺</span> → 無法分辨站在哪一棟樓前,難以「走到哪、講到哪」。
    </div>
  </Shell>
);

const Site: Page = () => (
  <Shell eyebrow="WHERE — 場域" title="新生南路側門路線 · 5 個 POI">
    <div style={{ display: 'flex', gap: 56, height: '100%' }}>
      <img src={gate} style={{ width: 560, height: 620, objectFit: 'cover', borderRadius: 18, border: `1px solid ${LINE}` }} />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <Poi n="p01" name="新生南路側門" d="路線起點 · 鄰近捷運忠孝新生站" />
        <Poi n="p02" name="學生餐廳入口" d="光華館美食街、便利商店、ATM" />
        <Poi n="p03" name="演講廳入口" d="第六教學大樓 B1 國際演講廳" />
        <Poi n="p04" name="第一教學大樓" d="一般課程、語言中心、工程學院" />
        <Poi n="p05" name="化工館" d="化學工程與生物科技系" />
      </div>
    </div>
  </Shell>
);

const Architecture: Page = () => (
  <Shell eyebrow="ARCHITECTURE — 系統架構" title="三層架構:手機端 / 雲端 / 訓練端">
    <Figure src={architecture} maxH={740} />
  </Shell>
);

const ARPositioning: Page = () => (
  <Shell eyebrow="功能① AR 定位" title="ARCore Geospatial(VPS 視覺定位)">
    <div style={{ height: 440 }}><Figure src={geospatial} maxH={440} /></div>
    <div style={{ display: 'flex', gap: 24, marginTop: 28 }}>
      <Stat v="< 0.5m" l="校外開闊處水平誤差" />
      <Stat v="< 1m" l="校內水平誤差" c={GREEN} />
      <Stat v="< 1°" l="方位角誤差" />
    </div>
  </Shell>
);

const Proximity: Page = () => (
  <Shell eyebrow="功能① AR 定位" title="POI 錨定與「走到就現身」">
    <Figure src={proximity} maxH={740} />
  </Shell>
);

const Engineering: Page = () => (
  <Shell eyebrow="ENGINEERING — 工程整合" title="雙平台 CI/CD 自動化建置">
    <div style={{ display: 'flex', gap: 40, height: '100%' }}>
      <div style={{ flex: 1.2 }}><Figure src={cicd} maxH={560} /></div>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 20, justifyContent: 'center' }}>
        <img src={ci} style={{ width: '100%', borderRadius: 12, border: `1px solid ${LINE}` }} />
        <img src={testflight} style={{ width: '100%', maxHeight: 300, objectFit: 'cover', objectPosition: 'top', borderRadius: 12, border: `1px solid ${LINE}` }} />
      </div>
    </div>
  </Shell>
);

const Div2: Page = () => <SectionDivider num="02" who="簡妤真" title="虛擬導遊 NPC · UI/UX 設計" />;

const NpcIntro: Page = () => (
  <Shell eyebrow="功能② 虛擬導遊" title="導遊 NPC「老黃」">
    <div style={{ display: 'flex', gap: 56, height: '100%' }}>
      <div style={{ width: 460, display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#fff', borderRadius: 18 }}>
        <img src={npcConcept} style={{ maxHeight: 600, maxWidth: '100%', objectFit: 'contain' }} />
      </div>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <ul style={{ ...bullet }}>
          <li>Jenson 角色:皮衣眼鏡造型,暱稱「老黃」</li>
          <li>走近 POI 自動現身、開口解說</li>
          <li>四組動畫:聆聽 / 說話 / 走動 / 揮手</li>
          <li>面向使用者時平滑轉身、不會瞬間扭頭(Slerp 球面插值補間)</li>
        </ul>
      </div>
    </div>
  </Shell>
);

const NpcMaking: Page = () => (
  <Shell eyebrow="功能② 虛擬導遊" title="生成式角色製作 — 與綁定踩雷">
    <div style={{ display: 'flex', gap: 48, height: '100%' }}>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <div style={{ fontSize: 32, lineHeight: 1.7, color: TXT }}>
          <div><span style={{ color: AMBER, fontWeight: 800 }}>1.</span> AI 生成角色概念圖(T-pose)</div>
          <div style={{ marginTop: 14 }}><span style={{ color: AMBER, fontWeight: 800 }}>2.</span> Meshy 由圖生成 3D biped 模型</div>
          <div style={{ marginTop: 14 }}><span style={{ color: AMBER, fontWeight: 800 }}>3.</span> 匯入 Unity,綁骨 + 四組動畫</div>
        </div>
        <div style={{ marginTop: 36, background: CARD, border: `1px solid ${LINE}`, borderRadius: 16, padding: '28px 30px' }}>
          <div style={{ fontSize: 28, fontWeight: 800, color: AMBER }}>踩雷與解決</div>
          <div style={{ fontSize: 27, color: MUTED, marginTop: 12, lineHeight: 1.6 }}>綁骨後手臂網格被拉扯沾黏到大腿 → 改成<span style={{ color: GREEN, fontWeight: 700 }}> T-pose 參考圖</span>,蒙皮權重才正確分離,才有流暢動畫。</div>
        </div>
      </div>
      <div style={{ width: 540, display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#15181f', borderRadius: 18, border: `1px solid ${LINE}` }}>
        <img src={npcRig} style={{ maxHeight: 600, maxWidth: '100%', objectFit: 'contain', borderRadius: 12 }} />
      </div>
    </div>
  </Shell>
);

const Dialogue: Page = () => (
  <Shell eyebrow="功能② 虛擬導遊" title="對話介面與動畫狀態機">
    <div style={{ display: 'flex', gap: 56, height: '100%' }}>
      <img src={appDialogue} style={{ height: '100%', maxHeight: 720, objectFit: 'contain', borderRadius: 24, border: `1px solid ${LINE}`, alignSelf: 'center' }} />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <div style={{ height: 210, marginBottom: 30 }}><Figure src={npcStates} maxH={210} /></div>
        <ul style={{ ...bullet }}>
          <li>對話面板顯示「老黃思考中…」「正在生成語音…」狀態</li>
          <li>送出時鎖定按鈕,避免重複送出</li>
          <li>文字先上畫面 → 語音隨後播放,NPC 同步切換動畫</li>
        </ul>
      </div>
    </div>
  </Shell>
);

const UIShot = ({ src, label }: { src: string; label: string }) => (
  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12 }}>
    <img src={src} style={{ height: 520, objectFit: 'contain', borderRadius: 18, border: `1px solid ${LINE}`, boxShadow: '0 8px 24px rgba(40,40,60,0.10)' }} />
    <div style={{ fontSize: 24, color: MUTED }}>{label}</div>
  </div>
);

const UIUX: Page = () => (
  <Shell eyebrow="DESIGN — UI/UX" title="暖色玻璃擬態設計系統">
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <div style={{ fontSize: 30, lineHeight: 1.6 }}>皮克敏暖色 + 玻璃擬態(glassmorphism),以程式化 UI builder 產生,風格一致、版控可控。</div>
      <div style={{ display: 'flex', gap: 14, marginTop: 18 }}>
        <Pill c="#E07B00">AccentOrange</Pill><Pill c={GREEN}>ButtonGreen</Pill><Pill c="#B08A4F">WarmBg</Pill><Pill c="#9A7B6A">GlassFill</Pill>
      </div>
      <div style={{ flex: 1, display: 'flex', gap: 40, justifyContent: 'center', alignItems: 'center' }}>
        <UIShot src={uiOnboarding} label="開場 Onboarding" />
        <UIShot src={uiHud} label="AR HUD 主畫面" />
        <UIShot src={uiPoi} label="POI 景點抽屜" />
        <UIShot src={uiHandbook} label="探索手帳" />
      </div>
    </div>
  </Shell>
);

const Div3: Page = () => <SectionDivider num="03" who="張凱琳" title="LLM 即時對話 · 語音 pipeline" />;

const LLM: Page = () => (
  <Shell eyebrow="功能③ LLM 對話" title="每棟建築專屬人設的即時對話">
    <ul style={{ ...bullet, marginBottom: 36 }}>
      <li>用統一介面 <code style={{ color: GREEN }}>ILlmClient</code> 接 LLM,可隨時抽換模型 / 供應商</li>
      <li>每個 POI 帶專屬 system prompt(該地標的知識與人設)</li>
      <li>只依該 POI 上下文回答,避免幻覺;先直答、最多 6 點</li>
    </ul>
    <div style={{ background: CARD, border: `1px solid ${LINE}`, borderRadius: 16, padding: '28px 32px', fontSize: 28, color: MUTED, lineHeight: 1.6 }}>
      <span style={{ color: AMBER }}>CURRENT_POI</span> = 化工館 → 導遊以化工系的知識回答提問
    </div>
  </Shell>
);

const DialogueFlow: Page = () => (
  <Shell eyebrow="功能③ LLM 對話" title="對話流程與穩定性">
    <div style={{ display: 'flex', gap: 56, height: '100%' }}>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <ul style={{ ...bullet }}>
          <li>文字先顯示 → 語音隨後合成播放</li>
          <li>429 / 逾時 / 5xx → 退避重試(≤2 次)</li>
          <li>仍失敗 → 友善降級訊息,App 不卡死</li>
        </ul>
      </div>
      <div style={{ width: 660, height: '100%' }}><Figure src={pipeline} maxH={760} /></div>
    </div>
  </Shell>
);

const Voice: Page = () => (
  <Shell eyebrow="VOICE — 語音 pipeline" title="TTS · STT · 簡繁轉換">
    <div style={{ display: 'flex', gap: 32, height: 460 }}>
      <VCard t="ElevenLabs STT" d="按住麥克風說話,放開即送出辨識(scribe_v1,最長 15 秒)。" c={GREEN} />
      <VCard t="GLM TTS" d="念出回答。音色「小陳」、語速 1.3×;以能量偵測裁掉免費版前置提示音。" />
      <VCard t="OpenCC 簡繁" d="端側 s2tw 轉換,把雲端輸出的簡體與用詞修正為台灣正體。" c="#C9B79C" />
    </div>
  </Shell>
);

const Div4: Page = () => <SectionDivider num="04" who="蔡宗育" title="AR 校園貓 · 強化學習" />;

const Cat: Page = () => (
  <Shell eyebrow="功能④ AR 校園貓" title="Pokémon Go 式召喚彩蛋">
    <div style={{ display: 'flex', gap: 48, height: '100%' }}>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <ul style={{ ...bullet }}>
          <li>點「罐頭」鈕 → 點 AR 平面放下飼料</li>
          <li>貓在約 2.5m 外現身,自動朝最近罐頭移動</li>
          <li>吃完一個轉向下一個,支援連續多罐頭</li>
          <li>端側查表推論,與導覽主線解耦</li>
        </ul>
      </div>
      <div style={{ width: 460, display: 'flex', justifyContent: 'center' }}>
        <img src={appCat} style={{ height: '100%', maxHeight: 740, objectFit: 'contain', borderRadius: 24, border: `1px solid ${LINE}` }} />
      </div>
    </div>
  </Shell>
);

const RLDesign: Page = () => (
  <Shell eyebrow="REINFORCEMENT LEARNING" title="強化學習 — Tabular Q-Learning">
    <div style={{ display: 'flex', gap: 40, height: '100%' }}>
      <div style={{ flex: 1.1 }}><Figure src={rlStates} maxH={620} /></div>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <ul style={{ ...bullet }}>
          <li>狀態:距離 4 × 角度 8 = <span style={{ color: AMBER, fontWeight: 800 }}>32 離散態</span></li>
          <li>動作:直走 / 左轉 / 右轉</li>
          <li>Reward:距離 shaping + 對齊 + 吃到 +15</li>
        </ul>
        <div style={{ fontSize: 26, color: MUTED, marginTop: 24, lineHeight: 1.6 }}>評估後此低維任務不需 ML-Agents 等級的神經網路,改用更輕量的經典 RL 方法。</div>
      </div>
    </div>
  </Shell>
);

const Training: Page = () => (
  <Shell eyebrow="功能④ AR 校園貓" title="訓練成果">
    <div style={{ display: 'flex', gap: 48, height: '100%', alignItems: 'center' }}>
      <div style={{ flex: 1 }}><Figure src={qloop} maxH={300} /></div>
      <div style={{ width: 460, display: 'flex', flexDirection: 'column', gap: 24 }}>
        <Stat v="1300" l="訓練 episodes(離線 Q-table)" />
        <Stat v="32 × 3" l="狀態 × 動作 查表" c={GREEN} />
        <div style={{ fontSize: 26, color: MUTED, lineHeight: 1.6 }}>主 App 純查表貪婪行動,模型僅一個小 JSON,零額外相依。</div>
      </div>
    </div>
  </Shell>
);

const Demo: Page = () => (
  <div style={{ ...fill, background: INK, color: TXT, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 36 }}>
    <div style={{ width: 0, height: 0, borderTop: '46px solid transparent', borderBottom: '46px solid transparent', borderLeft: `78px solid ${AMBER}`, marginLeft: 20 }} />
    <div style={{ fontFamily: DISPLAY, fontSize: 130, fontWeight: 900 }}>Demo 影片</div>
    <div style={{ fontSize: 38, color: MUTED }}>四大功能整合實機演示 — 定位 → 導遊 → 語音問答 → 校園貓</div>
  </div>
);

const Results: Page = () => (
  <Shell eyebrow="RESULTS — 成果總結" title="四大功能全數達成,雙平台部署">
    <div style={{ display: 'flex', gap: 28, height: 380 }}>
      <Done t="AR 定位導覽" d="Geospatial 公尺以下精度 + 接近觸發" />
      <Done t="虛擬導遊 NPC" d="Jenson + 動畫狀態機 + 解說" />
      <Done t="LLM 對話與語音" d="專屬人設 + TTS / STT / 簡繁" />
      <Done t="AR 校園貓 RL" d="Q-Learning 1300 episodes" />
    </div>
    <div style={{ marginTop: 40, fontSize: 32, lineHeight: 1.6 }}>
      實機驗證:<span style={{ color: GREEN, fontWeight: 700 }}>iOS TestFlight + Android</span> 雙平台 · 約 4 週、47 個 commit · 自動化 CI/CD 部署。
    </div>
  </Shell>
);

const Conclusion: Page = () => (
  <Shell eyebrow="CONCLUSION — 結論與未來" title="結論與未來展望">
    <div style={{ display: 'flex', gap: 40, height: '100%' }}>
      <div style={{ flex: 1, background: 'rgba(224,123,0,0.07)', border: `1px solid ${AMBER}`, borderRadius: 18, padding: '34px 36px' }}>
        <div style={{ fontSize: 32, fontWeight: 800, color: AMBER }}>目前痛點:即時性</div>
        <div style={{ fontSize: 29, color: TXT, marginTop: 18, lineHeight: 1.65 }}>對話走 <b>STT → LLM → TTS</b> 三段雲端串接,延遲累積;尤其 LLM 整段回完才送 TTS,Demo 可見停頓。</div>
        <div style={{ fontSize: 26, color: MUTED, marginTop: 18, lineHeight: 1.6 }}>優化:串流式 TTS、首句先回、邊收邊播、減少串接段數。</div>
      </div>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <div style={{ fontSize: 30, fontWeight: 700, marginBottom: 18 }}>後續方向</div>
        <ul style={{ fontSize: 32, lineHeight: 1.8, margin: 0, paddingLeft: 8 }}>
          <li>RAG 校園知識庫</li>
          <li>多語系(英 / 日)導覽</li>
          <li>更多 POI 與主題路線</li>
          <li>校園貓多目標 RL</li>
        </ul>
      </div>
    </div>
  </Shell>
);

const Thanks: Page = () => (
  <div style={{ ...fill, background: INK, color: TXT, display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '0 140px' }}>
    <div style={{ width: 96, height: 8, background: AMBER, borderRadius: 4, marginBottom: 36 }} />
    <h1 style={{ fontFamily: DISPLAY, fontSize: 140, fontWeight: 900, margin: 0 }}>謝謝聆聽</h1>
    <div style={{ fontSize: 44, color: AMBER, marginTop: 20, fontWeight: 600 }}>Q & A</div>
    <div style={{ fontSize: 30, color: MUTED, marginTop: 44, lineHeight: 1.7 }}>
      潘柏嘉 · 簡妤真 · 張凱琳 · 蔡宗育<br />
      iOS TestFlight　·　Android APK　·　原始碼公開於 GitHub
    </div>
  </div>
);

export const meta: SlideMeta = {
  title: '北科 AR 校園導覽 — 期末報告',
  createdAt: '2026-06-25T10:17:26.972Z',
};

export default [
  Cover,
  Div1, Motivation, Site, Architecture, ARPositioning, Proximity, Engineering,
  Div2, NpcIntro, NpcMaking, Dialogue, UIUX,
  Div3, LLM, DialogueFlow, Voice,
  Div4, Cat, RLDesign, Training,
  Demo,
  Results, Conclusion, Thanks,
] satisfies Page[];
