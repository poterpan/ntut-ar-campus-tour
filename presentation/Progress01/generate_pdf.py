#!/usr/bin/env python3
"""Generate PDF report from YOLO_Paper_And_Implementation_Report_EN.md following CLAUDE.md specs."""

import re
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib.colors import HexColor, Color, white, black
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    Preformatted, KeepTogether, HRFlowable, Image
)
from PIL import Image as PILImage
import os
import hashlib
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from reportlab.lib.fonts import addMapping
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfbase.cidfonts import UnicodeCIDFont

# Register CID font for Chinese
pdfmetrics.registerFont(UnicodeCIDFont('STSong-Light'))

# Colors from CLAUDE.md
NAVY = HexColor('#1a1a2e')
H1_COLOR = HexColor('#16213e')
H2_COLOR = HexColor('#0f3460')
CODE_BG = HexColor('#f5f5f5')
TABLE_HEADER_BG = HexColor('#e8eaf6')
BODY_COLOR = HexColor('#333333')
KEY_TAKEAWAY_GREEN = HexColor('#2e7d32')
LINK_COLOR = HexColor('#1565c0')
BLOCKQUOTE_BG = HexColor('#fff3e0')

# Styles
styles = {
    'title': ParagraphStyle(
        'Title', fontSize=22, leading=28, textColor=NAVY,
        alignment=TA_CENTER, spaceAfter=6, fontName='Helvetica-Bold'
    ),
    'student_info': ParagraphStyle(
        'StudentInfo', fontSize=10, leading=14, textColor=BODY_COLOR,
        alignment=TA_CENTER, spaceAfter=4, fontName='Helvetica',
        wordWrap='CJK'
    ),
    'h1': ParagraphStyle(
        'H1', fontSize=16, leading=22, textColor=H1_COLOR,
        spaceBefore=16, spaceAfter=8, fontName='Helvetica-Bold'
    ),
    'h2': ParagraphStyle(
        'H2', fontSize=13, leading=18, textColor=H2_COLOR,
        spaceBefore=12, spaceAfter=6, fontName='Helvetica-Bold'
    ),
    'body': ParagraphStyle(
        'Body', fontSize=10, leading=15, textColor=BODY_COLOR,
        spaceAfter=6, fontName='Helvetica'
    ),
    'body_bold': ParagraphStyle(
        'BodyBold', fontSize=10, leading=15, textColor=BODY_COLOR,
        spaceAfter=6, fontName='Helvetica-Bold'
    ),
    'bullet': ParagraphStyle(
        'Bullet', fontSize=10, leading=15, textColor=BODY_COLOR,
        leftIndent=20, bulletIndent=10, spaceAfter=3, fontName='Helvetica',
        bulletFontName='Helvetica', bulletFontSize=10
    ),
    'code': ParagraphStyle(
        'Code', fontSize=8.5, leading=12, textColor=black,
        fontName='Courier', backColor=CODE_BG, borderPadding=6,
        leftIndent=10, rightIndent=10, spaceAfter=8
    ),
    'key_takeaway': ParagraphStyle(
        'KeyTakeaway', fontSize=10, leading=15, textColor=KEY_TAKEAWAY_GREEN,
        spaceAfter=6, fontName='Helvetica-Bold'
    ),
    'link': ParagraphStyle(
        'Link', fontSize=10, leading=15, textColor=LINK_COLOR,
        alignment=TA_CENTER, spaceAfter=6, fontName='Helvetica'
    ),
    'blockquote': ParagraphStyle(
        'Blockquote', fontSize=9.5, leading=14, textColor=HexColor('#e65100'),
        leftIndent=15, spaceAfter=8, fontName='Helvetica-Oblique',
        backColor=BLOCKQUOTE_BG, borderPadding=6
    ),
    'caption': ParagraphStyle(
        'Caption', fontSize=8.5, leading=11, textColor=HexColor('#666'),
        alignment=TA_CENTER, spaceAfter=10, spaceBefore=2,
        fontName='Helvetica-Oblique'
    ),
}


def _format_caption(alt_text):
    return re.sub(r'([\u4e00-\u9fff\u3001-\u303f\uff00-\uffef]+)',
                  r'<font face="STSong-Light">\1</font>', alt_text)


