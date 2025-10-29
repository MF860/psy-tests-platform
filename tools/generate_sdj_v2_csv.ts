/**
 * SDJ V2 Seven Patterns CSV Generator
 * Generates 210 Likert items (10 per sub-dimension, 50% reversed)
 * Based on authoritative schema: backend/PsyApi/Domain/SdjV2SevenPatterns.json
 */

import * as fs from 'fs';
import * as path from 'path';

// ============================================================================
// TYPES
// ============================================================================

interface SubDimension {
    id: string;
    key: string;
    name_ar: string;
    name_en: string;
    description_ar: string;
}

interface Pattern {
    id: string;
    key: string;
    name_ar: string;
    name_en: string;
    description_ar: string;
    order: number;
    sub_dimensions: SubDimension[];
}

interface Schema {
    version: string;
    main_patterns: Pattern[];
    items_per_sub_dimension: number;
    reverse_ratio: number;
    likert_scale: {
        min: number;
        max: number;
        type: string;
        labels_ar: string[];
    };
}

interface CsvRow {
    ItemCode: string;
    TextAr: string;
    PatternId: string;
    PatternKey: string;
    PatternNameAr: string;
    SubId: string;
    SubKey: string;
    SubNameAr: string;
    Type: string;
    Reverse: string;
    TimeLimitSeconds: string;
    Weight: string;
    CorrectAnswer?: string;  // For MCQ
    Options?: string;         // For MCQ and Likert
}

// ============================================================================
// ARABIC ITEM TEMPLATES
// ============================================================================

/**
 * Arabic item templates for each sub-dimension (with style guide from existing CSV)
 * Each template has 10 variations: 5 direct statements, 5 reversed statements
 */
