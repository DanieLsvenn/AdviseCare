using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AdviseCare.API.Errors
{
    // Turns a database error that slipped past the services' own checks into a status code and a message a person can act on.
    // The services check duplicates first; this is the safety net for races (two admins saving the same code at once) and for
    // constraints a service does not check itself. Only a real optimistic-concurrency failure says "someone changed this record".
    public static class DatabaseErrorTranslator
    {
        // Friendly wording per unique constraint / unique index of AdviseCare_Database_Script.sql; {0} is the duplicate value
        private static readonly Dictionary<string, string> UniqueMessages = new(StringComparer.OrdinalIgnoreCase)
        {
            ["UQ_FACULTIES_facultyCode"] = "A faculty with code '{0}' already exists.",
            ["UQ_ACCOUNTS_email"] = "An account with email '{0}' already exists.",
            ["UQ_ACCOUNTS_userName"] = "An account with username '{0}' already exists.",
            ["UQ_STUDENTPROFILES_studentId"] = "This account already has a student profile.",
            ["UQ_STUDENTPROFILES_studentCode"] = "A student with student code '{0}' already exists.",
            ["UQ_ROLES_roleName"] = "A role named '{0}' already exists.",
            ["UQ_PROGRAMMES_programmeCode"] = "A programme with code '{0}' already exists.",
            ["UQ_DATAATTRIBUTES_attributeName"] = "A data attribute named '{0}' already exists.",
            ["UQ_SUBJECTS_subjectCode"] = "A subject with code '{0}' already exists.",
            ["UQ_DATASOURCES_dataAttributeId"] = "This data attribute already has a data source.",
            ["UQ_CONSENTPURPOSES_purposeName"] = "A consent purpose named '{0}' already exists.",
            ["UX_ACCOUNTS_googleId"] = "This Google account is already linked to another AdviseCare account.",
            ["UX_ACADEMICTERMS_status_current"] = "Only one academic term can be current. Set the current term to past or upcoming first.",
            ["UX_ATTRIBUTEKEYS_student_attribute_active"] = "This student already has an active key for this data attribute.",
            ["UX_CONSENTS_student_purpose_attribute_granted"] = "The student already shares this data attribute under this purpose.",
            ["UX_ACCESSPOLICIES_role_purpose_live"] = "This role already has a draft or active policy under this purpose.",
        };

        public static (int Status, string Message)? Translate(Exception exception)
        {
            if (exception is DbUpdateConcurrencyException)
                return (StatusCodes.Status409Conflict, "Someone changed this record while you were working. Reload it to see the latest version, then try again.");
            if (exception is not DbUpdateException update)
                return null;

            var sql = FindSqlException(update);
            if (sql == null)
                return (StatusCodes.Status409Conflict, "The change could not be saved because it conflicts with data already stored.");

            return sql.Number switch
            {
                2601 or 2627 => (StatusCodes.Status409Conflict, DuplicateMessage(sql.Message)),
                547 => ReferenceMessage(sql.Message),
                2628 or 8152 => (StatusCodes.Status400BadRequest, "A value is longer than the field allows. Shorten it and try again."),
                515 => (StatusCodes.Status400BadRequest, "A required value is missing."),
                _ => null
            };
        }

        private static SqlException? FindSqlException(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
                if (current is SqlException sql) return sql;
            return null;
        }

        // "Violation of UNIQUE KEY constraint 'UQ_X'. ... The duplicate key value is (abc)." or
        // "Cannot insert duplicate key row in object 'dbo.X' with unique index 'UX_Y'. The duplicate key value is (abc)."
        private static string DuplicateMessage(string sqlMessage)
        {
            var name = Regex.Match(sqlMessage, @"(?:constraint|unique index) '([^']+)'").Groups[1].Value;
            var value = Regex.Match(sqlMessage, @"duplicate key value is \((.*)\)\.?").Groups[1].Value;

            if (UniqueMessages.TryGetValue(name, out var template))
                return string.Format(template, value);
            if (name.StartsWith("PK_", StringComparison.OrdinalIgnoreCase))
                return "This record already exists.";
            return string.IsNullOrEmpty(value)
                ? "A record with the same value already exists."
                : $"A record with the value '{value}' already exists.";
        }

        // 547 covers foreign keys (a missing parent, or a parent still in use) and CHECK constraints (a value not allowed)
        private static (int Status, string Message) ReferenceMessage(string sqlMessage)
        {
            if (sqlMessage.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase))
            {
                var name = Regex.Match(sqlMessage, @"CHECK constraint ""?'?([^""']+)").Groups[1].Value;
                var column = Regex.Match(sqlMessage, @"column '([^']+)'").Groups[1].Value;
                return (StatusCodes.Status400BadRequest, string.IsNullOrEmpty(column)
                    ? $"A value is not allowed ({name})."
                    : $"The value of '{column}' is not allowed.");
            }
            if (sqlMessage.Contains("DELETE statement", StringComparison.OrdinalIgnoreCase))
                return (StatusCodes.Status409Conflict, "This record is still used by other records, so it cannot be removed. Archive it instead.");

            var table = Regex.Match(sqlMessage, @"table ""dbo\.([^""]+)""").Groups[1].Value;
            return (StatusCodes.Status400BadRequest, string.IsNullOrEmpty(table)
                ? "The record refers to something that does not exist."
                : $"The record refers to a {table.ToLowerInvariant().TrimEnd('s')} that does not exist.");
        }
    }
}
