import { Badge } from '@/components/ui/badge'
import { CheckCircle, Clock, Copy, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useState } from 'react'

interface ScoreBadgeProps {
  /** T-Score value */
  score: number
  /** Additional CSS classes */
  className?: string
}

/**
 * Score badge with color coding:
 * - Red: < 40 (Below Average)
 * - Orange: 40-54.9 (Average)  
 * - Green: >= 55 (Above Average)
 */
export function ScoreBadge({ score, className = '' }: ScoreBadgeProps) {
  const getScoreBand = (score: number) => {
    if (score >= 55) return { color: 'green', label: 'مرتفع', variant: 'default' as const }
    if (score >= 40) return { color: 'orange', label: 'متوسط', variant: 'secondary' as const }
    return { color: 'red', label: 'منخفض', variant: 'destructive' as const }
  }

  const band = getScoreBand(score)
  const formattedScore = new Intl.NumberFormat('ar-EG', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(score)

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <Badge 
        variant={band.variant}
        className="text-xs"
      >
        {formattedScore}
      </Badge>
      <span className={cn(
        "text-xs font-medium",
        band.color === 'green' && "text-green-600",
        band.color === 'orange' && "text-orange-600", 
        band.color === 'red' && "text-red-600"
      )}>
        {band.label}
      </span>
    </div>
  )
}

interface SessionStatusBadgeProps {
  /** Session ID - if present, shows active badge */
  sessionId?: string | number
  /** Additional CSS classes */
  className?: string
}

/**
 * Session status badge - indicates if result has an active session
 */
export function SessionStatusBadge({ sessionId, className = '' }: SessionStatusBadgeProps) {
  if (!sessionId) {
    return (
      <Badge variant="outline" className={`text-xs ${className}`}>
        <Clock className="h-3 w-3 mr-1" />
        مُكتمل
      </Badge>
    )
  }

  return (
    <Badge variant="default" className={`text-xs bg-green-100 text-green-800 ${className}`}>
      <CheckCircle className="h-3 w-3 mr-1" />
      نشِط
    </Badge>
  )
}

interface CopyableNationalIdProps {
  /** National ID to display and copy */
  nationalId: string
  /** Additional CSS classes */
  className?: string
}

/**
 * Copyable national ID with click-to-copy functionality
 */
export function CopyableNationalId({ nationalId, className = '' }: CopyableNationalIdProps) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(nationalId)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      console.error('Failed to copy:', err)
    }
  }

  return (
    <button
      onClick={handleCopy}
      className={cn(
        "flex items-center gap-1 text-sm font-mono hover:bg-gray-100 rounded px-1 py-0.5 transition-colors",
        className
      )}
      title={copied ? "تم النسخ!" : "انقر للنسخ"}
    >
      <span>{nationalId}</span>
      <Copy className={cn(
        "h-3 w-3",
        copied ? "text-green-600" : "text-gray-400"
      )} />
      {copied && (
        <span className="text-xs text-green-600 font-normal">تم النسخ!</span>
      )}
    </button>
  )
}

interface ValidationBadgeProps {
  /** Whether the result passed validation */
  isValid?: boolean
  /** Validation message */
  message?: string
  /** Additional CSS classes */
  className?: string
}

/**
 * Validation status badge for results
 */
export function ValidationBadge({ 
  isValid = true, 
  message, 
  className = '' 
}: ValidationBadgeProps) {
  if (isValid) {
    return (
      <Badge variant="outline" className={`text-xs text-green-600 border-green-200 ${className}`}>
        <CheckCircle className="h-3 w-3 mr-1" />
        صالح
      </Badge>
    )
  }

  return (
    <Badge 
      variant="outline" 
      className={`text-xs text-orange-600 border-orange-200 ${className}`}
      title={message}
    >
      <AlertTriangle className="h-3 w-3 mr-1" />
      يحتاج مراجعة
    </Badge>
  )
}