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
        className="grid grid-cols-1 gap-3"
      >
        {frequencyOptions.map((option, index) => (
          <div 
            key={`${questionId}-freq-${index}`} 
            className={`flex items-center gap-3 p-4 min-h-14 rounded-xl border-2 transition-all duration-200 cursor-pointer ${
              value === option.value 
                ? 'border-blue-400 bg-blue-500/20 backdrop-blur-sm shadow-lg' 
                : 'border-white/20 bg-white/5 hover:bg-white/10 hover:border-blue-400/50'
            }`}
            onClick={() => onChange(option.value)}
          >
            <RadioGroupItem 
              value={option.value} 
              id={`freq-${questionId}-${index}`}
              className="flex-shrink-0 min-w-[20px] min-h-[20px] border-white/30 data-[state=checked]:bg-blue-500 data-[state=checked]:border-blue-500"
            />
            <Label 
              htmlFor={`freq-${questionId}-${index}`} 
              className="cursor-pointer text-right font-medium flex-1 text-base break-words leading-relaxed text-white"
            >
              {option.label}
            </Label>
          </div>
        ))}
      </RadioGroup>
    </div>
  );
}