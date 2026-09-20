import Link from "next/link";
import type { ComponentProps, ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

type Variant = "primary" | "secondary" | "destructive" | "tertiary";
type Size = "sm" | "md";

const variantClasses: Record<Variant, string> = {
  primary:
    "bg-primary-container text-on-primary hover:bg-primary active:bg-[#134e4a] border border-transparent",
  secondary:
    "bg-surface-container-lowest text-on-surface border border-outline-variant/60 hover:bg-surface-container-low hover:border-outline-variant",
  destructive:
    "bg-status-urgent-text text-white border border-transparent hover:bg-[#991b1b]",
  tertiary:
    "bg-transparent text-on-surface-variant border border-transparent hover:bg-surface-container-high hover:text-on-surface",
};

const sizeClasses: Record<Size, string> = {
  sm: "h-8 px-space-md text-label-md",
  md: "h-9 px-3.5 text-label-md",
};

const base =
  "inline-flex items-center justify-center gap-space-sm rounded font-medium whitespace-nowrap transition-colors disabled:cursor-not-allowed disabled:opacity-50";

interface CommonProps {
  variant?: Variant;
  size?: Size;
  icon?: string;
  iconAfter?: string;
  children?: ReactNode;
  className?: string;
}

export function Button({
  variant = "primary",
  size = "md",
  icon,
  iconAfter,
  children,
  className,
  ...rest
}: CommonProps & ComponentProps<"button">) {
  return (
    <button
      type="button"
      className={cn(base, variantClasses[variant], sizeClasses[size], className)}
      {...rest}
    >
      {icon ? <Icon name={icon} /> : null}
      {children}
      {iconAfter ? <Icon name={iconAfter} /> : null}
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
      {icon ? <Icon name={icon} /> : null}
      {children}
      {iconAfter ? <Icon name={iconAfter} /> : null}
    </Link>
  );
}
