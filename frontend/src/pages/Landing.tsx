import { useState } from 'react';
import { Link } from 'react-router-dom';

const Landing = () => {
  const [nationalId, setNationalId] = useState('');


  const handleStartTest = (e: React.MouseEvent) => {
    if (!nationalId) {
      e.preventDefault();
      return;
    }

    // Store national ID in localStorage for use in the test page
    localStorage.setItem('nationalId', nationalId);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md bg-white rounded-xl shadow-lg p-8 space-y-6">
        <h1 className="text-3xl font-bold text-center text-indigo-700 mb-6">
          اختبار الشخصية
        </h1>

        <div className="space-y-4">
          <div>
            <label htmlFor="nationalId" className="block text-lg font-medium text-gray-700 mb-2">
              رقم وطني
            </label>
            <input
              id="nationalId"
              type="text"
              value={nationalId}
              onChange={(e) => setNationalId(e.target.value)}
              className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-lg"
              placeholder="أدخل رقمك الوطني"
              dir="rtl"
            />
          </div>

          <Link
            to={nationalId ? "/privacy" : "#"}
            className={`w-full py-3 px-4 rounded-lg text-white font-semibold text-center block transition ${
              nationalId 
                ? "bg-indigo-600 hover:bg-indigo-700" 
                : "bg-gray-400 cursor-not-allowed"
            }`}
            onClick={handleStartTest}
          >
            ابدأ الاختبار
          </Link>
        </div>
      </div>
    </div>
  );
};

export default Landing;
