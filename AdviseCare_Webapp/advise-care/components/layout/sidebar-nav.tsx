"use client";

import { cn } from "@/lib/cn";
import { Link, usePathname } from "@/i18n/navigation";
import { Icon } from "@/components/ui/icon";

export interface NavItem {
  href: string;
  label: string;
  icon: string;
  badge?: "dot" | number;
}

export function SidebarNav({ items }: { items: NavItem[] }) {
  const pathname = usePathname();

  return (
    <nav className="flex flex-col gap-space-2xs p-space-sm">
      {items.map((item) => {
        const isActive =
          pathname === item.href || pathname.startsWith(`${item.href}/`);

        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={isActive ? "page" : undefined}
            className={cn(
              "flex items-start gap-space-sm rounded px-space-md py-space-sm font-label-md text-label-md transition-colors",
              isActive
                ? "bg-surface-container-high font-semibold text-primary"
                : "text-on-surface-variant hover:bg-surface-container-high hover:text-on-surface",
            )}
          >
            <Icon name={item.icon} className="mt-0.5 text-[20px]" />
            {/* Nhãn được phép xuống dòng: tên mục tiếng Việt dài hơn tiếng Anh
                và một menu bị cắt chữ là menu không đọc được. */}
            <span className="min-w-0 flex-1 whitespace-normal break-words lg:whitespace-normal">
              {item.label}
            </span>
            {item.badge === "dot" ? (
              <span aria-hidden className="mt-1.5 size-1.5 shrink-0 rounded-full bg-status-urgent-text" />
            ) : typeof item.badge === "number" ? (
              <span className="rounded bg-surface-container-high px-1.5 py-0.5 font-caption text-caption text-on-surface-variant">
                {item.badge}
              </span>
            ) : null}
          </Link>
        );
      })}
    </nav>
  );
}
