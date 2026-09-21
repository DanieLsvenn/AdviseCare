import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { Icon, Panel, PanelBody, PanelHeader, StatusChip } from "@/components/ui";

export function WelfareHeader({ title, subtitle, actions }: { title: ReactNode; subtitle?: ReactNode; actions?: ReactNode }) {
  return <div className="flex flex-wrap items-start justify-between gap-space-md border-b border-outline-variant/30 pb-space-md"><div><p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">Welfare Workspace / Purpose-bound operational record</p><h1 className="mt-space-xs font-headline-lg text-headline-lg text-on-surface">{title}</h1>{subtitle ? <p className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">{subtitle}</p> : null}</div>{actions ? <div className="flex flex-wrap gap-space-sm">{actions}</div> : null}</div>;
}

export function WelfareMetric({ label, value, detail, tone = "primary", icon }: { label: string; value: string; detail: string; tone?: "primary" | "urgent" | "attention"; icon: string }) {
  return <div className="border border-outline-variant/40 bg-surface-container-lowest px-space-md py-space-base"><div className="flex items-center justify-between"><span className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">{label}</span><Icon name={icon} className={cn("text-[20px]", tone === "urgent" ? "text-status-urgent-text" : tone === "attention" ? "text-status-attention-text" : "text-primary")} /></div><p className="mt-space-sm font-display-sm text-display-sm text-on-surface">{value}</p><p className="mt-space-xs font-caption text-caption text-on-surface-variant">{detail}</p></div>;
}

export function ScopeCard({ title, body, allowed = true }: { title: string; body: string; allowed?: boolean }) {
  return <div className={cn("border px-space-md py-space-base", allowed ? "border-status-ontrack-border bg-status-ontrack-surface/30" : "border-outline-variant/40 bg-surface-container-low")}><div className="flex items-start justify-between gap-space-sm"><div className="flex items-start gap-space-sm"><Icon name={allowed ? "verified" : "block"} className={cn("text-[18px]", allowed ? "text-status-ontrack-text" : "text-on-surface-variant")} /><p className="font-label-md text-label-md font-semibold">{title}</p></div><StatusChip tone={allowed ? "ontrack" : "sealed"} label={allowed ? "Permitted" : "Withheld"} /></div><p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">{body}</p></div>;
}

export function TimelineEntry({ icon, title, meta, children, tone = "primary" }: { icon: string; title: string; meta: string; children: ReactNode; tone?: "primary" | "urgent" }) {
  return <div className="relative flex gap-space-md border-l-2 border-surface-container-high pl-space-lg"><span className={cn("absolute -left-[13px] top-0 flex size-6 items-center justify-center rounded-full text-on-primary", tone === "urgent" ? "bg-status-urgent-text" : "bg-primary")}><Icon name={icon} className="text-[13px]" /></span><div className="min-w-0 pb-space-md"><div className="flex flex-wrap items-baseline justify-between gap-space-sm"><p className="font-label-md text-label-md font-semibold">{title}</p><span className="font-code-audit text-code-audit text-on-surface-variant">{meta}</span></div><div className="mt-space-xs font-body-sm text-body-sm text-on-surface-variant">{children}</div></div></div>;
}

export function WelfareSection({ title, icon = "assignment", children, actions }: { title: string; icon?: string; children: ReactNode; actions?: ReactNode }) {
  return <Panel><PanelHeader icon={icon} title={title} actions={actions} /><PanelBody>{children}</PanelBody></Panel>;
}
