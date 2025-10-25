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
      <Label htmlFor={`numeric-${questionId}`} className="text-right block text-sm sm:text-base font-medium">
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
          h-12 sm:h-12 text-center font-mono text-base sm:text-xl w-full
          focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
          ${error ? 'border-destructive focus-visible:ring-destructive' : ''}
        `}
        dir="ltr"
      />
      {error && (
        <p className="text-destructive text-sm sm:text-base text-right font-medium bg-destructive/10 p-2 rounded-lg">
          {error}
        </p>
      )}
      <p className="text-xs sm:text-sm text-muted-foreground text-right leading-relaxed">
        أدخل رقماً صحيحاً أو عشرياً (استخدم النقطة للفاصل العشري)
      </p>
    </div>
  );
}