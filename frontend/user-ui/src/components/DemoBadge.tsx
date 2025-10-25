import React from 'react';

const DemoBadge: React.FC = () => {
  const env = (import.meta as any).env || {};
  const isDemoMode = env.VITE_DEMO_MODE === 'true';
  
  if (!isDemoMode) {
    return null;
  }
  
  return (
    <div 
      className="fixed top-2 right-2 z-50 bg-yellow-500 text-yellow-900 px-2 py-1 rounded text-xs font-bold shadow-lg"
      style={{ 
        fontFamily: 'Arial, sans-serif',
        direction: 'ltr' // Force LTR for "DEMO" text
      }}
    >
      DEMO
    </div>
  );
};

export default DemoBadge;