// tools/run-test4.js
const axios = require('axios');
const BASE_URL = 'http://localhost:5019/api';

// Test scenarios
const SCENARIOS = {
  CORRECT: 'correct',
  INCORRECT: 'incorrect'
};

// Command line argument to select scenario
const scenario = process.argv[2] || SCENARIOS.CORRECT;
if (!Object.values(SCENARIOS).includes(scenario)) {
  console.error('Invalid scenario. Use: correct or incorrect');
  process.exit(1);
}

// Use different National IDs for different scenarios
const NationalIds = {
  [SCENARIOS.CORRECT]: '1000000004',
  [SCENARIOS.INCORRECT]: '1000000005'
};

const NationalId = NationalIds[scenario];

const api = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

// Helper function to calculate numeric answers
function calculateAnswer(question, nums) {
  // Speed and distance calculations
  if (question.text_ar.includes('كم/ساعة') || question.text_ar.includes('سرعة')) {
    const speed = nums[0];
    let hours = question.text_ar.includes('ساعتين') ? 2 : 
                (question.text_ar.match(/(\d+)\s*ساع/) || [0, 1])[1];
    return (speed * hours).toString();
  }
  // Investment calculations
  else if (question.text_ar.includes('استثمرت') || question.text_ar.includes('نسبة') || question.text_ar.includes('معدل')) {
    const principal = nums[0];
    let rate = nums[1] / 100;
    let years = question.text_ar.includes('عامين') ? 2 : 
                question.text_ar.includes('سنوات') ? nums[2] : 1;
    
    if (question.text_ar.includes('ربع سنوياً')) {
      rate = rate * 4;
    }
    return Math.round(principal * Math.pow(1 + rate, years)).toString();
  }
  // Simple arithmetic
  else if (question.text_ar.includes('أكلت') || question.text_ar.includes('استخدمت') || 
            question.text_ar.includes('تبقى') || question.text_ar.includes('أنفقت')) {
    return (nums[0] - nums[1]).toString();
  }
  // Multiplication
  else if (question.text_ar.includes('صناديق') || question.text_ar.includes('كتاباً') || 
            question.text_ar.includes('صفحة') || question.text_ar.includes('يومياً')) {
    return (nums[0] * nums[1]).toString();
  }
  // Default to first number
  return nums[0].toString();
}

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

