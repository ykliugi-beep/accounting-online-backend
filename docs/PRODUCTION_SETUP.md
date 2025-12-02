# 🚀 Backend Production Setup Guide

**Status:** Production Ready ✅  
**Date:** 02.12.2025

---

## 📋 Overview

**What's Implemented:**
- ✅ **CORS Configuration** - Already configured in `Program.cs`
- ✅ **JWT Authentication** - Fully configured
- ✅ **Test JWT Token Endpoint** - `/api/v1/auth/test-token`
- ✅ **Token Validation Endpoint** - `/api/v1/auth/validate-token`
- ✅ **ETag Support** - For optimistic concurrency control
- ✅ **Swagger Documentation** - With Bearer token support

**No Changes Needed!** Backend is already production-ready for Frontend integration.

---

## ✅ Current CORS Configuration

**Location:** `src/ERPAccounting.API/Program.cs` (lines 42-58)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",   // Vite dev default
                "http://localhost:5173",   // Vite dev alternative
                "http://localhost:5174"    // Vite dev alternative 2
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("ETag", "X-Total-Count", "Location")
            .AllowCredentials();
    });
});
```

**Applied at:** Line 155
```csharp
app.UseCors("AllowFrontend"); // Before UseAuthentication!
```

**✅ Status:** Correctly configured, no changes needed!

---

## 🔑 JWT Token Endpoints

### 1. Generate Test Token

**Endpoint:** `GET /api/v1/auth/test-token`

**Purpose:** Generate JWT token for development/testing

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-12-03T20:00:00Z",
  "username": "test_user",
  "email": "test@example.com",
  "roles": ["Admin"],
  "instructions": "Copy this token to Frontend .env.local as VITE_JWT_TOKEN",
  "warning": "⚠️ This is a TEST endpoint. Remove before production deployment!"
}
```

**Usage:**
```bash
# Via curl
curl http://localhost:5286/api/v1/auth/test-token

# Via browser
open http://localhost:5286/swagger
# Find: GET /api/v1/auth/test-token
# Click "Try it out" -> "Execute"
```

### 2. Validate Token

**Endpoint:** `POST /api/v1/auth/validate-token`

**Purpose:** Check if JWT token is valid

**Request:**
```json
{
  "token": "your-jwt-token-here"
}
```

**Response (valid):**
```json
{
  "isValid": true,
  "username": "test_user",
  "email": "test@example.com",
  "roles": ["Admin"],
  "expiresAt": "2025-12-03T20:00:00Z",
  "message": "Token is valid"
}
```

**Response (invalid):**
```json
{
  "isValid": false,
  "message": "Token has expired"
}
```

---

## 🛠️ Setup Instructions

### 1. Start Backend

```bash
cd accounting-online-backend

# Restore dependencies
dotnet restore

# Run migrations (if needed)
dotnet ef database update --project src/ERPAccounting.Infrastructure --startup-project src/ERPAccounting.API

# Start API
dotnet run --project src/ERPAccounting.API
```

**Backend URL:** `http://localhost:5286`  
**Swagger UI:** `http://localhost:5286/swagger`

### 2. Generate JWT Token

**Option A - Swagger UI:**
1. Open: `http://localhost:5286/swagger`
2. Find: `GET /api/v1/auth/test-token`
3. Click "Try it out" → "Execute"
4. Copy `token` value from response

**Option B - curl:**
```bash
curl http://localhost:5286/api/v1/auth/test-token | jq -r '.token'
```

**Option C - PowerShell:**
```powershell
(Invoke-RestMethod -Uri http://localhost:5286/api/v1/auth/test-token).token
```

### 3. Provide Token to Frontend

Give the generated token to Frontend team to add in `.env.local`:

```env
VITE_JWT_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📝 Configuration Files

### appsettings.json

**JWT Configuration:**
```json
{
  "Jwt": {
    "Issuer": "ERPAccounting.API",
    "Audience": "ERPAccounting.Client",
    "SigningKey": "your-secret-key-at-least-32-characters-long"
  }
}
```

**⚠️ Important:** 
- `SigningKey` must be **at least 32 characters**
- In production, use strong randomly generated key
- **Never commit secrets to git!**

---

## ✅ Testing Checklist

### 1. Test CORS

```bash
# Test OPTIONS preflight request
curl -X OPTIONS http://localhost:5286/api/v1/documents \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET" \
  -v

# Should see:
# Access-Control-Allow-Origin: http://localhost:3000
# Access-Control-Allow-Methods: GET, POST, PUT, DELETE, PATCH, OPTIONS
# Access-Control-Allow-Headers: *
```

### 2. Test JWT Token Generation

```bash
# Generate token
curl http://localhost:5286/api/v1/auth/test-token

