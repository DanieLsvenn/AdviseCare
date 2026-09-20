import { getRequestConfig } from "next-intl/server";
import { hasLocale } from "next-intl";
import { routing } from "./routing";

/**
 * Từ điển được tách theo namespace (common, nav, auth, advisor, student, steward)
 * rồi ghép lại ở đây. Lý do: mỗi nhóm màn hình có một file riêng, tránh một
 * en.json khổng lồ mà cả nhóm phải sửa cùng lúc.
 *
 * Phạm vi dịch: toàn bộ "chrome" của giao diện — điều hướng, tiêu đề, nhãn cột,
 * nút, thông báo quy tắc. Nội dung bản ghi (advising note, justification của
 * sinh viên…) đến từ API và được giữ nguyên ngôn ngữ lúc soạn: đó là dữ liệu
 * pháp lý bất biến, không phải chuỗi giao diện để dịch lại.
 */
const namespaces = ["common", "nav", "auth", "advisor", "student", "steward"] as const;

export default getRequestConfig(async ({ requestLocale }) => {
  const requested = await requestLocale;
  const locale = hasLocale(routing.locales, requested)
    ? requested
    : routing.defaultLocale;

  const loaded = await Promise.all(
    namespaces.map(async (namespace) => [
      namespace,
      (await import(`../messages/${locale}/${namespace}.json`)).default,
    ]),
  );

  return {
    locale,
    messages: Object.fromEntries(loaded),
    timeZone: "Asia/Ho_Chi_Minh",
  };
});
