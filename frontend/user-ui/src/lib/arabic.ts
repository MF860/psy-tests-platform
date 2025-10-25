export function decodeHtmlEntities(input: string): string {
  if (!input) return "";
  if (typeof window === "undefined") return input;
  const textarea = document.createElement("textarea");
  textarea.innerHTML = input;
  return textarea.value;
}

export function normalizeArabic(input: string): string {
  if (!input) return "";
  try {
    const decoded = decodeHtmlEntities(input).replace(/&nbsp;/g, " ");
    // Remove stray directional control characters
    const cleaned = decoded.replace(/[\u200E\u200F\u202A-\u202E]/g, "");
    return cleaned.normalize("NFC").trim();
  } catch {
    return input;
  }
}

// Detect frequency wording to override Likert labels
export function containsFrequencyCues(text: string): boolean {
  if (!text) return false;
  const t = text.toString();
  // Check for frequency question patterns
  return /(كم|أبدًا|نادراً|نادرًا|أحياناً|أحيانًا|غالباً|غالبًا|دائماً|دائمًا)/.test(t);
}

export function normalizeNumeric(input: string): string {
  if (!input) return "";
  return input.trim()
    .replace(/[٠١٢٣٤٥٦٧٨٩]/g, m => String.fromCharCode(m.charCodeAt(0) - 1632 + 48)) // Arabic digits
    .replace(/[۰۱۲۳۴۵۶۷۸۹]/g, m => String.fromCharCode(m.charCodeAt(0) - 1776 + 48)) // Persian digits
    .replace(/٫/g, ".") // Arabic decimal separator
    .replace(/٬/g, "") // Arabic thousands separator
    .replace(/[^\d.-]/g, ""); // Keep only digits, dots, and minus signs
}

export function validateNumericInput(input: string, allowNegative = false): { isValid: boolean; value: number | null; error?: string } {
  if (!input.trim()) {
    return { isValid: false, value: null, error: "يجب إدخال قيمة رقمية" };
  }

  const normalized = normalizeNumeric(input);
  const num = Number(normalized);

  if (!Number.isFinite(num)) {
    return { isValid: false, value: null, error: "يجب إدخال رقم صحيح" };
  }

  if (!allowNegative && num < 0) {
    return { isValid: false, value: null, error: "لا يسمح بالأرقام السالبة" };
  }

  return { isValid: true, value: num };
}

