const fs = require('fs');
const path = require('path');

// Define file paths
const sourceCsvPath = path.join(__dirname, '..', 'backend', 'PsyApi', 'seed', 'items_100.csv');
const targetCsvPath = path.join(__dirname, '..', 'backend', 'PsyApi', 'seed', 'items_100_clean.csv');

// Check if source file exists
if (!fs.existsSync(sourceCsvPath)) {
    console.error(`Source file not found: ${sourceCsvPath}`);
    process.exit(1);
}

console.log(`Reading from: ${sourceCsvPath}`);

// Read the CSV file
const csvContent = fs.readFileSync(sourceCsvPath, 'utf8');
const lines = csvContent.split('\n');

// Process each line
const processedLines = lines.map((line, index) => {
    // Skip empty lines
    if (!line.trim()) return line;

    // Skip header line
    if (index === 0) return line;

    // Split the line by commas
    const parts = line.split(',');

    // If we don't have enough parts, return the original line
    if (parts.length < 8) return line;

    // Extract the text_ar field (second field, index 1)
    let textAr = parts[1];

    // Check if text_ar contains a comma and is not already quoted
    if (textAr.includes(',') && !textAr.startsWith('"')) {
        // Wrap with quotes
        textAr = `"${textAr}"`;
        parts[1] = textAr;

        // Reconstruct the line
        return parts.join(',');
    }

    // Return the original line if no changes needed
    return line;
});

// Join the processed lines back into a single string
const processedContent = processedLines.join('\n');

// Write the sanitized CSV
fs.writeFileSync(targetCsvPath, processedContent, 'utf8');

console.log(`Successfully created sanitized CSV: ${targetCsvPath}`);
console.log(`Processed ${lines.length} lines`);
