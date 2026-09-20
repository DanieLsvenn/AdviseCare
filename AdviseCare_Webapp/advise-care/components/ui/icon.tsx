import { cn } from "@/lib/cn";

interface IconProps {
  name: string;
  className?: string;
  filled?: boolean;
  /** Icon luôn là trang trí cạnh một nhãn chữ, nên mặc định ẩn với screen reader. */
  label?: string;
}

export function Icon({ name, className, filled, label }: IconProps) {
  return (
    <span
      aria-hidden={label ? undefined : true}
      aria-label={label}
      role={label ? "img" : undefined}
      className={cn(
        "material-symbols-outlined select-none",
        filled && "filled",
        className ?? "text-[18px]",
      )}
    >
      {name}
    </span>
  );
}
