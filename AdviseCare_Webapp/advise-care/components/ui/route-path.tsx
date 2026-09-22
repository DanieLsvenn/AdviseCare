"use client";

import { usePathname } from "@/i18n/navigation";
import { cn } from "@/lib/cn";

export function RoutePath({ className }: { className?: string }) {
  const pathname = usePathname();
  const segments = pathname.split("/").filter(Boolean);

  return (
    <nav
      aria-label="Current route"
      className={cn(
        "flex flex-wrap items-center gap-space-sm font-code-audit text-code-audit text-on-surface-variant",
        className,
      )}
    >
      {segments.map((segment, index) => (
        <span
          key={`${segment}-${index}`}
          className="flex items-center gap-space-sm"
        >
          {index > 0 ? <span className="text-outline-variant">/</span> : null}
          <span
            className={cn(
              index === segments.length - 1 && "font-semibold text-primary",
            )}
          >
            {segment}
          </span>
        </span>
      ))}
    </nav>
  );
}
