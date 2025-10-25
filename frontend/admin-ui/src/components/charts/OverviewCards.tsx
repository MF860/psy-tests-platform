import { motion } from 'framer-motion'
import { TrendingUp, TrendingDown, Users, CheckCircle, Clock, Activity } from 'lucide-react'
import { Card } from '../ui/card'

interface OverviewCardProps {
  title: string
  value: string | number
  change?: number
  icon: React.ReactNode
  trend?: 'up' | 'down' | 'neutral'
  subtitle?: string
}

function OverviewCard({ title, value, change, icon, trend = 'neutral', subtitle }: OverviewCardProps) {
  const getTrendColor = () => {
    switch (trend) {
      case 'up': return 'text-green-600 dark:text-green-400'
      case 'down': return 'text-red-600 dark:text-red-400'
      default: return 'text-muted-foreground'
    }
  }

  const getTrendIcon = () => {
    if (trend === 'up') return <TrendingUp className="h-4 w-4" />
    if (trend === 'down') return <TrendingDown className="h-4 w-4" />
    return null
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      whileHover={{ scale: 1.02 }}
    >
      <Card className="p-6 hover:shadow-lg transition-all duration-300 border-l-4 border-l-primary">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-muted-foreground mb-1">{title}</p>
            <motion.h3 
              className="text-3xl font-bold"
              initial={{ scale: 0.8 }}
              animate={{ scale: 1 }}
              transition={{ delay: 0.2, type: 'spring' }}
            >
              {value}
            </motion.h3>
            
            {change !== undefined && (
              <div className={`flex items-center gap-1 mt-2 text-sm font-medium ${getTrendColor()}`}>
                {getTrendIcon()}
                <span>{Math.abs(change)}%</span>
                <span className="text-xs text-muted-foreground">من الشهر الماضي</span>
              </div>
            )}
            
            {subtitle && (
              <p className="text-xs text-muted-foreground mt-1">{subtitle}</p>
            )}
          </div>
          
          <div className="p-3 bg-primary/10 rounded-lg">
            {icon}
          </div>
        </div>
      </Card>
    </motion.div>
  )
}

interface OverviewCardsProps {
  totalTests: number
  activeUsers: number
  completionRate: number
  avgTime: string
  testsChange?: number
  usersChange?: number
  completionChange?: number
}

export function OverviewCards({ 
  totalTests, 
  activeUsers, 
  completionRate, 
  avgTime,
  testsChange,
  usersChange,
  completionChange
}: OverviewCardsProps) {
  const cards = [
    {
      title: 'إجمالي الاختبارات',
      value: totalTests.toLocaleString('ar-SA'),
      change: testsChange,
      trend: testsChange && testsChange > 0 ? 'up' : testsChange && testsChange < 0 ? 'down' : 'neutral',
      icon: <Activity className="h-6 w-6 text-primary" />,
      subtitle: 'إجمالي الاختبارات المكتملة'
    },
    {
      title: 'المستخدمين النشطين',
      value: activeUsers.toLocaleString('ar-SA'),
      change: usersChange,
      trend: usersChange && usersChange > 0 ? 'up' : usersChange && usersChange < 0 ? 'down' : 'neutral',
      icon: <Users className="h-6 w-6 text-primary" />,
      subtitle: 'المستخدمين خلال آخر 30 يوم'
    },
    {
      title: 'معدل الإكمال',
      value: `${completionRate}%`,
      change: completionChange,
      trend: completionChange && completionChange > 0 ? 'up' : completionChange && completionChange < 0 ? 'down' : 'neutral',
      icon: <CheckCircle className="h-6 w-6 text-primary" />,
      subtitle: 'نسبة الاختبارات المكتملة'
    },
    {
      title: 'متوسط الوقت',
      value: avgTime,
      icon: <Clock className="h-6 w-6 text-primary" />,
      subtitle: 'متوسط وقت إتمام الاختبار'
    }
  ]

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {cards.map((card, index) => (
        <motion.div
          key={card.title}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: index * 0.1 }}
        >
          <OverviewCard {...card} trend={card.trend as any} />
        </motion.div>
      ))}
    </div>
  )
}

// Loading skeleton
export function OverviewCardsSkeleton() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {[1, 2, 3, 4].map((i) => (
        <Card key={i} className="p-6">
          <div className="flex items-start justify-between">
            <div className="flex-1 space-y-3">
              <div className="h-4 w-24 bg-muted animate-pulse rounded" />
              <div className="h-8 w-16 bg-muted animate-pulse rounded" />
              <div className="h-3 w-32 bg-muted animate-pulse rounded" />
            </div>
            <div className="h-12 w-12 bg-muted animate-pulse rounded-lg" />
          </div>
        </Card>
      ))}
    </div>
  )
}
