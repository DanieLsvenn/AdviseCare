import { getTranslations } from "next-intl/server";
import englishWorkspace from "@/messages/en/workspace.json";
import type {
  ApiWorkspaceDefinition,
  WorkspaceField,
  WorkspaceOperation,
  WorkspaceColumn,
} from "@/components/workspace/api-workspace";

type WorkspaceKey = keyof typeof englishWorkspace;
export type WorkspaceTranslator = (
  key: WorkspaceKey,
) => string;
export type WorkspaceScreen =
  | "admin.accounts"
  | "admin.masterData"
  | "admin.assignments"
  | "admin.studentData"
  | "admin.categories"
  | "admin.activity"
  | "admin.sessions"
  | "student.export"
  | "student.profile"
  | "student.cases"
  | "student.support"
  | "student.consent"
  | "student.consentRequests"
  | "student.corrections"
  | "student.data"
  | "student.access"
  | "student.events"
  | "advisor.caseload"
  | "advisor.caseDetail"
  | "advisor.consentRequests"
  | "advisor.corrections"
  | "advisor.referrals"
  | "advisor.emergency"
  | "advisor.studentData"
  | "advisor.historical"
  | "advisor.compare"
  | "advisor.caseHistory"
  | "welfare.referrals"
  | "welfare.interventions"
  | "welfare.caseDetail"
  | "welfare.externalReferrals"
  | "welfare.externalServices"
  | "steward.purposes"
  | "steward.policies"
  | "steward.attributes"
  | "steward.consents"
  | "steward.accessRecords"
  | "steward.erasure"
  | "steward.emergency"
  | "steward.keys"
  | "head.matching"
  | "head.cases"
  | "head.changeRequests"
  | "head.escalations"
  | "welfare.escalations"
  | "head.reports"
  | "head.workload"
  | "notifications"
  | "settings";

type WorkspaceMessages = Awaited<
  ReturnType<typeof getTranslations<"workspace">>
>;

