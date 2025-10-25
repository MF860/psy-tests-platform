import { Download } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface CsvExportProps {
  /** Data to export */
  data: Array<Record<string, any>>
  /** Filename for the CSV */
  filename: string
  /** Column headers mapping */
  headers?: Record<string, string>
  /** Button variant */
  variant?: "default" | "outline" | "ghost"
  /** Button size */
  size?: "default" | "sm" | "lg"
}

export function CsvExport({ 
  data, 
  filename, 
  headers = {},
  variant = "outline",
  size = "sm" 
}: CsvExportProps) {
  const handleExport = () => {
    if (!data.length) return

    // Get all unique keys from the data
    const allKeys = new Set<string>()
    data.forEach(item => {
      Object.keys(item).forEach(key => allKeys.add(key))
    })

    // Create CSV headers
    const csvHeaders = Array.from(allKeys).map(key => headers[key] || key)
    
    // Create CSV rows
    const csvRows = data.map(item => {
      return Array.from(allKeys).map(key => {
        const value = item[key]
        // Handle values that might contain commas or quotes
        if (typeof value === 'string' && (value.includes(',') || value.includes('"'))) {
          return `"${value.replace(/"/g, '""')}"`
        }
        return value || ''
      })
    })

    // Combine headers and rows
    const csvContent = [csvHeaders, ...csvRows]
      .map(row => row.join(','))
      .join('\n')

    // Create and trigger download
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob)
      link.setAttribute('href', url)
      link.setAttribute('download', `${filename}.csv`)
      link.style.visibility = 'hidden'
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)
    }
  }

  return (
    <Button 
      variant={variant} 
      size={size}
      onClick={handleExport}
      disabled={!data.length}
      className="flex items-center gap-2"
    >
      <Download className="h-4 w-4" />
      تحميل CSV
    </Button>
  )
}