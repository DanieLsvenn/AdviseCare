import { roleHomePath } from "@/lib/roles";

const WORKSPACES = ["me", "advisor", "head", "welfare", "steward", "admin"];

/**
 * The workspace a notification opens in: the one in the current address
 * (/advisor/..., /head/...), or, on a page outside the workspaces such as
 * /notifications, the home workspace of the account's role (one role per
 * account, QC-245, so it is never ambiguous).
 */
export function workspaceFor(pathname: string, roles: readonly string[]): string {
  const current = pathname.split("/")[1] ?? "";
  if (WORKSPACES.includes(current)) return current;
  for (const role of roles) {
    const home = roleHomePath(role);
    if (home) return home.split("/")[1] ?? "";
  }
  return "";
}

/**
 * The API stores role-neutral links ("/cases/{id}", "/referrals/{id}"); the
 * web app's pages live under each workspace (/me, /advisor, /head, /welfare).
 * A link that already names a workspace is used as it is.
 */
export function notificationHref(redirectUrl: string, workspace: string): string {
  if (!redirectUrl) return "/notifications";
  if (/^\/(me|advisor|head|welfare|steward|admin)(\/|$)/.test(redirectUrl)) return redirectUrl;
  const caseMatch = /^\/cases\/([^/?#]+)/.exec(redirectUrl);
  if (caseMatch) {
    if (workspace === "me") return `/me/cases/${caseMatch[1]}`;
    if (workspace === "advisor") return `/advisor/cases/${caseMatch[1]}`;
    if (workspace === "welfare") return `/welfare/cases/${caseMatch[1]}`;
    if (workspace === "head") return "/head/cases";
  }
  if (redirectUrl.startsWith("/referrals")) return workspace === "welfare" ? "/welfare/referrals" : "/advisor/referrals";
  if (redirectUrl.startsWith("/case-change-requests")) return workspace === "head" ? "/head/assignment-changes" : "/notifications";
  if (redirectUrl.startsWith("/escalations")) return workspace === "head" ? "/head/escalations" : "/welfare/escalations";
  if (redirectUrl.startsWith("/consents")) return workspace === "me" ? "/me/consent" : "/advisor/consent-requests";
  return "/notifications";
}
