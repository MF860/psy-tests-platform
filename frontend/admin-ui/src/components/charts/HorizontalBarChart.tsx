import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell, LabelList } from 'recharts';

interface HorizontalBarChartProps {
  data: Array<{
    name: string;
    value: number;
    band: 'Weak' | 'Average' | 'Excellent';
  }>;
}

const BAND_COLORS = {
  Weak: '#ef4444',       // red-500
  Average: '#f59e0b',    // amber-500
  Excellent: '#10b981'   // green-500
};

const BAND_COLORS_AR = {
  ضعيف: '#ef4444',
  متوسط: '#f59e0b',
  ممتاز: '#10b981'
};

export function HorizontalBarChart({ data }: HorizontalBarChartProps) {
  // Sort ascending by T-score
  const sortedData = [...data].sort((a, b) => a.value - b.value);

  // Custom label to show T-score at end of bar
  const renderCustomLabel = (props: any) => {
    const { x, y, width, value } = props;
    return (
      <text 
        x={x + width + 8} 
        y={y + 10} 
        fill="#334155" 
        fontSize="13" 
        fontWeight="600"
        textAnchor="start"
      >
        T={value.toFixed(1)}
      </text>
    );
  };

  return (
    <ResponsiveContainer width="100%" height={Math.max(400, sortedData.length * 50)}>
      <BarChart 
        data={sortedData} 
        layout="vertical"
        margin={{ top: 10, right: 60, left: 20, bottom: 10 }}
      >
        <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
        <XAxis 
          type="number" 
          domain={[0, 80]} 
          tick={{ fontSize: 12, fill: '#64748b' }}
          label={{ value: 'T-Score', position: 'insideBottom', offset: -5, style: { fontSize: 12, fill: '#475569' } }}
        />
        <YAxis 
          type="category" 
          dataKey="name" 
          width={120} 
          tick={{ fontSize: 13, fill: '#1e293b', fontWeight: 500, direction: 'rtl' }} 
          interval={0}
        />
        <Tooltip 
          contentStyle={{ 
            backgroundColor: '#ffffff', 
            border: '1px solid #e2e8f0', 
            borderRadius: '8px',
            padding: '8px 12px',
            direction: 'rtl'
          }}
          labelStyle={{ fontWeight: 600, marginBottom: 4 }}
          formatter={(value: number, name: string, props: any) => [
            `T: ${value.toFixed(1)} (${props.payload.band})`,
            ''
          ]}
        />
        <Bar dataKey="value" name="T-Score" radius={[0, 8, 8, 0]}>
          {sortedData.map((entry, index) => (
            <Cell 
              key={`cell-${index}`} 
              fill={BAND_COLORS[entry.band] || BAND_COLORS_AR[entry.band as keyof typeof BAND_COLORS_AR] || '#94a3b8'} 
            />
          ))}
          <LabelList content={renderCustomLabel} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
