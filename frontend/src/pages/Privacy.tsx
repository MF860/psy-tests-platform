import { Link } from 'react-router-dom';

const Privacy = () => {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-2xl bg-white rounded-xl shadow-lg p-8 space-y-6">
        <h1 className="text-3xl font-bold text-center text-indigo-700 mb-6">
          سياسة الخصوصية
        </h1>

        <div className="space-y-4 text-gray-700 text-lg leading-relaxed" dir="rtl">
          <p>
            نحن في منصة الاختبارات النفسية نلتزم بحماية خصوصيتك وبياناتك الشخصية. توضح هذه السياسة كيفية جمعنا واستخدامنا ومشاركتنا للمعلومات التي تقدمها لنا عند استخدامك لخدماتنا.
          </p>

          <h2 className="text-xl font-semibold text-indigo-600 mt-6">جمع المعلومات</h2>
          <p>
            نقوم بجمع المعلومات الشخصية التي تقدمها لنا طواعية، مثل الرقم الوطني وغيرها من البيانات اللازمة لإجراء الاختبارات النفسية. كما قد نجمع بيانات حول استخدامك للمنصة لتحسين خدماتنا.
          </p>

          <h2 className="text-xl font-semibold text-indigo-600 mt-6">استخدام المعلومات</h2>
          <p>
            نستخدم المعلومات التي نجمعها لتقديم خدماتنا، وتحسين تجربتك، وتطوير منتجاتنا، وإرسال معلومات تقنية أو تحديثات أو عروض ترويجية قد تهمك.
          </p>

          <h2 className="text-xl font-semibold text-indigo-600 mt-6">مشاركة المعلومات</h2>
          <p>
            لا نبيع أو نؤجر أو نتاجر بمعلوماتك الشخصية لأطراف ثالثة. قد نشارك معلوماتك فقط مع موفري الخدمات الذين يساعدوننا في تشغيل منصتنا، وبشكل يضمن حماية خصوصيتك.
          </p>

          <h2 className="text-xl font-semibold text-indigo-600 mt-6">حماية المعلومات</h2>
          <p>
            نتخذ تدابير أمنية مناسبة لحماية بياناتك من الوصول غير المصرح به أو الاستخدام أو التغيير أو الإتلاف. ومع ذلك، لا يمكننا ضمان أمان المعلومات المرسلة عبر الإنترنت بشكل مطلق.
          </p>

          <h2 className="text-xl font-semibold text-indigo-600 mt-6">حقوقك</h2>
          <p>
            لديك الحق في الوصول إلى بياناتك الشخصية وتصحيحها وحذفها. لممارسة هذه الحقوق، يرجى الاتصال بنا باستخدام المعلومات المقدمة في قسم "اتصل بنا".
          </p>
        </div>

        <div className="flex justify-center mt-8">
          <Link
            to="/test"
            className="py-3 px-6 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-lg transition"
          >
            أوافق
          </Link>
        </div>
      </div>
    </div>
  );
};

export default Privacy;
