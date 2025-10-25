import { useState } from 'react'
import { motion } from 'framer-motion'
import { 
  Moon, Sun, Sparkles, Brain, Download, 
  TrendingUp, Users, CheckCircle, MessageCircle 
} from 'lucide-react'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs'
import { Separator } from '../components/ui/separator'
import { Avatar, AvatarFallback } from '../components/ui/avatar'
import { ThemeToggle } from '../components/ui/theme-toggle'
import { 
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '../components/ui/tooltip'
import { toast } from 'sonner'

/**
 * Component Showcase
 * 
 * This page demonstrates all the new modern components
 * Use this as a reference when building new features
 */
export default function ComponentShowcase() {
  const [activeTab, setActiveTab] = useState('buttons')

  return (
    <div className="min-h-screen bg-background p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center space-y-4"
        >
          <h1 className="text-4xl font-bold bg-gradient-to-r from-primary to-purple-600 bg-clip-text text-transparent">
            مكتبة المكونات الحديثة
          </h1>
          <p className="text-muted-foreground text-lg">
            جميع المكونات الجديدة جاهزة للاستخدام
          </p>
          <div className="flex justify-center gap-4">
            <ThemeToggle />
          </div>
        </motion.div>

        <Separator />

        {/* Component Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <TabsList className="grid w-full grid-cols-5">
            <TabsTrigger value="buttons">أزرار</TabsTrigger>
            <TabsTrigger value="cards">بطاقات</TabsTrigger>
            <TabsTrigger value="notifications">إشعارات</TabsTrigger>
            <TabsTrigger value="icons">أيقونات</TabsTrigger>
            <TabsTrigger value="animations">تحريكات</TabsTrigger>
          </TabsList>

          {/* Buttons Tab */}
          <TabsContent value="buttons" className="space-y-6">
            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">أنواع الأزرار</h3>
              <div className="flex flex-wrap gap-4">
                <Button>افتراضي</Button>
                <Button variant="secondary">ثانوي</Button>
                <Button variant="destructive">حذف</Button>
                <Button variant="outline">محدد</Button>
                <Button variant="ghost">شفاف</Button>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">أحجام الأزرار</h3>
              <div className="flex flex-wrap items-center gap-4">
                <Button size="sm">صغير</Button>
                <Button size="default">متوسط</Button>
                <Button size="lg">كبير</Button>
                <Button size="icon"><Sparkles className="h-4 w-4" /></Button>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">أزرار مع أيقونات</h3>
              <div className="flex flex-wrap gap-4">
                <Button className="gap-2">
                  <Download className="h-4 w-4" />
                  تحميل
                </Button>
                <Button className="gap-2">
                  <MessageCircle className="h-4 w-4" />
                  مساعد AI
                </Button>
                <Button className="gap-2">
                  <Brain className="h-4 w-4" />
                  تحليل
                </Button>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">Tooltips</h3>
              <div className="flex flex-wrap gap-4">
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button variant="outline">مرر فوقي</Button>
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>هذا tooltip جميل!</p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
            </Card>
          </TabsContent>

          {/* Cards Tab */}
          <TabsContent value="cards" className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Stats Card */}
              <motion.div
                whileHover={{ scale: 1.02 }}
                transition={{ type: 'spring', stiffness: 300 }}
              >
                <Card className="p-6 border-l-4 border-l-blue-500">
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="text-sm text-muted-foreground mb-1">
                        إجمالي الاختبارات
                      </p>
                      <h3 className="text-3xl font-bold">1,234</h3>
                      <div className="flex items-center gap-1 mt-2 text-sm text-green-600">
                        <TrendingUp className="h-4 w-4" />
                        <span>12%</span>
                      </div>
                    </div>
                    <div className="p-3 bg-blue-500/10 rounded-lg">
                      <CheckCircle className="h-6 w-6 text-blue-500" />
                    </div>
                  </div>
                </Card>
              </motion.div>

              {/* User Card */}
              <Card className="p-6">
                <div className="flex items-center gap-4">
                  <Avatar>
                    <AvatarFallback className="bg-primary/10">
                      <Users className="h-5 w-5 text-primary" />
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <h4 className="font-semibold">المستخدمين النشطين</h4>
                    <p className="text-2xl font-bold">567</p>
                  </div>
                </div>
              </Card>

              {/* Glassmorphism Card */}
              <Card className="p-6 glassmorphism">
                <div className="space-y-2">
                  <h4 className="font-semibold">Glassmorphism</h4>
                  <p className="text-sm text-muted-foreground">
                    تأثير زجاجي حديث
                  </p>
                </div>
              </Card>
            </div>
          </TabsContent>

          {/* Notifications Tab */}
          <TabsContent value="notifications" className="space-y-6">
            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">Sonner Toasts</h3>
              <div className="flex flex-wrap gap-4">
                <Button onClick={() => toast.success('تم بنجاح!')}>
                  نجاح
                </Button>
                <Button 
                  variant="destructive"
                  onClick={() => toast.error('حدث خطأ!')}
                >
                  خطأ
                </Button>
                <Button 
                  variant="secondary"
                  onClick={() => toast.info('معلومة مفيدة')}
                >
                  معلومات
                </Button>
                <Button 
                  variant="outline"
                  onClick={() => {
                    const id = toast.loading('جاري التحميل...')
                    setTimeout(() => {
                      toast.success('تم الانتهاء!', { id })
                    }, 2000)
                  }}
                >
                  تحميل
                </Button>
                <Button onClick={() => 
                  toast('هل أنت متأكد؟', {
                    action: {
                      label: 'نعم',
                      onClick: () => toast.success('تم التأكيد')
                    }
                  })
                }>
                  مع إجراء
                </Button>
              </div>
            </Card>
          </TabsContent>

          {/* Icons Tab */}
          <TabsContent value="icons" className="space-y-6">
            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">Lucide Icons</h3>
              <div className="grid grid-cols-6 md:grid-cols-12 gap-4">
                <TooltipProvider>
                  {[
                    { Icon: Sparkles, name: 'AI' },
                    { Icon: Brain, name: 'تحليل' },
                    { Icon: Users, name: 'مستخدمين' },
                    { Icon: CheckCircle, name: 'نجاح' },
                    { Icon: TrendingUp, name: 'زيادة' },
                    { Icon: Download, name: 'تحميل' },
                    { Icon: MessageCircle, name: 'رسائل' },
                    { Icon: Sun, name: 'فاتح' },
                    { Icon: Moon, name: 'داكن' },
                  ].map(({ Icon, name }) => (
                    <Tooltip key={name}>
                      <TooltipTrigger asChild>
                        <div className="p-3 hover:bg-muted rounded-lg cursor-pointer transition-colors">
                          <Icon className="h-6 w-6" />
                        </div>
                      </TooltipTrigger>
                      <TooltipContent>{name}</TooltipContent>
                    </Tooltip>
                  ))}
                </TooltipProvider>
              </div>
            </Card>
          </TabsContent>

          {/* Animations Tab */}
          <TabsContent value="animations" className="space-y-6">
            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">Framer Motion</h3>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <motion.div
                  className="p-8 bg-primary/10 rounded-lg text-center"
                  animate={{ 
                    scale: [1, 1.1, 1],
                    rotate: [0, 5, -5, 0]
                  }}
                  transition={{ 
                    repeat: Infinity, 
                    duration: 2 
                  }}
                >
                  <Sparkles className="h-8 w-8 mx-auto mb-2" />
                  <p className="text-sm">تحريك متكرر</p>
                </motion.div>

                <motion.div
                  className="p-8 bg-secondary/10 rounded-lg text-center"
                  whileHover={{ scale: 1.1, rotate: 5 }}
                  whileTap={{ scale: 0.95 }}
                >
                  <Brain className="h-8 w-8 mx-auto mb-2" />
                  <p className="text-sm">Hover Effect</p>
                </motion.div>

                <motion.div
                  className="p-8 bg-accent/10 rounded-lg text-center"
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.5 }}
                >
                  <CheckCircle className="h-8 w-8 mx-auto mb-2" />
                  <p className="text-sm">Fade In</p>
                </motion.div>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold mb-4">Custom Animations</h3>
              <div className="space-y-4">
                <div className="p-4 bg-muted rounded-lg">
                  <div className="h-2 bg-primary rounded-full animate-pulse" />
                  <p className="text-xs text-center mt-2">Pulse Animation</p>
                </div>
                <div className="p-4 bg-muted rounded-lg">
                  <div className="h-2 bg-gradient-to-r from-primary to-purple-600 rounded-full animate-fade-in" />
                  <p className="text-xs text-center mt-2">Fade In Animation</p>
                </div>
              </div>
            </Card>
          </TabsContent>
        </Tabs>

        {/* Footer */}
        <Card className="p-6 text-center bg-gradient-to-r from-primary/5 to-purple-600/5">
          <h3 className="text-lg font-semibold mb-2">
            جميع المكونات جاهزة للاستخدام!
          </h3>
          <p className="text-sm text-muted-foreground">
            راجع DEVELOPER_GUIDE.md لأمثلة التطبيق
          </p>
        </Card>
      </div>
    </div>
  )
}
