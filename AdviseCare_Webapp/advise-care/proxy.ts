import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";

/**
 * Next.js 16 đổi tên `middleware` thành `proxy`. Lớp này chỉ làm một việc:
 * chuyển hướng "/" sang locale phù hợp và bảo đảm mọi route đều có prefix.
 */
export default createMiddleware(routing);

export const config = {
  matcher: "/((?!api|_next|_vercel|.*\\..*).*)",
};
