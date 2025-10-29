/**
 * SDJ V2 CSV Validator
 * Validates questions_sdj_v2_ar.csv against schema requirements
 * Ensures: 210 items, 10 per sub-dimension, 50% reversed, UTF-8 encoding
 */

import * as fs from 'fs';
import * as path from 'path';

// ============================================================================
// TYPES
// ============================================================================

interface Schema {
    version: string;
    main_patterns: Array<{
        id: string;
        key: string;
        sub_dimensions: Array<{
            id: string;
            key: string;
            name_ar: string;
        }>;
    }>;
    items_per_sub_dimension: number;
    reverse_ratio: number;
}

interface CsvRow {
    ItemCode: string;
    TextAr: string;
    PatternId: string;
    PatternKey: string;
    PatternNameAr: string;
    SubId: string;
    SubKey: string;
    SubNameAr: string;
    Type: string;
    Reverse: string;
    TimeLimitSeconds: string;
    Weight: string;
}

interface ValidationResult {
    valid: boolean;
    errors: string[];
    warnings: string[];
    stats: {
        totalItems: number;
        subDimensionCounts: Record<string, number>;
        reverseCounts: Record<string, { direct: number; reverse: number }>;
        patternCounts: Record<string, number>;
    };
}

// ============================================================================
// VALIDATION LOGIC
// ============================================================================

function loadSchema(): Schema {
    const schemaPath = path.join(__dirname, '..', 'backend', 'PsyApi', 'Domain', 'SdjV2SevenPatterns.json');
    const schemaContent = fs.readFileSync(schemaPath, 'utf-8');
    return JSON.parse(schemaContent) as Schema;
}

function loadCsv(csvPath: string): CsvRow[] {
    const content = fs.readFileSync(csvPath, 'utf-8');
    const lines = content.trim().split('\n');

    if (lines.length < 2) {
        throw new Error('CSV file is empty or has no data rows');
    }

    const header = lines[0];
    const expectedHeader = 'ItemCode,TextAr,PatternId,PatternKey,PatternNameAr,SubId,SubKey,SubNameAr,Type,Reverse,TimeLimitSeconds,Weight';

    if (!header.startsWith('ItemCode')) {
        throw new Error(`Invalid CSV header. Expected to start with "ItemCode", got: ${header.substring(0, 50)}`);
    }

    const rows: CsvRow[] = [];
    for (let i = 1; i < lines.length; i++) {
        const line = lines[i].trim();
        if (!line) continue;

        // Simple CSV parsing (assumes no commas in quoted fields for this validator)
        const values = parseCsvLine(line);

        if (values.length < 12) {
            throw new Error(`Row ${i + 1} has insufficient columns: ${values.length}`);
        }

        rows.push({
            ItemCode: values[0],
            TextAr: values[1],
            PatternId: values[2],
            PatternKey: values[3],
            PatternNameAr: values[4],
            SubId: values[5],
            SubKey: values[6],
            SubNameAr: values[7],
            Type: values[8],
            Reverse: values[9],
            TimeLimitSeconds: values[10],
            Weight: values[11]
        });
    }

    return rows;
}

function parseCsvLine(line: string): string[] {
    const values: string[] = [];
    let current = '';
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
        const char = line[i];

        if (char === '"') {
            if (inQuotes && line[i + 1] === '"') {
                // Escaped quote
                current += '"';
                i++;
            } else {
                // Toggle quotes
                inQuotes = !inQuotes;
            }
        } else if (char === ',' && !inQuotes) {
            values.push(current);
            current = '';
        } else {
            current += char;
        }
    }
    values.push(current); // Last value

    return values.map(v => v.trim());
}

