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

## Environment Variables

The following environment variables can be configured:

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `ADMIN_SEED_PASSWORD` - Password for the default admin account
- `Jwt__Secret` - JWT signing secret (minimum 64 characters)

## Deployment

### Production Deployment

1. Set up a PostgreSQL database in your production environment.

2. Configure the production environment variables:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=your-db-host;Database=your-db;Username=your-user;Password=your-password"
   export ADMIN_SEED_PASSWORD="your-secure-admin-password"
   export Jwt__Secret="your-very-secure-jwt-secret-minimum-64-characters-long"
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
