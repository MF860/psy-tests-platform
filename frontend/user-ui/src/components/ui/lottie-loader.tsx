import Lottie from 'lottie-react'

interface LottieLoaderProps {
  animationData?: any
  size?: 'sm' | 'md' | 'lg' | 'xl'
  loop?: boolean
  autoplay?: boolean
}

// Default animation (you can replace with actual JSON)
const defaultAnimation = {
  v: "5.5.7",
  fr: 60,
  ip: 0,
  op: 60,
  w: 200,
  h: 200,
  nm: "Loading",
  ddd: 0,
  assets: [],
  layers: []
}

const sizeClasses = {
  sm: 'w-16 h-16',
  md: 'w-24 h-24',
  lg: 'w-32 h-32',
  xl: 'w-48 h-48'
}

export function LottieLoader({ 
  animationData = defaultAnimation, 
  size = 'md',
  loop = true,
  autoplay = true 
}: LottieLoaderProps) {
  return (
    <div className={`flex items-center justify-center ${sizeClasses[size]}`}>
      <Lottie
        animationData={animationData}
        loop={loop}
        autoplay={autoplay}
      />
    </div>
  )
}

// Pre-configured loaders for common use cases
export function CelebrationAnimation() {
  return (
    <div className="flex flex-col items-center gap-4">
      <LottieLoader size="xl" loop={false} />
      <p className="text-xl font-bold text-primary">
        أحسنت! تم إكمال الاختبار
      </p>
    </div>
  )
}

export function LoadingAnimation() {
  return (
    <div className="flex flex-col items-center gap-4">
      <LottieLoader size="lg" />
      <p className="text-sm text-muted-foreground animate-pulse">
        جاري التحميل...
      </p>
    </div>
  )
}
