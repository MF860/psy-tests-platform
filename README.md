# Psychometric Testing Platform for the Jordanian Army

This platform is designed to administer and manage psychometric tests for the Jordanian Army recruitment and assessment processes.

## System Architecture

The platform consists of:
- **Backend**: ASP.NET Core Web API with SQLite (development) / PostgreSQL (production) database
- **User UI**: React + TypeScript + Vite application for test takers (80 questions per session)
- **Admin UI**: React + TypeScript + Vite application for administrators

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+
- PostgreSQL 14+ (production) or SQLite (development)
- Git

## Database Configuration

### Development (SQLite)
Set environment variable: `USE_SQLITE=1`
Database file: `psy_dev.db` (auto-created)

### Production (PostgreSQL)
Configure connection string in `appsettings.json`

## SDJ Mode Configuration

The platform supports **dual-mode operation** for psychometric assessments:

- **SDJ Mode (`USE_SDJ=1`)**: 120-item assessment with Structured Dimensional Judgment framework
  - 5 dimensions: Realistic, Investigative, Artistic, Social, Enterprising
  - 24 subdimensions mapped to career tracks
  - Career track recommendations (Top 3 fits)
  - 3-page PDF report with charts and interpretation
  - CSV file: `questions_sdj_ar.csv`

- **Legacy Mode (`USE_SDJ=0`)**: 200-item assessment with diverse question types
  - Mixed item types: MCQ, TIMED_NUMERIC, ORDERING, LikertAgreement, Frequency
  - Traditional dimensional scoring
  - 4-6 page PDF report
  - CSV file: `questions_fixed_extended_plus_personality.csv`

### Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `USE_SDJ` | `1` or `0` | Enables SDJ mode (1) or legacy mode (0) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` | Runtime environment |

### Switching Modes

#### Linux/macOS

```bash
# Enable SDJ mode (120 items)
export USE_SDJ=1
export ASPNETCORE_ENVIRONMENT=Development

# Enable legacy mode (200 items)
export USE_SDJ=0
export ASPNETCORE_ENVIRONMENT=Development
```

#### Windows (PowerShell)

```powershell
# Enable SDJ mode (120 items)
$env:USE_SDJ = "1"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Enable legacy mode (200 items)
$env:USE_SDJ = "0"
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

### Mode Switching Procedure

**Important:** Switching modes requires database reseeding.

1. Stop the backend application (Ctrl+C)
2. Delete the database file (development):
   ```bash
   rm backend/PsyApi/psy_dev.db
   ```
3. Set the desired `USE_SDJ` value
4. Restart the backend:
   ```bash
   cd backend/PsyApi
   dotnet run
   ```
5. Verify seed logs:
   - SDJ mode: `Successfully seeded 120 item parameters`
   - Legacy mode: `Successfully seeded 200 item parameters`

### Expected Behavior

| Mode | Items | CSV File | Result Field | PDF Pages | Admin UI Badge |
|------|-------|----------|--------------|-----------|----------------|
| SDJ (`USE_SDJ=1`) | 120 | `questions_sdj_ar.csv` | `sdjData` present | 3 | Blue "SDJ: {profile}" |
| Legacy (`USE_SDJ=0`) | 200 | `questions_fixed_extended_plus_personality.csv` | `sdjData` absent | 4-6 | No badge |

### API Verification

#### SDJ Mode
```bash
# Start session (SDJ)
curl -X POST http://localhost:5019/api/sessions/start \
  -H "Content-Type: application/json" \
  -d '{"nationalId":"1000000001"}'

# Expected response includes:
# - totalItems: 120
# - currentItem.type: "LikertAgreement_SDJ"
# - sdjData field in results
```

#### Legacy Mode
```bash
# Start session (Legacy)
curl -X POST http://localhost:5019/api/sessions/start \
  -H "Content-Type: application/json" \
  -d '{"nationalId":"1000000001"}'

# Expected response includes:
# - totalItems: 200
# - currentItem.type: "MCQ", "TIMED_NUMERIC", "ORDERING", etc.
# - NO sdjData field in results
```

### Production Deployment

For production, ensure CSV files are deployed:
- `backend/PsyApi/seed/questions_sdj_ar.csv` (SDJ mode)
- `backend/PsyApi/seed/questions_fixed_extended_plus_personality.csv` (Legacy mode)

Set `USE_SDJ=1` in production environment variables to enable SDJ mode.

### Documentation

- **Go-Live Checklist:** `docs/SDJ_GO_LIVE_CHECKLIST.md`
- **Implementation Guide:** `IMPLEMENTATIONS/SDJ_IMPLEMENTATION_COMPLETE.md`
- **API Documentation:** `backend/PsyApi/README.md`

## Setup Instructions

### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd psy-tests-platform/backend/PsyApi
   ```

2. Install dependencies:
   ```bash
   dotnet restore
   ```

3. Configure the database connection in `appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=psydb;Username=psyuser;Password=your_password"
   }
   ```

4. Apply EF migrations:
   ```bash
   dotnet ef database update
   ```

5. Run the backend:
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:7123`

### User UI Setup

1. Navigate to the user UI directory:
   ```bash
   cd psy-tests-platform/frontend/user-ui
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Run the development server:
   ```bash
   npm run dev
   ```

   The user UI will be available at `http://localhost:5175`

