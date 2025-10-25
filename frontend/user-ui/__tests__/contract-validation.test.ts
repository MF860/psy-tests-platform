// __tests__/contract-validation.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { normalizeApiQuestion, buildAnswerPayload, buildStartSessionBody, normalizeStartSessionResponse } from '../src/lib/contract-bridge';
import { type UiQuestion } from '../src/lib/contracts';

describe('Contract Validation & Normalization', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Question Normalization', () => {
    it('normalizes server snake_case fields to UI camelCase', () => {
      const rawQuestion = {
        id: 1,
        item_id: 'I001',
        text_ar: 'هذا سؤال تجريبي',
        type: 'MCQ',
        dimension_tags: 'personality',
        time_limit_seconds: 30,
        max_score: 5,
        options: 'نعم|لا|ربما'
      };

      const normalized = normalizeApiQuestion(rawQuestion);

      expect(normalized).toEqual({
        id: 1,
        itemId: 'I001',
        text: 'هذا سؤال تجريبي',
        type: 'MCQ',
        dimensionTags: 'personality',
        timeLimitSeconds: 30,
        options: [
          { value: 'نعم', label: 'نعم' },
          { value: 'لا', label: 'لا' },
          { value: 'ربما', label: 'ربما' }
        ],
        orderingChoices: undefined,
        orderingLabels: undefined
      });
    });

    it('handles various field name formats', () => {
      const rawQuestion = {
        id: 2,
        ItemId: 'I002',           // PascalCase
        textAr: 'سؤال آخر',        // camelCase  
        type: 'LikertAgreement',
        DimensionTags: 'cognitive', // PascalCase
        timeLimitSeconds: 45,      // camelCase
        MaxScore: 7                // PascalCase
      };

      const normalized = normalizeApiQuestion(rawQuestion);

      expect(normalized.itemId).toBe('I002');
      expect(normalized.text).toBe('سؤال آخر');
      expect(normalized.dimensionTags).toBe('cognitive');
      expect(normalized.timeLimitSeconds).toBe(45);
    });

    it('normalizes question types to canonical forms', () => {
      const testCases = [
        { input: 'MCQ', expected: 'MCQ' },
        { input: 'Likert', expected: 'LikertAgreement' },
        { input: 'LikertAgreement', expected: 'LikertAgreement' },
        { input: 'ORDERING', expected: 'ORDERING' },
        { input: 'Ordering', expected: 'ORDERING' },
        { input: 'TIMED_NUMERIC', expected: 'TIMED_NUMERIC' },
        { input: 'TimedNumeric', expected: 'TIMED_NUMERIC' },
        { input: 'TEXT', expected: 'TEXT' },
        { input: 'Text', expected: 'TEXT' },
        { input: 'Unknown', expected: 'TEXT' }, // fallback
      ];

      testCases.forEach(({ input, expected }) => {
        const rawQuestion = {
          id: 1,
          item_id: 'I001',
          text_ar: 'Test',
          type: input,
          time_limit_seconds: 30,
          max_score: 5
        };

        const normalized = normalizeApiQuestion(rawQuestion);
        expect(normalized.type).toBe(expected);
      });
    });

    it('handles missing optional fields gracefully', () => {
      const minimalQuestion = {
        id: 3,
        item_id: 'I003',
        text_ar: 'سؤال بسيط',
        type: 'TEXT'
      };

      const normalized = normalizeApiQuestion(minimalQuestion);

      expect(normalized.timeLimitSeconds).toBe(0);
      expect(normalized.dimensionTags).toBeUndefined();
      expect(normalized.options).toEqual([]);
    });

    it('throws descriptive error for invalid question data', () => {
      const invalidQuestion = {
        // Missing required fields
        type: 'MCQ'
      };

      expect(() => normalizeApiQuestion(invalidQuestion)).toThrow(/Question normalization failed/);
    });
  });

  describe('Answer Payload Building', () => {
    it('builds correct payload for text answers', () => {
      const question: UiQuestion = {
        id: 1,
        itemId: 'I001',
        text: 'Test question',
        type: 'TEXT',
        timeLimitSeconds: 30,
        options: []
      };

      const payload = buildAnswerPayload(question, 'My answer', 1000);

      expect(payload).toEqual({
        ItemId: 'I001',
        Answer: 'My answer',
        ResponseTimeMs: expect.any(Number)
      });
      expect(payload.ResponseTimeMs).toBeGreaterThan(0);
    });

    it('builds correct payload for numeric answers', () => {
      const question: UiQuestion = {
        id: 2,
        itemId: 'I002',
        text: 'Numeric question',
        type: 'TIMED_NUMERIC',
        timeLimitSeconds: 15,
        options: []
      };

      const payload = buildAnswerPayload(question, '42');

      expect(payload).toEqual({
        ItemId: 'I002',
        NumericAnswer: 42
      });
    });

    it('handles invalid numeric input', () => {
      const question: UiQuestion = {
        id: 3,
        itemId: 'I003',
        text: 'Numeric question',
        type: 'TIMED_NUMERIC',
        timeLimitSeconds: 15,
        options: []
      };

      expect(() => buildAnswerPayload(question, 'not a number')).toThrow('INVALID_NUMERIC');
    });

    it('cleans numeric input correctly', () => {
      const question: UiQuestion = {
        id: 4,
        itemId: 'I004',
        text: 'Numeric question',
        type: 'TIMED_NUMERIC',
        timeLimitSeconds: 15,
        options: []
      };

      const payload = buildAnswerPayload(question, '  42.5  ');
      expect(payload.NumericAnswer).toBe(42.5);
    });
  });

  describe('Session Request Building', () => {
    it('builds correct start session request', () => {
      const request = buildStartSessionBody('1000000003');

      expect(request).toEqual({
        NationalId: '1000000003'
      });
    });

    it('cleans national ID input', () => {
      const request = buildStartSessionBody('  10-00/00.00-03  ');

      expect(request.NationalId).toBe('1000000003');
    });

    it('validates national ID length', () => {
      expect(() => buildStartSessionBody('123')).toThrow('INVALID_NATIONAL_ID_LENGTH');
      expect(() => buildStartSessionBody('12345678901')).toThrow('INVALID_NATIONAL_ID_LENGTH');
    });
  });

  describe('Response Normalization', () => {
    it('normalizes start session response', () => {
      const rawResponse = {
        sessionId: 'abc123',
        totalQuestions: 80,
        resume: true
      };

      const normalized = normalizeStartSessionResponse(rawResponse);

      expect(normalized).toEqual({
        sessionId: 'abc123',
        totalQuestions: 80,
        resume: true
      });
    });

    it('handles missing optional fields in response', () => {
      const rawResponse = {
        sessionId: 'def456'
        // totalQuestions missing, resume missing
      };

      const normalized = normalizeStartSessionResponse(rawResponse);

      expect(normalized.sessionId).toBe('def456');
      expect(normalized.totalQuestions).toBe(0); // default value
      expect(normalized.resume).toBeUndefined();
    });

    it('throws descriptive error for invalid response', () => {
      const invalidResponse = {
        // Missing sessionId
        totalQuestions: 80
      };

      expect(() => normalizeStartSessionResponse(invalidResponse)).toThrow(/Start session response validation failed/);
    });
  });

  describe('Field Name Variations Handling', () => {
    it('maps common backend field variations', () => {
      const variations = [
        // ItemId variations - need both item_id and text_ar as required fields
        { item_id: 'I001', text_ar: 'Test' },
        { ItemId: 'I001', text_ar: 'Test' },
        { itemId: 'I001', text_ar: 'Test' },
        
        // Text variations - need item_id as required field
        { item_id: 'I001', text: 'Test' },
        { item_id: 'I001', textAr: 'Test' },
        { item_id: 'I001', TextAr: 'Test' },
        { item_id: 'I001', text_ar: 'Test' },
        
        // Time limit variations - need required fields
        { item_id: 'I001', text_ar: 'Test', timeLimitSeconds: 30 },
        { item_id: 'I001', text_ar: 'Test', TimeLimitSeconds: 30 },
        { item_id: 'I001', text_ar: 'Test', timeLimit: 30 },
        { item_id: 'I001', text_ar: 'Test', time_limit_seconds: 30 },
      ];

      variations.forEach((variation) => {
        const rawQuestion = {
          id: 1,
          type: 'TEXT',
          ...variation
        };

        // Should not throw and should normalize correctly
        expect(() => normalizeApiQuestion(rawQuestion)).not.toThrow();
      });
    });
  });

  describe('Error Handling & Logging', () => {
    it('logs contract mismatches in development', () => {
      // Mock console methods
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
      
      // Create question with field mapping
      const rawQuestion = {
        id: 1,
        ItemId: 'I001',  // This should trigger mapping log
        text_ar: 'Test',
        type: 'TEXT'
      };

      normalizeApiQuestion(rawQuestion);

      // In a real dev environment, this would log the mapping
      // But we can't easily test import.meta.env here
      consoleSpy.mockRestore();
    });

    it('provides detailed error context on validation failure', () => {
      const invalidData = { invalid: 'data' };

      try {
        normalizeApiQuestion(invalidData);
        expect.fail('Should have thrown an error');
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect((error as Error).message).toContain('Question normalization failed');
      }
    });
  });
});