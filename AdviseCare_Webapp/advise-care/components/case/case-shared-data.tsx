"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { ApiError, isRecord } from "@/lib/api-contract";
import { clientApiGet } from "@/lib/client-api";
import { formatDateTime } from "@/lib/format";
import { listOf, str, unwrap, type CaseRecord } from "@/components/case/case-api";
import { Button } from "@/components/ui/button";
import { Icon } from "@/components/ui/icon";
import { EmptyState, ErrorState, SkeletonRows } from "@/components/ui/states";
import { cn } from "@/lib/cn";

interface DataValue {
  dataAttributeId: string;
  attributeName: string;
  termName: string;
  value: string;
  provenanceNote: string;
  validFrom: string;
  isShared: boolean;
  isMissing: boolean;
  isErased: boolean;
}

function toValue(record: Record<string, unknown>): DataValue {
  return {
    dataAttributeId: str(record.dataAttributeId),
    attributeName: str(record.attributeName),
    termName: str(record.termName),
    value: record.value === null || record.value === undefined ? "" : str(record.value),
    provenanceNote: str(record.provenanceNote),
    validFrom: str(record.validFrom),
    isShared: record.isShared === true,
    isMissing: record.isMissing === true,
    isErased: record.isErased === true,
  };
}

/**
 * What the student shared with this request (CASE_ATTRIBUTE) and why.
 * The student sees the attributes and their own notes. The case's advisor
 * also sees the values, read through the access guard with this case's id
 * (so only attributes consented under the case category's purpose show a
 * value, and every value shown is logged).
 */
export function CaseSharedData({
  record,
  audience,
  canReadValues = false,
  className = "",
}: {
  record: CaseRecord;
  audience: "student" | "staff";
  canReadValues?: boolean;
  className?: string;
}) {
  const t = useTranslations("ui.caseShared");
  const [values, setValues] = useState<DataValue[]>([]);
  const [loading, setLoading] = useState(canReadValues);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    if (!canReadValues) return;
    setLoading(true);
    setError("");
    try {
      const payload = await clientApiGet<unknown>(
        `StudentData/students/${encodeURIComponent(record.studentId)}`,
        { query: { caseId: record.caseId } },
      );
      const data = unwrap(payload);
      const list = isRecord(data) && Array.isArray(data.values) ? listOf(data.values) : listOf(payload);
      setValues(list.map(toValue));
    } catch (cause) {
      setError(cause instanceof ApiError && cause.status < 500 && cause.message ? cause.message : t("loadError"));
    } finally {
      setLoading(false);
    }
  }, [canReadValues, record.caseId, record.studentId, t]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const notes = new Map(record.sharedAttributes.map((item) => [item.dataAttributeId, item]));
  const offeredIds = new Set(record.sharedAttributes.map((item) => item.dataAttributeId));

  // The advisor sees every value of the purpose, the attributes offered with this request first
  const rows = canReadValues
    ? [...values].sort(
        (a, b) =>
          Number(offeredIds.has(b.dataAttributeId)) - Number(offeredIds.has(a.dataAttributeId)) ||
          a.attributeName.localeCompare(b.attributeName),
      )
    : [];

  return (
    <section className={cn("rounded-lg border border-rule bg-paper p-space-lg", className)}>
      <div className="mb-space-md flex flex-wrap items-start justify-between gap-space-sm">
        <div className="flex items-start gap-space-sm">
          <Icon name="lab_profile" className="mt-0.5 text-[20px] text-ink" />
          <div>
            <h2 className="text-base font-semibold text-text">
              {audience === "student" ? t("titleStudent") : t("titleStaff")}
            </h2>
            <p className="mt-1 text-[0.8125rem] leading-5 text-text-muted">
              {audience === "student" ? t("introStudent") : canReadValues ? t("introAdvisor") : t("introStaff")}
            </p>
          </div>
        </div>
        {canReadValues ? (
          <Link
            href={`/advisor/students/${encodeURIComponent(record.studentId)}?caseId=${encodeURIComponent(record.caseId)}`}
            className="inline-flex items-center gap-1 text-sm font-semibold text-ink hover:underline"
          >
            {t("openFull")}
            <Icon name="chevron_right" className="text-[18px]" />
          </Link>
        ) : null}
      </div>

      {canReadValues ? (
        loading ? (
          <div className="rounded-md border border-rule">
            <SkeletonRows rows={3} columns={3} label={t("loading")} />
          </div>
        ) : error ? (
          <ErrorState
            title={error}
            action={
              <Button size="sm" variant="secondary" onClick={() => void load()}>
                {t("retry")}
              </Button>
            }
          />
        ) : rows.length ? (
          <ul className="divide-y divide-rule rounded-md border border-rule">
            {rows.map((row) => {
              const note = notes.get(row.dataAttributeId);
              return (
                <li key={`${row.dataAttributeId}-${row.termName}`} className="px-space-base py-space-sm">
                  <div className="flex flex-wrap items-baseline justify-between gap-x-space-md gap-y-1">
                    <p className="text-sm font-semibold text-text">
                      {row.attributeName}
                      {row.termName ? (
                        <span className="ml-space-sm text-[0.75rem] font-normal text-text-faint">{row.termName}</span>
                      ) : null}
                      {offeredIds.has(row.dataAttributeId) ? (
                        <span className="ml-space-sm text-[0.75rem] font-normal text-ink">{t("offered")}</span>
                      ) : null}
                    </p>
                    <p className="font-mono text-sm text-text">
                      {row.isErased
                        ? t("erased")
                        : !row.isShared
                          ? t("notShared")
                          : row.isMissing || !row.value
                            ? t("missing")
                            : row.value}
                    </p>
                  </div>
                  {row.isShared && row.provenanceNote ? (
                    <p className="mt-0.5 text-[0.75rem] text-text-faint">
                      {row.provenanceNote}
                      {row.validFrom ? ` · ${formatDateTime(row.validFrom)}` : ""}
                    </p>
                  ) : null}
                  {note?.shareReason ? (
                    <p className="mt-space-sm whitespace-pre-line rounded-md bg-paper-tint px-space-md py-space-sm text-sm leading-6 text-text">
                      <span className="mr-1 font-semibold">{t("studentNote")}</span>
                      {note.shareReason}
                    </p>
                  ) : null}
                </li>
              );
            })}
          </ul>
        ) : (
          <EmptyState icon="lab_profile" title={t("noValues")}>
            {t("noValuesHint")}
          </EmptyState>
        )
      ) : record.sharedAttributes.length ? (
        <ul className="divide-y divide-rule rounded-md border border-rule">
          {record.sharedAttributes.map((item) => (
            <li key={item.dataAttributeId} className="px-space-base py-space-sm">
              <p className="text-sm font-semibold text-text">{item.attributeName}</p>
              <p className="mt-0.5 whitespace-pre-line text-sm leading-6 text-text-muted">
                {item.shareReason || t("noNote")}
              </p>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-text-muted">{audience === "student" ? t("noneStudent") : t("noneStaff")}</p>
      )}
    </section>
  );
}
