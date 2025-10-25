// tools/run-test5.js
const axios = require('axios');
const BASE_URL = 'http://localhost:5019/api';

// Test scenarios
const SCENARIOS = {
  CORRECT: 'correct',
  MIXED: 'mixed',
  INCORRECT: 'incorrect'
};

// Command line argument to select scenario
const scenario = process.argv[2] || SCENARIOS.CORRECT;
if (!Object.values(SCENARIOS).includes(scenario)) {
  console.error('Invalid scenario. Use: correct, mixed, or incorrect');
  process.exit(1);
}

console.log(`Running test scenario: ${scenario}`);

// Use different National IDs for different scenarios
const NationalIds = {
  [SCENARIOS.CORRECT]: '1000000002',   // Perfect persona
  [SCENARIOS.MIXED]: '1000000005',     // Mixed persona
  [SCENARIOS.INCORRECT]: '1000000008'  // Low persona
};

const NationalId = NationalIds[scenario];

const api = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' }
});

// Known correct answers for specific questions
const knownAnswers = {
  'I005': 3,  // 5 تفاحات - 2
  'I010': 120, // 60 كم/ساعة  2 ساعات
  'I015': 75,  // 100 دينار - 25
  'I021': 30,  // 20 عاما + 10
  'I026': 15,  // 5 كم/ساعة  3 ساعات
  'I031': 4,   // 12 دفتر  3
  'I036': 16,  // 24 تفاحة - ثلثها
  'I040': 125, // 100 + (100  0.25)
  'I045': 20,  // (50 - 10)  2
  'I050': 50,  // 80 - 20 - 10
  'I055': 15,  // 60 - (15  3)
  'I060': 121,  // 100  (1.1)
  'I065': 1200, // 50 صندوق  24 عنبر
  'I070': 105,  // 100  (1 + 0.05) rounded
  'I075': 1260, // 1000  (1.08) rounded
  'I080': 6691,// 5000  (1.06) rounded
  'I105': 165, // 200 - 35
  'I110': 300, // 15  20
  'I161': 3,   // 5 - 2
  'I162': 4,   // 6 - 2
  'I163': 5,   // 7 - 2
  'I189': 120, // 200 - 80
  'I195': 36,  // 3  12
  'I200': 32   // 50 - 18
};

// Helper functions
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
    
    if (question.text_ar.includes('ربع سنويا')) {
      rate = rate * 4;
    }
    return Math.round(principal * Math.pow(1 + rate, years)).toString();
  }
  // Simple arithmetic
  else if (question.text_ar.includes('أكلت') || question.text_ar.includes('استخدمت') || 
           question.text_ar.includes('تبقى') || question.text_ar.includes('أنفقت')) {
    return (nums[0] - nums[1]).toString();
  }
  // Multiplication with boxes/containers
  else if (question.text_ar.includes('صندوق') || question.text_ar.includes('صناديق')) {
    // Find both numbers in the question
    const boxes = nums[0];  // Number of boxes
    const itemsPerBox = nums[1];  // Items per box
    return (boxes * itemsPerBox).toString();
  }
  // Other multiplication cases
  else if (question.text_ar.includes('كتابا') || 
           question.text_ar.includes('صفحة') || 
           question.text_ar.includes('يوميا')) {
    return (nums[0] * nums[1]).toString();
  }
  // Default to first number
  return nums[0].toString();
}

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function getRandomDelay(min, max) {
  return Math.floor(Math.random() * (max - min + 1) + min);
}

