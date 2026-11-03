import Link from "next/link";
import type { ComponentProps, ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

type Variant = "primary" | "secondary" | "quiet" | "destructive" | "tertiary";
type Size = "sm" | "md" | "lg";

/* Ballpoint-ink primary; everything else stays on the page's paper. */
const variantClasses: Record<Variant, string> = {
  primary:
    "border-transparent bg-ink text-white hover:bg-ink-hover active:bg-cover disabled:bg-rule-strong disabled:text-white",
  secondary:
    "border-rule-strong bg-paper text-text hover:border-text-faint hover:bg-paper-tint active:bg-ink-wash",
  quiet:
    "border-transparent bg-transparent text-ink hover:bg-ink-wash active:bg-ink-wash",
  tertiary:
    "border-transparent bg-transparent text-text-muted hover:bg-ink-wash hover:text-text",
  destructive:
    "border-transparent bg-red-ink text-white hover:bg-[#8c0f18] active:bg-[#700b13]",
};

const sizeClasses: Record<Size, string> = {
  sm: "h-8 gap-1.5 px-3 text-[0.8125rem]",
  md: "h-9 gap-2 px-3.5 text-sm",
  lg: "h-11 gap-2 px-5 text-base",
};

const base =
  "inline-flex shrink-0 items-center justify-center rounded-md border font-semibold whitespace-nowrap transition-colors duration-150 disabled:cursor-not-allowed disabled:opacity-60";

interface CommonProps {
  variant?: Variant;
  size?: Size;
  icon?: string;
  iconAfter?: string;
  loading?: boolean;
  children?: ReactNode;
  className?: string;
}

function Content({
  icon,
  iconAfter,
  loading,
  children,
}: Pick<CommonProps, "icon" | "iconAfter" | "loading" | "children">) {
  return (
    <>
      {loading ? (
        <Icon name="progress_activity" className="animate-spin text-[18px]" />
      ) : icon ? (
        <Icon name={icon} className="text-[18px]" />
      ) : null}
      {children}
      {iconAfter ? <Icon name={iconAfter} className="text-[18px]" /> : null}
    </>
  );
}

export function Button({
  variant = "primary",
  size = "md",
  icon,
  iconAfter,
  loading,
  children,
  className,
  disabled,
  ...rest
}: CommonProps & ComponentProps<"button">) {
  return (
    <button
      type="button"
      aria-busy={loading || undefined}
      disabled={disabled || loading}
      className={cn(base, variantClasses[variant], sizeClasses[size], className)}
      {...rest}
    >
      <Content icon={icon} iconAfter={iconAfter} loading={loading}>
        {children}
      </Content>
    </button>
  );
}

export function ButtonLink({
  variant = "primary",
  size = "md",
  icon,
  iconAfter,
  children,
  className,
  href,
  ...rest
}: CommonProps & ComponentProps<typeof Link>) {
  return (
    <Link
      href={href}
      className={cn(base, variantClasses[variant], sizeClasses[size], className)}
      {...rest}
    >
      <Content icon={icon} iconAfter={iconAfter}>
        {children}
      </Content>
    </Link>
  );
}
