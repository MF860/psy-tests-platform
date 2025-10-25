// tools/test-ordering-validation.js
// Quick test script to validate the new ORDERING validation logic

const axios = require('axios');

const BASE_URL = 'http://localhost:5019/api';
const api = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' }
});

async function testOrderingValidation() {
  console.log('🧪 Testing ORDERING validation logic...\n');
  
  try {
    // Start a test session
    console.log('1️⃣ Starting test session...');
    const startResponse = await api.post('/sessions/start', {
      NationalId: '1000000002'
    });
    const sessionId = startResponse.data.sessionId;
    console.log(`   ✅ Session started: ${sessionId}\n`);

    // Get first few questions to find an ORDERING question
    console.log('2️⃣ Finding ORDERING question...');
    let orderingQuestion = null;
    
    for (let i = 0; i < 10; i++) {
      const questionResponse = await api.get(`/sessions/${sessionId}/next`);
      const question = questionResponse.data;
      
      if (question.message === 'completed') {
        break;
      }
      
      console.log(`   Question ${i+1}: ${question.item_id} (${question.type})`);
      
      if (question.type === 'ORDERING') {
        orderingQuestion = question;
        console.log(`   ✅ Found ORDERING question: ${question.item_id}\n`);
        break;
      } else {
        // Submit a dummy answer to continue
        await api.post(`/sessions/${sessionId}/answer`, {
          itemId: question.item_id,
          answer: question.type === 'MCQ' ? question.options.split('|')[0] : 'test'
        });
      }
    }

    if (!orderingQuestion) {
      console.log('   ❌ No ORDERING question found in first 10 questions');
      return;
    }

    // Test various ORDERING validation scenarios
    console.log('3️⃣ Testing ORDERING validation scenarios...\n');
    
    const testCases = [
      {
        name: 'Valid label A',
        answer: 'A',
        expectSuccess: true
      },
      {
        name: 'Valid label B',
        answer: 'B',
        expectSuccess: true
      },
      {
        name: 'Valid numeric label 1',
        answer: '1',
        expectSuccess: true
      },
      {
        name: 'Valid numeric label 2',
        answer: '2',
        expectSuccess: true
      },
      {
        name: 'Invalid label E',
        answer: 'E',
        expectSuccess: false,
        expectedError: 'ORDERING_BAD_TOKEN'
      },
      {
        name: 'Invalid label 5',
        answer: '5',
        expectSuccess: false,
        expectedError: 'ORDERING_BAD_TOKEN'
      },
      {
        name: 'Empty answer',
        answer: '',
        expectSuccess: false,
        expectedError: 'ORDERING_EMPTY'
      }
    ];

    // Find item count to test complete permutation
    const colonIndex = orderingQuestion.text_ar.indexOf(':');
    let itemCount = 3; // default
    if (colonIndex !== -1) {
      const itemsText = orderingQuestion.text_ar.substring(colonIndex + 1);
      const tokens = itemsText.split('،').filter(t => t.trim().length > 0);
      itemCount = tokens.length;
    }

    // Add direct sequence tests
    testCases.push(
      {
        name: `Complete valid sequence (${itemCount} items)`,
        answer: Array.from({length: itemCount}, (_, i) => i + 1).join('|'),
        expectSuccess: true
      },
      {
        name: 'Incomplete sequence',
        answer: '1|2',
        expectSuccess: false,
        expectedError: 'ORDERING_INCOMPLETE'
      },
      {
        name: 'Invalid permutation (duplicate)',
        answer: '1|1|2|3',
        expectSuccess: false,
        expectedError: 'ORDERING_INVALID_PERM'
      },
      {
        name: 'Invalid permutation (out of range)',
        answer: Array.from({length: itemCount}, () => '9').join('|'),
        expectSuccess: false,
        expectedError: 'ORDERING_INVALID_PERM'
      },
      {
        name: 'Invalid format (non-numeric)',
        answer: 'a|b|c',
        expectSuccess: false,
        expectedError: 'ORDERING_INVALID_FORMAT'
      }
    );

    let passed = 0;
    let failed = 0;

    for (const testCase of testCases) {
      try {
        console.log(`   🔍 Testing: ${testCase.name}`);
        console.log(`      Input: "${testCase.answer}"`);
        
        const response = await api.post(`/sessions/${sessionId}/answer`, {
          itemId: orderingQuestion.item_id,
          answer: testCase.answer
        });
        
        if (testCase.expectSuccess) {
          console.log(`      ✅ PASS - Answer accepted`);
          passed++;
        } else {
          console.log(`      ❌ FAIL - Expected rejection but answer was accepted`);
          failed++;
        }
      } catch (error) {
        const errorData = error.response?.data;
        if (!testCase.expectSuccess) {
          if (testCase.expectedError && errorData?.code === testCase.expectedError) {
            console.log(`      ✅ PASS - Correctly rejected with code: ${errorData.code}`);
            passed++;
          } else {
            console.log(`      ❌ FAIL - Wrong error code. Expected: ${testCase.expectedError}, Got: ${errorData?.code}`);
            console.log(`      Error: ${errorData?.error}`);
            failed++;
          }
        } else {
          console.log(`      ❌ FAIL - Unexpected rejection: ${errorData?.error}`);
          failed++;
        }
      }
      console.log();
    }

    console.log('='.repeat(50));
    console.log(`📊 VALIDATION RESULTS`);
    console.log(`✅ Passed: ${passed}/${testCases.length}`);
    console.log(`❌ Failed: ${failed}/${testCases.length}`);
    console.log(`Success Rate: ${((passed / testCases.length) * 100).toFixed(1)}%`);
    
    if (failed === 0) {
      console.log('🎉 ALL VALIDATION TESTS PASSED!');
    } else {
      console.log('⚠️  Some validation tests failed.');
    }

  } catch (error) {
    console.error('❌ Test failed:', error.response?.data || error.message);
  }
}

// Run if called directly
if (require.main === module) {
  testOrderingValidation()
    .then(() => process.exit(0))
    .catch(error => {
      console.error('❌ Fatal error:', error.message);
      process.exit(1);
    });
}

module.exports = { testOrderingValidation };