const ITEM_TEMPLATES: Record<string, { direct: string[]; reverse: string[] }> = {
    // P1_S1: MBTI
    'mbti': {
        direct: [
            'أفضل العمل في بيئة منظمة ومخططة مسبقًا',
            'أستمد طاقتي من التفاعل مع الآخرين',
            'أعتمد على الحقائق والتجارب الملموسة في اتخاذ قراراتي',
            'أميل إلى اتخاذ قرارات بناءً على المنطق والتحليل الموضوعي',
            'أحب استكشاف الأفكار الجديدة والاحتمالات المختلفة'
        ],
        reverse: [
            'أجد صعوبة في الالتزام بالخطط الصارمة',
            'أشعر بالإرهاق من التفاعل الاجتماعي المستمر',
            'أفضل الأفكار المجردة على الحقائق الملموسة',
            'أتخذ قراراتي بناءً على مشاعري أكثر من المنطق',
            'أفضل التركيز على الواقع الحالي بدلاً من الاحتمالات المستقبلية'
        ]
    },
    
    // P1_S2: Big Five
    'big_five': {
        direct: [
            'أحب تجربة أشياء جديدة وغير مألوفة',
            'أنجز مهامي بدقة واهتمام بالتفاصيل',
            'أشعر بالراحة عند التواصل مع أشخاص جدد',
            'أتعاطف بسهولة مع مشاعر الآخرين',
            'أحافظ على هدوئي في المواقف الصعبة'
        ],
        reverse: [
            'أفضل الروتين على التجديد والتغيير',
            'أجد صعوبة في إكمال المهام التي بدأتها',
            'أفضل قضاء الوقت بمفردي على التجمعات الاجتماعية',
            'أركز على مصلحتي الشخصية أكثر من مصلحة الآخرين',
            'أشعر بالتوتر والقلق بسهولة في المواقف المختلفة'
        ]
    },
    
    // P1_S3: Learning Style
    'learning_style': {
        direct: [
            'أتعلم بشكل أفضل من خلال المشاهدة والرسوم التوضيحية',
            'أفضل التعلم من خلال الممارسة العملية والتجريب',
            'أستوعب المعلومات بشكل أفضل عند الاستماع والمناقشة',
            'أحتاج إلى تدوين الملاحظات لفهم المعلومات الجديدة',
            'أتعلم بفعالية عند العمل ضمن مجموعات'
        ],
        reverse: [
            'أجد صعوبة في التعلم من خلال القراءة فقط',
            'لا أفضل التعلم النظري دون تطبيق عملي',
            'أشعر بالملل من المحاضرات الطويلة',
            'أستطيع التعلم دون الحاجة لكتابة الملاحظات',
            'أفضل التعلم الذاتي على التعلم الجماعي'
        ]
    },
    
    // P2_S1: Multiple Intelligences
    'multiple_intelligences': {
        direct: [
            'أمتلك قدرة عالية على التعبير اللغوي والكتابي',
            'أستطيع حل المسائل الرياضية والمنطقية بسهولة',
            'أتخيل الأشياء والأماكن بوضوح في ذهني',
            'أتحرك بتناسق وأجيد الأنشطة الحركية',
            'أفهم مشاعر الآخرين وأتواصل معهم بفعالية'
        ],
        reverse: [
            'أجد صعوبة في التعبير عن أفكاري كتابيًا',
            'أواجه تحديات في المسائل التي تتطلب تفكيرًا منطقيًا',
            'أجد صعوبة في تخيل الأشياء المكانية',
            'لا أجيد الأنشطة التي تتطلب تنسيقًا حركيًا',
            'أجد صعوبة في فهم مشاعر الآخرين'
        ]
    },
    
    // P2_S2: Memory
    'memory': {
        direct: [
            'أتذكر التفاصيل الدقيقة للأحداث الماضية',
            'أستطيع حفظ المعلومات الجديدة بسرعة',
            'أتذكر الأسماء والوجوه بسهولة',
            'أستطيع استرجاع المعلومات عند الحاجة إليها',
            'أحتفظ بالذكريات لفترات طويلة'
        ],
        reverse: [
            'أنسى التفاصيل بسرعة بعد معرفتها',
            'أحتاج لتكرار المعلومات عدة مرات لحفظها',
            'أجد صعوبة في تذكر أسماء الأشخاص',
            'أواجه صعوبة في استرجاع المعلومات في الوقت المناسب',
            'تتلاشى ذكرياتي بسرعة مع مرور الوقت'
        ]
    },
    
    // P2_S3: Attention
    'attention': {
        direct: [
            'أستطيع التركيز على مهمة واحدة لفترات طويلة',
            'أحافظ على انتباهي حتى في البيئات المشتتة',
            'أنتبه للتفاصيل الدقيقة في عملي',
            'أستطيع التركيز على عدة مهام في نفس الوقت',
            'أعود بسهولة للتركيز بعد المقاطعة'
        ],
        reverse: [
            'أشعر بالتشتت بسهولة عند العمل على مهمة واحدة',
            'تؤثر المشتتات الخارجية على تركيزي بشكل كبير',
            'أغفل التفاصيل الصغيرة في المهام التي أقوم بها',
            'أجد صعوبة في التعامل مع عدة مهام في وقت واحد',
            'أحتاج وقتًا طويلاً لاستعادة تركيزي بعد المقاطعة'
        ]
    },
    
    // P2_S4: Creativity
    'creativity': {
        direct: [
            'أقترح أفكارًا إبداعية وغير تقليدية',
            'أستمتع بحل المشكلات بطرق مبتكرة',
            'أرى العلاقات والأنماط التي لا يراها الآخرون',
            'أتخيل حلولاً متعددة للمشكلة الواحدة',
            'أحب التفكير خارج الصندوق'
        ],
        reverse: [
            'أفضل اتباع الطرق التقليدية المجربة',
            'أجد صعوبة في ابتكار حلول جديدة للمشكلات',
            'أركز على الحقائق الواضحة دون البحث عن أنماط',
            'أفضل التقيد بحل واحد مثبت للمشكلات',
            'أجد صعوبة في التفكير الإبداعي'
        ]
    },
    
    // P3_S1: Stress Management
    'stress_management': {
        direct: [
            'أتعامل مع المواقف الضاغطة بهدوء وفعالية',
            'أستخدم استراتيجيات محددة لإدارة التوتر',
            'أحافظ على توازني النفسي في ظل الضغوط',
            'أتعرف على علامات التوتر مبكرًا وأتصرف',
            'أستطيع الأداء الجيد حتى تحت الضغط'
        ],
        reverse: [
            'أشعر بالإرهاق بسرعة عند مواجهة الضغوط',
            'لا أملك استراتيجيات فعالة لإدارة التوتر',
            'يؤثر التوتر على أدائي وحياتي اليومية',
            'أتجاهل علامات التوتر حتى تصبح شديدة',
            'ينخفض أدائي بشكل ملحوظ تحت الضغط'
        ]
    },
    
    // P3_S2: Anxiety
    'anxiety': {
        direct: [
            'أشعر بالهدوء والاطمئنان في معظم المواقف',
            'أتحكم في مخاوفي ولا أدعها تسيطر علي',
            'أواجه المواقف الجديدة دون قلق مفرط',
            'أنام بشكل جيد ولا أقلق بشأن المستقبل',
            'أشعر بالثقة عند مواجهة التحديات'
        ],
        reverse: [
            'أشعر بالقلق والتوتر في معظم الأوقات',
            'تسيطر علي المخاوف وتؤثر على قراراتي',
            'أشعر بالقلق الشديد عند مواجهة مواقف جديدة',
            'أعاني من صعوبة النوم بسبب القلق',
            'أفتقد الثقة عند مواجهة التحديات'
        ]
    },
    
    // P3_S3: Resilience
    'resilience': {
        direct: [
            'أتعافى بسرعة من الفشل والإحباطات',
            'أتعلم من الصعوبات وأصبح أقوى',
            'أحافظ على تفاؤلي حتى في الأوقات الصعبة',
            'أتكيف بسهولة مع التغييرات غير المتوقعة',
            'أثق بقدرتي على التغلب على العقبات'
        ],
        reverse: [
            'أحتاج وقتًا طويلاً للتعافي من الفشل',
            'تضعفني الصعوبات ولا أتعلم منها',
            'أفقد الأمل بسهولة في الأوقات الصعبة',
            'أجد صعوبة كبيرة في التكيف مع التغييرات',
            'أشك في قدرتي على التغلب على المشكلات'
        ]
    },
    
    // P3_S4: Emotional Intelligence
    'emotional_intelligence': {
        direct: [
            'أفهم مشاعري وأسبابها بوضوح',
            'أتحكم في انفعالاتي في المواقف الصعبة',
            'أتعاطف مع مشاعر الآخرين وأفهمها',
            'أستطيع قراءة لغة الجسد والإشارات غير اللفظية',
            'أستخدم مشاعري بشكل إيجابي لتحفيز نفسي'
        ],
        reverse: [
            'أجد صعوبة في فهم مشاعري الخاصة',
            'أفقد السيطرة على انفعالاتي بسهولة',
            'أتجاهل مشاعر الآخرين في كثير من الأحيان',
            'أجد صعوبة في فهم لغة الجسد',
            'تعيقني مشاعري عن تحقيق أهدافي'
        ]
    },
    
    // P4_S1: Adaptation
    'adaptation': {
        direct: [
            'أتكيف بسرعة مع البيئات والمواقف الجديدة',
            'أتقبل التغيير وأراه فرصة للنمو',
            'أستطيع تعديل أسلوبي بما يناسب الموقف',
            'أتعامل مع الغموض والعدم اليقين بمرونة',
            'أتعلم من التجارب الجديدة بسرعة'
        ],
        reverse: [
            'أجد صعوبة في التأقلم مع البيئات الجديدة',
            'أقاوم التغيير وأفضل الثبات',
            'أواجه صعوبة في تغيير أسلوبي',
            'أشعر بعدم الراحة في المواقف الغامضة',
            'أحتاج وقتًا طويلاً لأستفيد من التجارب الجديدة'
        ]
    },
    
    // P4_S2: Influence & Leadership
    'influence_leadership': {
        direct: [
            'أستطيع إقناع الآخرين بأفكاري ووجهات نظري',
            'أقود الفرق بفعالية نحو تحقيق الأهداف',
            'ألهم الآخرين للعمل بحماس وتفانٍ',
            'أتخذ المبادرة في المواقف التي تتطلب قيادة',
            'أؤثر إيجابيًا على سلوك وأداء الآخرين'
        ],
        reverse: [
            'أجد صعوبة في إقناع الآخرين بوجهة نظري',
            'أفضل أن يقود غيري المشاريع والفرق',
            'أفتقد القدرة على تحفيز الآخرين',
            'أتجنب المواقف التي تتطلب قيادة',
            'لا أرى تأثيرًا واضحًا لي على الآخرين'
        ]
    },
    
    // P4_S3: Anger Control
    'anger_control': {
        direct: [
            'أتحكم في غضبي وأعبر عنه بطريقة بناءة',
            'أحافظ على هدوئي حتى في المواقف المحبطة',
            'أفكر قبل أن أتصرف عندما أشعر بالغضب',
            'أستخدم استراتيجيات فعالة لتهدئة نفسي',
            'أتعامل مع الانفعالات السلبية بشكل صحي'
        ],
        reverse: [
            'أفقد السيطرة على غضبي بسهولة',
            'أنفعل بسرعة في المواقف المحبطة',
            'أتصرف باندفاع عندما أغضب دون تفكير',
            'أفتقد استراتيجيات فعالة للتحكم في غضبي',
            'أعبر عن انفعالاتي بطرق تضر بعلاقاتي'
        ]
    },
    
    // P5_S1: Arithmetic
    'arithmetic': {
        direct: [
            'أحل المسائل الحسابية بسرعة ودقة',
            'أستمتع بالعمل مع الأرقام والعمليات الرياضية',
            'أستطيع إجراء العمليات الحسابية ذهنيًا',
            'أفهم المفاهيم الرياضية والعددية بسهولة',
            'أستخدم الرياضيات في حل المشكلات اليومية'
        ],
        reverse: [
            'أجد صعوبة في حل المسائل الحسابية',
            'أشعر بعدم الارتياح عند العمل مع الأرقام',
            'أحتاج لآلة حاسبة حتى للعمليات البسيطة',
            'أواجه صعوبة في فهم المفاهيم الرياضية',
            'أتجنب استخدام الرياضيات في حياتي اليومية'
        ]
    },
    
    // P5_S2: Deduction
    'deduction': {
        direct: [
            'أستنتج النتائج المنطقية من المعلومات المتاحة',
            'أكتشف الأخطاء في الحجج والبراهين المنطقية',
            'أفكر بطريقة منهجية ومنطقية',
            'أستطيع تحليل المشكلات المعقدة خطوة بخطوة',
            'أصل للاستنتاجات الصحيحة من المقدمات'
        ],
        reverse: [
            'أجد صعوبة في الوصول لاستنتاجات منطقية',
            'لا ألاحظ الأخطاء المنطقية في الحجج',
            'أفضل الاعتماد على الحدس بدلاً من المنطق',
            'أشعر بالارتباك عند تحليل المشكلات المعقدة',
            'أصل لاستنتاجات خاطئة رغم توفر المعلومات'
        ]
    },
    
    // P5_S3: Correlation & Analysis
    'correlation': {
        direct: [
            'أكتشف العلاقات والأنماط في البيانات',
            'أستطيع تحليل البيانات واستخلاص النتائج',
            'أفهم العلاقات السببية بين المتغيرات',
            'أستخدم التحليل الإحصائي في اتخاذ القرارات',
            'أميز بين الارتباط والسببية في البيانات'
        ],
        reverse: [
            'أجد صعوبة في اكتشاف الأنماط في البيانات',
            'لا أستطيع تحليل البيانات بشكل فعال',
            'أخلط بين الارتباط والسببية',
            'أتجنب استخدام التحليل الإحصائي',
            'أفتقد القدرة على استخلاص النتائج من البيانات'
        ]
    },
    
    // P6_S1: Decision Making
    'decision_making': {
        direct: [
            'أتخذ قرارات مدروسة بناءً على المعلومات المتاحة',
            'أوازن بين الخيارات المختلفة قبل اتخاذ القرار',
            'أتحمل مسؤولية قراراتي ونتائجها',
            'أتخذ القرارات بثقة حتى في ظل عدم اليقين',
            'أقيّم نتائج قراراتي وأتعلم منها'
        ],
        reverse: [
            'أتردد كثيرًا قبل اتخاذ أي قرار',
            'أتخذ قرارات متسرعة دون تفكير كافٍ',
            'أتهرب من تحمل مسؤولية قراراتي',
            'أشعر بعدم الثقة عند اتخاذ القرارات المهمة',
            'لا أقيّم نتائج قراراتي ولا أتعلم منها'
        ]
    },
    
    // P6_S2: Delegation
    'delegation': {
        direct: [
            'أفوض المهام للآخرين بناءً على قدراتهم',
            'أثق بقدرة فريقي على إنجاز المهام المفوضة',
            'أوضح التوقعات والمسؤوليات عند التفويض',
            'أتابع المهام المفوضة دون التدخل المفرط',
            'أقدم الدعم اللازم للمفوض إليهم'
        ],
        reverse: [
            'أجد صعوبة في تفويض المهام للآخرين',
            'لا أثق بقدرة الآخرين على إنجاز المهام',
            'أفشل في توضيح التوقعات عند التفويض',
            'أتدخل بشكل مفرط في المهام المفوضة',
            'لا أقدم الدعم الكافي للمفوض إليهم'
        ]
    },
    
    // P6_S3: Reward & Accountability
    'reward_accountability': {
        direct: [
            'أطبق نظامًا عادلاً للثواب والعقاب',
            'أحاسب على النتائج بشكل موضوعي ومنصف',
            'أكافئ الأداء الممتاز والجهود الاستثنائية',
            'أواجه الأداء الضعيف بطريقة بناءة',
            'أوازن بين المساءلة والدعم'
        ],
        reverse: [
            'أفشل في تطبيق نظام عادل للثواب والعقاب',
            'أحاسب الآخرين بطريقة غير موضوعية',
            'لا أكافئ الأداء الجيد بشكل كافٍ',
            'أتجنب مواجهة الأداء الضعيف',
            'أركز على المساءلة دون تقديم الدعم'
        ]
    },
    
    // P7_S1: Complex Work Situations
    'complex_work_situations': {
        direct: [
            'أتعامل مع المواقف المهنية المعقدة بفعالية',
            'أحلل المشكلات متعددة الأبعاد بشكل شامل',
            'أدير الأولويات المتضاربة بمهارة',
            'أتخذ قرارات سليمة في ظل الضغوط والتعقيد',
            'أستفيد من الموارد المتاحة لحل المشكلات المعقدة'
        ],
        reverse: [
            'أشعر بالإرباك أمام المواقف المهنية المعقدة',
            'أجد صعوبة في فهم المشكلات متعددة الأبعاد',
            'لا أستطيع إدارة الأولويات المتضاربة',
            'أتخذ قرارات خاطئة تحت الضغط والتعقيد',
            'أفشل في استخدام الموارد بفعالية لحل المشكلات'
        ]
    }
};