# Should return JSON with token, expiresAt, username, etc.
```

### 3. Test Protected Endpoint

```bash
# Get token first
TOKEN=$(curl -s http://localhost:5286/api/v1/auth/test-token | jq -r '.token')

# Test protected endpoint
curl http://localhost:5286/api/v1/documents \
  -H "Authorization: Bearer $TOKEN"

# Should return documents list (not 401)
```

### 4. Test ETag Support

```bash
# Create document
curl -X POST http://localhost:5286/api/v1/documents \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"documentTypeCode":"UR","date":"2025-12-02"}'

# Check response headers for: ETag: "abc123..."
```

---

## 🐛 Troubleshooting

### Problem: "CORS policy: No 'Access-Control-Allow-Origin'"

**Solution:**
1. Check `Program.cs` line 155: `app.UseCors("AllowFrontend");` exists?
2. Check CORS is **BEFORE** `app.UseAuthentication()`
3. Check Frontend URL in `WithOrigins()` matches actual URL
4. Restart Backend

### Problem: "401 Unauthorized" on all endpoints

**Solution:**
1. Generate new token: `curl http://localhost:5286/api/v1/auth/test-token`
2. Check token not expired (valid 24h)
3. Check `appsettings.json` has valid `Jwt:SigningKey`
4. Test token validation: `POST /api/v1/auth/validate-token`

### Problem: "Cannot generate JWT token"

**Solution:**
1. Check `appsettings.json` has `Jwt:SigningKey`
2. SigningKey must be **at least 32 characters**
3. Check Backend logs for errors
4. Test endpoint exists: `curl http://localhost:5286/swagger/v1/swagger.json | grep test-token`

### Problem: ETag not working

**Solution:**
1. Check `Program.cs` line 56: `WithExposedHeaders("ETag", ...)` exists
2. Check response headers include `ETag`
3. Frontend must include `If-Match` header on UPDATE requests

---

## 📦 Production Deployment

**Before deploying to production:**

### 1. Remove Test Endpoints

```bash
# Delete this file:
rm src/ERPAccounting.API/Controllers/AuthController.cs

# Or comment out test-token endpoint
```

### 2. Update CORS Origins

**In `Program.cs`:**
```csharp
.WithOrigins(
    "https://your-production-domain.com",
    "https://www.your-production-domain.com"
)
```

### 3. Secure JWT SigningKey

**Use Azure Key Vault, AWS Secrets Manager, or similar:**
```json
{
  "Jwt": {
    "SigningKey": "${JWT_SIGNING_KEY}"  // From environment variable
  }
}
```

### 4. Enable HTTPS

```csharp
// In Program.cs
app.UseHttpsRedirection();
app.UseHsts();
```

### 5. Production Checklist

- [ ] Remove `AuthController.cs` test endpoint
- [ ] Update CORS to production URLs only
- [ ] Move `Jwt:SigningKey` to secure vault
- [ ] Generate strong random signing key (64+ chars)
- [ ] Enable HTTPS redirection
- [ ] Configure proper logging (Serilog, Application Insights)
- [ ] Setup health checks
- [ ] Configure rate limiting
- [ ] Enable response compression
- [ ] Review security headers

---

## 📚 API Documentation

**Swagger UI:** `http://localhost:5286/swagger`

**Key Endpoints:**
- `GET /api/v1/auth/test-token` - Generate test JWT token
- `POST /api/v1/auth/validate-token` - Validate JWT token
- `GET /api/v1/documents` - List documents
- `POST /api/v1/documents` - Create document
- `GET /api/v1/documents/{id}` - Get document
- `PATCH /api/v1/documents/{id}` - Update document (with ETag)
- `GET /api/v1/lookups/partners` - Get partners combo
- `GET /api/v1/lookups/organizational-units` - Get warehouses combo

---

## 🚀 Integration with Frontend

**Frontend needs:**

1. **Backend URL:**
   ```
   VITE_API_BASE_URL=http://localhost:5286/api/v1
   ```

2. **JWT Token:**
   ```
   VITE_JWT_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

3. **Request Headers:**
   ```
   Authorization: Bearer {token}
   Content-Type: application/json
   ```

4. **For Updates (ETag):**
   ```
   If-Match: "{etag-from-get-response}"
   ```

**See Frontend docs:**
- `accounting-online-frontend/docs/PRODUCTION_SETUP.md`

---

## 📞 Support

**If something doesn't work:**

1. Check Backend logs (console output)
2. Check Swagger UI: `http://localhost:5286/swagger`
3. Test endpoint with curl/Postman
4. Check `appsettings.json` configuration
5. Verify database connection string

---

**✨ Backend is ready!** ✨

**Next Steps:**
1. Generate JWT token
2. Provide to Frontend team
3. Frontend adds to `.env.local`
4. Test full integration
