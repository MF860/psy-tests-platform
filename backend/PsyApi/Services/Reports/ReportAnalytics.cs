using PsyApi.Models;
using System.Globalization;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// محرك التحليل النفسي - قواعد التحليل والقوالب العربية والتوصيات
    /// بدون اتصال خارجي، كل التحليل داخلي مبني على T-scores والـ Clusters
    /// </summary>
    public static class ReportAnalytics
    {
        #region Cluster Definitions (COG/EMO/SOC/ORG)

        /// <summary>
        /// خريطة تجميع الأبعاد إلى محاور رئيسية (Clusters)
        /// COG = معرفي، EMO = انفعالي، SOC = اجتماعي، ORG = تنظيمي
        /// </summary>
        public static readonly Dictionary<string, string> ClusterMap = new()
        {
            // المحور المعرفي (Cognitive - COG)
            ["التحليل"] = "COG",
            ["التحليل المنطقي"] = "COG",
            ["التركيز"] = "COG",
            ["الانتباه"] = "COG",
            ["حل المشكلات"] = "COG",
            ["الذاكرة"] = "COG",
            ["الذاكرة العاملة"] = "COG",
            ["التفكير النقدي"] = "COG",
            ["الإدراك"] = "COG",
            ["المعالجة المعرفية"] = "COG",
            
            // المحور الانفعالي (Emotional - EMO)
            ["الاستقرار العاطفي"] = "EMO",
            ["إدارة الضغط"] = "EMO",
            ["التعاطف"] = "EMO",
            ["المرونة النفسية"] = "EMO",
            ["الذكاء العاطفي"] = "EMO",
            ["التحكم الانفعالي"] = "EMO",
            ["الثبات الانفعالي"] = "EMO",
            ["التكيف العاطفي"] = "EMO",
            
            // المحور الاجتماعي (Social - SOC)
            ["التواصل"] = "SOC",
            ["التأثير"] = "SOC",
            ["العمل الجماعي"] = "SOC",
            ["القيادة"] = "SOC",
            ["التفاعل الاجتماعي"] = "SOC",
            ["بناء العلاقات"] = "SOC",
            ["الإقناع"] = "SOC",
            
            // المحور التنظيمي (Organizational - ORG)
            ["التخطيط"] = "ORG",
            ["الانضباط"] = "ORG",
            ["المسؤولية"] = "ORG",
            ["التكيف المهني"] = "ORG",
            ["إدارة الوقت"] = "ORG",
            ["التنظيم"] = "ORG",
            ["الالتزام"] = "ORG",
            ["المبادرة"] = "ORG"
        };

        /// <summary>
        /// مقدمات تعريفية لكل محور (Cluster Introductions)
        /// </summary>
        public static readonly Dictionary<string, string> ClusterIntro = new()
        {
            ["COG"] = "المكوّن المعرفي يعكس مهارات التحليل والذاكرة والتركيز وحل المشكلات، وهو أساس القدرات العقلية العليا.",
            ["EMO"] = "المكوّن الانفعالي يرتبط بالاستقرار العاطفي، إدارة الضغط، والقدرة على التعاطف والتكيّف مع المواقف الصعبة.",
            ["SOC"] = "المكوّن الاجتماعي يتناول التواصل الفعّال، التأثير في الآخرين، والعمل ضمن فرق بكفاءة عالية.",
            ["ORG"] = "المكوّن التنظيمي يضم التخطيط، الانضباط، وتحمل المسؤولية والتكيّف المهني في بيئة العمل."
        };

        /// <summary>
        /// أسماء المحاور بالعربية
        /// </summary>
        public static readonly Dictionary<string, string> ClusterNames = new()
        {
            ["COG"] = "المحور المعرفي",
            ["EMO"] = "المحور الانفعالي",
            ["SOC"] = "المحور الاجتماعي",
            ["ORG"] = "المحور التنظيمي"
        };

        #endregion

        #region Dimension Analysis Templates

        /// <summary>
        /// توليد وصف تحليلي عربي لبُعد معين حسب الـ Band
        /// Bands: Excellent (≥65), Good (55-64), Average (40-54), Weak (<40)
        /// </summary>
        public static string DescribeDimension(string nameAr, double t)
        {
            // Excellent band (≥65)
            if (t >= 65)
                return $"يمتلك الممتحَن مستوى متميّزًا في «{nameAr}»، بما يعكس توظيفًا متّسقًا وعالي الكفاءة لهذه المهارة في مختلف المواقف.";
            
            // Good band (55-64)
            if (t >= 55)
                return $"يُظهر الممتحَن أداءً جيدًا في «{nameAr}» مع ثبات مقبول عبر المواقف اليومية، ويمكن تعزيز الأداء بالممارسة المستمرة.";
            
            // Average band (40-54)
            if (t >= 40)
                return $"يمتلك الممتحَن مستوى مقبولًا في «{nameAr}»، ويمكن تعزيز الأداء بالممارسة الموجّهة والمتابعة الدورية.";
            
            // Weak band (<40)
            return $"تمثّل «{nameAr}» مجال تطوير أساسي حاليًا؛ يُوصى بخطة تدريب قصيرة المدى مع متابعة دورية لتحسين الأداء.";
        }

        /// <summary>
        /// توليد وصف موسّع للبُعد (3-4 جمل)
        /// </summary>
        public static string DescribeDimensionExtended(string nameAr, double t, double percentile)
        {
            var band = GetBand(t);
            var pctText = ReportTheme.FormatNum(percentile, 0);
            var tText = ReportTheme.FormatNum(t, 1);

            return band switch
            {
                "Excellent" => $"يتمتع الممتحَن بمستوى استثنائي في «{nameAr}» (T={tText}، مئيني {pctText})، " +
                              $"مما يضعه في مرتبة متقدمة مقارنة بالأقران. " +
                              $"هذه النقطة تمثل قوة جوهرية يمكن الاستفادة منها في المهام المعقدة والقيادية. " +
                              $"يُنصح بتوظيف هذا البُعد كمحور ارتكاز في التطوير المهني.",
                
                "Good" => $"يُظهر الممتحَن أداءً جيدًا في «{nameAr}» (T={tText}، مئيني {pctText})، " +
                         $"بما يعكس قدرة ثابتة في معظم المواقف. " +
                         $"مع الممارسة المنتظمة والتغذية الراجعة، يمكن الوصول إلى مستويات متقدمة. " +
                         $"يُوصى بتخصيص وقت أسبوعي لتطوير هذا الجانب.",
                
                "Average" => $"يمتلك الممتحَن مستوى متوسطًا في «{nameAr}» (T={tText}، مئيني {pctText})، " +
                            $"مما يشير إلى قدرة أساسية قابلة للتحسين. " +
                            $"يُنصح بوضع خطة تطوير موجّهة تشمل تمارين يومية وتطبيقات عملية. " +
                            $"المتابعة الشهرية ضرورية لقياس التقدم.",
                
                _ => $"يحتاج الممتحَن إلى تطوير «{nameAr}» (T={tText}، مئيني {pctText}) بشكل جدّي وعاجل. " +
                    $"هذا البُعد يمثل تحديًا رئيسيًا قد يؤثر على الأداء العام. " +
                    $"يُوصى بخطة تدريب مكثفة تشمل جلسات إرشادية أسبوعية وتمارين يومية. " +
                    $"المراجعة كل 4-6 أسابيع ضرورية لتتبع التحسن."
            };
        }

        #endregion

        #region Risk Flags & Insights

        /// <summary>
        /// تحديد مؤشرات المخاطرة (Flags) بناءً على الدرجات المتطرفة
        /// </summary>
        public static IEnumerable<string> RiskFlags(IEnumerable<DimensionScore> dimensions)
        {
            foreach (var d in dimensions)
            {
                // Very low (≤35)
                if (d.T <= 35)
                    yield return $"⚠️ قد تشير الدرجة المنخفضة جدًا في «{d.Dimension}» (T={ReportTheme.FormatNum(d.T, 1)}) إلى تعثّر ظرفي أو حاجة ملحّة للتدخل؛ يُنصح بخطة تقوية مكثفة ومراجعة بعد 4-6 أسابيع.";
                
                // Very high (≥75)
                if (d.T >= 75)
                    yield return $"ℹ️ الدرجة المرتفعة جدًا في «{d.Dimension}» (T={ReportTheme.FormatNum(d.T, 1)}) قد تعكس اعتمادًا زائدًا على هذه المهارة؛ يُفضّل ضبط التوازن وتنويع الاستراتيجيات حسب السياق.";
            }
        }

        /// <summary>
        /// توليد رؤى عامة (General Insights) بناءً على التوزيع
        /// </summary>
        public static List<string> GenerateInsights(
            IEnumerable<DimensionScore> dimensions,
            int excellentCount,
            int goodCount,
            int averageCount,
            int weakCount)
        {
            var insights = new List<string>();
            var total = excellentCount + goodCount + averageCount + weakCount;
            
            if (total == 0) return insights;

            // نسبة التميز
            var excellentPct = (excellentCount * 100.0) / total;
            if (excellentPct >= 50)
                insights.Add($"🌟 يتمتع الملف النفسي بنسبة عالية من الأبعاد المتميزة ({ReportTheme.FormatNum(excellentPct, 0)}%)، مما يعكس قدرات استثنائية في معظم المجالات.");
            else if (excellentPct < 20)
                insights.Add($"💡 نسبة الأبعاد المتميزة منخفضة ({ReportTheme.FormatNum(excellentPct, 0)}%)؛ يُنصح بالتركيز على تطوير نقاط القوة الموجودة كأساس للنمو.");

            // نسبة الضعف
            var weakPct = (weakCount * 100.0) / total;
            if (weakPct >= 40)
                insights.Add($"⚡ يحتاج الملف إلى خطة تطوير شاملة، حيث تمثل الأبعاد الضعيفة {ReportTheme.FormatNum(weakPct, 0)}% من المجموع. التدخل المبكر ضروري.");
            else if (weakPct < 10)
                insights.Add($"✅ الملف متوازن بشكل جيد، مع نسبة ضعف منخفضة ({ReportTheme.FormatNum(weakPct, 0)}%)؛ التركيز على الصقل والتحسين المستمر مناسب.");

            // التجانس (Homogeneity)
            var stdDev = CalculateStandardDeviation(dimensions.Select(d => d.T));
            if (stdDev < 8)
                insights.Add($"📊 الأبعاد متجانسة نسبيًا (انحراف معياري {ReportTheme.FormatNum(stdDev, 1)})، مما يشير إلى أداء متسق عبر المجالات.");
            else if (stdDev > 15)
                insights.Add($"📊 تتفاوت الأبعاد بشكل ملحوظ (انحراف معياري {ReportTheme.FormatNum(stdDev, 1)})؛ يُنصح بالتركيز على سد الفجوات لتحقيق توازن أفضل.");

            return insights;
        }

        #endregion

        #region Cluster Analysis

        /// <summary>
        /// تحليل محور كامل (Cluster) بناءً على أبعاده
        /// </summary>
        public static string AnalyzeCluster(
            string clusterCode,
            IEnumerable<DimensionScore> clusterDimensions)
        {
            var dimensions = clusterDimensions.ToList();
            if (!dimensions.Any())
                return $"لا توجد بيانات كافية لتحليل {ClusterNames.GetValueOrDefault(clusterCode, "هذا المحور")}.";

            var avgT = dimensions.Average(d => d.T);
            var clusterName = ClusterNames.GetValueOrDefault(clusterCode, "المحور");
            var intro = ClusterIntro.GetValueOrDefault(clusterCode, "");

            var band = GetBand(avgT);
            var performance = band switch
            {
                "Excellent" => "متميز",
                "Good" => "جيد",
                "Average" => "متوسط",
                _ => "يحتاج تطوير"
            };

            // إيجاد أبرز بُعد (أعلى T)
            var topDimension = dimensions.OrderByDescending(d => d.T).First();
            var bottomDimension = dimensions.OrderBy(d => d.T).First();

            var analysis = $"{intro}\n\n";
            analysis += $"الأداء العام في {clusterName} **{performance}** (متوسط T={ReportTheme.FormatNum(avgT, 1)}). ";
            
            if (topDimension.T - bottomDimension.T > 20)
            {
                analysis += $"يُلاحظ تفاوت كبير بين «{topDimension.Dimension}» (T={ReportTheme.FormatNum(topDimension.T, 1)}) و«{bottomDimension.Dimension}» (T={ReportTheme.FormatNum(bottomDimension.T, 1)})؛ ";
                analysis += $"يُنصح بالعمل على سد هذه الفجوة.";
            }
            else
            {
                analysis += $"الأداء متجانس نسبيًا عبر الأبعاد، مما يعكس قدرة متوازنة.";
            }

            return analysis;
        }

        /// <summary>
        /// تجميع الأبعاد حسب المحاور (Clusters)
        /// </summary>
        public static Dictionary<string, List<DimensionScore>> GroupDimensionsByClusters(
            IEnumerable<DimensionScore> dimensions)
        {
            var grouped = new Dictionary<string, List<DimensionScore>>
            {
                ["COG"] = new(),
                ["EMO"] = new(),
                ["SOC"] = new(),
                ["ORG"] = new(),
                ["OTHER"] = new() // للأبعاد غير المصنفة
            };

            foreach (var dim in dimensions)
            {
                var cluster = ClusterMap.GetValueOrDefault(dim.Dimension, "OTHER");
                grouped[cluster].Add(dim);
            }

            return grouped;
        }

        #endregion

        #region Action Recommendations

        /// <summary>
        /// توليد توصية عملية لبُعد ضعيف
        /// </summary>
        public static string MakeActionForWeakDimension(string nameAr, double t)
        {
            var urgency = t < 30 ? "عاجل" : t < 35 ? "مهم" : "مستحسن";
            
            return $"**{nameAr}** ({urgency}): تمرين يومي موجّه لمدة 15-20 دقيقة، تطبيق عملي أسبوعي على مواقف حقيقية، ومراجعة النتائج شهريًا. " +
                   $"الهدف: رفع T من {ReportTheme.FormatNum(t, 1)} إلى ≥45 خلال 6-8 أسابيع.";
        }

        /// <summary>
        /// توصيات عامة لكل محور (Cluster Actions)
        /// </summary>
        public static readonly Dictionary<string, string[]> ClusterActions = new()
        {
            ["COG"] = new[]
            {
                "اعتماد روتين يومي لتمارين الانتباه والذاكرة (10-15 دقيقة) باستخدام تطبيقات التدريب المعرفي.",
                "تقسيم المشكلات المعقّدة إلى مهام صغيرة قابلة للإدارة مع مهلة زمنية محددة لكل مهمة.",
                "ممارسة الألعاب الذهنية (شطرنج، سودوكو، ألغاز منطقية) 3 مرات أسبوعيًا على الأقل.",
                "تدوين ملاحظات يومية لتعزيز الذاكرة وتنظيم الأفكار، مع مراجعة أسبوعية للتقدم."
            },
            ["EMO"] = new[]
            {
                "تطبيق تقنيات التنفّس الواعي (4-7-8) قبل المواقف الضاغطة للحفاظ على الهدوء.",
                "تتبع المزاج اليومي باستخدام دفتر أو تطبيق، وتحديد المحفّزات السلبية وخطط التهدئة.",
                "ممارسة التأمل اليقظ (Mindfulness) 10 دقائق يوميًا لتعزيز الاستقرار العاطفي.",
                "طلب التغذية الراجعة من الأقران حول ردود الأفعال الانفعالية، والعمل على تحسينها."
            },
            ["SOC"] = new[]
            {
                "تدريب على مهارات الاستماع العاكس (Reflective Listening) وبناء رسائل واضحة ومباشرة.",
                "ممارسة مواقف لعب الدور (Role-play) لتحسين الإقناع والتأثير في الآخرين.",
                "المشاركة في نشاط جماعي (رياضي، تطوعي، مهني) مرة واحدة أسبوعيًا على الأقل.",
                "طلب الإرشاد (Mentorship) من شخص ذو خبرة في التواصل والقيادة الفعّالة."
            },
            ["ORG"] = new[]
            {
                "استخدام جدول أسبوعي مرئي (Kanban، Trello) مع أولويات يومية محددة (3 أهداف كحد أقصى).",
                "مراجعة نهاية الأسبوع: تقييم ما تم إنجازه وما يحتاج ترحيلًا، وتحديد العقبات.",
                "تطبيق تقنية بومودورو (25 دقيقة عمل + 5 دقائق راحة) لتحسين التركيز والانضباط.",
                "تحديد أهداف SMART (محددة، قابلة للقياس، قابلة للتحقيق، ذات صلة، محددة زمنيًا) شهريًا."
            }
        };

        /// <summary>
        /// توليد خطة عملية (Action Plan) بناءً على أضعف 3-5 أبعاد
        /// </summary>
        public static List<ActionItem> GenerateActionPlan(
            IEnumerable<DimensionScore> weakestDimensions,
            Dictionary<string, List<DimensionScore>> clusterGroups)
        {
            var actions = new List<ActionItem>();

            // 1. توصيات للأبعاد الأضعف (3-5 أبعاد)
            foreach (var dim in weakestDimensions.Take(5))
            {
                actions.Add(new ActionItem
                {
                    Title = $"تطوير {dim.Dimension}",
                    Description = MakeActionForWeakDimension(dim.Dimension, dim.T),
                    Priority = dim.T < 30 ? "عالية" : dim.T < 35 ? "متوسطة" : "منخفضة",
                    Timeline = "6-8 أسابيع",
                    KPI = $"رفع T من {ReportTheme.FormatNum(dim.T, 1)} إلى ≥45"
                });
            }

            // 2. توصيات المحاور (Cluster-level)
            var weakestClusters = clusterGroups
                .Where(g => g.Value.Any())
                .OrderBy(g => g.Value.Average(d => d.T))
                .Take(2);

            foreach (var cluster in weakestClusters)
            {
                var clusterName = ClusterNames.GetValueOrDefault(cluster.Key, "المحور");
                var clusterActions = ClusterActions.GetValueOrDefault(cluster.Key, Array.Empty<string>());
                
                if (clusterActions.Length > 0)
                {
                    var selectedAction = clusterActions[0]; // أول توصية
                    actions.Add(new ActionItem
                    {
                        Title = $"تحسين {clusterName}",
                        Description = selectedAction,
                        Priority = "متوسطة",
                        Timeline = "4 أسابيع",
                        KPI = "تنفيذ الإجراء بانتظام 80% من الأيام"
                    });
                }
            }

            return actions;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// تحديد النطاق (Band) بناءً على T-score
        /// Bands: Excellent (≥65), Good (55-64), Average (40-54), Weak (<40)
        /// </summary>
        public static string GetBand(double t)
        {
            if (t >= 65) return "Excellent";
            if (t >= 55) return "Good";
            if (t >= 40) return "Average";
            return "Weak";
        }

        /// <summary>
        /// حساب الانحراف المعياري
        /// </summary>
        private static double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count == 0) return 0;

            var mean = valuesList.Average();
            var sumOfSquares = valuesList.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumOfSquares / valuesList.Count);
        }

        /// <summary>
        /// تنسيق قائمة بأسماء الأبعاد
        /// </summary>
        public static string FormatDimensionList(IEnumerable<DimensionScore> dimensions, int maxItems = 3)
        {
            var names = dimensions.Take(maxItems).Select(d => $"«{d.Dimension}»");
            var formatted = string.Join("، ", names);
            
            var remaining = dimensions.Count() - maxItems;
            if (remaining > 0)
                formatted += $" وأخرى ({remaining})";
            
            return formatted;
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// عنصر خطة العمل (Action Item)
    /// </summary>
    public class ActionItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "متوسطة"; // عالية، متوسطة، منخفضة
        public string Timeline { get; set; } = "";
        public string KPI { get; set; } = "";
    }

    #endregion
}
