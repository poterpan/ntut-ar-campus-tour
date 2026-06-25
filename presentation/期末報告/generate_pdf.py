#!/usr/bin/env python3
"""Generate the final-report PDF from a markdown file, following CLAUDE.md specs.

Usage:
    python generate_pdf.py <input.md> <output.pdf> ["PDF Title"]

Fonts are EMBEDDED TrueType (Arial Unicode MS for full glyph coverage incl. CJK /
Greek / math / box-drawing, Heiti TC Medium for bold) so the PDF renders in any
viewer — unlike the non-embedded STSong-Light CID font, whose CJK glyphs go blank
where the Adobe Asian font pack is absent.

Handles: 4 heading levels (doc title + chapter # + ## + ###), centered title-block
+ subtitle, green key-takeaways, CJK-safe code blocks (XPreformatted), tables,
lists, blockquotes, links. Dependencies: reportlab only.
"""

import re
import sys
import os
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib.colors import HexColor, black
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_CENTER
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    XPreformatted, HRFlowable, Image, PageBreak
)
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfbase.cidfonts import UnicodeCIDFont
from reportlab.lib.fonts import addMapping

# ---- Fonts: 微軟正黑體 (Microsoft JhengHei) for all text — CJK + Latin. ----
_DF = '/Applications/Microsoft Word.app/Contents/Resources/DFonts'
_MSJH = f'{_DF}/MSJH.ttf'      # 微軟正黑體 Regular
_MSJHB = f'{_DF}/MSJHBD.ttf'   # 微軟正黑體 Bold
_UNI = '/System/Library/Fonts/Supplemental/Arial Unicode.ttf'
try:
    pdfmetrics.registerFont(TTFont('MSJH', _MSJH))
    pdfmetrics.registerFont(TTFont('MSJHB', _MSJHB))
    pdfmetrics.registerFont(TTFont('Uni', _UNI))   # code-block non-ASCII fallback (box-drawing 等)
    addMapping('MSJH', 0, 0, 'MSJH')    # normal
    addMapping('MSJH', 1, 0, 'MSJHB')   # <b> -> 微軟正黑 Bold
    BASE, BOLD, CODE_NONASCII = 'MSJH', 'MSJHB', 'Uni'
except Exception as e:                  # portability fallback
    sys.stderr.write(f'[warn] 微軟正黑 unavailable ({e}); using STSong-Light CID\n')
    pdfmetrics.registerFont(UnicodeCIDFont('STSong-Light'))
    BASE = BOLD = CODE_NONASCII = 'STSong-Light'

# Colors from CLAUDE.md
NAVY = HexColor('#1a1a2e')
H1_COLOR = HexColor('#16213e')
H2_COLOR = HexColor('#0f3460')
H3_COLOR = HexColor('#3a5a78')
CODE_BG = HexColor('#f5f5f5')
TABLE_HEADER_BG = HexColor('#e8eaf6')
BODY_COLOR = HexColor('#333333')
GREEN = HexColor('#2e7d32')
LINK_COLOR = HexColor('#1565c0')
QUOTE_BG = HexColor('#fff3e0')

styles = {
    'title': ParagraphStyle('Title', fontSize=21, leading=27, textColor=NAVY,
                            alignment=TA_CENTER, spaceAfter=6, fontName=BOLD),
    'subtitle': ParagraphStyle('Subtitle', fontSize=12, leading=17, textColor=H2_COLOR,
                            alignment=TA_CENTER, spaceAfter=8, fontName=BOLD),
    'info': ParagraphStyle('Info', fontSize=10, leading=15, textColor=BODY_COLOR,
                            alignment=TA_CENTER, spaceAfter=3, fontName=BASE),
    'h1': ParagraphStyle('H1', fontSize=16, leading=22, textColor=H1_COLOR,
                            spaceBefore=16, spaceAfter=8, fontName=BOLD),
    'h2': ParagraphStyle('H2', fontSize=13, leading=18, textColor=H2_COLOR,
                            spaceBefore=12, spaceAfter=6, fontName=BOLD),
    'h3': ParagraphStyle('H3', fontSize=11, leading=16, textColor=H3_COLOR,
                            spaceBefore=8, spaceAfter=4, fontName=BOLD),
    'body': ParagraphStyle('Body', fontSize=10, leading=15.5, textColor=BODY_COLOR,
                            spaceAfter=6, fontName=BASE),
    'bullet': ParagraphStyle('Bullet', fontSize=10, leading=15.5, textColor=BODY_COLOR,
                            leftIndent=20, bulletIndent=8, spaceAfter=3, fontName=BASE),
    'code': ParagraphStyle('Code', fontSize=8.5, leading=12, textColor=black,
                            fontName='Courier', backColor=CODE_BG, borderPadding=6,
                            leftIndent=8, rightIndent=8, spaceAfter=8),
    'takeaway': ParagraphStyle('Takeaway', fontSize=10, leading=15.5, textColor=GREEN,
                            spaceAfter=6, fontName=BOLD, leftIndent=6),
    'link': ParagraphStyle('Link', fontSize=10, leading=15, textColor=LINK_COLOR,
                            alignment=TA_CENTER, spaceAfter=6, fontName=BASE),
    'quote': ParagraphStyle('Quote', fontSize=9.5, leading=14, textColor=HexColor('#8a5a00'),
                            leftIndent=14, spaceAfter=8, fontName=BASE,
                            backColor=QUOTE_BG, borderPadding=6),
    'caption': ParagraphStyle('Caption', fontSize=9, leading=13, textColor=HexColor('#666666'),
                            alignment=TA_CENTER, spaceBefore=2, spaceAfter=10, fontName=BASE),
}

