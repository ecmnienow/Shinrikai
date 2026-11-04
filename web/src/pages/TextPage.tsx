import { useEffect, useState, type MouseEvent } from 'react'
import { api } from '../api/client'
import type { ContentGroup, TextContent } from '../api/types'
import { ContentReader } from '../components/ContentReader'
import { HELP, HelpButton } from '../components/Help'

/**
 * Port de Views/Text/Index.cshtml — leitura com stretchtext (técnica 4.3): a furigana das palavras
 * ligadas ao vocabulário vem escondida conforme o conhecimento do aluno; clicar revela e registra
 * o desconhecimento (RegisterLackOfKnowledge).
 */
export function TextPage() {
  const [groups, setGroups] = useState<ContentGroup[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<ContentGroup[]>('/texts').then(setGroups).catch((e) => setError(e.message))
  }, [])

  function onContentClick(e: MouseEvent<HTMLDivElement>) {
    const span = (e.target as HTMLElement).closest<HTMLElement>('.stretchText')
    if (!span) return
    const rts = span.querySelectorAll<HTMLElement>('rt')
    const hidden = Array.from(rts).some((rt) => getComputedStyle(rt).display === 'none')
    if (hidden && span.dataset.id) {
      api(`/texts/words/${span.dataset.id}/lack-of-knowledge`, { method: 'POST' }).catch(() => {})
    }
    rts.forEach((rt) => (rt.style.display = ''))
  }

  return (
    <>
      {error && <p className="notice error">{error}</p>}
      <ContentReader
        title="Leitura de Textos"
        help={<HelpButton id={HELP.text} />}
        groups={groups}
        emptyText="Selecione um dos textos listados para começar."
        loadItem={async (id) => {
          const t = await api<TextContent>(`/texts/${id}`)
          return { title: t.title, body: t.body }
        }}
        onLocked={() => {}}
        onReadLongEnough={(id) => api(`/texts/${id}/knowledge`, { method: 'POST' }).catch(() => {})}
        showExercises={() => true}
        exercisesLink={(id) => `/exercicios?textId=${id}`}
        editLink={(id) => `/professor/textos?id=${id}`}
        onContentClick={onContentClick}
      />
    </>
  )
}
