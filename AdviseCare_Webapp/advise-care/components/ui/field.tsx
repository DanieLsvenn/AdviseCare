import type { ComponentProps, ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

export const controlClass =
  "rounded-md border border-rule-strong bg-paper px-3 text-sm text-text placeholder:text-text-faint transition-colors hover:border-text-faint focus:border-ink focus:outline-none focus:ring-2 focus:ring-ink/20 disabled:cursor-not-allowed disabled:bg-paper-tint disabled:text-text-faint aria-[invalid=true]:border-red-ink";
const control = controlClass;

export function Field({
  label,
  hint,
  required,
  children,
  className,
}: {
  label: ReactNode;
  hint?: ReactNode;
  required?: boolean;
  children: ReactNode;
  className?: string;
}) {
  return (
    <label className={cn("block", className)}>
      <span className="mb-1.5 flex items-center gap-1 text-sm font-semibold text-text">
        {label}
        {required ? <span aria-hidden className="text-red-ink">*</span> : null}
      </span>
      {children}
      {hint ? (
        <span className="mt-1 block text-[0.8125rem] leading-5 text-text-faint">
          {hint}
        </span>
      ) : null}
    </label>
  );
}

export function TextInput({ className, ...rest }: ComponentProps<"input">) {
  return <input className={cn(control, "h-9 w-full", className)} {...rest} />;
}

export function TextArea({ className, ...rest }: ComponentProps<"textarea">) {
  return <textarea className={cn(control, "w-full py-space-sm", className)} {...rest} />;
}

export function Select({ className, children, ...rest }: ComponentProps<"select">) {
  return (
    <select className={cn(control, "h-9 max-w-full", className)} {...rest}>
      {children}
    </select>
  );
}

export function SearchInput({
  className,
  ...rest
}: ComponentProps<"input"> & { className?: string }) {
  return (
    <div className={cn("relative", className)}>
      <Icon
        name="search"
        className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[18px] text-text-faint"
      />
      <input className={cn(control, "h-9 w-full pl-9")} {...rest} />
    </div>
  );
}

export function Checkbox({ className, ...rest }: ComponentProps<"input">) {
  return (
    <input
      type="checkbox"
      className={cn(
        "size-4 shrink-0 rounded-sm border border-rule-strong accent-[#2142b8]",
        className,
      )}
      {...rest}
    />
  );
}
