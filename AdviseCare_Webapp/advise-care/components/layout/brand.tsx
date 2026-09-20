import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/icon";

/** Emblem thể chế: khối vuông bo 4px, không dùng ảnh — tránh phụ thuộc asset ngoài. */
export function Brand({
  subtitle,
  compact,
  className,
}: {
  subtitle: string;
  compact?: boolean;
  className?: string;
}) {
  return (
    <div className={cn("flex min-w-0 items-center gap-space-sm", className)}>
      <span className="flex size-8 shrink-0 items-center justify-center rounded bg-primary text-on-primary">
        <Icon name="school" className="text-[20px]" filled />
      </span>
      <div className="min-w-0">
        <p
          className={cn(
            "truncate font-headline-sm text-headline-sm leading-none text-on-surface",
            compact && "text-label-md font-headline-sm",
          )}
        >
          AdviseCare
        </p>
        <p className="mt-0.5 truncate font-caption text-caption leading-tight text-on-surface-variant">
          {subtitle}
        </p>
      </div>
    </div>
  );
}
