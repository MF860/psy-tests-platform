import { ChevronUp, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ColumnHeaderProps {
  /** Column title */
  title: string
  /** Sort key for this column */
  sortKey?: string
  /** Current sort state */
  currentSort?: {
    by?: string
    dir?: 'asc' | 'desc'
  }
  /** Sort change handler */
  onSortChange?: (sortKey: string) => void
  /** Additional CSS classes */
  className?: string
  /** Whether column is sortable */
  sortable?: boolean
}

export function ColumnHeader({
  title,
  sortKey,
  currentSort,
  onSortChange,
  className = '',
  sortable = false
}: ColumnHeaderProps) {
  const isActive = currentSort?.by === sortKey
  const direction = currentSort?.dir

  const handleClick = () => {
    if (!sortable || !sortKey || !onSortChange) return
    onSortChange(sortKey)
  }

  if (!sortable) {
    return (
      <th className={cn(
        "px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider",
        className
      )}>
        {title}
      </th>
    )
  }

  return (
    <th className={cn(
      "px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider",
      className
    )}>
      <button
        onClick={handleClick}
        className="flex items-center gap-1 hover:text-gray-700 transition-colors group"
      >
        <span>{title}</span>
        <div className="flex flex-col">
          <ChevronUp className={cn(
            "h-3 w-3 transition-colors",
            isActive && direction === 'asc' 
              ? "text-blue-600" 
              : "text-gray-300 group-hover:text-gray-400"
          )} />
          <ChevronDown className={cn(
            "h-3 w-3 -mt-1 transition-colors",
            isActive && direction === 'desc' 
              ? "text-blue-600" 
              : "text-gray-300 group-hover:text-gray-400"
          )} />
        </div>
      </button>
    </th>
  )
}

/**
 * Loading skeleton rows for table
 */
interface LoadingRowsProps {
  /** Number of columns */
  columns: number
  /** Number of rows to show */
  rows?: number
}

export function LoadingRows({ columns, rows = 10 }: LoadingRowsProps) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <tr key={i} className="border-b">
          {Array.from({ length: columns }).map((_, j) => (
            <td key={j} className="px-6 py-4">
              <div className="animate-pulse bg-gray-200 rounded h-4 w-full" />
            </td>
          ))}
        </tr>
      ))}
    </>
  )
}