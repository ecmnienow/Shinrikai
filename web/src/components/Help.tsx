import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import { Modal } from './Dialogs'

/** IDs das notificações de ajuda de cada tela (tabela NOTIFICATIONS). */
export const HELP = {
  transcription: 1,
  grammar: 3,
  text: 4,
  exercises: 5,
  vocabulary: 6,
  kanji: 7,
} as const

/**
 * Port de NotificationDialog (bootstrap.dialogs/dialogs.js): ao abrir a tela mostra a ajuda se o
 * aluno ainda não clicou em "Entendi"; o botão "?" força a exibição (sem o "Entendi").
 * A mensagem vem do banco em HTML, como no original.
 */
export function HelpButton({ id }: { id: number }) {
  const [message, setMessage] = useState<string | null>(null)
  const [forced, setForced] = useState(false)

  const load = useCallback(async (force: boolean) => {
    try {
      const n = await api<{ message: string }>(`/notifications/${id}?force=${force}`)
      if (n.message) {
        setForced(force)
        setMessage(n.message)
      }
    } catch {
      /* ajuda é opcional */
    }
  }, [id])

  useEffect(() => {
    load(false)
  }, [load])

  async function understood() {
    setMessage(null)
    await api(`/notifications/${id}/understood`, { method: 'POST' }).catch(() => {})
  }

  return (
    <>
      <button type="button" className="icon help" title="Ajuda" aria-label="Ajuda" onClick={() => load(true)}>?</button>
      {message !== null && (
        <Modal
          title="Ajuda"
          onClose={() => setMessage(null)}
          footer={
            <>
              {!forced && <button type="button" className="primary" onClick={understood}>✔ Entendi</button>}
              <button type="button" onClick={() => setMessage(null)}>Sair</button>
            </>
          }
        >
          <div className="help-message" dangerouslySetInnerHTML={{ __html: message }} />
        </Modal>
      )}
    </>
  )
}
