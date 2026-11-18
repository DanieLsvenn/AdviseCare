import random, os, sys, json
sys.path.insert(0, os.path.dirname(__file__))
from make_transcripts import build_pdf

SE = [  # (term, code, name, prereq) - the BIT_SE_NET curriculum of the sample transcript
 (0,"OTP101","Orientation and General Training Program","-"),(0,"TRS601","English 6 (University success)","-"),
 (0,"VOV114","Vovinam 1/3","-"),(0,"VOV124","Vovinam 2/3","VOV114"),(0,"VOV134","Vovinam 3/3","VOV124"),
 (1,"CEA201","Computer Organization and Architecture","-"),(1,"CSI104","Introduction to Computer Science","-"),
 (1,"MAE101","Mathematics for Engineering","-"),(1,"PRF192","Programming Fundamentals","-"),(1,"SSL101c","Academic Skills for University Success","-"),
 (2,"MAD101","Discrete mathematics","-"),(2,"NWC203c","Computer Networking","-"),(2,"OSG202","Operating Systems","-"),
 (2,"PRO192","Object-Oriented Programming","PRF192"),(2,"SSG104","Communication and In-Group Working Skills","-"),
 (3,"CSD201","Data Structures and Algorithms","PRO192"),(3,"DBI202","Introduction to Databases","-"),(3,"JPD113","Elementary Japanese 1- A1.1","-"),
 (3,"LAB211","OOP with Java Lab","PRO192"),(3,"WED201c","Web Design","-"),
 (4,"IOT102","Internet of Things","-"),(4,"JPD123","Elementary Japanese 1-A1.2","JPD113"),(4,"MAS291","Statistics & Probability","MAC101 / MAE101"),
 (4,"PRJ301","Java Web Application Development","DBI202, PRO192"),(4,"SWE201c","Introduction to Software Engineering","-"),
 (5,"ITE302c","Ethics in IT","-"),(5,"PRN212","Basic Cross-Platform Application Programming With .NET","DBI202, PRO192"),
 (5,"SWP391","Software development project","LAB211, PRJ301, SWE201c / LAB211, PRJ301, SWE202c"),
 (5,"SWR302","Software Requirement","SWE102 / SWE201c / SWE202c"),(5,"SWT301","Software Testing","SWE102 / SWE201c / SWE202c"),
 (6,"ENW492c","Academic Writing Skills","-"),(6,"OJT202","On-The-Job Training","-"),
 (7,"EXE101","Experiential Entrepreneurship 1","-"),(7,"PMG201c","Project Management","-"),
 (7,"PRN222","Advanced Cross-Platform Application Programming With .NET","PRN212"),(7,"PRU212","C# Programming and Unity","-"),
 (7,"SWD392","Software Architecture and Design","PRO192, SWE201c / PRO192, SWE202c"),
 (8,"EXE201","Experiential Entrepreneurship 2","-"),(8,"MLN111","Philosophy of Marxism – Leninism","-"),(8,"MLN122","Political economics of Marxism – Leninism","-"),
 (8,"PRM392","Mobile Programming","-"),(8,"PRN232","Xây dựng ứng dụng back-end với .NET","PRN221 / PRN222"),(8,"WDU203c","UI/UX Design","-"),
 (9,"HCM202","HCM Ideology","MLN111, MLN122"),(9,"MLN131","Scientific socialism","MLN111, MLN122"),(9,"SEP490","SE Capstone Project","-"),
 (9,"VNR202","History of CPV","MLN111, MLN122"),
]
BA = [
 (0,"OTP101","Orientation and General Training Program","-"),(0,"TRS601","English 6 (University success)","-"),
 (0,"VOV114","Vovinam 1/3","-"),(0,"VOV124","Vovinam 2/3","VOV114"),(0,"VOV134","Vovinam 3/3","VOV124"),
 (1,"ECO111","Microeconomics","-"),(1,"MGT103","Introduction to Management","-"),(1,"MKT101","Marketing Principles","-"),
 (1,"OBE102c","Organizational Behavior","-"),(1,"SSL101c","Academic Skills for University Success","-"),
 (2,"ACC101","Principles of Accounting","-"),(2,"ECO121","Macroeconomics","ECO111"),(2,"MAS202","Applied Statistics for Business","-"),
 (2,"SSG104","Communication and In-Group Working Skills","-"),
 (3,"FIN202","Corporate Finance","ACC101"),(3,"HRM202c","Human Resource Management","MGT103"),(3,"LAW102","Business Law and Ethics Fundamentals","-"),
 (3,"MKT201","Consumer Behavior","MKT101"),(3,"JPD113","Elementary Japanese 1- A1.1","-"),
 (4,"FIN303","Investments","FIN202"),(4,"MKT301","Digital Marketing","MKT101"),(4,"OPM201","Operations Management","MGT103"),(4,"JPD123","Elementary Japanese 1-A1.2","JPD113"),
 (5,"ENW492c","Academic Writing Skills","-"),(5,"OJT202","On-The-Job Training","-"),
 (6,"EXE101","Experiential Entrepreneurship 1","-"),(6,"MLN111","Philosophy of Marxism – Leninism","-"),(6,"MLN122","Political economics of Marxism – Leninism","-"),
 (6,"PMG201c","Project Management","-"),
 (7,"EXE201","Experiential Entrepreneurship 2","-"),(7,"HCM202","HCM Ideology","MLN111, MLN122"),(7,"IBC201","International Business","ECO121"),
 (7,"MLN131","Scientific socialism","MLN111, MLN122"),(7,"VNR202","History of CPV","MLN111, MLN122"),
 (8,"GRF497","Business Administration Graduation Thesis","-"),
]
SEASONS = ["Spring", "Summer", "Fall"]
def semester(start_year, idx):  # idx 0 = Fall of start_year
    n = 2 + idx  # Fall = position 2 of start_year
    return f"{SEASONS[n % 3]}{start_year + n // 3}"