# Emoji / symbols absent from the fonts -> strip
_EMOJI = ['🟢', '✅', '✔', '▶', '★', '🆕', '📇', '📊', '⚠️', '⚠', '👍', '✓']
# Box-drawing -> ASCII keeps code-block alignment in monospace Courier
_BOX = {ord(c): '+' for c in '┌┐└┘├┤┬┴┼╭╮╰╯'}
_BOX[ord('─')] = '-'
_BOX[ord('│')] = '|'


def sanitize(text):
    for e in _EMOJI:
        text = text.replace(e, '')
    return text.replace('…', '...')


def _inline_code(m):
    """Render inline `code`: ASCII in Courier, CJK/non-ASCII runs in the base font."""
    parts = re.split(r'([^\x00-\x7F]+)', m.group(1))
    out = ''
    for k, p in enumerate(parts):
        if not p:
            continue
        face = BASE if k % 2 == 1 else 'Courier'   # odd indices = non-ASCII runs
        out += '<font face="%s" size="9" color="#c62828">%s</font>' % (face, p)
    return out


def format_inline(text):
    """Inline markdown -> ReportLab mini-XML. Base font covers CJK/Greek/math."""
    text = sanitize(text)
    text = re.sub(r'\*\*`([^`]+)`\*\*', r'<b><font face="Courier" size="9">\1</font></b>', text)
    text = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', text)
    text = re.sub(r'`([^`]+)`', _inline_code, text)
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'<a href="\2" color="#1565c0">\1</a>', text)
    return text


def build_code(code_lines):
    code_text = '\n'.join(code_lines).translate(_BOX)
    code_text = code_text.replace("maxₐ'", "max_a'").replace('ₐ', 'a')
    code_text = sanitize(code_text)
    code_text = code_text.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
    # wrap non-ASCII runs (CJK / Greek / box) in a full-coverage font; Latin stays Courier
    code_text = re.sub(r'([^\x00-\x7F]+)', r'<font face="%s">\1</font>' % CODE_NONASCII, code_text)
    return XPreformatted(code_text, styles['code'])


def build_image(path, alt, base_dir, width_mm=None):
    """Embed an image, downsampled to ~200dpi of its on-page size and re-encoded
    (smaller of JPEG-q88 / optimized-PNG) to keep the PDF small. Diagrams stay PNG
    (lossless, crisp); photos become JPEG. Compressed variants cached in temp."""
    full = path if os.path.isabs(path) else os.path.join(base_dir, path)
    if not os.path.isfile(full):
        return [Paragraph('<i>[image missing: %s]</i>' % path, styles['caption'])]
    from reportlab.lib.utils import ImageReader
    try:
        from PIL import Image as PILImage
        pil = PILImage.open(full)
        w, h = pil.size
    except Exception:
        pil = None
        w, h = ImageReader(full).getSize()
    max_w = A4[0] - 40 * mm
    max_h = 135 * mm
    if width_mm:
        tw = width_mm * mm
        th = tw * h / w
    else:
        tw, th = max_w, max_w * h / w
        if th > max_h:
            th, tw = max_h, max_h * w / h
    src = full
    if pil is not None:
        import io, tempfile, hashlib  # noqa
        target_px = max(1, int(tw / 72.0 * 200))            # ~200 dpi of on-page width
        if w > target_px:
            pil = pil.resize((target_px, max(1, round(h * target_px / w))), PILImage.LANCZOS)
        has_alpha = pil.mode in ('RGBA', 'LA') or (pil.mode == 'P' and 'transparency' in pil.info)
        cache = os.path.join(tempfile.gettempdir(), 'osd-imgcache')
        os.makedirs(cache, exist_ok=True)
        key = hashlib.md5(('%s|%d|%s' % (full, int(tw), os.path.getmtime(full))).encode()).hexdigest()
        if has_alpha:
            src = os.path.join(cache, key + '.png')
            pil.save(src, format='PNG', optimize=True)
        else:
            jp = os.path.join(cache, key + '.jpg')
            pil.convert('RGB').save(jp, format='JPEG', quality=88)
            pp = os.path.join(cache, key + '.png')
            pil.save(pp, format='PNG', optimize=True)
            src = jp if os.path.getsize(jp) <= os.path.getsize(pp) else pp   # smaller wins
    img = Image(src, width=tw, height=th)
    img.hAlign = 'CENTER'
    out = [Spacer(1, 4), img]
    if alt.strip():
        out.append(Paragraph(sanitize(alt), styles['caption']))
    else:
        out.append(Spacer(1, 8))
    return out


