// tools/run-scenarios.js
const axios = require('axios');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5019/api';
const ADMIN_USER = process.env.ADMIN_USER || 'root';
const ADMIN_PASS = process.env.ADMIN_PASS || 'StrongAdmin!23!';

const api = axios.create({ baseURL: BASE_URL, timeout: 20000, headers: { 'Content-Type': 'application/json' } });

function delay(ms) { return new Promise((res) => setTimeout(res, ms)); }

function pick(obj, keys) { for (const k of keys) if (obj && obj[k] !== undefined) return obj[k]; return undefined; }

async function adminLogin() {
  const r = await api.post('/admin/login', { username: ADMIN_USER, password: ADMIN_PASS });
  const token = r.data?.token;
  if (!token) throw new Error('Admin login failed: no token');
  api.defaults.headers.Authorization = `Bearer ${token}`;
  return token;
}

async function validateQuestions() {
  const r = await api.get('/admin/questions/validate');
  return r.data;
}

async function startSession(nationalId) {
  const r = await api.post('/sessions/start', { NationalId: nationalId });
  const sessionId = pick(r.data, ['sessionId', 'id', 'session_id']);
  if (!sessionId) throw new Error('No sessionId');
  return sessionId;
}

async function getNext(sessionId) {
  const r = await api.get(`/sessions/${sessionId}/next`);
  if (r.data && r.data.message === 'completed') return { completed: true };
  return {
    completed: false,
    id: pick(r.data, ['id', 'itemId']) || 0,
    itemId: pick(r.data, ['item_id', 'itemId']) || '',
    type: r.data.type,
    text: pick(r.data, ['text_ar', 'textAr', 'text']) || '',
    dimensionTags: pick(r.data, ['dimension_tags', 'dimensionTags']) || ''
  };
}

function buildTextFromTags(tags) {
  if (!tags) return 'نص عام';
  // Expand dimension tags into Arabic-looking text to maximize text scoring
  return Array(3).fill(tags.replace(/[,_]/g, ' ')).join(' ').slice(0, 400);
}

function answerFor(scenario, q) {
  const t = (q.type || '').toUpperCase();
  const isLikertish = t === 'LIKERT' || t === 'LIKERTAGREEMENT' || t === 'FREQUENCY';
  if (scenario === 'HIGH') {
    if (isLikertish) return '5';
    if (t === 'TEXT') return buildTextFromTags(q.dimensionTags);
    if (t === 'TIMED_NUMERIC') return '0'; // likely wrong, but focus on Likert/Text
    if (t === 'ORDERING') return 'أ,ب,ج,د'; // non-intersecting to avoid accidental matches
    if (t === 'MCQ') return 'WRONG';
  } else if (scenario === 'LOW') {
    if (isLikertish) return '1';
    if (t === 'TEXT') return 'بدون تفاصيل';
    if (t === 'TIMED_NUMERIC') return '999999';
    if (t === 'ORDERING') return 'x|y|z';
    if (t === 'MCQ') return 'WRONG';
  } else {
    // RANDOM
    if (isLikertish) return String(1 + Math.floor(Math.random() * 5));
    if (t === 'TEXT') return Math.random() > 0.5 ? buildTextFromTags(q.dimensionTags) : 'إجابة عامة';
    if (t === 'TIMED_NUMERIC') return String(Math.floor(Math.random() * 100));
    if (t === 'ORDERING') return ['1','2','3','4'].sort(() => Math.random() - 0.5).join('|');
    if (t === 'MCQ') return 'X';
  }
  return '0';
}

async function answerAll(sessionId, scenario) {
  let answered = 0;
  for (let i = 0; i < 200; i++) {
    const q = await getNext(sessionId);
    if (q.completed) break;
    const ans = answerFor(scenario, q);
    const payload = { ItemId: q.itemId.toString() };
    if ((q.type || '').toUpperCase() === 'TIMED_NUMERIC') {
      if (!isNaN(Number(ans))) payload.NumericAnswer = Number(ans);
      else payload.NumericAnswer = 0;
    } else {
      payload.Answer = ans;
    }
    await api.post(`/sessions/${sessionId}/answer`, payload);
    answered++;
  }
  const r = await api.post(`/sessions/${sessionId}/submit`);
  return { submitted: true, answered, r: r.data };
}

