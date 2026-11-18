import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import vm from "node:vm";
import ts from "typescript";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");

function loadTypeScript(file, imports) {
  const source = fs.readFileSync(path.join(root, file), "utf8");
  const code = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, esModuleInterop: true },
  }).outputText;
  const exports = {};
  vm.runInNewContext(code, {
    exports,
    require(id) {
      if (Object.hasOwn(imports, id)) return imports[id];
      throw new Error(`Unexpected import: ${id}`);
    },
  });
  return exports;
}

const { getWorkspaceDefinitions } = loadTypeScript(
  path.join("components", "workspace", "workspace-definitions.ts"),
  { "@/messages/en/workspace.json": {} },
);
const { extractRecords, getPathValue, readRecordValue } = loadTypeScript(
  path.join("components", "workspace", "workspace-helpers.ts"),
  {
    "@/lib/api-contract": {
      isRecord: (value) => value !== null && typeof value === "object" && !Array.isArray(value),
    },
    "@/lib/format": {},
  },
);

function tableValues(screen, listPath, payload) {
  const definition = getWorkspaceDefinitions(screen, (key) => key)
    .find((item) => item.listPath === listPath);
  assert.ok(definition, `Missing ${screen} table for ${listPath}`);
  const records = extractRecords(payload, definition.collectionKey);
  assert.ok(records);
  return records.map((record) => Object.fromEntries(definition.columns.map((column) => [
    column.label,
    getPathValue(record, column.key) ?? readRecordValue(record, column.key),
  ])));
}

test("advisor table columns match their API response contracts", () => {
  const contracts = [
    ["advisor.caseload", "Cases/assigned", "Case", "GetAssignedCaseWithPaginationResponse"],
    ["advisor.caseDetail", "Cases/:caseId", "Case", "GetCaseByIdResponse"],
    ["advisor.consentRequests", "ConsentRequests/me/sent", "ConsentRequest", "GetMyConsentRequestResponse"],
    ["advisor.corrections", "CorrectionRequests/search", "CorrectionRequest", "SearchCorrectionRequestResponse"],
    ["advisor.referrals", "Referrals/me/incoming", "Referral", "GetMyIncomingReferralResponse"],
    ["advisor.referrals", "Referrals/me/outgoing", "Referral", "GetMyOutgoingReferralResponse"],
    ["advisor.emergency", "EmergencyAccessRequests/me", "EmergencyAccessRequest", "GetMyEmergencyAccessRequestResponse"],
    ["advisor.studentData", "StudentData/students/:studentId", "StudentData", "StudentDataValueResponse"],
    ["advisor.historical", "StudentData/students/:studentId/as-of", "StudentData", "StudentDataValueResponse"],
    ["advisor.compare", "StudentData/students/:studentId/compare", "StudentData", "CompareStudentDataValueResponse"],
    ["advisor.caseHistory", "CaseStageHistories/search", "Case", "SearchCaseStageHistoryResponse"],
  ];
  for (const [screen, listPath, domain, response] of contracts) {
    const file = path.join(root, "..", "..", "AdviseCare_API", "AdviseCare.Application",
      "Contracts", "Responses", domain, `${domain}Response.cs`);
    const source = fs.readFileSync(file, "utf8");
    const properties = (type) => {
      const body = source.match(new RegExp(`class ${type}\\s*\\{([\\s\\S]*?)\\r?\\n    \\}`));
      assert.ok(body, `Missing response type ${type}`);
      return new Map([...body[1].matchAll(/public\s+([\w?]+)\s+(\w+)\s*\{\s*get;/g)]
        .map((match) => [match[2][0].toLowerCase() + match[2].slice(1), match[1].replace("?", "")]));
    };
    const definition = getWorkspaceDefinitions(screen, (key) => key)
      .find((item) => item.listPath === listPath);
    assert.ok(definition, `Missing ${screen} table for ${listPath}`);
    for (const column of definition.columns) {
      let type = response;
      for (const part of column.key.split(".")) {
        const nextType = properties(type).get(part);
        assert.ok(nextType, `${screen}: ${column.key} is missing from ${type}`);
        type = nextType;
      }
    }
  }
});

test("advisor case and correction tables display API response fields", () => {
  const [caseRow] = tableValues("advisor.caseload", "Cases/assigned", [
    { studentFullName: "Student A", studentCode: "S001", categoryName: "Study plan" },
  ]);
  assert.equal(caseRow.studentName, "Student A");
  assert.equal(caseRow.studentCode, "S001");
  assert.equal(caseRow.supportCategoryName, "Study plan");

  const [detail] = tableValues("advisor.caseDetail", "Cases/:caseId", [
    { studentFullName: "Student A" },
  ]);
  assert.equal(detail.studentName, "Student A");

  const [correction] = tableValues("advisor.corrections", "CorrectionRequests/search", [
    { studentFullName: "Student B", attributeName: "Attendance", proposedValue: "90", submittedAt: "2026-01-02T10:00:00Z", isShared: true },
  ]);
  assert.equal(correction.studentName, "Student B");
  assert.equal(correction.dataAttributeName, "Attendance");
  assert.equal(correction.proposedValue, "90");
  assert.equal(correction.submittedAt, "2026-01-02T10:00:00Z");

  const [withheld] = tableValues("advisor.corrections", "CorrectionRequests/search", [
    { studentFullName: "Student C", proposedValue: null, isShared: false },
  ]);
  assert.equal(withheld.proposedValue, null);
});

test("advisor request and referral tables display names and dates", () => {
  const [consent] = tableValues("advisor.consentRequests", "ConsentRequests/me/sent", [
    { studentFullName: "Student A", purposeName: "Advising", requestedAt: "2026-01-03T10:00:00Z" },
  ]);
  assert.equal(consent.studentName, "Student A");
  assert.equal(consent.consentPurposeName, "Advising");
  assert.equal(consent.requestedAt, "2026-01-03T10:00:00Z");

  for (const [path, nameKey, label] of [
    ["Referrals/me/incoming", "fromAccountFullName", "fromAccountName"],
    ["Referrals/me/outgoing", "toAccountFullName", "toAccountName"],
  ]) {
    const [referral] = tableValues("advisor.referrals", path, [
      { studentFullName: "Student B", [nameKey]: "Advisor A" },
    ]);
    assert.equal(referral.studentName, "Student B");
    assert.equal(referral[label], "Advisor A");
  }

  const [emergency] = tableValues("advisor.emergency", "EmergencyAccessRequests/me", [
    { studentFullName: "Student C" },
  ]);
  assert.equal(emergency.studentName, "Student C");

  const [history] = tableValues("advisor.caseHistory", "CaseStageHistories/search", [
    { changedByFullName: "Advisor B", changedAt: "2026-01-04T10:00:00Z" },
  ]);
  assert.equal(history.changedByName, "Advisor B");
  assert.equal(history.changedAt, "2026-01-04T10:00:00Z");
});

test("advisor data comparison displays nested values without revealing withheld data", () => {
  const rows = tableValues("advisor.compare", "StudentData/students/:studentId/compare", {
    dateA: "2026-01-01",
    dateB: "2026-02-01",
    values: [
      { attributeName: "Attendance", dateAValue: { value: "80" }, dateBValue: { value: "90" } },
      { attributeName: "Other", dateAValue: null, dateBValue: { value: null, isShared: false } },
    ],
  });
  assert.equal(rows[0].dataAttributeName, "Attendance");
  assert.equal(rows[0].valueA, "80");
  assert.equal(rows[0].valueB, "90");
  assert.equal(rows[1].valueA, undefined);
  assert.equal(rows[1].valueB, undefined);
});