// ============================================================================
// MAIN GENERATION LOGIC
// ============================================================================

function loadSchema(): Schema {
    const schemaPath = path.join(__dirname, '..', 'backend', 'PsyApi', 'Domain', 'SdjV2SevenPatterns.json');
    const schemaContent = fs.readFileSync(schemaPath, 'utf-8');
    return JSON.parse(schemaContent) as Schema;
}

function generateCsvRows(schema: Schema): CsvRow[] {
    const rows: CsvRow[] = [];
    let itemCounter = 1;

    console.log('🎯 Generating SDJ V2 Seven Patterns CSV...\n');

    for (const pattern of schema.main_patterns) {
        console.log(`📦 Pattern ${pattern.id}: ${pattern.name_ar}`);

        for (const subDim of pattern.sub_dimensions) {
            console.log(`   └─ ${subDim.id}: ${subDim.name_ar}`);

            const templates = ITEM_TEMPLATES[subDim.key];
            if (!templates) {
                throw new Error(`❌ No templates found for sub-dimension: ${subDim.key}`);
            }

            // Generate 5 MCQ items (3 direct, 2 reverse)
            for (let i = 0; i < 3; i++) {
                const question = templates.direct[i];
                const correctAnswer = i % 2 === 0 ? 'A' : 'B';
                const options = correctAnswer === 'A' 
                    ? 'نعم، دائماً|لا، نادراً|أحياناً|لا أعلم'
                    : 'لا، نادراً|نعم، دائماً|أحياناً|لا أعلم';
                
                rows.push({
                    ItemCode: `I${itemCounter.toString().padStart(3, '0')}`,
                    TextAr: question,
                    PatternId: pattern.id,
                    PatternKey: pattern.key,
                    PatternNameAr: pattern.name_ar,
                    SubId: subDim.id,
                    SubKey: subDim.key,
                    SubNameAr: subDim.name_ar,
                    Type: 'MCQ',
                    Reverse: '0',
                    TimeLimitSeconds: '45',
                    Weight: '1',
                    CorrectAnswer: correctAnswer,
                    Options: options
                });
                itemCounter++;
            }

            // 2 reverse MCQ
            for (let i = 0; i < 2; i++) {
                const question = templates.reverse[i];
                const correctAnswer = i % 2 === 0 ? 'A' : 'B';
                const options = correctAnswer === 'A'
                    ? 'نعم، غالباً|لا، أبداً|أحياناً|لا أعلم'
                    : 'لا، أبداً|نعم، غالباً|أحياناً|لا أعلم';
                
                rows.push({
                    ItemCode: `I${itemCounter.toString().padStart(3, '0')}`,
                    TextAr: question,
                    PatternId: pattern.id,
                    PatternKey: pattern.key,
                    PatternNameAr: pattern.name_ar,
                    SubId: subDim.id,
                    SubKey: subDim.key,
                    SubNameAr: subDim.name_ar,
                    Type: 'MCQ',
                    Reverse: '1',
                    TimeLimitSeconds: '45',
                    Weight: '1',
                    CorrectAnswer: correctAnswer,
                    Options: options
                });
                itemCounter++;
            }

            // Generate 3 Likert direct items
            for (let i = 3; i < 5; i++) {
                rows.push({
                    ItemCode: `I${itemCounter.toString().padStart(3, '0')}`,
                    TextAr: templates.direct[i],
                    PatternId: pattern.id,
                    PatternKey: pattern.key,
                    PatternNameAr: pattern.name_ar,
                    SubId: subDim.id,
                    SubKey: subDim.key,
                    SubNameAr: subDim.name_ar,
                    Type: 'LikertAgreement',
                    Reverse: '0',
                    TimeLimitSeconds: '45',
                    Weight: '1'
                });
                itemCounter++;
            }

            // Generate 3 Likert reverse items
            for (let i = 2; i < 5; i++) {
                rows.push({
                    ItemCode: `I${itemCounter.toString().padStart(3, '0')}`,
                    TextAr: templates.reverse[i],
                    PatternId: pattern.id,
                    PatternKey: pattern.key,
                    PatternNameAr: pattern.name_ar,
                    SubId: subDim.id,
                    SubKey: subDim.key,
                    SubNameAr: subDim.name_ar,
                    Type: 'LikertAgreement',
                    Reverse: '1',
                    TimeLimitSeconds: '45',
                    Weight: '1'
                });
                itemCounter++;
            }

            console.log(`      ✓ Generated 10 items (5 MCQ, 5 Likert)`);
        }
        console.log('');
    }

    console.log(`✅ Total items generated: ${rows.length}\n`);
    return rows;
}

