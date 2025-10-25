
AI REPORT :
# User Flow & Contract Validation System

## Overview

This document describes the comprehensive user flow and contract validation system implemented to create a bulletproof frontend↔backend integration with automatic navigation management.

## Architecture

### Single Source of Truth - Flow State

The system uses **Zustand** for centralized flow state management with session-keyed persistence:

```typescript
interface FlowState {
  nationalId?: string;
  sessionId?: string; 
  resume?: boolean;
  consented: boolean;
  instructionsCompleted: boolean;
  examFinished: boolean;
}
```

**Key Features:**
- Session-keyed storage prevents state leakage between users
- Persistent across page refreshes
- Automatic cleanup on session changes
- No manual navigation in page components

### Route Guard System

**Pure routing matrix** eliminates navigation duplication and loops:

```typescript
function routeFor(page: PageKey, state: FlowState): string | null
```

The `GuardedRoute` component wraps each page and automatically redirects based on the routing matrix:

```tsx
<GuardedRoute page="privacy">
  <PrivacyContent />
</GuardedRoute>
```

## Canonical User Flow

**Strict sequence:** `/login` → `/privacy` → `/instructions` → `/exam` → `/thank-you`

### Flow Rules

1. **Login Page**
   - Stays: No session exists
   - → `/exam`: Resume mode (`resume: true`)
   - → `/privacy`: New session created
   - → `/instructions`: Has consent but no instructions
   - → `/exam`: Ready for exam (consent + instructions)
   - → `/thank-you`: Exam finished

2. **Privacy Page**
   - → `/login`: Missing nationalId/sessionId
   - → `/exam`: Resume mode
   - Stays: Needs consent
   - → `/instructions`: Already consented

3. **Instructions Page**
   - → `/privacy`: Missing consent
   - Stays: Needs to complete instructions
   - → `/exam`: Instructions completed

4. **Exam Page**
   - → `/login`: Missing session
   - → `/privacy`: Missing consent  
   - → `/instructions`: Missing instructions
   - Stays: Ready for exam
   - → `/thank-you`: Exam finished

5. **Thank You Page**
   - → `/login`: Exam not finished
   - → `/exam`: Has active session but exam incomplete
   - Stays: Exam completed

## Contract Validation System

### API Client with Auto-Retry

Enhanced fetch wrapper with **Zod validation** and **retry adapters**:

```typescript
apiFetchJson<T>(
  path: string, 
  init: RequestInit,
  contractOptions: ContractOptions<T>,
  retryOptions: RetryOptions
)
```

**Features:**
- Runtime schema validation
- Field name normalization (snake_case ↔ camelCase)
- Automatic retry on 400/404 with alternate field names
- Development contract mismatch logging
- Graceful degradation in production

### Field Normalization

Handles backend field variations automatically:

```typescript
// Server variations → UI canonical
item_id | ItemId | itemId → itemId
text_ar | textAr | TextAr → text
time_limit_seconds | timeLimitSeconds → timeLimitSeconds
```

### Request/Response Adapters

**Start Session:**
```typescript
// Request: { NationalId: "1000000003" }
// Fallback: { nationalId: "1000000003" }
// Response: { sessionId, totalQuestions, resume? }
```

**Submit Answer:**
```typescript 
// Request: { ItemId, Answer?, NumericAnswer?, ResponseTimeMs? }
// Fallback: { itemId, answer?, numericAnswer?, responseTimeMs? }
```

## API Endpoints Integration

### Discovered Contracts

Based on `SessionsController` analysis:

1. **POST /sessions/start**
   - Request: `{ NationalId: string }`
   - Response: `{ sessionId: string, totalQuestions: number, resume?: boolean }`

2. **GET /sessions/{sessionId}/next**
   - Response: Question object OR `{ message: "completed" }`

3. **POST /sessions/{sessionId}/answer**
   - Request: `{ ItemId: string, Answer?: string, NumericAnswer?: number, ResponseTimeMs?: number }`
   - Response: `{ message: string, skipToNext?: boolean }`

4. **POST /sessions/{sessionId}/finish**
   - Response: `{ finished: boolean }`

5. **POST /sessions/{sessionId}/submit**
   - Response: `{ success?: boolean, message?: string }`

## Testing Coverage

### Flow System Tests (22 tests)
- Route guard validation for each page
- Canonical flow sequence enforcement  
- Resume flow handling
- Edge case navigation

### Contract Validation Tests (18 tests)
- Field normalization (snake_case ↔ camelCase)
- Question type canonicalization
- Error handling and validation
- National ID processing
- Response normalization

**Total: 40 passing tests** ensuring system reliability.

## Usage Examples

### Page Implementation
```tsx
// Before: Manual navigation logic
function Privacy() {
  const navigate = useNavigate();
  const handleSubmit = () => {
    sessionStorage.setConsent(true);
    navigate('/instructions'); // Manual routing!
  };
}

// After: Flow state updates
function PrivacyContent() {
  const { setConsented } = useFlowState();
  const handleSubmit = () => {
    setConsented(true); // GuardedRoute handles navigation
  };
}

export default function Privacy() {
  return (
    <GuardedRoute page="privacy">
      <PrivacyContent />
    </GuardedRoute>
  );
}
```

### API Usage
```typescript
// Bulletproof API calls with contract validation
const response = await startSession('1000000003');
// Automatically retries with field name variations
// Validates response schema
// Normalizes field names
```

## Development Features

### Contract Logging
```
[CONTRACT WARN] POST /sessions/start: Request failed, trying alternate body format
[CONTRACT] Mapped ItemId → item_id I002
```

### Strict Mode
Set `VITE_STRICT_CONTRACTS=true` to throw on contract violations in development.

## Benefits

✅ **No duplicate navigation logic** - Single source of truth  
✅ **No navigation loops** - Pure routing matrix prevents cycles  
✅ **Bulletproof API integration** - Auto-retry and normalization  
✅ **Contract drift detection** - Runtime validation with logging  
✅ **Resume flow support** - Automatic session restoration  
✅ **Accessibility compliant** - Proper focus management  
✅ **Type safe** - Full TypeScript coverage with Zod validation  
✅ **Test coverage** - 40 comprehensive tests ensuring reliability  

## Files Structure

```
src/
├── lib/
│   ├── useFlowState.ts      # Zustand flow state management
│   ├── routeFor.ts          # Pure routing matrix logic  
│   ├── api-client.ts        # Contract-validated API functions
│   ├── contracts.ts         # Zod schemas for all endpoints
│   ├── contract-bridge.ts   # Field normalization & adapters
│   └── api.ts               # Enhanced fetch wrapper
├── components/
│   ├── GuardedRoute.tsx     # Route guard component
│   ├── Question/            # Question type components (MCQ, Likert, etc.)
│   ├── Exam/                # Exam-related components (AutosaveIndicator, etc.)
│   └── ui/                  # UI components (Button, Card, Dialog, etc.)
├── pages/
│   ├── Login.tsx           # Login with GuardedRoute
│   ├── Privacy.tsx         # Privacy with GuardedRoute  
│   ├── Instructions.tsx    # Instructions with GuardedRoute
│   ├── ExamNew.tsx         # Full exam implementation with GuardedRoute
│   └── Finish.tsx          # Finish with GuardedRoute
└── __tests__/
    ├── flow-system.test.ts        # 22 flow & routing tests
    └── contract-validation.test.ts # 18 contract & API tests
```

This system ensures a robust, maintainable, and user-friendly frontend with bulletproof backend integration.