import { Clock, Timer, Shield, CheckCircle, MousePointer2, Type, Hash, List } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { strings } from "../lib/strings";

function InstructionsContent() {
  const { setInstructionsCompleted } = useFlowState();

  const handleStartExam = () => {
    // Update flow state - GuardedRoute will handle navigation
    setInstructionsCompleted(true);
  };

  const mainInstructions = [
    {
      icon: Clock,
      text: "مدة الاختبار الإجمالية: 60 دقيقة",
      color: "text-blue-600"
    },
    {
      icon: Timer,
      text: "لكل سؤال وقت محدد يظهر أعلى السؤال (غالباً 45 ثانية لأسئلة التقييم الذاتي)",
      color: "text-orange-600"
    },
    {
      icon: Shield,
      text: "لا تغلق الصفحة أثناء الحل. يمكنك الاستئناف إذا انقطع الاتصال",
      color: "text-green-600"
    }
  ];

  const questionTypes = [
    {
      icon: MousePointer2,
      title: "اختيار من متعدد / التكرار / ليكرت",
      description: "اختر إجابة واحدة من الخيارات المتاحة"
    },
    {
      icon: List,
      title: "ترتيب (Ordering)",
      description: "اسحب ورتّب الخيارات (يوجد بديل بدون سحب للأجهزة القديمة)"
    },
    {
      icon: Hash,
      title: "رقمي",
      description: "أدخل قيمة رقمية فقط"
    },
    {
      icon: Type,
      title: "نصي",
      description: "أجب بإيجاز ووضوح"
    }
  ];

  return (
    <div className="relative min-h-screen w-screen overflow-y-auto bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900">
      {/* Animated background particles */}
      <div className="absolute inset-0 opacity-20">
        <div className="absolute top-20 left-20 w-72 h-72 bg-blue-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse"></div>
        <div className="absolute top-40 right-20 w-72 h-72 bg-purple-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-2000"></div>
        <div className="absolute bottom-20 left-1/3 w-72 h-72 bg-teal-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-4000"></div>
      </div>

      {/* Header with logos - Responsive */}
      <header className="absolute top-0 w-full flex justify-between items-center p-4 sm:p-6 md:p-8 z-10" dir="rtl">
        <div className="flex-1 hidden sm:block"></div>
        <div className="flex-1 flex justify-center">
          <img 
            src="/STEST.png" 
            alt="شعار المنصة" 
            className="h-12 sm:h-14 md:h-16 object-contain drop-shadow-lg"
          />
        </div>
        <div className="flex-1 flex justify-end items-center gap-2 sm:gap-4">
          <div className="px-2.5 sm:px-3 md:px-4 py-1.5 sm:py-2 bg-white/10 backdrop-blur-sm border border-white/20 rounded-full text-blue-300 text-xs sm:text-sm font-medium">
            التعليمات
          </div>
          <img 
            src="/FB-ICON.png" 
            alt="أيقونة" 
            className="h-8 sm:h-10 md:h-12 object-contain drop-shadow-lg"
          />
        </div>
      </header>

      {/* Main content - Responsive */}
      <main className="relative z-10 pt-24 sm:pt-28 md:pt-32 pb-8 sm:pb-10 md:pb-12 px-3 sm:px-4">
        <div className="max-w-5xl mx-auto space-y-6 sm:space-y-8">
          {/* Welcome Section */}
          <div className="text-center space-y-3 sm:space-y-4 mb-8 sm:mb-10 md:mb-12">
            <h1 className="text-2xl sm:text-3xl md:text-4xl font-bold text-white leading-tight" dir="rtl">
              {strings.instructions.title}
            </h1>
            <p className="text-base sm:text-lg text-gray-300 max-w-2xl mx-auto leading-relaxed px-2" dir="rtl">
              {strings.instructions.subtitle}
            </p>
          </div>

          {/* Main Instructions Grid - Responsive */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-5 md:gap-6">
            {mainInstructions.map((instruction, index) => {
              const IconComponent = instruction.icon;
              return (
                <div 
                  key={index} 
                  className="bg-white/10 backdrop-blur-lg rounded-xl p-4 sm:p-5 md:p-6 border border-white/20 hover:bg-white/15 transition-all duration-300"
                >
                  <div className="flex flex-col items-center text-center space-y-3 sm:space-y-4" dir="rtl">
                    <div className={`w-12 h-12 sm:w-14 sm:h-14 md:w-16 md:h-16 rounded-full flex items-center justify-center ${instruction.color.replace('text-', 'bg-').replace('600', '500')}/20`}>
                      <IconComponent className={`w-6 h-6 sm:w-7 sm:h-7 md:w-8 md:h-8 ${instruction.color.replace('600', '400')}`} />
                    </div>
                    <p className="text-white font-medium leading-relaxed text-sm sm:text-base">
                      {instruction.text}
                    </p>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Question Types - Responsive */}
          <div className="bg-white/10 backdrop-blur-lg rounded-xl sm:rounded-2xl border border-white/20 overflow-hidden">
            <div className="p-4 sm:p-5 md:p-6 text-center border-b border-white/20">
              <h2 className="text-lg sm:text-xl font-semibold text-white" dir="rtl">
                {strings.instructions.questionTypes.title}
              </h2>
            </div>
            <div className="p-4 sm:p-5 md:p-6">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
                {questionTypes.map((type, index) => {
                  const IconComponent = type.icon;
                  return (
                    <div 
                      key={index} 
                      className="bg-white/5 rounded-lg p-3 sm:p-4 border border-white/10 hover:bg-white/10 transition-all duration-300"
                    >
                      <div className="space-y-1.5 sm:space-y-2" dir="rtl">
                        <div className="flex items-center gap-2">
                          <IconComponent className="h-4 w-4 sm:h-5 sm:w-5 text-blue-400 flex-shrink-0" />
                          <h4 className="font-semibold text-xs sm:text-sm text-white">{type.title}</h4>
                        </div>
                        <p className="text-[10px] sm:text-xs text-gray-300 leading-relaxed">
                          {type.description}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Final Note - Responsive */}
          <div className="bg-gradient-to-r from-blue-500/20 to-teal-500/20 backdrop-blur-sm rounded-xl p-4 sm:p-5 md:p-6 border border-blue-400/30">
            <div className="flex items-start gap-2.5 sm:gap-3" dir="rtl">
              <CheckCircle className="h-5 w-5 sm:h-6 sm:w-6 text-teal-400 flex-shrink-0 mt-1" />
              <p className="text-white font-medium leading-relaxed text-sm sm:text-base">
                {strings.instructions.finalNote}
              </p>
            </div>
          </div>

          {/* Start Button - Responsive */}
          <div className="text-center space-y-2 sm:space-y-3 pt-3 sm:pt-4">
            <button 
              onClick={handleStartExam}
              className="bg-gradient-to-r from-blue-500 to-teal-400 text-white font-bold py-3 sm:py-4 px-8 sm:px-12 rounded-lg shadow-lg hover:scale-105 hover:shadow-2xl transition-all duration-300 flex items-center gap-2 sm:gap-3 mx-auto text-base sm:text-lg"
            >
              <CheckCircle className="w-5 h-5 sm:w-6 sm:h-6" />
              <span>{strings.instructions.startButton}</span>
            </button>
            <p className="text-[10px] sm:text-xs text-gray-400 px-2" dir="rtl">
              بالضغط على "ابدأ الاختبار" فإنك تؤكد قراءتك لجميع التعليمات
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}

export default function Instructions() {
  return (
    <GuardedRoute page="instructions">
      <InstructionsContent />
    </GuardedRoute>
  );
}