def render_math_block(latex_src, base_dir):
    """Render a LaTeX display-math block to PNG via matplotlib mathtext, return Image flowable list."""
    cache_dir = os.path.join(base_dir, 'images', '_math')
    os.makedirs(cache_dir, exist_ok=True)
    digest = hashlib.md5(latex_src.encode('utf-8')).hexdigest()[:12]
    out_path = os.path.join(cache_dir, f'eq_{digest}.png')
    if not os.path.isfile(out_path):
        # mathtext does not support \text{} or \begin{aligned}; preprocess.
        src = latex_src
        src = re.sub(r'\\begin\{aligned\}|\\end\{aligned\}', '', src)
        src = re.sub(r'\\text\{([^}]*)\}', r'\\mathrm{\1}', src)
        src = src.replace('&', '')
        # Split into lines on \\ so we can stack them vertically.
        raw_lines = [ln.strip() for ln in re.split(r'\\\\', src) if ln.strip()]
        if not raw_lines:
            raw_lines = [src.strip()]
        # Render each line into its own figure, then stack via PIL.
        line_imgs = []
        for ln in raw_lines:
            fig = plt.figure(figsize=(0.01, 0.01))
            try:
                fig.text(0, 0, f'${ln}$', fontsize=14)
                tmp_path = out_path + f'.line{len(line_imgs)}.png'
                fig.savefig(tmp_path, dpi=300, bbox_inches='tight', pad_inches=0.05,
                            transparent=False, facecolor='white')
                line_imgs.append(tmp_path)
            finally:
                plt.close(fig)
        # Stack vertically with small gaps and centre-align horizontally.
        pil_imgs = [PILImage.open(p) for p in line_imgs]
        max_w = max(im.width for im in pil_imgs)
        gap = 12
        total_h = sum(im.height for im in pil_imgs) + gap * (len(pil_imgs) - 1)
        canvas = PILImage.new('RGB', (max_w, total_h), 'white')
        y = 0
        for im in pil_imgs:
            x = (max_w - im.width) // 2
            canvas.paste(im, (x, y))
            y += im.height + gap
        canvas.save(out_path)
        for p in line_imgs:
            try: os.remove(p)
            except OSError: pass
    pil = PILImage.open(out_path)
    src_w, src_h = pil.size
    aspect = src_h / src_w
    max_w = (A4[0] - 40 * mm) * 0.85  # leave breathing room
    target_w = max_w
    target_h = target_w * aspect
    max_h = 60 * mm
    if target_h > max_h:
        target_h = max_h
        target_w = target_h / aspect
    return [
        Spacer(1, 4),
        Image(out_path, width=target_w, height=target_h, hAlign='CENTER'),
        Spacer(1, 4),
    ]


def make_image_flowable(img_path, alt_text, base_dir):
    """Build an Image + caption pair sized to fit page width."""
    full_path = img_path if os.path.isabs(img_path) else os.path.join(base_dir, img_path)
    if not os.path.isfile(full_path):
        return [Paragraph(f'<i>[image missing: {img_path}]</i>', styles['caption'])]
    pil = PILImage.open(full_path)
    src_w, src_h = pil.size
    aspect = src_h / src_w
    max_w = A4[0] - 40 * mm
    max_h = 110 * mm
    target_w = max_w
    target_h = target_w * aspect
    if target_h > max_h:
        target_h = max_h
        target_w = target_h / aspect
    return [
        Spacer(1, 4),
        Image(full_path, width=target_w, height=target_h),
        Paragraph(_format_caption(alt_text), styles['caption']),
    ]


def make_image_pair_flowable(items, base_dir):
    """Place two images side-by-side in a 2-col table with captions below each."""
    cells = []
    available = A4[0] - 40 * mm
    col_w = (available - 6 * mm) / 2  # gap between cols
    cap_style = ParagraphStyle('PairCap', parent=styles['caption'], spaceBefore=2, spaceAfter=0)
    img_cells = []
    cap_cells = []
    for path, alt in items:
        full_path = path if os.path.isabs(path) else os.path.join(base_dir, path)
        if not os.path.isfile(full_path):
            img_cells.append(Paragraph(f'<i>[missing: {path}]</i>', styles['caption']))
            cap_cells.append(Paragraph('', cap_style))
            continue
        pil = PILImage.open(full_path)
        src_w, src_h = pil.size
        aspect = src_h / src_w
        target_w = col_w
        target_h = target_w * aspect
        max_h = 130 * mm
        if target_h > max_h:
            target_h = max_h
            target_w = target_h / aspect
        img_cells.append(Image(full_path, width=target_w, height=target_h))
        cap_cells.append(Paragraph(_format_caption(alt), cap_style))
    table = Table([img_cells, cap_cells], colWidths=[col_w, col_w])
    table.setStyle(TableStyle([
        ('VALIGN', (0, 0), (-1, 0), 'BOTTOM'),
        ('VALIGN', (0, 1), (-1, 1), 'TOP'),
        ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
        ('TOPPADDING', (0, 0), (-1, -1), 0),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 0),
    ]))
    return [Spacer(1, 4), table, Spacer(1, 8)]


