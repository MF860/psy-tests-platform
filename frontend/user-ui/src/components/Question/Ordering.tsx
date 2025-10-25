import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';

interface OrderingProps {
  value?: string;
  onChange: (value: string) => void;
  questionId: string;
  text?: string;
  choices?: string[];
  labels?: string[];
}

export default function Ordering({ choices, labels, value, onChange, questionId }: OrderingProps) {
  console.log('Ordering question data:', { questionId, choices, labels, value });
  
  // If we have choices and labels, create multiple choice options
  // Backend expects us to send the selected label (A, B, C, D), not a sequence
  if (choices && labels && choices.length === labels.length && choices.length > 0) {
    const multipleChoiceOptions = choices.map((choice, index) => ({
      value: labels[index], // Send the label (A, B, C, D) as the answer
      label: choice // Display the full choice text
    }));

    return (
      <div className="space-y-3 sm:space-y-4">
        <p className="text-sm sm:text-base text-muted-foreground">
          اختر الترتيب الصحيح من الخيارات التالية:
        </p>
        <RadioGroup 
          value={value} 
          onValueChange={onChange}
          className="grid grid-cols-1 gap-3 sm:gap-4"
        >
          {multipleChoiceOptions.map((option, index) => (
            <div 
              key={`${questionId}-choice-${index}`} 
              className={`flex items-start gap-3 p-3 sm:p-4 min-h-12 rounded-xl border transition-all duration-200 cursor-pointer hover:bg-muted/50 hover:shadow-soft ${
                value === option.value 
                  ? 'border-primary bg-primary/5 ring-1 ring-primary/20 shadow-soft' 
                  : 'border-border hover:border-primary/30'
              }`}
              onClick={() => onChange(option.value)}
            >
              <RadioGroupItem 
                value={option.value} 
                id={`choice-${questionId}-${index}`}
                className="flex-shrink-0 min-w-[20px] min-h-[20px] mt-0.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
              />
              <Label 
                htmlFor={`choice-${questionId}-${index}`} 
                className="cursor-pointer text-right flex-1 text-sm sm:text-base break-words leading-relaxed"
              >
                <span className="font-medium text-primary mr-2">{option.value}.</span>
                {option.label}
              </Label>
            </div>
          ))}
        </RadioGroup>
      </div>
    );
  }

  // Fallback for legacy ordering questions without proper labels
  const fallbackOptions = [
    { value: "الخيار الأول", label: "الخيار الأول" },
    { value: "الخيار الثاني", label: "الخيار الثاني" },
    { value: "الخيار الثالث", label: "الخيار الثالث" },
    { value: "الخيار الرابع", label: "الخيار الرابع" },
  ];

  return (
    <div className="space-y-3 sm:space-y-4">
      <p className="text-sm sm:text-base text-muted-foreground">
        اختر الترتيب المناسب من الخيارات التالية:
      </p>
      <RadioGroup 
        value={value} 
        onValueChange={onChange}
        className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4"
      >
        {fallbackOptions.map((option, index) => (
          <div 
            key={`${questionId}-order-${index}`} 
            className={`flex items-center gap-3 p-3 sm:p-4 min-h-12 rounded-xl border transition-all duration-200 cursor-pointer hover:bg-muted/50 hover:shadow-soft ${
              value === option.value 
                ? 'border-primary bg-primary/5 ring-1 ring-primary/20 shadow-soft' 
                : 'border-border hover:border-primary/30'
            }`}
            onClick={() => onChange(option.value)}
          >
            <RadioGroupItem 
              value={option.value} 
              id={`order-${questionId}-${index}`}
              className="flex-shrink-0 min-w-[20px] min-h-[20px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            />
            <Label 
              htmlFor={`order-${questionId}-${index}`} 
              className="cursor-pointer text-right flex-1 text-sm sm:text-base break-words leading-relaxed"
            >
              {option.label}
            </Label>
          </div>
        ))}
      </RadioGroup>
    </div>
  );
}