def parse_table(lines):
    rows = []
    for line in lines:
        line = line.strip()
        if line.startswith('|') and not re.match(r'^\|[\s\-:|]+\|$', line):
            rows.append([c.strip() for c in line.split('|')[1:-1]])
    return rows


def build_table(rows):
    if not rows:
        return None
    formatted = []
    for i, row in enumerate(rows):
        frow = []
        for cell in row:
            if i == 0:
                st = ParagraphStyle('TH', fontSize=9, leading=12, fontName=BOLD, textColor=H1_COLOR)
            else:
                st = ParagraphStyle('TD', fontSize=9, leading=12.5, fontName=BASE, textColor=BODY_COLOR)
            frow.append(Paragraph(format_inline(cell), st))
        formatted.append(frow)
    num_cols = len(formatted[0])
    available = A4[0] - 40 * mm
    if num_cols == 2:
        col_widths = [available * 0.32, available * 0.68]
    elif num_cols == 3:
        col_widths = [available * 0.16, available * 0.30, available * 0.54]
    else:
        col_widths = [available / num_cols] * num_cols
    table = Table(formatted, colWidths=col_widths, repeatRows=1)
    table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), TABLE_HEADER_BG),
        ('GRID', (0, 0), (-1, -1), 0.5, HexColor('#cccccc')),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('TOPPADDING', (0, 0), (-1, -1), 4),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
        ('LEFTPADDING', (0, 0), (-1, -1), 6),
        ('RIGHTPADDING', (0, 0), (-1, -1), 6),
    ]))
    return table


def make_toc():
    """A clickable Table of Contents flowable (chapters / H1 only)."""
    toc = TableOfContents()
    toc.levelStyles = [ParagraphStyle(
        'TOCH1', fontName=BOLD, fontSize=11.5, leading=24, textColor=H1_COLOR,
        leftIndent=16, firstLineIndent=-16, spaceAfter=2)]
    return toc


class ReportDoc(SimpleDocTemplate):
    """Adds PDF bookmarks + notifies the TOC for every H1 chapter heading."""
    def beforeDocument(self):
        self._sec = 0  # reset per build pass so TOC keys are stable → multiBuild converges

    def afterFlowable(self, flowable):
        if flowable.__class__.__name__ == 'Paragraph':
            st = getattr(flowable, 'style', None)
            if st is not None and st.name == 'H1':
                txt = flowable.getPlainText()
                self._sec = getattr(self, '_sec', 0) + 1
                key = 'sec%d' % self._sec
                self.canv.bookmarkPage(key)
                self.canv.addOutlineEntry(txt, key, level=0, closed=False)
                self.notify('TOCEntry', (0, txt, self.page, key))


def _draw_page_number(canvas, doc):
    """頁腳頁碼:封面(第 1 頁)不放,其餘置中。採 canvas 實際頁次,與可點目錄的頁碼一致。"""
    page = canvas.getPageNumber()
    if page <= 1:
        return
    canvas.saveState()
    canvas.setFont(BASE, 9)
    canvas.setFillColor(HexColor('#888888'))
    canvas.drawCentredString(A4[0] / 2.0, 11 * mm, str(page))
    canvas.restoreState()


_INFO_PREFIXES = ('**課程**', '**組員**', '**日期**', '**Course**', '**Team**', '**Date**')


