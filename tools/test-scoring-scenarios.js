/**
 * Test scoring scenarios for PERFECT/MIXED/LOW performance patterns
 */

const fs = require('fs');
const axios = require('axios');

// Read questions to understand structure
const csv = fs.readFileSync('../seed/questions_fixed_extended_plus_personality.csv', 'utf8');
const lines = csv.split('\n').filter(line => line.trim());
const questions = lines.slice(1).map(line => {
  const parts = line.split(',');
  return {
    id: parts[0],
    text_ar: parts[1],
    type: parts[2],
    dimension: parts[3],
    difficulty: parseInt(parts[4]) || 1,
    time_limit: parseInt(parts[5]) || 30,
    max_score: parseInt(parts[6]) || 1,
    correct_answer: parts[7],
    options: parts[8]
  };
});

const API_BASE = process.env.API_BASE || 'https://localhost:5001';

// Generate answers based on scenario
function generateAnswers(scenario) {
  const answers = {};
  
  questions.forEach(q => {
    let answer;
    
    switch (q.type) {
      case 'MCQ':
        if (scenario === 'PERFECT') {
          answer = q.correct_answer;
        } else if (scenario === 'MIXED') {
          answer = Math.random() < 0.5 ? q.correct_answer : 'wrong';
        } else { // LOW
          answer = 'wrong';
        }
        break;
        
      case 'TIMED_NUMERIC':
        if (scenario === 'PERFECT') {
          answer = q.correct_answer;
        } else if (scenario === 'MIXED') {
          if (Math.random() < 0.5) {
            answer = q.correct_answer;
          } else {
            const correct = parseFloat(q.correct_answer) || 0;
            answer = (correct + Math.random() * 10).toString();
          }
        } else { // LOW
          const correct = parseFloat(q.correct_answer) || 0;
          answer = (correct + Math.random() * 50 + 10).toString();
        }
        break;
        
      case 'ORDERING':
        if (scenario === 'PERFECT') {
          answer = q.correct_answer;
        } else if (scenario === 'MIXED') {
          const order = q.correct_answer.split('|');
          if (Math.random() < 0.5) {
            answer = q.correct_answer;
          } else {
            // Shuffle partially
            answer = order.sort(() => Math.random() - 0.5).join('|');
          }
        } else { // LOW
          const order = q.correct_answer.split('|');
          answer = order.sort(() => Math.random() - 0.5).join('|');
        }
        break;
        
      case 'LikertAgreement':
      case 'Frequency':
        if (scenario === 'PERFECT') {
          answer = Math.random() < 0.8 ? '4' : '5'; // Mostly high
        } else if (scenario === 'MIXED') {
          answer = (Math.floor(Math.random() * 3) + 2).toString(); // 2-4
        } else { // LOW
          answer = Math.random() < 0.8 ? '1' : '2'; // Mostly low
        }
        break;
        
      case 'Text':
        if (scenario === 'PERFECT') {
          answer = `أجابة مفصلة حول ${q.dimension} تتضمن نقاط مهمة ومعلومات شاملة وأمثلة واضحة توضح الفهم العميق للموضوع والقدرة على التفكير النقدي والتحليل المتقدم`;
        } else if (scenario === 'MIXED') {
          answer = `أجابة متوسطة حول ${q.dimension} تتضمن بعض النقاط المهمة`;
        } else { // LOW
          answer = 'أجابة قصيرة';
        }
        break;
        
      default:
        answer = scenario === 'PERFECT' ? '5' : scenario === 'MIXED' ? '3' : '1';
    }
    
    answers[q.id] = answer;
  });
  
  return answers;
}

// Create session and submit answers
async function testScenario(scenario) {
  console.log(`\n=== Testing ${scenario} scenario ===`);
  
  try {
    // Create session
    const sessionResp = await axios.post(`${API_BASE}/api/sessions`, {
      userId: `test-${scenario.toLowerCase()}`,
      userInfo: {
        name: `Test User ${scenario}`,
        nationalId: `TEST${Date.now()}`,
        additionalInfo: {}
      }
    });
    
    const sessionId = sessionResp.data.sessionId;
    console.log(`Created session: ${sessionId}`);
    
    // Generate answers
    const answers = generateAnswers(scenario);
    
    // Submit answers
    const submitPromises = questions.map(async (q) => {
      const responseTime = scenario === 'PERFECT' ? 
        Math.random() * q.time_limit * 800 : // Fast and accurate
        scenario === 'MIXED' ?
        Math.random() * q.time_limit * 1200 : // Normal time
        Math.random() * q.time_limit * 1500 + q.time_limit * 500; // Slow
        
      return axios.post(`${API_BASE}/api/sessions/${sessionId}/answer`, {
        itemId: q.id,
        answer: answers[q.id],
        responseTimeMs: Math.floor(responseTime)
      });
    });
    
    await Promise.all(submitPromises);
    console.log(`Submitted ${questions.length} answers`);
    
    // Get results
    const resultsResp = await axios.get(`${API_BASE}/api/sessions/${sessionId}/results`);
    const results = resultsResp.data;
    
    console.log(`\n${scenario} RESULTS:`);
    console.log(`Total Score: ${results.totalScore}`);
    console.log(`\nDimension Scores (T-scores):`);
    
    const knowledgeDims = results.dimensionScores
      .filter(ds => ['الحساب الذهني', 'المعرفة العلمية', 'المعرفة الجغرافية', 'المعرفة التاريخية', 'المعرفة العامة', 'المعرفة التكنولوجية', 'المعرفة البيولوجية'].includes(ds.dimension))
      .sort((a, b) => b.t - a.t);
    
    knowledgeDims.forEach(ds => {
      console.log(`${ds.dimension}: T=${ds.t}, Raw=${ds.raw}, Percentile=${ds.percentile}%`);
    });
    
    // Analyze results
    const highT = knowledgeDims.filter(ds => ds.t >= 55).length;
    const totalKnowledge = knowledgeDims.length;
    const highPct = (highT / totalKnowledge) * 100;
    
    console.log(`\nAnalysis: ${highT}/${totalKnowledge} knowledge dimensions with T>=55 (${highPct.toFixed(1)}%)`);
    
    return {
      scenario,
      sessionId,
      results,
      knowledgeDims,
      highTPercentage: highPct
    };
    
  } catch (error) {
    console.error(`Error in ${scenario} scenario:`, error.response?.data || error.message);
    return null;
  }
}

// Main execution
async function main() {
  console.log('Testing scoring scenarios...');
  
  const scenarios = ['PERFECT', 'MIXED', 'LOW'];
  const results = {};
  
  for (const scenario of scenarios) {
    results[scenario] = await testScenario(scenario);
    await new Promise(resolve => setTimeout(resolve, 1000)); // Brief delay
  }
  
  console.log('\n=== SCENARIO COMPARISON ===');
  scenarios.forEach(scenario => {
    if (results[scenario]) {
      const avgT = results[scenario].knowledgeDims.reduce((sum, ds) => sum + ds.t, 0) / results[scenario].knowledgeDims.length;
      console.log(`${scenario}: Avg T-score = ${avgT.toFixed(1)}, High T% = ${results[scenario].highTPercentage.toFixed(1)}%`);
    }
  });
  
  // Validation
  if (results.PERFECT && results.PERFECT.highTPercentage >= 80) {
    console.log('✅ PERFECT scenario passes: ≥80% knowledge dims with T≥55');
  } else {
    console.log('❌ PERFECT scenario fails: <80% knowledge dims with T≥55');
  }
}

if (require.main === module) {
  main().catch(console.error);
}

module.exports = { generateAnswers, testScenario };