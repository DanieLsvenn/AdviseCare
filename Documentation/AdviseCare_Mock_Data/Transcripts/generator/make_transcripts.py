"""Mock transcript PDFs for AdviseCare (grade files like the university's ACADEMIC TRANSCRIPT, and attendance files of the same layout)."""
import random, sys, os, json
from reportlab.lib.pagesizes import A4
from reportlab.platypus import SimpleDocTemplate, Paragraph, Table, TableStyle, Spacer
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

pdfmetrics.registerFont(TTFont("DV", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"))
pdfmetrics.registerFont(TTFont("DVB", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"))
CELL = ParagraphStyle("cell", fontName="DV", fontSize=7.5, leading=11)
HEAD = ParagraphStyle("head", fontName="DV", fontSize=7.5, leading=11)
TITLE = ParagraphStyle("title", fontName="DVB", fontSize=14, leading=18, alignment=1)
INFO = ParagraphStyle("info", fontName="DV", fontSize=8, leading=11, alignment=1)
SECTION = ParagraphStyle("section", fontName="DVB", fontSize=9, leading=12)
NOTE = ParagraphStyle("note", fontName="DV", fontSize=7.5, leading=11)

def esc(t): return str(t).replace("&", "&amp;").replace("<", "&lt;")

def build_pdf(path, kind, student, curriculum_code, rows, outside):
    """kind: grade | attendance. rows: dicts with term, semester, code, name, credits, prereq, replacement, value, status."""
    doc = SimpleDocTemplate(path, pagesize=A4, leftMargin=24, rightMargin=24, topMargin=26, bottomMargin=36,
                            title=("Academic transcript " if kind == "grade" else "Attendance transcript ") + student["code"])
    footer = f'{student["name"]} {student["code"]} | Page '
    def on_page(canvas, d):
        canvas.setFont("DV", 7.5)
        canvas.drawRightString(A4[0] - 24, 18, footer + str(d.page))
    story = [Paragraph("ACADEMIC TRANSCRIPT" if kind == "grade" else "ATTENDANCE TRANSCRIPT", TITLE), Spacer(1, 6),
             Paragraph(f'Full name: {esc(student["name"])}&nbsp;&nbsp; Student ID: {student["code"]}&nbsp;&nbsp; Curriculum: {curriculum_code}', INFO),
             Spacer(1, 10), Paragraph("CURRICULUM SUBJECTS", SECTION), Spacer(1, 6)]
    P = lambda t, s=CELL: Paragraph(esc(t), s)
    if kind == "grade":
        header = ["No.", "Term", "Semester", "Subject code", "Prerequisites", "Replacement", "Subject name", "Credits", "Grade", "Status"]
        widths = [27, 23, 54, 54, 54, 54, 142, 33, 33, 73]
        body = [[P(i + 1), P(r["term"]), P(r["semester"] or "-"), P(r["code"]), P(r.get("prereq", "-")), P(r.get("replacement", "-")),
                 P(r["name"]), P(r.get("credits", 3)), P(r["value"]), P(r["status"])] for i, r in enumerate(rows)]
    else:
        header = ["No.", "Term", "Semester", "Subject code", "Subject name", "Attendance (%)", "Status"]
        widths = [27, 23, 54, 54, 230, 87, 72]
        body = [[P(i + 1), P(r["term"]), P(r["semester"] or "-"), P(r["code"]), P(r["name"]), P(r["value"]), P(r["status"])]
                for i, r in enumerate(rows)]
    style = TableStyle([("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#B9C4DE")), ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#EEF1F7")),
                        ("VALIGN", (0, 0), (-1, -1), "TOP"), ("LEFTPADDING", (0, 0), (-1, -1), 5), ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                        ("TOPPADDING", (0, 0), (-1, -1), 6), ("BOTTOMPADDING", (0, 0), (-1, -1), 6)])
    story.append(Table([[P(h, HEAD) for h in header]] + body, colWidths=widths, repeatRows=1, style=style))
    story += [Spacer(1, 14), Paragraph("SUBJECTS OUTSIDE THE CURRICULUM", SECTION), Spacer(1, 6)]
    if kind == "grade":
        oheader, owidths = ["No.", "Semester", "Subject code", "Subject name", "Credits", "Grade", "Status"], [27, 60, 60, 245, 40, 40, 75]
        orows = [[P(i + 1), P(r["semester"]), P(r["code"]), P(r["name"]), P(r.get("credits", 3)), P(r["value"]), P(r["status"])] for i, r in enumerate(outside)]
    else:
        oheader, owidths = ["No.", "Semester", "Subject code", "Subject name", "Attendance (%)", "Status"], [27, 60, 60, 272, 93, 75]
        orows = [[P(i + 1), P(r["semester"]), P(r["code"]), P(r["name"]), P(r["value"]), P(r["status"])] for i, r in enumerate(outside)]
    story.append(Table([[P(h, HEAD) for h in oheader]] + orows, colWidths=owidths, repeatRows=1, style=style))
    if not outside:
        story += [Spacer(1, 4), Paragraph("No subjects", NOTE)]
    doc.build(story, onFirstPage=on_page, onLaterPages=on_page)
