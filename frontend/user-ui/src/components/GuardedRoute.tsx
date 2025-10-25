// components/GuardedRoute.tsx
import React, { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useFlowState } from '../lib/useFlowState';
import { routeFor, type PageKey } from '../lib/routeFor';

interface GuardedRouteProps {
  page: PageKey;
  children: React.ReactNode;
}

/**
 * GuardedRoute - Single source of truth for navigation decisions
 * Automatically redirects users to the correct page based on flow state
 */
export function GuardedRoute({ page, children }: GuardedRouteProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const flowState = useFlowState();

  useEffect(() => {
    const correctRoute = routeFor(page, flowState);
    
    if (correctRoute && correctRoute !== location.pathname) {
      console.log(`[GuardedRoute] Redirecting from ${location.pathname} to ${correctRoute}`, {
        page,
        state: flowState,
        reason: getRedirectReason(page, flowState)
      });
      navigate(correctRoute, { replace: true });
    }
  }, [page, flowState, navigate, location.pathname]);

  // Only render children if we're on the correct page
  const correctRoute = routeFor(page, flowState);
  if (correctRoute && correctRoute !== location.pathname) {
    return <div className="flex items-center justify-center min-h-screen">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>;
  }

  return <>{children}</>;
}

/**
 * Helper function to get human-readable redirect reasons for debugging
 */
function getRedirectReason(page: PageKey, state: any): string {
  const { nationalId, sessionId, resume, consented, instructionsCompleted, examFinished } = state;

  switch (page) {
    case 'login':
      if (sessionId && !examFinished && resume) return 'Resume mode - skip to exam';
      if (sessionId && consented && !instructionsCompleted) return 'Has consent - go to instructions';
      if (sessionId && consented && instructionsCompleted && !examFinished) return 'Ready for exam';
      if (sessionId && examFinished) return 'Exam completed - go to results';
      return 'Staying on login';

    case 'privacy':
      if (!nationalId && !sessionId) return 'No session - go to login';
      if (resume && sessionId) return 'Resume mode - skip to exam';
      if (consented) return 'Already consented - go to instructions';
      return 'Need consent';

    case 'instructions':
      if (!nationalId || !consented) return 'Missing consent - go to privacy';
      if (instructionsCompleted) return 'Instructions done - go to exam';
      return 'Need to complete instructions';

    case 'exam':
      if (!sessionId || !consented || !instructionsCompleted) return 'Missing prerequisites';
      if (examFinished) return 'Exam done - go to results';
      return 'Ready for exam';

    case 'thank-you':
      if (!examFinished) return 'Exam not finished';
      return 'Results ready';

    default:
      return 'Unknown page';
  }
}