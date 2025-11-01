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
        className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4"
      >
        {options.map((option, index) => (
          <div 
            key={`${questionId}-${index}`} 
            className={`flex items-start gap-3 p-3 sm:p-4 min-h-12 rounded-xl border-2 transition-all duration-200 cursor-pointer ${
              value === option.value 
                ? 'border-blue-400 bg-blue-500/20 backdrop-blur-sm shadow-lg' 
                : 'border-white/20 bg-white/5 hover:bg-white/10 hover:border-blue-400/50'
            }`}
            onClick={() => onChange(option.value)}
          >
            <RadioGroupItem 
              value={option.value} 
              id={`mcq-${questionId}-${index}`}
              className="mt-0.5 flex-shrink-0 min-w-[20px] min-h-[20px] border-white/30 data-[state=checked]:bg-blue-500 data-[state=checked]:border-blue-500"
            />
            <Label 
              htmlFor={`mcq-${questionId}-${index}`} 
              className="cursor-pointer text-right leading-relaxed flex-1 text-sm sm:text-base break-words text-white"
            >
              {option.label}
            </Label>
          </div>
        ))}
      </RadioGroup>
    </div>
  );
}