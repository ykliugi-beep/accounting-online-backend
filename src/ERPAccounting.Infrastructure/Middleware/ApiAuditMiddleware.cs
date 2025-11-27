using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ERPAccounting.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware za automatsko logovanje svih API poziva sa JSON snapshot podrškom.
    /// Hvata request/response i čuva u tblAPIAuditLog tabelu.
    /// 
    /// NOVI PRISTUP SA HttpContext.Items:
    /// - Koristi HttpContext.Items za deljenje audit log ID-a
    /// - Svi DbContext instance-i mogu da pročitaju audit log ID iz HttpContext-a
    /// - Rešava problem različitih DI scope-ova
    /// - Hvata RequestBody i ResponseBody za SVE HTTP metode
    /// </summary>
    public class ApiAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiAuditMiddleware> _logger;
        
        // Ključ za čuvanje audit log ID-a u HttpContext.Items
        public const string AuditLogIdKey = "__AuditLogId__";

        public ApiAuditMiddleware(RequestDelegate next, ILogger<ApiAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAuditLogService auditLogService,
            ICurrentUserService currentUserService)
        {
            var request = context.Request;
            var originalBodyStream = context.Response.Body;

            // 1. Pripremi audit log pre izvršavanja requesta
            var auditLog = new ApiAuditLog
            {
                Timestamp = DateTime.UtcNow,
                HttpMethod = request.Method,
                Endpoint = request.Path.Value ?? string.Empty,
                RequestPath = request.Path.Value ?? string.Empty,
                QueryString = request.QueryString.HasValue ? request.QueryString.Value : null,
                IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = request.Headers["User-Agent"].ToString(),
                UserId = currentUserService.UserId,
                Username = currentUserService.Username,
                CorrelationId = Guid.NewGuid()
            };

            // 2. ISPRAVKA: Pročitaj request body za SVE metode koje imaju content
            if (request.ContentLength > 0 && request.Body.CanRead)
            {
                try
                {
                    request.EnableBuffering();

                    if (request.Body.CanSeek)
                    {
                        request.Body.Seek(0, SeekOrigin.Begin);
                    }

                    using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
                    {
                        auditLog.RequestBody = await reader.ReadToEndAsync();
                    }

                    if (request.Body.CanSeek)
                    {
                        request.Body.Seek(0, SeekOrigin.Begin);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read request body");
                }
            }

            // 3. Privremeni stream za response
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            var stopwatch = Stopwatch.StartNew();
            int auditLogId = 0;

            try
            {
                // 4. Kreiraj audit log I DOBIJ ID
                await auditLogService.LogAsync(auditLog);
                auditLogId = auditLog.IDAuditLog;

                // 5. KRITIČNO: Postavi audit log ID u HttpContext.Items
                // Ovaj ID će biti dostupan svim DbContext instance-ima kroz HttpContextAccessor
                if (auditLogId > 0)
                {
                    context.Items[AuditLogIdKey] = auditLogId;
                    
                    _logger.LogDebug(
                        "Audit log ID {AuditLogId} set in HttpContext for {Method} {Endpoint}",
                        auditLogId,
                        request.Method,
                        request.Path);
                }

                // 6. Izvrši request pipeline (ovde se događa SaveChangesAsync sa audit tracking-om)
                await _next(context);

                stopwatch.Stop();

                // 7. Nakon uspešnog izvršavanja
                auditLog.ResponseStatusCode = context.Response.StatusCode;
                auditLog.IsSuccess = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400;
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

                // 8. ISPRAVKA: Hvata response body ZA SVE metode
                if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true))
                    {
                        auditLog.ResponseBody = await reader.ReadToEndAsync();
                    }
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                }

                // 9. Ažuriraj audit log sa response podacima
                await auditLogService.UpdateAsync(auditLog);

                // 10. Vrati response u originalni stream
                if (responseBodyStream.Length > 0)
                {
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // 11. U slučaju exceptiona, upiši detalje
                auditLog.ResponseStatusCode = context.Response.StatusCode > 0 
                    ? context.Response.StatusCode 
                    : StatusCodes.Status500InternalServerError;
                auditLog.IsSuccess = false;
                auditLog.ErrorMessage = ex.Message;
                auditLog.ExceptionDetails = ex.ToString();
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

                // Pokuša da pročita response body i za error
                if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true))
                    {
                        auditLog.ResponseBody = await reader.ReadToEndAsync();
                    }
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                }

                try
                {
                    await auditLogService.UpdateAsync(auditLog);
                }
                catch (Exception auditEx)
                {
                    // Ne dozvoljavamo da audit failure crashuje aplikaciju
                    _logger.LogError(auditEx, "Failed to update audit entry for failed request");
                }

                // 12. Vrati response body u originalni stream ako ima nečega
                if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                }

                // Re-throw originalni exception
                throw;
            }
            finally
            {
                // 13. Uvek vrati originalni stream i očisti privremeni
                context.Response.Body = originalBodyStream;
                await responseBodyStream.DisposeAsync();
            }
        }
    }
}