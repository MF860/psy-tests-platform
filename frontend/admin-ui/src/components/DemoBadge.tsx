import React from 'react';

const DemoBadge: React.FC = () => {
  const env = (import.meta as any).env || {};
  const isDemoMode = env.VITE_DEMO_MODE === 'true';
  
  if (!isDemoMode) {
    return null;
  }
  
  return (
    <div 
      className="fixed top-4 right-4 z-50 bg-yellow-500 text-yellow-900 px-3 py-1 rounded text-sm font-bold shadow-lg"
      style={{ 
        fontFamily: 'Arial, sans-serif',
        direction: 'ltr' // Force LTR for "DEMO" text
      }}
    >
      DEMO MODE
    </div>
  );
};

export default DemoBadge;