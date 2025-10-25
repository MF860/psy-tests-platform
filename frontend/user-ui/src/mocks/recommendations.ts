/**
 * Mock AI recommendations based on dimension scores
 * Used when VITE_OPENAI_KEY is not available
 */

export interface MockRecommendation {
  resultId: number;
  participantId: string;
  generatedAt: string;
  modelVersion: string;
  fromCache: boolean;
  summary: {
    profileType: string;
    description: string;
    overallScore: number;
    performanceLevel: string;
    keyCharacteristics: string[];
  };
  strengths: Array<{
    dimension: string;
    tScore: number;
    title: string;
    description: string;
    applications: string[];
    reinforcementTip: string;
  }>;
  growthAreas: Array<{
    dimension: string;
    tScore: number;
    title: string;
    challenge: string;
    improvementStrategies: string[];
    priority: string;
  }>;
  recommendations: Array<{
    category: string;
    title: string;
    description: string;
    actionSteps: string[];
    timeline: string;
    priority: string;
    expectedOutcomes: string[];
  }>;
  courses: Array<{
    name: string;
    description: string;
    provider: string;
    duration: string;
    level: string;
    topics: string[];
    relevancyReason: string;
    externalUrl?: string;
  }>;
}

/**
 * Generate mock recommendations based on dimension scores
 */
export function generateMockRecommendations(
  resultId: number,
  nationalId: string,
  dimensions: Array<{ dimension: string; t: number }>
): MockRecommendation {
  
  const totalScore = dimensions.reduce((sum, dim) => sum + dim.t, 0) / dimensions.length;
  
  // Determine profile type based on overall score
  let profileType: string;
  let performanceLevel: string;
  
  if (totalScore >= 65) {
    profileType = "القائد المبدع";
    performanceLevel = "متميز";
  } else if (totalScore >= 60) {
    profileType = "المفكر التحليلي";
    performanceLevel = "جيد جداً";
  } else if (totalScore >= 55) {
    profileType = "المنفذ المتوازن";
    performanceLevel = "جيد";
  } else if (totalScore >= 50) {
    profileType = "الباحث عن النمو";
    performanceLevel = "مقبول";
  } else {
    profileType = "البناء التدريجي";
    performanceLevel = "يحتاج تطوير";
  }

  // Get strengths (dimensions with T-score >= 60)
  const strengths = dimensions
    .filter(dim => dim.t >= 60)
    .map(dim => generateStrengthInsight(dim.dimension, dim.t));

  // Get growth areas (dimensions with T-score < 55)
  const growthAreas = dimensions
    .filter(dim => dim.t < 55)
    .map(dim => generateGrowthArea(dim.dimension, dim.t));

  // Generate recommendations based on profile
  const recommendations = generateRecommendationsList(profileType, totalScore);
  
  // Generate course recommendations
  const courses = generateCourseRecommendations(strengths, growthAreas);

  return {
    resultId,
    participantId: nationalId,
    generatedAt: new Date().toISOString(),
    modelVersion: "Mock-AI-v1.0",
    fromCache: false,
    summary: {
      profileType,
      description: generateProfileDescription(profileType, totalScore),
      overallScore: Math.round(totalScore * 10) / 10,
      performanceLevel,
      keyCharacteristics: generateKeyCharacteristics(profileType)
    },
    strengths,
    growthAreas,
    recommendations,
    courses
  };
}