function validateCsv(csvPath: string): ValidationResult {
    const result: ValidationResult = {
        valid: true,
        errors: [],
        warnings: [],
        stats: {
            totalItems: 0,
            subDimensionCounts: {},
            reverseCounts: {},
            patternCounts: {}
        }
    };

    console.log('🔍 Validating SDJ V2 CSV...\n');

    try {
        // Load schema and CSV
        const schema = loadSchema();
        const rows = loadCsv(csvPath);

        result.stats.totalItems = rows.length;

        // Count items by sub-dimension and pattern
        for (const row of rows) {
            // Sub-dimension count
            if (!result.stats.subDimensionCounts[row.SubId]) {
                result.stats.subDimensionCounts[row.SubId] = 0;
            }
            result.stats.subDimensionCounts[row.SubId]++;

            // Pattern count
            if (!result.stats.patternCounts[row.PatternId]) {
                result.stats.patternCounts[row.PatternId] = 0;
            }
            result.stats.patternCounts[row.PatternId]++;

            // Reverse count
            if (!result.stats.reverseCounts[row.SubId]) {
                result.stats.reverseCounts[row.SubId] = { direct: 0, reverse: 0 };
            }
            if (row.Reverse === '1') {
                result.stats.reverseCounts[row.SubId].reverse++;
            } else {
                result.stats.reverseCounts[row.SubId].direct++;
            }
        }

        // Validation 1: Total item count (210)
        const expectedTotal = schema.items_per_sub_dimension * 
            schema.main_patterns.reduce((sum, p) => sum + p.sub_dimensions.length, 0);

        if (result.stats.totalItems !== expectedTotal) {
            result.errors.push(`Total items: Expected ${expectedTotal}, got ${result.stats.totalItems}`);
        } else {
            console.log(`✅ Total items: ${result.stats.totalItems}`);
        }

        // Validation 2: Exactly 10 items per sub-dimension
        console.log('\n📊 Sub-dimension item counts:');
        for (const pattern of schema.main_patterns) {
            console.log(`\n${pattern.id}:`);
            for (const subDim of pattern.sub_dimensions) {
                const count = result.stats.subDimensionCounts[subDim.id] || 0;
                const icon = count === schema.items_per_sub_dimension ? '✅' : '❌';
                console.log(`   ${icon} ${subDim.id} (${subDim.name_ar}): ${count} items`);

                if (count !== schema.items_per_sub_dimension) {
                    result.errors.push(`${subDim.id}: Expected ${schema.items_per_sub_dimension} items, got ${count}`);
                }
            }
        }

        // Validation 3: Exactly 50% reversed per sub-dimension
        console.log('\n🔄 Reverse scoring distribution:');
        for (const pattern of schema.main_patterns) {
            for (const subDim of pattern.sub_dimensions) {
                const counts = result.stats.reverseCounts[subDim.id];
                if (!counts) {
                    result.errors.push(`${subDim.id}: No items found`);
                    continue;
                }

                const reverseRatio = counts.reverse / (counts.direct + counts.reverse);
                const icon = Math.abs(reverseRatio - schema.reverse_ratio) < 0.01 ? '✅' : '❌';
                console.log(`   ${icon} ${subDim.id}: ${counts.direct} direct, ${counts.reverse} reverse (${(reverseRatio * 100).toFixed(0)}%)`);

                if (Math.abs(reverseRatio - schema.reverse_ratio) > 0.01) {
                    result.errors.push(`${subDim.id}: Expected 50% reversed, got ${(reverseRatio * 100).toFixed(0)}%`);
                }
            }
        }

        // Validation 4: UTF-8 encoding check (Arabic characters)
        console.log('\n📝 UTF-8 encoding check:');
        let arabicCount = 0;
        let invalidGlyphs = 0;

        for (const row of rows) {
            // Check for Arabic characters (Unicode range 0x0600-0x06FF)
            const hasArabic = /[\u0600-\u06FF]/.test(row.TextAr);
            if (hasArabic) arabicCount++;

            // Check for invalid glyphs (replacement character �)
            if (row.TextAr.includes('�') || row.PatternNameAr.includes('�') || row.SubNameAr.includes('�')) {
                invalidGlyphs++;
                result.errors.push(`${row.ItemCode}: Contains invalid glyphs (�)`);
            }
        }

        console.log(`   ✅ Arabic text items: ${arabicCount}/${result.stats.totalItems}`);
        if (invalidGlyphs > 0) {
            console.log(`   ❌ Invalid glyphs found: ${invalidGlyphs}`);
        } else {
            console.log(`   ✅ No invalid glyphs detected`);
        }

        // Validation 5: Type consistency (MCQ and LikertAgreement allowed)
        console.log('\n📋 Item type check:');
        const typeSet = new Set(rows.map(r => r.Type));
        const validTypes = ['MCQ', 'LikertAgreement'];
        const invalidTypes = Array.from(typeSet).filter(t => !validTypes.includes(t));
        if (invalidTypes.length === 0) {
            const mcqCount = rows.filter(r => r.Type === 'MCQ').length;
            const likertCount = rows.filter(r => r.Type === 'LikertAgreement').length;
            console.log(`   ✅ Valid types: ${mcqCount} MCQ, ${likertCount} Likert`);
        } else {
            console.log(`   ❌ Invalid types found: ${invalidTypes.join(', ')}`);
            result.errors.push(`Invalid types found: ${invalidTypes.join(', ')}, only MCQ and LikertAgreement allowed`);
        }

        // Validation 6: Item code uniqueness
        console.log('\n🔢 Item code uniqueness:');
        const itemCodes = new Set<string>();
        const duplicates: string[] = [];
        for (const row of rows) {
            if (itemCodes.has(row.ItemCode)) {
                duplicates.push(row.ItemCode);
            }
            itemCodes.add(row.ItemCode);
        }
        if (duplicates.length === 0) {
            console.log(`   ✅ All item codes unique`);
        } else {
            console.log(`   ❌ Duplicate item codes: ${duplicates.join(', ')}`);
            result.errors.push(`Duplicate item codes found: ${duplicates.join(', ')}`);
        }

        // Validation 7: Item code sequence (I001-I210)
        console.log('\n🔢 Item code sequence:');
        const expectedCodes = Array.from({ length: expectedTotal }, (_, i) => `I${(i + 1).toString().padStart(3, '0')}`);
        const actualCodes = rows.map(r => r.ItemCode).sort();
        const missingCodes = expectedCodes.filter(code => !actualCodes.includes(code));
        const extraCodes = actualCodes.filter(code => !expectedCodes.includes(code));

        if (missingCodes.length === 0 && extraCodes.length === 0) {
            console.log(`   ✅ Item codes I001-I${expectedTotal.toString().padStart(3, '0')} complete`);
        } else {
            if (missingCodes.length > 0) {
                console.log(`   ❌ Missing codes: ${missingCodes.slice(0, 5).join(', ')}${missingCodes.length > 5 ? '...' : ''}`);
                result.errors.push(`Missing ${missingCodes.length} item codes`);
            }
            if (extraCodes.length > 0) {
                console.log(`   ❌ Extra codes: ${extraCodes.slice(0, 5).join(', ')}${extraCodes.length > 5 ? '...' : ''}`);
                result.errors.push(`Found ${extraCodes.length} unexpected item codes`);
            }
        }

        // Set valid flag
        result.valid = result.errors.length === 0;

    } catch (error) {
        result.valid = false;
        result.errors.push(`Fatal error: ${(error as Error).message}`);
    }

    return result;
}

