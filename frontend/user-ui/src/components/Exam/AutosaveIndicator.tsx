import { Check, Loader2 } from "lucide-react";

type AutosaveState = "idle" | "saving" | "saved" | "error";

interface AutosaveIndicatorProps {
  state: AutosaveState;
  className?: string;
}

export default function AutosaveIndicator({ state, className = "" }: AutosaveIndicatorProps) {
  const getContent = () => {
    switch (state) {
      case "saving":
        return (
          <>
            <Loader2 className="h-3 w-3 animate-spin" />
            <span className="text-xs font-medium">جاري الحفظ...</span>
          </>
        );
      case "saved":
        return (
          <>
            <Check className="h-3 w-3" />
            <span className="text-xs font-medium">تم الحفظ</span>
          </>
        );
      case "error":
        return (
          <>
            <span className="text-xs font-medium text-red-600">خطأ في الحفظ</span>
          </>
        );
      default:
        return null;
    }
  };

  const getStyles = () => {
    switch (state) {
      case "saving":
        return "bg-blue-500/20 text-blue-200 border-blue-400/30";
      case "saved":
        return "bg-green-500/20 text-green-200 border-green-400/30";
      case "error":
        return "bg-red-500/20 text-red-200 border-red-400/30";
      default:
        return "invisible";
    }
  };

  return (
    <div 
      className={`flex items-center gap-1.5 px-2 py-1 rounded-full border text-xs transition-all duration-200 ${getStyles()} ${className}`}
      role="status"
      aria-live="polite"
    >
      {getContent()}
    </div>
  );
}