namespace PsyApi.Services.Scoring
{
    /// <summary>
    /// Provides rule-based course recommendations for weak sub-dimensions
    /// All recommendations are in Arabic, deterministic (no AI/external calls)
    /// </summary>
    public static class CourseRecommendationsMapper
    {
        /// <summary>
        /// Maps sub-dimension names (Arabic) to course recommendations
        /// Each entry contains 2-3 short course suggestions
        /// </summary>
        private static readonly Dictionary<string, SubDimensionCourseInfo> SubDimensionCourses = new()
        {
            // التميز الذاتي (Self-Excellence)
            {
                "الوعي الذاتي",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الوعي الذاتي",
                    Status = "يحتاج إلى تعزيز القدرة على التأمل الذاتي وفهم نقاط القوة والضعف",
                    Courses = new List<string>
                    {
                        "دورة التطوير الذاتي والوعي بالذات (3 أيام)",
                        "ورشة عمل اكتشاف القدرات الشخصية (يومان)",
                        "برنامج التأمل وتحليل الشخصية (أسبوع)"
                    }
                }
            },
            {
                "الثقة بالنفس",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الثقة بالنفس",
                    Status = "يحتاج إلى بناء الثقة بالنفس والتخلص من الخوف من الفشل",
                    Courses = new List<string>
                    {
                        "دورة بناء الثقة بالنفس والتحدث أمام الجمهور (5 أيام)",
                        "ورشة التغلب على القلق والخوف (3 أيام)",
                        "برنامج تطوير المهارات القيادية الشخصية (أسبوع)"
                    }
                }
            },
            {
                "التنظيم الذاتي",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "التنظيم الذاتي",
                    Status = "يحتاج إلى تحسين مهارات التنظيم وإدارة الأولويات",
                    Courses = new List<string>
                    {
                        "دورة إدارة الوقت والأولويات (3 أيام)",
                        "ورشة التخطيط الشخصي الفعال (يومان)",
                        "برنامج الانضباط الذاتي والعادات الإنتاجية (أسبوع)"
                    }
                }
            },
            {
                "التعلم المستمر",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "التعلم المستمر",
                    Status = "يحتاج إلى تعزيز الدافعية للتعلم واكتساب المهارات الجديدة",
                    Courses = new List<string>
                    {
                        "دورة استراتيجيات التعلم السريع (3 أيام)",
                        "ورشة بناء خطة التطوير المهني (يومان)",
                        "برنامج القراءة الفعالة والتعلم الذاتي (أسبوع)"
                    }
                }
            },
            {
                "المرونة النفسية",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "المرونة النفسية",
                    Status = "يحتاج إلى تطوير القدرة على التكيف مع التغيير",
                    Courses = new List<string>
                    {
                        "دورة إدارة التغيير والتكيف (4 أيام)",
                        "ورشة المرونة النفسية والصلابة (3 أيام)",
                        "برنامج التعامل مع الضغوط والتحديات (أسبوع)"
                    }
                }
            },

            // التواصل والعلاقات (Communication & Relationships)
            {
                "الذكاء العاطفي",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الذكاء العاطفي",
                    Status = "يحتاج إلى تحسين إدارة المشاعر والتعاطف مع الآخرين",
                    Courses = new List<string>
                    {
                        "دورة الذكاء العاطفي في بيئة العمل (5 أيام)",
                        "ورشة إدارة العواطف والانفعالات (3 أيام)",
                        "برنامج تطوير مهارات التعاطف (أسبوع)"
                    }
                }
            },
            {
                "التواصل الفعال",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "التواصل الفعال",
                    Status = "يحتاج إلى تطوير مهارات التواصل والاستماع الفعال",
                    Courses = new List<string>
                    {
                        "دورة مهارات الاتصال والتواصل الفعال (4 أيام)",
                        "ورشة فن الاستماع والحوار (يومان)",
                        "برنامج لغة الجسد والتواصل غير اللفظي (3 أيام)"
                    }
                }
            },
            {
                "التعاون",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "التعاون",
                    Status = "يحتاج إلى تعزيز مهارات العمل الجماعي والتعاون",
                    Courses = new List<string>
                    {
                        "دورة بناء فرق العمل الفعالة (4 أيام)",
                        "ورشة التعاون وديناميكيات الفريق (3 أيام)",
                        "برنامج القيادة التعاونية (أسبوع)"
                    }
                }
            },
            {
                "حل النزاعات",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "حل النزاعات",
                    Status = "يحتاج إلى تطوير مهارات الوساطة وحل الخلافات",
                    Courses = new List<string>
                    {
                        "دورة إدارة النزاعات وحل الخلافات (5 أيام)",
                        "ورشة مهارات التفاوض والوساطة (3 أيام)",
                        "برنامج التواصل في أوقات الأزمات (أسبوع)"
                    }
                }
            },
            {
                "بناء العلاقات",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "بناء العلاقات",
                    Status = "يحتاج إلى تطوير مهارات التشبيك وبناء العلاقات المهنية",
                    Courses = new List<string>
                    {
                        "دورة بناء شبكة العلاقات المهنية (3 أيام)",
                        "ورشة فن التعامل مع الناس (يومان)",
                        "برنامج التسويق الشخصي والعلامة التجارية (أسبوع)"
                    }
                }
            },

            // النجاح المهني (Professional Success)
            {
                "القيادة",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "القيادة",
                    Status = "يحتاج إلى تطوير مهارات القيادة واتخاذ القرار",
                    Courses = new List<string>
                    {
                        "دورة المهارات القيادية المتقدمة (أسبوع)",
                        "ورشة القيادة الموقفية (3 أيام)",
                        "برنامج القيادة التحويلية (10 أيام)"
                    }
                }
            },
            {
                "حل المشكلات",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "حل المشكلات",
                    Status = "يحتاج إلى تحسين التفكير المنطقي والتحليلي",
                    Courses = new List<string>
                    {
                        "دورة التفكير النقدي وحل المشكلات (5 أيام)",
                        "ورشة تحليل المشكلات المعقدة (3 أيام)",
                        "برنامج اتخاذ القرارات المبنية على البيانات (أسبوع)"
                    }
                }
            },
            {
                "الإبداع والابتكار",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الإبداع والابتكار",
                    Status = "يحتاج إلى تعزيز التفكير الإبداعي والابتكاري",
                    Courses = new List<string>
                    {
                        "دورة التفكير الإبداعي والابتكار (5 أيام)",
                        "ورشة تقنيات العصف الذهني (يومان)",
                        "برنامج Design Thinking للابتكار (أسبوع)"
                    }
                }
            },
            {
                "إدارة الوقت",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "إدارة الوقت",
                    Status = "يحتاج إلى تطوير مهارات تنظيم الوقت والأولويات",
                    Courses = new List<string>
                    {
                        "دورة إتقان إدارة الوقت (3 أيام)",
                        "ورشة مصفوفة أيزنهاور للأولويات (يومان)",
                        "برنامج الإنتاجية الشخصية (أسبوع)"
                    }
                }
            },
            {
                "التخطيط الاستراتيجي",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "التخطيط الاستراتيجي",
                    Status = "يحتاج إلى تطوير مهارات التخطيط طويل المدى",
                    Courses = new List<string>
                    {
                        "دورة التخطيط الاستراتيجي المتقدم (أسبوع)",
                        "ورشة تحليل SWOT وSWOT (3 أيام)",
                        "برنامج إدارة المشاريع الاستراتيجية (10 أيام)"
                    }
                }
            },

            // المسؤولية الاجتماعية (Social Responsibility)
            {
                "الوعي المجتمعي",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الوعي المجتمعي",
                    Status = "يحتاج إلى تعزيز الاهتمام بالقضايا المجتمعية",
                    Courses = new List<string>
                    {
                        "دورة المسؤولية الاجتماعية (3 أيام)",
                        "ورشة القيادة المجتمعية (يومان)",
                        "برنامج العمل التطوعي والمبادرات المجتمعية (أسبوع)"
                    }
                }
            },
            {
                "الأخلاق المهنية",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الأخلاق المهنية",
                    Status = "يحتاج إلى تعزيز الالتزام بالمعايير الأخلاقية",
                    Courses = new List<string>
                    {
                        "دورة أخلاقيات العمل والسلوك المهني (3 أيام)",
                        "ورشة النزاهة والشفافية (يومان)",
                        "برنامج الحوكمة والامتثال (أسبوع)"
                    }
                }
            },
            {
                "الاستدامة",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الاستدامة",
                    Status = "يحتاج إلى تطوير الوعي البيئي والممارسات المستدامة",
                    Courses = new List<string>
                    {
                        "دورة التنمية المستدامة (4 أيام)",
                        "ورشة الاقتصاد الأخضر (3 أيام)",
                        "برنامج إدارة البيئة والموارد (أسبوع)"
                    }
                }
            },
            {
                "العمل التطوعي",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "العمل التطوعي",
                    Status = "يحتاج إلى تعزيز المشاركة في الأنشطة التطوعية",
                    Courses = new List<string>
                    {
                        "دورة إدارة المبادرات التطوعية (3 أيام)",
                        "ورشة تصميم المشاريع المجتمعية (يومان)",
                        "برنامج ريادة الأعمال الاجتماعية (أسبوع)"
                    }
                }
            },
            {
                "المواطنة الفاعلة",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "المواطنة الفاعلة",
                    Status = "يحتاج إلى تعزيز المشاركة المدنية والسياسية",
                    Courses = new List<string>
                    {
                        "دورة المواطنة الفاعلة والمشاركة المجتمعية (3 أيام)",
                        "ورشة الديمقراطية والحقوق المدنية (يومان)",
                        "برنامج القيادة السياسية والإدارة العامة (أسبوع)"
                    }
                }
            },

            // الصحة والتوازن (Health & Balance)
            {
                "الصحة النفسية",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الصحة النفسية",
                    Status = "يحتاج إلى تحسين الصحة النفسية والتعامل مع التوتر",
                    Courses = new List<string>
                    {
                        "دورة الصحة النفسية والرفاهية (4 أيام)",
                        "ورشة إدارة القلق والتوتر (3 أيام)",
                        "برنامج التأمل والاسترخاء الذهني (أسبوع)"
                    }
                }
            },
            {
                "الصحة الجسدية",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "الصحة الجسدية",
                    Status = "يحتاج إلى تحسين اللياقة البدنية والعادات الصحية",
                    Courses = new List<string>
                    {
                        "دورة اللياقة البدنية والتغذية السليمة (أسبوع)",
                        "ورشة العادات الصحية اليومية (3 أيام)",
                        "برنامج إدارة الوزن والصحة (شهر)"
                    }
                }
            },
            {
                "إدارة الضغوط",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "إدارة الضغوط",
                    Status = "يحتاج إلى تطوير استراتيجيات التعامل مع الضغوط",
                    Courses = new List<string>
                    {
                        "دورة إدارة الضغوط النفسية (4 أيام)",
                        "ورشة تقنيات الاسترخاء والتنفس (يومان)",
                        "برنامج اليقظة الذهنية (Mindfulness) (أسبوع)"
                    }
                }
            },
            {
                "التوازن بين العمل والحياة",
                new SubDimensionCourseInfo
                {
                    SubDimensionAr = "التوازن بين العمل والحياة",
                    Status = "يحتاج إلى تحسين التوازن بين الحياة المهنية والشخصية",
                    Courses = new List<string>
                    {
                        "دورة Work-Life Balance (3 أيام)",
                        "ورشة إدارة الحدود الشخصية والمهنية (يومان)",
                        "برنامج الرفاهية الشاملة (أسبوع)"
                    }
                }
            }
        };

        /// <summary>
        /// Gets course recommendations for a specific sub-dimension
        /// </summary>
        public static SubDimensionCourseInfo? GetCoursesForSubDimension(string subDimensionAr)
        {
            SubDimensionCourses.TryGetValue(subDimensionAr, out var courseInfo);
            return courseInfo;
        }

        /// <summary>
        /// Gets course recommendations for all weak sub-dimensions (T < 40)
        /// </summary>
        public static List<SubDimensionCourseRecommendation> GetRecommendationsForWeakSubDimensions(
            List<SdjSubDimensionScore> subDimensions)
        {
            var recommendations = new List<SubDimensionCourseRecommendation>();

            var weakSubDimensions = subDimensions
                .Where(sd => sd.T < 40.0)
                .OrderBy(sd => sd.T)
                .ToList();

            foreach (var subDim in weakSubDimensions)
            {
                var courseInfo = GetCoursesForSubDimension(subDim.SubDimension);
                
                recommendations.Add(new SubDimensionCourseRecommendation
                {
                    SubDimensionAr = subDim.SubDimension,
                    TScore = subDim.T,
                    Percentile = subDim.Percentile,
                    Band = subDim.Band,
                    Status = courseInfo?.Status ?? "يحتاج إلى تحسين وتطوير",
                    RecommendedCourses = courseInfo?.Courses ?? new List<string>
                    {
                        $"دورة تطوير مهارات {subDim.SubDimension} (3-5 أيام)",
                        $"ورشة عملية في {subDim.SubDimension} (يومان)",
                        $"برنامج تدريبي متقدم في {subDim.SubDimension} (أسبوع)"
                    }
                });
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Course information for a specific sub-dimension
    /// </summary>
    public class SubDimensionCourseInfo
    {
        public string SubDimensionAr { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Courses { get; set; } = new();
    }

    /// <summary>
    /// Course recommendation output for PDF rendering
    /// </summary>
    public class SubDimensionCourseRecommendation
    {
        public string SubDimensionAr { get; set; } = string.Empty;
        public double TScore { get; set; }
        public double Percentile { get; set; }
        public string Band { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> RecommendedCourses { get; set; } = new();
    }
}