function generateStrengthInsight(dimension: string, tScore: number) {
  const strengthsMap: Record<string, any> = {
    "الذكاء العام": {
      title: "قدرة تحليلية متميزة",
      description: "تتمتع بقدرة عالية على التفكير النقدي وحل المشاكل المعقدة بطريقة منهجية ومبدعة.",
      applications: [
        "القيادة الاستراتيجية في المؤسسات",
        "تحليل البيانات واتخاذ القرارات المهمة",
        "التدريب والتطوير للآخرين"
      ],
      reinforcementTip: "استمر في تحدي نفسك بمشاكل معقدة وشارك معرفتك مع الآخرين"
    },
    "الاستقرار العاطفي": {
      title: "تنظيم عاطفي ممتاز",
      description: "تتميز بقدرة عالية على إدارة المشاعر والحفاظ على الهدوء تحت الضغط.",
      applications: [
        "إدارة الفرق في المواقف الصعبة",
        "التفاوض والوساطة",
        "العمل في البيئات عالية الضغط"
      ],
      reinforcementTip: "استخدم هذه القدرة لمساعدة الآخرين في إدارة ضغوطهم"
    },
    "الانفتاح على التجارب": {
      title: "عقلية إبداعية منفتحة",
      description: "تتمتع بحب الاستطلاع والرغبة في تجربة أفكار وأساليب جديدة.",
      applications: [
        "الابتكار وتطوير المنتجات",
        "قيادة التغيير في المؤسسات",
        "البحث والتطوير"
      ],
      reinforcementTip: "استكشف مجالات جديدة باستمرار وشارك في مشاريع إبداعية"
    },
    "الضمير الحي": {
      title: "انضباط ومسؤولية عالية",
      description: "تتميز بالالتزام والدقة في العمل والقدرة على إنجاز المهام بجودة عالية.",
      applications: [
        "إدارة المشاريع الكبيرة",
        "ضمان الجودة والامتثال",
        "التخطيط الاستراتيجي طويل المدى"
      ],
      reinforcementTip: "كن قدوة للآخرين واستخدم تنظيمك لتحسين كفاءة الفريق"
    },
    "الانبساطية": {
      title: "طاقة اجتماعية إيجابية",
      description: "تتمتع بقدرة طبيعية على التواصل وإلهام الآخرين.",
      applications: [
        "القيادة والإدارة",
        "المبيعات والتسويق",
        "التدريب والعروض التقديمية"
      ],
      reinforcementTip: "استغل طاقتك الاجتماعية لبناء شبكات قوية وتحفيز الفرق"
    }
  };

  const base = strengthsMap[dimension] || {
    title: "نقطة قوة مميزة",
    description: `تظهر أداءً متميزاً في ${dimension}`,
    applications: ["تطبيقات متنوعة في العمل"],
    reinforcementTip: "استمر في تطوير هذه النقطة"
  };

  return {
    dimension,
    tScore,
    ...base
  };
}

function generateGrowthArea(dimension: string, tScore: number) {
  const growthMap: Record<string, any> = {
    "الذكاء العام": {
      title: "تطوير القدرات التحليلية",
      challenge: "قد تحتاج لمزيد من الممارسة في التفكير النقدي وحل المشاكل المعقدة.",
      improvementStrategies: [
        "حل الألغاز والمسائل التحليلية يومياً",
        "قراءة كتب في المنطق والتفكير النقدي",
        "المشاركة في نقاشات فكرية متنوعة"
      ],
      priority: tScore < 45 ? "عالية" : "متوسطة"
    },
    "الاستقرار العاطفي": {
      title: "إدارة المشاعر والضغوط",
      challenge: "قد تواجه صعوبة في إدارة المشاعر تحت الضغط.",
      improvementStrategies: [
        "ممارسة تقنيات التأمل والاسترخاء",
        "تطوير استراتيجيات إدارة الضغط",
        "طلب الدعم عند الحاجة"
      ],
      priority: tScore < 45 ? "عالية" : "متوسطة"
    },
    "الانفتاح على التجارب": {
      title: "توسيع الآفاق والتجارب",
      challenge: "قد تميل للالتزام بالطرق المعتادة وتجنب التغيير.",
      improvementStrategies: [
        "تجربة أنشطة وهوايات جديدة",
        "السفر واستكشاف ثقافات مختلفة",
        "قراءة كتب في مجالات متنوعة"
      ],
      priority: "متوسطة"
    },
    "الضمير الحي": {
      title: "تحسين التنظيم والانضباط",
      challenge: "قد تحتاج لتحسين مهارات إدارة الوقت والتنظيم.",
      improvementStrategies: [
        "استخدام أدوات التخطيط والتنظيم",
        "وضع روتين يومي واضح",
        "تقسيم المهام الكبيرة لخطوات صغيرة"
      ],
      priority: tScore < 45 ? "عالية" : "متوسطة"
    },
    "الانبساطية": {
      title: "تطوير المهارات الاجتماعية",
      challenge: "قد تحتاج لتحسين مهارات التواصل والتفاعل الاجتماعي.",
      improvementStrategies: [
        "الانضمام لمجموعات اجتماعية ومهنية",
        "ممارسة العروض التقديمية",
        "تطوير مهارات الاستماع الفعال"
      ],
      priority: "متوسطة"
    }
  };

  const base = growthMap[dimension] || {
    title: "منطقة تطوير",
    challenge: `يمكن تطوير ${dimension} للوصول لمستوى أفضل`,
    improvementStrategies: ["وضع خطة تطوير محددة"],
    priority: "متوسطة"
  };

  return {
    dimension,
    tScore,
    ...base
  };
}

