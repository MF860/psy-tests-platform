// tools/run-test3.js
const axios = require('axios');
const BASE_URL = 'http://localhost:5019/api';
const NationalId = '1000000001'; // Mock user ID

const api = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function adminLogin() {
  const response = await api.post('/admin/login', {
    username: 'root',
    password: 'StrongAdmin!23!'
  });
  const token = response.data.token;
  return token;
}

async function run() {
  try {
    console.log('=== Starting Test Session ===');

    // Start session
    console.log('User login...');
    const sessionResponse = await api.post('/public/user-login', {
      nationalId: NationalId
    });

    // Create a new session if needed
    const sessionId = sessionResponse.data.sessionId;
    console.log('Session started:', sessionId);

    // 2. Get questions and submit answers
    for (let i = 0; i < 80; i++) {
      console.log(`\nQuestion ${i + 1}`);
      
      await delay(500); // Wait between questions
      const questionResponse = await api.get(`/sessions/${sessionId}/next`);
      const question = questionResponse.data;
      
      console.log('Raw question:', JSON.stringify(question, null, 2));
      
      if (question.message === 'completed') {
        console.log('All questions completed!');
        break;
      }

      console.log('Question:', question.text_ar);
      console.log('Type:', question.type);
      
      if (question.options) console.log('Options:', question.options);
      if (question.orderingChoices) console.log('Ordering choices:', question.orderingChoices);
      if (question.orderingLabels) console.log('Ordering labels:', question.orderingLabels);

      // Prepare answer based on question type
      let answer;
      switch (question.type) {
        case 'MCQ':
          const mcqOptions = question.options.split('|');
          answer = mcqOptions[0]; // Always choose first option
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
          answer = 42; // A numeric answer
          break;
        case 'ORDERING':
          answer = question.orderingLabels[0]; // Take first choice label (A)
          break;
        default:
          throw new Error(`Unknown question type: ${question.type}`);
      }

      // Submit answer
      console.log('Submitting answer:', answer);
      await delay(500); // Wait between answers
      try {
        await api.post(`/sessions/${sessionId}/answers`, {
          item_id: question.item_id,
          answer: answer
        });
        console.log('Answer accepted');
      } catch (err) {
        console.error('Failed to submit answer:', err.response?.data);
        if (question.type === 'TIMED_NUMERIC') {
          console.log('Retrying with numeric string...');
          await delay(500);
          await api.post(`/sessions/${sessionId}/answers`, {
            item_id: question.item_id,
            answer: "42"
          });
          console.log('Answer accepted');
        } else {
          throw err;
        }
      }
    }

    // 3. Submit test
    console.log('\nSubmitting test...');
    await delay(1000);
    const submitResponse = await api.post(`/sessions/${sessionId}/submit`);
    console.log('Test submitted:', submitResponse.data);

    // 4. Get PDF report
    console.log('\nGetting admin token...');
    await delay(1000);
    adminToken = await adminLogin();
    api.defaults.headers.Authorization = `Bearer ${adminToken}`;

    // Get session user's result
    console.log('\nGetting results...');
    await delay(1000);
    const resultsResponse = await api.get('/admin/results');
    const results = resultsResponse.data.data;
    const userResult = results.find(r => r.sessionId === sessionId);

    if (userResult) {
      console.log('Getting PDF report...');
      await delay(1000);
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