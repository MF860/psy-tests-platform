# Demo Mode Implementation

This implementation provides a complete **client-side demo mode** for both the user UI and admin UI, allowing the application to run 100% without a backend server.

## 🚀 Quick Start

### Enable Demo Mode

1. **User UI:**
   ```bash
   cd frontend/user-ui
   cp .env.demo .env
   npm run dev
   ```

2. **Admin UI:**
   ```bash
   cd frontend/admin-ui
   cp .env.demo .env
   npm run dev
   ```

The demo badge will appear in the top-right corner when demo mode is active.

## 📋 Features

### ✅ Complete API Simulation
- **Session Management**: Start, resume, answer submission, completion
- **Question Delivery**: 80 realistic questions covering all types
- **Scoring Engine**: Generates believable T-scores and dimension analysis  
- **Admin Dashboard**: Results management, analytics, AI recommendations
- **Persistence**: LocalStorage-based session and answer storage

### ✅ Question Types Supported
- **MCQ**: Multiple choice with correct/incorrect scoring
- **Likert Agreement**: 1-5 scale personality assessment
- **Frequency**: Behavioral frequency scales
- **ORDERING**: Drag-and-drop priority ranking
- **TIMED_NUMERIC**: Speed-based math problems
- **Text**: Open-ended text responses

### ✅ Admin Features
- Results dashboard with filtering and pagination
- Individual result details with dimension breakdowns
- AI-powered recommendations (mock or real via OpenAI)
- Analytics overview with realistic metrics
- PDF export simulation

## 🔧 Configuration

### Environment Variables

```env
# Enable demo mode
VITE_DEMO_MODE=true

# Optional: Real OpenAI integration even in demo mode
VITE_OPENAI_KEY=sk-your-key-here

# API base (ignored in demo mode)
VITE_API_BASE_URL=http://localhost:5019/api
```

### Demo vs Production Mode

| Feature | Demo Mode | Production Mode |
|---------|-----------|-----------------|
| Backend Required | ❌ No | ✅ Yes |
| Data Persistence | localStorage | Database |
| AI Recommendations | Mock/OpenAI | OpenAI |
| Session Resume | ✅ Yes | ✅ Yes |
| Real-time Sync | ❌ No | ✅ Yes |

## 📊 Mock Data

### Questions (`src/mocks/questions.json`)
- 80 carefully crafted questions
- Arabic text with proper RTL support
- All question types represented
- Realistic time limits and scoring

### Results (`src/mocks/results.json`)
- 10 sample participant results
- Realistic T-scores (40-70 range)
- Proper dimension analysis
- Arabic names and national IDs

### Recommendations (`src/mocks/recommendations.ts`)
- Dimension-based analysis
- Strengths and growth areas
- Actionable development plans
- Course recommendations

## 🧪 Testing Demo Mode

### Browser Console Testing
```javascript
// Load test utilities
import('./src/mocks/test-demo.ts').then(tests => {
  window.demoTests = tests;
});

// Run full test suite
window.demoTests.runDemoTests();

// Test individual functions
const sessionId = await window.demoTests.testSessionStart();
const question = await window.demoTests.testGetQuestion(sessionId);
```

### User Flow Testing
1. **Login**: Enter any national ID (demo user created automatically)
2. **Privacy**: Accept terms and start session  
3. **Instructions**: Read and proceed to exam
4. **Exam**: Answer questions (auto-saves to localStorage)
5. **Results**: Complete exam to see scoring

### Admin Flow Testing
1. **Login**: Use any credentials in demo mode
2. **Dashboard**: View analytics and recent results
3. **Results**: Browse paginated results list
4. **Details**: Click result for full analysis
5. **AI**: Generate recommendations for any result

## 🔒 Security & Limitations

### Demo Mode Limitations
- ❌ No real authentication
- ❌ Data not persisted between browser sessions (localStorage only)
- ❌ No server-side validation
- ❌ Limited to single device/browser

### Production Safety
- ✅ Zero impact on production builds when `VITE_DEMO_MODE=false`
- ✅ Same API interfaces maintained
- ✅ Type safety preserved
- ✅ Mock router only loads in demo mode

## 🛠️ Architecture

### Mock Router System
```typescript
// Intercepts API calls in demo mode
if (isDemoMode()) {
  return await mockRouter<T>(path, init);
}
// Falls back to real API
return fetch(buildUrl(path), options);
```

### Storage Strategy
```typescript
// Session data
localStorage.setItem(`demo_session_${sessionId}`, JSON.stringify(session));

// Results for admin
localStorage.setItem('demo_results', JSON.stringify(results));

// Resume support
localStorage.setItem(`demo_session_by_id_${nationalId}`, sessionId);
```

### Scoring Algorithm
```typescript
// Realistic T-score generation
const tScore = Math.max(40, Math.min(70, baseScore + variance));
const percentile = Math.round(((tScore - 30) / 40) * 100);
```

## 🔄 Migration Path

### Demo → Production
1. Set `VITE_DEMO_MODE=false`
2. Configure real API endpoints
3. Set up authentication
4. Deploy with backend services

### Data Export (if needed)
```javascript
// Export demo results
const results = JSON.parse(localStorage.getItem('demo_results') || '[]');
console.log('Demo results:', results);
```

## 🐛 Troubleshooting

### Demo Badge Not Showing
- Check `.env` file has `VITE_DEMO_MODE=true`
- Restart dev server after env changes
- Verify environment variable in browser console: `import.meta.env.VITE_DEMO_MODE`

### API Calls Not Mocked
- Ensure mock imports are valid (JSON files exist)
- Check browser console for mock router logs
- Verify API calls use relative paths (not absolute URLs)

### Session Not Resuming  
- Check localStorage in browser DevTools
- Look for `demo_session_*` keys
- Ensure same nationalId is used

### Scoring Issues
- Verify all question types are handled in `calculateMockScores`
- Check dimension weights sum to ~1.0
- Ensure T-scores stay in 40-70 range

## 📞 Support

For issues with demo mode implementation:
1. Check browser console logs
2. Verify environment configuration  
3. Test with provided demo test functions
4. Review this README for configuration steps

---

**Demo Mode Status**: ✅ Fully Implemented
**Type Safety**: ✅ Maintained  
**Production Ready**: ✅ Zero impact when disabled