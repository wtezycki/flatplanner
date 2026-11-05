import { useEffect, useRef, type KeyboardEvent, type ReactNode } from 'react'

interface Props {
  label: string
  onClose: () => void
  onKeyDown?: (event: KeyboardEvent) => void
  children: ReactNode
}

export default function FullscreenDialog({ label, onClose, onKeyDown, children }: Props) {
  const dialog = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    dialog.current?.showModal()
  }, [])

  return (
    <dialog
      ref={dialog}
      className="lightbox"
      aria-label={label}
      onClose={onClose}
      onKeyDown={onKeyDown}
      onClick={(event) => event.target === event.currentTarget && onClose()}
    >
      {children}
      <button className="lightbox__button lightbox__close" type="button" aria-label="Zamknij" onClick={onClose}>
        ×
      </button>
    </dialog>
  )
}
