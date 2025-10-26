import { RadioGroup, RadioGroupItem } from "../ui/radio-group";
import { Label } from "../ui/label";
import { strings } from "../../lib/strings";

interface LikertProps {
  value: string;
  onChange: (value: string) => void;
  questionId: string;
  options?: Array<{ value: string; label: string }>;
}

// SDJ mode: Store numeric values (1-5) while displaying Arabic labels
const DEFAULT_LIKERT_OPTIONS = [
  { value: "1", label: strings.likert.stronglyDisagree },
  { value: "2", label: strings.likert.disagree },
  { value: "3", label: strings.likert.neutral },
  { value: "4", label: strings.likert.agree },
  { value: "5", label: strings.likert.stronglyAgree },
];

export default function Likert({ value, onChange, questionId, options }: LikertProps) {
  // Use provided options if available, otherwise use defaults
  const likertOptions = options && options.length > 0 ? options : DEFAULT_LIKERT_OPTIONS;
  return (
    <div className="space-y-3 sm:space-y-4">
      <RadioGroup 
        value={value} 
        onValueChange={onChange}
        className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4"
      >
        {likertOptions.map((option, index) => (
          <div 
            key={`${questionId}-likert-${index}`} 
            className={`flex items-center gap-3 p-3 sm:p-4 min-h-12 rounded-xl border transition-all duration-200 cursor-pointer hover:bg-muted/50 hover:shadow-soft ${
              value === option.value 
                ? 'border-primary bg-primary/5 ring-1 ring-primary/20 shadow-soft' 
                : 'border-border hover:border-primary/30'
            }`}
            onClick={() => onChange(option.value)}
          >
            <RadioGroupItem 
              value={option.value} 
              id={`likert-${questionId}-${index}`}
              className="flex-shrink-0 min-w-[20px] min-h-[20px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            />
            <Label 
              htmlFor={`likert-${questionId}-${index}`} 
              className="cursor-pointer text-right font-medium flex-1 text-sm sm:text-base break-words leading-relaxed"
            >
              {option.label}
            </Label>
          </div>
        ))}
      </RadioGroup>
    </div>
  );
}