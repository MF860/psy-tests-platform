// lib/contract-bridge.ts
import { 
  ApiQuestionZ, 
  UiQuestionZ, 
  type UiQuestion, 
  type AnswerPayload,
  type StartSessionRequest,
  StartSessionResponseZ,
  CompleteSessionResponseZ,
  ReportStatusResponseZ
} from "./contracts";

/** 
 * Comprehensive question normalizer - handles multiple backend field name variations
 * Maps all known server formats to canonical UI shape
 */
export function normalizeApiQuestion(raw: unknown): UiQuestion {
  try {
    // First try to parse with the expected schema
    let q: any;
    try {
      q = ApiQuestionZ.parse(raw);
    } catch {
      // If validation fails, try to normalize field names first
      if (raw && typeof raw === 'object') {
        q = normalizeQuestionFields(raw as Record<string, unknown>);
        q = ApiQuestionZ.parse(q);
      } else {
        throw new Error('Invalid question data structure');
      }
    }

    const canonicalType = normalizeQuestionType((q.type || "").trim());

    // Parse options based on question type and format
    let opts: Array<{ value: string; label: string }> = [];
    // MCQ, LikertAgreement, and Frequency all use options
    if (canonicalType === "MCQ" || canonicalType === "LikertAgreement" || canonicalType === "Frequency") {
      if (typeof q.options === "string" && q.options.trim()) {
        // Handle string options (pipe-separated)
        const optionLabels = q.options.split("|").map((s: string) => s.trim()).filter(Boolean);
        
        // For Likert and Frequency, map to numeric values (1-5)
        if (canonicalType === "LikertAgreement" || canonicalType === "Frequency") {
          opts = optionLabels.map((label: string, index: number) => ({ 
            value: String(index + 1), 
            label 
          }));
        } else {
          // For MCQ, value and label are the same
          opts = optionLabels.map((x: string) => ({ value: x, label: x }));
        }
      } else if (Array.isArray(q.options) && q.options.length > 0) {
        // Handle array options
        opts = q.options.map((item: any) => {
          if (typeof item === "string") {
            return { value: item, label: item };
          } else if (item && typeof item === "object" && item.value) {
            // Handle both 'label' and 'text' properties
            const label = item.label || item.text || item.value;
            return { value: item.value, label: String(label) };
          } else {
            return { value: String(item), label: String(item) };
          }
        });
      }
    }
    // For non-option questions (like TEXT, ORDERING, TIMED_NUMERIC), options should be empty array

    const ui = {
      id: q.id,
      itemId: q.item_id,                 // snake_case → camelCase
      text: q.text_ar,
      type: canonicalType,
      dimensionTags: q.dimension_tags ?? undefined,
      timeLimitSeconds: Math.max(0, q.time_limit_seconds ?? 0),
      options: opts,
      orderingChoices: q.orderingChoices || undefined,  // Convert null to undefined
      orderingLabels: q.orderingLabels || undefined     // Convert null to undefined
    };

    return UiQuestionZ.parse(ui);
  } catch (error) {
    console.error('[CONTRACT] Failed to normalize API question:', error);
    console.error('[CONTRACT] Raw question data:', JSON.stringify(raw, null, 2));
    throw new Error(`Question normalization failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/**
 * Normalize various field name formats to the expected snake_case format
 * Handles both snake_case (standard) and PascalCase (fallback) gracefully
 */
function normalizeQuestionFields(raw: Record<string, unknown>): Record<string, unknown> {
  const normalized: Record<string, unknown> = { ...raw };
  
  // Comprehensive field mapping: PascalCase/camelCase → snake_case
  const fieldMap: Record<string, string> = {
    // ID field variations
    'id': 'id',
    'Id': 'id',
    
    // ItemId variations (CRITICAL - this was the main issue)
    'itemId': 'item_id',
    'ItemId': 'item_id',
    'item_id': 'item_id',
    
    // Text variations  
    'text': 'text_ar',
    'textAr': 'text_ar',
    'TextAr': 'text_ar',
    'text_ar': 'text_ar',
    
    // Type variations
    'type': 'type',
    'Type': 'type',
    
    // Dimension tags
    'dimensionTags': 'dimension_tags', 
    'DimensionTags': 'dimension_tags',
    'dimension_tags': 'dimension_tags',
    
    // Difficulty
    'difficulty': 'difficulty',
    'Difficulty': 'difficulty',
    
    // Time limit
    'timeLimitSeconds': 'time_limit_seconds',
    'TimeLimitSeconds': 'time_limit_seconds',
    'timeLimit': 'time_limit_seconds',
    'time_limit_seconds': 'time_limit_seconds',
    
    // Max score
    'maxScore': 'max_score',
    'MaxScore': 'max_score', 
    'max_score': 'max_score',
    
    // Options
    'options': 'options',
    'Options': 'options',
    
    // Ordering fields
    'orderingChoices': 'orderingChoices',
    'OrderingChoices': 'orderingChoices',
    'orderingLabels': 'orderingLabels',
    'OrderingLabels': 'orderingLabels',
  };

  // Apply field mappings with priority (snake_case takes precedence)
  for (const [sourceKey, targetKey] of Object.entries(fieldMap)) {
    if (sourceKey in raw) {
      // Only map if target doesn't already exist (prefer existing snake_case)
      if (!(targetKey in normalized) || sourceKey === targetKey) {
        normalized[targetKey] = raw[sourceKey];
        
        // Log mapping in development for debugging
        const env = (import.meta as any).env || {};
        if (env.DEV && sourceKey !== targetKey) {
          console.log(`[CONTRACT] Mapped ${sourceKey} → ${targetKey}:`, raw[sourceKey]);
        }
      }
    }
  }

  return normalized;
}

/** Build the exact answer payload expected by backend */
export function buildAnswerPayload(q: UiQuestion, rawValue: string, startedAtMs?: number): AnswerPayload {
  const payload: AnswerPayload = { ItemId: q.itemId };

  if (q.type === "TIMED_NUMERIC") {
    const cleanValue = String(rawValue).trim().replace(/[^\d.-]/g, '');
    if (!cleanValue) {
      throw new Error("INVALID_NUMERIC");
    }
    const num = Number(cleanValue);
    if (Number.isNaN(num)) {
      throw new Error("INVALID_NUMERIC");
    }
    payload.NumericAnswer = num;
  } else {
    payload.Answer = String(rawValue).trim();
  }

  if (startedAtMs && startedAtMs > 0) {
    payload.ResponseTimeMs = Date.now() - startedAtMs;
  }

  return payload;
}

/** Accepts both camel and Pascal case for nationalId; returns { NationalId } */
export function buildStartSessionBody(nationalId: string): StartSessionRequest {
  const clean = nationalId.replace(/[^\d]/g, "");
  if (clean.length !== 10) {
    throw new Error("INVALID_NATIONAL_ID_LENGTH");
  }
  return { NationalId: clean };
}

/** Validate and normalize start session response */
export function normalizeStartSessionResponse(raw: unknown) {
  try {
    return StartSessionResponseZ.parse(raw);
  } catch (error) {
    console.error('Failed to normalize start session response:', error);
    console.error('Raw response:', raw);
    throw new Error(`Start session response validation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/** Validate and normalize complete session response */
export function normalizeCompleteSessionResponse(raw: unknown) {
  try {
    return CompleteSessionResponseZ.parse(raw);
  } catch (error) {
    console.error('Failed to normalize complete session response:', error);
    console.error('Raw response:', raw);
    throw new Error(`Complete session response validation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/** Validate and normalize report status response */
export function normalizeReportStatusResponse(raw: unknown) {
  try {
    return ReportStatusResponseZ.parse(raw);
  } catch (error) {
    console.error('Failed to normalize report status response:', error);
    console.error('Raw response:', raw);
    throw new Error(`Report status response validation failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/** Type guards for question types */
export const QuestionTypeGuards = {
  isMCQ: (type: string): boolean => type === "MCQ",
  isLikert: (type: string): boolean => type === "LikertAgreement",
  isFrequency: (type: string): boolean => type === "Frequency", 
  isOrdering: (type: string): boolean => type === "ORDERING",
  isTimedNumeric: (type: string): boolean => type === "TIMED_NUMERIC",
  isText: (type: string): boolean => type === "TEXT" || type === "Text",
} as const;

/** Normalize question type to canonical form */
export function normalizeQuestionType(rawType: string): string {
  const type = (rawType || "").trim();
  
  // Handle common variations
  const typeMap: Record<string, string> = {
    "MCQ": "MCQ",
    "Likert": "LikertAgreement",
    "LikertAgreement": "LikertAgreement", 
    "Frequency": "Frequency",
    "ORDERING": "ORDERING",
    "Ordering": "ORDERING",
    "TIMED_NUMERIC": "TIMED_NUMERIC",
    "TimedNumeric": "TIMED_NUMERIC",
    "TEXT": "TEXT",
    "Text": "TEXT",
    "TextAnswer": "TEXT"
  };

  return typeMap[type] || "TEXT"; // fallback to TEXT
}

/** Validate national ID format */
export function validateNationalId(nationalId: string): { isValid: boolean; error?: string } {
  const cleaned = nationalId.replace(/[^\d]/g, "");
  
  if (!cleaned) {
    return { isValid: false, error: "الرقم الوطني مطلوب" };
  }
  
  if (cleaned.length !== 10) {
    return { isValid: false, error: "الرقم الوطني يجب أن يكون 10 أرقام" };
  }

  // Basic checksum validation for Saudi National ID
  const digits = cleaned.split('').map(Number);
  const sum = digits.reduce((acc, digit, index) => {
    if (index % 2 === 0) {
      const doubled = digit * 2;
      return acc + (doubled > 9 ? doubled - 9 : doubled);
    }
    return acc + digit;
  }, 0);

  if (sum % 10 !== 0) {
    return { isValid: false, error: "الرقم الوطني غير صحيح" };
  }

  return { isValid: true };
}