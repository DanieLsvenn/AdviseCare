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
    if (error instanceof ApiError && error.status === 401) {
      redirect(`/${locale}/sign-in`);
    }
    throw error;
  }
}
