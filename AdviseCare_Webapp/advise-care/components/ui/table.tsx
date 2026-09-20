import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/** Bảng sổ cái: hàng 40px, header 11px uppercase, phân cách bằng viền 1px. */
export function DataTable({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className="w-full overflow-x-auto">
      <table className={cn("w-full min-w-[720px] border-collapse text-left", className)}>
        {children}
      </table>
    </div>
  );
}

export function Th({
  children,
  className,
  align = "left",
}: {
  children?: ReactNode;
  className?: string;
  align?: "left" | "right" | "center";
}) {
  return (
    <th
      scope="col"
      className={cn(
        "border-b border-outline-variant/60 bg-surface-container-low px-space-md py-space-sm font-caption text-caption font-semibold uppercase tracking-[0.05em] text-on-surface-variant",
        align === "right" && "text-right",
        align === "center" && "text-center",
        className,
      )}
    >
      {children}
    </th>
  );
}

export function Td({
  children,
  className,
  align = "left",
}: {
  children?: ReactNode;
  className?: string;
  align?: "left" | "right" | "center";
}) {
  return (
    <td
      className={cn(
        "border-b border-outline-variant/25 px-space-md py-space-md align-top font-body-sm text-body-sm text-on-surface",
        align === "right" && "text-right",
        align === "center" && "text-center",
        className,
      )}
    >
      {children}
    </td>
  );
}

export function Tr({
  children,
  className,
  highlighted,
}: {
  children: ReactNode;
  className?: string;
  highlighted?: boolean;
}) {
  return (
    <tr
      className={cn(
        "transition-colors hover:bg-surface-container-low/70",
        highlighted && "bg-secondary-container/20",
        className,
      )}
    >
      {children}
    </tr>
  );
}
