import type { ComponentProps, ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./icon";

const control =
  "rounded border border-outline-variant bg-surface-container-lowest px-space-md font-body-sm text-body-sm text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary";

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
      <span className="mb-space-xs flex items-center gap-1 font-label-md text-label-md font-medium text-on-surface">
        {label}
        {required ? <span className="text-status-urgent-text">*</span> : null}
      </span>
      {children}
      {hint ? (
        <span className="mt-space-xs block font-body-sm text-body-sm text-on-surface-variant">
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
        className="pointer-events-none absolute left-space-md top-1/2 -translate-y-1/2 text-[18px] text-on-surface-variant"
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
        "size-4 shrink-0 rounded-sm border border-outline-variant text-primary accent-[#0f766e]",
        className,
      )}
      {...rest}
    />
  );
}
