# AdviseCare Web App

Frontend của AdviseCare Academic Advising & Student Welfare Management System.
Giao diện hiện tại gọi API .NET thật; các Route Handler mock của prototype đã
được loại bỏ để không che khuất API backend.

## Ngăn xếp

- Next.js 16 App Router và React 19
- Tailwind CSS v4
- `next-intl` với tiếng Anh và tiếng Việt
- TypeScript

## Chạy dự án

```powershell
pnpm install
pnpm dev:local-api
pnpm lint
pnpm build
```

`.env.local` đã được cấu hình cho API phát triển tại
`https://localhost:7286`. Nếu cần đổi môi trường, đặt
`ADVISECARE_API_BASE_URL` thành origin của ASP.NET Core API, không bao gồm
đường dẫn `/api`; ví dụ:

```dotenv
ADVISECARE_API_BASE_URL=https://localhost:7001
```

Trên Windows, dùng `pnpm dev:local-api` để chạy với API cục bộ. Lệnh này chỉ
xuất chứng chỉ công khai của ASP.NET Core vào thư mục tạm và cấu hình Node tin
cậy chứng chỉ đó; xác thực TLS vẫn được bật. Nếu cần chọn cổng frontend khác,
chạy `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev-local-api.ps1 -Port 3100`.

Next.js chuyển tiếp yêu cầu `/api/*` qua Route Handler
`app/api/[...path]/route.ts`. URL upstream được tạo thành
`{ADVISECARE_API_BASE_URL}/api/{path}`. Biến này chỉ được đọc phía server và
không nên dùng tiền tố `NEXT_PUBLIC_`.

Đăng nhập bằng Google cần thêm Client ID (OAuth Web client) của Google
Identity Services; nếu không đặt biến này, trang đăng nhập chỉ hiện form mật
khẩu:

```dotenv
NEXT_PUBLIC_GOOGLE_CLIENT_ID=xxxxxxxx.apps.googleusercontent.com
```

## Thiết kế

Hệ thiết kế "Sổ liên lạc" được mô tả trong `DESIGN.md` (bảng màu, font Source
Sans 3 / Source Code Pro, mật độ, mẫu "mục có chữ ký" cho mọi lịch sử chỉ
ghi thêm). `PRODUCT.md` tóm tắt sự thật sản phẩm từ SRS mà mọi màn phải tôn
trọng. Thành phần dùng chung nằm trong `components/ui`; màn danh sách sinh tự
động từ `components/workspace/workspace-definitions.ts`.

Các luồng chính có màn thiết kế riêng:

| Luồng | Route |
|---|---|
| Đăng nhập, liên kết Google, đặt lại mật khẩu | `/sign-in`, `/link-account?token=`, `/reset-password?token=` |
| Sinh viên gửi yêu cầu hỗ trợ | `/me/support-request` |
| Sinh viên xem một yêu cầu | `/me/cases/[caseId]` |
| Hàng đợi ghép cố vấn (Programme Head) | `/head`, `/head/matching` |
| Chi tiết case của cố vấn | `/advisor/cases/[caseId]` |

## Tích hợp API và phiên đăng nhập

- Đăng nhập, liên kết Google, khôi phục mật khẩu và thao tác nghiệp vụ gọi API
  backend qua BFF cùng origin.
- BFF lưu token trong cookie `HttpOnly`, không trả token trong payload đăng
  nhập, kiểm tra nguồn gốc của yêu cầu ghi và xóa cookie khi phiên hết hạn.
- Server Components lấy người dùng hiện tại bằng `Auth/me`; các layout vai trò
  kiểm tra quyền trước khi hiển thị workspace.
- Các thao tác CRUD, tìm kiếm, tải lên và xuất dữ liệu dùng DTO/endpoint hiện
  hành của API. Không có dữ liệu mock làm phương án thay thế khi API không khả
  dụng; lỗi cấu hình hoặc kết nối được hiển thị rõ trên giao diện.

## Workspace

- **Quản trị:** tài khoản và vai trò; khoa, chương trình, môn học và học kỳ;
  nhập dữ liệu sinh viên; danh mục hỗ trợ và thuộc tính; lịch sử hoạt động.
- **Cố vấn:** danh sách ca, chi tiết và lịch sử ca; yêu cầu đồng ý; đính chính;
  chuyển tuyến; truy cập khẩn cấp; dữ liệu sinh viên theo thời điểm và đối chiếu.
- **Sinh viên:** hồ sơ, ca hỗ trợ và biểu mẫu gửi ca; đồng ý và yêu cầu đồng ý;
  yêu cầu đính chính; lịch sử truy cập; hoạt động; xuất dữ liệu cá nhân.
- **Cán bộ bảo vệ dữ liệu:** mục đích đồng ý, chính sách truy cập, thuộc tính,
  lịch sử đồng ý, nhật ký truy cập, yêu cầu xóa, truy cập khẩn cấp và khóa mã hóa.
- **Công tác xã hội:** ca, chuyển tuyến đến và từ dịch vụ bên ngoài, dịch vụ và
  can thiệp.
- **Trưởng chương trình:** hàng đợi ghép cố vấn (gợi ý cố vấn, ghép một chạm),
  yêu cầu đổi cố vấn, yêu cầu chuyển cấp, tất cả case, báo cáo ca và khối
  lượng cố vấn.

Thông báo và cài đặt tài khoản dùng chung AppShell đã xác thực. Các workspace
hiển thị kết quả trực tiếp từ API và cung cấp lọc, phân trang, biểu mẫu thao
tác, trạng thái đang tải/lỗi và chi tiết phản hồi khi endpoint trả dữ liệu.

## Giới hạn theo API hiện tại

Các màn thống kê không có endpoint tương ứng được ghi rõ là chưa khả dụng thay
vì trình bày dữ liệu giả. Kiểm thử giao diện với dữ liệu thật cần API đang chạy,
tài khoản/role phù hợp và `ADVISECARE_API_BASE_URL` được cấu hình trong môi
trường.
