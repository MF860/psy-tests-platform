import { ReactNode } from 'react';
import { Card } from '../ui/card';

interface SectionCardProps {
  children: ReactNode;
  className?: string;
  variant?: 'default' | 'ghost' | 'outline';
}

export function SectionCard({ 
  children, 
  className = '', 
  variant = 'default' 
}: SectionCardProps) {
  const variants = {
    default: 'rounded-2xl shadow-soft bg-card text-card-foreground border',
    ghost: 'rounded-2xl bg-transparent',
    outline: 'rounded-2xl border-2 bg-card text-card-foreground'
  };

  return (
    <Card className={`${variants[variant]} p-4 sm:p-6 ${className}`}>
      {children}
    </Card>
  );
}

export default SectionCard;