def escape_xml(text):
    """Escape XML special characters for Preformatted code blocks."""
    text = text.replace('&', '&amp;')
    # Keep < and > as-is in code blocks (Preformatted handles them)
    return text


CJK_PATTERN = re.compile(r'([一-鿿、-〿＀-￯]+)')


def wrap_cjk(text):
    """Wrap CJK characters in STSong-Light font tags so PDF renders Chinese."""
    return CJK_PATTERN.sub(r'<font face="STSong-Light">\1</font>', text)


def format_inline(text):
    """Convert inline markdown to ReportLab XML."""
    # Bold + code: **`text`**
    text = re.sub(r'\*\*`([^`]+)`\*\*', r'<b><font face="Courier" size="9">\1</font></b>', text)
    # Bold
    text = re.sub(r'\*\*([^*]+)\*\*', r'<b>\1</b>', text)
    # Inline code
    text = re.sub(r'`([^`]+)`', r'<font face="Courier" size="9" color="#c62828">\1</font>', text)
    # Links
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'<a href="\2" color="#1565c0">\1</a>', text)
    # CJK font wrapping(放最後,避免破壞上面的 XML tags)
    text = wrap_cjk(text)
    return text


def parse_table(lines):
    """Parse markdown table lines into a list of rows."""
    rows = []
    for line in lines:
        line = line.strip()
        if line.startswith('|') and not re.match(r'^\|[\s\-:|]+\|$', line):
            cells = [c.strip() for c in line.split('|')[1:-1]]
            rows.append(cells)
    return rows


def build_table(rows):
    """Build a ReportLab Table from parsed rows."""
    if not rows:
        return None

    # Format cells
    formatted = []
    for i, row in enumerate(rows):
        frow = []
        for cell in row:
            cell_text = format_inline(cell)
            if i == 0:
                style = ParagraphStyle('TH', fontSize=9, leading=12,
                                       fontName='Helvetica-Bold', textColor=H1_COLOR)
            else:
                style = ParagraphStyle('TD', fontSize=9, leading=12,
                                       fontName='Helvetica', textColor=BODY_COLOR)
            frow.append(Paragraph(cell_text, style))
        formatted.append(frow)

    # Calculate column widths
    num_cols = len(formatted[0]) if formatted else 0
    available = A4[0] - 40 * mm
    if num_cols == 2:
        col_widths = [available * 0.25, available * 0.75]
    elif num_cols == 3:
        col_widths = [available * 0.28, available * 0.22, available * 0.50]
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


