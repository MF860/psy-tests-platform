// lib/routeFor.ts
export interface FlowState {
  nationalId?: string;
  sessionId?: string;
  resume?: boolean;
  consented: boolean;
  instructionsCompleted: boolean;
  examFinished: boolean;
}

export type PageKey = 'login' | 'privacy' | 'instructions' | 'exam' | 'thank-you';

/**
 * Pure routing matrix - determines the correct route for each page based on flow state
 * Returns the route the user should be on, or null if they can stay on the current page
 */
export function routeFor(page: PageKey, state: FlowState): string | null {
  const { nationalId, sessionId, resume, consented, instructionsCompleted, examFinished } = state;

  switch (page) {
    case 'login':
      // If we have a session and should resume, go to exam
      if (sessionId && !examFinished && resume) return '/exam';
      // If we have consent but not instructions, go to instructions  
      if (sessionId && consented && !instructionsCompleted) return '/instructions';
      // If we have instructions done but exam not finished, go to exam
      if (sessionId && consented && instructionsCompleted && !examFinished) return '/exam';
      // If exam is finished, go to thank you
      if (sessionId && examFinished) return '/thank-you';
      // If we have sessionId but no consent yet, go to privacy
      if (sessionId && !consented) return '/privacy';
      // Otherwise, stay on login
      return null;

    case 'privacy':
      // Requires nationalId OR sessionId, else go to login
      if (!nationalId && !sessionId) return '/login';
      // If resume mode, go directly to exam
      if (resume && sessionId) return '/exam';
      // If already consented, go to instructions
      if (consented) return '/instructions';
      // Otherwise, stay on privacy
      return null;

    case 'instructions':
      // Requires nationalId AND consented, else go back to privacy
      if (!nationalId || !consented) return '/privacy';
      // If instructions already completed, go to exam
      if (instructionsCompleted) return '/exam';
      // Otherwise, stay on instructions
      return null;

    case 'exam':
      // Requires sessionId AND consented AND instructionsCompleted
      if (!sessionId || !consented || !instructionsCompleted) {
        // Find the earliest missing step
        if (!nationalId) return '/login';
        if (!consented) return '/privacy';
        if (!instructionsCompleted) return '/instructions';
        return '/login'; // fallback
      }
      // If exam is finished, go to thank you
      if (examFinished) return '/thank-you';
      // Otherwise, stay on exam
      return null;

    case 'thank-you':
      // Requires examFinished
      if (!examFinished) {
        // If we have an active session, go to exam
        if (sessionId && consented && instructionsCompleted) return '/exam';
        // Otherwise go to login
        return '/login';
      }
      // Otherwise, stay on thank-you
      return null;

    default:
      return '/login';
  }
}

/**
 * Helper to get the canonical flow sequence
 */
export const CANONICAL_FLOW: PageKey[] = ['login', 'privacy', 'instructions', 'exam', 'thank-you'];

/**
 * Check if a transition from one page to another is valid
 */
export function isValidTransition(from: PageKey, to: PageKey): boolean {
  const fromIndex = CANONICAL_FLOW.indexOf(from);
  const toIndex = CANONICAL_FLOW.indexOf(to);
  
  // Allow forward movement or staying on the same page
  return toIndex >= fromIndex;
}

/**
 * Get the next page in the canonical flow
 */
export function getNextPage(current: PageKey): PageKey | null {
  const currentIndex = CANONICAL_FLOW.indexOf(current);
  if (currentIndex === -1 || currentIndex === CANONICAL_FLOW.length - 1) {
    return null;
  }
  return CANONICAL_FLOW[currentIndex + 1];
}

/**
 * Get the previous page in the canonical flow
 */
export function getPreviousPage(current: PageKey): PageKey | null {
  const currentIndex = CANONICAL_FLOW.indexOf(current);
  if (currentIndex <= 0) {
    return null;
  }
  return CANONICAL_FLOW[currentIndex - 1];
}