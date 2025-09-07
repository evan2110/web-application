# Web Application

A full‑stack demo implementing authentication with JWT access/refresh tokens, email verification, token blacklisting, and a small React client.

### Features
- **User registration and login**: Email + password with hashed storage (BCrypt).
- **Email verification flow**: Send verification code, verify user before enabling access.
- **JWT authentication**: Short‑lived access token, long‑lived refresh token.
- **Token refresh**: Issue new access token using a valid refresh token.
- **Token blacklist**: Invalidate tokens on logout or security events; periodic cleanup.
- **Protected routes (client)**: Guarded views using React Router.
- **Basic profile and todo demo pages**: Example UI wiring with context for auth state.
- **Swagger/OpenAPI**: Interactive API docs in development.

### System Architecture
- **Client**: React 18 (Create React App), React Router v6.
- **Server**: ASP.NET Core 7 Web API with JWT authentication, background service for blacklist cleanup, and email sending service.
- **Database**: Supabase/PostgreSQL.

### Database Design
The schema contains four main tables, matching the diagram provided:

- **user**
  - `id` (int8, PK)
  - `created_at` (timestamptz)
  - `email` (varchar)
  - `password` (varchar, BCrypt‑hashed)
  - `user_type` (varchar)

- **refresh_token**
  - `id` (int8, PK)
  - `created_at` (timestamptz)
  - `token` (varchar)
  - `expires_at` (timestamptz)
  - `revoked_at` (timestamptz, nullable)
  - `user_id` (int8, FK → user.id)

- **blacklisted_token**
  - `id` (int8, PK)
  - `blacklisted_at` (timestamptz)
  - `token` (varchar)
  - `expires_at` (timestamptz)
  - `user_id` (int8, FK → user.id)
  - `reason` (varchar)

- **user_code_verify**
  - `id` (int8, PK)
  - `user_id` (int8, FK → user.id)
  - `verify_code` (varchar)
  - `status` (varchar)

Relations: `user` has many `refresh_token`, many `blacklisted_token`, and many `user_code_verify` records.

### API Overview (Server)
- `AuthController`
  - `POST /api/auth/register`: Register a new user and send a verification code.
  - `POST /api/auth/verify`: Verify code to activate the account.
  - `POST /api/auth/login`: Authenticate; returns access and refresh tokens.
  - `POST /api/auth/refresh`: Exchange refresh token for a new access token.
  - `POST /api/auth/logout`: Blacklist active tokens and revoke refresh token.

Middleware and services:
- `TokenValidationMiddleware`: Handles token validation and blacklist checks.
- `BlacklistCleanupService`: Background job that removes expired blacklisted tokens.
- `ITokenService`/`TokenService`: Create/validate JWTs, manage refresh tokens.
- `IBlacklistService`/`BlacklistService`: Add/check blacklisted tokens.
- `IMailService`/`MailService`: Send verification emails.
- `ISupabaseService`/`SupabaseService`: Data access using Supabase client.

### Tech Stack
- **Frontend**: React 18, React Router 6, CRA (react‑scripts).
- **Backend**: .NET 7, ASP.NET Core Web API, JWT Bearer auth, Swagger.
- **Database**: PostgreSQL via Supabase.
- **Testing**: xUnit tests for controllers, models, and services in `test` project.

### Packages

Client (`client/package.json`):
- `react`, `react-dom`
- `react-router-dom`
- `react-scripts`
- `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`
- `web-vitals`

Server (`server/server.csproj`):
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`
- `Swashbuckle.AspNetCore` (Swagger)
- `Microsoft.AspNetCore.OpenApi`
- `BCrypt.Net-Next` (password hashing)
- `Supabase` (data access)
- `MailKit`, `MimeKit` (email)

### Configuration
- Configure JWT and mail settings in `server/appsettings.json` (and `appsettings.Development.json`).
- CORS is enabled for `http://localhost:3000` in `Program.cs`.

### Running Locally
1. Server
   - .NET 7 SDK required.
   - From `server/`: `dotnet restore` then `dotnet run`.
   - Swagger UI available in development at `/swagger`.
2. Client
   - Node 16+ recommended.
   - From `client/`: `npm install` then `npm start`.

### Folder Structure
- `client/`: React app source.
- `server/`: ASP.NET Core API.
- `test/`: xUnit tests for server components.

### Notes
- Access tokens are short‑lived; refresh tokens are stored in DB and can be revoked.
- On logout, tokens are blacklisted; middleware rejects blacklisted tokens.
