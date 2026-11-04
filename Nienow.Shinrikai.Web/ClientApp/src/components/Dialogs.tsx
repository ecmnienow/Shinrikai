import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'

/**
 * Substitui Scripts/bootstrap.dialogs (AlertDialog, ErrorDialog, InformationDialog, SuccessDialog,
 * QuestionDialog) e Scripts/bootstrap.image (ShowImage). Cada função devolve uma Promise que resolve
 * quando o usuário fecha — o equivalente ao "callback" do original.
 */

type Kind = 'alert' | 'error' | 'info' | 'success' | 'question'

interface DialogState {
  kind: Kind
  title: string
  message: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  resolve: (ok: boolean) => void
}

interface DialogApi {
  alert: (message: ReactNode, title?: string) => Promise<void>
  error: (message: ReactNode, title?: string) => Promise<void>
  info: (message: ReactNode, title?: string) => Promise<void>
  success: (message: ReactNode, title?: string) => Promise<void>
  confirm: (message: ReactNode, title?: string) => Promise<boolean>
  /** Pede um texto (substitui window.prompt). Resolve null se cancelado. */
  prompt: (label: string, initial?: string, title?: string) => Promise<string | null>
  showImage: (url: string, title?: string) => void
}

interface PromptState {
  label: string
  title: string
  value: string
  resolve: (value: string | null) => void
}

const DialogContext = createContext<DialogApi | null>(null)

const ICON: Record<Kind, string> = { alert: '⚠️', error: '⛔', info: 'ℹ️', success: '✅', question: '❓' }

export function Modal({ title, onClose, children, footer, wide }: {
  title: ReactNode
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
  wide?: boolean
}) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    ref.current?.querySelector<HTMLElement>('button.primary, button')?.focus()
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="modal-backdrop" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div className={`modal ${wide ? 'wide' : ''}`} role="dialog" aria-modal="true" ref={ref}>
        <header className="modal-header">
          <h3>{title}</h3>
          <button type="button" className="icon" aria-label="Fechar" onClick={onClose}>×</button>
        </header>
        <div className="modal-body">{children}</div>
        {footer && <footer className="modal-footer">{footer}</footer>}
      </div>
    </div>
  )
}

export function DialogProvider({ children }: { children: ReactNode }) {
  const [dialog, setDialog] = useState<DialogState | null>(null)
  const [image, setImage] = useState<{ url: string; title: string } | null>(null)
  const [prompt, setPrompt] = useState<PromptState | null>(null)

  const open = useCallback(
    (kind: Kind, message: ReactNode, title?: string) =>
      new Promise<boolean>((resolve) => setDialog({ kind, message, title: title || 'Shinrikai', resolve })),
    [],
  )

  const api = useMemo<DialogApi>(
    () => ({
      alert: (m, t) => open('alert', m, t).then(() => undefined),
      error: (m, t) => open('error', m, t || 'Erro').then(() => undefined),
      info: (m, t) => open('info', m, t).then(() => undefined),
      success: (m, t) => open('success', m, t).then(() => undefined),
      confirm: (m, t) => open('question', m, t),
      prompt: (label, initial, title) =>
        new Promise<string | null>((resolve) => setPrompt({ label, value: initial ?? '', title: title || 'Shinrikai', resolve })),
      showImage: (url, title) => setImage({ url, title: title || 'Imagem' }),
    }),
    [open],
  )

  function close(ok: boolean) {
    dialog?.resolve(ok)
    setDialog(null)
  }

  return (
    <DialogContext.Provider value={api}>
      {children}
      {dialog && (
        <Modal
          title={<><span aria-hidden="true">{ICON[dialog.kind]}</span> {dialog.title}</>}
          onClose={() => close(false)}
          footer={
            dialog.kind === 'question' ? (
              <>
                <button type="button" className="primary" onClick={() => close(true)}>Sim</button>
                <button type="button" onClick={() => close(false)}>Não</button>
              </>
            ) : (
              <button type="button" className="primary" onClick={() => close(true)}>OK</button>
            )
          }
        >
          <div className={`dialog-message ${dialog.kind}`}>{dialog.message}</div>
        </Modal>
      )}
      {prompt && (
        <Modal
          title={prompt.title}
          onClose={() => { prompt.resolve(null); setPrompt(null) }}
          footer={
            <>
              <button type="submit" form="prompt-form" className="primary">OK</button>
              <button type="button" onClick={() => { prompt.resolve(null); setPrompt(null) }}>Cancelar</button>
            </>
          }
        >
          <form
            id="prompt-form"
            onSubmit={(e) => { e.preventDefault(); prompt.resolve(prompt.value); setPrompt(null) }}
          >
            <label>
              {prompt.label}
              <input autoFocus lang="ja" value={prompt.value} onChange={(e) => setPrompt({ ...prompt, value: e.target.value })} />
            </label>
          </form>
        </Modal>
      )}
      {image && (
        <Modal title={image.title} onClose={() => setImage(null)} wide footer={<button type="button" onClick={() => setImage(null)}>Fechar</button>}>
          <img src={image.url} alt={image.title} className="viewer-image" />
        </Modal>
      )}
    </DialogContext.Provider>
  )
}

export function useDialogs() {
  const ctx = useContext(DialogContext)
  if (!ctx) throw new Error('useDialogs fora de <DialogProvider>')
  return ctx
}
