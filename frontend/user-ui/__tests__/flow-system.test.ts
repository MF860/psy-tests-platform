// __tests__/flow-system.test.ts
import { describe, it, expect, beforeEach } from 'vitest';
import { routeFor, type PageKey, type FlowState } from '../src/lib/routeFor';

describe('Flow System - Route Guards', () => {
  let baseState: FlowState;

  beforeEach(() => {
    baseState = {
      nationalId: undefined,
      sessionId: undefined,
      resume: false,
      consented: false,
      instructionsCompleted: false,
      examFinished: false,
    };
  });

  describe('Login Page', () => {
    it('allows staying on login when no session exists', () => {
      const result = routeFor('login', baseState);
      expect(result).toBeNull();
    });

    it('redirects to exam when resume is true', () => {
      const state = { ...baseState, sessionId: 'session123', resume: true };
      const result = routeFor('login', state);
      expect(result).toBe('/exam');
    });

    it('redirects to instructions when consented but instructions not completed', () => {
      const state = { ...baseState, sessionId: 'session123', consented: true };
      const result = routeFor('login', state);
      expect(result).toBe('/instructions');
    });

    it('redirects to exam when ready for exam', () => {
      const state = {
        ...baseState,
        sessionId: 'session123',
        consented: true,
        instructionsCompleted: true,
      };
      const result = routeFor('login', state);
      expect(result).toBe('/exam');
    });

    it('redirects to thank-you when exam is finished', () => {
      const state = {
        ...baseState,
        sessionId: 'session123',
        examFinished: true,
      };
      const result = routeFor('login', state);
      expect(result).toBe('/thank-you');
    });
  });

  describe('Privacy Page', () => {
    it('redirects to login when no nationalId or sessionId', () => {
      const result = routeFor('privacy', baseState);
      expect(result).toBe('/login');
    });

    it('allows staying when has nationalId', () => {
      const state = { ...baseState, nationalId: '1000000003' };
      const result = routeFor('privacy', state);
      expect(result).toBeNull();
    });

    it('redirects to exam when in resume mode', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        sessionId: 'session123',
        resume: true,
      };
      const result = routeFor('privacy', state);
      expect(result).toBe('/exam');
    });

    it('redirects to instructions when already consented', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        consented: true,
      };
      const result = routeFor('privacy', state);
      expect(result).toBe('/instructions');
    });
  });

  describe('Instructions Page', () => {
    it('redirects to privacy when missing consent', () => {
      const state = { ...baseState, nationalId: '1000000003' };
      const result = routeFor('instructions', state);
      expect(result).toBe('/privacy');
    });

    it('allows staying when has nationalId and consent', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        consented: true,
      };
      const result = routeFor('instructions', state);
      expect(result).toBeNull();
    });

    it('redirects to exam when instructions completed', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        consented: true,
        instructionsCompleted: true,
      };
      const result = routeFor('instructions', state);
      expect(result).toBe('/exam');
    });
  });

  describe('Exam Page', () => {
    it('redirects to login when missing session', () => {
      const result = routeFor('exam', baseState);
      expect(result).toBe('/login');
    });

    it('redirects to privacy when missing consent', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        sessionId: 'session123',
      };
      const result = routeFor('exam', state);
      expect(result).toBe('/privacy');
    });

    it('redirects to instructions when missing instructions', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        sessionId: 'session123',
        consented: true,
      };
      const result = routeFor('exam', state);
      expect(result).toBe('/instructions');
    });

    it('allows staying when all prerequisites met', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        sessionId: 'session123',
        consented: true,
        instructionsCompleted: true,
      };
      const result = routeFor('exam', state);
      expect(result).toBeNull();
    });

    it('redirects to thank-you when exam finished', () => {
      const state = {
        ...baseState,
        nationalId: '1000000003',
        sessionId: 'session123',
        consented: true,
        instructionsCompleted: true,
        examFinished: true,
      };
      const result = routeFor('exam', state);
      expect(result).toBe('/thank-you');
    });
  });

  describe('Thank You Page', () => {
    it('redirects to login when exam not finished', () => {
      const result = routeFor('thank-you', baseState);
      expect(result).toBe('/login');
    });

    it('redirects to exam when has active session but exam not finished', () => {
      const state = {
        ...baseState,
        sessionId: 'session123',
        consented: true,
        instructionsCompleted: true,
      };
      const result = routeFor('thank-you', state);
      expect(result).toBe('/exam');
    });

    it('allows staying when exam is finished', () => {
      const state = {
        ...baseState,
        examFinished: true,
      };
      const result = routeFor('thank-you', state);
      expect(result).toBeNull();
    });
  });
});

describe('Canonical Flow Validation', () => {
  it('enforces correct sequence: login → privacy → instructions → exam → thank-you', () => {
    let state: FlowState = {
      nationalId: undefined,
      sessionId: undefined,
      resume: false,
      consented: false,
      instructionsCompleted: false,
      examFinished: false,
    };

    // Step 1: Login - should stay on login
    expect(routeFor('login', state)).toBeNull();

    // Step 2: After login, should go to privacy
    state = { ...state, nationalId: '1000000003', sessionId: 'session123' };
    expect(routeFor('login', state)).toBe('/privacy');

    // Step 3: On privacy, should stay until consented
    expect(routeFor('privacy', state)).toBeNull();

    // Step 4: After consent, should go to instructions
    state = { ...state, consented: true };
    expect(routeFor('privacy', state)).toBe('/instructions');

    // Step 5: On instructions, should stay until completed
    expect(routeFor('instructions', state)).toBeNull();

    // Step 6: After instructions, should go to exam
    state = { ...state, instructionsCompleted: true };
    expect(routeFor('instructions', state)).toBe('/exam');

    // Step 7: On exam, should stay until finished
    expect(routeFor('exam', state)).toBeNull();

    // Step 8: After exam, should go to thank-you
    state = { ...state, examFinished: true };
    expect(routeFor('exam', state)).toBe('/thank-you');

    // Step 9: On thank-you, should stay
    expect(routeFor('thank-you', state)).toBeNull();
  });

  it('handles resume flow correctly', () => {
    const resumeState: FlowState = {
      nationalId: '1000000003',
      sessionId: 'session123',
      resume: true,
      consented: true,
      instructionsCompleted: true,
      examFinished: false,
    };

    // Should skip directly to exam from login
    expect(routeFor('login', resumeState)).toBe('/exam');

    // Should skip to exam from privacy
    expect(routeFor('privacy', resumeState)).toBe('/exam');

    // Should allow staying on exam
    expect(routeFor('exam', resumeState)).toBeNull();
  });
});