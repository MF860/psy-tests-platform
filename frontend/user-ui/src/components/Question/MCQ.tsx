import { RadioGroup, RadioGroupItem } from "../ui/radio-group";
import { Label } from "../ui/label";

interface MCQProps {
  options: { value: string; label: string }[];
  value: string;
  onChange: (value: string) => void;
  questionId: string;
}

export default function MCQ({ options, value, onChange, questionId }: MCQProps) {
  return (
    <div className="space-y-3 sm:space-y-4">
      <RadioGroup 
        value={value} 
        onValueChange={onChange}
        className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4"
      >
        {options.map((option, index) => (
          <div 
            key={`${questionId}-${index}`} 
            className={`flex items-start gap-3 p-3 sm:p-4 min-h-12 rounded-xl border transition-all duration-200 cursor-pointer hover:bg-muted/50 hover:shadow-soft ${
              value === option.value 
                ? 'border-primary bg-primary/5 ring-1 ring-primary/20 shadow-soft' 
                : 'border-border hover:border-primary/30'
            }`}
            onClick={() => onChange(option.value)}
          >
            <RadioGroupItem 
              value={option.value} 
              id={`mcq-${questionId}-${index}`}
              className="mt-0.5 flex-shrink-0 min-w-[20px] min-h-[20px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            />
            <Label 
              htmlFor={`mcq-${questionId}-${index}`} 
              className="cursor-pointer text-right leading-relaxed flex-1 text-sm sm:text-base break-words"
            >
              {option.label}
            </Label>
          </div>
        ))}
      </RadioGroup>
    </div>
  );
}