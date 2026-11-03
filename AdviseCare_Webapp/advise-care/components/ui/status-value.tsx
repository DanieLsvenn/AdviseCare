import { useTranslations } from "next-intl";
import { humanize, stageFields, statusKey, statusTone } from "@/lib/status";
import { StatusChip } from "./status-chip";

/**
 * A domain status (case stage, priority, consent state…) rendered as a chip
 * with its translated label. Unknown values still get a readable label and a
 * neutral tone, never a bare code.
 */
export function StatusValue({
  value,
  field,
  size = "md",
}: {
  value: string;
  field?: string;
  size?: "sm" | "md";
}) {
  const t = useTranslations("ui.status");
  const key = statusKey(value);
  const label = t.has(key) ? t(key) : humanize(value);
  return (
    <StatusChip
      dot={field ? stageFields.has(field) : false}
      label={label}
      size={size}
      tone={statusTone(value) ?? "sealed"}
    />
  );
}
