using AdviseCare.Application.Contracts.Requests.StudentIndicator;
using AdviseCare.Application.Contracts.Responses.StudentIndicator;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text.Json;

namespace AdviseCare.Application.Services
{
    public class StudentIndicatorImportService(
        IExcelReader excelReader,
        ICsvReader csvReader,
        IGenericRepository<Account> accountRepository,
        IGenericRepository<AccountRole> accountRoleRepository,
        IGenericRepository<Indicator> indicatorRepository,
        IGenericRepository<Academicterm> academicTermRepository) : IStudentIndicatorImportService
    {
        private static readonly JsonSerializerOptions MappingJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IExcelReader _excelReader = excelReader;
        private readonly ICsvReader _csvReader = csvReader;
        private readonly IGenericRepository<Account> _accountRepository = accountRepository;
        private readonly IGenericRepository<AccountRole> _accountRoleRepository = accountRoleRepository;
        private readonly IGenericRepository<Indicator> _indicatorRepository = indicatorRepository;
        private readonly IGenericRepository<Academicterm> _academicTermRepository = academicTermRepository;

        public Task<PreviewStudentIndicatorImportResponse> PreviewAsync(PreviewStudentIndicatorImportRequest request)
        {
            var rows = ReadRows(request.File);
            if (rows.Count == 0)
                throw new ValidationException("The file has no data rows under the header row.");

            return Task.FromResult(new PreviewStudentIndicatorImportResponse
            {
                Headers = rows[0].Values.Keys.ToList(),
                SampleRows = rows.Take(5).Select(row => new StudentIndicatorImportPreviewRow
                {
                    RowNumber = row.RowNumber,
                    Values = row.Values
                }).ToList()
            });
        }

        public async Task<ValidateStudentIndicatorImportResponse> ValidateAsync(
            ValidateStudentIndicatorImportRequest request)
        {
            var rows = ReadRows(request.File);
            var columnMapping = ParseColumnMapping(request.ColumnMapping);
            var validationErrors = new List<StudentIndicatorImportRowError>();

            if (rows.Count == 0)
            {
                validationErrors.Add(new StudentIndicatorImportRowError
                {
                    RowNumber = 1,
                    Messages = { "The file has no data rows under the header row." }
                });
                return new ValidateStudentIndicatorImportResponse { Errors = validationErrors };
            }

            var mappedHeaders = GetMappedHeaders(columnMapping);
            if (mappedHeaders.Any(string.IsNullOrWhiteSpace))
            {
                validationErrors.Add(new StudentIndicatorImportRowError
                {
                    RowNumber = 1,
                    Messages = { "Every required field must be mapped to a column." }
                });
                return new ValidateStudentIndicatorImportResponse { Errors = validationErrors };
            }

            if (mappedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).Count() != mappedHeaders.Length)
            {
                validationErrors.Add(new StudentIndicatorImportRowError
                {
                    RowNumber = 1,
                    Messages = { "Each required field must map to a different column." }
                });
                return new ValidateStudentIndicatorImportResponse { Errors = validationErrors };
            }

            var availableHeaders = rows[0].Values.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingHeaders = mappedHeaders.Where(header => !availableHeaders.Contains(header)).ToList();
            if (missingHeaders.Count > 0)
            {
                validationErrors.Add(new StudentIndicatorImportRowError
                {
                    RowNumber = 1,
                    Messages = { $"Mapped column(s) not found in the file: {string.Join(", ", missingHeaders)}." }
                });
                return new ValidateStudentIndicatorImportResponse { Errors = validationErrors };
            }

            var accounts = await _accountRepository.GetAllAsync();
            var accountRoles = await _accountRoleRepository.GetAllAsyncInclude(accountRole => accountRole.Role);
            var studentAccountIds = accountRoles
                .Where(accountRole => accountRole.Role.RoleName == "Student")
                .Select(accountRole => accountRole.AccountId)
                .ToHashSet();
            var accountsByIdentifier = BuildAccountLookup(accounts);
            var indicators = await _indicatorRepository.GetAllAsync();
            var indicatorsByName = indicators.ToDictionary(
                indicator => indicator.IndicatorName,
                StringComparer.OrdinalIgnoreCase);
            var terms = await _academicTermRepository.GetAllAsync();
            var termsByName = terms
                .GroupBy(term => term.TermName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var messages = new List<string>();
                var identifier = GetValue(row, columnMapping.StudentIdentifier);
                var indicatorName = GetValue(row, columnMapping.Indicator);
                var value = GetValue(row, columnMapping.Value);
                var termName = GetValue(row, columnMapping.Term);
                var validFromText = GetValue(row, columnMapping.ValidFrom);

                if (string.IsNullOrWhiteSpace(identifier))
                    messages.Add("Student identifier is required.");
                if (string.IsNullOrWhiteSpace(indicatorName))
                    messages.Add("Indicator is required.");
                if (string.IsNullOrWhiteSpace(value))
                    messages.Add("Indicator value is required.");
                if (string.IsNullOrWhiteSpace(termName))
                    messages.Add("Term is required.");
                if (string.IsNullOrWhiteSpace(validFromText))
                    messages.Add("Valid-from date is required.");

                if (!string.IsNullOrWhiteSpace(identifier))
                {
                    if (!accountsByIdentifier.TryGetValue(identifier, out var matchingAccounts))
                        messages.Add($"Student '{identifier}' does not exist.");
                    else if (matchingAccounts.Count != 1)
                        messages.Add($"Student identifier '{identifier}' is ambiguous.");
                    else if (!studentAccountIds.Contains(matchingAccounts[0].AccountId))
                        messages.Add($"Account '{identifier}' does not have the Student role.");
                }

                if (!string.IsNullOrWhiteSpace(indicatorName))
                {
                    if (!indicatorsByName.TryGetValue(indicatorName, out var indicator))
                    {
                        messages.Add($"Indicator '{indicatorName}' does not exist.");
                    }
                    else if (!string.IsNullOrWhiteSpace(value) && !IsValidValue(indicator.DataType, value))
                    {
                        messages.Add($"Value '{value}' is not valid for {indicator.DataType} indicator '{indicatorName}'.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(termName))
                {
                    if (!termsByName.TryGetValue(termName, out var matchingTerms))
                        messages.Add($"Term '{termName}' does not exist.");
                    else if (matchingTerms.Count != 1)
                        messages.Add($"Term name '{termName}' is ambiguous.");
                }

                if (!string.IsNullOrWhiteSpace(validFromText) &&
                    !DateTimeOffset.TryParse(
                        validFromText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out _))
                {
                    messages.Add($"Valid-from date '{validFromText}' is not a valid date.");
                }

                if (messages.Count > 0)
                {
                    validationErrors.Add(new StudentIndicatorImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Messages = messages
                    });
                }
            }

            return new ValidateStudentIndicatorImportResponse
            {
                IsValid = validationErrors.Count == 0,
                ValidRowCount = validationErrors.Count == 0 ? rows.Count : 0,
                Errors = validationErrors
            };
        }

        private List<ExcelRow> ReadRows(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                throw new ValidationException("A non-empty .xlsx or .csv file is required.");

            var extension = Path.GetExtension(file.FileName);
            if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Only .xlsx and .csv files can be imported.");
            }

            try
            {
                using var stream = file.OpenReadStream();
                return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                    ? _csvReader.ReadRows(stream)
                    : _excelReader.ReadRows(stream);
            }
            catch (InvalidDataException exception)
            {
                throw new ValidationException(exception.Message);
            }
            catch (Exception exception) when (
                exception is IOException or FormatException or ArgumentException or InvalidOperationException)
            {
                throw new ValidationException($"The file could not be read as {extension} data.");
            }
        }

        private static ColumnMapping ParseColumnMapping(string columnMappingJson)
        {
            try
            {
                return JsonSerializer.Deserialize<ColumnMapping>(columnMappingJson, MappingJsonOptions)
                    ?? throw new ValidationException("Column mapping must be a JSON object.");
            }
            catch (JsonException)
            {
                throw new ValidationException("Column mapping must be valid JSON.");
            }
        }

        private static string[] GetMappedHeaders(ColumnMapping mapping) =>
        [
            mapping.StudentIdentifier?.Trim() ?? string.Empty,
            mapping.Indicator?.Trim() ?? string.Empty,
            mapping.Value?.Trim() ?? string.Empty,
            mapping.Term?.Trim() ?? string.Empty,
            mapping.ValidFrom?.Trim() ?? string.Empty
        ];

        private static string GetValue(ExcelRow row, string? header) =>
            !string.IsNullOrWhiteSpace(header) && row.Values.TryGetValue(header.Trim(), out var value)
                ? value.Trim()
                : string.Empty;

        private static Dictionary<string, List<Account>> BuildAccountLookup(IEnumerable<Account> accounts)
        {
            var lookup = new Dictionary<string, List<Account>>(StringComparer.OrdinalIgnoreCase);
            foreach (var account in accounts)
            {
                AddAccountIdentifier(lookup, account.Email, account);
                AddAccountIdentifier(lookup, account.UserName, account);
            }

            return lookup;
        }

        private static void AddAccountIdentifier(
            Dictionary<string, List<Account>> lookup,
            string? identifier,
            Account account)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return;

            if (!lookup.TryGetValue(identifier, out var matches))
            {
                matches = new List<Account>();
                lookup.Add(identifier, matches);
            }

            if (matches.All(match => match.AccountId != account.AccountId))
                matches.Add(account);
        }

        private static bool IsValidValue(string dataType, string value) => dataType switch
        {
            "numeric" => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            "boolean" => bool.TryParse(value, out _),
            "categorical" => !string.IsNullOrWhiteSpace(value),
            _ => false
        };

        private sealed record ColumnMapping(
            string? StudentIdentifier,
            string? Indicator,
            string? Value,
            string? Term,
            string? ValidFrom);
    }
}
