import { useState, useRef, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { MessageCircle, Send, Loader2, Copy, Check, Sparkles, X, History, Archive, Trash2, Plus } from 'lucide-react'
import { Button } from '../ui/button'
import { Input } from '../ui/input'
import { Card } from '../ui/card'
import { Avatar, AvatarFallback } from '../ui/avatar'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../ui/tooltip'
import { toast } from 'sonner'

interface Message {
  role: 'user' | 'assistant'
  content: string
}

interface DisplayMessage extends Message {
  id: string
  timestamp: Date
}

interface ChatSession {
  id: number
  sessionId: string
  userId?: number
  resultId?: number
  messages: Message[]
  title?: string
  status: string
  messageCount: number
  lastActivityAt: string
  createdAt: string
}

interface AIChatAssistantProps {
  resultId: number
  resultData?: any
  onClose?: () => void
}

export function AIChatAssistant({ resultId, resultData, onClose }: AIChatAssistantProps) {
  const [displayMessages, setDisplayMessages] = useState<DisplayMessage[]>([])
  const [input, setInput] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [copiedId, setCopiedId] = useState<string | null>(null)
  const [currentSession, setCurrentSession] = useState<ChatSession | null>(null)
  const [sessions, setSessions] = useState<ChatSession[]>([])
  const [showSessionList, setShowSessionList] = useState(false)
  const [sessionLoading, setSessionLoading] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    scrollToBottom()
  }, [displayMessages])

  useEffect(() => {
    loadSessions()
  }, [resultId])

  const getAuthHeaders = () => ({
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${localStorage.getItem('token')}`
  })

  const loadSessions = async () => {
    try {
      const response = await fetch(`/api/chat-sessions?resultId=${resultId}`, {
        headers: getAuthHeaders()
      })
      if (response.ok) {
        const data = await response.json()
        setSessions(data.sessions || [])
        
        // Load most recent active session if exists
        const activeSession = data.sessions?.find((s: ChatSession) => s.status === 'Active')
        if (activeSession) {
          loadSession(activeSession.sessionId)
        } else {
          // Show welcome message if no session
          setDisplayMessages([{
            id: '1',
            role: 'assistant',
            content: 'مرحباً! أنا مساعد الذكاء الاصطناعي. يمكنني مساعدتك في تفسير نتائج الاختبار النفسي وتقديم رؤى إضافية. ما هو سؤالك؟',
            timestamp: new Date()
          }])
        }
      }
    } catch (error) {
      console.error('Failed to load sessions:', error)
    }
  }

  const loadSession = async (sessionId: string) => {
    try {
      setSessionLoading(true)
      const response = await fetch(`/api/chat-sessions/${sessionId}`, {
        headers: getAuthHeaders()
      })
      if (response.ok) {
        const session: ChatSession = await response.json()
        setCurrentSession(session)
        
        // Convert messages to display format
        const displayMsgs: DisplayMessage[] = session.messages.map((msg, index) => ({
          ...msg,
          id: `${sessionId}-${index}`,
          timestamp: new Date(session.lastActivityAt)
        }))
        
        // Add welcome message if no messages
        if (displayMsgs.length === 0) {
          displayMsgs.push({
            id: '1',
            role: 'assistant',
            content: 'مرحباً! أنا مساعد الذكاء الاصطناعي. يمكنني مساعدتك في تفسير نتائج الاختبار النفسي وتقديم رؤى إضافية. ما هو سؤالك؟',
            timestamp: new Date()
          })
        }
        
        setDisplayMessages(displayMsgs)
      }
    } catch (error) {
      console.error('Failed to load session:', error)
    } finally {
      setSessionLoading(false)
    }
  }

  const createNewSession = async () => {
    try {
      setSessionLoading(true)
      const response = await fetch('/api/chat-sessions', {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify({
          resultId,
          title: 'محادثة جديدة',
          initialMessages: []
        })
      })
      
      if (response.ok) {
        const session: ChatSession = await response.json()
        setCurrentSession(session)
        setSessions(prev => [session, ...prev])
        setDisplayMessages([{
          id: '1',
          role: 'assistant',
          content: 'مرحباً! أنا مساعد الذكاء الاصطناعي. يمكنني مساعدتك في تفسير نتائج الاختبار النفسي وتقديم رؤى إضافية. ما هو سؤالك؟',
          timestamp: new Date()
        }])
        setShowSessionList(false)
        toast.success('تم إنشاء محادثة جديدة')
      }
    } catch (error) {
      toast.error('فشل في إنشاء محادثة جديدة')
    } finally {
      setSessionLoading(false)
    }
  }

  const quickQuestions = [
    'ما هي نقاط القوة الرئيسية؟',
    'ما هي المجالات التي تحتاج إلى تطوير؟',
    'ما هي التوصيات العامة؟',
    'كيف يمكن تحسين النتائج؟'
  ]

  const handleSend = async (text?: string) => {
    const messageText = text || input.trim()
    if (!messageText || isLoading) return

    // Create or ensure we have a session
    if (!currentSession) {
      await createNewSession()
      return
    }

    const userMessage: DisplayMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: messageText,
      timestamp: new Date()
    }

    setDisplayMessages(prev => [...prev, userMessage])
    setInput('')
    setIsLoading(true)

    try {
      // Add message to session
      await fetch(`/api/chat-sessions/${currentSession.sessionId}/messages`, {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify({
          message: { role: 'user', content: messageText }
        })
      })

      // Call AI API with session ID
      const response = await fetch('/api/admin/ai/chat', {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify({
          resultId,
          sessionId: currentSession.sessionId,
          messages: [{ role: 'user', content: messageText }]
        })
      })

      if (!response.ok) {
        throw new Error('فشل في الحصول على رد من الذكاء الاصطناعي')
      }

      const data = await response.json()
      const aiResponse = data.messages?.[0]?.content || 'عذراً، لم أتمكن من معالجة طلبك.'

      const assistantMessage: DisplayMessage = {
        id: Date.now().toString(),
        role: 'assistant',
        content: aiResponse,
        timestamp: new Date()
      }

      setDisplayMessages(prev => [...prev, assistantMessage])

      // Add AI response to session
      await fetch(`/api/chat-sessions/${currentSession.sessionId}/messages`, {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify({
          message: { role: 'assistant', content: aiResponse }
        })
      })

    } catch (error) {
      toast.error('حدث خطأ في الاتصال بالذكاء الاصطناعي')
      
      // Fallback response
      const fallbackMessage: DisplayMessage = {
        id: Date.now().toString(),
        role: 'assistant',
        content: 'عذراً، واجهت مشكلة في معالجة طلبك. يرجى المحاولة مرة أخرى.',
        timestamp: new Date()
      }
      setDisplayMessages(prev => [...prev, fallbackMessage])
    } finally {
      setIsLoading(false)
    }
  }

  const handleCopy = (content: string, id: string) => {
    navigator.clipboard.writeText(content)
    setCopiedId(id)
    toast.success('تم النسخ إلى الحافظة')
    setTimeout(() => setCopiedId(null), 2000)
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const archiveSession = async (sessionId: string) => {
    try {
      await fetch(`/api/chat-sessions/${sessionId}/archive`, {
        method: 'POST',
        headers: getAuthHeaders()
      })
      setSessions(prev => prev.map(s => s.sessionId === sessionId ? { ...s, status: 'Archived' } : s))
      toast.success('تم أرشفة المحادثة')
    } catch (error) {
      toast.error('فشل في أرشفة المحادثة')
    }
  }

  if (showSessionList) {
    return (
      <Card className="flex flex-col h-[600px] max-h-[80vh] w-full max-w-2xl mx-auto shadow-lg border-2">
        {/* Session List Header */}
        <div className="flex items-center justify-between p-4 border-b bg-gradient-to-r from-primary/10 to-primary/5">
          <h3 className="font-semibold text-lg">المحادثات السابقة</h3>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={createNewSession} disabled={sessionLoading}>
              <Plus className="h-4 w-4 mr-1" />
              محادثة جديدة
            </Button>
            <Button variant="ghost" size="icon" onClick={() => setShowSessionList(false)}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Session List */}
        <div className="flex-1 overflow-y-auto p-4 space-y-2">
          {sessions.length === 0 ? (
            <div className="text-center text-muted-foreground py-8">
              <MessageCircle className="h-12 w-12 mx-auto mb-2 opacity-50" />
              <p>لا توجد محادثات سابقة</p>
            </div>
          ) : (
            sessions.map((session) => (
              <Card 
                key={session.sessionId}
                className={`p-3 cursor-pointer hover:bg-muted/50 transition-colors ${
                  currentSession?.sessionId === session.sessionId ? 'bg-primary/10 border-primary' : ''
                }`}
                onClick={() => {
                  loadSession(session.sessionId)
                  setShowSessionList(false)
                }}
              >
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <h4 className="font-medium">{session.title || 'محادثة'}</h4>
                    <p className="text-sm text-muted-foreground">
                      {session.messageCount} رسالة • {new Date(session.lastActivityAt).toLocaleDateString('ar-SA')}
                    </p>
                  </div>
                  <div className="flex gap-1">
                    {session.status === 'Active' && (
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        className="h-6 w-6"
                        onClick={(e) => {
                          e.stopPropagation()
                          archiveSession(session.sessionId)
                        }}
                      >
                        <Archive className="h-3 w-3" />
                      </Button>
                    )}
                  </div>
                </div>
              </Card>
            ))
          )}
        </div>
      </Card>
    )
  }

  return (
    <Card className="flex flex-col h-[600px] max-h-[80vh] w-full max-w-2xl mx-auto shadow-lg border-2">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b bg-gradient-to-r from-primary/10 to-primary/5">
        <div className="flex items-center gap-3">
          <div className="relative">
            <Avatar className="h-10 w-10 bg-primary/20">
              <AvatarFallback>
                <Sparkles className="h-5 w-5 text-primary" />
              </AvatarFallback>
            </Avatar>
            <motion.div
              className="absolute -bottom-1 -right-1 w-3 h-3 bg-green-500 rounded-full border-2 border-white"
              animate={{ scale: [1, 1.2, 1] }}
              transition={{ repeat: Infinity, duration: 2 }}
            />
          </div>
          <div>
            <h3 className="font-semibold text-lg">
              {currentSession?.title || 'مساعد الذكاء الاصطناعي'}
            </h3>
            <p className="text-xs text-muted-foreground">متصل الآن</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" size="icon" onClick={() => setShowSessionList(true)}>
            <History className="h-4 w-4" />
          </Button>
          {onClose && (
            <Button variant="ghost" size="icon" onClick={onClose}>
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-muted/20">
        {sessionLoading ? (
          <div className="flex justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin" />
          </div>
        ) : (
          <AnimatePresence>
            {displayMessages.map((message, index) => (
              <motion.div
                key={message.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                transition={{ delay: index * 0.05 }}
                className={`flex gap-3 ${message.role === 'user' ? 'flex-row-reverse' : ''}`}
              >
                <Avatar className={`h-8 w-8 ${message.role === 'assistant' ? 'bg-primary/20' : 'bg-secondary'}`}>
                  <AvatarFallback>
                    {message.role === 'assistant' ? (
                      <Sparkles className="h-4 w-4 text-primary" />
                    ) : (
                      'أنت'
                    )}
                  </AvatarFallback>
                </Avatar>

                <div className={`flex-1 ${message.role === 'user' ? 'text-right' : ''}`}>
                  <Card className={`p-3 ${
                    message.role === 'user' 
                      ? 'bg-primary text-primary-foreground ml-8' 
                      : 'bg-card mr-8'
                  }`}>
                    <p className="text-sm whitespace-pre-wrap leading-relaxed">
                      {message.content}
                    </p>
                  </Card>

                  <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                    <span>{message.timestamp.toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit' })}</span>
                    {message.role === 'assistant' && (
                      <TooltipProvider>
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <button
                              onClick={() => handleCopy(message.content, message.id)}
                              className="hover:text-foreground transition-colors"
                            >
                              {copiedId === message.id ? (
                                <Check className="h-3 w-3" />
                              ) : (
                                <Copy className="h-3 w-3" />
                              )}
                            </button>
                          </TooltipTrigger>
                          <TooltipContent>نسخ الرسالة</TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    )}
                  </div>
                </div>
              </motion.div>
            ))}
          </AnimatePresence>
        )}

        {isLoading && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="flex gap-3"
          >
            <Avatar className="h-8 w-8 bg-primary/20">
              <AvatarFallback>
                <Sparkles className="h-4 w-4 text-primary" />
              </AvatarFallback>
            </Avatar>
            <Card className="p-3 bg-card">
              <div className="flex gap-1">
                <motion.div
                  className="w-2 h-2 bg-primary rounded-full"
                  animate={{ scale: [1, 1.5, 1] }}
                  transition={{ repeat: Infinity, duration: 0.8, delay: 0 }}
                />
                <motion.div
                  className="w-2 h-2 bg-primary rounded-full"
                  animate={{ scale: [1, 1.5, 1] }}
                  transition={{ repeat: Infinity, duration: 0.8, delay: 0.2 }}
                />
                <motion.div
                  className="w-2 h-2 bg-primary rounded-full"
                  animate={{ scale: [1, 1.5, 1] }}
                  transition={{ repeat: Infinity, duration: 0.8, delay: 0.4 }}
                />
              </div>
            </Card>
          </motion.div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Quick Questions */}
      {displayMessages.length === 1 && (
        <div className="px-4 py-2 border-t bg-muted/10">
          <p className="text-xs text-muted-foreground mb-2">أسئلة سريعة:</p>
          <div className="flex flex-wrap gap-2">
            {quickQuestions.map((question, index) => (
              <Button
                key={index}
                variant="outline"
                size="sm"
                onClick={() => handleSend(question)}
                className="text-xs"
                disabled={isLoading}
              >
                {question}
              </Button>
            ))}
          </div>
        </div>
      )}

      {/* Input */}
      <div className="p-4 border-t bg-background">
        <div className="flex gap-2">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="اكتب سؤالك هنا..."
            disabled={isLoading}
            className="flex-1"
            dir="rtl"
          />
          <Button
            onClick={() => handleSend()}
            disabled={!input.trim() || isLoading}
            size="icon"
            className="shrink-0"
          >
            {isLoading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Send className="h-4 w-4" />
            )}
          </Button>
        </div>
        <p className="text-xs text-muted-foreground mt-2 text-center">
          اضغط Enter للإرسال، Shift+Enter لسطر جديد
        </p>
      </div>
    </Card>
  )
}

export default AIChatAssistant
