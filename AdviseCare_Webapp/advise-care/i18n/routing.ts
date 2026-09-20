import { defineRouting } from "next-intl/routing";

/**
 * EN là ngôn ngữ mặc định theo Design Rule §4.7; VI luôn có mặt qua bộ chuyển
 * ngôn ngữ ở header. Prefix "always" để mọi URL đều mang locale — cần thiết cho
 * một hệ thống có kiểm toán truy cập: link trong access log không được mơ hồ.
 */
export const routing = defineRouting({
  locales: ["en", "vi"],
  defaultLocale: "en",
  localePrefix: "always",
});

export type Locale = (typeof routing.locales)[number];