def parse_markdown(md_text, base_dir='.'):
    story = []
    lines = md_text.split('\n')
    i = 0
    title_done = False
    expect_subtitle = False

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        if not stripped:
            i += 1
            continue

        if stripped == '<!-- pagebreak -->':
            story.append(PageBreak())
            i += 1
            continue
        if stripped == '[TOC]':
            story.append(make_toc())
            i += 1
            continue

        m = re.match(r'^(#{1,6})\s+(.*)', stripped)
        if m:
            level, text = len(m.group(1)), m.group(2)
            if level == 1 and not title_done:
                story.append(Paragraph(sanitize(text), styles['title']))
                title_done = True
                expect_subtitle = True
                i += 1
                continue
            if level == 3 and expect_subtitle:
                story.append(Paragraph(sanitize(text), styles['subtitle']))
                expect_subtitle = False
                i += 1
                continue
            expect_subtitle = False
            key = {1: 'h1', 2: 'h2'}.get(level, 'h3')
            story.append(Paragraph(format_inline(text), styles[key]))
            i += 1
            continue
        expect_subtitle = False

        if stripped.startswith(_INFO_PREFIXES):
            story.append(Paragraph(format_inline(stripped), styles['info']))
            i += 1
            continue

        low = stripped.lower()
        if ('demo' in low or '影片' in stripped or 'video' in low) and '](' in stripped:
            story.append(Paragraph(format_inline(stripped), styles['link']))
            i += 1
            continue

        if stripped == '---':
            story.append(Spacer(1, 3))
            story.append(HRFlowable(width="100%", thickness=0.5, color=HexColor('#cccccc')))
            story.append(Spacer(1, 3))
            i += 1
            continue

        if '🟢' in stripped or stripped.startswith('> **Key Takeaway'):
            t = stripped[2:] if stripped.startswith('> ') else stripped
            story.append(Paragraph(format_inline(t), styles['takeaway']))
            i += 1
            continue

        img_m = re.match(r'^!\[([^\]]*)\]\(([^)]+)\)(?:\{w=(\d+)\})?\s*$', stripped)
        if img_m:
            wmm = int(img_m.group(3)) if img_m.group(3) else None
            for fl in build_image(img_m.group(2), img_m.group(1), base_dir, wmm):
                story.append(fl)
            i += 1
            continue

        if stripped.startswith('```'):
            code_lines = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith('```'):
                code_lines.append(lines[i])
                i += 1
            i += 1
            story.append(build_code(code_lines))
            continue

        if stripped.startswith('> '):
            story.append(Paragraph(format_inline(stripped[2:]), styles['quote']))
            i += 1
            continue

        if stripped.startswith('|'):
            table_lines = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                table_lines.append(lines[i])
                i += 1
            tbl = build_table(parse_table(table_lines))
            if tbl:
                story.append(tbl)
                story.append(Spacer(1, 6))
            continue

        bm = re.match(r'^(\s*)-\s+(.*)', line)
        if bm:
            indent = len(bm.group(1))
            st = ParagraphStyle('B', parent=styles['bullet'],
                                leftIndent=20 + indent, bulletIndent=8 + indent)
            story.append(Paragraph(format_inline(bm.group(2)), st, bulletText='•'))
            i += 1
            continue

        nm = re.match(r'^(\s*)(\d+)\.\s+(.*)', line)
        if nm:
            indent = len(nm.group(1))
            st = ParagraphStyle('N', parent=styles['bullet'],
                                leftIndent=20 + indent, bulletIndent=8 + indent)
            story.append(Paragraph(format_inline(nm.group(3)), st, bulletText=f'{nm.group(2)}.'))
            i += 1
            continue

        story.append(Paragraph(format_inline(stripped), styles['body']))
        i += 1

    return story


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    md_path, pdf_path = sys.argv[1], sys.argv[2]
    title = sys.argv[3] if len(sys.argv) > 3 else 'NTUT AR Campus Tour — Final Report'
    with open(md_path, 'r', encoding='utf-8') as f:
        md_text = f.read()
    # 微軟正黑 (MSJH.ttf) 缺 U+2212 數學減號字形 → 統一換成 ASCII 連字號(外觀相同、全字型皆有)
    md_text = md_text.replace('−', '-')
    doc = ReportDoc(
        pdf_path, pagesize=A4,
        topMargin=25 * mm, bottomMargin=20 * mm,
        leftMargin=20 * mm, rightMargin=20 * mm,
        title=title, author='NTUT Group 4',
    )
    story = parse_markdown(md_text, base_dir=os.path.dirname(os.path.abspath(md_path)))
    doc.multiBuild(story, onFirstPage=_draw_page_number, onLaterPages=_draw_page_number)
    print(f'PDF generated: {pdf_path}  ({os.path.getsize(pdf_path)//1024} KB)')


if __name__ == '__main__':
    main()
