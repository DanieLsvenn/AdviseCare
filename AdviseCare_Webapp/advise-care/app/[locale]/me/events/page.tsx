import { getTranslations, setRequestLocale } from "next-intl/server";
import {
  Button,
  ButtonLink,
  Icon,
  Notice,
  PageHeader,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  Select,
  StatusChip,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { CampusEvent } from "@/lib/types";

/** S09 — Events. */
export default async function EventsPage({ params }: PageProps<"/[locale]/me/events">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.events");
  const registered = (events: CampusEvent[]) => events.filter((event) => event.Registered).length;

  const { events } = await apiGet<{ events: CampusEvent[] }>("/api/events");

  return (
    <>
      <PageHeader
        eyebrow={["Decree 13/2023/ND-CP", "Institutional rule 2"]}
        title={t("title")}
        badge={<StatusChip tone="sealed" icon="link_off" label={t("decoupled")} />}
        meta={
          <div className="flex flex-wrap items-center gap-space-sm">
            <StatusChip
              tone="referral"
              icon="calendar_month"
              label={t("available", { count: events.length })}
            />
            <StatusChip
              tone="ontrack"
              icon="bookmark_added"
              label={t("confirmed", { count: registered(events) })}
            />
          </div>
        }
        actions={
          <ButtonLink href="/me/consent" variant="secondary" size="sm" iconAfter="arrow_forward">
            {t("manageConsent")}
          </ButtonLink>
        }
      />

      <Notice tone="sealed" icon="gavel" title={t("architectureTitle")}>
        <p>{t("architectureBody")}</p>
        <p className="mt-space-sm flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="ontrack" icon="verified" label={t("invariant")} />
          <StatusChip tone="sealed" icon="dns" label={t("demoData")} />
        </p>
      </Notice>

      <Panel>
        <div className="flex flex-wrap items-center gap-space-md px-space-lg py-space-base">
          <Select aria-label={t("allCategories")} defaultValue="all" className="w-auto">
            <option value="all">{t("allCategories")}</option>
            <option value="academic">Academic skills & strategy</option>
            <option value="career">Career & industry advisory</option>
            <option value="wellbeing">Health, wellbeing & resilience</option>
            <option value="governance">Institutional governance & ethics</option>
          </Select>
          <Select aria-label={t("dateRange")} defaultValue="range" className="w-auto">
            <option value="range">{t("dateRange")}</option>
            <option value="7">Next 7 days</option>
            <option value="30">Next 30 days</option>
            <option value="past">Past archive</option>
          </Select>
          <Select aria-label={t("allFormats")} defaultValue="all" className="w-auto">
            <option value="all">{t("allFormats")}</option>
            <option value="in-person">In-person only</option>
            <option value="hybrid">Hybrid (simulcast)</option>
          </Select>
          <Button variant="secondary" size="sm" icon="restart_alt">
            {t("resetFilters")}
          </Button>
          <span className="ml-auto font-body-sm text-body-sm text-on-surface-variant">
            {t("viewMode")}
          </span>
        </div>
      </Panel>

      <div className="grid gap-space-md xl:grid-cols-2">
        {events.map((event) => {
          const free = event.SeatsTotal - event.SeatsTaken;
          const percent = Math.round((event.SeatsTaken / event.SeatsTotal) * 100);

          return (
            <Panel key={event.EventId}>
              <PanelHeader
                icon="school"
                title={event.Title}
                subtitle={event.Category}
                actions={
                  <StatusChip
                    tone={event.Registered ? "ontrack" : free === 0 ? "sealed" : "referral"}
                    icon={event.Registered ? "how_to_reg" : free === 0 ? "block" : "event_available"}
                    label={
                      event.Registered
                        ? t("registered")
                        : free === 0
                          ? t("full")
                          : t("openForRegistration")
                    }
                  />
                }
              />
              <PanelBody className="space-y-space-base">
                <p className="font-body-sm text-body-sm text-on-surface-variant">{event.Summary}</p>

                <ul className="space-y-space-xs font-body-sm text-body-sm text-on-surface-variant">
                  {[
                    { icon: "schedule", label: `${event.StartsAt} – ${event.EndsAt.slice(-9)}` },
                    { icon: "pin_drop", label: event.LocationLabel },
                    { icon: "record_voice_over", label: event.HostLabel },
                  ].map((row) => (
                    <li key={row.label} className="flex items-center gap-1.5">
                      <Icon name={row.icon} className="text-[14px]" />
                      {row.label}
                    </li>
                  ))}
                </ul>

                <div>
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-caption text-caption uppercase tracking-wider text-on-surface-variant">
                      {t("seatAllocation")}
                    </p>
                    <p className="font-body-sm text-body-sm text-on-surface-variant">
                      {t("seatsFilled", { taken: event.SeatsTaken, total: event.SeatsTotal })}
                    </p>
                  </div>
                  <span className="mt-space-sm block h-1.5 w-full overflow-hidden rounded-full bg-surface-container-high">
                    <span
                      className="block h-full bg-primary"
                      style={{ width: `${percent}%` }}
                    />
                  </span>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    {t("seatsFree", { free, total: event.SeatsTotal })}
                  </p>
                </div>

                <div className="rounded-md border border-outline-variant/40 bg-surface-container-low/40 px-space-base py-space-md">
                  <p className="flex items-center gap-1.5 font-body-sm text-body-sm text-on-surface-variant">
                    <Icon name="lock" className="text-[14px]" />
                    {event.Registered
                      ? `${t("bookingRef")}: REG-EVT-90412`
                      : t("notRegistered")}
                  </p>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    {t("notLinked")}
                  </p>
                  <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                    {event.ConsentNote}
                  </p>
                </div>
              </PanelBody>
              <PanelFooter>
                <span className="font-code-audit text-code-audit">{event.EventCode}</span>
                <div className="flex flex-wrap gap-space-sm">
                  {event.Registered ? (
                    <Button variant="secondary" size="sm" icon="qr_code_2">
                      {t("checkInPass")}
                    </Button>
                  ) : null}
                  <ButtonLink
                    href={`/me/events/${event.EventCode}`}
                    size="sm"
                    iconAfter="arrow_forward"
                  >
                    {event.Registered ? t("viewDetails") : t("selectEvent")}
                  </ButtonLink>
                </div>
              </PanelFooter>
            </Panel>
          );
        })}
      </div>
    </>
  );
}
