import { getTranslations, setRequestLocale } from "next-intl/server";
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
import type { AccessRequest, ConsentGrant, SensitiveAttributeType } from "@/lib/types";

/** S02 — My consent & privacy permissions. */
export default async function MyConsentPage({ params }: PageProps<"/[locale]/me/consent">) {
  const { locale } = await params;
  setRequestLocale(locale);

  const t = await getTranslations("student.consent");
  const tc = await getTranslations("common");

  const [{ grants, attributes }, { requests }] = await Promise.all([
    apiGet<{ grants: ConsentGrant[]; attributes: SensitiveAttributeType[] }>(
      "/api/consent/grants?studentId=1",
    ),
    apiGet<{ requests: AccessRequest[] }>("/api/access-requests"),
  ]);

  const labelFor = (code: string) =>
    attributes.find((attribute) => attribute.Code === code)?.Label ?? code;

  const activeGrant = grants.find(
    (grant) => grant.PurposeCode === "ACADEMIC_ADVISING" && grant.State === "GRANTED",
  );
  const pendingRequest = requests.find(
    (request) => request.StudentId === 1 && request.State === "PENDING",
  );

  return (
    <>
      <PageHeader
        eyebrow={["Privacy governance", "S02 · Consent ledger"]}
        title={t("title")}
        badge={<StatusChip tone="sealed" icon="lock" label={t("mode")} />}
        description={t("intro")}
        meta={<StatusChip tone="sealed" icon="fingerprint" label="CID: 0x7E9A…94A2" />}
      />

      <Notice tone="sealed" icon="verified_user" title={t("agencyTitle")}>
        <p>{t("agencyBody")}</p>
        <p className="mt-space-sm flex flex-wrap items-center gap-space-sm">
          <StatusChip tone="ontrack" icon="block" label={t("blanketProhibited")} />
          <span className="font-code-audit text-code-audit">POL-STUDENT-AUTONOMY-V4</span>
        </p>
      </Notice>

      <div className="flex flex-wrap items-center justify-between gap-space-sm">
        <h2 className="flex items-center gap-space-sm font-headline-sm text-headline-sm text-on-surface">
          <Icon name="policy" className="text-[20px] text-primary" />
          {t("purposeGroups")}
        </h2>
        <StatusChip tone="sealed" icon="shield" label={t("isolation")} />
      </div>

      {activeGrant ? (
        <Panel>
          <PanelHeader
            icon="school"
            title={`Purpose group 1: ${activeGrant.PurposeLabel}`}
            subtitle="Allows designated academic advisors to coordinate academic intervention, tutoring and study pacing adjustments during your degree."
            actions={
              <StatusChip tone="ontrack" icon="check_circle" label={t("granted")} />
            }
          />
          <PanelBody className="space-y-space-base">
            <div>
              <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                {t("coveredAttributes")}
              </h3>
              <ul className="mt-space-sm space-y-space-sm">
                <li className="rounded-md border border-status-ontrack-border bg-status-ontrack-surface/30 px-space-base py-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {labelFor("FIN_HARDSHIP")}
                    </p>
                    <StatusChip tone="ontrack" icon="check" label={tc("granted")} />
                  </div>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    ATTR-FIN-001
                  </p>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    Allows fee relief verification and hardship grant coordination without sharing
                    bank details or exact balances.
                  </p>
                </li>
                <li className="rounded-md border border-outline-variant/50 bg-surface-container-low/50 px-space-base py-space-md">
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {labelFor("MED_CLINICAL")}
                    </p>
                    <StatusChip tone="sealed" icon="do_not_disturb_on" label={t("withheld")} />
                  </div>
                  <p className="mt-1 font-code-audit text-code-audit text-on-surface-variant">
                    ATTR-MED-CORE
                  </p>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    Protected healthcare category. Never disclosed to, or requested by, academic
                    advising staff.
                  </p>
                </li>
              </ul>
            </div>

            <KeyValueGrid columns={3}>
              <KeyValue label={t("recipientRoles")} value={activeGrant.SharedWith.join(", ")} />
              <KeyValue
                label={t("scopeTerm")}
                value={`${activeGrant.ExpiresAt ?? "—"} · auto-revoke on file`}
              />
              <KeyValue label={t("lastAffirmation")} value={activeGrant.RecordedAt} mono />
            </KeyValueGrid>
          </PanelBody>
          <PanelFooter>
            <span className="flex items-center gap-1.5">
              <Icon name="encrypted" className="text-[16px]" />
              {t("sealedUnder")} #CONS-{activeGrant.ConsentGrantId}
            </span>
            <div className="flex flex-wrap gap-space-sm">
              <ButtonLink
                href={`/me/consent/${activeGrant.ConsentGrantId}`}
                variant="secondary"
                size="sm"
                icon="tune"
              >
                {t("changeScope")}
              </ButtonLink>
              <ButtonLink
                href={`/me/consent/${activeGrant.ConsentGrantId}/withdraw`}
                variant="secondary"
                size="sm"
                icon="gavel"
              >
                {t("withdraw")}
              </ButtonLink>
            </div>
          </PanelFooter>
        </Panel>
      ) : null}

      {pendingRequest ? (
        <Panel className="border-status-attention-border">
          <PanelHeader
            icon="volunteer_activism"
            title={`Purpose group 2: ${pendingRequest.PurposeLabel}`}
            subtitle="Coordinates emergency living arrangements, hardship bursaries and specialised wellbeing services. No data has been transmitted yet."
            actions={
              <StatusChip
                tone="attention"
                icon="pending_actions"
                label={`${pendingRequest.RequestCode} · ${tc("pending")}`}
              />
            }
          />
          <PanelBody className="space-y-space-base">
            <div className="flex flex-wrap items-center justify-between gap-space-sm">
              <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
                {t("requestedAttributes")}
              </h3>
              <StatusChip tone="sealed" icon="splitscreen" label={t("unbundled")} />
            </div>
            <ul className="space-y-space-sm">
              {["HOUSING", "FIN_HARDSHIP"].map((code) => (
                <li
                  key={code}
                  className="rounded-md border border-outline-variant/40 px-space-base py-space-md"
                >
                  <div className="flex flex-wrap items-center justify-between gap-space-sm">
                    <p className="font-label-md text-label-md font-semibold text-on-surface">
                      {labelFor(code)}
                    </p>
                    <StatusChip
                      tone="attention"
                      icon="help_outline"
                      label={t("pendingAffirmation")}
                    />
                  </div>
                  <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
                    {code === "HOUSING"
                      ? "Verification of living disruption for emergency hostel placement and short-term rent relief vouchers."
                      : "Hardship tier evaluation for the Fall 2026 emergency subsistence bursary."}
                  </p>
                </li>
              ))}
            </ul>

            <Notice tone="sealed" icon="verified">
              {t("guardrail")}
            </Notice>

            <KeyValueGrid columns={3}>
              <KeyValue label={t("recipientRoles")} value="Student Welfare Officer" />
              <KeyValue label={t("proposedTerm")} value={pendingRequest.ExpiresAt ?? "—"} />
              <KeyValue
                label={t("requestAudit")}
                value={`${pendingRequest.RaisedAt} · ${pendingRequest.RequesterName}`}
                mono
              />
            </KeyValueGrid>
          </PanelBody>
          <PanelFooter>
            <span className="flex items-center gap-1.5">
              <Icon name="timer" className="text-[16px]" />
              {t("awaitingAction")}
            </span>
            <div className="flex flex-wrap gap-space-sm">
              <Button variant="tertiary" size="sm" icon="close">
                {t("decline")}
              </Button>
              <ButtonLink
                href={`/me/consent/${pendingRequest.AccessRequestId}`}
                size="sm"
                iconAfter="arrow_forward"
              >
                {t("reviewRequest")} ({pendingRequest.RequestCode})
              </ButtonLink>
            </div>
          </PanelFooter>
        </Panel>
      ) : null}

      <Panel>
        <PanelHeader
          icon="sports_kabaddi"
          title="Purpose group 3: event & co-curricular participation"
          subtitle="Voluntary participation in student union events, hackathons and departmental workshops requiring dietary or accessibility accommodations."
          actions={
            <StatusChip tone="sealed" icon="radio_button_unchecked" label={t("noDecision")} />
          }
        />
        <PanelBody>
          <h3 className="font-label-sm text-label-sm uppercase tracking-wider text-on-surface-variant">
            {t("optionalAttributes")}
          </h3>
          <div className="mt-space-sm rounded-md border border-outline-variant/40 px-space-base py-space-md">
            <div className="flex flex-wrap items-center justify-between gap-space-sm">
              <p className="font-label-md text-label-md font-semibold text-on-surface">
                Accessibility & dietary requirements
              </p>
              <StatusChip tone="sealed" icon="remove" label="Inactive / not decided" />
            </div>
            <p className="mt-space-sm font-body-sm text-body-sm text-on-surface-variant">
              Used solely for catering procurement and wheelchair accessibility arrangements at
              student union gatherings.
            </p>
          </div>
        </PanelBody>
      </Panel>
    </>
  );
}
