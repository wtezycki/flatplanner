import { useRef, useState } from 'react'
import type { FormEvent, KeyboardEvent } from 'react'
import Icon from './Icon.tsx'

const COUNTER_FROM_SHARE = 0.9

interface Props {
  label: string
  placeholder: string
  maxLength?: number
  autoFocus?: boolean
  onSubmit: (text: string) => void
}

export default function Composer({ label, placeholder, maxLength, autoFocus, onSubmit }: Props) {
  const input = useRef<HTMLTextAreaElement>(null)
  const [value, setValue] = useState('')
  const text = value.trim()
  const showCounter = maxLength !== undefined && value.length >= maxLength * COUNTER_FROM_SHARE

  const resize = () => {
    const element = input.current
    if (element) {
      element.style.height = 'auto'
      element.style.height = `${element.scrollHeight}px`
    }
  }

  const submit = (event?: FormEvent) => {
    event?.preventDefault()
    if (!text) {
      return
    }
    onSubmit(text)
    setValue('')
    requestAnimationFrame(resize)
  }

  const keyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      submit(event)
    }
  }

  return (
    <form className="composer" role="search" onSubmit={submit}>
      <div className="composer__box">
        <textarea
          ref={input}
          className="composer__input"
          rows={1}
          aria-label={label}
          placeholder={placeholder}
          maxLength={maxLength}
          autoFocus={autoFocus}
          value={value}
          onChange={(event) => {
            setValue(event.target.value)
            resize()
          }}
          onKeyDown={keyDown}
        />
        <button className="composer__send" type="submit" aria-label="Wyślij" disabled={!text}>
          <Icon name="arrowUp" />
        </button>
      </div>
      {showCounter && (
        <p className="composer__counter num" aria-live="polite">
          {value.length}/{maxLength}
        </p>
      )}
    </form>
  )
}