export function getWorkspaceDefinitions(
  screen: WorkspaceScreen,
  t: WorkspaceMessages,
): ApiWorkspaceDefinition[] {
  const field = (
    key: WorkspaceKey,
    type: WorkspaceField["type"] = "text",
    required = false,
  ): WorkspaceField => ({
    key,
    label: t(key),
    type,
    required,
  });
  const select = (
    key: WorkspaceKey,
    options: WorkspaceKey[],
    required = false,
  ): WorkspaceField => ({
    ...field(key, "select", required),
    options: options.map((option) => ({
      label: t(option),
      value: option,
    })),
  });
  const selectValues = (
    key: WorkspaceKey,
    options: Array<{ label: WorkspaceKey; value: string }>,
    required = false,
  ): WorkspaceField => ({
    ...field(key, "select", required),
    options: options.map((option) => ({
      label: t(option.label),
      value: option.value,
    })),
  });
  // A column is a message key, or [data key, message key] when the API field
  // name differs from the label's key (e.g. categoryName → "Support category").
  const columns = (keys: Array<WorkspaceKey | [string, WorkspaceKey]>): WorkspaceColumn[] =>
    keys.map((key) =>
      Array.isArray(key) ? { key: key[0], label: t(key[1]) } : { key, label: t(key) },
    );
  const operation = (
    labelKey: WorkspaceKey,
    method: WorkspaceOperation["method"],
    path: string,
    fields?: WorkspaceField[],
    options: Pick<
      WorkspaceOperation,
      "confirm" | "bodyFromRecord" | "body" | "destructive" | "description" | "icon" | "when"
    > = {},
  ): WorkspaceOperation => ({
    label: t(labelKey),
    method,
    path,
    fields,
    ...options,
  });
  const workspace = (
    titleKey: WorkspaceKey,
    listPath: string,
    columnKeys: Array<WorkspaceKey | [string, WorkspaceKey]>,
    options: Omit<
      ApiWorkspaceDefinition,
      "title" | "listPath" | "columns"
    > = {},
  ): ApiWorkspaceDefinition => ({
    title: t(titleKey),
    listPath,
    columns: columns(columnKeys),
    ...options,
  });
  const caseStageOptions = [
    "pending",
    "assigned",
    "contacted",
    "followUp",
    "referred",
    "closed",
  ] as WorkspaceKey[];
  const caseStatusOptions = ["open", "closed", "reopened"] as WorkspaceKey[];
  const priorityOptions = ["low", "medium", "high", "urgent"] as WorkspaceKey[];
  // Pickers backed by the API, so nobody has to type a GUID or a role name.
  const roleSource = (excludeStudent = false) => ({
    path: "Roles",
    valueKey: "roleName",
    labelKeys: ["roleName"],
    ...(excludeStudent ? { exclude: { roleName: ["Student"] } } : {}),
  });
  const roleIdSource = (excludeStudent = false) => ({
    ...roleSource(excludeStudent),
    valueKey: "roleId",
  });
  const facultySource = {
    path: "Faculties",
    valueKey: "facultyId",
    labelKeys: ["facultyCode", "facultyName"],
    match: { status: "active" },
  };
  const purposeSource = {
    path: "ConsentPurposes",
    valueKey: "consentPurposeId",
    labelKeys: ["purposeName"],
    exclude: { status: ["retired"] },
  };
  const subjectSource = {
    path: "Subjects",
    valueKey: "subjectId",
    labelKeys: ["subjectCode", "subjectName"],
  };
  const activeSubjectSource = { ...subjectSource, match: { status: "active" } };
  const termSource = {
    path: "AcademicTerms",
    valueKey: "termId",
    labelKeys: ["termName"],
  };
  const attributeSource = {
    path: "DataAttributes",
    valueKey: "dataAttributeId",
    labelKeys: ["attributeName"],
  };
  const activeAttributeSource = { ...attributeSource, match: { status: "active" } };
  const activePurposeSource = { ...purposeSource, match: { status: "active" } };
  const categorySource = {
    path: "SupportCategories",
    valueKey: "supportCategoryId",
    labelKeys: ["categoryName"],
  };
  const externalServiceSource = {
    path: "ExternalServices",
    valueKey: "externalServiceId",
    labelKeys: ["serviceName"],
    match: { status: "active" },
  };
  // People are found by typing part of a name, code or email (search pickers), never by pasting an ID.
  const studentSearch = {
    path: "StudentProfiles/search",
    valueKey: "studentId",
    labelKeys: ["fullName"],
    detailKeys: ["studentCode", "email"],
    searchKeys: ["fullName", "studentCode"],
  };
  const staffSearch = (roleNames: string[] = []) => ({
    path: "Accounts/staff/search",
    valueKey: "accountId",
    labelKeys: ["fullName"],
    detailKeys: ["email"],
    searchKeys: ["fullName", "email"],
    ...(roleNames.length ? { query: { roleNames: roleNames.join(",") } } : {}),
  });
  const advisorRoles = ["Academic Advisor", "Wellbeing Advisor"];
  const accountSearch = {
    path: "Accounts/search",
    valueKey: "accountId",
    labelKeys: ["fullName"],
    detailKeys: ["email"],
    searchKeys: ["fullName", "email", "userName"],
  };
  // Cases the signed-in advisor holds, and cases referred to the signed-in Welfare Officer.
  const advisorCaseSource = {
    path: "Cases/assigned",
    valueKey: "caseId",
    labelKeys: ["studentFullName", "categoryName"],
  };
  const welfareCaseSource = {
    path: "Referrals/me/incoming",
    valueKey: "caseId",
    labelKeys: ["studentFullName", "categoryName"],
  };
  const search = (
    key: WorkspaceKey,
    optionsFrom: WorkspaceField["optionsFrom"],
    required = false,
    labelKey: WorkspaceKey = key,
  ): WorkspaceField => ({ ...field(key, "search", required), label: t(labelKey), optionsFrom });
  const picker = (
    key: WorkspaceKey,
    optionsFrom: WorkspaceField["optionsFrom"],
    required = false,
    type: "select" | "multiselect" = "select",
    labelKey: WorkspaceKey = key,
  ): WorkspaceField => ({ ...field(key, type, required), label: t(labelKey), optionsFrom });
  const academicStatusOptions: Array<{ label: WorkspaceKey; value: string }> = [
    { label: "enrolled", value: "enrolled" },
    { label: "onLeave", value: "onLeave" },
    { label: "withdrawnStudent", value: "withdrawn" },
    { label: "graduated", value: "graduated" },
  ];

  switch (screen) {
    case "student.export":
      return [];
    case "admin.accounts":
      return [
        workspace(
          "accountList",
          "Accounts/search",
          [
            "fullName",
            "email",
            "userName",
            "facultyName",
            "roles",
            "status",
            "linkStatus",
          ],
          {
            queryFields: [
              field("email", "email"),
              field("fullName"),
              field("userName"),
              field("studentCode"),
              picker("facultyId", facultySource, false, "select", "faculty"),
              picker("roleName", roleSource(), false, "select", "role"),
              selectValues("academicStatus", academicStatusOptions),
              field("linkStatus"),
              select("status", ["active", "inactive", "banned"] as WorkspaceKey[]),
            ],
            // Importing has its own page section (AccountImportWorkflow): upload,
            // an editable preview with the problems of every row, then import.
            create: operation("create", "POST", "Accounts", [
              field("email", "email", true),
              field("fullName", "text", true),
              field("userName", "text", true),
              picker("facultyId", facultySource, false, "select", "faculty"),
              // One role per account: a Student never holds a staff role, and staff hold one role each.
              { ...picker("roleNames", roleSource(), true, "select", "role"), asArray: true, hint: t("rolesHint") },
              {
                ...field("studentCode", "text", true),
                showWhen: { key: "roleNames", includes: "Student" },
                hint: t("studentCodeHint"),
              },
            ]),
            rowOperations: [
              operation("update", "PUT", "Accounts/:accountId", [
                field("email", "email", true),
                field("fullName", "text", true),
                field("userName", "text", true),
                picker("facultyId", facultySource, false, "select", "faculty"),
              ], {
                bodyFromRecord: {
                  email: { key: "email" },
                  fullName: { key: "fullName" },
                  userName: { key: "userName" },
                  facultyId: { key: "facultyId" },
                },
              }),
              operation("sendLink", "POST", "Accounts/linking-emails", undefined, {
                confirm: true,
                icon: "forward_to_inbox",
                description: t("sendLinkHint"),
                when: { key: "linkStatus", notIn: ["linked"] },
                bodyFromRecord: {
                  accountIds: { key: "accountId", asArray: true },
                },
              }),
              operation(
                "changeRoles",
                "PUT",
                "Accounts/:accountId/roles",
                [
                  // Staff move between staff roles only; Student accounts keep the Student role.
                  { ...picker("roleNames", roleSource(true), true, "select", "role"), asArray: true },
                ],
                {
                  bodyFromRecord: {
                    roleNames: { key: "roles" },
                  },
                  description: t("changeRolesHint"),
                  icon: "badge",
                  when: { key: "roles", notIn: ["student"] },
                },
              ),
              operation("activate", "PUT", "Accounts/:accountId/activate", undefined, {
                confirm: true,
                icon: "toggle_on",
                when: { key: "status", in: ["inactive"] },
              }),
              operation("unban", "PUT", "Accounts/:accountId/unban", undefined, {
                confirm: true,
                icon: "lock_open",
                when: { key: "status", in: ["banned"] },
              }),
              operation("deactivate", "PUT", "Accounts/:accountId/deactivate", undefined, {
                confirm: true,
                destructive: true,
                icon: "toggle_off",
                description: t("deactivateHint"),
                when: { key: "status", in: ["active"] },
              }),
              operation("ban", "PUT", "Accounts/:accountId/ban", undefined, {
                confirm: true,
                destructive: true,
                icon: "block",
                description: t("banHint"),
                when: { key: "status", notIn: ["banned"] },
              }),
            ],
            pageSize: 20,
          },
        ),
      ];

    case "admin.masterData":
      return [
        workspace(
          "faculty",
          "Faculties/search",
          ["facultyId", "facultyCode", "facultyName", "status"],
          {
            queryFields: [field("facultyCode"), field("facultyName"), field("status")],
            create: operation("create", "POST", "Faculties", [
              field("facultyCode", "text", true),
              field("facultyName", "text", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "Faculties/:facultyId", [
                field("facultyCode", "text", true),
                field("facultyName", "text", true),
              ], {
                bodyFromRecord: {
                  facultyCode: { key: "facultyCode" },
                  facultyName: { key: "facultyName" },
                },
              }),
            ],
          },
        ),
        workspace(
          "programme",
          "Programmes/search",
          ["programmeId", "programmeCode", "programmeName", "facultyId", "status"],
          {
            queryFields: [field("programmeCode"), field("programmeName"), picker("facultyId", facultySource, false, "select", "faculty")],
            create: operation("create", "POST", "Programmes", [
              field("programmeCode", "text", true),
              field("programmeName", "text", true),
              picker("facultyId", facultySource, true, "select", "faculty"),
            ]),
            rowOperations: [
              operation("update", "PUT", "Programmes/:programmeId", [
                field("programmeCode", "text", true),
                field("programmeName", "text", true),
                picker("facultyId", facultySource, true, "select", "faculty"),
                select("status", ["active", "archived"], true),
              ], {
                bodyFromRecord: {
                  programmeCode: { key: "programmeCode" },
                  programmeName: { key: "programmeName" },
                  facultyId: { key: "facultyId" },
                  status: { key: "status" },
                },
              }),
            ],
          },
        ),
        workspace(
          "term",
          "AcademicTerms/search",
          ["termName", "startDate", "endDate", "status"],
          {
            queryFields: [field("termName"), field("status")],
            create: operation("create", "POST", "AcademicTerms", [
              field("termName", "text", true),
              field("startDate", "date", true),
              field("endDate", "date", true),
              select("status", ["upcoming", "current", "past"], true),
            ]),
            rowOperations: [
              operation("update", "PUT", "AcademicTerms/:termId", [
                field("termName", "text", true),
                field("startDate", "date", true),
                field("endDate", "date", true),
                select("status", ["upcoming", "current", "past"], true),
              ], {
                bodyFromRecord: {
                  termName: { key: "termName" },
                  startDate: { key: "startDate" },
                  endDate: { key: "endDate" },
                  status: { key: "status" },
                },
              }),
            ],
          },
        ),
        workspace(
          "subject",
          "Subjects/search",
          ["subjectId", "subjectCode", "subjectName", "facultyId", "status"],
          {
            queryFields: [
              field("subjectCode"),
              field("subjectName"),
              picker("facultyId", facultySource, false, "select", "faculty"),
            ],
            create: operation("create", "POST", "Subjects", [
              picker("facultyId", facultySource, true, "select", "faculty"),
              field("subjectCode", "text", true),
              field("subjectName", "text", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "Subjects/:subjectId", [
                picker("facultyId", facultySource, true, "select", "faculty"),
                field("subjectCode", "text", true),
                field("subjectName", "text", true),
                select("status", ["active", "archived"], true),
              ], {
                bodyFromRecord: {
                  facultyId: { key: "facultyId" },
                  subjectCode: { key: "subjectCode" },
                  subjectName: { key: "subjectName" },
                  status: { key: "status" },
                },
              }),
            ],
          },
        ),
      ];

    case "admin.assignments":
      return [
        workspace(
          "subjectAssignments",
          "Subjects/account-subjects/search",
          ["accountId", "accountName", "subjectId", "subjectCode", "subjectName"],
          {
            queryFields: [search("accountId", staffSearch(["Academic Advisor"]), false, "advisor"), picker("subjectId", subjectSource, false, "select", "subject")],
            create: operation("create", "POST", "Subjects/account-subjects", [
              search("accountId", staffSearch(["Academic Advisor"]), true, "advisor"),
              picker("subjectId", activeSubjectSource, true, "select", "subject"),
            ]),
            rowOperations: [
              operation(
                "remove",
                "DELETE",
                "Subjects/account-subjects/:accountId/:subjectId",
                undefined,
                { confirm: true },
              ),
            ],
          },
        ),
      ];

    case "admin.studentData":
      return [
        workspace(
          "dataLedger",
          "StudentData/search",
          [
            ["studentFullName", "student"],
            ["attributeName", "dataAttributeName"],
            ["termName", "term"],
            ["validFrom", "ledgerValidFrom"],
            ["transactionFrom", "ledgerRecordedAt"],
            ["status", "ledgerRowKind"],
            ["isMissing", "ledgerMissing"],
            ["erasedAt", "ledgerErasedAt"],
          ],
          {
            description: t("dataLedgerDescription"),
            queryFields: [
              picker("dataAttributeId", {
                path: "DataAttributes",
                valueKey: "dataAttributeId",
                labelKeys: ["attributeName"],
              }, false, "select", "dataAttributeName"),
              picker("termId", {
                path: "AcademicTerms",
                valueKey: "termId",
                labelKeys: ["termName"],
              }, false, "select", "term"),
              selectValues("status", [
                { label: "ledgerOriginal", value: "original" },
                { label: "ledgerCorrection", value: "correction" },
              ]),
              field("isMissing", "checkbox"),
              search("studentId", studentSearch, false, "student"),
            ],
            emptyTitle: t("dataLedgerEmptyTitle"),
            emptyBody: t("dataLedgerEmptyBody"),
          },
        ),
      ];

    case "admin.categories":
      return [
        workspace(
          "supportCategories",
          "SupportCategories/search",
          [
            "supportCategoryId",
            "categoryName",
            "supportType",
            "consentPurposeName",
            "status",
          ],
          {
            queryFields: [
              field("categoryName"),
              select("supportType", ["academic", "wellbeing"]),
              field("status"),
            ],
            create: operation("create", "POST", "SupportCategories", [
              field("categoryName", "text", true),
              field("description", "textarea"),
              select("supportType", ["academic", "wellbeing"], true),
              picker("consentPurposeId", activePurposeSource, true, "select", "consentPurpose"),
            ]),
            rowOperations: [
              operation("update", "PUT", "SupportCategories/:supportCategoryId", [
                field("categoryName", "text", true),
                field("description", "textarea", true),
                select("supportType", ["academic", "wellbeing"], true),
                picker("consentPurposeId", activePurposeSource, true, "select", "consentPurpose"),
                select("status", ["active", "archived"], true),
              ], {
                bodyFromRecord: {
                  categoryName: { key: "categoryName" },
                  description: { key: "description" },
                  supportType: { key: "supportType" },
                  consentPurposeId: { key: "consentPurposeId" },
                  status: { key: "status" },
                },
              }),
            ],
          },
        ),
        workspace(
          "dataAttributes",
          "DataAttributes/search",
          ["dataAttributeId", "attributeName", "dataType", "status"],
          {
            queryFields: [field("attributeName"), field("status")],
            create: operation("create", "POST", "DataAttributes", [
              field("attributeName", "text", true),
              field("attributeDescription", "textarea"),
              selectValues("dataType", [
                { label: "numericType", value: "numeric" },
                { label: "categoricalType", value: "categorical" },
                { label: "booleanType", value: "boolean" },
              ], true),
            ]),
            rowOperations: [
              operation(
                "archive",
                "PUT",
                "DataAttributes/:dataAttributeId/archive",
                undefined,
                { confirm: true },
              ),
            ],
          },
        ),
      ];

    case "admin.activity":
      return [
        workspace(
          "activityLog",
          "ActivityLogs/search",
          [
            "activityLogId",
            "activityType",
            "actorAccountId",
            "subjectAccountId",
            "createdAt",
          ],
          {
            queryFields: [
              field("activityType"),
              field("fromDate", "date"),
              field("toDate", "date"),
            ],
            pageSize: 30,
          },
        ),
      ];

    case "admin.sessions":
      return [
        workspace(
          "sessions",
          "Sessions/search",
          ["sessionId", "accountName", "deviceInfo", "createdAt", "lastSeenAt", "isActive"],
          {
            queryFields: [search("accountId", accountSearch, false, "account"), field("isActive", "checkbox")],
            rowOperations: [
              operation(
                "endSession",
                "PUT",
                "Sessions/:sessionId",
                undefined,
                {
                  confirm: true,
                  destructive: true,
                  // Ending a session = PUT Sessions/{id} with isActive = false (logoutAt is set to now)
                  body: { isActive: false },
                  when: { key: "isActive", in: ["true"] },
                },
              ),
            ],
          },
        ),
      ];

    case "student.profile":
    case "settings":
      return [
        workspace(
          "accountSettings",
          "Auth/me",
          [
            "accountId",
            "fullName",
            "email",
            "userName",
            "preferredLanguage",
            "roles",
          ],
          {
            globalOperations: [
              operation("editProfile", "PUT", "Auth/me", [
                field("userName", "text", true),
                selectValues(
                  "preferredLanguage",
                  [
                    { label: "english", value: "en" },
                    { label: "vietnamese", value: "vi" },
                  ],
                  true,
                ),
              ], { icon: "edit" }),
              operation("changePassword", "POST", "Auth/change-password", [
                field("oldPassword", "text", true),
                field("newPassword", "text", true),
                field("confirmPassword", "text", true),
              ], { icon: "lock" }),
              operation("changeAvatar", "POST", "Auth/me/avatar", [
                field("avatar", "file", true),
              ], { icon: "account_circle" }),
            ],
          },
        ),
        workspace(
          "notifications",
          "NotificationPreferences/me",
          ["channel", "isEnabled"],
          {
            globalOperations: [
              operation("update", "PUT", "NotificationPreferences/me", [
                field("channel", "text", true),
                field("isEnabled", "checkbox"),
              ]),
            ],
          },
        ),
      ];

    case "student.cases":
      return [
        workspace(
          "myRequests",
          "Cases/me",
          [
            ["categoryName", "supportCategoryName"],
            "subjectName",
            "advisorName",
            "currentStage",
            "openedAt",
          ],
          {
            description: t("myRequestsDescription"),
            emptyTitle: t("myRequestsEmptyTitle"),
            emptyBody: t("myRequestsEmptyBody"),
            rowHref: "/me/cases/:caseId",
            primaryLink: { label: t("requestSupport"), href: "/me/support-request", icon: "add" },
            queryFields: [
              select("currentStage", caseStageOptions),
              select("status", caseStatusOptions),
            ],
          },
        ),
      ];

    case "student.support":
      return [];

    case "student.consent":
      return [
        // Consents/me answers one entry per purpose with its attributes inside;
        // the list shows one row per purpose + attribute with the student's state.
        workspace(
          "consent",
          "Consents/me",
          [
            ["purposeName", "consentPurpose"],
            ["attributeName", "dataAttributeName"],
            "status",
            "grantedAt",
            "expiresAt",
          ],
          {
            flattenKey: "attributes",
            emptyBody: t("studentConsentEmpty"),
            rowOperations: [
              operation("grantConsent", "POST", "Consents", [
                { ...field("expiresAt", "datetime-local"), noPrefill: true },
              ], {
                icon: "verified_user",
                description: t("grantConsentHint"),
                bodyFromRecord: {
                  consentPurposeId: { key: "consentPurposeId" },
                  dataAttributeIds: { key: "dataAttributeId", asArray: true },
                },
                when: { key: "status", in: ["none", "withdrawn", "expired"] },
              }),
              operation("update", "PUT", "Consents/:consentId", [
                field("expiresAt", "datetime-local"),
              ], {
                when: { key: "status", in: ["granted"] },
              }),
              operation(
                "withdraw",
                "PUT",
                "Consents/:consentId/withdraw",
                undefined,
                {
                  confirm: true,
                  destructive: true,
                  description: t("withdrawConsentHint"),
                  when: { key: "status", in: ["granted"] },
                },
              ),
            ],
          },
        ),
      ];

    case "student.consentRequests":
      return [
        workspace(
          "receivedRequests",
          "ConsentRequests/me/received",
          [
            "consentRequestId",
            "advisorName",
            "consentPurposeName",
            ["attributeName", "dataAttributeName"],
            "purposeStated",
            "status",
            "createdAt",
          ],
          {
            rowOperations: [
              operation("grant", "PUT", "ConsentRequests/:consentRequestId/grant", undefined, {
                confirm: true,
              }),
              operation("deny", "PUT", "ConsentRequests/:consentRequestId/deny", undefined, {
                confirm: true,
              }),
            ],
          },
        ),
      ];

    case "student.corrections":
      return [
        workspace(
          "studentCorrections",
          "CorrectionRequests/me",
          [
            "correctionRequestId",
            "studentDataId",
            "proposedValue",
            "status",
            "createdAt",
          ],
          {
            create: operation("create", "POST", "CorrectionRequests", [
              {
                key: "studentDataId",
                label: t("correctionDataValue"),
                type: "select",
                required: true,
                optionsFrom: {
                  path: "StudentData/me",
                  collectionKey: "values",
                  valueKey: "studentDataId",
                  labelKeys: ["attributeName", "termName", "value"],
                  match: {
                    hasValue: true,
                    isMissing: false,
                    isErased: false,
                  },
                  requiredValueKeys: ["value"],
                  labels: {
                    loading: t("loadingStudentDataValues"),
                    error: t("studentDataOptionsUnavailable"),
                    select: t("selectStudentDataValue"),
                    empty: t("noCorrectableStudentData"),
                  },
                },
              },
              field("explanation", "textarea", true),
              field("proposedValue", "textarea", true),
            ]),
            rowOperations: [
              operation(
                "withdraw",
                "PUT",
                "CorrectionRequests/:correctionRequestId/withdraw",
                undefined,
                { confirm: true },
              ),
            ],
          },
        ),
      ];

    case "student.data":
      return [
        workspace(
          "studentData",
          "StudentData/me",
          [
            ["attributeName", "dataAttributeName"],
            "value",
            "termName",
            "isMissing",
            "isErased",
            "validFrom",
          ],
          { queryFields: [picker("termId", termSource, false, "select", "term")] },
        ),
      ];

    case "student.access":
      return [
        workspace(
          "accessLogs",
          "AccessLogs/me",
          [
            "accessLogId",
            "accessorName",
            "roleName",
            "purposeName",
            "accessedAt",
            "isFlagged",
          ],
          {
            queryFields: [
              field("fromDate", "date"),
              field("toDate", "date"),
              field("roleName"),
            ],
            rowOperations: [
              operation(
                "flag",
                "PUT",
                "AccessLogs/me/:accessLogId/flag",
                undefined,
                { confirm: true },
              ),
            ],
            pageSize: 30,
          },
        ),
      ];

    case "student.events":
      return [
        workspace(
          "activityLog",
          "ActivityLogs/me",
          ["activityType", "createdAt", "details"],
          { pageSize: 30 },
        ),
      ];

    case "advisor.caseload":
      return [
        workspace(
          "myCases",
          "Cases/assigned",
          [
            "studentName",
            "studentCode",
            ["categoryName", "supportCategoryName"],
            "subjectName",
            "currentStage",
            "priority",
            "unreadNotificationCount",
            "openedAt",
          ],
          {
            description: t("myCasesDescription"),
            emptyTitle: t("myCasesEmptyTitle"),
            emptyBody: t("myCasesEmptyBody"),
            rowHref: "/advisor/cases/:caseId",
            queryFields: [
              select("currentStage", caseStageOptions),
              select("priority", priorityOptions),
              select("supportType", ["academic", "wellbeing"]),
              field("student"),
              select("status", caseStatusOptions),
              field("unreadOnly", "checkbox"),
              select("sortBy", ["openedAt", "priority", "stage", "studentName"]),
              select("sortDirection", ["asc", "desc"]),
            ],
            pageSize: 20,
          },
        ),
      ];

    case "advisor.caseDetail":
      return [
        workspace(
          "cases",
          "Cases/:caseId",
          [
            "caseId",
            "studentName",
            ["categoryName", "supportCategoryName"],
            "supportType",
            "subjectName",
            "requestReason",
            "currentStage",
            "status",
            "priority",
            "openedAt",
          ],
          {
            rowOperations: [
              operation("update", "PUT", "Cases/:caseId/stage", [
                select("toStage", ["contacted", "followUp", "closed"], true),
                field("reason", "textarea", true),
              ]),
              operation("update", "PUT", "Cases/:caseId/priority", [
                select("priority", priorityOptions, true),
              ]),
              operation("update", "PUT", "Cases/:caseId/reopen", [
                field("reason", "textarea", true),
              ], { confirm: true }),
              operation("create", "POST", "Referrals", [
                search("toAccountId", staffSearch(["Academic Advisor", "Wellbeing Advisor", "Student Welfare Officer"]), true, "referTo"),
                field("referralReason", "textarea", true),
                select("priority", priorityOptions),
              ], {
                bodyFromRecord: { caseId: { key: "caseId" } },
              }),
              operation("create", "POST", "CaseNotes", [
                field("noteText", "textarea", true),
              ], {
                bodyFromRecord: { caseId: { key: "caseId" } },
              }),
            ],
          },
        ),
      ];

    case "advisor.consentRequests":
      return [
        workspace(
          "sentRequests",
          "ConsentRequests/me/sent",
          [
            "consentRequestId",
            "studentName",
            "consentPurposeName",
            ["attributeName", "dataAttributeName"],
            "purposeStated",
            "status",
            "createdAt",
          ],
          {
            create: operation("create", "POST", "ConsentRequests", [
              picker("consentPurposeId", activePurposeSource, true, "select", "consentPurpose"),
              picker("dataAttributeId", activeAttributeSource, true, "select", "dataAttributeName"),
              search("studentId", studentSearch, true, "student"),
              field("purposeStated", "textarea", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "ConsentRequests/:consentRequestId", [
                field("purposeStated", "textarea", true),
              ]),
              operation(
                "withdraw",
                "PUT",
                "ConsentRequests/:consentRequestId/withdraw",
                undefined,
                { confirm: true },
              ),
            ],
          },
        ),
      ];

    case "advisor.corrections":
      return [
        workspace(
          "studentCorrections",
          "CorrectionRequests/search",
          [
            "correctionRequestId",
            "studentName",
            ["attributeName", "dataAttributeName"],
            "proposedValue",
            "status",
            "createdAt",
          ],
          {
            queryFields: [
              search("studentId", studentSearch, false, "student"),
              field("status"),
            ],
            rowOperations: [
              operation("approve", "PUT", "CorrectionRequests/:correctionRequestId/approve", [
                field("decisionNotes", "textarea", true),
              ], { confirm: true }),
              operation("reject", "PUT", "CorrectionRequests/:correctionRequestId/reject", [
                field("decisionNotes", "textarea", true),
              ], { confirm: true }),
            ],
          },
        ),
      ];

    case "advisor.referrals":
      return [
        workspace(
          "receivedRequests",
          "Referrals/me/incoming",
          [
            "studentName",
            ["categoryName", "supportCategoryName"],
            "fromAccountName",
            "referralReason",
            "priority",
            "status",
            "createdAt",
          ],
          {
            description: t("referralsDescription"),
            emptyTitle: t("referralsEmptyTitle"),
            emptyBody: t("referralsEmptyBody"),
            queryFields: [select("status", ["pending", "accepted", "returned"])],
            rowOperations: [
              operation("accept", "PUT", "Referrals/:referralId/accept", undefined, {
                confirm: true,
                description: t("acceptReferralHint"),
              }),
              operation("returnReferral", "PUT", "Referrals/:referralId/return", [
                field("reason", "textarea", true),
              ], { confirm: true, destructive: true }),
            ],
          },
        ),
        workspace(
          "sentRequests",
          "Referrals/me/outgoing",
          [
            "studentName",
            ["categoryName", "supportCategoryName"],
            "toAccountName",
            "referralReason",
            "priority",
            "status",
            "createdAt",
          ],
          { queryFields: [select("status", ["pending", "accepted", "returned"])] },
        ),
      ];

    case "advisor.emergency":
      return [
        workspace(
          "myEmergencyRequests",
          "EmergencyAccessRequests/me",
          [
            "emergencyAccessRequestId",
            "studentName",
            "justification",
            "status",
            "scopeExpiresAt",
            "isExpired",
          ],
          {
            create: operation("request", "POST", "EmergencyAccessRequests", [
              search("studentId", studentSearch, true, "student"),
              field("justification", "textarea", true),
            ], { confirm: true }),
            rowOperations: [
              operation("update", "PUT", "EmergencyAccessRequests/:emergencyAccessRequestId", [
                field("justification", "textarea", true),
              ]),
            ],
          },
        ),
      ];

    case "advisor.studentData":
      return [
        workspace(
          "studentData",
          "StudentData/students/:studentId",
          [["attributeName", "dataAttributeName"], "value", "termName", "isShared", "isMissing", "validFrom"],
          {
            queryFields: [
              picker("caseId", advisorCaseSource, false, "select", "case"),
              picker("termId", termSource, false, "select", "term"),
            ],
          },
        ),
      ];

    case "advisor.historical":
      return [
        workspace(
          "studentData",
          "StudentData/students/:studentId/as-of",
          [["attributeName", "dataAttributeName"], "value", "termName", "isShared", "isMissing", "validFrom"],
          {
            queryFields: [
              field("date", "date", true),
              select("timeAxis", ["system", "valid"]),
              picker("caseId", advisorCaseSource, false, "select", "case"),
              picker("termId", termSource, false, "select", "term"),
            ],
          },
        ),
      ];

    case "advisor.compare":
      return [
        workspace(
          "studentData",
          "StudentData/students/:studentId/compare",
          [["attributeName", "dataAttributeName"], "dateA", "dateB", "valueA", "valueB"],
          {
            queryFields: [
              field("dateA", "date", true),
              field("dateB", "date", true),
              select("timeAxis", ["system", "valid"]),
              picker("caseId", advisorCaseSource, false, "select", "case"),
              picker("termId", termSource, false, "select", "term"),
            ],
          },
        ),
      ];

    case "advisor.caseHistory":
      return [
        workspace(
          "caseHistory",
          "CaseStageHistories/search",
          [
            "caseStageHistoryId",
            "fromStage",
            "toStage",
            "reason",
            "changedByName",
            "createdAt",
          ],
          { queryFields: [picker("caseId", advisorCaseSource, true, "select", "case")] },
        ),
      ];

    case "welfare.referrals":
      return [
        workspace(
          "welfareReferrals",
          "Referrals/me/incoming",
          [
            "studentName",
            ["categoryName", "supportCategoryName"],
            "fromAccountName",
            "referralReason",
            "priority",
            "status",
            "createdAt",
          ],
          {
            description: t("welfareReferralsDescription"),
            emptyTitle: t("referralsEmptyTitle"),
            emptyBody: t("referralsEmptyBody"),
            queryFields: [select("status", ["pending", "accepted", "returned"])],
            rowOperations: [
              operation("accept", "PUT", "Referrals/:referralId/accept", undefined, {
                confirm: true,
                description: t("acceptWelfareReferralHint"),
              }),
              operation("changePriority", "PUT", "Cases/:caseId/priority", [
                select("priority", priorityOptions, true),
              ]),
              operation("returnReferral", "PUT", "Referrals/:referralId/return", [
                field("reason", "textarea", true),
              ], { confirm: true, destructive: true }),
              operation("escalate", "POST", "Escalations", [
                field("escalatedTo", "text", true),
                field("reason", "textarea", true),
              ], { bodyFromRecord: { caseId: { key: "caseId" } } }),
            ],
          },
        ),
      ];

    case "welfare.escalations":
      return [
        workspace(
          "escalations",
          "Escalations/search",
          ["caseId", "escalatedToFullName", "reason", "response", "status", "escalatedAt"],
          {
            description: t("welfareEscalationsDescription"),
            emptyTitle: t("escalationsEmptyTitle"),
            emptyBody: t("escalationsEmptyBody"),
            queryFields: [select("status", ["pending", "responded", "closed"])],
          },
        ),
      ];

    case "welfare.interventions":
      return [
        workspace(
          "welfareInterventions",
          "Interventions/search",
          [
            "interventionId",
            "caseId",
            "studentName",
            "interventionType",
            "interventionDate",
            "status",
            "outcome",
          ],
          {
            queryFields: [
              picker("caseId", welfareCaseSource, false, "select", "case"),
              search("studentId", studentSearch, false, "student"),
              field("status"),
            ],
            create: operation("create", "POST", "Interventions", [
              picker("caseId", welfareCaseSource, true, "select", "case"),
              selectValues("interventionType", [
                { label: "loanArrangement", value: "loanArrangement" },
                { label: "financialAid", value: "financialAid" },
                { label: "bankingPaperwork", value: "bankingPaperwork" },
                { label: "other", value: "other" },
              ], true),
              field("interventionDate", "date", true),
              field("notes", "textarea"),
              select("status", ["scheduled", "inProgress"], true),
            ]),
            rowOperations: [
              operation("update", "PUT", "Interventions/:interventionId", [
                selectValues("interventionType", [
                  { label: "loanArrangement", value: "loanArrangement" },
                  { label: "financialAid", value: "financialAid" },
                  { label: "bankingPaperwork", value: "bankingPaperwork" },
                  { label: "other", value: "other" },
                ], true),
                field("interventionDate", "date", true),
                field("notes", "textarea"),
                field("outcome", "textarea"),
                select("status", ["scheduled", "inProgress"], true),
              ]),
              operation("close", "PUT", "Interventions/:interventionId/close", [
                field("outcome", "textarea", true),
              ], { confirm: true }),
            ],
          },
        ),
      ];

    case "welfare.caseDetail":
      return [
        workspace(
          "cases",
          "Cases/:caseId",
          [
            "caseId",
            "studentName",
            ["categoryName", "supportCategoryName"],
            "currentStage",
            "status",
            "priority",
            "requestReason",
          ],
          {
            rowOperations: [
              operation("update", "PUT", "Cases/:caseId/priority", [
                select("priority", priorityOptions, true),
              ]),
            ],
          },
        ),
      ];

    case "welfare.externalServices":
      return [
        workspace(
          "externalServices",
          "ExternalServices/search",
          ["externalServiceId", "serviceName", "serviceType", "contactInfo", "isActive"],
          {
            queryFields: [field("serviceName"), field("serviceType"), field("isActive", "checkbox")],
            create: operation("create", "POST", "ExternalServices", [
              field("serviceName", "text", true),
              field("serviceType", "text", true),
              field("contactInfo", "textarea"),
            ]),
            rowOperations: [
              operation("update", "PUT", "ExternalServices/:externalServiceId", [
                field("serviceName", "text", true),
                field("serviceType", "text", true),
                field("contactInfo", "textarea"),
                field("isActive", "checkbox"),
              ]),
            ],
          },
        ),
      ];

    case "welfare.externalReferrals":
      return [
        workspace(
          "externalReferrals",
          "ExternalReferrals/search",
          [
            "externalReferralId",
            "caseId",
            "serviceName",
            "status",
            "followUpDueAt",
            "followUpOutcome",
          ],
          {
            queryFields: [picker("caseId", welfareCaseSource, false, "select", "case"), field("isOverdue", "checkbox")],
            create: operation("create", "POST", "ExternalReferrals", [
              picker("caseId", welfareCaseSource, true, "select", "case"),
              picker("externalServiceId", externalServiceSource, true, "select", "externalService"),
              field("reason", "textarea", true),
              field("followUpDueAt", "date"),
            ]),
            rowOperations: [
              operation("update", "PUT", "ExternalReferrals/:externalReferralId", [
                field("followUpDueAt", "date"),
                field("followUpOutcome", "textarea"),
                field("status"),
              ]),
              operation("close", "PUT", "ExternalReferrals/:externalReferralId/close", [
                field("followUpOutcome", "textarea", true),
              ], { confirm: true }),
            ],
          },
        ),
      ];

    case "steward.purposes":
      return [
        workspace(
          "purposes",
          "ConsentPurposes/search",
          ["consentPurposeId", "purposeName", "description", "status"],
          {
            queryFields: [field("purposeName"), field("status")],
            create: operation("create", "POST", "ConsentPurposes", [
              field("purposeName", "text", true),
              field("description", "textarea", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "ConsentPurposes/:consentPurposeId", [
                field("purposeName", "text", true),
                field("description", "textarea", true),
              ]),
              operation("activate", "PUT", "ConsentPurposes/:consentPurposeId/activate", undefined, {
                confirm: true,
              }),
              operation("deactivate", "PUT", "ConsentPurposes/:consentPurposeId/deactivate", undefined, {
                confirm: true,
              }),
              operation("archive", "PUT", "ConsentPurposes/:consentPurposeId/retire", undefined, {
                confirm: true,
                destructive: true,
                when: { key: "status", notIn: ["retired"] },
              }),
              // Attributes are picked from the database; the purpose comes from the row.
              operation("addPurposeAttribute", "POST", "ConsentPurposes/purpose-attributes", [
                picker("dataAttributeId", activeAttributeSource, true, "select", "dataAttributeName"),
              ], {
                icon: "playlist_add",
                bodyFromRecord: { consentPurposeId: { key: "consentPurposeId" } },
                when: { key: "status", notIn: ["retired"] },
              }),
            ],
          },
        ),
        workspace(
          "purposeAttributes",
          "ConsentPurposes/purpose-attributes/search",
          [
            "consentPurposeId",
            "purposeName",
            "dataAttributeId",
            "attributeName",
            "status",
          ],
          {
            queryFields: [picker("consentPurposeId", purposeSource, false, "select", "consentPurpose"), picker("dataAttributeId", attributeSource, false, "select", "dataAttributeName")],
            create: operation("create", "POST", "ConsentPurposes/purpose-attributes", [
              picker("consentPurposeId", purposeSource, true, "select", "consentPurpose"),
              picker("dataAttributeId", activeAttributeSource, true, "select", "dataAttributeName"),
            ]),
            rowOperations: [
              operation(
                "remove",
                "DELETE",
                "ConsentPurposes/purpose-attributes/:consentPurposeId/:dataAttributeId",
                undefined,
                { confirm: true },
              ),
            ],
          },
        ),
      ];

    case "steward.policies":
      return [
        workspace(
          "policies",
          "AccessPolicies/search",
          [
            ["roleName", "role"],
            ["purposeName", "consentPurpose"],
            ["creatorFullName", "createdBy"],
            "status",
          ],
          {
            queryFields: [
              picker("roleId", roleIdSource(true), false, "select", "role"),
              picker("consentPurposeId", purposeSource, false, "select", "consentPurpose"),
              selectValues("status", [
                { label: "draft", value: "draft" },
                { label: "active", value: "active" },
                { label: "revoked", value: "revoked" },
              ]),
            ],
            // Students always see their own data, so the Student role never gets a policy.
            create: operation("create", "POST", "AccessPolicies", [
              picker("roleId", roleIdSource(true), true, "select", "role"),
              picker("consentPurposeId", purposeSource, true, "select", "consentPurpose"),
            ]),
            rowOperations: [
              operation("update", "PUT", "AccessPolicies/:accessPolicyId", [
                picker("roleId", roleIdSource(true), true, "select", "role"),
                picker("consentPurposeId", purposeSource, true, "select", "consentPurpose"),
              ], {
                when: { key: "status", in: ["draft"] },
              }),
              operation("activate", "PUT", "AccessPolicies/:accessPolicyId/activate", undefined, {
                confirm: true,
                when: { key: "status", in: ["draft"] },
              }),
              operation("revoke", "PUT", "AccessPolicies/:accessPolicyId/revoke", undefined, {
                confirm: true,
                destructive: true,
                when: { key: "status", notIn: ["revoked"] },
              }),
            ],
          },
        ),
      ];

    case "steward.attributes":
      return [
        workspace(
          "dataAttributes",
          "DataAttributes/search",
          ["dataAttributeId", "attributeName", "dataType", "status"],
          {
            queryFields: [field("attributeName"), field("status")],
          },
        ),
      ];

    case "steward.consents":
      return [
        workspace(
          "consent",
          "Consents/histories/search",
          [
            ["changedAt", "changedAt"],
            ["changedByFullName", "changedBy"],
            ["purposeName", "consentPurpose"],
            ["attributeName", "dataAttributeName"],
            ["previousStatus", "previousStatus"],
            ["newStatus", "newStatus"],
          ],
          {
            queryFields: [search("studentId", studentSearch, false, "student"), field("consentId")],
          },
        ),
        workspace(
          "receivedRequests",
          "ConsentRequests/search",
          [
            ["studentFullName", "studentName"],
            ["advisorFullName", "advisorName"],
            ["purposeName", "consentPurpose"],
            ["attributeName", "dataAttributeName"],
            "status",
            "requestedAt",
          ],
          {
            queryFields: [
              selectValues("status", [
                { label: "pending", value: "pending" },
                { label: "granted", value: "granted" },
                { label: "denied", value: "denied" },
                { label: "withdrawn", value: "withdrawn" },
              ]),
              picker("consentPurposeId", purposeSource, false, "select", "consentPurpose"),
              search("studentId", studentSearch, false, "student"),
              search("advisorId", staffSearch(advisorRoles), false, "advisor"),
            ],
          },
        ),
      ];

    case "steward.accessRecords":
      return [
        workspace(
          "accessLogs",
          "AccessLogs/search",
          [
            "accessLogId",
            "studentName",
            "accessorName",
            "roleName",
            "purposeName",
            "accessedAt",
            "isFlagged",
            "reviewStatus",
          ],
          {
            queryFields: [
              search("recordedUserId", studentSearch, false, "student"),
              search("accessorId", staffSearch(), false, "accessorName"),
              field("fromDate", "date"),
              field("toDate", "date"),
              field("isFlagged", "checkbox"),
            ],
            rowOperations: [
              operation("flag", "PUT", "AccessLogs/:accessLogId/flag", undefined, {
                confirm: true,
              }),
              operation("review", "PUT", "AccessLogs/:accessLogId/review", [
                select("status", ["cleared", "confirmedIssue"], true),
              ]),
            ],
            pageSize: 30,
          },
        ),
      ];

    case "steward.erasure":
      return [
        workspace(
          "erasure",
          "ErasureRequests/search",
          [
            "erasureRequestId",
            "studentName",
            "reason",
            "status",
            "requestedAt",
            "reviewedAt",
          ],
          {
            queryFields: [search("studentId", studentSearch, false, "student"), field("status")],
            rowOperations: [
              operation(
                "execute",
                "PUT",
                "ErasureRequests/:erasureRequestId/execute",
                undefined,
                { confirm: true },
              ),
            ],
          },
        ),
      ];

    case "steward.emergency":
      return [
        workspace(
          "emergencyAccess",
          "EmergencyAccessRequests/search",
          [
            "emergencyAccessRequestId",
            "requestedBy",
            "studentName",
            "justification",
            "status",
            "scopeExpiresAt",
            "isExpired",
          ],
          {
            queryFields: [search("studentId", studentSearch, false, "student"), field("status")],
            rowOperations: [
              operation("approve", "PUT", "EmergencyAccessRequests/:emergencyAccessRequestId/approve", [
                field("scopeExpiresAt", "datetime-local", true),
              ], { confirm: true }),
              operation("reject", "PUT", "EmergencyAccessRequests/:emergencyAccessRequestId/reject", [
                field("rejectionReason", "textarea", true),
              ], { confirm: true }),
            ],
          },
        ),
      ];

    case "steward.keys":
      return [
        workspace(
          "attributeKeys",
          "AttributeKeys/search",
          [
            "attributeKeyId",
            "dataAttributeId",
            "attributeName",
            "keyVersion",
            "status",
            "createdAt",
          ],
          {
            queryFields: [picker("dataAttributeId", attributeSource, false, "select", "dataAttributeName"), field("status")],
            globalOperations: [
              operation("rewrap", "PUT", "AttributeKeys/rewrap", undefined, {
                confirm: true,
              }),
            ],
          },
        ),
      ];

    case "head.matching":
      return [
        workspace(
          "matchingQueue",
          "Cases/matching-queue",
          [
            "caseId",
            "studentName",
            ["categoryName", "supportCategoryName"],
            "supportType",
            "subjectName",
            "priority",
            "openedAt",
          ],
          {
            rowOperations: [
              operation("preview", "GET", "Cases/:caseId/suggested-advisors"),
              operation("assign", "PUT", "Cases/:caseId/assign", [
                search("advisorId", staffSearch(advisorRoles), true, "advisor"),
                field("reason", "textarea"),
              ], { confirm: true }),
              operation("update", "PUT", "Cases/:caseId/reassign", [
                search("advisorId", staffSearch(advisorRoles), true, "advisor"),
                field("reason", "textarea", true),
              ], { confirm: true }),
            ],
          },
        ),
      ];

    case "head.cases":
      return [
        workspace(
          "allCases",
          "Cases/search",
          [
            "studentName",
            "advisorName",
            ["categoryName", "supportCategoryName"],
            "supportType",
            "currentStage",
            "priority",
            "openedAt",
          ],
          {
            description: t("allCasesDescription"),
            queryFields: [
              select("currentStage", caseStageOptions),
              select("priority", priorityOptions),
              select("supportType", ["academic", "wellbeing"]),
              select("status", caseStatusOptions),
              search("studentId", studentSearch, false, "student"),
              search("advisorId", staffSearch(advisorRoles), false, "advisor"),
              picker("supportCategoryId", categorySource, false, "select", "supportCategory"),
            ],
            rowOperations: [
              operation("reassign", "PUT", "Cases/:caseId/reassign", [
                search("advisorId", staffSearch(advisorRoles), true, "advisor"),
                field("reason", "textarea", true),
              ], { confirm: true }),
            ],
            pageSize: 20,
          },
        ),
      ];

    case "head.changeRequests":
      return [
        workspace(
          "caseChangeRequests",
          "CaseChangeRequests/search",
          [
            "studentName",
            ["categoryName", "supportCategoryName"],
            "currentAdvisorName",
            "proposedAdvisorName",
            "requestedByName",
            "reason",
            "status",
            "requestedAt",
          ],
          {
            description: t("caseChangeRequestsDescription"),
            emptyTitle: t("caseChangeRequestsEmptyTitle"),
            emptyBody: t("caseChangeRequestsEmptyBody"),
            queryFields: [select("status", ["pending", "approved", "rejected"])],
            rowOperations: [
              operation("approve", "PUT", "CaseChangeRequests/:caseChangeRequestId/approve", [
                field("decisionNotes", "textarea"),
              ], { confirm: true, description: t("approveChangeHint") }),
              operation("reject", "PUT", "CaseChangeRequests/:caseChangeRequestId/reject", [
                field("reason", "textarea", true),
              ], { confirm: true, destructive: true }),
            ],
          },
        ),
      ];

    case "head.escalations":
      return [
        workspace(
          "escalations",
          "Escalations/search",
          ["caseId", "escalatedByFullName", "reason", "response", "status", "escalatedAt"],
          {
            description: t("headEscalationsDescription"),
            emptyTitle: t("escalationsEmptyTitle"),
            emptyBody: t("escalationsEmptyBody"),
            queryFields: [select("status", ["pending", "responded", "closed"])],
            rowOperations: [
              operation("respond", "PUT", "Escalations/:escalationId/respond", [
                field("response", "textarea", true),
              ]),
            ],
          },
        ),
      ];

    case "head.reports":
      return [
        workspace(
          "caseReports",
          "Reports/cases",
          [
            "caseId",
            "studentName",
            "advisorName",
            "supportType",
            "currentStage",
            "status",
            "priority",
            "openedAt",
          ],
          {
            queryFields: [
              field("from", "date", true),
              field("to", "date", true),
              search("advisorId", staffSearch(advisorRoles), false, "advisor"),
              field("supportType"),
              picker("subjectId", subjectSource, false, "select", "subject"),
              field("currentStage"),
            ],
          },
        ),
        workspace(
          "caseReports",
          "Reports/caseload-closure",
          [
            ["advisorFullName", "advisorName"],
            "totalCases",
            "openCases",
            "closedCases",
            "averageDaysToClose",
          ],
          { queryFields: [field("from", "date", true), field("to", "date", true)] },
        ),
      ];

    case "head.workload":
      return [
        workspace(
          "advisorWorkload",
          "Reports/caseload-closure",
          [
            ["advisorFullName", "advisorName"],
            "totalCases",
            "openCases",
            "closedCases",
            "averageDaysToClose",
          ],
          { queryFields: [field("from", "date", true), field("to", "date", true)] },
        ),
      ];

    case "notifications":
      return [
        workspace(
          "notifications",
          "Notifications/me",
          ["notificationId", "title", "message", "createdAt", "isRead"],
          {
            rowOperations: [
              operation("markRead", "PUT", "Notifications/me/:notificationId/read", undefined, { confirm: false }),
              operation("remove", "DELETE", "Notifications/me/:notificationId", undefined, {
                confirm: true,
              }),
            ],
            globalOperations: [
              operation("markAllRead", "PUT", "Notifications/me/read-all", undefined, { confirm: false }),
              operation("clearRead", "DELETE", "Notifications/me/clear", undefined, {
                confirm: true,
              }),
            ],
          },
        ),
      ];
  }
}