function writeCsv(rows: CsvRow[], outputPath: string): void {
    const header = 'ItemCode,TextAr,PatternId,PatternKey,PatternNameAr,SubId,SubKey,SubNameAr,Type,Reverse,TimeLimitSeconds,Weight,CorrectAnswer,Options';
    const lines = [header];

    for (const row of rows) {
        const line = [
            row.ItemCode,
            escapeCsvField(row.TextAr),
            row.PatternId,
            row.PatternKey,
            escapeCsvField(row.PatternNameAr),
            row.SubId,
            row.SubKey,
            escapeCsvField(row.SubNameAr),
            row.Type,
            row.Reverse,
            row.TimeLimitSeconds,
            row.Weight,
            row.CorrectAnswer || '',
            row.Options ? escapeCsvField(row.Options) : ''
        ].join(',');
        lines.push(line);
    }

    fs.writeFileSync(outputPath, lines.join('\n'), 'utf-8');
    console.log(`📄 CSV written to: ${outputPath}`);
}

function escapeCsvField(field: string): string {
    // Escape fields containing commas or quotes
    if (field.includes(',') || field.includes('"') || field.includes('\n')) {
        return `"${field.replace(/"/g, '""')}"`;
    }
    return field;
}

function main() {
    try {
        const schema = loadSchema();
        const rows = generateCsvRows(schema);

        // Write to backend Resources folder
        const backendOutput = path.join(__dirname, '..', 'backend', 'PsyApi', 'Resources', 'Questions', 'questions_sdj_v2_ar.csv');
        writeCsv(rows, backendOutput);

        // Also write to seed folder for backup
        const seedOutput = path.join(__dirname, '..', 'seed', 'questions_sdj_v2_ar.csv');
        writeCsv(rows, seedOutput);

        console.log('\n✅ SDJ V2 CSV Generation Complete!');
        console.log(`   Total Items: ${rows.length}`);
        console.log(`   Patterns: ${schema.main_patterns.length}`);
        console.log(`   Sub-dimensions: ${schema.main_patterns.reduce((sum, p) => sum + p.sub_dimensions.length, 0)}`);
        console.log('\n📋 Next Steps:');
        console.log('   1. Run: npm run validate-csv');
        console.log('   2. Review Arabic text quality manually');
        console.log('   3. Proceed with database migration\n');

    } catch (error) {
        console.error('❌ Generation failed:', error);
        process.exit(1);
    }
}

// Run if executed directly
if (require.main === module) {
    main();
}

export { generateCsvRows, loadSchema };
