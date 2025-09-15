import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  startSession, 
  getNextQuestion, 
  submitAnswer, 
  submitTest,

} from '../api/psyApi';
import type { QuestionResponse } from '../api/psyApi';

const Test = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [question, setQuestion] = useState<QuestionResponse | null>(null);
  const [answer, setAnswer] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    const initializeTest = async () => {
      try {
        // Get national ID from localStorage
        const nationalId = localStorage.getItem('nationalId');
        if (!nationalId) {
          navigate('/');
          return;
        }

        // Start session
        const sessionResponse = await startSession(nationalId);
        setSessionId(sessionResponse.sessionId);

        // Get first question
        const questionResponse = await getNextQuestion(sessionResponse.sessionId);

        if ('message' in questionResponse && questionResponse.message === 'completed') {
          // If no questions, submit test and redirect
          await submitTest(sessionResponse.sessionId);
          navigate('/end');
          return;
        }

        setQuestion(questionResponse as QuestionResponse);
      } catch (err) {
        console.error('Error initializing test:', err);
        setError('فشل في تحميل الاختبار. يرجى المحاولة مرة أخرى.');
      } finally {
        setLoading(false);
      }
    };

    initializeTest();
  }, [navigate]);

  const handleAnswerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!sessionId || !question || !answer.trim()) return;

    setSubmitting(true);
    try {
      // Submit answer
      await submitAnswer(sessionId, question.id.toString(), answer);

      // Get next question
      const nextQuestion = await getNextQuestion(sessionId);

      if ('message' in nextQuestion && nextQuestion.message === 'completed') {
        // If no more questions, submit test and redirect
        await submitTest(sessionId);
        navigate('/end');
        return;
      }

      // Update question and reset answer
      setQuestion(nextQuestion as QuestionResponse);
      setAnswer('');
    } catch (err) {
      console.error('Error submitting answer:', err);
      setError('فشل في إرسال الإجابة. يرجى المحاولة مرة أخرى.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
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
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
        <div className="w-full max-w-2xl bg-white rounded-xl shadow-lg p-8 space-y-6">
          <h1 className="text-3xl font-bold text-center text-red-600 mb-6">
            خطأ
          </h1>
          <p className="text-lg text-gray-700 text-center">{error}</p>
          <div className="flex justify-center mt-6">
            <button
              onClick={() => window.location.reload()}
              className="py-2 px-6 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-lg transition"
            >
              إعادة المحاولة
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (!question) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
        <div className="w-full max-w-2xl bg-white rounded-xl shadow-lg p-8 space-y-6">
          <h1 className="text-3xl font-bold text-center text-indigo-700 mb-6">
            خطأ
          </h1>
          <p className="text-lg text-gray-700 text-center">لم يتم العثور على سؤال</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-2xl bg-white rounded-xl shadow-lg p-8 space-y-6">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold text-indigo-700">
            اختبار الشخصية
          </h1>
          <div className="text-sm text-gray-500">
            النوع: {question.type}
          </div>
        </div>

        <div className="bg-indigo-50 rounded-lg p-6 mb-6">
          <p className="text-xl text-gray-800 leading-relaxed" dir="rtl">
            {question.text_ar}
          </p>
        </div>

        <form onSubmit={handleAnswerSubmit} className="space-y-6">
          {question.type === 'MCQ' && question.options ? (
            <div className="space-y-3" dir="rtl">
              {question.options.map((option, index) => (
                <label key={index} className="flex items-center p-3 bg-gray-50 rounded-lg hover:bg-gray-100 cursor-pointer transition">
                  <input
                    type="radio"
                    name="answer"
                    value={option}
                    checked={answer === option}
                    onChange={() => setAnswer(option)}
                    className="ml-3 h-5 w-5 text-indigo-600"
                  />
                  <span className="text-lg">{option}</span>
                </label>
              ))}
            </div>
          ) : (
            <div className="space-y-3" dir="rtl">
              <label htmlFor="answer" className="block text-lg font-medium text-gray-700">
                إجابتك:
              </label>
              <input
                type="text"
                id="answer"
                value={answer}
                onChange={(e) => setAnswer(e.target.value)}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-lg"
                placeholder="أدخل إجابتك هنا"
                dir="rtl"
              />
            </div>
          )}

          <div className="flex justify-center">
            <button
              type="submit"
              disabled={submitting || !answer.trim()}
              className={`py-3 px-8 rounded-lg text-white font-semibold text-lg transition ${
                submitting || !answer.trim()
                  ? "bg-gray-400 cursor-not-allowed"
                  : "bg-indigo-600 hover:bg-indigo-700"
              }`}
            >
              {submitting ? (
                <span className="flex items-center">
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  جاري الإرسال...
                </span>
              ) : (
                "إرسال الإجابة"
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default Test;
