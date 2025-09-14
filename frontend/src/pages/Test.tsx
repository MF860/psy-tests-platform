const Test = () => {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-2xl bg-white rounded-xl shadow-lg p-8 space-y-6">
        <h1 className="text-3xl font-bold text-center text-indigo-700 mb-6">
          الاختبار قيد التقدم...
        </h1>

        <div className="flex flex-col items-center justify-center py-12">
          <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-indigo-500 mb-6"></div>
          <p className="text-lg text-gray-600">
            يرجى الانتظار بينما نقوم بتحميل أسئلة الاختبار
          </p>
        </div>
      </div>
    </div>
  );
};

export default Test;
