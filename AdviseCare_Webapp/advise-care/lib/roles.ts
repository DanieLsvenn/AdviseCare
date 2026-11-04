export type AppRole =
  | "System Administrator"
  | "Data Protection Steward"
  | "Academic Advisor"
  | "Wellbeing Advisor"
  | "Student Welfare Officer"
  | "Student"
  | "Programme Head";

const roleHomePaths = {
  "System Administrator": "/admin/m01",
  "Data Protection Steward": "/steward/purposes",
  "Academic Advisor": "/advisor/caseload",
  "Wellbeing Advisor": "/advisor/caseload",
  "Student Welfare Officer": "/welfare/referrals",
  Student: "/me/profile",
  "Programme Head": "/head",
} satisfies Record<AppRole, `/${string}`>;

export type RoleHomePath = (typeof roleHomePaths)[AppRole];
export type RoleTranslationKey =
  | "roles.systemAdministrator"
  | "roles.dataProtectionSteward"
  | "roles.academicAdvisor"
  | "roles.wellbeingAdvisor"
  | "roles.studentWelfareOfficer"
  | "roles.student"
  | "roles.programmeHead";

const landingRoleOrder: AppRole[] = [
  "System Administrator",
  "Data Protection Steward",
  "Programme Head",
  "Academic Advisor",
  "Wellbeing Advisor",
  "Student Welfare Officer",
  "Student",
];

export function roleHomePath(role: string): RoleHomePath | undefined {
  return Object.prototype.hasOwnProperty.call(roleHomePaths, role)
    ? roleHomePaths[role as AppRole]
    : undefined;
}

export function landingPathForRoles(roles: readonly string[]): string {
  for (const role of landingRoleOrder) {
    if (roles.includes(role)) return roleHomePaths[role];
  }
  return "/access-denied";
}

export function localizeRole(
  role: string,
  translate: (key: RoleTranslationKey) => string,
): string {
  switch (role) {
    case "System Administrator":
      return translate("roles.systemAdministrator");
    case "Data Protection Steward":
      return translate("roles.dataProtectionSteward");
    case "Academic Advisor":
      return translate("roles.academicAdvisor");
    case "Wellbeing Advisor":
      return translate("roles.wellbeingAdvisor");
    case "Student Welfare Officer":
      return translate("roles.studentWelfareOfficer");
    case "Student":
      return translate("roles.student");
    case "Programme Head":
      return translate("roles.programmeHead");
    default:
      return role;
  }
}
