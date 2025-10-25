/**
 * Convert Arabic numerals (٠-٩) to English numerals (0-9) and remove non-digit characters
 */
export const toEnglishDigits = (value: string): string => {
  return value
    .replace(/[٠-٩]/g, (digit) => {
      const arabicDigits = '٠١٢٣٤٥٦٧٨٩';
      return arabicDigits.indexOf(digit).toString();
    })
    .replace(/[^\d]/g, ''); // Remove all non-digit characters
};

/**
 * Validate that a string is exactly 10 English digits
 */
export const isValidNationalId = (value: string): boolean => {
  return /^\d{10}$/.test(value);
};