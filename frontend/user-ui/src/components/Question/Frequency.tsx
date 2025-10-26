import { RadioGroup, RadioGroupItem } from "../ui/radio-group";
import { Label } from "../ui/label";
import { strings } from "../../lib/strings";

interface FrequencyProps {
  value: string;
  onChange: (value: string) => void;
  questionId: string;
  options?: Array<{ value: string; label: string }>;
}

const DEFAULT_FREQUENCY_OPTIONS = [
  { value: "1", label: strings.frequency.never },
  { value: "2", label: strings.frequency.rarely },
  { value: "3", label: strings.frequency.sometimes },
  { value: "4", label: strings.frequency.often },
  { value: "5", label: strings.frequency.always },
];

export default function Frequency({ value, onChange, questionId, options }: FrequencyProps) {
  // Use provided options if available, otherwise use defaults
  const frequencyOptions = options && options.length > 0 ? options : DEFAULT_FREQUENCY_OPTIONS;
  
  return (
    <div className="space-y-3 sm:space-y-4">
      <RadioGroup 
        value={value} 
        onValueChange={onChange}
        className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4"
      >
        {frequencyOptions.map((option, index) => (
          <div 
            key={`${questionId}-freq-${index}`} 
            className={`flex items-center gap-3 p-3 sm:p-4 min-h-12 rounded-xl border transition-all duration-200 cursor-pointer hover:bg-muted/50 hover:shadow-soft ${
              value === option.value 
                ? 'border-primary bg-primary/5 ring-1 ring-primary/20 shadow-soft' 
                : 'border-border hover:border-primary/30'
            }`}
            onClick={() => onChange(option.value)}
          >
            <RadioGroupItem 
              value={option.value} 
              id={`freq-${questionId}-${index}`}
              className="flex-shrink-0 min-w-[20px] min-h-[20px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            />
            <Label 
              htmlFor={`freq-${questionId}-${index}`} 
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