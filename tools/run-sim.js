/// tools/run-sim.js
const axios = require('axios');
const BASE_URL = 'http://localhost:5019/api';
const NationalId = '1000000001'; // Mock user ID

const api = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

function pick(obj, keys) {
  for (const k of keys) if (obj && obj[k] !== undefined) return obj[k];
  return undefined;
}

async function run() {
  try {
    console.log('=== PsyApi E2E Simulation ===');
    console.log('Connecting to API at:', BASE_URL);

    // 1) Start session
    console.log('Starting session for national ID:', NationalId);
    const startRes = await api.post('/sessions/start', { NationalId: NationalId });
    console.log('Start session response:', startRes.data);

    const sessionId = pick(startRes.data, ['sessionId', 'id', 'session_id']);
    if (!sessionId) {
      throw new Error('Could not extract session ID from response');
    }
    console.log('Session started:', sessionId);

    // Helper to get next question
    async function getNext() {
      console.log('Getting next question for session:', sessionId);
      const r = await api.get(`/sessions/${sessionId}/next`);
      console.log('Get next question response:', r.data);
      if (r.data && r.data.message === 'completed') return { completed: true };

      const itemId = pick(r.data, ['item_id', 'itemId', 'id']);
      const text = pick(r.data, ['text_ar', 'textAr', 'text']);
      if (!itemId || !text) {
        throw new Error('Could not extract item ID or text from response');
      }
      return { completed: false, itemId, text, type: r.data.type };
    }

    // Q1
    console.log('Getting first question...');
    const q1 = await getNext();
    console.log('Q1:', q1.itemId, q1.text);
    console.log('Submitting answer for Q1...');
    await api.post(`/sessions/${sessionId}/answer`, {
      ItemId: q1.itemId.toString(),
      Answer: 'أوافق',
    });

    // Q2
    console.log('Getting second question...');
    const q2 = await getNext();
    console.log('Q2:', q2.itemId, q2.text);
    console.log('Submitting answer for Q2...');
    await api.post(`/sessions/${sessionId}/answer`, {
      ItemId: q2.itemId.toString(),
      Answer: 'الإنجاز',
    });

    // Submit test
    console.log('Submitting test...');
    const submitRes = await api.post(`/sessions/${sessionId}/submit`);
    console.log('Submit test response:', submitRes.data);

    const submitStatus = pick(submitRes.data, ['message', 'status']);
    console.log('\n=== Final Summary ===');
    console.log(`User: ${NationalId}`);
    console.log(`- SessionId: ${sessionId}`);
    console.log(`- First Question: ${q1.itemId} ${q1.text}`);
    console.log(`- Answer1: "موافق"`);
    console.log(`- Second Question: ${q2.itemId} ${q2.text}`);
    console.log(`- Answer2: "غير موافق"`);
    console.log(`- Submit Status: ${submitStatus}`);
  } catch (err) {
    console.error('Simulation failed:');
    if (err.response) {
      console.error('Status:', err.response.status);
      console.error('Data:', err.response.data);
      console.error('Headers:', err.response.headers);
    } else if (err.request) {
      console.error('Request error:', err.request);
    } else {
      console.error('Error message:', err.message);
    }
    console.error('Stack:', err.stack);
    process.exit(1);
  }
}

run();
