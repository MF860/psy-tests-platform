import { CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { Clock, Timer, Shield, CheckCircle, MousePointer2, Type, Hash, List } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { strings } from "../lib/strings";
import UserHeader from "../components/layout/UserHeader";
import ScreenContainer from "../components/layout/ScreenContainer";
import SectionCard from "../components/layout/SectionCard";

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
    <>
      <UserHeader showStep stepLabel="التعليمات" />
      <main className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
        <ScreenContainer maxWidth="2xl" className="section-spacing">
          <div className="space-2xl">
            {/* Welcome Section */}
            <div className="text-center space-lg">
              <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-slate-900 leading-tight">
                {strings.instructions.title}
              </h1>
              <p className="text-base sm:text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                {strings.instructions.subtitle}
              </p>
            </div>

            {/* Main Instructions Grid */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-md">
              {mainInstructions.map((instruction, index) => {
                const IconComponent = instruction.icon;
                return (
                  <SectionCard key={index} className="space-md hover:shadow-lg transition-all duration-200">
                    <div className="flex items-start space-md">
                      <div className="flex-shrink-0">
                        <div className={`w-12 h-12 ${instruction.color.replace('text-', 'bg-').replace('600', '100')} rounded-xl flex items-center justify-center`}>
                          <IconComponent className={`w-6 h-6 ${instruction.color}`} />
                        </div>
                      </div>
                      <div className="flex-1">
                        <p className="text-sm font-medium text-slate-900 leading-relaxed">
                          {instruction.text}
                        </p>
                      </div>
                    </div>
                  </SectionCard>
                );
              })}
            </div>

            {/* Question Types */}
            <SectionCard>
              <CardHeader className="text-center space-sm">
                <CardTitle className="text-lg font-semibold text-slate-900">
                  {strings.instructions.questionTypes.title}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-md">
                  {questionTypes.map((type, index) => {
                    const IconComponent = type.icon;
                    return (
                      <div key={index} className="space-sm p-4 border border-border rounded-xl hover:bg-muted/30 transition-colors duration-200">
                        <div className="flex items-center space-sm">
                          <IconComponent className="h-5 w-5 text-primary flex-shrink-0" />
                          <h4 className="font-semibold text-sm text-slate-900">{type.title}</h4>
                        </div>
                        <p className="text-xs text-muted-foreground leading-relaxed">
                          {type.description}
                        </p>
                      </div>
                    );
                  })}
                </div>
              </CardContent>
            </SectionCard>

            {/* Final Note */}
            <SectionCard className="border-primary/20 bg-primary/5">
              <CardContent className="space-sm">
                <div className="flex items-start space-sm">
                  <CheckCircle className="h-5 w-5 text-primary flex-shrink-0 mt-1" />
                  <p className="text-primary font-medium text-sm leading-relaxed">
                    {strings.instructions.finalNote}
                  </p>
                </div>
              </CardContent>
            </SectionCard>

            {/* Start Button */}
            <div className="text-center space-sm">
              <Button 
                onClick={handleStartExam}
                size="lg"
                className="touch-target text-lg font-semibold bg-primary hover:bg-primary/90 transition-colors duration-200 px-8"
              >
                <CheckCircle className="w-5 h-5 me-2" />
                {strings.instructions.startButton}
              </Button>
              <p className="text-xs text-muted-foreground">
                بالضغط على "ابدأ الاختبار" فإنك تؤكد قراءتك لجميع التعليمات
              </p>
            </div>
          </div>
        </ScreenContainer>
      </main>
    </>
  );
}

export default function Instructions() {
  return (
    <GuardedRoute page="instructions">
      <InstructionsContent />
    </GuardedRoute>
  );
}