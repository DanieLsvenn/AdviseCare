# AdviseCare — Web app

Frontend của AdviseCare (Academic Advising & Student Welfare Management System),
dựng theo bộ prototype trong `Prototype/stitch_advisecare_academic_advising_platform`
và bộ quy tắc `advisecare_project_design_rules.md`.

## Ngăn xếp

| Thành phần | Lựa chọn | Lý do |
|---|---|---|
| Framework | Next.js 16.3 (App Router, Turbopack) | Server Components giúp mọi màn đọc dữ liệu ngay trên server, không cần state client cho phần lớn giao diện |
| UI | React 19 + Tailwind CSS v4 | Token thiết kế khai báo trực tiếp trong `@theme`, không cần file config riêng |
| i18n | next-intl 4 (`[locale]`, EN/VI) | Design Rule §4.7 bắt buộc mọi màn có toggle EN \| VI |
| Font | `@fontsource/be-vietnam-pro`, `@fontsource/jetbrains-mono`, `material-symbols` | Tự lưu trữ qua npm — build và chạy được trong mạng nội bộ, không gọi fonts.googleapis.com |
| Dữ liệu | Route Handlers `/api/*` trả mock | Frontend fetch như thật; khi API .NET sẵn sàng chỉ cần đổi base URL |

## Chạy dự án

```bash
pnpm install
pnpm dev      # http://localhost:3000 → tự chuyển sang /en
pnpm build
pnpm lint
```

## Cấu trúc

```
app/
  [locale]/
    layout.tsx            root layout (html/body, NextIntlClientProvider)
    sign-in/              C01 — đăng nhập qua SSO
    advisor/              A01–A14 (shell riêng, sidebar 256px)
    me/                   S01–S10 (shell sinh viên, sidebar rút gọn)
    steward/              D01–D06 (shell cán bộ bảo vệ dữ liệu)
  api/                    Route Handlers trả mock theo đúng tên trường của ERD
  globals.css             design tokens (@theme) + base layer
components/
  ui/                     Button, StatusChip, Panel, DataTable, ImmutableRecord, …
  layout/                 AppShell, TopBar, SidebarNav, Brand
  advisor/                thành phần riêng của luồng cố vấn
i18n/                     routing.ts, request.ts, navigation.ts
lib/
  types.ts                kiểu domain bám ERD (PascalCase như schema)
  api.ts                  lớp truy cập dữ liệu duy nhất
  mock/                   dữ liệu mẫu
messages/{en,vi}/         từ điển theo namespace
proxy.ts                  chuyển hướng "/" → locale (Next 16 đổi tên middleware → proxy)
```

## Những quyết định thiết kế cần nhớ khi bảo vệ đồ án

1. **Trạng thái không bao giờ chỉ là màu.** `StatusChip` luôn gồm bộ ba token
   (chữ / nền / viền) và một nhãn chữ. Xem `components/ui/status-chip.tsx`.
2. **Thuộc tính không có quyền thì vắng mặt hoàn toàn**, không có giá trị bị che
   kiểu `***`, không có nút "xin quyền". So sánh A02 (có consent) với A09 (sau khi
   sinh viên rút) để thấy rõ khác biệt.
3. **Chiều sâu bằng viền, không bằng bóng đổ.** Chỉ dropdown và modal mới có
   shadow; mọi panel dùng viền 1px trên nền trắng.
4. **Bản ghi đã chốt không có nút sửa/xóa.** `ImmutableRecord` không nhận prop
   `onEdit`/`onDelete`; đính chính đi qua addendum (A11, S07).
5. **Trích dẫn giữ đúng chuỗi đã hiển thị.** `CitationList` in ra `DisplayedValue`
   kèm valid-time và transaction-time — đây là thứ cho phép tái dựng màn hình tại
   thời điểm ra quyết định (A07) và phân biệt với "dữ liệu hiện tại nói gì".
6. **Thiếu dữ liệu là một trạng thái hiển thị được.** Mỗi khối chứng cứ công bố
   "Sources used" và "Missing sources" ngang hàng nhau (`EvidenceSection`).
7. **Phạm vi dịch:** toàn bộ chrome giao diện có EN/VI. Nội dung bản ghi (ghi chú
   tư vấn, lời trình bày của sinh viên) đến từ API và giữ nguyên ngôn ngữ lúc
   soạn — đó là dữ liệu pháp lý bất biến, không phải chuỗi giao diện.

## Cắm backend thật

`lib/api.ts` là chỗ duy nhất biết dữ liệu đến từ đâu. Khi `AdviseCare_API` sẵn
sàng, đặt biến môi trường:

```
NEXT_PUBLIC_API_BASE_URL=https://api.advisecare.local
```

Payload trong `app/api/*` đã dùng đúng tên trường PascalCase của schema
(`StudentId`, `ConsentGrantId`, `RecordedAt`, `SupersededAt`…) nên không cần lớp
ánh xạ khi đổi sang API thật.