def index_of(start_year, season, year): return (year - start_year) * 3 + SEASONS.index(season) - 2

def make_student(st, rnd):
    code = st["code"]; is_se = code.startswith("SE"); cur_list = SE if is_se else BA
    last_term = max(t for t, *_ in cur_list)
    start_year = 2004 + int(code[2:4])  # K18 -> started Fall 2022
    current_idx = index_of(start_year, "Fall", 2026)
    if st["status"] == "graduated": current_idx = index_of(start_year, "Spring", 2026) + 1  # finished the term before
    idx_of_term = lambda k: round(k * (current_idx if st["status"] != "graduated" else current_idx - 1) / last_term)
    ability = rnd.uniform(5.6, 8.9); presence = rnd.uniform(86, 97)
    withdrawn_after = rnd.randint(3, 5) if st["status"] == "withdrawn" else None
    grade_rows, att_rows = [], []
    for term, scode, name, prereq in cur_list:
        idx = idx_of_term(term)
        stop = (withdrawn_after is not None and term > withdrawn_after) or (st["status"] == "onLeave" and idx >= current_idx)
        sem = "" if stop else semester(start_year, min(idx, current_idx))
        current = not stop and min(idx, current_idx) >= current_idx and st["status"] != "graduated"
        credits = 0 if scode in ("OTP101", "TRS601") else 10 if scode in ("OJT202", "SEP490", "GRF497") else 2 if scode.startswith(("VOV", "HCM", "MLN12", "MLN13", "VNR")) else 3
        base = dict(term=term, semester=sem, code=scode, name=name, credits=credits, prereq=prereq, replacement="-")
        if stop:
            grade_rows.append({**base, "value": "-", "status": "Not started"}); att_rows.append({**base, "value": "-", "status": "Not started"}); continue
        if scode == "TRS601":
            grade_rows.append({**base, "value": "-", "status": "Exempt"}); att_rows.append({**base, "value": "-", "status": "Exempt"}); continue
        att = max(45.0, min(100.0, rnd.gauss(presence, 6)))
        if current:
            att = max(55.0, min(100.0, rnd.gauss(presence - 2, 9)))
            grade_rows.append({**base, "value": "-", "status": "Studying"}); att_rows.append({**base, "value": f"{att:.0f}", "status": "Studying"}); continue
        grade = max(0.0, min(10.0, rnd.gauss(ability, 1.1)))
        if att < 80 and rnd.random() < 0.35:
            status, gtext = "Attendance fail", "-"
        elif grade < 5:
            status, gtext = "Not passed", f"{grade:.1f}"
        else:
            status, gtext = "Passed", (f"{grade:.1f}" if scode != "LAB211" else "-")
        grade_rows.append({**base, "value": gtext, "status": status}); att_rows.append({**base, "value": f"{att:.0f}", "status": status})
    outside_g, outside_a = [], []
    if rnd.random() < 0.25 and st["status"] in ("enrolled", "graduated"):
        sem = semester(start_year, max(1, current_idx - 2)); g = rnd.uniform(6.5, 9.2); a = rnd.uniform(80, 100)
        o = dict(semester=sem, code="TMI101", name="Traditional musical instrument", credits=3)
        outside_g.append({**o, "value": f"{g:.1f}", "status": "Passed"}); outside_a.append({**o, "value": f"{a:.0f}", "status": "Passed"})
    curriculum = ("BIT_SE_NET_" if is_se else "BBA_MKT_") + code[2:4] + "A"
    return curriculum, grade_rows, att_rows, outside_g, outside_a

def main(out_dir, students_json):
    students = json.load(open(students_json, encoding="utf-8"))
    rnd = random.Random(20261118)
    os.makedirs(os.path.join(out_dir, "grade"), exist_ok=True); os.makedirs(os.path.join(out_dir, "attendance"), exist_ok=True)
    for st in students:
        curriculum, g, a, og, oa = make_student(st, rnd)
        if st["code"] == "SE180001":  # student@gmail.com: low attendance this term (matches its Attendance rate 72 story)
            for r in a:
                if r["semester"] == "Fall2026" and r["code"] == "SEP490": r["value"] = "72"
                if r["semester"] == "Fall2026" and r["code"] == "HCM202": r["value"] = "68"
        build_pdf(os.path.join(out_dir, "grade", f'Transcript_{st["code"]}.pdf'), "grade", st, curriculum, g, og)
        build_pdf(os.path.join(out_dir, "attendance", f'Attendance_{st["code"]}.pdf'), "attendance", st, curriculum, a, oa)
    # Unhappy cases
    bad = os.path.join(out_dir, "invalid"); os.makedirs(bad, exist_ok=True)
    ghost = {"code": "SE999999", "name": "Nguyễn Văn Không Có"}
    c, g, a, og, oa = make_student({"code": "SE189999", "status": "enrolled"}, rnd)
    build_pdf(os.path.join(bad, "Transcript_SE999999_unknown_student.pdf"), "grade", ghost, c, g, og)
    st = students[2]
    c, g, a, og, oa = make_student(st, rnd)
    g[6]["value"] = "11.5"; g[9]["status"] = "Passsed"; g[12]["semester"] = "Autumn2023"
    build_pdf(os.path.join(bad, f'Transcript_{st["code"]}_bad_values.pdf'), "grade", st, c, g, og)
    a[7]["value"] = "105"
    build_pdf(os.path.join(bad, f'Attendance_{st["code"]}_bad_values.pdf'), "attendance", st, c, a, oa)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
