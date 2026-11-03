import { useTranslations } from "next-intl";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/icon";

const PIPELINE = ["pending", "assigned", "contacted", "followUp", "closed"] as const;

/**
 * Where a case is in its pipeline (pending → assigned → contacted →
 * follow-up → closed). "Referred" is a detour, shown on the step the case
 * left from. Each step is labelled; the current one is also announced.
 */
export function CaseProgress({
  stage,
  className,
}: {
  stage: string;
  className?: string;
}) {
  const t = useTranslations("ui");
  const referred = stage === "referred";
  // A referred case sits between "assigned" and "contacted" in practice.
  const currentIndex = referred
    ? 1
    : Math.max(0, PIPELINE.indexOf(stage as (typeof PIPELINE)[number]));
  const closed = stage === "closed";

  return (
    <ol
      aria-label={t("case.progress")}
      className={cn("grid grid-cols-5 gap-1.5", className)}
    >
      {PIPELINE.map((step, index) => {
        const done = index < currentIndex || (closed && index === currentIndex);
        const current = index === currentIndex && !closed;
        return (
          <li key={step} aria-current={current ? "step" : undefined} className="min-w-0">
            <span
              aria-hidden
              className={cn(
                "block h-1.5 rounded-full",
                done ? "bg-ink" : current ? (referred ? "bg-status-referral-text" : "bg-ink") : "bg-rule",
              )}
            />
            <span
              className={cn(
                "mt-2 flex items-center gap-1 text-[0.8125rem] leading-4",
                current ? "font-semibold text-text" : done ? "text-text-muted" : "text-text-faint",
              )}
            >
              {done ? <Icon name="check" className="text-[14px] text-ink" /> : null}
              <span className="truncate">{t(`case.step.${step}`)}</span>
            </span>
            {current && referred ? (
              <span className="mt-0.5 block text-[0.75rem] font-semibold text-status-referral-text">
                {t("status.referred")}
              </span>
            ) : null}
          </li>
        );
      })}
    </ol>
  );
}
