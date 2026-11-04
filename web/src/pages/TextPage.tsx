import type { MouseEvent } from 'react'
import { fire } from '../api/client'
import { ContentReader } from '../components/ContentReader'
import { HELP, HelpButton } from '../components/Help'

/**
 * Port de Views/Text/Index.cshtml — leitura com stretchtext (técnica 4.3): a furigana das palavras
 * ligadas ao vocabulário vem escondida conforme o conhecimento do aluno; clicar revela e registra
 * o desconhecimento (RegisterLackOfKnowledge).
 */
export function TextPage() {
  function onContentClick(e: MouseEvent<HTMLDivElement>) {
    const span = (e.target as HTMLElement).closest<HTMLElement>('.stretchText')
    if (!span) return
    const rts = span.querySelectorAll<HTMLElement>('rt')
    const hidden = Array.from(rts).some((rt) => getComputedStyle(rt).display === 'none')
    if (hidden && span.dataset.id) fire(`/texts/words/${span.dataset.id}/lack-of-knowledge`)
    rts.forEach((rt) => (rt.style.display = ''))
  }

  return (
    <ContentReader
      basePath="/texts"
      title="Leitura de Textos"
      help={<HelpButton id={HELP.text} />}
      emptyText="Selecione um dos textos listados para começar."
      showExercises={() => true}
      exercisesLink={(id) => `/exercicios?textId=${id}`}
      editLink={(id) => `/professor/textos?id=${id}`}
      onContentClick={onContentClick}
    />
  )
}
