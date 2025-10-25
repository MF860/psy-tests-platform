/**
 * Validate SDJ CSV for completeness and correctness
 * Run with: node tools/validate_sdj_csv.js
 */

const fs = require('fs');
const path = require('path');

const CSV_PATH = path.join(__dirname, '..', 'seed', 'questions_sdj_ar.csv');

const REQUIRED_FIELDS = [
    'item_code',
    'text_ar',
    'type',
    'dimension',
    'sub_dimension',
    'anchors_ar',
    'reverse',
    'time_limit_seconds',
    'max_score',
    'difficulty'
];

const EXPECTED_ANCHORS = 'لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة';

const SDJ_DIMENSIONS = {
    'التميز الذاتي': ['الوعي الذاتي', 'الثقة بالنفس', 'التنظيم الذاتي', 'التعلم المستمر', 'المرونة النفسية'],
    'التواصل والعلاقات': ['الذكاء العاطفي', 'التواصل الفعال', 'التعاون', 'حل النزاعات', 'بناء العلاقات'],
    'النجاح المهني': ['القيادة', 'حل المشكلات', 'الإبداع والابتكار', 'إدارة الوقت', 'التخطيط الاستراتيجي'],
    'المسؤولية الاجتماعية': ['الوعي المجتمعي', 'الأخلاق المهنية', 'الاستدامة', 'العمل التطوعي', 'المواطنة الفاعلة'],
    'الصحة والتوازن': ['الصحة النفسية', 'الصحة الجسدية', 'إدارة الضغوط', 'التوازن بين العمل والحياة', 'الرفاهية الشاملة']
};

function parseCSV(content) {
    const lines = content.trim().split('\n');
    const headers = lines[0].split(',').map(h => h.trim());
    
    const rows = [];
    for (let i = 1; i < lines.length; i++) {
        const values = lines[i].split(',').map(v => v.trim());
        const row = {};
        headers.forEach((h, idx) => {
            row[h] = values[idx] || '';
        });
        rows.push(row);
    }
    
    return { headers, rows };
}

