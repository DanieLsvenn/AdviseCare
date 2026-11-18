# Step 1 of the full mock data: read the transcript PDFs (grade, attendance, invalid) into one JSON file for the generator.
# Usage: python extract_transcripts.py <AdviseCare_Mock_Data/Transcripts folder> <out.json>   (needs: pip install pdfplumber)
import sys, os, glob, json, hashlib
import pdfplumber

root, out = sys.argv[1], sys.argv[2]
records = []
for sub in ("grade", "attendance", "invalid"):
    for path in sorted(glob.glob(os.path.join(root, sub, "*.pdf"))):
        data = open(path, "rb").read()
        rec = {"path": f"{sub}/{os.path.basename(path)}", "fileName": os.path.basename(path),
               "sha": hashlib.sha256(data).hexdigest().upper(), "pages": []}
        with pdfplumber.open(path) as pdf:
            for page in pdf.pages:
                rec["pages"].append({"text": page.extract_text(), "tables": page.extract_tables()})
        records.append(rec)
json.dump(records, open(out, "w", encoding="utf-8"), ensure_ascii=False)
print(len(records), "files ->", out)
