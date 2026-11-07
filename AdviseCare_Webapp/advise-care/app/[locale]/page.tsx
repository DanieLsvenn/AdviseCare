import { ApiError } from "@/lib/api-contract";
import { getCurrentUser } from "@/lib/auth";
import { landingPathForRoles } from "@/lib/roles";
import { redirect } from "next/navigation";

export const dynamic = "force-dynamic";

/**
 * Gốc của mỗi locale không có màn riêng: AdviseCare bắt đầu từ màn đăng nhập
 * (C01), rồi mới rẽ theo vai trò. Chuyển hướng ở server nên người dùng không
 * bao giờ thấy một trang trung gian trống.
 */
export default async function LocaleRootPage({ params }: PageProps<"/[locale]">) {
  const { locale } = await params;
  try {
    const user = await getCurrentUser();
    redirect(`/${locale}${landingPathForRoles(user.roles)}`);
  } catch (error) {
    // 401: phiên hết hạn; 404: token còn hạn nhưng tài khoản không còn (vd. DB vừa nạp lại mock data)
    if (error instanceof ApiError && (error.status === 401 || error.status === 404)) {
      redirect(`/${locale}/sign-in?reason=expired`);
    }
    // 403: tài khoản bị khoá/vô hiệu hoá, hoặc token trỏ tới tài khoản không còn tồn tại
    if (error instanceof ApiError && error.status === 403) {
      redirect(`/${locale}/sign-in?reason=account`);
    }
    throw error;
  }
}
