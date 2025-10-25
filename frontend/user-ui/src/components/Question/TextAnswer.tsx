import { useState, useEffect } from "react";
import { Textarea } from "../ui/textarea";
import { Label } from "../ui/label";
import { strings } from "../../lib/strings";

interface TextAnswerProps {
  value: string;
  onChange: (value: string) => void;
  questionId: string;
  maxLength?: number;
  minLength?: number;
}

export default function TextAnswer({ 
  value, 
  onChange, 
  questionId, 
  maxLength = 500,
  minLength = 1
}: TextAnswerProps) {
  const [charCount, setCharCount] = useState(0);

  useEffect(() => {
    setCharCount(value.length);
  }, [value]);

  const handleChange = (newValue: string) => {
    if (newValue.length <= maxLength) {
      onChange(newValue);
    }
  };

  const isNearLimit = charCount > maxLength * 0.8;
  const isOverMinLength = charCount >= minLength;

  return (
    <div className="space-y-3 sm:space-y-4 w-full">
      <Label htmlFor={`text-${questionId}`} className="text-right block text-sm sm:text-base font-medium">
        {strings.exam.placeholders.textAnswer}
      </Label>
      <Textarea
        id={`text-${questionId}`}
        value={value}
        onChange={(e) => handleChange(e.target.value)}
        placeholder="اكتب إجابتك هنا..."
        className="min-h-[120px] sm:min-h-[140px] resize-y w-full text-sm sm:text-base leading-relaxed focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
        rows={4}
      />
      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-2 text-xs sm:text-sm">
        <div className={`order-2 sm:order-1 text-right sm:text-left ${isNearLimit ? 'text-warning' : 'text-muted-foreground'}`}>
          {charCount} / {maxLength} حرف
        </div>
        <div className={`order-1 sm:order-2 text-right ${isOverMinLength ? 'text-green-600' : 'text-muted-foreground'}`}>
          {isOverMinLength ? '✓ طول مناسب' : `الحد الأدنى ${minLength} حرف`}
        </div>
      </div>
      {isNearLimit && (
        <p className="text-warning text-xs sm:text-sm text-right bg-warning/10 p-2 rounded-lg">
          اقتربت من الحد الأقصى للأحرف
        </p>
      )}
    </div>
  );
}