function generateRecommendationsList(profileType: string, totalScore: number) {
  const baseRecommendations = [
    {
      category: "التطوير المهني",
      title: `خطة التطوير الشخصي للـ${profileType}`,
      description: "وضع خطة تطوير شاملة تركز على نقاط القوة وتعالج مناطق التحسين.",
      actionSteps: [
        "تحديد الأهداف المهنية قصيرة وطويلة المدى",
        "اختيار برامج تدريبية متخصصة",
        "إيجاد موجه مهني (Mentor) في مجال عملك",
        "المشاركة في مشاريع تطبق المهارات الجديدة"
      ],
      timeline: totalScore >= 60 ? "3-6 أشهر" : "6-12 شهر",
      priority: "عالية",
      expectedOutcomes: [
        "وضوح في المسار المهني",
        "تحسن ملحوظ في الأداء",
        "زيادة الثقة بالنفس"
      ]
    },
    {
      category: "المهارات الاجتماعية",
      title: "تطوير مهارات التواصل",
      description: "تعزيز قدرتك على التواصل الفعال والتأثير الإيجابي.",
      actionSteps: [
        "حضور ورش عمل في التواصل الفعال",
        "ممارسة العروض التقديمية بانتظام",
        "تطوير مهارات الاستماع النشط",
        "بناء شبكة مهنية قوية"
      ],
      timeline: "2-4 أشهر",
      priority: totalScore >= 55 ? "متوسطة" : "عالية",
      expectedOutcomes: [
        "تحسن في العلاقات المهنية",
        "زيادة التأثير والحضور",
        "فرص مهنية أفضل"
      ]
    }
  ];

  if (totalScore >= 60) {
    baseRecommendations.push({
      category: "القيادة",
      title: "تطوير المهارات القيادية",
      description: "الاستفادة من نقاط قوتك لتطوير قدرات قيادية متميزة.",
      actionSteps: [
        "حضور برامج تطوير القادة",
        "تولي مسؤوليات قيادية في مشاريع",
        "دراسة نماذج قيادية ناجحة",
        "ممارسة التدريب والإرشاد للآخرين"
      ],
      timeline: "6-12 شهر",
      priority: "عالية",
      expectedOutcomes: [
        "مهارات قيادية متقدمة",
        "قدرة على إلهام الفرق",
        "فرص ترقية مهنية"
      ]
    });
  }

  return baseRecommendations;
}

