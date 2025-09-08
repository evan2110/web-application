# Web Application

A full‑stack demo implementing authentication with JWT access/refresh tokens, email verification, token blacklisting, and a small React client.

### Features
- **User registration and login**: Email + password with hashed storage (BCrypt).
- **Email verification (link-based)**: Verification link sent on registration; client page `/verify-email` calls API and shows success/failure.
- **Admin code verification (2-step on login)**: Admin users must enter a verification code emailed at login.
- **Resend verification code**: Endpoint to resend admin verification code.
- **Forgot/Reset password**: Email reset link with token, secure password update.
- **JWT authentication**: Short‑lived access token, long‑lived refresh token.
- **Token refresh**: Issue new access token using a valid refresh token.
- **Token blacklist**: Invalidate tokens on logout or security events; periodic cleanup.
- **Protected routes (client)**: Guarded views using React Router.
- **Basic profile and todo demo pages**: Example UI wiring with context for auth state.
- **Swagger/OpenAPI**: Interactive API docs in development.
- **Role-based Admin UI (client)**: Admin-only dashboard and Users list; guarded on client and server.

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
  - `confirmed_at` (timestamptz, nullable)
  - `confirmed_token` (varchar, nullable)
  - `reset_token` (varchar, nullable)

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
  - `user_id` (int8, FK → user.id, nullable)
  - `reason` (varchar)

- **user_code_verify**
  - `id` (int8, PK)
  - `user_id` (int8, FK → user.id)
  - `verify_code` (varchar)
  - `status` (int)

Relations: `user` has many `refresh_token`, many `blacklisted_token`, and many `user_code_verify` records.

#### Schema

User (`user`)
- id: bigint, primary key
- created_at: timestamptz, required, default now()
- email: varchar, required, unique
- password: varchar, required (BCrypt hashed)
- user_type: varchar, required
- confirmed_at: timestamptz, nullable
- confirmed_token: varchar, nullable
- reset_token: varchar, nullable

RefreshToken (`refresh_token`)
- id: bigint, primary key
- created_at: timestamptz, required, default now()
- token: varchar, required
- expires_at: timestamptz, required
- revoked_at: timestamptz, nullable
- user_id: bigint, required, FK → user.id (cascade on delete)

BlacklistedToken (`blacklisted_token`)
- id: bigint, primary key
- blacklisted_at: timestamptz, required, default now()
- token: varchar, required
- expires_at: timestamptz, required
- user_id: bigint, nullable, FK → user.id (set null on delete)
- reason: varchar, required

UserCodeVerify (`user_code_verify`)
- id: bigint, primary key
- user_id: bigint, required, FK → user.id (cascade on delete)
- verify_code: varchar, required
- status: int, required

### API Overview (Server)
- `AuthController`
  - `POST /api/auth/register`: Register a new user and send a verification code.
  - `GET /api/auth/verify-email?token=...`: Validate verification token; returns JSON success/error. Client page handles UX.
  - `POST /api/auth/verify`: Verify admin code during login and issue tokens.
  - `POST /api/auth/login`: Authenticate; returns access and refresh tokens.
  - `POST /api/auth/refresh`: Exchange refresh token for a new access token.
  - `POST /api/auth/logout`: Blacklist active tokens and revoke refresh token.
  - `GET /api/auth/sendMail?email=...`: Resend admin verification code to an email.
  - `POST /api/auth/forgot-password`: Send password reset link to email.
  - `POST /api/auth/reset-password`: Reset password using a valid reset token.
- `UsersController`
  - `GET /api/users` (Admin only): Returns list of users. Secured with `[Authorize(Roles = "admin")]`.

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
- Email verification and password reset use base URLs from config:
  - `Frontend:BaseUrl` (client links, e.g., `/verify-email?token=...`)
  - `Backend:BaseUrl` (server origin)
- Messages: `Messages` section defines all user-facing texts; controllers use `IMessageProvider` with codes from `CommonUtils.MessageCodes`.

### Running Locally
1. Server (HTTPS)
   - .NET 7 SDK required.
   - Trust dev HTTPS certificate (one‑time): `dotnet dev-certs https --trust`
   - From `server/`:
     - Restore deps: `dotnet restore`
     - Run API (HTTPS): `dotnet run`
   - The API listens on: `https://localhost:7297`
   - Swagger UI (dev): `https://localhost:7297/swagger`
   - Note: Project is configured to redirect HTTP → HTTPS.
2. Client (React)
   - Node 16+ recommended.
   - Create env file `client/.env` with API base URL (optional in dev due to proxy):
     - `REACT_APP_API_BASE_URL=https://localhost:7297`
   - From `client/`:
     - Install deps: `npm install`
     - Start dev server: `npm start` (dev proxy to API is configured in `client/package.json`)
   - The app runs on: `http://localhost:3000`
   - The client reads the API base URL from `REACT_APP_API_BASE_URL`.

### Folder Structure
- `client/`: React app source.
- `server/`: ASP.NET Core API.
- `test/`: xUnit tests for server components.

### Notes
- Access tokens are short‑lived; refresh tokens are stored in DB and can be revoked.
- On logout, tokens are blacklisted; middleware rejects blacklisted tokens.
- Admin role string is `admin` (lowercase) in both client guards and server `[Authorize(Roles = "admin")]`.

### Tests & Coverage
- Run unit tests (server):
  - From `test/`: `dotnet test`
- View existing coverage report:
  - Open `coverage/index.html` in a browser.
- Regenerate coverage (Coverlet + optional HTML):
  - From `test/` (save results to `../coverage/`):
    - Collect coverage: `dotnet test /p:CollectCoverage=true /p:CoverletOutput=../coverage/coverage` 
    - Output formats (choose as needed): add `/p:CoverletOutputFormat=opencover` or `lcov`.
    - If you want an HTML report, generate from the coverage file using ReportGenerator:
      - `reportgenerator -reports:../coverage/coverage.opencover.xml -targetdir:../coverage -reporttypes:Html`
  - Then open `coverage/index.html`.
