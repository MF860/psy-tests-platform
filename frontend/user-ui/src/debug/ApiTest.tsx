// Temporary debug component to test API calls directly
import { useState } from 'react';
import { getNextQuestion, startSession } from '../lib/api-client';

export default function ApiTest() {
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const testApi = async () => {
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      // First create a new session using our client function
      console.log('Creating new session...');
      const sessionData = await startSession('1000000003');
      console.log('Session created:', sessionData);
      const sessionId = sessionData.sessionId;
      
      console.log('Testing API with session:', sessionId);
      
      const response = await fetch(`http://localhost:5019/api/sessions/${sessionId}/next`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      console.log('Response status:', response.status);
      console.log('Response headers:', response.headers);
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      
      const data = await response.json();
      console.log('Raw API response:', data);
      setResult(data);
      
      // Now test with our client function
      const clientResponse = await getNextQuestion(sessionId);
      console.log('Client function response:', clientResponse);
      
    } catch (err: any) {
      console.error('API test error:', err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-4">API Debug Test</h1>
      
      <button
        onClick={testApi}
        disabled={loading}
        className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded mb-4"
      >
        {loading ? 'Testing...' : 'Test API Call'}
      </button>
      
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          <strong>Error:</strong> {error}
        </div>
      )}
      
      {result && (
        <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
          <strong>Result:</strong>
          <pre className="mt-2 text-sm overflow-auto">
            {JSON.stringify(result, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
}