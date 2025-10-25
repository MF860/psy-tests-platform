import { Sun } from "lucide-react"
import { Button } from "./button"

// Light-only theme toggle (disabled)
export function ThemeToggle() {
  return (
    <Button variant="outline" size="icon" className="relative" disabled>
      <Sun className="h-[1.2rem] w-[1.2rem]" />
      <span className="sr-only">المظهر الفاتح (ثابت)</span>
    </Button>
  )
}
