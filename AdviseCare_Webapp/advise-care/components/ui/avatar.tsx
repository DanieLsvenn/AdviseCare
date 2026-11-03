import { cn } from "@/lib/cn";

/** People are circles with their initials in ink on a pale wash. */
export function Avatar({
  initials,
  tone = "primary",
  className,
}: {
  initials: string;
  tone?: "primary" | "urgent" | "neutral" | "cover";
  className?: string;
}) {
  const toneClass = {
    primary: "bg-ink-wash text-ink",
    urgent: "bg-status-urgent-surface text-status-urgent-text",
    neutral: "bg-surface-container text-text-muted",
    cover: "bg-cover-raised text-white ring-1 ring-cover-line",
  }[tone];

  return (
    <span
      aria-hidden
      className={cn(
        "inline-flex size-9 shrink-0 items-center justify-center rounded-full text-[0.8125rem] font-semibold",
        toneClass,
        className,
      )}
    >
      {initials}
    </span>
  );
}
