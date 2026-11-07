# AdviseCare - Bộ mock data đầy đủ

File SQL: `AdviseCare_Mock_Data.sql` (sinh bởi `generate_mock_data.py`). Ngày tham chiếu của dữ liệu: **05/11/2026 03:00 UTC** (Fall 2026 là kỳ hiện tại).

## Cách chạy

1. Chạy `AdviseCare_Database_Script.sql` (tạo DB + 45 bảng). Nếu DB cũ chưa có `CASEMESSAGES` và `CASE_ATTRIBUTE`, chỉ cần chạy `AdviseCare_Case_Messages_And_Attributes_Migration.sql`.
2. Chạy `AdviseCare_Mock_Data.sql`: script **xoá toàn bộ dữ liệu của 45 bảng** rồi nạp lại (`CASEMESSAGES` và `CASE_ATTRIBUTE` chưa có mock data, được để trống) trong một transaction (lỗi thì rollback hết). Chạy lại bao nhiêu lần cũng được.
   - File ~7 MB: nếu SSMS mở chậm, dùng `sqlcmd -S localhost -E -f 65001 -i AdviseCare_Mock_Data.sql` (hoặc `-U sa -P ...` thay cho `-E`).
3. API phải dùng **đúng master key đã dùng khi sinh file** (`Encryption:MasterKeys:1` trong `AdviseCare_API/AdviseCare.API/appsettings.json`). Nếu đổi key, chạy lại `python generate_mock_data.py` để sinh file mới, nếu không mọi giá trị dữ liệu sinh viên sẽ báo không giải mã được.

## Tài khoản

Mật khẩu chung: **123456**. Đăng nhập bằng email hoặc username.

| Vai trò | Username (email = username@example.com) | Ghi chú |
|---|---|---|
| System Administrator | admin02, admin03 | admin03 chưa link Google |
| Data Protection Steward | steward02, steward03 | |
| Programme Head | ph.quanpm, aa.khoavd | aa.khoavd vừa là Academic Advisor vừa là Programme Head |
| Academic Advisor | aa.hungpt, aa.lannt, aa.khoavd, aa.thaonp, aa.phongld, aa.vylt, aa.tuandq, aa.maidt, aa.huyenbt, aa.trangnh | aa.sonhm đã bị deactivate (đăng nhập phải ra 403) |
| Wellbeing Advisor | wa.ngocbt, wa.tamlh, wa.linhvk, wa.dungpt | wa.hoaitt đã bị deactivate |
| Student Welfare Officer | wo.yennh, wo.namtv, wo.thuylm | |
| Student | 150 tài khoản mock, xem các ví dụ bên dưới | username dạng `tenhoxxse18xxxx` |

7 tài khoản seed (`admin@gmail.com`, `steward@gmail.com`, ... `programmehead@gmail.com`) vẫn **sạch** như trước để chạy theo `AdviseCare_API_Workflow_List.txt` từ đầu.

Tài khoản đặc biệt:
- Chỉ có Google, không có mật khẩu (hasPassword = false, không đăng nhập bằng mật khẩu được; dùng để kiểm tra dashboard admin): quanngse207323, huongnhse179039, trangcmse206114, tramqnba193706, vinhddse182414
- Bị ban: uyendtse188112, phattaai188976; bị deactivate: thaotgse168678, huydbse160646, anhttba161981, phuongptia160977
- `linkStatus` có đủ 3 giá trị notLinked / emailSent / linked để test gửi lại email liên kết.

## Dữ liệu theo từng chức năng

**Ghép case (Programme Head)** - đăng nhập `ph.quanpm`: hàng đợi có 18 case `pending`, trong đó 5 case chờ quá 2 ngày. Có 4 yêu cầu đổi cố vấn đang chờ duyệt và 2 escalation chưa trả lời. Báo cáo: chọn khoảng 01/05/2026 - 31/12/2026 để thấy cả case kỳ Summer.

**Cố vấn (danh sách case được giao)** - số case đang mở: wa.linhvk (18), wa.dungpt (18), wa.ngocbt (17), wa.tamlh (17), aa.huyenbt (10), aa.maidt (9), aa.phongld (8), aa.khoavd (8), aa.lannt (7), aa.hungpt (7), aa.trangnh (7), aa.tuandq (7), aa.vylt (6), aa.thaonp (6).

**Xem dữ liệu sinh viên có đồng ý** - đăng nhập `aa.maidt`, mở case `0D86962C-D8B2-5095-97C2-2BBFD0296242` của `nganlkba183583`: các thuộc tính đã chia sẻ hiện giá trị + nguồn import, phần còn lại hiện "exists, not shared", dòng thiếu hiện isMissing. Mỗi lần xem sinh ra dòng ACCESSLOGS mới.

**Way back machine (EX13)** - sinh viên `trungdmse207956` có Attendance rate Fall 2026 = 87 (import 28/09), được đính chính thành 95 ngày 15/10/2026 với validFrom 26/09, sau đó import giữa kỳ 26/10 ghi giá trị mới. Thử as-of `2026-10-14` với timeAxis=system (giá trị cũ) và timeAxis=valid (giá trị đã sửa), rồi so sánh với `2026-11-01`. Đăng nhập `wa.ngocbt` (người đã duyệt đính chính, sinh viên đã chia sẻ Attendance rate cho vai trò này).

**Yêu cầu đính chính (EX15)** - `trungdmse207956` (Attendance rate, người duyệt: `aa.thaonp`), `khanhcvai187390` (GPA, người duyệt: `wa.linhvk`), `khanhcvai187390` (Tuition outstanding, người duyệt: `wa.linhvk`), `khanhcvai187390` (Credits earned, người duyệt: `wa.linhvk`) đang chờ duyệt. Tổng: 19 yêu cầu (pending/approved/rejected/withdrawn).

