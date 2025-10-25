import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { sessionStorage } from '../lib/session';

interface RouteGuardProps {
  children: React.ReactNode;
  requiresSession?: boolean;
  requiresConsent?: boolean;
  requiresInstructions?: boolean;
}

export default function RouteGuard({ 
  children, 
  requiresSession = false,
  requiresConsent = false,
  requiresInstructions = false 
}: RouteGuardProps) {
  const location = useLocation();
  const session = sessionStorage.get();

  // If session is required but doesn't exist, redirect to login
  if (requiresSession && !session?.sessionId) {
    return <Navigate to="/login" replace />;
  }

  // If consent is required but not given, redirect to privacy
  if (requiresConsent && !session?.consented) {
    return <Navigate to="/privacy" replace />;
  }

  // If instructions completion is required but not done, redirect to instructions
  if (requiresInstructions && !session?.instructionsCompleted) {
    return <Navigate to="/instructions" replace />;
  }

  // Prevent going back to completed steps
  if (session?.sessionId) {
    // If already consented, don't allow going back to privacy
    if (location.pathname === '/privacy' && session.consented) {
      return <Navigate to="/instructions" replace />;
    }
    
    // If instructions completed, don't allow going back to instructions
    if (location.pathname === '/instructions' && session.instructionsCompleted) {
      return <Navigate to="/exam" replace />;
    }
    
    // If on login but session exists, redirect to appropriate step
    if (location.pathname === '/login' && session.sessionId) {
      if (!session.consented) {
        return <Navigate to="/privacy" replace />;
      } else if (!session.instructionsCompleted) {
        return <Navigate to="/instructions" replace />;
      } else {
        return <Navigate to="/exam" replace />;
      }
    }
  }

  return <>{children}</>;
}