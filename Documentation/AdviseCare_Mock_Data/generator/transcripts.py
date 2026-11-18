# Reads the text/tables extracted from the transcript PDFs and applies the same checks as AcademicResultService.BuildRows
import json, re

STATUSES = {"passed": "passed", "pass": "passed", "notpassed": "notPassed", "failed": "notPassed", "fail": "notPassed",
            "attendancefail": "attendanceFail", "exempt": "exempt", "exempted": "exempt", "studying": "studying",
            "notstarted": "notStarted"}
SEM = re.compile(r"^(spring|summer|fall)\s*(\d{4})$", re.I)


def term_name_of(sem):
    m = SEM.match(sem.strip())
    if not m: return None
    y = int(m.group(2))
    if y < 1990 or y > 2100: return None
    s = m.group(1).lower()
    return f"{s[0].upper()}{s[1:]} {y}"


def letters(t): return "".join(ch for ch in t.lower() if ch.isalpha())


def parse_file(rec):
    text = rec["pages"][0]["text"]
    kind = "grade" if text.startswith("ACADEMIC TRANSCRIPT") else "attendance" if text.startswith("ATTENDANCE TRANSCRIPT") else ""
    m = re.search(r"Full name:\s*(.*?)\s+Student ID:\s*(\S+)\s+Curriculum:\s*(\S+)", text)
    full, code, curr = (m.group(1), m.group(2), m.group(3)) if m else ("", "", "")
    raw = []  # (inCurr, cells dict)
    section = None
    for pg in rec["pages"]:
        for tb in pg["tables"]:
            for row in tb:
                cells = [(c or "").strip() for c in row]
                if cells and cells[0] == "No.":
                    section = "in" if any(c.replace("\n", "") == "Term" for c in cells) else "out"
                    continue
                if not cells or not cells[0].strip().isdigit():
                    continue
                if section == "in":
                    if kind == "grade":   # No, Term, Semester, Code, Prereq, Replacement, Name, Credits, Grade, Status
                        no, term, sem, sc, _, _, name, _, val, st = cells[:10]
                    else:                 # No, Term, Semester, Code, Name, Attendance, Status
                        no, term, sem, sc, name, val, st = cells[:7]
                    raw.append(dict(no=int(no), inCurr=True, term=term, sem=sem, code=sc, name=name, val=val, st=st))
                elif section == "out":
                    if kind == "grade":   # No, Semester, Code, Name, Credits, Grade, Status
                        no, sem, sc, name, _, val, st = cells[:7]
                    else:
                        no, sem, sc, name, val, st = cells[:6]
                    raw.append(dict(no=int(no), inCurr=False, term="", sem=sem, code=sc, name=name, val=val, st=st))
    is_grade = kind == "grade"
    errors, warnings, rows, seen, skipped = [], [], [], set(), 0
    for r in raw:
        code_ = r["code"].replace("\n", "").strip()
        sem = r["sem"].replace("\n", "").strip()
        label = f"Row {r['no']}" + (f" ({code_})" if code_ and code_ != "-" else "")
        if sem == "" or sem == "-":
            skipped += 1; continue
        tn = term_name_of(sem)
        if tn is None:
            errors.append(f'{label}: semester "{sem}" is not like Fall2024.'); continue
        value = None
        vt = r["val"].replace("\n", "").strip()
        if vt and vt != "-":
            try:
                num = float(vt.rstrip("%").strip())
            except ValueError:
                errors.append(f'{label}: "{vt}" is not a number.'); continue
            if num < 0 or num > (10 if is_grade else 100):
                errors.append(f"{label}: grade {vt} is not between 0 and 10." if is_grade else f"{label}: attendance {vt} is not between 0 and 100."); continue
            value = vt.rstrip("%").strip()
        status = None
        stt = r["st"].replace("\n", " ").strip()
        if stt and stt != "-":
            if letters(stt) not in STATUSES:
                errors.append(f'{label}: status "{stt}" is not one of Passed, Not passed, Attendance fail, Exempt, Studying, Not started.'); continue
            status = STATUSES[letters(stt)]
        ct = None
        if r["term"] and r["term"] != "-":
            ct = int(r["term"])
        if (code_.lower(), tn) in seen:
            errors.append(f"{label}: {code_} in {tn} appears more than once."); continue
        seen.add((code_.lower(), tn))
        rows.append(dict(rowNumber=r["no"], isInCurriculum=r["inCurr"], curriculumTerm=ct, termName=tn, subjectCode=code_,
                         subjectName=" ".join(r["name"].split())[:255], value=value, status=status))
    if skipped:
        warnings.append(f"{skipped} row(s) have no semester yet (not started) and are left out.")
    if not rows and not errors:
        errors.append("No subject row with a semester was found in the file.")
    return dict(fileName=rec["fileName"], sha=rec["sha"], importType=kind, studentCode=code, fullName=full, curriculum=curr,
                rows=rows, errors=errors, warnings=warnings, skipped=skipped, path=rec["path"])


def load(path):
    return [parse_file(r) for r in json.load(open(path, encoding="utf-8"))]


if __name__ == "__main__":
    fs = load("/mnt/user-data/uploads/SEP/Claude outputs/_stage/transcripts_extract.json")
    for f in fs:
        if f["errors"] or "SE180001" in f["fileName"] or "BA160143" in f["fileName"]:
            print(f["fileName"], f["importType"], f["studentCode"], f["curriculum"], len(f["rows"]), f["errors"][:4], f["warnings"])
    print(sum(len(f["rows"]) for f in fs))
    print(fs[0]["rows"][:2])
    import collections
    print(collections.Counter(r["termName"] for f in fs for r in f["rows"]))
