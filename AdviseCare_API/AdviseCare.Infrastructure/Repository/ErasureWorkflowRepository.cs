using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace AdviseCare.Infrastructure.Repository;

public sealed class ErasureWorkflowRepository : IErasureWorkflowRepository
{
    private readonly AdviseCareContext _context;

    public ErasureWorkflowRepository(AdviseCareContext context)
    {
        _context = context;
    }

    public async Task<(Guid AttributeKeyId, string KeyManagementKeyId)> ExecuteErasureAsync(
        Guid erasureRequestId,
        Guid executedBy,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_ExecuteErasure";
            command.CommandType = CommandType.StoredProcedure;
            AddGuidParameter(command, "@erasureRequestId", erasureRequestId);
            AddGuidParameter(command, "@executedBy", executedBy);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("The erasure procedure returned no key reference.");
            }

            var attributeKeyId = reader.GetGuid(reader.GetOrdinal("attributeKeyId"));
            var keyManagementKeyId = reader.GetString(reader.GetOrdinal("keyManagementKeyId"));

            if (string.IsNullOrWhiteSpace(keyManagementKeyId))
            {
                throw new InvalidOperationException("The erasure procedure returned an empty KMS key reference.");
            }

            return (attributeKeyId, keyManagementKeyId);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task ConfirmKmsKeyDestructionAsync(
        Guid erasureRequestId,
        string keyManagementKeyId,
        Guid executedBy,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_ConfirmKmsKeyDestruction";
            command.CommandType = CommandType.StoredProcedure;
            AddGuidParameter(command, "@erasureRequestId", erasureRequestId);
            AddStringParameter(command, "@keyManagementKeyId", keyManagementKeyId, 255);
            AddGuidParameter(command, "@executedBy", executedBy);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddGuidParameter(DbCommand command, string name, Guid value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Guid;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void AddStringParameter(DbCommand command, string name, string value, int size)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Size = size;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
