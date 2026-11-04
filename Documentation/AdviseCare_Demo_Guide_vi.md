# AdviseCare — Hướng dẫn demo giao diện (Frontend) từ đầu đến cuối

> Mục đích: giúp bạn **demo lướt qua toàn bộ chức năng** của app trên giao diện web.
> Mỗi bước ghi rõ: **ai đăng nhập → vào đâu → nhập gì vào ô nào → kết quả mong đợi**.
>
> Nguồn: đã đối chiếu với giao diện thật (chạy tại máy, chỉ đọc), mã nguồn frontend/backend và hai file tài liệu workflow trong thư mục `Documentation`.
> Các chỗ **chưa chạy thử thật** được ghi chú rõ ở mục "Lưu ý & rủi ro khi demo" (mục 10).

---

## Mục lục

1. [Chuẩn bị trước khi demo](#1-chuẩn-bị-trước-khi-demo)
2. [Bức tranh tổng thể & kịch bản demo gợi ý](#2-bức-tranh-tổng-thể--kịch-bản-demo-gợi-ý)
3. [Phần A — Quản trị viên (Admin): dựng dữ liệu nền](#3-phần-a--quản-trị-viên-admin-dựng-dữ-liệu-nền)
4. [Phần B — Cán bộ bảo vệ dữ liệu (Steward): thiết lập quy tắc truy cập](#4-phần-b--cán-bộ-bảo-vệ-dữ-liệu-steward-thiết-lập-quy-tắc-truy-cập)
5. [Phần C — LUỒNG CHÍNH: Sinh viên xin hỗ trợ → Trưởng chương trình ghép → Cố vấn xử lý](#5-phần-c--luồng-chính)
6. [Phần D — Các tình huống mở rộng](#6-phần-d--các-tình-huống-mở-rộng)
7. [Phần E — Quyền riêng tư của sinh viên](#7-phần-e--quyền-riêng-tư-của-sinh-viên)
8. [Phần F — Steward giám sát](#8-phần-f--steward-giám-sát)
9. [Phần G/H — Báo cáo của Trưởng chương trình, Thông báo & Cài đặt](#9-phần-gh--báo-cáo-thông-báo--cài-đặt)
10. [Lưu ý & rủi ro khi demo](#10-lưu-ý--rủi-ro-khi-demo)
11. [Phụ lục: bảng GUID, dữ liệu mẫu, từ điển trạng thái](#11-phụ-lục)

---

## 1. Chuẩn bị trước khi demo

### 1.1. Khởi động hệ thống

| Thành phần | Lệnh | Địa chỉ |
|---|---|---|
| Database | Có sẵn `AdviseCareDB` trong SQL Server (nếu cần dựng lại: chạy `Documentation\AdviseCare_Database_Script.sql`) | — |
| API (.NET) | Trong thư mục `AdviseCare_API\AdviseCare.API`: `dotnet run --launch-profile https` | https://localhost:7286 (Swagger: `/swagger`) |
| Web (Next.js) | Trong thư mục `AdviseCare_Webapp\advise-care`: `pnpm dev:local-api` | http://localhost:3000 |

Mở trình duyệt: **http://localhost:3000/vi/sign-in** (đuôi `/vi` = giao diện tiếng Việt, đổi thành `/en` để xem tiếng Anh).

> Trang đăng nhập **không có nút "Đăng nhập bằng Google"** (chưa cấu hình `NEXT_PUBLIC_GOOGLE_CLIENT_ID`). Hãy đăng nhập bằng email + mật khẩu như bên dưới.

### 1.2. Tài khoản demo (mật khẩu chung: `123456`)

| Vai trò | Email đăng nhập | Sau khi đăng nhập vào trang |
|---|---|---|
| Quản trị viên hệ thống | `admin@gmail.com` (hoặc `admin`) | `/vi/admin/m01` |
| Cán bộ bảo vệ dữ liệu (Steward) | `steward@gmail.com` | trang Steward |
| Cố vấn học tập | `academicadvisor@gmail.com` | `/vi/advisor/caseload` |
| Cố vấn wellbeing | `wellbeingadvisor@gmail.com` | `/vi/advisor/caseload` |
| Cán bộ công tác sinh viên (Welfare) | `welfareofficer@gmail.com` | `/vi/welfare/referrals` |
| Sinh viên (SE180001) | `student@gmail.com` | `/vi/me/profile` |
| Trưởng chương trình | `programmehead@gmail.com` | `/vi/head` |

> **Mẹo demo hai vai trò cùng lúc:** mở thêm **một cửa sổ ẩn danh** (hoặc profile trình duyệt thứ hai) — cửa sổ 1 là Sinh viên, cửa sổ 2 là Cố vấn/Trưởng chương trình. Không cần đăng xuất qua lại. Muốn đăng xuất: bấm **"Đăng xuất"** ở góc trên.

### 1.3. File mẫu để import (thư mục [`demo-samples`](./demo-samples))

| File | Dùng ở đâu | Kết quả mong đợi |
|---|---|---|
| [`studentdata_demo.csv`](./demo-samples/studentdata_demo.csv) | Admin → "Nhập dữ liệu sinh viên" | **Hợp lệ**: 5 dòng dữ liệu cho `student@gmail.com` (GPA 6.8, Attendance rate 72, Accommodation status rented, Family income bracket low, Financial difficulty true — học kỳ Fall 2026) |
| [`studentdata_invalid_demo.csv`](./demo-samples/studentdata_invalid_demo.csv) | cùng chỗ trên | **Bị từ chối** với 3 lỗi: email không tồn tại, thuộc tính `Height` không có, giá trị GPA `abc` không phải số |
| [`accounts_valid_demo.xlsx`](./demo-samples/accounts_valid_demo.xlsx) | Admin → "Tài khoản" → "Nhập từ Excel" | 5 tài khoản (3 sinh viên, 1 cố vấn, 1 nhân sự có 2 vai trò) |
| [`accounts_invalid_demo.xlsx`](./demo-samples/accounts_invalid_demo.xlsx) | cùng chỗ trên | **Bị từ chối toàn bộ** (có 1 email sai định dạng, 1 mã khoa `XX` không tồn tại) → minh họa nguyên tắc "một dòng lỗi thì không tạo gì cả" |

> Các file `.csv`/`.xlsx` mẫu đã được kiểm tra bằng API ở chế độ **kiểm tra thử (dry-run)**. File `accounts_valid_demo.xlsx` **chưa nhập thật** để không làm bẩn DB — nếu bạn nhập, 5 tài khoản mới sẽ ở trạng thái "chưa liên kết" và không đăng nhập được (xem mục 10).

### 1.4. Đặt lại dữ liệu (nếu demo bị "bẩn")

Tài khoản `student@gmail.com` hiện **đã có dữ liệu thử** (2 yêu cầu: "Accommodation" – Đã liên hệ, "Money" – Đã ghép cố vấn, và một số đồng ý). Có 2 cách:

- **Không reset:** cứ tạo yêu cầu mới từ `student@gmail.com` — vẫn demo bình thường.
- **Reset sạch:** chạy lại `Documentation\AdviseCare_Database_Script.sql` (xóa toàn bộ bảng, tạo lại, nạp dữ liệu mẫu). Lưu ý: sau khi reset, sinh viên **chưa có giá trị dữ liệu nào** → phải làm lại bước import ở Phần A.5.

---

## 2. Bức tranh tổng thể & kịch bản demo gợi ý

### 2.1. Luồng nghiệp vụ trong một câu

> Admin dựng tài khoản & dữ liệu sinh viên → Steward quy định "ai được xem dữ liệu gì, vì mục đích gì" → **Sinh viên gửi yêu cầu & tự chọn dữ liệu chia sẻ** → Trưởng chương trình **ghép cố vấn** → Cố vấn **liên hệ, theo dõi, ghi chú, đóng ca** (xem dữ liệu đã được sinh viên cho phép, mọi lần xem đều bị ghi log) → Sinh viên xem lại **ai đã xem dữ liệu của mình** → Steward giám sát.

```
Sinh viên ── gửi yêu cầu (+ chọn dữ liệu chia sẻ) ──► [Chờ ghép]
Trưởng CT ── ghép cố vấn ────────────────────────► [Đã ghép]
Cố vấn ── liên hệ SV ────────────────────────────► [Đã liên hệ]
Cố vấn ── theo dõi tiếp ─────────────────────────► [Đang theo dõi]
Cố vấn ── đóng ca ───────────────────────────────► [Đã đóng]   (có thể "Mở lại")
          └─ nhánh: chuyển tuyến sang Welfare ───► [Đã chuyển tuyến] ─ Welfare xử lý xong ─► quay về cố vấn
```

### 2.2. Thứ tự demo gợi ý

**Bản ngắn (~15 phút) — "xương sống" của app**

| # | Ai | Làm gì | Mục |
|---|---|---|---|
| 1 | Admin | Import `studentdata_demo.csv` (để cố vấn có dữ liệu mà xem) | A.5 |
| 2 | Sinh viên | Gửi yêu cầu hỗ trợ học tập + chia sẻ GPA, Attendance | C.1 |
| 3 | Trưởng CT | Ghép cố vấn | C.2 |
| 4 | Cố vấn | Mở ca, xem dữ liệu được chia sẻ, ghi chú, chuyển giai đoạn | C.3 |
| 5 | Sinh viên | Xem tiến độ ca + "Lịch sử xem" (cố vấn đã xem gì) | C.4 |

**Bản đầy đủ (30+ phút):** bản ngắn + Phần B (Steward) + Phần D (outreach, yêu cầu đồng ý, chuyển tuyến Welfare, đổi cố vấn) + Phần E (quyền riêng tư) + Phần F (giám sát) + Phần G (báo cáo).

---

## 3. Phần A — Quản trị viên (Admin): dựng dữ liệu nền

Đăng nhập `admin@gmail.com`. Menu bên trái (nhóm "Quản trị hệ thống"):
**Tài khoản và vai trò · Danh mục học thuật · Nhập dữ liệu sinh viên · Danh mục hỗ trợ · Lịch sử hoạt động · Thông báo · Cài đặt tài khoản · Phân vai trò · Phiên đăng nhập · Chuyên môn cố vấn.**

### A.1. Tài khoản (`/vi/admin/m01` — "Tài khoản và vai trò")

Trang hiện có khoảng 68 tài khoản, các cột: Họ và tên / Email / Tên đăng nhập / Vai trò / Trạng thái / Trạng thái liên kết; có bộ lọc.

**Cách 1 — Nhập hàng loạt từ Excel:** bấm **"Nhập từ Excel"** → chọn [`accounts_valid_demo.xlsx`](./demo-samples/accounts_valid_demo.xlsx).

File gồm 7 cột: `email, fullName, userName, facultyCode, role, studentCode, academicStatus`.

- Dòng **sinh viên** điền đủ 7 cột (ví dụ `demo.student01@example.com | Nguyễn Văn An | demo_student01 | SE | Student | SE189001 | enrolled`).
- Dòng **nhân sự** chỉ điền 5 cột đầu (không có mã sinh viên); nhiều vai trò viết cách nhau bằng dấu phẩy (`Academic Advisor, Programme Head`).
- Chỉ cần **1 dòng lỗi thì không tạo gì cả**, lỗi được báo theo từng dòng → thử file [`accounts_invalid_demo.xlsx`](./demo-samples/accounts_invalid_demo.xlsx) để minh họa (lỗi dòng 3: email sai; dòng 4: mã khoa không tồn tại).

**Cách 2 — Tạo từng tài khoản:** bấm **"Tạo"**, điền:

| Ô | Ví dụ | Ghi chú |
|---|---|---|
| Email* | `sv.demo@example.com` | |
| Họ và tên* | `Nguyễn Văn Demo` | |
| Tên đăng nhập* | `sv_demo` | |
| Khoa | `SE` | chọn trong danh sách |
| Tên vai trò* | `Student` | mỗi vai trò một dòng |
| Mã sinh viên | `SE189999` | **chỉ khi là Student** |
| Trạng thái học tập | `enrolled` | enrolled / onLeave / withdrawn / graduated |

> Quy tắc: tài khoản sinh viên **bắt buộc** có mã sinh viên + trạng thái học tập; tài khoản nhân sự thì **không được** có mã sinh viên.

**Thao tác trên từng dòng:** Gửi liên kết tài khoản (email hiệu lực 7 ngày — trạng thái chuyển *chưa liên kết → đã gửi email → đã liên kết*), Thêm vai trò, Kích hoạt / Tắt hoạt động, **Cấm tài khoản** (chặn đăng nhập, kết thúc mọi phiên) / Gỡ cấm.

### A.2. Phân công vai trò (`/vi/admin/m06` — "Phân vai trò")

Trả lời câu hỏi "một người có thể có nhiều vai trò không?" — **có**: bảng này là quan hệ nhiều–nhiều giữa tài khoản và vai trò (hiện ~69 dòng cho ~68 tài khoản, vì một vài người có 2 vai trò). Cột: *Mã tài khoản / Tài khoản / Mã vai trò*.

Bấm **"Thêm vai trò"**:
- **Tài khoản**: gõ tên hoặc email để tìm.
- **Vai trò**: chọn một trong 7 vai trò.
- **Mã sinh viên / Trạng thái học tập**: **chỉ hiện khi vai trò là Student** (vì chỉ sinh viên mới có hồ sơ sinh viên).

> 💡 Mẹo: ở bảng này có **biểu tượng copy** cạnh "Mã tài khoản" → dùng để lấy GUID khi cần điền vào các form chưa có ô chọn (xem mục 10).

### A.3. Danh mục học thuật (`/vi/admin/m02`)

4 tab, mỗi tab có nút **"Tạo"**:

| Tab | Các ô | Ví dụ |
|---|---|---|
| Khoa | Mã khoa, Tên khoa | `AI` / `Artificial Intelligence` |
| Chương trình | Mã, Tên, Khoa | `AIE` / `AI Engineering` / chọn khoa |
| Học kỳ | Tên, Ngày bắt đầu, Ngày kết thúc, Trạng thái | `Winter 2027`, 2027-01-01 → 2027-04-30, `upcoming` |
| Môn học | Khoa, Mã môn, Tên môn | `SE` / `PRN999` / `Demo Subject` |

Dữ liệu có sẵn: khoa SE, BA, SA; học kỳ Spring 2026 (đã qua), Summer 2026 (đã qua), **Fall 2026 (hiện tại)**, Spring 2027 (sắp tới).

### A.4. Phân công môn học cho cố vấn (`/vi/admin/m08` — "Chuyên môn cố vấn")

Gán **cố vấn ↔ môn học** để hệ thống **gợi ý đúng cố vấn** khi Trưởng CT ghép. Chọn **Cố vấn** (gõ tên để tìm) + **Môn học** → Lưu.

### A.5. Nhập dữ liệu sinh viên (`/vi/admin/m03`) — ⭐ BƯỚC QUAN TRỌNG

> **Tại sao quan trọng:** sinh viên **không thể tự gõ** GPA, thu nhập… vào app. Dữ liệu này chỉ vào hệ thống qua file import của Admin. Nếu **không import**, ở bước sau cố vấn sẽ chỉ thấy "có tồn tại nhưng không có giá trị".

Thao tác (file `.xlsx` hoặc `.csv`):

1. Bấm chọn file → [`studentdata_demo.csv`](./demo-samples/studentdata_demo.csv) → **"Xem trước"** (chỉ hiện tiêu đề + vài dòng mẫu, **chưa ghi gì**).
2. **Ghép cột** (5 ô dropdown):

   | Ô | Chọn |
   |---|---|
   | Cột định danh sinh viên | `Student email` *(chấp nhận email, tên đăng nhập hoặc mã sinh viên)* |
   | Mục dữ liệu | `Data attribute` |
   | Cột giá trị | `Value` |
   | Học kỳ | `Term` |
   | Có hiệu lực từ | `Valid from` |

3. Bấm **"Kiểm tra dữ liệu"** (chạy thử) → kết quả mong đợi: **5 dòng hợp lệ**.
4. Bấm **"Nhập dữ liệu"** (chỉ bấm được sau khi kiểm tra thành công).
5. Cuộn xuống **"Sổ dữ liệu sinh viên"** → thấy 5 giá trị vừa nhập.

Thử thêm file [`studentdata_invalid_demo.csv`](./demo-samples/studentdata_invalid_demo.csv) ở bước 3 → hệ thống báo **3 lỗi** (dòng 3, 4, 5), nút "Nhập dữ liệu" vẫn bị khóa.

> Nhập lại cùng một file chỉ **thêm dòng mới**, không ghi đè/cập nhật dòng cũ.

### A.6. Danh mục hỗ trợ (`/vi/admin/m04`)

Gồm 2 phần: **Danh mục hỗ trợ** (8 mục: Academic warning, Course content question, Exam preparation, Study plan, Thesis topic *(đã lưu trữ)*, Accommodation, Money, Stress and balance) và **Thuộc tính dữ liệu** (25 mục).

Bấm **"Tạo"** ở *Danh mục hỗ trợ*:

| Ô | Ví dụ |
|---|---|
| Tên danh mục* | `Career guidance` |
| Mô tả | `Định hướng nghề nghiệp` |
| Loại hỗ trợ* | `academic` (Học tập) hoặc `wellbeing` (Đời sống) |
| Mục đích đồng ý* | chọn trong danh sách (ví dụ *Academic advising*) |

> ⚠️ **Không demo "Thuộc tính dữ liệu → Tạo" bằng tài khoản Admin** — API trả **403** vì quyền tạo thuộc tính chỉ dành cho Steward (chi tiết mục 10). Hãy dùng 25 thuộc tính có sẵn.

### A.7. Các trang theo dõi

- **Lịch sử hoạt động** (`m05`, ~1.200 bản ghi): nhật ký mọi hành động trong hệ thống.
- **Phiên đăng nhập** (`m07`, ~150 bản ghi): các phiên đang/đã đăng nhập.

---

## 4. Phần B — Cán bộ bảo vệ dữ liệu (Steward): thiết lập quy tắc truy cập

Đăng nhập `steward@gmail.com`. Menu: **Mục đích đồng ý · Chính sách truy cập · Nhật ký truy cập và xử lý · Hàng đợi xóa dữ liệu · Duyệt truy cập khẩn cấp · Thuộc tính dữ liệu · Lịch sử đồng ý · Trạng thái khóa mã hóa.**

**Ý tưởng cốt lõi:** *Mục đích đồng ý* (vì sao cần dữ liệu) ↔ *Thuộc tính dữ liệu* (dữ liệu nào) ↔ *Chính sách truy cập* (vai trò nào được xem theo mục đích nào).

Dữ liệu seed đã có sẵn **5 mục đích** và **8 chính sách**, ví dụ:
- *Academic advising* → Cố vấn học tập
- *Wellbeing support* → Cố vấn wellbeing
- *Financial support* → Cố vấn wellbeing và Cán bộ công tác sinh viên

Nếu muốn demo việc **tạo mới** (thứ tự đúng: mục đích → gắn thuộc tính → kích hoạt → chính sách → kích hoạt → kiểm tra):

| Bước | Trang | Thao tác & ví dụ |
|---|---|---|
| B.1 | Mục đích đồng ý → **Tạo** | Tên mục đích\*: `Career support`; Mô tả\*: `Hỗ trợ định hướng nghề nghiệp`. Mục đích mới ở trạng thái **chưa hoạt động**. |
| B.2 | Cùng trang, mục *purposeAttributes* → **Tạo** | Chọn **Mục đích đồng ý** = `Career support`, rồi chọn **Mục đích dữ liệu** (thuộc tính) = `GPA` *(danh sách chỉ hiện sau khi chọn mục đích)* |
| B.3 | Mục đích → dòng `Career support` → **Kích hoạt** | Cũng có Vô hiệu, Lưu trữ |
| B.4 | Chính sách truy cập → **Tạo** | Vai trò = `Academic Advisor`, Mục đích = `Career support` (tạo ra bản **nháp**) |
| B.5 | Chính sách → **Kích hoạt** | Cũng có **Thu hồi** |
| B.6 | Chính sách → **Kiểm tra** | Chọn một sinh viên → hệ thống trả từng thuộc tính: `allowed` (được xem) / `allowedOnceActive` / `notShared` (sinh viên chưa chia sẻ). Bước này **không ghi log truy cập** |

> ⚠️ Trang **"Thuộc tính dữ liệu"** của Steward hiện chỉ có danh sách (25 mục), **không có nút "Tạo"** dù API cho phép Steward tạo. Vì vậy khi demo hãy dùng thuộc tính có sẵn.

---

## 5. Phần C — LUỒNG CHÍNH

> Đây là phần nên demo kỹ nhất. Điều kiện: đã làm **A.5** (import dữ liệu sinh viên).

### C.1. Sinh viên gửi yêu cầu hỗ trợ

Đăng nhập `student@gmail.com` → menu **"Gửi yêu cầu hỗ trợ"** (`/vi/me/support-request`). Form gồm 4 bước:

| Bước | Ô | Điền gì (ví dụ học tập) |
|---|---|---|
| **1. Loại hỗ trợ** | Học tập / Đời sống | Chọn **Học tập** |
| **2. Danh mục** | Danh mục + Môn học (tùy chọn) | Danh mục: `Course content question`; Môn học: `PRN212` |
| **3. Chia sẻ dữ liệu** | Tick các thuộc tính cho phép cố vấn xem | Tick **GPA** và **Attendance rate**. **Mỗi ô tick sẽ sinh ra một ô ghi chú riêng** (để sinh viên giải thích số liệu đó) |
| **4. Lý do** | "Hãy cho cố vấn biết chuyện gì đang xảy ra" (tối đa 4000 ký tự, tự lưu nháp) | `I don't understand dependency injection in PRN212 and failed the progress test. I need help before the final exam.` |

Ghi chú theo từng mục tick (gợi ý):
- *GPA*: `GPA học kỳ này giảm còn 6.8 vì trượt bài kiểm tra tiến độ.`
- *Attendance rate*: `Tôi nghỉ nhiều buổi vì đi làm thêm, tỉ lệ đi học khoảng 72%.`

Bấm **"Gửi yêu cầu"** → xuất hiện **hộp thoại "Mọi thứ đã đúng chưa?"** tóm tắt lại loại hỗ trợ, danh mục, dữ liệu sẽ chia sẻ và nội dung. Chọn **"Quay lại chỉnh sửa"** hoặc **"Gửi yêu cầu"** để xác nhận.

> Vì sao có hộp thoại xác nhận: sau khi gửi, **sinh viên không thể sửa hay hủy** yêu cầu (backend chưa có chức năng này).
>
> Những thuộc tính đã tick sẽ **tự tạo đồng ý chia sẻ** khi gửi — không tick gì thì cố vấn **không** xem được dữ liệu nào.

**Kịch bản "Đời sống" (để demo chuyển tuyến ở D.3):** chọn *Đời sống* → danh mục `Money` → tick **Financial difficulty** → lý do: `My family can no longer pay my tuition this term and I may have to drop out.`

Sau khi gửi, vào **"Yêu cầu"** (`/vi/me/cases`) → mở ca vừa tạo: thấy thanh tiến độ **Chờ ghép → Đã ghép → Đã liên hệ → Đang theo dõi → Đã đóng**, khung **"Nội dung bạn đã gửi"**, bảng **"Lịch sử"**.

### C.2. Trưởng chương trình ghép cố vấn

Đăng nhập `programmehead@gmail.com` → **"Hàng đợi ghép cố vấn"** (`/vi/head/matching`).

1. Cột trái là danh sách **"Chờ ghép cố vấn"**, mỗi dòng ghi mức ưu tiên, danh mục, mã sinh viên, thời gian chờ ("Quá hạn · N ngày"). Chọn dòng có mã **SE180001** (yêu cầu vừa gửi). Hàng đợi còn ~6 yêu cầu mẫu của sinh viên khác.
2. Cột phải hiện **nội dung sinh viên viết** và **"Cố vấn được gợi ý"**. Thứ tự gợi ý: *đang cố vấn cho sinh viên này → chuyên môn đúng môn → cùng khoa → ít ca đang mở nhất*; mỗi người có nhãn lý do ("Specialised in PRN212", "Same faculty as the student", "3 open case(s)").
3. Bấm **"Ghép"** cạnh cố vấn muốn chọn → hộp thoại: có thể nhập **lý do (không bắt buộc)** → xác nhận.
   - Để đăng nhập tiếp bằng tài khoản có sẵn, chọn cố vấn có tên **"Academic Advisor"** (= `academicadvisor@gmail.com`) cho yêu cầu học tập; yêu cầu "Đời sống" chọn **"Wellbeing Advisor"** (= `wellbeingadvisor@gmail.com`).
4. Trạng thái ca chuyển **Chờ ghép → Đã ghép**; cố vấn và sinh viên được thông báo.

### C.3. Cố vấn xử lý ca

Đăng nhập `academicadvisor@gmail.com` → **"Danh sách phụ trách"** (`/vi/advisor/caseload`) → mở ca của SE180001 (`/vi/advisor/cases/{id}`).

**(a) Xem dữ liệu sinh viên đã chia sẻ** — khung **"Dữ liệu sinh viên chia sẻ với bạn"** (trong "Thao tác khác"):
- GPA = 6.8, Attendance rate = 72 (kèm ghi chú nguồn) → vì sinh viên **đã tick**.
- Các thuộc tính khác hiển thị **"có tồn tại nhưng không chia sẻ"** (không lộ giá trị).
- ⚠️ **Mỗi giá trị hiển thị đều bị ghi vào nhật ký truy cập** (để sinh viên thấy ở bước C.4).

**(b) Chuyển giai đoạn** — khung **"Chuyển giai đoạn"**:

| Thao tác | Chọn | Ô "Điều đã diễn ra" (bắt buộc) |
|---|---|---|
| Đã liên hệ | `Đã liên hệ` | `Đã gọi điện cho sinh viên, hẹn gặp 14h thứ Năm tại phòng cố vấn.` |
| Theo dõi | `Đang theo dõi` | `Sinh viên đã đi học đều 2 tuần; tiếp tục theo dõi điểm bài kiểm tra tiếp theo.` |
| Đóng ca | `Đã đóng` *(có hộp thoại xác nhận)* | `Sinh viên đã vượt qua bài kiểm tra tiến độ, không cần hỗ trợ thêm.` |

Ca đã đóng sẽ có nút **"Mở lại"**.

**(c) Ghi chú ca** — khung **"Ghi chú case"** (chỉ nhân sự thấy, **sinh viên không thấy**): **"Thêm ghi chú"** → nhập nội dung → **"Lưu nháp"** → khi chắc chắn bấm **"Chốt"** (sau khi chốt **không sửa được**).

**(d) Lịch sử giai đoạn:** bảng ghi lại mỗi lần chuyển (ai, lúc nào, lý do).

**(e) Mức ưu tiên:** đổi trong "Thao tác khác".

### C.4. Sinh viên theo dõi

Quay lại `student@gmail.com`:
- **"Yêu cầu" → mở ca:** thanh tiến độ đã nhảy theo từng giai đoạn; bảng **"Lịch sử"** có các dòng mới; thấy khung **"Cố vấn của bạn"**.
- **"Lịch sử xem"** (`/vi/me/access-history`): thấy **cố vấn nào đã xem dữ liệu nào, lúc nào, vì mục đích gì**. Lọc theo ngày / vai trò. Bấm **"Đánh dấu"** (flag) nếu thấy truy cập đáng ngờ — Steward sẽ xem xét (Phần F).

> Hạn chế đã biết (nên nói trước khi giảng viên hỏi): sinh viên **không được thông báo** khi ca chuyển *Đã liên hệ/Đang theo dõi*; giai đoạn *Đang theo dõi* **không có ngày hẹn hay nhắc nhở**; sinh viên **không có nút "xác nhận đã giải quyết"** hay "yêu cầu mở lại".

---

## 6. Phần D — Các tình huống mở rộng

### D.1. Cố vấn chủ động mở ca cho sinh viên (outreach)

Cố vấn (`academicadvisor@gmail.com`) → **"Danh sách phụ trách"** → nút **"Mở case cho sinh viên"** → form 4 bước:

1. **Tìm sinh viên** (gõ tên hoặc mã): `SE180001`.
2. **Danh mục**: ví dụ `Study plan`.
3. **Môn học** (tùy chọn): `SWP391`.
4. **Lý do**: `Điểm danh giảm còn 72% và GPA 6.8 học kỳ này. Muốn cùng em lập kế hoạch học cho SWP391.`

Bấm **"Mở case"** → ca được tạo **ở trạng thái "Đã ghép" luôn** (bỏ qua hàng đợi) và **sinh viên nhận thông báo**.

### D.2. Cố vấn xin sinh viên cấp thêm quyền xem dữ liệu ("Yêu cầu đồng ý")

Cố vấn học tập hoặc wellbeing → menu **"Yêu cầu đồng ý"** → **"Tạo"**:

| Ô | Ví dụ |
|---|---|
| Mục đích đồng ý | `Academic advising` |
| Thuộc tính dữ liệu | chọn các thuộc tính (danh sách phụ thuộc vào mục đích đã chọn), ví dụ `Attendance rate`, `Accommodation status` |
| Sinh viên | `SE180001` |
| Lý do hiển thị cho sinh viên | `Để cố vấn hiểu hoàn cảnh học tập và nơi ở của em.` |

Sau đó sinh viên vào **"Yêu cầu dữ liệu"** (`/vi/me/consent/requests`) → chọn **"Đồng ý chia sẻ"** hoặc **"Từ chối yêu cầu"**. Nếu từ chối, cố vấn vẫn chỉ thấy "có tồn tại nhưng không chia sẻ". Yêu cầu còn **đang chờ** có thể được cố vấn **cập nhật / rút lại**.

### D.3. Chuyển tuyến sang Welfare (vấn đề tài chính)

> Kịch bản: yêu cầu "Money" đã được ghép cho `wellbeingadvisor@gmail.com`.

**1) Cố vấn wellbeing** → mở ca → "Thao tác khác" → **"Chuyển tuyến cho người khác"**:

| Ô | Điền |
|---|---|
| ID tài khoản người nhận | **GUID của Welfare:** `0CAA0436-8CFC-5AB5-B05E-025D8C0280C5` *(form này chưa có ô chọn — xem mục 10)* |
| Lý do | `Gia đình sinh viên gặp khó khăn học phí, cần phương án vay/hỗ trợ tài chính.` |
| Mức ưu tiên | `Cao` |

Bấm **"Gửi chuyển tuyến"** → ca sang giai đoạn **"Đã chuyển tuyến"**.

**2) Cán bộ Welfare** (`welfareofficer@gmail.com`) → **"Yêu cầu được chuyển đến"** (`/vi/welfare/referrals`):
- **"Chấp nhận"**; hoặc **"Đổi mức ưu tiên"**; hoặc **"Trả lại"** (kèm lý do); hoặc **"Chuyển lên Trưởng chương trình"** (xem D.5).

**3) Tạo can thiệp** — menu **"Xử lý công tác sinh viên"** (`/vi/welfare/cases`) → **"Tạo"**:

| Ô | Ví dụ |
|---|---|
| Mã ca hỗ trợ\* | dán **ID ca** (lấy ở danh sách, bấm biểu tượng copy cạnh ID) |
| Loại can thiệp\* | `Thỏa thuận khoản vay` / `Hỗ trợ tài chính` / `Thủ tục ngân hàng` / `Khác` |
| Ngày can thiệp\* | ngày hôm nay |
| Ghi chú | `Hẹn tư vấn gói hỗ trợ học phí.` |
| Trạng thái\* | `Đã lên lịch` hoặc `Đang thực hiện` |

Trên dòng đã tạo: **Cập nhật** (có thêm ô *Kết quả*), **Đóng** (**bắt buộc** nhập *Kết quả*, ví dụ `Đã được giảm 30% học phí`). **Khi Welfare đóng can thiệp, ca quay lại cho cố vấn** để tiếp tục.

**4) Giới thiệu ra ngoài** — menu **"Giới thiệu ra ngoài"** → **"Tạo"**:

| Ô | Ví dụ |
|---|---|
| Mã ca hỗ trợ\* | dán ID ca |
| Dịch vụ bên ngoài\* | chọn 1 trong **6 dịch vụ có sẵn** |
| Lý do\* | `Cần tư vấn tâm lý chuyên sâu ngoài trường.` |
| Hạn theo dõi | một ngày trong tương lai |

Dòng có thể **Cập nhật** (Hạn theo dõi / Kết quả theo dõi / Trạng thái) hoặc **Đóng**. Cờ **quá hạn** do tác vụ nền kiểm tra tự động.

> ⚠️ Nút **"Tạo"** ở trang *Dịch vụ bên ngoài* của Welfare trả **403** — đừng demo, hãy dùng 6 dịch vụ sẵn có (mục 10).

### D.4. Cố vấn xin đổi cố vấn khác

Cố vấn → ca → "Thao tác khác" → **"Đề nghị đổi cố vấn"**:

| Ô | Điền |
|---|---|
| ID tài khoản cố vấn đề xuất | ví dụ **Đỗ Thanh Mai**: `E7101E7D-9CD9-553E-B5D9-2F0C7DC19032` |
| Lý do | `Sinh viên thuộc khoa BA, cố vấn Mai phù hợp hơn.` |

Bấm **"Gửi đề nghị"**. Trưởng CT vào **"Yêu cầu đổi cố vấn"** (`/vi/head/assignment-changes`) → **"Chấp thuận"** (ghi chú tùy chọn) hoặc **"Từ chối"** (**bắt buộc** nhập lý do).

Ngoài ra Trưởng CT có thể chủ động **"Phân công lại"** ở **"Tất cả case"** (`/vi/head/cases`).

### D.5. Chuyển cấp lên Trưởng chương trình (escalation)

Welfare → "Yêu cầu được chuyển đến" → **"Chuyển lên Trưởng chương trình"**:

| Ô | Điền |
|---|---|
| ID tài khoản Trưởng chương trình | `3978D886-B5A9-5108-89A0-5DD87AA154B4` |
| Lý do | `Cần quyết định miễn giảm học phí vượt thẩm quyền Welfare.` |

Trưởng CT → **"Yêu cầu chuyển cấp"** (`/vi/head/escalations`) → **"Phản hồi"** (**bắt buộc** nhập nội dung phản hồi).

### D.6. Truy cập khẩn cấp (break-glass)

Cố vấn cần xem dữ liệu **mà sinh viên chưa cho phép** trong tình huống khẩn:

1. Cố vấn → **"Truy cập khẩn cấp"** → **"Tạo"** → chọn **Sinh viên**, nhập **"Lý do yêu cầu"** (`Sinh viên có dấu hiệu khủng hoảng, cần xem thông tin liên quan gấp.`).
2. **Steward** → **"Duyệt truy cập khẩn cấp"** → **"Chấp thuận"** (chọn *Thời điểm hết hạn truy cập*) hoặc **"Từ chối"** (nhập *Lý do từ chối*).
3. Truy cập khẩn cấp **tự hết hạn** và được ghi log.

---

## 7. Phần E — Quyền riêng tư của sinh viên

Đăng nhập `student@gmail.com`.

| Trang | Làm gì |
|---|---|
| **Đồng ý** (`/vi/me/consent`) | Danh sách ~26 dòng (mục đích × thuộc tính). Mở một dòng → **"Đồng ý chia sẻ"** (có thể đặt **"Ngày hết hạn đồng ý (không bắt buộc)"**). Dòng đã đồng ý có nút **"Rút"** để thu hồi — cố vấn lập tức không còn thấy giá trị. |
| **Yêu cầu dữ liệu** (`/vi/me/consent/requests`) | Các yêu cầu từ cố vấn (xem D.2) → **Đồng ý chia sẻ / Từ chối yêu cầu** |
| **Lịch sử xem** (`/vi/me/access-history`) | Ai đã xem dữ liệu của mình; lọc theo ngày/vai trò; **"Đánh dấu"** để báo nghi vấn |
| **Hồ sơ** (`/vi/me/profile`) | Thông tin cá nhân, dữ liệu học tập đã import |
| **Yêu cầu đính chính** (`/vi/me/corrections`) | Xem bên dưới |
| **Xuất dữ liệu của tôi** | **"Tải xuống"** file JSON chứa toàn bộ dữ liệu cá nhân (quyền "data portability") |

**Yêu cầu đính chính dữ liệu** — bấm **"Tạo"**:

| Ô | Ví dụ |
|---|---|
| Giá trị cần điều chỉnh | chọn trong danh sách các giá trị đã import, ví dụ `GPA = 6.8` |
| Giải thích\* | `GPA của tôi sau khi phúc khảo là 7.2, hệ thống chưa cập nhật.` |
| Giá trị đề xuất\* | `7.2` |

Sinh viên có thể **"Rút"** yêu cầu khi chưa được xử lý.

**Cố vấn xử lý đính chính:** (cố vấn có ca đang mở của sinh viên đó) menu **"Đính chính"** → **"Chấp thuận"** hoặc **"Từ chối"**, mỗi lựa chọn **bắt buộc** nhập *Ghi chú quyết định*.

> Lưu ý: quy tắc "chỉ cố vấn có ca đang mở mới được xử lý" đã có; còn quy tắc "chỉ sinh viên có ca mở mới được gửi đính chính" mới **nằm trong kế hoạch**, chưa được áp dụng.

---

## 8. Phần F — Steward giám sát

Đăng nhập `steward@gmail.com`:

| Trang | Dùng để demo |
|---|---|
| **Lịch sử đồng ý** | Toàn bộ lịch sử cấp/rút đồng ý của sinh viên (~600 dòng) + các yêu cầu đã nhận |
| **Nhật ký truy cập và xử lý** | Mỗi lần ai xem dữ liệu gì. Có bộ lọc. **"Đánh dấu"** để gắn cờ; **"Xem xét"** → chọn `Đã xác nhận hợp lệ` hoặc `Đã xác nhận sự cố`; **"Tải xuống"** để xuất hồ sơ xử lý (phục vụ kiểm toán) |
| **Hàng đợi xóa dữ liệu** | Yêu cầu xóa; **"Thực hiện xóa"** (sau khi xóa, khóa mã hóa của thuộc tính bị hủy — không khôi phục được) |
| **Duyệt truy cập khẩn cấp** | Xem D.6 |
| **Trạng thái khóa mã hóa** | Tình trạng khóa của từng thuộc tính; **"Mã hóa lại khóa"** (rewrap) |

> Đừng bấm "Thực hiện xóa" khi demo trên dữ liệu bạn còn cần — thao tác này **không hoàn tác được**.

---

## 9. Phần G/H — Báo cáo, Thông báo & Cài đặt

### G. Trưởng chương trình

- **"Khối lượng cố vấn"** (`/vi/head/advisor-workload`): chọn **Từ\*** / **Đến\*** (ngày) → xem số ca mỗi cố vấn.
- **"Báo cáo ca hỗ trợ"** (`/vi/head/reports`): chọn **Từ\*** / **Đến\*** → xem thống kê, có thể **xuất Excel hoặc PDF**.
- **"Tất cả case"**: lọc, **"Phân công lại"**.

### H. Chung cho mọi vai trò

- **Thông báo** (`/vi/notifications`): đánh dấu đã đọc, đánh dấu tất cả, xóa, xóa tất cả đã đọc. Chuông thông báo hiện ở thanh trên.
- **Cài đặt** (`/vi/settings`): bật/tắt thông báo qua email / trong app, đổi **ngôn ngữ**, đổi mật khẩu, ảnh đại diện.

---

## 10. Lưu ý & rủi ro khi demo

### 10.1. Bốn điểm "lệch" giữa giao diện và backend (nên tránh hoặc dùng cách vòng)

| # | Hiện tượng | Cách xử lý khi demo |
|---|---|---|
| 1 | Form **"Chuyển tuyến cho người khác"** và **"Đề nghị đổi cố vấn"** (trang ca của cố vấn) **vẫn bắt gõ GUID** ("ID tài khoản người nhận", "ID tài khoản cố vấn đề xuất") | Dán GUID từ [Phụ lục 11.1](#111-guid-các-tài-khoản-mẫu), hoặc copy ở Admin → "Phân vai trò" (biểu tượng copy cạnh "Mã tài khoản") |
| 2 | Các form của **Welfare** vẫn gõ ID: "Mã ca hỗ trợ" (Can thiệp, Giới thiệu ra ngoài), "ID tài khoản Trưởng chương trình" (Chuyển cấp) | Copy ID ca ở danh sách (biểu tượng copy); GUID Trưởng CT ở Phụ lục |
| 3 | Admin → "Danh mục hỗ trợ" → **Thuộc tính dữ liệu → Tạo** trả **403** (API chỉ cho **Steward**), còn trang của Steward lại **không có nút Tạo** | Dùng 25 thuộc tính có sẵn; không demo tạo thuộc tính |
| 4 | Welfare → **Dịch vụ bên ngoài → Tạo** trả **403** (API chỉ cho **System Administrator**), mà Admin lại **không có trang** dịch vụ bên ngoài | Chỉ chọn trong 6 dịch vụ có sẵn |

### 10.2. Các hạn chế khác

- Sinh viên **không sửa/hủy** được yêu cầu sau khi gửi.
- Không có thông báo cho sinh viên khi ca chuyển *Đã liên hệ / Đang theo dõi*; *Đang theo dõi* không có ngày hẹn/nhắc việc.
- Sinh viên không thể xác nhận "đã giải quyết" hoặc xin mở lại ca.
- Giới hạn 4000 ký tự của lý do chỉ có ở **frontend**.
- Tài khoản tạo bằng Excel/biểu mẫu có trạng thái **"chưa liên kết"** và **không có mật khẩu** → chưa đăng nhập được (cần email liên kết). Vì vậy demo đăng nhập chỉ dùng **7 tài khoản có sẵn**.
- Trang đầu của Welfare (`/welfare/referrals`) và "Chuyển cấp trên" có thể **trống** nếu chưa có ca nào được chuyển tuyến — hãy làm D.3 trước.
- Không có nút đăng nhập Google.

### 10.3. Khi gặp sự cố

| Triệu chứng | Nguyên nhân & cách xử lý |
|---|---|
| Cố vấn mở ca nhưng **không thấy giá trị GPA** | Chưa import dữ liệu (làm A.5) hoặc sinh viên chưa tick chia sẻ |
| Trưởng CT không thấy yêu cầu mới trong hàng đợi | Bấm **"Làm mới"**; tìm theo mã **SE180001** (hàng đợi có sẵn ~6 yêu cầu mẫu) |
| Không thấy "Academic Advisor" trong danh sách gợi ý | Chọn một cố vấn khác để ghép; muốn dùng tài khoản demo thì sau đó dùng "Phân công lại" ở `/vi/head/cases` |
| Lỗi 403 ở một nút | Xem 10.1 (#3, #4) hoặc bạn đang đăng nhập sai vai trò |
| Trang trắng / lỗi mạng | Kiểm tra API đang chạy ở https://localhost:7286 và đã chấp nhận chứng chỉ HTTPS dev |
| Dữ liệu demo rối | Reset DB (mục 1.4) |

---

## 11. Phụ lục

### 11.1. GUID các tài khoản mẫu

| Tài khoản | Vai trò | accountId |
|---|---|---|
| `admin@gmail.com` | Quản trị viên hệ thống | `D2AA2929-437D-50E2-A61D-40EA2B09010E` |
| `steward@gmail.com` | Cán bộ bảo vệ dữ liệu | `4FA71983-548F-5C26-BEFE-0E089DF4956F` |
| `academicadvisor@gmail.com` | Cố vấn học tập | `16C05C9C-133D-5FAC-B287-37C15764B71E` |
| `wellbeingadvisor@gmail.com` | Cố vấn wellbeing | `B6944BEC-089A-5878-9C34-EED3583DAABF` |
| `welfareofficer@gmail.com` | Cán bộ công tác sinh viên | `0CAA0436-8CFC-5AB5-B05E-025D8C0280C5` |
| `student@gmail.com` (SE180001) | Sinh viên | `6341514A-BC92-5729-9218-7DA322CDCD4E` |
| `programmehead@gmail.com` | Trưởng chương trình | `3978D886-B5A9-5108-89A0-5DD87AA154B4` |

Cố vấn mẫu khác (dùng làm "cố vấn đề xuất" ở D.4):

| Tên | Email | accountId |
|---|---|---|
| Đỗ Thanh Mai | `aa.maidt@example.com` | `E7101E7D-9CD9-553E-B5D9-2F0C7DC19032` |
| Phạm Thanh Hùng | `aa.hungpt@example.com` | `F9552B17-90D1-5795-BB26-0875EE5BD496` |
| Nguyễn Thị Lan | `aa.lannt@example.com` | `A6E940DF-7FE9-551A-A267-C50775E7BA4E` |

> Các tài khoản mock này dùng mật khẩu khác (không có trong tài liệu) nên **không dùng để đăng nhập demo**; chỉ dùng GUID để "ghép/đề xuất".

### 11.2. Từ điển trạng thái ca hỗ trợ

| Giai đoạn (stage) | Hiển thị | Ai chuyển được |
|---|---|---|
| `pending` | Chờ ghép | (tự động khi sinh viên gửi) |
| `assigned` | Đã ghép | Trưởng CT (ghép) / Cố vấn (outreach) / Welfare đóng can thiệp |
| `contacted` | Đã liên hệ | Cố vấn |
| `referred` | Đã chuyển tuyến | Cố vấn (chuyển tuyến) |
| `followUp` | Đang theo dõi | Cố vấn |
| `closed` | Đã đóng | Cố vấn (có thể "Mở lại") |

Trạng thái tổng: `open` / `closed` / `reopened`. API chuyển giai đoạn của cố vấn **chỉ nhận** `contacted` / `followUp` / `closed`.

### 11.3. Dữ liệu tham chiếu có sẵn

- **Danh mục hỗ trợ (8):** Academic warning, Course content question, Exam preparation, Study plan, Thesis topic (đã lưu trữ), Accommodation, Money, Stress and balance.
- **Thuộc tính dữ liệu (25):** ví dụ GPA, Attendance rate, Accommodation status, Family income bracket, Financial difficulty… (kiểu: số / phân loại / đúng-sai; 23 đang hoạt động, 2 đã lưu trữ).
- **Mục đích đồng ý (5), Chính sách (8), Dịch vụ bên ngoài (6).**
- **Khoa:** SE, BA, SA. **Học kỳ hiện tại:** Fall 2026.

### 11.4. Bảng "nên điền gì" nhanh cho các ô hay gặp

| Ô | Gợi ý điền |
|---|---|
| Lý do gửi yêu cầu | Một-hai câu mô tả vấn đề (tiếng Anh hoặc tiếng Việt đều được) |
| Điều đã diễn ra (chuyển giai đoạn) | Việc cố vấn đã làm: gọi điện, họp, nhắc nhở… |
| Lý do đổi/chuyển tuyến | Vì sao cố vấn hiện tại không phù hợp / vấn đề vượt thẩm quyền |
| Giải thích đính chính | Dữ liệu hiện tại sai ở đâu và có chứng cứ gì |
| Kết quả (đóng can thiệp / giới thiệu) | Kết quả cuối: được hỗ trợ gì, còn gì tồn đọng |

---

*Tài liệu này dựa trên bản dev hiện tại của dự án; nếu giao diện/backend thay đổi, hãy đối chiếu lại các nhãn nút và đường dẫn trước khi demo.*
