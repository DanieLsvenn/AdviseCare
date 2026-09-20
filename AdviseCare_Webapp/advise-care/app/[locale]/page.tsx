import { redirect } from "@/i18n/navigation";

/**
 * Gốc của mỗi locale không có màn riêng: AdviseCare bắt đầu từ màn đăng nhập
 * (C01), rồi mới rẽ theo vai trò. Chuyển hướng ở server nên người dùng không
 * bao giờ thấy một trang trung gian trống.
 */
export default async function LocaleRootPage({ params }: PageProps<"/[locale]">) {
  const { locale } = await params;
  redirect({ href: "/sign-in", locale });
}
