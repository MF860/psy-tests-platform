import { useState } from "react";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { strings } from "../../lib/strings";

interface TimedNumericProps {
  value: string;
  onChange: (value: string) => void;
  questionId: string;
}

export default function TimedNumeric({ value, onChange, questionId }: TimedNumericProps) {
  const [error, setError] = useState<string | null>(null);

  const validateNumericInput = (input: string): { isValid: boolean; value?: number; error?: string } => {
    const trimmed = input.trim();
    
    if (!trimmed) {
      return { isValid: false, error: "القيمة مطلوبة" };
    }
    
    // Convert Arabic numerals to English numerals
    const normalized = trimmed.replace(/[٠-٩]/g, (d) => 
      '٠١٢٣٤٥٦٧٨٩'.indexOf(d).toString()
    );
    
    // Check if it's a valid number
    const num = parseFloat(normalized);
    if (isNaN(num)) {
      return { isValid: false, error: strings.exam.errors.invalidNumeric };
    }
    
    return { isValid: true, value: num };
  };

  const normalizeNumeric = (input: string): string => {
    // Convert Arabic numerals to English numerals and remove non-numeric chars except decimal point and minus
    return input
      .replace(/[٠-٩]/g, (d) => '٠١٢٣٤٥٦٧٨٩'.indexOf(d).toString())
      .replace(/[^\d.-]/g, '');
  };

  const handleChange = (newValue: string) => {
    const normalized = normalizeNumeric(newValue);
    onChange(normalized);
    
    // Clear error when user starts typing
    if (error && newValue.trim()) {
      setError(null);
    }
  };

  const handleBlur = () => {
    if (value.trim()) {
      const validation = validateNumericInput(value);
      setError(validation.error || null);
    }
  };

  return (
    <div className="space-y-3 sm:space-y-4 w-full max-w-sm mx-auto">
      <Label htmlFor={`numeric-${questionId}`} className="text-right block text-sm sm:text-base font-medium text-white">
        {strings.exam.placeholders.numericAnswer}
      </Label>
      <Input
        id={`numeric-${questionId}`}
        type="text"
        inputMode="decimal"
        pattern="[0-9]*"
        value={value}
        onChange={(e) => handleChange(e.target.value)}
        onBlur={handleBlur}
        placeholder="مثال: 25 أو 3.5"
        className={`
          h-14 sm:h-16 text-center font-mono text-xl sm:text-2xl w-full
          bg-white/10 border-2 text-white rounded-xl
          focus:border-blue-400 focus:bg-white/15
          ${error ? 'border-red-400 focus:border-red-400' : 'border-white/20'}
        `}
        dir="ltr"
      />
      {error && (
        <p className="text-red-300 text-sm sm:text-base text-right font-medium bg-red-500/20 border border-red-400/30 p-3 rounded-lg">
          {error}
        </p>
      )}
      <p className="text-xs sm:text-sm text-gray-400 text-right leading-relaxed">
        أدخل رقماً صحيحاً أو عشرياً (استخدم النقطة للفاصل العشري)
      </p>
    </div>
  );
}