### Admin UI Setup

1. Navigate to the admin UI directory:
   ```bash
   cd psy-tests-platform/frontend/admin-ui
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Run the development server:
   ```bash
   npm run dev
   ```

   The admin UI will be available at `http://localhost:5173`

## Default Admin Login

- Username: `root`
- Password: `StrongAdmin!23!` (can be changed via `ADMIN_SEED_PASSWORD` environment variable)

## API Endpoints

### User Endpoints

- `POST /api/sessions` - Start a new test session
- `GET /api/sessions/{id}` - Get session details
- `POST /api/sessions/{id}/answers` - Submit answers
- `GET /api/results/{id}/birkman` - Get Birkman analysis report

### Admin Endpoints

- `POST /api/admin/login` - Admin login
- `GET /api/admin/analytics/overview` - Get analytics overview
- `GET /api/results` - Get all results
- `GET /api/results/{id}/pdf` - Download PDF report
- `GET /api/admin/audit` - Get audit logs

## Features

### User Interface

- National ID entry and validation
- Privacy policy acceptance
- 100-question psychometric test with timer
- Support for multiple question types (MCQ, Likert, Numeric, Ordering, Text)
- Light mode interface with professional design
- Thank you page with session summary

### Admin Interface

- Dashboard with KPIs and charts
- Results management with search, pagination, and sorting
- Detailed result view with dimension scores and charts
- PDF report generation and download
- Audit log viewing with filters
- Settings page for password change

### Backend Features

- JWT-based authentication for admin routes
- Rate limiting for security
- Response compression for performance
- Comprehensive audit logging
- Birkman personality assessment mapping
- PDF report generation in Arabic and English
- Analytics and reporting capabilities
- **AI-Powered Analysis** with DeepSeek for SDJ results (Arabic)

## Environment Variables

The following environment variables can be configured:

### Database & Authentication
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `ADMIN_SEED_PASSWORD` - Password for the default admin account
- `Jwt__Secret` - JWT signing secret (minimum 64 characters)

### AI Analysis (SDJ Mode)
- `DEEPSEEK_API_KEY` - **Required** for AI-powered analysis (server-side only)
- `DEEPSEEK_MODEL` - Model to use (default: `deepseek-chat`)
- `DEEPSEEK_MAX_TOKENS` - Max tokens for response (default: 1500)
- `DEEPSEEK_TEMPERATURE` - Creativity level (default: 0.2)

**Setting up AI Analysis:**

```powershell
# Windows PowerShell
setx DEEPSEEK_API_KEY "sk-9c6a11074bac4e598ffef48e9bad9380"

# Then restart the backend
cd backend\PsyApi
dotnet run
```

```bash
# Linux/macOS
export DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"

# Then restart the backend
cd backend/PsyApi
dotnet run
```

**Important Security Notes:**
- ✅ API keys must ONLY be set via environment variables
- ✅ Never commit API keys to source control
- ✅ Keys are never exposed to frontend
- ✅ Fallback analysis available if API key not set

**Learn more:** See `docs/AI_ANALYZER_SDJ.md` for detailed documentation.

## Deployment

### Production Deployment

1. Set up a PostgreSQL database in your production environment.

2. Configure the production environment variables:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=your-db-host;Database=your-db;Username=your-user;Password=your-password"
   export ADMIN_SEED_PASSWORD="your-secure-admin-password"
   export Jwt__Secret="your-very-secure-jwt-secret-minimum-64-characters-long"
   export DEEPSEEK_API_KEY="your-deepseek-api-key"
   ```

3. Build and publish the backend:
   ```bash
   cd psy-tests-platform/backend/PsyApi
   dotnet publish -c Release -o ./publish
   ```

4. Build the frontend applications:
   ```bash
   cd psy-tests-platform/frontend/user-ui
   npm run build

   cd ../admin-ui
   npm run build
   ```

5. Deploy the applications to your web server.

### Docker Deployment

Docker support can be added by creating Dockerfile and docker-compose.yml files for containerized deployment.

## Testing

### API Testing

You can test the API endpoints using curl or any API client like Postman.

Example login request:
```bash
curl -X POST https://your-api-domain/api/admin/login   -H "Content-Type: application/json"   -d '{"username":"root","password":"StrongAdmin!23!"}'
```

Example get results request:
```bash
curl -X GET https://your-api-domain/api/results   -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### PDF Generation

To test PDF generation:
```bash
curl -X GET https://your-api-domain/api/results/1/pdf   -H "Authorization: Bearer YOUR_JWT_TOKEN"   -o report.pdf
```

## Security Considerations

- All admin routes are protected with JWT authentication
- Rate limiting is implemented to prevent brute force attacks
- Audit logs track all admin actions
- Input validation is performed on all endpoints
- Passwords are securely hashed
- CORS is configured to only allow specific origins

## Performance Optimizations

- Response compression is enabled for API responses
- Database indexes are optimized for query performance
- ETags are implemented for conditional requests
- Frontend assets are optimized for fast loading

## License

This project is proprietary software for the Jordanian Army. All rights reserved.
#   F o r c e   R e n d e r   r e d e p l o y  
 