// tools/run-all-scenarios.js
// Master test runner for all three personas with PDF generation

const { spawn } = require('child_process');
const path = require('path');

const SCENARIOS = ['correct', 'mixed', 'incorrect'];
const PERSONAS = {
  correct: { nationalId: '1000000002', name: 'Perfect Performer' },
  mixed: { nationalId: '1000000005', name: 'Mixed Performance' },
  incorrect: { nationalId: '1000000008', name: 'Low Performance' }
};

async function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function runScenario(scenario) {
  return new Promise((resolve, reject) => {
    console.log(`\n=== Running ${scenario.toUpperCase()} scenario (${PERSONAS[scenario].name} - ID: ${PERSONAS[scenario].nationalId}) ===`);
    
    const testProcess = spawn('node', ['run-test5.js', scenario], {
      cwd: __dirname,
      stdio: 'inherit'
    });

    testProcess.on('close', (code) => {
      if (code === 0) {
        console.log(`✅ ${scenario.toUpperCase()} scenario completed successfully`);
        resolve();
      } else {
        console.log(`❌ ${scenario.toUpperCase()} scenario failed with code ${code}`);
        reject(new Error(`Scenario ${scenario} failed`));
      }
    });

    testProcess.on('error', (error) => {
      console.error(`❌ Failed to start ${scenario} scenario:`, error.message);
      reject(error);
    });
  });
}

async function runAllScenarios() {
  console.log('🚀 Starting comprehensive testing of all three personas...\n');
  
  const results = {
    completed: [],
    failed: []
  };

  for (const scenario of SCENARIOS) {
    try {
      await runScenario(scenario);
      results.completed.push(scenario);
      
      // Wait between scenarios to allow result processing
      if (scenario !== SCENARIOS[SCENARIOS.length - 1]) {
        console.log('\n⏳ Waiting 5 seconds before next scenario...');
        await delay(5000);
      }
    } catch (error) {
      console.error(`❌ Scenario ${scenario} failed:`, error.message);
      results.failed.push(scenario);
    }
  }

  // Summary
  console.log('\n' + '='.repeat(80));
  console.log('📊 TEST EXECUTION SUMMARY');
  console.log('='.repeat(80));
  
  console.log(`\n✅ Completed scenarios (${results.completed.length}/${SCENARIOS.length}):`);
  results.completed.forEach(scenario => {
    const persona = PERSONAS[scenario];
    console.log(`   • ${scenario.toUpperCase()}: ${persona.name} (${persona.nationalId})`);
  });

  if (results.failed.length > 0) {
    console.log(`\n❌ Failed scenarios (${results.failed.length}/${SCENARIOS.length}):`);
    results.failed.forEach(scenario => {
      const persona = PERSONAS[scenario];
      console.log(`   • ${scenario.toUpperCase()}: ${persona.name} (${persona.nationalId})`);
    });
  }

  console.log('\n📁 Generated PDF files:');
  const fs = require('fs');
  const files = fs.readdirSync(__dirname).filter(f => f.startsWith('test_report_') && f.endsWith('.pdf'));
  files.forEach(file => console.log(`   • ${file}`));

  if (results.completed.length === SCENARIOS.length) {
    console.log('\n🎉 ALL SCENARIOS COMPLETED SUCCESSFULLY!');
    console.log('Ready for validation testing and acceptance verification.');
    return 0;
  } else {
    console.log(`\n⚠️  ${results.failed.length} scenario(s) failed. Check logs above.`);
    return 1;
  }
}

// Run if called directly
if (require.main === module) {
  runAllScenarios()
    .then(exitCode => process.exit(exitCode))
    .catch(error => {
      console.error('❌ Fatal error:', error.message);
      process.exit(1);
    });
}

module.exports = { runAllScenarios };