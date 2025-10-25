import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Check, AlertCircle, Info, X } from 'lucide-react';

interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info';
}

const toastIcons = {
  success: Check,
  error: AlertCircle,
  info: Info,
};

const toastStyles = {
  success: 'bg-green-50 text-green-800 border-green-200',
  error: 'bg-red-50 text-red-800 border-red-200',
  info: 'bg-blue-50 text-blue-800 border-blue-200',
};

// Global toast state
const toasts: Toast[] = [];
const listeners: ((toasts: Toast[]) => void)[] = [];

let toastId = 0;

const emitChange = () => {
  listeners.forEach(listener => listener([...toasts]));
};

export const addToast = (message: string, type: 'success' | 'error' | 'info' = 'info') => {
  const id = (++toastId).toString();
  toasts.push({ id, message, type });
  emitChange();
  
  setTimeout(() => {
    const index = toasts.findIndex(toast => toast.id === id);
    if (index > -1) {
      toasts.splice(index, 1);
      emitChange();
    }
  }, 4000);
};

const removeToast = (id: string) => {
  const index = toasts.findIndex(toast => toast.id === id);
  if (index > -1) {
    toasts.splice(index, 1);
    emitChange();
  }
};

export function ToastContainer() {
  const [currentToasts, setCurrentToasts] = useState<Toast[]>([]);

  useEffect(() => {
    const unsubscribe = () => {
      listeners.push(setCurrentToasts);
      return () => {
        const index = listeners.indexOf(setCurrentToasts);
        if (index > -1) {
          listeners.splice(index, 1);
        }
      };
    };
    return unsubscribe();
  }, []);

  return (
    <div className="fixed top-20 right-4 z-50 space-y-2" dir="rtl">
      <AnimatePresence>
        {currentToasts.map((toast) => {
          const Icon = toastIcons[toast.type];
          return (
            <motion.div
              key={toast.id}
              initial={{ opacity: 0, y: -50, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -50, scale: 0.95 }}
              transition={{ duration: 0.2 }}
              className={`flex items-center gap-3 p-3 rounded-lg border shadow-lg min-w-80 ${toastStyles[toast.type]}`}
            >
              <Icon className="h-5 w-5 flex-shrink-0" />
              <p className="flex-1 text-sm font-medium">{toast.message}</p>
              <button
                onClick={() => removeToast(toast.id)}
                className="flex-shrink-0 p-1 hover:bg-black/10 rounded"
              >
                <X className="h-4 w-4" />
              </button>
            </motion.div>
          );
        })}
      </AnimatePresence>
    </div>
  );
}