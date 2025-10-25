/**
 * Simple tests for mock functionality
 * Run these in the browser console when demo mode is enabled
 */

// Test session start
async function testSessionStart() {
  console.log('Testing session start...');
  try {
    const response = await fetch('/api/sessions/start', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nationalId: '1234567890' })
    });
    const data = await response.json();
    console.log('Session start response:', data);
    return data.sessionId;
  } catch (error) {
    console.error('Session start error:', error);
  }
}

// Test getting questions
async function testGetQuestion(sessionId: string) {
  console.log('Testing get question...');
  try {
    const response = await fetch(`/api/sessions/${sessionId}/next`);
    const data = await response.json();
    console.log('Question response:', data);
    return data;
  } catch (error) {
    console.error('Get question error:', error);
  }
}

// Test submitting answer
async function testSubmitAnswer(sessionId: string, questionId: string) {
  console.log('Testing submit answer...');
  try {
    const response = await fetch(`/api/sessions/${sessionId}/answer`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ItemId: questionId,
        Answer: 'Test Answer',
        ResponseTimeMs: 5000
      })
    });
    const data = await response.json();
    console.log('Submit answer response:', data);
    return data;
  } catch (error) {
    console.error('Submit answer error:', error);
  }
}

// Run full test sequence
async function runDemoTests() {
  console.log('🚀 Running demo mode tests...');
  
  // Test session lifecycle
  const sessionId = await testSessionStart();
  if (!sessionId) {
    console.error('❌ Failed to start session');
    return;
  }
  
  const question = await testGetQuestion(sessionId);
  if (!question || question.message === 'completed') {
    console.error('❌ Failed to get question');
    return;
  }
  
  const answerResult = await testSubmitAnswer(sessionId, question.item_id);
  if (!answerResult) {
    console.error('❌ Failed to submit answer');
    return;
  }
  
  console.log('✅ All demo tests passed!');
  console.log('Demo mode is working correctly.');
}

// Export functions for manual testing
(window as any).demoTests = {
  testSessionStart,
  testGetQuestion,
  testSubmitAnswer,
  runDemoTests
};

console.log('Demo test functions available as window.demoTests');
export { testSessionStart, testGetQuestion, testSubmitAnswer, runDemoTests };