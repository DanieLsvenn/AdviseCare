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
  | "admin.roles"
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
      "confirm" | "bodyFromRecord" | "destructive" | "description" | "icon" | "when"
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
              field("facultyId"),
              field("roleName"),
              selectValues("academicStatus", academicStatusOptions),
              field("linkStatus"),
              select("status", ["active", "inactive", "banned"] as WorkspaceKey[]),
            ],
            globalOperations: [
              {
                ...operation("importAccounts", "POST", "Accounts/import", [
                  {
                    key: "file",
                    label: t("file"),
                    type: "file",
                    required: true,
                    accept: ".xlsx",
                  },
                ]),
                description: t("accountImportDescription"),
              },
            ],
            create: operation("create", "POST", "Accounts", [
              field("email", "email", true),
              field("fullName", "text", true),
              field("userName", "text", true),
              field("facultyId"),
              field("roleNames", "list", true),
              field("studentCode"),
              selectValues("academicStatus", academicStatusOptions),
            ]),
            rowOperations: [
              operation("update", "PUT", "Accounts/:accountId", [
                field("email", "email", true),
                field("fullName", "text", true),
                field("userName", "text", true),
                field("facultyId"),
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
                "addRole",
                "POST",
                "Accounts/roles",
                [
                  field("roleId", "text", true),
                  field("studentCode"),
                  selectValues("academicStatus", academicStatusOptions),
                ],
                {
                  bodyFromRecord: { accountId: { key: "accountId" } },
                  icon: "badge",
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

    case "admin.roles":
      return [
        workspace(
          "roleAssignments",
          "Accounts/roles/search",
          ["accountId", "accountName", "roleId", "roleName", "createdAt"],
          {
            queryFields: [field("accountId"), field("roleId")],
            create: operation("addRole", "POST", "Accounts/roles", [
              field("accountId", "text", true),
              field("roleId", "text", true),
              field("studentCode"),
              selectValues("academicStatus", academicStatusOptions),
            ]),
            rowOperations: [
              operation(
                "removeRole",
                "DELETE",
                "Accounts/:accountId/roles/:roleId",
                undefined,
                { confirm: true },
              ),
            ],
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
            queryFields: [field("programmeCode"), field("programmeName"), field("facultyId")],
            create: operation("create", "POST", "Programmes", [
              field("programmeCode", "text", true),
              field("programmeName", "text", true),
              field("facultyId", "text", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "Programmes/:programmeId", [
                field("programmeCode", "text", true),
                field("programmeName", "text", true),
                field("facultyId", "text", true),
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
          ["academicTermId", "termName", "startDate", "endDate", "status"],
          {
            queryFields: [field("termName"), field("status")],
            create: operation("create", "POST", "AcademicTerms", [
              field("termName", "text", true),
              field("startDate", "date", true),
              field("endDate", "date", true),
              select("status", ["upcoming", "current", "past"], true),
            ]),
            rowOperations: [
              operation("update", "PUT", "AcademicTerms/:academicTermId", [
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
              field("facultyId"),
            ],
            create: operation("create", "POST", "Subjects", [
              field("facultyId", "text", true),
              field("subjectCode", "text", true),
              field("subjectName", "text", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "Subjects/:subjectId", [
                field("facultyId", "text", true),
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
            queryFields: [field("accountId"), field("subjectId")],
            create: operation("create", "POST", "Subjects/account-subjects", [
              field("accountId", "text", true),
              field("subjectId", "text", true),
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
            "studentDataId",
            "studentId",
            "dataAttributeName",
            "termName",
            "validFrom",
            "status",
            "isMissing",
            "isErased",
          ],
          {
            queryFields: [
              field("studentId"),
              field("dataAttributeId"),
              field("termId"),
              field("status"),
              field("isMissing", "checkbox"),
            ],
            pageSize: 20,
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
              field("consentPurposeId", "text", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "SupportCategories/:supportCategoryId", [
                field("categoryName", "text", true),
                field("description", "textarea", true),
                select("supportType", ["academic", "wellbeing"], true),
                field("consentPurposeId", "text", true),
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
            queryFields: [field("accountId"), field("isActive", "checkbox")],
            rowOperations: [
              operation(
                "revoke",
                "PUT",
                "Sessions/:sessionId/revoke",
                undefined,
                { confirm: true },
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
              operation("update", "PUT", "Auth/me", [
                field("userName", "text", true),
                selectValues(
                  "preferredLanguage",
                  [
                    { label: "english", value: "en" },
                    { label: "vietnamese", value: "vi" },
                  ],
                  true,
                ),
              ]),
              operation("update", "POST", "Auth/change-password", [
                field("oldPassword", "text", true),
                field("newPassword", "text", true),
                field("confirmPassword", "text", true),
              ]),
              operation("update", "POST", "Auth/me/avatar", [
                field("avatar", "file", true),
              ]),
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
        workspace(
          "consent",
          "Consents/me",
          [
            "consentId",
            "consentPurposeName",
            "dataAttributeName",
            "status",
            "expiresAt",
          ],
          {
            create: operation("create", "POST", "Consents", [
              field("consentPurposeId", "text", true),
              field("dataAttributeIds", "list", true),
              field("expiresAt", "datetime-local"),
            ]),
            rowOperations: [
              operation("update", "PUT", "Consents/:consentId", [
                field("expiresAt", "datetime-local"),
              ]),
              operation(
                "withdraw",
                "PUT",
                "Consents/:consentId/withdraw",
                undefined,
                { confirm: true },
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
            "dataAttributeName",
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
            "dataAttributeName",
            "value",
            "termName",
            "isMissing",
            "isErased",
            "validFrom",
          ],
          { queryFields: [field("termId")] },
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
                field("toAccountId", "text", true),
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
            "dataAttributeName",
            "purposeStated",
            "status",
            "createdAt",
          ],
          {
            create: operation("create", "POST", "ConsentRequests", [
              field("consentPurposeId", "text", true),
              field("dataAttributeId", "text", true),
              field("studentId", "text", true),
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
            "dataAttributeName",
            "proposedValue",
            "status",
            "createdAt",
          ],
          {
            queryFields: [
              field("studentId"),
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
              field("studentId", "text", true),
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
          ["dataAttributeName", "value", "termName", "isShared", "isMissing", "validFrom"],
          {
            queryFields: [
              field("caseId"),
              field("termId"),
            ],
          },
        ),
      ];

    case "advisor.historical":
      return [
        workspace(
          "studentData",
          "StudentData/students/:studentId/as-of",
          ["dataAttributeName", "value", "termName", "isShared", "isMissing", "validFrom"],
          {
            queryFields: [
              field("date", "date", true),
              select("timeAxis", ["system", "valid"]),
              field("caseId"),
              field("termId"),
            ],
          },
        ),
      ];

    case "advisor.compare":
      return [
        workspace(
          "studentData",
          "StudentData/students/:studentId/compare",
          ["dataAttributeName", "dateA", "dateB", "valueA", "valueB"],
          {
            queryFields: [
              field("dateA", "date", true),
              field("dateB", "date", true),
              select("timeAxis", ["system", "valid"]),
              field("caseId"),
              field("termId"),
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
          { queryFields: [field("caseId", "text", true)] },
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
              field("caseId"),
              field("studentId"),
              field("status"),
            ],
            create: operation("create", "POST", "Interventions", [
              field("caseId", "text", true),
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
            queryFields: [field("caseId"), field("isOverdue", "checkbox")],
            create: operation("create", "POST", "ExternalReferrals", [
              field("caseId", "text", true),
              field("externalServiceId", "text", true),
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
            queryFields: [field("consentPurposeId"), field("dataAttributeId")],
            create: operation("create", "POST", "ConsentPurposes/purpose-attributes", [
              field("consentPurposeId", "text", true),
              field("dataAttributeId", "text", true),
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
            "accessPolicyId",
            "roleName",
            "consentPurposeName",
            "status",
            "createdAt",
          ],
          {
            queryFields: [field("roleId"), field("consentPurposeId"), field("status")],
            create: operation("create", "POST", "AccessPolicies", [
              field("roleId", "text", true),
              field("consentPurposeId", "text", true),
            ]),
            rowOperations: [
              operation("update", "PUT", "AccessPolicies/:accessPolicyId", [
                field("roleId", "text", true),
                field("consentPurposeId", "text", true),
              ]),
              operation("activate", "PUT", "AccessPolicies/:accessPolicyId/activate", undefined, {
                confirm: true,
              }),
              operation("revoke", "PUT", "AccessPolicies/:accessPolicyId/revoke", undefined, {
                confirm: true,
              }),
              operation("test", "POST", "AccessPolicies/:accessPolicyId/test", [
                field("studentId", "text", true),
              ]),
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
            "consentHistoryId",
            "consentId",
            "studentName",
            "purposeName",
            "dataAttributeName",
            "status",
            "createdAt",
          ],
          {
            queryFields: [field("studentId"), field("consentId")],
          },
        ),
        workspace(
          "receivedRequests",
          "ConsentRequests/search",
          [
            "consentRequestId",
            "studentName",
            "advisorName",
            "purposeName",
            "dataAttributeName",
            "status",
          ],
          {
            queryFields: [
              field("studentId"),
              field("advisorId"),
              field("consentPurposeId"),
              field("dataAttributeId"),
              field("status"),
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
              field("studentId"),
              field("recordedUserId"),
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
            queryFields: [field("studentId"), field("status")],
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
            queryFields: [field("studentId"), field("status")],
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
            queryFields: [field("dataAttributeId"), field("status")],
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
                field("advisorId", "text", true),
                field("reason", "textarea"),
              ], { confirm: true }),
              operation("update", "PUT", "Cases/:caseId/reassign", [
                field("advisorId", "text", true),
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
              field("studentId"),
              field("advisorId"),
              field("supportCategoryId"),
            ],
            rowOperations: [
              operation("reassign", "PUT", "Cases/:caseId/reassign", [
                field("advisorId", "text", true),
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
              field("advisorId"),
              field("supportType"),
              field("subjectId"),
              field("currentStage"),
            ],
          },
        ),
        workspace(
          "caseReports",
          "Reports/caseload-closure",
          ["advisorName", "totalCases", "closedCases", "closureRate"],
          { queryFields: [field("from", "date", true), field("to", "date", true)] },
        ),
      ];

    case "head.workload":
      return [
        workspace(
          "advisorWorkload",
          "Reports/caseload-closure",
          ["advisorName", "totalCases", "closedCases", "closureRate"],
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
              operation("markRead", "PUT", "Notifications/me/:notificationId/read"),
              operation("remove", "DELETE", "Notifications/me/:notificationId", undefined, {
                confirm: true,
              }),
            ],
            globalOperations: [
              operation("markAllRead", "PUT", "Notifications/me/read-all"),
              operation("clearRead", "DELETE", "Notifications/me/clear", undefined, {
                confirm: true,
              }),
            ],
          },
        ),
      ];
  }
}
