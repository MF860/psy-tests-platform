// Arabic UI strings and messages for the psychological test platform

export const strings = {
  // Login page
  login: {
    title: "تسجيل الدخول",
    subtitle: "أدخل الرقم الوطني للمتابعة",
    nationalIdLabel: "الرقم الوطني",
    nationalIdPlaceholder: "أدخل الرقم الوطني (10 أرقام)",
    continueButton: "متابعة",
    errors: {
      required: "الرقم الوطني مطلوب",
      invalid: "يجب أن يكون الرقم الوطني 10 أرقام",
      numbersOnly: "الرقم الوطني يجب أن يحتوي على أرقام فقط",
    },
  },

  // Privacy page
  privacy: {
    title: "سياسة الخصوصية",
    content: {
      introduction: "مرحباً بك في منصة الاختبارات النفسية. نحن نلتزم بحماية خصوصيتك وبياناتك الشخصية.",
      dataCollection: "البيانات التي نجمعها:",
      dataList: [
        "الرقم الوطني للتحقق من الهوية",
        "إجابات الاختبار النفسي",
        "الوقت المستغرق لكل سؤال",
        "تاريخ ووقت إجراء الاختبار"
      ],
      usage: "نستخدم هذه البيانات لأغراض البحث العلمي وتحليل النتائج النفسية.",
      security: "نحافظ على سرية بياناتك ولا نشاركها مع أطراف ثالثة إلا بموافقتك الصريحة.",
      rights: "لك الحق في الحصول على نسخة من بياناتك أو طلب حذفها في أي وقت.",
    },
    consentText: "أوافق على سياسة الخصوصية وأسمح بجمع واستخدام بياناتي للأغراض المذكورة أعلاه",
    continueButton: "أوافق وأتابع",
  },

  // Instructions page
  instructions: {
    title: "تعليمات الاختبار",
    subtitle: "الرجاء قراءة التعليمات التالية بعناية قبل البدء",
    content: [
      {
        icon: "clock",
        text: "مدة الاختبار الإجمالية: 60 دقيقة"
      },
      {
        icon: "timer",
        text: "لكل سؤال وقت محدد يظهر أعلى السؤال"
      },
      {
        icon: "shield",
        text: "لا تغلق الصفحة أثناء الحل. يمكنك الاستئناف إذا انقطع الاتصال"
      }
    ],
    questionTypes: {
      title: "أنواع الأسئلة وكيفية الإجابة:",
      types: [
        "اختيار من متعدد / التكرار (Frequency) / ليكرت: اختر إجابة واحدة",
        "ترتيب (Ordering): اسحب ورتّب الخيارات (يوجد بديل بدون سحب للأجهزة القديمة)",
        "رقمي: أدخل قيمة رقمية فقط",
        "نصي: أجب بإيجاز ووضوح"
      ]
    },
    finalNote: "عند آخر سؤال سيكون زر إنهاء الاختبار",
    startButton: "ابدأ الاختبار",
  },

  // Exam page
  exam: {
    progress: "السؤال {{current}} من {{total}}",
    timeLeft: "الوقت المتبقي",
    globalTimer: "الوقت المتبقي للاختبار",
    questionTimer: "الوقت المتبقي للسؤال",
    navigation: {
      previous: "السابق",
      next: "التالي",
      finish: "إنهاء الاختبار",
      lastQuestion: "السؤال الأخير"
    },
    loading: "جاري التحميل...",
    submitting: "جاري إرسال الإجابة...",
    finishing: "جاري إنهاء الاختبار...",
    confirmFinish: {
      title: "تأكيد إنهاء الاختبار",
      message: "هل أنت متأكد من إنهاء الاختبار؟ لن تتمكن من العودة بعد الإرسال.",
      confirm: "إنهاء الاختبار",
      cancel: "إلغاء"
    },
    resume: {
      banner: "تم استئناف الاختبار من حيث توقفت"
    },
    placeholders: {
      textAnswer: "اكتب إجاباتك هنا",
      numericAnswer: "أدخل الرقم المطلوب",
      orderingAnswer: "أدخل الترتيب المطلوب (مثال: 3|1|2)"
    },
    errors: {
      loadingQuestion: "حدث خطأ أثناء جلب السؤال. حاول مرة أخرى.",
      submittingAnswer: "حدث خطأ أثناء إرسال الإجابة. حاول مرة أخرى.",
      finishingExam: "حدث خطأ أثناء إنهاء الاختبار. الرجاء المحاولة مرة أخرى.",
      invalidNumeric: "القيمة يجب أن تكون رقمية",
      alreadyAnswered: "هذا السؤال تم الإجابة عليه مسبقاً"
    }
  },

  // Finish page
  finish: {
    title: "تم إنهاء الاختبار بنجاح",
    subtitle: "شكراً لك على إكمال الاختبار النفسي",
    sessionInfo: {
      code: "رقم الجلسة: {{code}}",
      timestamp: "تاريخ الإكمال: {{timestamp}}"
    },
    actions: {
      home: "العودة للصفحة الرئيسية",
      reports: "عرض التقارير لاحقاً من صفحة الحساب"
    },
    message: "تم إرسال إجاباتك بنجاح وسيتم معالجة النتائج خلال 24 ساعة."
  },

  // Likert scale labels
  likert: {
    stronglyDisagree: "لا أوافق بشدة",
    disagree: "لا أوافق",
    neutral: "محايد",
    agree: "أوافق",
    stronglyAgree: "أوافق بشدة"
  },

  // Frequency labels  
  frequency: {
    never: "أبدًا",
    rarely: "نادرًا",
    sometimes: "أحيانًا",
    often: "غالبًا",
    always: "دائمًا"
  },

  // Common UI elements
  common: {
    loading: "جاري التحميل...",
    error: "حدث خطأ",
    retry: "إعادة المحاولة",
    close: "إغلاق",
    confirm: "تأكيد",
    cancel: "إلغاء",
    save: "حفظ",
    required: "مطلوب",
    optional: "اختياري"
  },

  // Time formatting
  time: {
    seconds: "ث",
    minutes: "د",
    hours: "س"
  }
};

export const formatTime = (seconds: number): string => {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
};

export const formatProgress = (current: number, total: number): string => {
  return strings.exam.progress.replace('{{current}}', current.toString()).replace('{{total}}', total.toString());
};

export const formatSessionCode = (code: string): string => {
  return strings.finish.sessionInfo.code.replace('{{code}}', code);
};

export const formatTimestamp = (timestamp: string): string => {
  const date = new Date(timestamp);
  const formatted = date.toLocaleString('ar-SA', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
  return strings.finish.sessionInfo.timestamp.replace('{{timestamp}}', formatted);
};