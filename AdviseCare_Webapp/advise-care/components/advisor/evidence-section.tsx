import { getTranslations } from "next-intl/server";
import {
  ButtonLink,
  Icon,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  StatusChip,
} from "@/components/ui";
import type { ProfileSection } from "@/lib/mock/profile";

/**
 * Một khối chứng cứ = số liệu + danh sách cờ + công bố nguồn.
 * Phần "Missing sources" nằm ngang hàng với "Sources used", không phải chú thích
 * mờ ở cuối: thiếu dữ liệu là một trạng thái phải đọc được, không phải ghi chú nhỏ.
 */
export async function EvidenceSection({ section }: { section: ProfileSection }) {
  const t = await getTranslations("advisor.profile");

  return (
    <Panel>
      <PanelHeader
        icon={section.icon}
        title={`${section.index}. ${section.title}`}
        actions={
          <StatusChip
            tone={section.headlineChip.tone}
            icon={section.headlineChip.icon}
            label={section.headlineChip.label}
          />
        }
      />

      <PanelBody className="space-y-space-lg">
        <div className="grid gap-space-md sm:grid-cols-2">
          {section.metrics.map((metric) => (
            <div
              key={metric.label}
              className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 px-space-base py-space-md"
            >
              <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                {metric.label}
              </p>
              <p className="mt-1 flex items-baseline gap-1.5">
                <span className="font-display-sm text-display-sm text-on-surface">
                  {metric.value}
                </span>
                {metric.unit ? (
                  <span className="font-body-sm text-body-sm text-on-surface-variant">
                    {metric.unit}
                  </span>
                ) : null}
              </p>
              <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                {metric.caption}
              </p>
            </div>
          ))}
        </div>

        <div>
          <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
            {section.listTitle}
          </h3>
          <ul className="mt-space-sm space-y-space-sm">
            {section.flags.map((flag) => (
              <li
                key={flag.title}
                className="flex flex-wrap items-start justify-between gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md"
              >
                <div className="min-w-0 flex-1">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">
                    {flag.title}
                  </p>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    {flag.detail}
                  </p>
                </div>
                <StatusChip tone={flag.tone} label={flag.chipLabel} />
              </li>
            ))}
          </ul>
        </div>

        {section.note ? (
          <p className="rounded-md border-l-2 border-l-outline-variant bg-surface-container-low/60 px-space-base py-space-md font-body-sm text-body-sm text-on-surface-variant">
            {section.note}
          </p>
        ) : null}

        <ul className="space-y-space-sm border-t border-outline-variant/30 pt-space-md">
          {section.sources.map((source) => (
            <li
              key={source.label}
              className="flex items-start gap-space-sm font-body-sm text-body-sm"
            >
              <Icon
                name={source.kind === "USED" ? "check_circle" : "hourglass_top"}
                className={
                  source.kind === "USED"
                    ? "mt-0.5 text-[16px] text-status-ontrack-text"
                    : "mt-0.5 text-[16px] text-status-attention-text"
                }
              />
              <span className="text-on-surface-variant">
                <strong className="font-semibold text-on-surface">
                  {source.kind === "USED" ? t("sourcesUsed") : t("sourcesMissing")}:
                </strong>{" "}
                {source.label}
              </span>
            </li>
          ))}
        </ul>
      </PanelBody>

      <PanelFooter>
        <span className="font-code-audit text-code-audit">
          {section.nodeLabel}: {section.nodeRef}
        </span>
        <ButtonLink
          href={section.evidenceHref}
          variant="tertiary"
          size="sm"
          iconAfter="arrow_forward"
        >
          {t("viewEvidence")}
        </ButtonLink>
      </PanelFooter>
    </Panel>
  );
}