def parse_markdown(md_text, base_dir='.'):
    """Parse markdown into ReportLab flowables."""
    story = []
    lines = md_text.split('\n')
    i = 0

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        # Skip empty lines
        if not stripped:
            i += 1
            continue

        # Title (# )
        if stripped.startswith('# ') and not stripped.startswith('## '):
            title = stripped[2:]
            story.append(Paragraph(wrap_cjk(title), styles['title']))
            i += 1
            continue

        # Student / team info lines(置中)
        if stripped.startswith(('**Name:**', '**Student ID:**', '**Program:**',
                                '**Team:**', '**Members:**', '**Date:**')):
            text = format_inline(stripped)
            # Wrap Chinese characters in STSong-Light font
            text = re.sub(r'([\u4e00-\u9fff\u3001-\u303f\uff00-\uffef]+)',
                         r'<font face="STSong-Light">\1</font>', text)
            story.append(Paragraph(text, styles['student_info']))
            i += 1
            continue

        # H2 (## )
        if stripped.startswith('## ') and not stripped.startswith('### '):
            text = stripped[3:]
            story.append(Spacer(1, 8))
            story.append(Paragraph(wrap_cjk(text), styles['h1']))
            i += 1
            continue

        # H3 (### )
        if stripped.startswith('### '):
            text = stripped[4:]
            story.append(Paragraph(wrap_cjk(text), styles['h2']))
            i += 1
            continue

        # Horizontal rule
        if stripped == '---':
            story.append(Spacer(1, 4))
            story.append(HRFlowable(width="100%", thickness=0.5, color=HexColor('#cccccc')))
            story.append(Spacer(1, 4))
            i += 1
            continue

        # Display math block ($$...$$)
        if stripped.startswith('$$'):
            math_lines = []
            # consume the opening $$
            after_open = stripped[2:].strip()
            if after_open.endswith('$$') and len(after_open) > 2:
                # Single-line $$...$$
                math_lines.append(after_open[:-2])
                i += 1
            else:
                if after_open:
                    math_lines.append(after_open)
                i += 1
                while i < len(lines):
                    line_stripped = lines[i].strip()
                    if line_stripped.endswith('$$'):
                        before_close = line_stripped[:-2].strip()
                        if before_close:
                            math_lines.append(before_close)
                        i += 1
                        break
                    math_lines.append(lines[i])
                    i += 1
            latex_src = '\n'.join(math_lines).strip()
            for fl in render_math_block(latex_src, base_dir):
                story.append(fl)
            continue

        # Code block
        if stripped.startswith('```'):
            code_lines = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith('```'):
                code_lines.append(lines[i])
                i += 1
            i += 1  # skip closing ```
            code_text = escape_xml('\n'.join(code_lines))
            story.append(Preformatted(code_text, styles['code']))
            continue

        # Blockquote
        if stripped.startswith('> '):
            quote_text = format_inline(stripped[2:])
            story.append(Paragraph(quote_text, styles['blockquote']))
            i += 1
            continue

        # Table
        if stripped.startswith('|'):
            table_lines = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                table_lines.append(lines[i])
                i += 1
            rows = parse_table(table_lines)
            if rows:
                tbl = build_table(rows)
                if tbl:
                    story.append(tbl)
                    story.append(Spacer(1, 6))
            continue

        # Key Takeaway
        if stripped.startswith('**Key Takeaway:**'):
            text = format_inline(stripped)
            story.append(Paragraph(text, styles['key_takeaway']))
            i += 1
            continue

        # Bullet list
        if stripped.startswith('- '):
            text = format_inline(stripped[2:])
            story.append(Paragraph(text, styles['bullet'], bulletText='\u2022'))
            i += 1
            continue

        # Numbered list
        num_match = re.match(r'^(\d+)\.\s+(.*)', stripped)
        if num_match:
            num, text = num_match.groups()
            text = format_inline(text)
            story.append(Paragraph(text, styles['bullet'], bulletText=f'{num}.'))
            i += 1
            continue

        # Image: ![alt](path) — collect adjacent image lines for side-by-side rendering
        img_match = re.match(r'^!\[([^\]]*)\]\(([^)]+)\)\s*$', stripped)
        if img_match:
            pair = [(img_match.group(2), img_match.group(1))]
            j = i + 1
            # Skip blank lines and check next non-blank
            while j < len(lines) and not lines[j].strip():
                j += 1
            if j < len(lines):
                next_match = re.match(r'^!\[([^\]]*)\]\(([^)]+)\)\s*$', lines[j].strip())
                if next_match:
                    pair.append((next_match.group(2), next_match.group(1)))
            if len(pair) == 2:
                for fl in make_image_pair_flowable(pair, base_dir):
                    story.append(fl)
                i = j + 1
            else:
                for fl in make_image_flowable(pair[0][0], pair[0][1], base_dir):
                    story.append(fl)
                i += 1
            continue

        # Demo Video link
        link_match = re.match(r'\[([^\]]+)\]\(([^)]+)\)', stripped)
        if link_match and 'video' in stripped.lower():
            label, url = link_match.groups()
            story.append(Paragraph(f'<a href="{url}" color="#1565c0">{label}</a>', styles['link']))
            i += 1
            continue

        # Regular paragraph
        text = format_inline(stripped)
        story.append(Paragraph(text, styles['body']))
        i += 1

    return story


def main():
    import os
    base = os.path.dirname(os.path.abspath(__file__))
    md_path = os.path.join(base, 'Progress01.md')
    pdf_path = os.path.join(base, 'Progress01.pdf')

    with open(md_path, 'r', encoding='utf-8') as f:
        md_text = f.read()

    doc = SimpleDocTemplate(
        pdf_path,
        pagesize=A4,
        topMargin=25 * mm,
        bottomMargin=20 * mm,
        leftMargin=20 * mm,
        rightMargin=20 * mm,
        title='AR Campus Tour — Progress Report 01',
        author='NTUT Group 4'
    )

    story = parse_markdown(md_text, base_dir=base)
    doc.build(story)
    print(f'PDF generated: {pdf_path}')


if __name__ == '__main__':
    main()
