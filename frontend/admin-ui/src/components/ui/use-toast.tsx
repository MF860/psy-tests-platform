import * as React from 'react'
import { createPortal } from 'react-dom'

import { cn } from '@/lib/utils'

export type ToastOptions = {
  id?: string
  title?: string
  description?: string
  duration?: number
  variant?: 'default' | 'destructive'
}

type ToastRecord = Required<Pick<ToastOptions, 'id'>> & Omit<ToastOptions, 'id'>

type ToastContextValue = {
  toasts: ToastRecord[]
  toast: (options: ToastOptions) => string
  dismiss: (id: string) => void
}

const ToastContext = React.createContext<ToastContextValue | undefined>(undefined)

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = React.useState<ToastRecord[]>([])
  const timeouts = React.useRef<Map<string, number>>(new Map())

  const dismiss = React.useCallback((id: string) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
    const timeoutId = timeouts.current.get(id)
    if (timeoutId !== undefined) {
      window.clearTimeout(timeoutId)
      timeouts.current.delete(id)
    }
  }, [])

  const toast = React.useCallback(
    ({ id: providedId, duration, ...options }: ToastOptions) => {
      const id = providedId ?? Math.random().toString(36).slice(2)
      setToasts((current) => {
        const existing = current.filter((toast) => toast.id !== id)
        return [...existing, { id, duration, ...options }]
      })

      const timeout = window.setTimeout(() => dismiss(id), duration ?? 4000)
      timeouts.current.set(id, timeout)
      return id
    },
    [dismiss],
  )

  React.useEffect(() => {
    return () => {
      timeouts.current.forEach((timeoutId) => window.clearTimeout(timeoutId))
      timeouts.current.clear()
    }
  }, [])

  const value = React.useMemo<ToastContextValue>(() => ({ toasts, toast, dismiss }), [toasts, toast, dismiss])

  return (
    <ToastContext.Provider value={value}>
      {children}
      <ToastViewport toasts={toasts} dismiss={dismiss} />
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = React.useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return { toast: context.toast, dismiss: context.dismiss }
}

export function Toaster() {
  const context = React.useContext(ToastContext)
  if (!context) {
    throw new Error('Toaster must be used within a ToastProvider')
  }
  return <ToastViewport toasts={context.toasts} dismiss={context.dismiss} />
}

function ToastViewport({ toasts, dismiss }: { toasts: ToastRecord[]; dismiss: (id: string) => void }) {
  const portalTarget = typeof document !== 'undefined' ? document.body : null

  if (!portalTarget) {
    return null
  }

  return createPortal(
    <div className="pointer-events-none fixed inset-x-0 top-4 z-50 flex flex-col items-center gap-3 px-4 sm:items-end sm:px-6">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={cn(
            'pointer-events-auto w-full max-w-sm rounded-md border bg-white p-4 shadow-lg ring-1 ring-black/5 transition',
            toast.variant === 'destructive' ? 'border-red-200 bg-red-50 text-red-800' : 'border-slate-200 text-slate-900',
          )}
          role="status"
          aria-live="polite"
        >
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              {toast.title ? <p className="font-semibold">{toast.title}</p> : null}
              {toast.description ? <p className="text-sm text-slate-600">{toast.description}</p> : null}
            </div>
            <button
              type="button"
              className="text-sm font-medium text-slate-500 transition hover:text-slate-900"
              onClick={() => dismiss(toast.id)}
            >
              Close
            </button>
          </div>
        </div>
      ))}
    </div>,
    portalTarget,
  )
}
