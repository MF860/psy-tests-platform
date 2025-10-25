import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Badge } from './ui/badge';
import { TrendingUp, Target, Users } from 'lucide-react';

interface SdjTrack {
  trackNameAr: string;
  trackNameEn: string;
  fitLevel: 'high' | 'medium' | 'low';
  fitScore: number;
  reasoningAr: string;
  keyCompetencies: string[];
}

interface SdjTrackCardProps {
  track: SdjTrack;
}

const FIT_CONFIG = {
  high: { 
    color: 'bg-green-100 text-green-800 border-green-200', 
    icon: TrendingUp, 
    label: 'ملائمة عالية',
    bgClass: 'bg-green-50/50'
  },
  medium: { 
    color: 'bg-amber-100 text-amber-800 border-amber-200', 
    icon: Target, 
    label: 'ملائمة متوسطة',
    bgClass: 'bg-amber-50/50'
  },
  low: { 
    color: 'bg-red-100 text-red-800 border-red-200', 
    icon: Users, 
    label: 'تحتاج تطوير',
    bgClass: 'bg-red-50/50'
  }
};

export function SdjTrackCard({ track }: SdjTrackCardProps) {
  const config = FIT_CONFIG[track.fitLevel];
  const Icon = config.icon;

  return (
    <Card className={`hover:shadow-lg transition-all duration-200 border-2 ${config.bgClass}`}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-lg mb-1 text-slate-900" dir="rtl">
              {track.trackNameAr}
            </CardTitle>
            <p className="text-sm text-slate-600">{track.trackNameEn}</p>
          </div>
          <Badge variant="outline" className={`${config.color} flex-shrink-0 px-2 py-1`}>
            <Icon className="h-3 w-3 ml-1" />
            {config.label}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-center gap-3">
          <span className="text-3xl font-bold text-primary">{track.fitScore.toFixed(1)}</span>
          <span className="text-sm text-muted-foreground">درجة T</span>
        </div>
        
        <p className="text-sm text-slate-700 leading-relaxed" dir="rtl">
          {track.reasoningAr}
        </p>

        <div>
          <p className="text-xs font-semibold text-slate-500 mb-2" dir="rtl">الكفاءات الرئيسية:</p>
          <div className="flex flex-wrap gap-1.5">
            {track.keyCompetencies.map((comp, idx) => (
              <Badge 
                key={idx} 
                variant="secondary" 
                className="text-xs py-0.5 px-2 bg-slate-100 text-slate-700 hover:bg-slate-200"
              >
                {comp}
              </Badge>
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
