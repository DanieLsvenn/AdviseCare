using AdviseCare.Application.Contracts.Requests.StudentIndicator;
using AdviseCare.Application.Contracts.Responses.StudentIndicator;
using AdviseCare.Application.DTOs;
using AdviseCare.Application.Exceptions;
using AdviseCare.Application.Interfaces.Excel;
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Application.Interfaces.Security;
using AdviseCare.Application.Interfaces.Services;
using AdviseCare.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace AdviseCare.Application.Services
{
    public class StudentIndicatorImportService(
        IExcelReader excelReader,
        ICsvReader csvReader,
        IGenericRepository<Account> accountRepository,
        IGenericRepository<AccountRole> accountRoleRepository,
        IGenericRepository<Indicator> indicatorRepository,
        IGenericRepository<Academicterm> academicTermRepository,
        IGenericRepository<Studentprofile> studentProfileRepository,
        IGenericRepository<Attributekey> attributeKeyRepository,
        IGenericRepository<Studentindicator> studentIndicatorRepository,
        IAuthenticatedEnvelopeEncryptionService encryptionService,
        IKeyManagementService keyManagementService,
        IActivityLogService activityLogService,
        IHttpContextAccessor httpContextAccessor) : IStudentIndicatorImportService
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
        private readonly IGenericRepository<Studentprofile> _studentProfileRepository = studentProfileRepository;
        private readonly IGenericRepository<Attributekey> _attributeKeyRepository = attributeKeyRepository;
        private readonly IGenericRepository<Studentindicator> _studentIndicatorRepository = studentIndicatorRepository;
        private readonly IAuthenticatedEnvelopeEncryptionService _encryptionService = encryptionService;
        private readonly IKeyManagementService _keyManagementService = keyManagementService;
        private readonly IActivityLogService _activityLogService = activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
            var studentProfiles = await _studentProfileRepository.GetAllAsync();
            var accountsByIdentifier = BuildAccountLookup(accounts, studentProfiles);
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
                    else if (!string.IsNullOrWhiteSpace(value) && !IndicatorValueValidator.IsValid(indicator.DataType, value))
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

        public async Task<ImportStudentIndicatorResponse> ImportAsync(
            ValidateStudentIndicatorImportRequest request)
        {
            var validation = await ValidateAsync(request);
            if (!validation.IsValid)
            {
                return new ImportStudentIndicatorResponse
                {
                    Errors = validation.Errors
                };
            }

            var sourceFileName = Path.GetFileName(request.File!.FileName);
            if (string.IsNullOrWhiteSpace(sourceFileName))
            {
                throw new ValidationException("The uploaded file must have a file name.");
            }

            var rows = ReadRows(request.File);
            var columnMapping = ParseColumnMapping(request.ColumnMapping);
            var accounts = await _accountRepository.GetAllAsync();
            var accountRoles = await _accountRoleRepository.GetAllAsyncInclude(accountRole => accountRole.Role);
            var studentAccountIds = accountRoles
                .Where(accountRole => accountRole.Role.RoleName == "Student")
                .Select(accountRole => accountRole.AccountId)
                .ToHashSet();
            var studentProfiles = await _studentProfileRepository.GetAllAsync();
            var accountsByIdentifier = BuildAccountLookup(accounts, studentProfiles);
            var indicatorsByName = (await _indicatorRepository.GetAllAsync())
                .ToDictionary(indicator => indicator.IndicatorName, StringComparer.OrdinalIgnoreCase);
            var termsByName = (await _academicTermRepository.GetAllAsync())
                .GroupBy(term => term.TermName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var importedRows = new List<ResolvedImportRow>(rows.Count);
            foreach (var row in rows)
            {
                var identifier = GetValue(row, columnMapping.StudentIdentifier);
                var indicatorName = GetValue(row, columnMapping.Indicator);
                var termName = GetValue(row, columnMapping.Term);
                var validFromText = GetValue(row, columnMapping.ValidFrom);

                if (!accountsByIdentifier.TryGetValue(identifier, out var matchingAccounts) ||
                    matchingAccounts.Count != 1 ||
                    !studentAccountIds.Contains(matchingAccounts[0].AccountId) ||
                    !indicatorsByName.TryGetValue(indicatorName, out var indicator) ||
                    !termsByName.TryGetValue(termName, out var matchingTerms) ||
                    matchingTerms.Count != 1 ||
                    !DateTimeOffset.TryParse(
                        validFromText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var validFrom))
                {
                    throw new InvalidOperationException(
                        "Import reference data changed after validation. Validate the file again before importing.");
                }

                importedRows.Add(new ResolvedImportRow(
                    matchingAccounts[0],
                    indicator,
                    matchingTerms[0],
                    GetValue(row, columnMapping.Value),
                    validFrom.UtcDateTime));
            }

            var activeStudentAccounts = accounts
                .Where(account => account.Status == "active" && studentAccountIds.Contains(account.AccountId))
                .ToList();
            var studentIds = importedRows.Select(row => row.Student.AccountId).Distinct().ToHashSet();
            var dataAttributeIds = importedRows.Select(row => row.Indicator.DataAttributeId).Distinct().ToHashSet();
            var relevantKeys = await _attributeKeyRepository.SearchAsync(key =>
                studentIds.Contains(key.StudentId) &&
                dataAttributeIds.Contains(key.DataAttributeId) &&
                (key.Status == "active" || key.Status == "pendingKmsDestruction"));
            var activeKeysByPair = new Dictionary<(Guid StudentId, Guid DataAttributeId), Attributekey>();
            var pendingKeyPairs = new HashSet<(Guid StudentId, Guid DataAttributeId)>();

            foreach (var key in relevantKeys)
            {
                var pair = (key.StudentId, key.DataAttributeId);
                if (key.Status == "pendingKmsDestruction")
                {
                    pendingKeyPairs.Add(pair);
                }
                else if (!activeKeysByPair.TryAdd(pair, key))
                {
                    throw new InvalidOperationException(
                        "Multiple active encryption keys exist for the same student and data attribute.");
                }
            }

            if (importedRows.Any(row => pendingKeyPairs.Contains(
                    (row.Student.AccountId, row.Indicator.DataAttributeId))))
            {
                throw new ValidationException(
                    "Import is blocked for a student attribute while its erasure is awaiting Vault key destruction.");
            }

            var actorId = GetCurrentAccountId();
            var transactionAt = DateTime.UtcNow;
            var importedPairs = importedRows
                .Select(row => (row.Student.AccountId, row.Indicator.IndicatorId, row.Term.TermId))
                .ToHashSet();
            var expectedIndicatorPairs = importedRows
                .Select(row => (row.Indicator, row.Term))
                .DistinctBy(pair => (pair.Indicator.IndicatorId, pair.Term.TermId))
                .ToList();
            var createdVaultKeyIds = new List<string>();
            var missingCount = 0;

            try
            {
                foreach (var row in importedRows)
                {
                    var keyPair = (row.Student.AccountId, row.Indicator.DataAttributeId);
                    var studentIndicatorId = Guid.NewGuid();
                    EncryptedValue encryptedValue;

                    if (activeKeysByPair.TryGetValue(keyPair, out var attributeKey))
                    {
                        encryptedValue = await _encryptionService.EncryptAsync(
                            attributeKey,
                            studentIndicatorId,
                            "indicatorValue",
                            row.Value);
                    }
                    else
                    {
                        var provisionedValue = await _encryptionService.ProvisionAndEncryptAsync(
                            row.Student.AccountId,
                            row.Indicator.DataAttributeId,
                            studentIndicatorId,
                            "indicatorValue",
                            row.Value);
                        attributeKey = provisionedValue.AttributeKey;
                        createdVaultKeyIds.Add(attributeKey.KeyManagementKeyId);
                        activeKeysByPair.Add(keyPair, attributeKey);
                        _attributeKeyRepository.PrepareCreate(attributeKey);
                        encryptedValue = new EncryptedValue(
                            provisionedValue.Ciphertext,
                            provisionedValue.Hmac);
                    }

                    _studentIndicatorRepository.PrepareCreate(new Studentindicator
                    {
                        StudentIndicatorId = studentIndicatorId,
                        DataSourceId = null,
                        IndicatorId = row.Indicator.IndicatorId,
                        DataAttributeId = row.Indicator.DataAttributeId,
                        StudentId = row.Student.AccountId,
                        AttributeKeyId = attributeKey.AttributeKeyId,
                        TermId = row.Term.TermId,
                        IndicatorValue = encryptedValue.Ciphertext,
                        ValueHmac = encryptedValue.Hmac,
                        ProvenanceNote = $"Imported from {sourceFileName} on {transactionAt:yyyy-MM-dd}.",
                        TransactionFrom = transactionAt,
                        ValidFrom = row.ValidFrom,
                        IsMissing = false,
                        ErasedAt = null,
                        Status = "original"
                    });
                }

                var expectedFileName = sourceFileName;
                foreach (var student in activeStudentAccounts)
                {
                    foreach (var (indicator, term) in expectedIndicatorPairs)
                    {
                        if (importedPairs.Contains((student.AccountId, indicator.IndicatorId, term.TermId)))
                        {
                            continue;
                        }

                        _studentIndicatorRepository.PrepareCreate(new Studentindicator
                        {
                            StudentIndicatorId = Guid.NewGuid(),
                            DataSourceId = null,
                            IndicatorId = indicator.IndicatorId,
                            DataAttributeId = indicator.DataAttributeId,
                            StudentId = student.AccountId,
                            AttributeKeyId = null,
                            TermId = term.TermId,
                            IndicatorValue = null,
                            ValueHmac = null,
                            ProvenanceNote = $"Expected in {expectedFileName}; no value was supplied. Recorded {transactionAt:yyyy-MM-dd}.",
                            TransactionFrom = transactionAt,
                            ValidFrom = term.StartDate,
                            IsMissing = true,
                            ErasedAt = null,
                            Status = "original"
                        });
                        missingCount++;
                    }
                }

                var activityDetail =
                    $"Imported {importedRows.Count} indicator value(s) from {sourceFileName}; recorded {missingCount} missing indicator(s).";
                // LogAsync saves the staged profiles, keys, indicator rows, and this log through the shared scoped DbContext.
                await _activityLogService.LogAsync(actorId, "indicatorsImported", activityDetail);

                return new ImportStudentIndicatorResponse
                {
                    IsSuccess = true,
                    InsertedCount = importedRows.Count,
                    MissingCount = missingCount
                };
            }
            catch (Exception importError)
            {
                var cleanupErrors = new List<Exception>();
                foreach (var keyName in createdVaultKeyIds.Distinct(StringComparer.Ordinal))
                {
                    try
                    {
                        await _keyManagementService.DestroyKeyAsync(keyName, CancellationToken.None);
                    }
                    catch (Exception cleanupError)
                    {
                        cleanupErrors.Add(cleanupError);
                    }
                }

                if (cleanupErrors.Count > 0)
                {
                    cleanupErrors.Insert(0, importError);
                    throw new AggregateException(
                        "Indicator import failed and one or more newly provisioned Vault keys could not be cleaned up.",
                        cleanupErrors);
                }

                throw;
            }
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

        private Guid GetCurrentAccountId()
        {
            var accountIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Sid);
            if (!Guid.TryParse(accountIdClaim, out var accountId))
            {
                throw new InvalidOperationException(
                    "The authenticated account identifier is required to record an indicator import.");
            }

            return accountId;
        }

        private static Dictionary<string, List<Account>> BuildAccountLookup(
            IEnumerable<Account> accounts,
            IEnumerable<Studentprofile> studentProfiles)
        {
            var lookup = new Dictionary<string, List<Account>>(StringComparer.OrdinalIgnoreCase);
            var accountsById = accounts.ToDictionary(account => account.AccountId);
            foreach (var account in accounts)
            {
                AddAccountIdentifier(lookup, account.Email, account);
                AddAccountIdentifier(lookup, account.UserName, account);
            }

            foreach (var profile in studentProfiles)
            {
                if (accountsById.TryGetValue(profile.StudentId, out var account))
                {
                    AddAccountIdentifier(lookup, profile.StudentCode, account);
                }
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

        private sealed record ColumnMapping(
            string? StudentIdentifier,
            string? Indicator,
            string? Value,
            string? Term,
            string? ValidFrom);

        private sealed record ResolvedImportRow(
            Account Student,
            Indicator Indicator,
            Academicterm Term,
            string Value,
            DateTime ValidFrom);
    }
}
