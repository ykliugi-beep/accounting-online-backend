using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ERPAccounting.Common.Interfaces;
using ERPAccounting.Domain.Entities;
using ERPAccounting.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace ERPAccounting.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware za automatsko logovanje svih API poziva.
    /// Hvataj request/response i čuva u tblAPIAuditLog tabelu.
    /// Setuje audit log ID na AppDbContext za entity-level tracking.
    /// </summary>
    public class ApiAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IAuditLogService auditLogService,
            ICurrentUserService currentUserService,
            AppDbContext dbContext)
        {
            // Kreiraj audit log objekat
            var auditLog = new ApiAuditLog
            {
                Timestamp = DateTime.UtcNow,
                HttpMethod = context.Request.Method,
                Endpoint = context.Request.Path,
                RequestPath = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Username = currentUserService.Username,
                UserId = currentUserService.UserId,
                CorrelationId = Guid.NewGuid()
            };

            // Capture request body za POST/PUT/PATCH operacije
            if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
            {
                context.Request.EnableBuffering();
                
                using (var reader = new StreamReader(
                    context.Request.Body, 
                    Encoding.UTF8, 
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 4096,
                    leaveOpen: true))
                {
                    auditLog.RequestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0; // Reset stream za dalje procesiranje
                }
            }

            // Measure response time
            var stopwatch = Stopwatch.StartNew();
            
            // Capture response body
            var originalBodyStream = context.Response.Body;
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                // BITNO: Loguj audit log PRE pozivanja next middleware-a
                // da bi imali IDAuditLog za entity tracking
                await auditLogService.LogAsync(auditLog);

                // Setuj audit log ID na DbContext - entity changes će se vezati za ovaj request
                dbContext.SetCurrentAuditLogId(auditLog.IDAuditLog);

                // Pozovi sledeći middleware u pipeline
                await _next(context);
                
                stopwatch.Stop();
                
                // Populate response info
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                auditLog.ResponseStatusCode = context.Response.StatusCode;
                auditLog.IsSuccess = context.Response.StatusCode < 400;

                // Capture response body (opciono - samo za error responses)
                responseBody.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(responseBody, leaveOpen: true))
                {
                    // Loguj samo za error responses (IsSuccess == false)
                    if (auditLog.IsSuccess == false)
                    {
                        auditLog.ResponseBody = await reader.ReadToEndAsync();
                    }
                }
                
                // Copy response back to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);

                // Ažuriraj audit log sa response podacima
                await auditLogService.UpdateAsync(auditLog);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log exception info
                auditLog.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                auditLog.IsSuccess = false;
                auditLog.ErrorMessage = ex.Message;
                auditLog.ExceptionDetails = ex.ToString();
                auditLog.ResponseStatusCode = 500;

                // Ažuriraj audit log sa exception podacima
                try
                {
                    await auditLogService.UpdateAsync(auditLog);
                }
                catch
                {
                    // Ignore audit update failure
                }
                
                // Re-throw exception - audit ne sme da proguta greške
                throw;
            }
            finally
            {
                // Vrati originalni stream i dispose responseBody
                context.Response.Body = originalBodyStream;
                await responseBody.DisposeAsync();
            }
        }
    }
}