// Helper function to get random delay between min and max
function getRandomDelay(min, max) {
  return Math.floor(Math.random() * (max - min + 1) + min);
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
    console.log('Starting session...');
    const startSessionResponse = await api.post('/sessions/start', {
      NationalId: NationalId
    });
    console.log('Start response:', startSessionResponse.data);
    const sessionId = startSessionResponse.data.sessionId;
    console.log('Session started:', sessionId);

    // 2. Get questions and submit answers
    for (let i = 0; i < 80; i++) {
      console.log(`\nQuestion ${i + 1}`);
      
      // Add significant delay between questions to avoid rate limits
      await delay(getRandomDelay(2000, 4000));
      
      // Add retry logic for rate limits
      let retries = 3;
      let questionResponse;
      while (retries > 0) {
        try {
          questionResponse = await api.get(`/sessions/${sessionId}/next`);
          break;
        } catch (err) {
          if (err.response?.status === 429 && retries > 1) {
            console.log('Rate limited, waiting before retry...');
            await delay(10000); // Wait 10 seconds before retry
            retries--;
            continue;
          }
          throw err;
        }
      }
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

      // Prepare answer based on question type and scenario
      let answer;
      switch (question.type) {
        case 'MCQ':
          const mcqOptions = question.options.split('|');
          if (scenario === SCENARIOS.CORRECT) {
            answer = mcqOptions[0]; // First option is usually correct
          } else {
            answer = mcqOptions[mcqOptions.length - 1]; // Last option for incorrect
          }
          break;
        case 'Text':
          if (scenario === SCENARIOS.CORRECT) {
            // Detailed, contextual answer
            const topic = question.dimension_tags;
            answer = `إجابة تفصيلية ومدروسة تتعلق بموضوع ${topic}. تشمل الإجابة عناصر مهمة وأمثلة واقعية وتحليل عميق للموضوع المطروح.`;
          } else {
            // Very short, low-effort answer
            answer = 'لا أعرف.';
          }
          break;
        case 'LikertAgreement':
          const likertOptions = question.options.split('|');
          if (scenario === SCENARIOS.CORRECT) {
            answer = 'أوافق بشدة'; // Strong positive
          } else {
            answer = 'لا أوافق بشدة'; // Strong negative
          }
          break;
        case 'Frequency':
          const frequencyOptions = question.options.split('|');
          if (scenario === SCENARIOS.CORRECT) {
            answer = 'دائمًا'; // Always
          } else {
            answer = 'أبدًا'; // Never
          }
          break;
        case 'TIMED_NUMERIC':
          let numberMatches = question.text_ar.match(/(\d+)/g);
          let nums = numberMatches?.map(n => parseInt(n)) || [];
          
          // Helper function to extract numbers from text
          const getNumber = (index) => nums[index] || 0;
          
          // Store the itemId for special case handling
          const itemId = question.item_id;
          
          // Known answers for specific questions
          const knownAnswers = {
            'I005': 3,  // 5 تفاحات - 2
            'I010': 120, // 60 كم/ساعة × 2 ساعات
            'I015': 75,  // 100 دينار - 25
            'I021': 30,  // 20 عاماً + 10
            'I026': 15,  // 5 كم/ساعة × 3 ساعات
            'I031': 4,   // 12 دفتر ÷ 3
            'I036': 16,  // 24 تفاحة - ثلثها
            'I040': 125, // 100 + (100 × 0.25)
            'I045': 20,  // (50 - 10) ÷ 2
            'I050': 50,  // 80 - 20 - 10
            'I055': 15,  // 60 - (15 × 3)
            'I060': 121, // 100 × (1.1)²
            'I070': 105, // 100 × (1 + 0.05)⁴ rounded
            'I075': 1260,// 1000 × (1.08)³ rounded
            'I080': 6691,// 5000 × (1.06)⁵ rounded
            'I105': 165, // 200 - 35
            'I110': 300, // 15 × 20
            'I161': 3,   // 5 - 2
            'I162': 4,   // 6 - 2
            'I163': 5,   // 7 - 2
            'I189': 120, // 200 - 80
            'I195': 36,  // 3 × 12
            'I200': 32   // 50 - 18
          };
          
          // If we have a known answer, use it
          if (knownAnswers.hasOwnProperty(itemId)) {
            answer = knownAnswers[itemId].toString();
          }
          // Otherwise, try to calculate based on patterns
          else {
            // Speed and distance calculations
            if (question.text_ar.includes('كم/ساعة') || question.text_ar.includes('سرعة')) {
              const speed = getNumber(0);
              let hours = question.text_ar.includes('ساعتين') ? 2 : 
                         (question.text_ar.match(/(\d+)\s*ساع/) || [0, 1])[1];
              answer = (speed * hours).toString();
            }
            // Investment calculations
            else if (question.text_ar.includes('استثمرت') || question.text_ar.includes('نسبة') || question.text_ar.includes('معدل')) {
              const principal = getNumber(0);
              let rate = getNumber(1) / 100;
              let years = question.text_ar.includes('عامين') ? 2 : 
                         question.text_ar.includes('سنوات') ? getNumber(2) : 1;
              
              if (question.text_ar.includes('ربع سنوياً')) {
                rate = rate * 4;
              }
              answer = Math.round(principal * Math.pow(1 + rate, years)).toString();
            }
            // Simple arithmetic operations
            else if (question.text_ar.includes('أكلت') || question.text_ar.includes('استخدمت') || 
                     question.text_ar.includes('تبقى') || question.text_ar.includes('أنفقت') || 
                     question.text_ar.includes('الباقي') || question.text_ar.includes('المتبقي')) {
              answer = (nums[0] - nums[1]).toString();
            }
            else if (question.text_ar.includes('صناديق') || question.text_ar.includes('كتاباً') || 
                     question.text_ar.includes('صفحة') || question.text_ar.includes('يومياً') ||
                     (question.text_ar.includes('كل') && nums.length >= 2)) {
              answer = (nums[0] * nums[1]).toString();
            }
            // Add support for percentage calculations
            else if (question.text_ar.includes('نسبة') || question.text_ar.includes('%') || 
                     question.text_ar.includes('بالمئة')) {
              const base = nums[0];
              const percentage = nums[1] / 100;
              answer = Math.round(base * (1 + percentage)).toString();
            }
            // Multiple step calculations
            else if (question.text_ar.includes('ثم') || question.text_ar.includes('بعد ذلك')) {
              if (question.text_ar.includes('نصف المتبقي')) {
                answer = (nums[0] - nums[1]) / 2;
              } else {
                // For other multi-step problems, use known answers
                answer = nums[0].toString();
              }
            }
            // Default case: if two numbers and first is larger, assume subtraction
            else if (nums.length >= 2 && nums[0] > nums[1]) {
              answer = (nums[0] - nums[1]).toString();
            }
          }
          break;
        case 'ORDERING':
          if (scenario === SCENARIOS.CORRECT) {
            answer = "1|2|3"; // Correct sequence
          } else {
            answer = "3|2|1"; // Reversed sequence
          }
          break;
        case 'TIMED_NUMERIC':
          let numberMatches = question.text_ar.match(/(\d+)/g);
          let nums = numberMatches?.map(n => parseInt(n)) || [];
          
          // Store the itemId for special case handling
          const itemId = question.item_id;
          
          // Known correct answers for specific questions
          const knownAnswers = {
            'I005': 3,  // 5 تفاحات - 2
            'I010': 120, // 60 كم/ساعة × 2 ساعات
            'I015': 75,  // 100 دينار - 25
            'I021': 30,  // 20 عاماً + 10
            'I026': 15,  // 5 كم/ساعة × 3 ساعات
            'I031': 4,   // 12 دفتر ÷ 3
            'I036': 16,  // 24 تفاحة - ثلثها
            'I040': 125, // 100 + (100 × 0.25)
            'I045': 20,  // (50 - 10) ÷ 2
            'I050': 50,  // 80 - 20 - 10
            'I055': 15,  // 60 - (15 × 3)
            'I060': 121, // 100 × (1.1)²
            'I070': 105, // 100 × (1 + 0.05)⁴ rounded
            'I075': 1260,// 1000 × (1.08)³ rounded
            'I080': 6691,// 5000 × (1.06)⁵ rounded
            'I105': 165, // 200 - 35
            'I110': 300, // 15 × 20
            'I161': 3,   // 5 - 2
            'I162': 4,   // 6 - 2
            'I163': 5,   // 7 - 2
            'I189': 120, // 200 - 80
            'I195': 36,  // 3 × 12
            'I200': 32   // 50 - 18
          };

          if (scenario === SCENARIOS.CORRECT) {
            // Use exact known answers for correct scenario
            if (knownAnswers.hasOwnProperty(itemId)) {
              answer = knownAnswers[itemId].toString();
            } else {
              // Calculate based on patterns (using existing logic)
              answer = calculateAnswer(question, nums);
            }
          } else {
            // For incorrect scenario, add 100 to the correct answer
            const correctAnswer = knownAnswers[itemId] || calculateAnswer(question, nums);
            answer = (parseInt(correctAnswer) + 100).toString();
          }
          break;
        default:
          throw new Error(`Unknown question type: ${question.type}`);
      }

      // Submit answer
      console.log('Submitting answer:', answer);
      // Add realistic user response delay based on question type
      const minDelay = question.type === 'TIMED_NUMERIC' ? 5000 : 3000;
      const maxDelay = question.type === 'Text' ? 15000 : 8000;
      await delay(getRandomDelay(minDelay, maxDelay));

      try {
        const responseTime = getRandomDelay(2000, Math.min(question.time_limit_seconds * 1000, 15000));
        const payload = {
          itemId: question.item_id,
          answer: answer,
          responseTimeMs: responseTime
        };
        console.log('Submitting payload:', JSON.stringify(payload, null, 2));

        // Add retry logic for rate limits
        let retries = 3;
        while (retries > 0) {
          try {
            await api.post(`/sessions/${sessionId}/answer`, payload);
            break;
          } catch (err) {
            if (err.response?.status === 429 && retries > 1) {
              console.log('Rate limited, waiting before retry...');
              await delay(10000); // Wait 10 seconds before retry
              retries--;
              continue;
            }
            throw err;
          }
        }
        console.log('Answer accepted');
      } catch (err) {
        console.error('Failed to submit answer:', err.response?.data);
        throw err;
      }
    }

    // 3. Submit test
    console.log('\nSubmitting test...');
    await delay(getRandomDelay(5000, 8000)); // Add realistic delay before submitting
    const submitResponse = await api.post(`/sessions/${sessionId}/submit`);
    console.log('Test submitted:', submitResponse.data);

    // 4. Get PDF report
    console.log('\nGetting admin token...');
    await delay(1000);
    const adminToken = await adminLogin();
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