async function testTimedNumericRejection() {
  const nid = `1999${Math.floor(Math.random() * 1000000).toString().padStart(6, '0')}`;
  const sessionId = await startSession(nid);
  // Find a TIMED_NUMERIC question
  for (let i = 0; i < 50; i++) {
    const q = await getNext(sessionId);
    if (q.completed) break;
    if ((q.type || '').toUpperCase() === 'TIMED_NUMERIC') {
      try {
        await api.post(`/sessions/${sessionId}/answer`, { ItemId: q.itemId.toString(), NumericAnswer: 'abc' });
        console.log('Unexpected: TIMED_NUMERIC accepted letters');
      } catch (e) {
        console.log('TIMED_NUMERIC rejection (expected 400):', (e && e.response && e.response.status) || e.message);
      }
      await api.post(`/sessions/${sessionId}/answer`, { ItemId: q.itemId.toString(), NumericAnswer: 120 });
      return { sessionId };
    }
    // otherwise answer quickly to progress
    await api.post(`/sessions/${sessionId}/answer`, { ItemId: q.itemId.toString(), Answer: '1' });
  }
  return { sessionId };
}

async function findResultBySession(sessionId) {
  const page = await api.get('/admin/results', { params: { page: 1, pageSize: 50 } }).then(r => r.data);
  const found = (page.data || []).find(x => x.sessionId === sessionId);
  if (!found) return null;
  const detail = await api.get(`/admin/results/${found.resultId}`).then(r => r.data);
  return detail;
}

async function main() {
  console.log('=== Starting API Scenarios against', BASE_URL, '===');
  console.log('Logging in as admin...');
  await adminLogin();
  console.log('Validating questions dataset...');
  const validation = await validateQuestions();
  console.log('Validator:', JSON.stringify(validation, null, 2));
  console.log('Running TIMED_NUMERIC rejection/acceptance quick test...');
  await testTimedNumericRejection();

  const scenarios = ['HIGH', 'LOW', 'RANDOM'];
  const outputs = {};

  for (const s of scenarios) {
    const nid = `1990${Math.floor(Math.random() * 1000000).toString().padStart(6, '0')}`;
    console.log(`\n--- Scenario ${s} for NationalId=${nid} ---`);
    const sessionId = await startSession(nid);
    const result = await answerAll(sessionId, s);
    console.log(`Submitted session ${sessionId}, answered ${result.answered}`);
    await delay(500);
    const detail = await findResultBySession(sessionId);
    if (!detail) {
      console.log('Result not found for session', sessionId);
      continue;
    }
    outputs[s] = detail;
    const tAvg = detail.dimensions.length ? (detail.dimensions.reduce((a, d) => a + d.t, 0) / detail.dimensions.length) : 0;
    const pAvg = detail.dimensions.length ? (detail.dimensions.reduce((a, d) => a + d.percentile, 0) / detail.dimensions.length) : 0;
    console.log(`Avg T=${tAvg.toFixed(1)}, Avg %ile=${pAvg.toFixed(1)}`);
    console.log('Dimensions sample:', detail.dimensions.slice(0, 5));
  }

  console.log('\n=== Scenario Summaries ===');
  for (const [k, v] of Object.entries(outputs)) {
    const tAvg = v.dimensions.length ? (v.dimensions.reduce((a, d) => a + d.t, 0) / v.dimensions.length) : 0;
    const pAvg = v.dimensions.length ? (v.dimensions.reduce((a, d) => a + d.percentile, 0) / v.dimensions.length) : 0;
    console.log(`- ${k}: AvgT=${tAvg.toFixed(1)} Avg%ile=${pAvg.toFixed(1)} Total=${v.totalScore}`);
  }
}

main().catch((e) => {
  console.error('Simulation error:', e?.response?.data || e.message);
  process.exit(1);
});
