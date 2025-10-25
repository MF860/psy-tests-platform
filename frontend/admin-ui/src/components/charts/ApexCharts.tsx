import { useEffect, useRef } from 'react'
import ApexCharts from 'apexcharts'

interface TestsTrendChartProps {
  data: Array<{ date: string; count: number }>
  isDark?: boolean
}

export function TestsTrendChart({ data, isDark = false }: TestsTrendChartProps) {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<ApexCharts | null>(null)

  useEffect(() => {
    if (!chartRef.current || !data.length) return

    const options: ApexCharts.ApexOptions = {
      series: [{
        name: 'عدد الاختبارات',
        data: data.map(d => d.count)
      }],
      chart: {
        type: 'area',
        height: 350,
        fontFamily: 'Noto Naskh Arabic, sans-serif',
        toolbar: {
          show: true,
          tools: {
            download: true,
            zoom: true,
            zoomin: true,
            zoomout: true,
            pan: true,
            reset: true
          }
        },
        animations: {
          enabled: true,
          easing: 'easeinout',
          speed: 800,
        },
        background: 'transparent'
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        curve: 'smooth',
        width: 3
      },
      xaxis: {
        categories: data.map(d => d.date),
        labels: {
          style: {
            colors: isDark ? '#9CA3AF' : '#6B7280'
          },
          formatter: (value) => {
            const date = new Date(value)
            return date.toLocaleDateString('ar-SA', { month: 'short', day: 'numeric' })
          }
        }
      },
      yaxis: {
        labels: {
          style: {
            colors: isDark ? '#9CA3AF' : '#6B7280'
          },
          formatter: (value) => Math.round(value).toString()
        }
      },
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.7,
          opacityTo: 0.2,
          stops: [0, 90, 100]
        }
      },
      colors: ['#3B82F6'],
      grid: {
        borderColor: isDark ? '#374151' : '#E5E7EB',
        strokeDashArray: 4
      },
      tooltip: {
        theme: isDark ? 'dark' : 'light',
        y: {
          formatter: (value) => `${value} اختبار`
        }
      }
    }

    chartInstance.current = new ApexCharts(chartRef.current, options)
    chartInstance.current.render()

    return () => {
      chartInstance.current?.destroy()
    }
  }, [data, isDark])

  return <div ref={chartRef} className="w-full" />
}

interface ScoreDistributionChartProps {
  data: Array<{ score: string; count: number }>
  isDark?: boolean
}

