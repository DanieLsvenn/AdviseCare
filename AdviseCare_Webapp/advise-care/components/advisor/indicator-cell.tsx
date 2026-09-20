import type { IndicatorCell as IndicatorCellData } from "@/lib/types";
import { StatusChip } from "@/components/ui/status-chip";

/**
 * Một ô chỉ số = chip có nhãn + dòng chi tiết. Bốn chiều chỉ số nằm ở bốn cột
 * tách biệt nên người đọc không thể vô tình cộng gộp chúng thành "điểm rủi ro".
 */
export function IndicatorCell({ cell }: { cell: IndicatorCellData }) {
  return (
    <div className="min-w-0">
      <StatusChip tone={cell.Tone} icon={cell.Icon} label={cell.Label} />
      <p className="mt-1 truncate font-body-sm text-body-sm text-on-surface-variant">
        {cell.Detail}
      </p>
    </div>
  );
}