function printSummary(result: ValidationResult): void {
    console.log('\n' + '='.repeat(70));
    console.log('📋 VALIDATION SUMMARY');
    console.log('='.repeat(70));

    if (result.valid) {
        console.log('✅ STATUS: PASSED');
        console.log('\n🎉 CSV is valid and ready for use!');
    } else {
        console.log('❌ STATUS: FAILED');
        console.log(`\n🚨 Found ${result.errors.length} error(s):`);
        result.errors.forEach((err, i) => console.log(`   ${i + 1}. ${err}`));
    }

    if (result.warnings.length > 0) {
        console.log(`\n⚠️  ${result.warnings.length} warning(s):`);
        result.warnings.forEach((warn, i) => console.log(`   ${i + 1}. ${warn}`));
    }

    console.log('\n📊 Statistics:');
    console.log(`   Total Items: ${result.stats.totalItems}`);
    console.log(`   Patterns: ${Object.keys(result.stats.patternCounts).length}`);
    console.log(`   Sub-dimensions: ${Object.keys(result.stats.subDimensionCounts).length}`);

    console.log('\n' + '='.repeat(70) + '\n');
}

function main() {
    const csvPath = path.join(__dirname, '..', 'backend', 'PsyApi', 'Resources', 'Questions', 'questions_sdj_v2_ar.csv');

    if (!fs.existsSync(csvPath)) {
        console.error(`❌ CSV file not found: ${csvPath}`);
        console.error('   Run: npm run generate-csv first');
        process.exit(1);
    }

    console.log(`📂 Validating: ${csvPath}\n`);

    const result = validateCsv(csvPath);
    printSummary(result);

    process.exit(result.valid ? 0 : 1);
}

// Run if executed directly
if (require.main === module) {
    main();
}

export { validateCsv, ValidationResult };
