// tools/run-test.js
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
    console.log('=== Starting Test Session ===');

    // 1. Start session
    console.log('Starting session...');
    const sessionResponse = await api.post('/sessions/start', {
      NationalId: NationalId
    });
    const sessionId = sessionResponse.data.sessionId;
    console.log('Session started:', sessionId);

    // 2. Get questions and submit answers
    for (let i = 0; i < 80; i++) {
      console.log(`\nQuestion ${i + 1}`);
      
      // Get next question
      const questionResponse = await api.get(`/sessions/${sessionId}/next`);
      const question = questionResponse.data;
      
      console.log('Raw response:', JSON.stringify(question, null, 2));
      
      if (question.message === 'completed') {
        console.log('All questions completed!');
        break;
      }

      console.log('Question:', question.text_ar);
      console.log('Type:', question.type);

      // Prepare answer based on question type
      let answer;
      switch (question.type) {
        case 'MCQ':
          const options = question.options.split('|');
          answer = options[0]; // Always choose first option
          break;
        case 'Text':
          answer = 'إجابة نصية للاختبار.';
          break;
        case 'LikertAgreement':
          answer = 'أوافق'; // Always agree
          break;
        case 'Frequency':
          answer = 'أحياناً'; // Sometimes
          break;
        case 'TIMED_NUMERIC':
          answer = '42';
          break;
        case 'ORDERING':
          const choices = question.orderingChoices.split('|');
          answer = choices.join('|'); // Keep original order
          break;
        default:
          throw new Error(`Unknown question type: ${question.type}`);
      }

      // Submit answer
      console.log('Submitting answer:', answer);
      await api.post(`/sessions/${sessionId}/answer`, {
        item_id: question.item_id,
        answer: answer
      });
      console.log('Answer accepted');
    }

    // 3. Submit test
    console.log('\nSubmitting test...');
    const submitResponse = await api.post(`/sessions/${sessionId}/submit`);
    console.log('Test submitted:', submitResponse.data);

    // 4. Admin flow - login and get PDF
    console.log('\nLogging in as admin...');
    const loginResponse = await api.post('/admin/login', {
      username: 'root',
      password: 'StrongAdmin!23!'
    });
    const token = loginResponse.data.token;
    api.defaults.headers.Authorization = `Bearer ${token}`;

    // Get session user's result
    console.log('\nGetting results...');
    const resultsResponse = await api.get('/admin/results');
    const results = resultsResponse.data.data;
    const userResult = results.find(r => r.sessionId === sessionId);

    if (userResult) {
      console.log('Getting PDF report...');
      const pdfResponse = await api.get(`/admin/results/${userResult.resultId}/pdf`, {
        responseType: 'stream'
      });

      // Save the PDF
      const fs = require('fs');
      const writer = fs.createWriteStream('test_report.pdf');
      pdfResponse.data.pipe(writer);

      return new Promise((resolve, reject) => {
        writer.on('finish', resolve);
        writer.on('error', reject);
      });
    } else {
      console.log('No result found for session', sessionId);
    }

  } catch (err) {
    console.error('Error:', err.response?.data || err.message);
    if (err.response) {
      console.error('Status:', err.response.status);
      console.error('Headers:', err.response.headers);
    }
    process.exit(1);
  }
}

run();