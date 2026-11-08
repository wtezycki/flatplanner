import { useEffect, useRef } from 'react'
import Icon from './Icon.tsx'

const TITLE = 'Jak to działa?'

const POINTS = [
  'Napisz własnymi słowami, czego potrzebujesz, np. pies, praca na nocki, szkoła dla dziecka. Nie musisz znać filtrów.',
  'AI czyta opis każdego ogłoszenia i sprawdza okolicę na mapie. Przy każdym warunku pokazuje dowód: cytat z opisu, miejsce w pobliżu albo dane z ogłoszenia.',
  'Wyniki dzielimy na: Pasuje, Pasuje częściowo i Prawie pasuje.',
  'Dopytuj w tej samej rozmowie, np. „a do 600 tys.?”, albo usuń warunek krzyżykiem.',
  'Mamy oferty sprzedaży mieszkań z Gdańska, Gdyni i Sopotu.',
]

export default function ChatHelp({ compact = false }: { compact?: boolean }) {
  const help = useRef<HTMLDetailsElement>(null)

  useEffect(() => {
    if (!compact) {
      return
    }
    const closeOutside = (event: PointerEvent) => {
      const element = help.current
      if (element?.open && event.target instanceof Node && !element.contains(event.target)) {
        element.open = false
      }
    }
    document.addEventListener('pointerdown', closeOutside)
    return () => document.removeEventListener('pointerdown', closeOutside)
  }, [compact])

  return (
    <details ref={help} className={compact ? 'chat-help chat-help--compact' : 'chat-help'}>
      <summary className="chat-help__toggle" title={compact ? TITLE : undefined}>
        <Icon name="info" />
        <span className={compact ? 'sr-only' : undefined}>{TITLE}</span>
      </summary>
      <div className="chat-help__panel">
        {compact && <p className="chat-help__title">{TITLE}</p>}
        <ul className="chat-help__list">
          {POINTS.map((point) => (
            <li key={point}>{point}</li>
          ))}
        </ul>
      </div>
    </details>
  )
}