async function adminLogin() {
  const response = await api.post('/admin/login', {
    username: 'root',
    password: 'StrongAdmin!23!'
  });
  return response.data.token;
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

    // Track stats
    let stats = {
      total: 0,
      correct: 0,
      incorrect: 0,
      byType: {}
    };

    // Get questions and submit answers
    for (let i = 0; i < 80; i++) {
      console.log(`\nQuestion ${i + 1}`);
      await delay(getRandomDelay(100, 200)); // Reduced from 1000-2000 to 100-200
      
      let retries = 3;
      let questionResponse;
      while (retries > 0) {
        try {
          questionResponse = await api.get(`/sessions/${sessionId}/next`);
          break;
        } catch (err) {
          if (err.response?.status === 429 && retries > 1) {
            console.log('Rate limited, waiting before retry...');
            await delay(10000);
            retries--;
            continue;
          }
          throw err;
        }
      }
      const question = questionResponse.data;
      
      if (question.message === 'completed') {
        console.log('All questions completed!');
        break;
      }

      stats.total++;
      stats.byType[question.type] = (stats.byType[question.type] || 0) + 1;

      console.log('Question:', question.text_ar);
      console.log('Type:', question.type);
      
      if (question.options) console.log('Options:', question.options);
      if (question.orderingChoices) console.log('Ordering choices:', question.orderingChoices);
      if (question.orderingLabels) console.log('Ordering labels:', question.orderingLabels);

      // Prepare answer based on question type and scenario
      let answer;
      
      // For MIXED scenario, randomly choose correct or incorrect (50/50)
      const shouldAnswerCorrectly = scenario === SCENARIOS.CORRECT || 
        (scenario === SCENARIOS.MIXED && Math.random() < 0.5);
      
      // Track accuracy for reporting
      if (shouldAnswerCorrectly) {
        stats.correct++;
        console.log('🎯 Answering correctly');
      } else {
        stats.incorrect++;
        console.log('❌ Answering incorrectly');
      }
      
      switch (question.type) {
        case 'MCQ':
          const mcqOptions = question.options.split('|');
          if (shouldAnswerCorrectly) {
            answer = mcqOptions[0]; // First option is usually correct
          } else {
            answer = mcqOptions[mcqOptions.length - 1]; // Last option
          }
          break;

        case 'Text':
          if (shouldAnswerCorrectly) {
            // Detailed, contextual answer based on dimension tags
            const topic = question.dimension_tags;
            answer = `إجابة تفصيلية ومدروسة تتعلق بموضوع ${topic}. تشمل الإجابة شرح مفصل للمفاهيم الأساسية وأمثلة واقعية من الحياة اليومية وتحليل عميق يظهر الفهم الكامل للموضوع. كما تتضمن الإجابة ربط للموضوع بالخبرات الشخصية والمهنية.`;
          } else {
            // Very short, low-effort answer
            answer = 'لا أعرف.';
          }
          break;

        case 'LikertAgreement':
          if (shouldAnswerCorrectly) {
            answer = 'أوافق بشدة'; // Strong positive
          } else {
            answer = 'لا أوافق بشدة'; // Strong negative
          }
          break;

        case 'Frequency':
          const freqOptions = question.options.split('|');
          if (shouldAnswerCorrectly) {
            answer = freqOptions[4]; // دائمًا - Always - last option
          } else {
            answer = freqOptions[0]; // أبدًا - Never - first option
          }
          break;

        case 'ORDERING':
          if (shouldAnswerCorrectly) {
            // Use the correct sequence from orderingChoices[0] (which is always correct)
            if (question.orderingChoices && question.orderingChoices.length > 0) {
              // The correct answer is the first choice (A)
              answer = 'A';
            } else {
              // Fallback: determine item count from question text
              const colonIndex = question.text_ar.indexOf(':');
              if (colonIndex !== -1) {
                const itemsText = question.text_ar.substring(colonIndex + 1);
                const tokens = itemsText.split('،').filter(t => t.trim().length > 0);
                const itemCount = tokens.length;
                // Generate correct sequence 1|2|3|... up to item count
                answer = Array.from({length: itemCount}, (_, i) => i + 1).join('|');
              } else {
                answer = "1|2|3|4"; // Default fallback
              }
            }
          } else {
            // For incorrect answers, choose a random wrong option (B, C, or D)
            if (question.orderingChoices && question.orderingChoices.length > 1) {
              const wrongOptions = ['B', 'C', 'D'];
              const randomIndex = Math.floor(Math.random() * wrongOptions.length);
              answer = wrongOptions[randomIndex];
            } else {
              // Fallback: create an incorrect permutation
              const colonIndex = question.text_ar.indexOf(':');
              if (colonIndex !== -1) {
                const itemsText = question.text_ar.substring(colonIndex + 1);
                const tokens = itemsText.split('،').filter(t => t.trim().length > 0);
                const itemCount = tokens.length;
                // Generate reversed sequence for incorrect answer
                answer = Array.from({length: itemCount}, (_, i) => itemCount - i).join('|');
              } else {
                answer = "4|3|2|1"; // Default fallback
              }
            }
          }
          break;

        case 'TIMED_NUMERIC':
          let numberMatches = question.text_ar.match(/(\d+)/g);
          let nums = numberMatches?.map(n => parseInt(n)) || [];
          
          if (shouldAnswerCorrectly) {
            // Use exact known answers or calculate
            if (knownAnswers.hasOwnProperty(question.item_id)) {
              answer = knownAnswers[question.item_id].toString();
            } else {
              answer = calculateAnswer(question, nums);
            }
          } else {
            // For incorrect scenario, add 100 to correct answer
            const correctAnswer = knownAnswers[question.item_id] || calculateAnswer(question, nums);
            answer = (parseInt(correctAnswer) + 100).toString();
          }
          break;

        default:
          throw new Error(`Unknown question type: ${question.type}`);
      }

      // Submit answer with proper type checking
      console.log('Submitting answer:', answer);
      
      const minDelay = question.type === 'TIMED_NUMERIC' ? 500 : 300;  // Reduced from 5000/3000
      const maxDelay = question.type === 'Text' ? 1000 : 800;      // Reduced from 15000/8000
      await delay(getRandomDelay(minDelay, maxDelay));

      try {
        const responseTime = getRandomDelay(2000, Math.min(question.time_limit_seconds * 1000, 15000));
        const payload = {
          itemId: question.item_id,
          answer: answer,
          responseTimeMs: responseTime
        };
        console.log('Submitting payload:', JSON.stringify(payload, null, 2));

        let retries = 3;
        while (retries > 0) {
          try {
            await api.post(`/sessions/${sessionId}/answer`, payload);
            break;
          } catch (err) {
            if (err.response?.status === 429 && retries > 1) {
              console.log('Rate limited, waiting before retry...');
              await delay(10000);
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

    // Print statistics
    console.log('\n=== Test Statistics ===');
    console.log(`Total questions answered: ${stats.total}`);
    console.log(`Correct answers: ${stats.correct} (${((stats.correct / stats.total) * 100).toFixed(1)}%)`);
    console.log(`Incorrect answers: ${stats.incorrect} (${((stats.incorrect / stats.total) * 100).toFixed(1)}%)`);
    console.log('Questions by type:');
    Object.entries(stats.byType).forEach(([type, count]) => {
      console.log(`  ${type}: ${count}`);
    });

    // Submit test
    console.log('\nSubmitting test...');
    await delay(getRandomDelay(500, 800));  // Reduced from 2000-3000
    const submitResponse = await api.post(`/sessions/${sessionId}/submit`);
    console.log('Test submitted:', submitResponse.data);
    
    // Wait for result processing
    console.log('Waiting for result processing...');
    await delay(3000); // Reduced from 10000
    
    // Get PDF report
    console.log('\nGetting admin token...');
    await delay(300);  // Reduced from 1000
    const adminToken = await adminLogin();
    api.defaults.headers.Authorization = `Bearer ${adminToken}`;

    // Get results with GUID-based lookup and polling
    console.log('\nGetting results...');
    console.log('Session GUID:', sessionId);
    
    let userResult = null;
    const maxPollingAttempts = 10;
    const pollingDelay = 2000; // 2 seconds between attempts
    
    for (let attempt = 1; attempt <= maxPollingAttempts; attempt++) {
      console.log(`Polling attempt ${attempt}/${maxPollingAttempts}...`);
      
      try {
        const resultsResponse = await api.get('/admin/results');
        const results = resultsResponse.data.data;
        
        console.log('Available results:', results.map(r => ({ 
          resultId: r.resultId, 
          sessionId: r.sessionId, 
          sessionGuid: r.sessionGuid,
          nationalId: r.nationalId
        })));
        
        // Look for result by sessionGuid (not sessionId)
        userResult = results.find(r => r.sessionGuid === sessionId);
        
        if (userResult) {
          console.log(`Found result after ${attempt} attempt(s):`, {
            resultId: userResult.resultId,
            sessionGuid: userResult.sessionGuid,
            totalScore: userResult.totalScore
          });
          break;
        } else {
          console.log(`No result found for session GUID: ${sessionId}`);
          if (attempt < maxPollingAttempts) {
            console.log(`Waiting ${pollingDelay}ms before next attempt...`);
            await delay(pollingDelay);
          }
        }
      } catch (error) {
        console.log(`Polling attempt ${attempt} failed:`, error.message);
        if (attempt < maxPollingAttempts) {
          await delay(pollingDelay);
        }
      }
    }
    
    if (userResult) {
      console.log('Getting PDF report...');
      await delay(300);
      const reportName = `test_report_${scenario}_${NationalId}.pdf`;
      console.log(`Downloading PDF report to ${reportName}...`);
      
      try {
        const pdfResponse = await api.get(`/admin/results/${userResult.resultId}/pdf`, {
          responseType: 'arraybuffer'
        });

        // Save the PDF
        const fs = require('fs');
        await fs.promises.writeFile(reportName, pdfResponse.data);

        console.log(`PDF report saved successfully as ${reportName}`);
        console.log(`Final score: ${userResult.totalScore}`);
      } catch (error) {
        console.error('Failed to download PDF:', error.message);
      }
    } else {
      console.log(`Failed to find result for session GUID: ${sessionId} after ${maxPollingAttempts} attempts`);
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
