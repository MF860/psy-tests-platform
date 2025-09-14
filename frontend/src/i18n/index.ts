import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

// the translations
const resources = {
  ar: {
    translation: {
      "welcome": "مرحباً بك",
      "nationalId": "رقم وطني",
      "startTest": "ابدأ الاختبار",
      "privacyPolicy": "سياسة الخصوصية",
      "agree": "أوافق",
      "testInProgress": "الاختبار قيد التقدم...",
      "thankYou": "شكراً لك",
      "testSubmitted": "لقد تم إرسال إجاباتك بنجاح",
      "backToHome": "العودة إلى الصفحة الرئيسية"
    }
  }
};

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: "ar",
    fallbackLng: "ar",
    interpolation: {
      escapeValue: false // react already escapes by default
    }
  });

export default i18n;
