import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { InspectorLayout } from "@/components/layout/app-shell";
import {
  Button,
  ButtonLink,
  Icon,
  KeyValue,
  KeyValueGrid,
  Notice,
  PageHeader,
  Panel,
  PanelBody,
  PanelFooter,
  PanelHeader,
  StatusChip,
} from "@/components/ui";
import { apiGet } from "@/lib/api";
import type { CampusEvent } from "@/lib/types";

const covered = [
  "Room digital check-in timestamp",
  "Workshop completion milestone badge",
  "Logistical attendance duration record",
];

const excluded = [
  "Personal student notes and drafts",
  "Session Q&A verbal questions",
  "Peer cohort interaction logs (never shared)",
];

/** S10 — Study planning workshop (event detail & participation consent). */
export default async function EventDetailPage({
  params,
}: PageProps<"/[locale]/me/events/[eventId]">) {
  const { locale, eventId } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.event");
  const te = await getTranslations("student.events");

  const data = await apiGet<{ event: CampusEvent }>(`/api/events/${eventId}`).catch(
    () => null,
  );
  if (!data) notFound();

  const { event } = data;
  const free = event.SeatsTotal - event.SeatsTaken;
  const isFull = free === 0;

  return (
    <>
      <PageHeader
        eyebrow={[event.EventCode, event.Category]}
        title={event.Title}
        badge={
          <StatusChip
            tone={event.Registered ? "ontrack" : "referral"}
            icon={event.Registered ? "check_circle" : "event_available"}
            label={event.Registered ? te("registered") : te("openForRegistration")}
          />
        }
        description={event.Summary}
      />

      <InspectorLayout
        inspector={
          <>
            <Panel>
              <PanelHeader
                icon="group"
                title={t("capacity")}
                actions={
                  <StatusChip
                    tone={isFull ? "sealed" : "ontrack"}
                    icon={isFull ? "block" : "event_seat"}
                    label={isFull ? te("full") : t("admissionsOpen")}
                  />
                }
              />
              <PanelBody>
                <p className="font-display-sm text-display-sm text-on-surface">
                  {event.SeatsTaken} / {event.SeatsTotal}
                </p>
                <p className="font-body-sm text-body-sm text-on-surface-variant">
                  {t("seatsRemaining", { count: free })}
                </p>
                <span className="mt-space-sm block h-1.5 w-full overflow-hidden rounded-full bg-surface-container-high">
                  <span
                    className="block h-full bg-primary"
                    style={{
                      width: `${Math.round((event.SeatsTaken / event.SeatsTotal) * 100)}%`,
                    }}
                  />
                </span>
                {event.Registered ? (
                  <p className="mt-space-base font-code-audit text-code-audit text-on-surface-variant">
                    {te("bookingRef")}: REG-EVT-90412
                  </p>
                ) : null}
              </PanelBody>
              <PanelFooter>
                <span>{t("logisticalOnly")}</span>
                <Button size="sm" icon="how_to_reg" disabled={isFull}>
                  {t("confirmBooking")}
                </Button>
              </PanelFooter>
            </Panel>

            <Panel>
              <PanelHeader
                icon="pin_drop"
                title={t("checkInTitle")}
                actions={
                  <StatusChip
                    tone={event.Registered ? "ontrack" : "sealed"}
                    icon={event.Registered ? "lock_open" : "lock_clock"}
                    label={event.Registered ? t("windowActive") : te("notRegistered")}
                  />
                }
              />
              <PanelBody>
                {event.Registered ? (
                  <Notice tone="ontrack" icon="sensors" title={t("windowOpenTitle")}>
                    <p>{t("windowOpenBody")}</p>
                    <p className="mt-space-sm">
                      <Button size="sm" icon="qr_code_2">
                        {t("checkIn")}
                      </Button>
                    </p>
                  </Notice>
                ) : (
                  <Notice tone="sealed" icon="lock_clock" title={t("windowLockedTitle")}>
                    {t("windowLockedBody")}
                  </Notice>
                )}
                <p className="mt-space-md font-code-audit text-code-audit text-on-surface-variant">
                  ROOM-SCANNER-A201
                </p>
              </PanelBody>
            </Panel>
          </>
        }
      >
        <Panel>
          <PanelBody>
            <KeyValueGrid columns={3}>
              <KeyValue
                label={t("dateTime")}
                value={`${event.StartsAt} – ${event.EndsAt.slice(-9)}`}
              />
              <KeyValue label={t("location")} value={event.LocationLabel} />
              <KeyValue label={t("organiser")} value={event.HostLabel} />
            </KeyValueGrid>
          </PanelBody>
        </Panel>

        {isFull ? (
          <Notice tone="attention" icon="info" title={t("fullPolicyTitle")}>
            {t("fullPolicyBody")}
          </Notice>
        ) : null}

        <Panel>
          <PanelHeader
            icon="policy"
            title={t("matrixTitle")}
            subtitle="PURP-STUDENT-EVENT-PARTICIPATION"
          />
          <PanelBody className="space-y-space-lg">
            <Notice tone="sealed" icon="gavel">
              {t("statutoryRule")}
            </Notice>

            <KeyValueGrid columns={2}>
              <KeyValue
                label={t("recipient")}
                value={`Dr. Le Van Khoa (assigned academic advisor) — ${t("recipientBody")}`}
              />
              <KeyValue
                label={t("expiry")}
                value={`180 days (expires 14 Mar 2027) — ${t("expiryBody")}`}
              />
            </KeyValueGrid>

            <section>
              <h2 className="font-headline-sm text-headline-sm text-on-surface">
                {t("boundary")}
              </h2>
              <div className="mt-space-md grid gap-space-md sm:grid-cols-2">
                <div className="rounded-md border border-status-ontrack-border bg-status-ontrack-surface/25 px-space-base py-space-md">
                  <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                    <Icon name="check_circle" className="text-[16px] text-status-ontrack-text" />
                    {t("covered")}
                  </p>
                  <ul className="mt-space-sm space-y-space-xs font-body-sm text-body-sm text-on-surface-variant">
                    {covered.map((item) => (
                      <li key={item}>· {item}</li>
                    ))}
                  </ul>
                </div>
                <div className="rounded-md border border-outline-variant/50 bg-surface-container-low/50 px-space-base py-space-md">
                  <p className="flex items-center gap-1.5 font-label-md text-label-md font-semibold text-on-surface">
                    <Icon name="block" className="text-[16px] text-status-urgent-text" />
                    {t("excluded")}
                  </p>
                  <ul className="mt-space-sm space-y-space-xs font-body-sm text-body-sm text-on-surface-variant">
                    {excluded.map((item) => (
                      <li key={item}>· {item}</li>
                    ))}
                  </ul>
                </div>
              </div>
            </section>

            <section>
              <h2 className="font-headline-sm text-headline-sm text-on-surface">
                {t("chooseLevel")}
              </h2>
              <div className="mt-space-md space-y-space-sm">
                {[
                  { id: "a", title: t("optionA"), body: t("optionABody"), active: true },
                  { id: "b", title: t("optionB"), body: t("optionBBody"), active: false },
                ].map((option) => (
                  <label
                    key={option.id}
                    className={
                      option.active
                        ? "flex cursor-pointer items-start gap-space-md rounded-md border border-secondary bg-secondary-container/20 px-space-base py-space-md"
                        : "flex cursor-pointer items-start gap-space-md rounded-md border border-outline-variant/40 px-space-base py-space-md"
                    }
                  >
                    <input
                      type="radio"
                      name="participation"
                      defaultChecked={option.active}
                      className="mt-1 accent-[#0f766e]"
                    />
                    <span className="min-w-0">
                      <span className="flex flex-wrap items-center gap-space-sm font-label-md text-label-md font-semibold text-on-surface">
                        {option.title}
                        {option.id === "b" ? (
                          <StatusChip tone="referral" label={t("recommended")} size="sm" />
                        ) : null}
                      </span>
                      <span className="mt-1 block font-body-sm text-body-sm text-on-surface-variant">
                        {option.body}
                      </span>
                    </span>
                  </label>
                ))}
              </div>
            </section>
          </PanelBody>
          <PanelFooter>
            <span className="flex items-center gap-1.5 font-code-audit text-code-audit">
              <Icon name="shield_person" className="text-[14px]" />
              {t("consentToken")}: CNS-2026-9912-PRP
            </span>
            <div className="flex flex-wrap gap-space-sm">
              <ButtonLink href="/me/events" variant="tertiary" size="sm" icon="arrow_back">
                {t("backToEvents")}
              </ButtonLink>
              <Button size="sm" icon="save">
                {t("savePreference")}
              </Button>
            </div>
          </PanelFooter>
        </Panel>
      </InspectorLayout>
    </>
  );
}