function validateCSV() {
    console.log('🔍 Validating SDJ CSV...\n');
    
    if (!fs.existsSync(CSV_PATH)) {
        console.error(`❌ CSV file not found: ${CSV_PATH}`);
        process.exit(1);
    }
    
    const content = fs.readFileSync(CSV_PATH, 'utf-8');
    const { headers, rows } = parseCSV(content);
    
    // Check headers
    console.log('📋 Checking headers...');
    const missingHeaders = REQUIRED_FIELDS.filter(f => !headers.includes(f));
    if (missingHeaders.length > 0) {
        console.error(`❌ Missing headers: ${missingHeaders.join(', ')}`);
        process.exit(1);
    }
    console.log('✅ All required headers present\n');
    
    // Validate rows
    console.log('📊 Validating rows...');
    const errors = [];
    const warnings = [];
    const itemCodes = new Set();
    const dimensionCounts = {};
    const subDimensionCounts = {};
    let reverseCount = 0;
    
    rows.forEach((row, idx) => {
        const lineNum = idx + 2; // +2 for header + 0-indexing
        
        // Check for empty required fields
        REQUIRED_FIELDS.forEach(field => {
            if (!row[field] || row[field] === '') {
                errors.push(`Line ${lineNum}: Missing ${field}`);
            }
        });
        
        // Check item_code format (I001, I002, etc.)
        if (row.item_code && !/^I\d{3}$/.test(row.item_code)) {
            errors.push(`Line ${lineNum}: Invalid item_code format '${row.item_code}' (expected I001-I999)`);
        }
        
        // Check for duplicate item_code
        if (itemCodes.has(row.item_code)) {
            errors.push(`Line ${lineNum}: Duplicate item_code '${row.item_code}'`);
        }
        itemCodes.add(row.item_code);
        
        // Check type (should be LikertAgreement for SDJ)
        if (row.type !== 'LikertAgreement') {
            warnings.push(`Line ${lineNum}: Type '${row.type}' is not LikertAgreement`);
        }
        
        // Check dimension/sub-dimension validity
        if (row.dimension && !SDJ_DIMENSIONS[row.dimension]) {
            errors.push(`Line ${lineNum}: Unknown dimension '${row.dimension}'`);
        } else if (row.dimension && row.sub_dimension) {
            const validSubs = SDJ_DIMENSIONS[row.dimension] || [];
            if (!validSubs.includes(row.sub_dimension)) {
                errors.push(`Line ${lineNum}: Unknown sub-dimension '${row.sub_dimension}' for dimension '${row.dimension}'`);
            }
        }
        
        // Check anchors (should match expected Likert scale)
        if (row.anchors_ar !== EXPECTED_ANCHORS) {
            warnings.push(`Line ${lineNum}: Anchors don't match standard Likert scale`);
        }
        
        // Check reverse flag (0 or 1)
        if (row.reverse !== '0' && row.reverse !== '1') {
            errors.push(`Line ${lineNum}: Invalid reverse value '${row.reverse}' (expected 0 or 1)`);
        }
        if (row.reverse === '1') {
            reverseCount++;
        }
        
        // Check time_limit_seconds (should be 20-120)
        const timeLimit = parseInt(row.time_limit_seconds);
        if (isNaN(timeLimit) || timeLimit < 20 || timeLimit > 120) {
            warnings.push(`Line ${lineNum}: time_limit_seconds '${row.time_limit_seconds}' outside recommended range (20-120)`);
        }
        
        // Check max_score (should be 5 for Likert)
        if (row.max_score !== '5' && row.type === 'LikertAgreement') {
            warnings.push(`Line ${lineNum}: max_score should be 5 for LikertAgreement`);
        }
        
        // Check difficulty (1-5)
        const difficulty = parseInt(row.difficulty);
        if (isNaN(difficulty) || difficulty < 1 || difficulty > 5) {
            errors.push(`Line ${lineNum}: Invalid difficulty '${row.difficulty}' (expected 1-5)`);
        }
        
        // Count by dimension and sub-dimension
        if (row.dimension) {
            dimensionCounts[row.dimension] = (dimensionCounts[row.dimension] || 0) + 1;
        }
        if (row.sub_dimension) {
            subDimensionCounts[row.sub_dimension] = (subDimensionCounts[row.sub_dimension] || 0) + 1;
        }
    });
    
    console.log(`  Total items: ${rows.length}`);
    console.log(`  Reverse-scored items: ${reverseCount}`);
    console.log('');
    
    // Report errors
    if (errors.length > 0) {
        console.error(`❌ Found ${errors.length} error(s):\n`);
        errors.slice(0, 10).forEach(e => console.error(`  - ${e}`));
        if (errors.length > 10) {
            console.error(`  ... and ${errors.length - 10} more errors`);
        }
        console.log('');
    }
    
    // Report warnings
    if (warnings.length > 0) {
        console.warn(`⚠️  Found ${warnings.length} warning(s):\n`);
        warnings.slice(0, 10).forEach(w => console.warn(`  - ${w}`));
        if (warnings.length > 10) {
            console.warn(`  ... and ${warnings.length - 10} more warnings`);
        }
        console.log('');
    }
    
    // Dimension distribution
    console.log('📊 Dimension Distribution:\n');
    Object.entries(dimensionCounts)
        .sort((a, b) => b[1] - a[1])
        .forEach(([dim, count]) => {
            console.log(`  ${dim}: ${count} items`);
        });
    console.log('');
    
    console.log('📊 Sub-Dimension Distribution:\n');
    Object.entries(subDimensionCounts)
        .sort((a, b) => b[1] - a[1])
        .forEach(([subDim, count]) => {
            const minItems = 3; // Minimum recommended items per sub-dimension
            const status = count >= minItems ? '✅' : '⚠️ ';
            console.log(`  ${status} ${subDim}: ${count} items`);
        });
    console.log('');
    
    // Final verdict
    if (errors.length === 0) {
        console.log('✅ CSV validation passed!');
        if (warnings.length > 0) {
            console.log(`   (with ${warnings.length} warnings)`);
        }
        process.exit(0);
    } else {
        console.error('❌ CSV validation failed!');
        process.exit(1);
    }
}

validateCSV();
