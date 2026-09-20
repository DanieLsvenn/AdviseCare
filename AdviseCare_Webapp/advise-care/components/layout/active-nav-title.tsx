"use client";

import { usePathname } from "@/i18n/navigation";
import type { NavItem } from "./sidebar-nav";

/**
 * Tiêu đề trên utility header phản ánh mục điều hướng đang mở, còn tiêu đề chi
 * tiết của từng màn nằm trong PageHeader. Tách hai tầng giúp header giữ nguyên
 * khi điều hướng trong cùng một mục (Design Rule §8).
 */
export function ActiveNavTitle({
  items,
  fallback,
}: {
  items: NavItem[];
  fallback: string;
}) {
  const pathname = usePathname();
  const match = items
    .filter((item) => pathname === item.href || pathname.startsWith(`${item.href}/`))
    .sort((a, b) => b.href.length - a.href.length)[0];

  return <>{match?.label ?? fallback}</>;
}
