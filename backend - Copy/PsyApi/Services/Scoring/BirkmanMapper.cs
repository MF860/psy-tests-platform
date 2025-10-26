using PsyApi.Models;

namespace PsyApi.Services.Scoring
{
    public class BirkmanMapper
    {
        // Map dimensions to Birkman axes
        public static BirkmanAxes MapToBirkmanAxes(IEnumerable<DimensionScore> dimensions)
        {
            var dimsList = dimensions.ToList();

            // Relation: communication / emotional intelligence / relationships
            var relationDims = dimsList.Where(d =>
                d.Dimension.Contains("ذكاء عاطفي") ||
                d.Dimension.Contains("تواصل") ||
                d.Dimension.Contains("علاقات")
            ).ToList();
            var relationScore = relationDims.Any() ? relationDims.Average(d => d.T) : 50;

            // Task: organization / problem solving / planning
            var taskDims = dimsList.Where(d =>
                d.Dimension.Contains("تنظيم") ||
                d.Dimension.Contains("حل المشكلات") ||
                d.Dimension.Contains("تخطيط")
            ).ToList();
            var taskScore = taskDims.Any() ? taskDims.Average(d => d.T) : 50;

            // Stress: pressure / flexibility / tension
            var stressDims = dimsList.Where(d =>
                d.Dimension.Contains("ضغط") ||
                d.Dimension.Contains("مرونة") ||
                d.Dimension.Contains("توتر")
            ).ToList();
            var stressScore = stressDims.Any() ? stressDims.Average(d => d.T) : 50;

            // Energy: self-learning / creativity / energy
            var energyDims = dimsList.Where(d =>
                d.Dimension.Contains("تعلم ذاتي") ||
                d.Dimension.Contains("إبداع") ||
                d.Dimension.Contains("طاقة")
            ).ToList();
            var energyScore = energyDims.Any() ? energyDims.Average(d => d.T) : 50;

            // Normalize to 0-100 scale
            return new BirkmanAxes
            {
                Relation = Math.Max(0, Math.Min(100, (relationScore - 20) * 100 / 60)),
                Task = Math.Max(0, Math.Min(100, (taskScore - 20) * 100 / 60)),
                Stress = Math.Max(0, Math.Min(100, (stressScore - 20) * 100 / 60)),
                Energy = Math.Max(0, Math.Min(100, (energyScore - 20) * 100 / 60))
            };
        }

        // Arabic recommendations (UTF-8 safe)
        public static List<string> GenerateRecommendations(BirkmanAxes axes, List<string> strengths, List<string> risks)
        {
            var recommendations = new List<string>();

            if (axes.Relation < 40)
            {
                recommendations.Add("عزّز مهارات التواصل النشط والاستماع والتغذية الراجعة البنّاءة.");
                recommendations.Add("خطّط لاجتماعات دورية مع الفريق لبناء الثقة وتقوية العلاقات.");
            }
            else if (axes.Relation > 70)
            {
                recommendations.Add("استثمر قدرتك على بناء العلاقات في التوجيه والإرشاد للآخرين.");
                recommendations.Add("شارك في مبادرات التعاون بين الفرق لرفع التأثير المؤسسي.");
            }

            if (axes.Task < 40)
            {
                recommendations.Add("استخدم أدوات إدارة المهام وحدد أولويات واضحة بمؤشرات قياس.");
                recommendations.Add("قسّم المشكلات الكبيرة إلى مهام صغيرة مع مواعيد نهائية محددة.");
            }
            else if (axes.Task > 70)
            {
                recommendations.Add("قم بقيادة مبادرات تحسين العمليات وتوثيق الإجراءات القياسية.");
                recommendations.Add("درّب الزملاء على تقنيات التخطيط وحل المشكلات المتقدمة.");
            }

            if (axes.Stress < 40)
            {
                recommendations.Add("طوّر استراتيجيات إدارة الضغط مثل التنفس العميق وتنظيم الوقت.");
                recommendations.Add("ضع حدودًا واضحة لساعات العمل وتبنَّ نمط استراحة منتظم.");
            }
            else if (axes.Stress > 70)
            {
                recommendations.Add("استثمر هدوءك تحت الضغط في المهام الحرجة وادعم زملاءك.");
                recommendations.Add("شارك أفضل ممارساتك لإدارة التوتر مع الفريق.");
            }

            if (axes.Energy < 40)
            {
                recommendations.Add("نوّع المهام اليومية وأدرج أنشطة تعلّم قصيرة لتحفيز الطاقة.");
                recommendations.Add("حدّد أوقات الذروة لديك لإنجاز المهام التي تتطلب تركيزًا عاليًا.");
            }
            else if (axes.Energy > 70)
            {
                recommendations.Add("وجّه طاقتك نحو الابتكار والمشاريع الريادية داخل الفريق.");
                recommendations.Add("تطوّع لقيادة مبادرات تتطلب حماسًا وتحريكًا للآخرين.");
            }

            return recommendations;
        }

        public static List<CourseRecommendation> GenerateCourses(BirkmanAxes axes)
        {
            var courses = new List<CourseRecommendation>();

            if (axes.Relation < 40)
            {
                courses.Add(new CourseRecommendation { Title = "أساسيات التواصل الفعّال", Description = "تقنيات الإصغاء، طرح الأسئلة، وإدارة الحوار.", Level = "مبتدئ", Duration = "4 أسابيع" });
            }
            if (axes.Task < 40)
            {
                courses.Add(new CourseRecommendation { Title = "إدارة الوقت والأولويات", Description = "أطر تحديد الأهداف وقياس الأداء الشخصي.", Level = "مبتدئ", Duration = "3 أسابيع" });
            }
            if (axes.Stress < 40)
            {
                courses.Add(new CourseRecommendation { Title = "إدارة الضغط والمرونة النفسية", Description = "أدوات عملية للتعامل مع الضغوط اليومية.", Level = "متوسط", Duration = "4 أسابيع" });
            }
            if (axes.Energy < 40)
            {
                courses.Add(new CourseRecommendation { Title = "تحفيز الذات والتعلّم المستمر", Description = "بناء عادات فعّالة للتعلم والإنتاجية.", Level = "مبتدئ", Duration = "4 أسابيع" });
            }
            if (axes.Task > 70)
            {
                courses.Add(new CourseRecommendation { Title = "قيادة العمليات وتحسينها", Description = "خرائط تدفّق العمليات ومؤشرات الجودة.", Level = "متقدم", Duration = "6 أسابيع" });
            }
            if (axes.Relation > 70)
            {
                courses.Add(new CourseRecommendation { Title = "القيادة بالتأثير وبناء الفرق", Description = "التأثير غير الرسمي وإدارة أصحاب المصلحة.", Level = "متقدم", Duration = "6 أسابيع" });
            }

            return courses;
        }
    }

    public class BirkmanAxes
    {
        public double Relation { get; set; }
        public double Task { get; set; }
        public double Stress { get; set; }
        public double Energy { get; set; }
    }

    public class CourseRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }
}

