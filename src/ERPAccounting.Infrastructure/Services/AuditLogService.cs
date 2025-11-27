using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ERPAccounting.Infrastructure.Services
{
    /// <summary>
    /// Implementacija servisa za logovanje API poziva.
    /// Koristi AppDbContext za perzistenciju u bazu.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            IDbContextFactory<AppDbContext> contextFactory,
            ILogger<AuditLogService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Asinkrono loguje API poziv u bazu (initial request logging).
        /// Ne baca exception ako logovanje faila - samo loguje error.
        /// </summary>
        public async Task LogAsync(ApiAuditLog auditLog)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                context.ApiAuditLogs.Add(auditLog);
                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "API call logged: {Method} {Endpoint} - Started",
                    auditLog.HttpMethod,
                    auditLog.Endpoint);
            }
            catch (Exception ex)
            {
                // KRITIČNO: Ne bacaj exception - audit failure ne sme da prekine request
                _logger.LogError(ex,
                    "Failed to log API audit entry for {Method} {Endpoint}",
                    auditLog.HttpMethod,
                    auditLog.Endpoint);
            }
        }

        /// <summary>
        /// Ažurira postojeći audit log sa response podacima.
        /// </summary>
        public async Task UpdateAsync(ApiAuditLog auditLog)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Nađi postojeći audit log
                var existing = await context.ApiAuditLogs
                    .FirstOrDefaultAsync(a => a.IDAuditLog == auditLog.IDAuditLog);

                if (existing == null)
                {
                    _logger.LogWarning(
                        "Audit log {AuditLogId} not found for update",
                        auditLog.IDAuditLog);
                    return;
                }

                // Ažuriraj response podatke
                existing.ResponseStatusCode = auditLog.ResponseStatusCode;
                existing.ResponseBody = auditLog.ResponseBody;
                existing.ResponseTimeMs = auditLog.ResponseTimeMs;
                existing.IsSuccess = auditLog.IsSuccess;
                existing.ErrorMessage = auditLog.ErrorMessage;
                existing.ExceptionDetails = auditLog.ExceptionDetails;

                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "API call updated: {Method} {Endpoint} - {StatusCode} ({ResponseTime}ms)",
                    auditLog.HttpMethod,
                    auditLog.Endpoint,
                    auditLog.ResponseStatusCode,
                    auditLog.ResponseTimeMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update API audit entry {AuditLogId}",
                    auditLog.IDAuditLog);
            }
        }

        /// <summary>
        /// Loguje field-level izmene entiteta.
        /// </summary>
        public async Task LogEntityChangeAsync(
            int auditLogId,
            string entityType,
            string entityId,
            string operationType,
            Dictionary<string, (object OldValue, object NewValue)> changes)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                if (changes == null || changes.Count == 0)
                {
                    _logger.LogWarning(
                        "No changes to log for {EntityType} {EntityId}",
                        entityType,
                        entityId);
                    return;
                }

                foreach (var change in changes)
                {
                    var entityChange = new ApiAuditLogEntityChange
                    {
                        IDAuditLog = auditLogId,
                        PropertyName = change.Key,
                        OldValue = SerializeValue(change.Value.OldValue),
                        NewValue = SerializeValue(change.Value.NewValue),
                        DataType = change.Value.NewValue?.GetType().Name ?? "null"
                    };

                    context.ApiAuditLogEntityChanges.Add(entityChange);
                }

                await context.SaveChangesAsync(default);

                _logger.LogDebug(
                    "Logged {Count} field changes for {EntityType} {EntityId}",
                    changes.Count,
                    entityType,
                    entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to log entity changes for {EntityType} {EntityId}",
                    entityType,
                    entityId);
            }
        }

        /// <summary>
        /// Helper metoda za serijalizaciju vrednosti u string format.
        /// Rukuje null, complex types, i edge cases.
        /// </summary>
        public static string? SerializeValue(object? value)
        {
            if (value is null)
                return null;

            // Za jednostavne tipove - direktna konverzija
            if (value is string str)
                return str;

            if (value.GetType().IsPrimitive || value is DateTime || value is decimal)
                return value.ToString();

            // Za complex tipove - JSON serijalizacija
            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch
            {
                return value.ToString();
            }
        }
    }
}