export function ScoreDistributionChart({ data, isDark = false }: ScoreDistributionChartProps) {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<ApexCharts | null>(null)

  useEffect(() => {
    if (!chartRef.current || !data.length) return

    const options: ApexCharts.ApexOptions = {
      series: [{
        name: 'عدد الاختبارات',
        data: data.map(d => d.count)
      }],
      chart: {
        type: 'bar',
        height: 350,
        fontFamily: 'Noto Naskh Arabic, sans-serif',
        toolbar: {
          show: true
        },
        background: 'transparent'
      },
      plotOptions: {
        bar: {
          borderRadius: 8,
          distributed: true,
          dataLabels: {
            position: 'top'
          }
        }
      },
      dataLabels: {
        enabled: true,
        offsetY: -20,
        style: {
          fontSize: '12px',
          colors: [isDark ? '#F9FAFB' : '#111827']
        }
      },
      xaxis: {
        categories: data.map(d => d.score),
        labels: {
          style: {
            colors: isDark ? '#9CA3AF' : '#6B7280',
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        labels: {
          style: {
            colors: isDark ? '#9CA3AF' : '#6B7280'
          }
        }
      },
      colors: ['#10B981', '#3B82F6', '#F59E0B', '#EF4444', '#8B5CF6'],
      legend: {
        show: false
      },
      grid: {
        borderColor: isDark ? '#374151' : '#E5E7EB'
      },
      tooltip: {
        theme: isDark ? 'dark' : 'light',
        y: {
          formatter: (value) => `${value} اختبار`
        }
      }
    }

    chartInstance.current = new ApexCharts(chartRef.current, options)
    chartInstance.current.render()

    return () => {
      chartInstance.current?.destroy()
    }
  }, [data, isDark])

  return <div ref={chartRef} className="w-full" />
}

interface DimensionRadarChartProps {
  data: Array<{ dimension: string; score: number }>
  isDark?: boolean
}

export function DimensionRadarChart({ data, isDark = false }: DimensionRadarChartProps) {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<ApexCharts | null>(null)

  useEffect(() => {
    if (!chartRef.current || !data.length) return

    const options: ApexCharts.ApexOptions = {
      series: [{
        name: 'النتيجة',
        data: data.map(d => d.score)
      }],
      chart: {
        type: 'radar',
        height: 400,
        fontFamily: 'Noto Naskh Arabic, sans-serif',
        toolbar: {
          show: false
        },
        background: 'transparent'
      },
      xaxis: {
        categories: data.map(d => d.dimension),
        labels: {
          style: {
            colors: Array(data.length).fill(isDark ? '#9CA3AF' : '#6B7280'),
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        show: false,
        min: 0,
        max: 100
      },
      fill: {
        opacity: 0.2
      },
      stroke: {
        show: true,
        width: 2,
        colors: ['#3B82F6']
      },
      markers: {
        size: 4,
        colors: ['#3B82F6'],
        strokeColors: '#fff',
        strokeWidth: 2
      },
      grid: {
        show: true
      },
      tooltip: {
        theme: isDark ? 'dark' : 'light',
        y: {
          formatter: (value) => `${value.toFixed(1)}%`
        }
      }
    }

    chartInstance.current = new ApexCharts(chartRef.current, options)
    chartInstance.current.render()

    return () => {
      chartInstance.current?.destroy()
    }
  }, [data, isDark])

  return <div ref={chartRef} className="w-full" />
}

interface DonutChartProps {
  data: Array<{ category: string; value: number }>
  isDark?: boolean
}

export function CategoryDonutChart({ data, isDark = false }: DonutChartProps) {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<ApexCharts | null>(null)

  useEffect(() => {
    if (!chartRef.current || !data.length) return

    const options: ApexCharts.ApexOptions = {
      series: data.map(d => d.value),
      chart: {
        type: 'donut',
        height: 350,
        fontFamily: 'Noto Naskh Arabic, sans-serif',
        background: 'transparent'
      },
      labels: data.map(d => d.category),
      colors: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'],
      dataLabels: {
        enabled: true,
        style: {
          fontSize: '14px',
          colors: [isDark ? '#F9FAFB' : '#111827']
        }
      },
      legend: {
        position: 'bottom',
        labels: {
          colors: isDark ? '#9CA3AF' : '#6B7280'
        }
      },
      plotOptions: {
        pie: {
          donut: {
            size: '65%',
            labels: {
              show: true,
              name: {
                show: true,
                fontSize: '16px',
                color: isDark ? '#F9FAFB' : '#111827'
              },
              value: {
                show: true,
                fontSize: '24px',
                fontWeight: 'bold',
                color: isDark ? '#F9FAFB' : '#111827'
              },
              total: {
                show: true,
                label: 'الإجمالي',
                fontSize: '14px',
                color: isDark ? '#9CA3AF' : '#6B7280',
                formatter: (w) => {
                  return w.globals.seriesTotals.reduce((a: number, b: number) => a + b, 0).toString()
                }
              }
            }
          }
        }
      },
      tooltip: {
        theme: isDark ? 'dark' : 'light'
      }
    }

    chartInstance.current = new ApexCharts(chartRef.current, options)
    chartInstance.current.render()

    return () => {
      chartInstance.current?.destroy()
    }
  }, [data, isDark])

  return <div ref={chartRef} className="w-full" />
}
