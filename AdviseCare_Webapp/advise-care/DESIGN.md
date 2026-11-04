# AdviseCare — Design system "Sổ liên lạc"

Replaces the earlier "Institutional Minimalism" teal system (Prototype/…/
advisecare_project_design_rules.md). Product truth lives in `PRODUCT.md`; the
governance rules there (status label + colour, absent unconsented attributes,
no edit on committed records, bilingual toggle) still bind every screen.

## World

The *sổ liên lạc* is the contact book every Vietnamese student carries between
school and home: a blue cover, white pages ruled in pale blue, entries written
in ballpoint ink, each one dated and signed by the person who wrote it. It is the
oldest record the audience knows of "a student, the adults who look after them,
and a running history nobody tears pages out of" — which is exactly what an
AdviseCare case is.

The world lends four things only (Operate mode): **type, palette, density and one
signature move.** Layout, navigation and controls stay standard web-app patterns.

## Palette — strategy: Committed shell, restrained canvas

Scene: staff work at office desks under daylight and fluorescent light for hours;
students open the app on a phone or laptop at a moment of need → light theme.

| Token | Value | Job |
|---|---|---|
| `cover` | `#162B6B` | Sidebar / student top bar — the book's cover |
| `cover-raised` | `#1E3780` | Hover / active row on the cover |
| `cover-ink` | `#C9D4F5` | Secondary text on the cover |
| `ink` | `#2142B8` | Ballpoint blue: primary actions, links, selection, focus |
| `ink-hover` | `#1A3596` | Primary hover |
| `ink-wash` | `#E8EDFB` | Selected row, active tab ground |
| `desk` | `#F3F5FA` | App canvas behind pages |
| `paper` | `#FFFFFF` | Panels, tables, forms |
| `rule` | `#D9E0F0` | Ruled lines: borders, row dividers |
| `rule-strong` | `#B9C4DE` | Input borders |
| `text` | `#0F1733` | Body text (blue-black ink) |
| `text-muted` | `#444D68` | Secondary text |
| `text-faint` | `#5D6684` | Metadata (≥4.5:1 on paper) |
| `red-ink` | `#B3141F` | Teacher's red pen → urgent / error only |

State colours (text / surface / border, always with a text label):
success `#136C3A / #E5F3EA / #A9D8BA`, attention `#8A4B00 / #FFF3DC / #F2CF8C`,
urgent `#B3141F / #FDE9EA / #F3B2B6`, referral `#5B32B4 / #F1ECFC / #CDBDF1`,
neutral `#4A5470 / #EEF1F7 / #CCD3E3`.

## Type

- **Source Sans 3** (400/500/600/700, Latin + Vietnamese) for all UI. A humanist
  textbook sans: it reads like the printed headings of a school form.
- **Source Code Pro** (400/500) only for data that is measured or keyed: dates
  and times in ledgers, student codes, case references, counts in tables.
- Fixed rem scale, ratio ≈1.15: 12 · 13 · 14 · 16 · 18 · 21 · 24 · 28.
  Staff tables run at 14px; prose and student screens at 16px.

## Density

Staff screens are desk tools: 40px table rows, 36px controls, filters in one
toolbar line. Student screens are roomier: 44px controls, single column ≤720px.

## Shape and depth

6px radius on controls, 8px on panels, 999px only on status pills. Depth is a 1px
`rule` line. Shadows exist only on things that float (menus, drawer, dialog).

## Signature move — the signed entry

Every append-only history is set in the contact book's three columns:
**Date | Entry | Signature.** The date column is Source Code Pro, the entry is the
text, and the signature column carries who wrote it (name · role) over a short ink
underline. A committed entry is *signed*; a draft shows an empty dashed signature
line labelled "Not signed yet". Used by: case stage history, case notes, referrals,
consent changes, access log, activity log. Component: `components/ui/ledger.tsx`.

## Components (all in `components/ui`)

Button (primary / secondary / quiet / danger; sm / md), Field + Input/Select/
Textarea, StatusChip (tone + label, mandatory text), Panel, PageHeader, Tabs,
Table, Drawer (right sheet for create / detail), ConfirmDialog (`<dialog>`),
EmptyState, Skeleton, Notice, Ledger.

## Motion

150–200ms ease-out on hover, drawer slide and dialog fade. No page-load
choreography. `prefers-reduced-motion` turns it off.
