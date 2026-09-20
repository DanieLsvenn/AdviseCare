import { cn } from "@/lib/cn";

/** Avatar là hình chữ nhật bo 4px — ẩn dụ "hồ sơ lưu trữ", không dùng hình tròn. */
export function Avatar({
  initials,
  tone = "primary",
  className,
}: {
  initials: string;
  tone?: "primary" | "urgent" | "neutral";
  className?: string;
}) {
  const toneClass = {
    primary: "bg-primary text-on-primary",
    urgent: "bg-status-urgent-surface text-status-urgent-text",
    neutral: "bg-surface-container-high text-on-surface-variant",
  }[tone];

  return (
    <span
      aria-hidden
      className={cn(
        "inline-flex h-9 w-9 shrink-0 items-center justify-center rounded font-label-md text-label-md font-semibold",
        toneClass,
        className,
      )}
    >
      {initials}
    </span>
  );
}