**Chuyển tuyến (EX4, WF2b)** - 20 referral đang chờ: `wo.namtv` nhận từ `wa.tamlh`, `wo.yennh` nhận từ `wa.dungpt`, `wo.namtv` nhận từ `wa.ngocbt`, `aa.phongld` nhận từ `aa.vylt`, `aa.lannt` nhận từ `aa.khoavd`, `wa.linhvk` nhận từ `wa.ngocbt`.

**Welfare Officer** - case đã nhận: `wo.thuylm` (10), `wo.namtv` (6), `wo.yennh` (5). Có 32 intervention, 13 support grant (có grant bị revoke), 14 external referral (4 quá hạn), 6 escalation.

**Consent (EX7, EX8)** - 845 consent đang granted (58 đã hết hạn theo expiresAt), 56 đã rút. Consent request: denied 6, granted 20, pending 8, withdrawn 4.
Sinh viên có request đang chờ trả lời: `nhanbcse172545`, `trungdtai196624`, `namlqba186687`, `mykkse203363`.
Sinh viên có consent đã hết hạn: `toanpdse185116`.

**Xoá mật mã (crypto erasure)** - Steward thấy 20 erasure request `pending` và 31 đã thực hiện (31 key destroyed, 60 giá trị đã bị xoá). Request của `lambtai197428` (Has part-time job) **không thực hiện được** vì sinh viên đã chia sẻ lại thuộc tính này ở purpose khác (API phải trả 409). Sinh viên `thanhttai195740` có Commute time hiện "đã xoá" trong hồ sơ của chính mình.

**Truy cập khẩn cấp (EX10)** - đang hiệu lực: `wa.ngocbt` → `trangcmse206114` đến 12/11/2026 10:00 UTC, `aa.hungpt` → `thuymtse180642` đến 20/12/2026 10:00 UTC; 3 request chờ Steward duyệt, có cả request đã hết hạn và bị từ chối. Sau ngày hết hạn, quyền tự mất (đúng thiết kế).

**Nhật ký truy cập (EX9)** - 2207 dòng ACCESSLOGS (có dòng purpose = null của truy cập khẩn cấp), 14 dòng bị flag (7 chưa review). Ví dụ sinh viên có dòng bị flag: `huyvbba193689`.

**Import dữ liệu (WF5)** - 4 lần import: Spring 2026 (30/04), Summer 2026 (28/08), Fall 2026 tuần 3 (28/09), Fall 2026 giữa kỳ (26/10) + 1 lần import lỗi trong activity log. 488 dòng `isMissing`, 5 dòng `correction`. Bảng DATASOURCES / INGESTIONSCHEDULES / INGESTIONJOBS có job success, failed, running, pending.

**Thông báo, phiên đăng nhập, lịch sử hoạt động** - 1396 notification (cả đã đọc và chưa đọc), 464 session (64 còn active), 3158 activity log, 60 cài đặt kênh thông báo.

**Master data** - 6 khoa (JP archived), 8 chương trình, 5 kỳ (Fall 2025 → Spring 2027), 28 môn (2 archived), 13 support category (Thesis topic archived; Career orientation gắn purpose Career guidance chưa active nên sinh viên không chia sẻ dữ liệu được), 9 external service (1 inactive), 9 access policy (active/draft/revoked).

## Số dòng theo bảng

| Bảng | Số dòng |
|---|---:|
| FACULTIES | 6 |
| ACCOUNTS | 181 |
| STUDENTPROFILES | 151 |
| ROLES | 7 |
| ACCOUNT_ROLE | 182 |
| SESSIONS | 464 |
| NOTIFICATIONS | 1396 |
| NOTIFICATIONPREFERENCES | 60 |
| ACTIVITYLOGS | 3158 |
| PROGRAMMES | 8 |
| ACADEMICTERMS | 5 |
| DATAATTRIBUTES | 25 |
| ATTRIBUTEKEYS | 3647 |
| SUBJECTS | 28 |
| DATASOURCES | 5 |
| INGESTIONSCHEDULES | 5 |
| INGESTIONJOBS | 61 |
| STUDENTDATA | 7158 |
| ACCOUNT_SUBJECT | 27 |
| CONSENTPURPOSES | 5 |
| SUPPORTCATEGORIES | 13 |
| CASES | 258 |
| CASECHANGEREQUESTS | 10 |
| CASENOTES | 327 |
| CASESTAGEHISTORIES | 995 |
| REFERRALS | 54 |
| CORRECTIONREQUESTS | 19 |
| INTERVENTIONS | 32 |
| SUPPORTGRANTS | 13 |
| EXTERNALSERVICES | 9 |
| EXTERNALREFERRALS | 14 |
| ESCALATIONS | 6 |
| PURPOSE_ATTRIBUTE | 33 |
| CONSENTS | 901 |
| CONSENTHISTORIES | 957 |
| CONSENTREQUESTS | 38 |
| ACCESSPOLICIES | 9 |
| ACCESSLOGS | 2207 |
| EMERGENCYACCESSREQUESTS | 9 |
| ERASUREREQUESTS | 51 |
| REPORTS | 10 |
| BACKUPS | 21 |
| RESTOREOPERATIONS | 3 |

## Sinh lại dữ liệu

```
pip install cryptography
python generate_mock_data.py                      # đọc master key từ ../../AdviseCare_API/AdviseCare.API/appsettings.json
python generate_mock_data.py --master-key <base64> --master-key-version 1
```
Seed ngẫu nhiên cố định nên nội dung, id và thời gian giống nhau mỗi lần sinh; chỉ phần mã hoá (nonce, data key) thay đổi.
