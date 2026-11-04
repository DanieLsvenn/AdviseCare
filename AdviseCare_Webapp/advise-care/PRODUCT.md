# Product

<!-- impeccable:product-schema 1 -->

Source of truth: `Documentation/AdviseCare_Functional_Requirements.txt`,
`AdviseCare_Workflow_List.txt` and the FA26SE308 registration (revised Oct 2026).
Facts below are taken from those documents; anything marked *(inferred)* was not
confirmed by the team and should be checked.

## Platform

web

## Users

Seven roles at one Vietnamese university, all signed in with a personal Google
account or a password:

- **Student** — asks for help (academic or wellbeing), follows their own cases,
  decides which personal data each purpose may use, sees who read their data.
  Uses the app occasionally, often on a phone *(inferred)*, at a moment of need.
- **Academic Advisor / Wellbeing Advisor** — works the cases matched to them:
  reads the shared data, writes notes, moves the case through its stages,
  refers it on. Daily desk use.
- **Programme Head** — one person for the whole app: matches the queue of new
  requests to advisors by hand, approves advisor changes, answers escalations,
  reports on caseload and closure times.
- **Student Welfare Officer** — money matters only (loans, financial support,
  bank paperwork); takes referrals, logs interventions and grants, links
  students to external services, escalates.
- **Data Protection Steward** — defines data attributes, consent purposes and
  access policies; reviews the access log; approves emergency access; executes
  cryptographic erasure.
- **System Administrator** — accounts and roles, master data (terms, faculties,
  subjects, specialisations, support categories), all-or-nothing data imports.

## Product Purpose

Match each student's support request to the right advisor and track the case
until it closes, while the student keeps control over which of their data
each purpose can use. Success: no request waits unseen in the queue, every
case shows where it stands, and every read of a student's data is consented and
logged.

## Positioning

A university advising system in which consent is enforced per purpose and per
attribute, every data read is logged and visible to the student, and a withdrawn
attribute is erased cryptographically — not merely hidden.

## Operating Context

- Flow: student request (type, category or subject, reason, chosen attributes)
  → Programme Head matches manually → student gets advisor contact by email and
  in-app → meeting happens outside the system → advisor notes, stages,
  referrals → closed. Advisors can also open a case from a student's indicators.
- Case stages: pending, assigned, contacted, referred, followUp, closed
  (status open / closed / reopened; priority low / medium / high / urgent).
- Notes, stage history and referrals are append-only; a finalized note cannot
  be edited.
- Data enters only by file import; the cohort is synthetic.

## Capabilities and Constraints

- Next.js 16 App Router + React 19 + Tailwind v4 + next-intl (EN default, VI).
  The UI talks to the ASP.NET Core API through a same-origin BFF
  (`app/api/[...path]`); no mock data in the UI.
- Most screens are generated from `components/workspace/workspace-definitions.ts`
  (one list endpoint + filters + create / row operations per screen).
- Out of scope now: automated/AI matching, in-app scheduling or chat, event
  register, mobile check-in, anonymised cohort reports.
- Pending review: emergency access, correction requests.

## Brand Commitments

- Name: AdviseCare. Bilingual EN | VI toggle visible on every screen.
- Status is never colour alone: every status carries a text label.
- An attribute the viewer may not see shows no value, no mask and no
  "request access" button.
- Committed records have no edit or delete affordance; corrections are
  additions.
- Notifications never put protected values in previews or email subjects.
- No decorative hero imagery.

## Evidence on Hand

Synthetic cohort only. No real testimonials, logos of partners or statistics;
none may be invented.

## Product Principles

1. The request is the unit of work: every screen answers "where is this case and
   who has it now?"
2. Consent is visible, not implied: what is shared, with whom, for which purpose.
3. History is additive: nothing committed disappears; changes stack on top.
4. Staff screens are for daily desk work — dense, scannable, keyboard-friendly.
5. The student side is calm and plain-spoken; it is used at a moment of need.

## Accessibility & Inclusion

WCAG 2.1 AA contrast; full Vietnamese diacritics in every face; keyboard
operation of all workflows; labels never rely on colour alone.
