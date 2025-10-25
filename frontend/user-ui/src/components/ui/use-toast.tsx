import { useCallback } from 'react';

interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info';
}

const toasts: Toast[] = [];
const listeners: ((toasts: Toast[]) => void)[] = [];

let toastId = 0;

const emitChange = () => {
  listeners.forEach(listener => listener(toasts));
};

const addToast = (message: string, type: 'success' | 'error' | 'info' = 'info') => {
  const id = (++toastId).toString();
  toasts.push({ id, message, type });
  emitChange();
  
  setTimeout(() => {
    const index = toasts.findIndex(toast => toast.id === id);
    if (index > -1) {
      toasts.splice(index, 1);
      emitChange();
    }
  }, 3000);
};

export const useToast = () => {
  const toast = useCallback((message: string, type: 'success' | 'error' | 'info' = 'info') => {
    addToast(message, type);
  }, []);

  return { toast };
};