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
  layers: [
    {
      ddd: 0,
      ind: 1,
      ty: 4,
      nm: "Circle",
      sr: 1,
      ks: {
        o: { a: 0, k: 100 },
        r: { a: 1, k: [{ t: 0, s: [0], e: [360] }, { t: 60 }] },
        p: { a: 0, k: [100, 100, 0] },
        a: { a: 0, k: [0, 0, 0] },
        s: { a: 0, k: [100, 100, 100] }
      },
      shapes: []
    }
  ]
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
export function BrainLoader() {
  return (
    <div className="flex flex-col items-center gap-4">
      <LottieLoader size="lg" />
      <p className="text-sm text-muted-foreground animate-pulse">
        جاري التحليل بالذكاء الاصطناعي...
      </p>
    </div>
  )
}

export function SuccessAnimation() {
  return (
    <div className="flex flex-col items-center gap-4">
      <LottieLoader size="xl" loop={false} />
      <p className="text-lg font-semibold text-green-600">
        تم بنجاح!
      </p>
    </div>
  )
}

export function DocumentLoader() {
  return (
    <div className="flex flex-col items-center gap-4">
      <LottieLoader size="lg" />
      <p className="text-sm text-muted-foreground animate-pulse">
        جاري إنشاء التقرير...
      </p>
    </div>
  )
}