function generateCourseRecommendations(strengths: any[], growthAreas: any[]) {
  const courses = [
    {
      name: "أساسيات القيادة الفعالة",
      description: "برنامج شامل لتطوير المهارات القيادية الأساسية والمتقدمة.",
      provider: "أكاديمية المهارات المهنية",
      duration: "8 أسابيع",
      level: strengths.length > 3 ? "متقدم" : "متوسط",
      topics: ["التواصل القيادي", "إدارة الفرق", "اتخاذ القرار", "إدارة التغيير"],
      relevancyReason: `يطور مهارات القيادة والإدارة بناءً على ${strengths.length} نقاط قوة لديك`,
      externalUrl: "https://example.com/leadership-course"
    },
    {
      name: "إدارة الذات والوقت",
      description: "تعلم تقنيات إدارة الوقت وتنظيم المهام لزيادة الإنتاجية.",
      provider: "معهد التطوير الذاتي",
      duration: "4 أسابيع",
      level: "مبتدئ",
      topics: ["تخطيط الأهداف", "إدارة الأولويات", "تقنيات الإنتاجية", "التوازن الحياتي"],
      relevancyReason: "يساعد في تحسين التنظيم والانضباط الذاتي"
    },
    {
      name: "الذكاء العاطفي في العمل",
      description: "تطوير مهارات الذكاء العاطفي للتميز في البيئة المهنية.",
      provider: "مركز التطوير المهني",
      duration: "6 أسابيع",
      level: "متوسط",
      topics: ["إدارة المشاعر", "التعاطف", "المهارات الاجتماعية", "الدافعية الذاتية"],
      relevancyReason: "يعزز الاستقرار العاطفي ومهارات التعامل مع الآخرين"
    }
  ];

  // Add specific courses based on growth areas
  if (growthAreas.some(area => area.dimension === "الانفتاح على التجارب")) {
    courses.push({
      name: "التفكير الإبداعي والابتكار",
      description: "تطوير قدرات التفكير الإبداعي وتقنيات الابتكار.",
      provider: "مؤسسة الإبداع والابتكار",
      duration: "5 أسابيع",
      level: "متقدم",
      topics: ["تقنيات العصف الذهني", "حل المشاكل الإبداعي", "الابتكار في العمل", "التفكير خارج الصندوق"],
      relevancyReason: "يطور الانفتاح على التجارب الجديدة والتفكير الإبداعي"
    });
  }

  return courses;
}

function generateProfileDescription(profileType: string, totalScore: number): string {
  const descriptions: Record<string, string> = {
    "القائد المبدع": "شخصية قيادية تتمتع بقدرات استثنائية في التفكير الاستراتيجي والإبداع. يتميز بالقدرة على إلهام الآخرين وقيادة التغيير الإيجابي.",
    "المفكر التحليلي": "شخصية تحليلية تتمتع بقدرة عالية على حل المشاكل المعقدة واتخاذ قرارات مدروسة. يركز على التفاصيل والدقة في العمل.",
    "المنفذ المتوازن": "شخصية متوازنة تتمتع بمهارات جيدة في معظم المجالات. قادر على العمل بفعالية في بيئات متنوعة ومع فرق مختلفة.",
    "الباحث عن النمو": "شخصية متحمسة للتعلم والتطوير. يسعى باستمرار لتحسين قدراته ومهاراته في المجالات المختلفة.",
    "البناء التدريجي": "شخصية تحتاج لمزيد من التطوير والدعم. مع التوجيه المناسب والجهد المستمر، يمكن تحقيق تقدم ملحوظ."
  };

  const baseDescription = descriptions[profileType] || "شخصية فريدة تتمتع بإمكانيات متنوعة للنمو والتطوير.";
  const scoreContext = totalScore >= 65 ? " تظهر أداءً متميزاً" : totalScore >= 55 ? " تظهر أداءً جيداً" : " لديها إمكانيات للتطوير";
  
  return baseDescription + scoreContext + " في التقييم الشامل.";
}

function generateKeyCharacteristics(profileType: string): string[] {
  const characteristics: Record<string, string[]> = {
    "القائد المبدع": [
      "رؤية استراتيجية واضحة",
      "قدرة على الإلهام والتحفيز",
      "مهارات تواصل متميزة",
      "تفكير إبداعي ومبتكر"
    ],
    "المفكر التحليلي": [
      "تفكير منطقي ومنهجي",
      "دقة في التحليل والتقييم",
      "قدرة على حل المشاكل المعقدة",
      "اتخاذ قرارات مدروسة"
    ],
    "المنفذ المتوازن": [
      "مرونة في التكيف",
      "مهارات تعاون جيدة",
      "أداء مستقر ومتوازن",
      "قدرة على العمل تحت الضغط"
    ],
    "الباحث عن النمو": [
      "حماس للتعلم والتطوير",
      "مرونة في التغيير",
      "قابلية للتحسن السريع",
      "روح إيجابية ومتفائلة"
    ],
    "البناء التدريجي": [
      "إمكانيات كامنة للتطوير",
      "حاجة للتوجيه والدعم",
      "قدرة على التحسن التدريجي",
      "استعداد للعمل الجاد"
    ]
  };

  return characteristics[profileType] || [
    "شخصية فريدة",
    "إمكانيات متنوعة",
    "قابلية للتطوير",
    "مسار نمو شخصي"
  ];
}