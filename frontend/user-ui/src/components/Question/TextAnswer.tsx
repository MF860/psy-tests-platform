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
      <Label htmlFor={`text-${questionId}`} className="text-right block text-sm sm:text-base font-medium text-white">
        {strings.exam.placeholders.textAnswer}
      </Label>
      <Textarea
        id={`text-${questionId}`}
        value={value}
        onChange={(e) => handleChange(e.target.value)}
        placeholder="اكتب إجابتك هنا..."
        className="min-h-[120px] sm:min-h-[140px] resize-y w-full text-sm sm:text-base leading-relaxed bg-white/10 border-2 border-white/20 text-white placeholder:text-gray-400 focus:border-blue-400 focus:bg-white/15 rounded-xl p-4"
        rows={4}
      />
      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-2 text-xs sm:text-sm">
        <div className={`order-2 sm:order-1 text-right sm:text-left ${isNearLimit ? 'text-yellow-400' : 'text-gray-400'}`}>
          {charCount} / {maxLength} حرف
        </div>
        <div className={`order-1 sm:order-2 text-right ${isOverMinLength ? 'text-green-400' : 'text-gray-400'}`}>
          {isOverMinLength ? '✓ طول مناسب' : `الحد الأدنى ${minLength} حرف`}
        </div>
      </div>
      {isNearLimit && (
        <p className="text-yellow-300 text-xs sm:text-sm text-right bg-yellow-500/20 border border-yellow-400/30 p-3 rounded-lg">
          اقتربت من الحد الأقصى للأحرف
        </p>
      )}
    </div>
  );
}