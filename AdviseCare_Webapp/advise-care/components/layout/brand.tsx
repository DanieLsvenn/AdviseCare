import { cn } from "@/lib/cn";

/**
 * The AdviseCare mark: a contact book seen from the front — the cover, its
 * spine and two ruled lines. Drawn in currentColor so it sits on the blue
 * cover (white) or on paper (ink).
 */
export function BrandMark({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden
      viewBox="0 0 32 32"
      className={cn("size-8 shrink-0", className)}
      fill="none"
    >
      <rect x="5" y="3.5" width="22" height="25" rx="3" stroke="currentColor" strokeWidth="2" />
      <path d="M10 3.5v25" stroke="currentColor" strokeWidth="2" />
      <path d="M14 11h9M14 15.5h9M14 20h5.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  );
}

export function Brand({
  subtitle,
  tone = "cover",
  className,
}: {
  subtitle?: string;
  tone?: "cover" | "paper";
  /** @deprecated kept for older call sites */
  compact?: boolean;
  className?: string;
}) {
  return (
    <div className={cn("flex min-w-0 items-center gap-2.5", className)}>
      <BrandMark className={tone === "cover" ? "text-white" : "text-ink"} />
      <div className="min-w-0 leading-none">
        <p
          className={cn(
            "truncate text-[1.0625rem] font-bold tracking-[-0.01em]",
            tone === "cover" ? "text-white" : "text-text",
          )}
        >
          AdviseCare
        </p>
        {subtitle ? (
          <p
            className={cn(
              "mt-1 truncate text-[0.75rem]",
              tone === "cover" ? "text-cover-ink" : "text-text-faint",
            )}
          >
            {subtitle}
          </p>
        ) : null}
      </div>
    </div>
  );
}
