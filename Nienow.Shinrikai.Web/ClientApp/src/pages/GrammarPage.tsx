import { api, errorMessage } from '../api/client'
import type { GrammarRequisites } from '../api/types'
import { ContentReader } from '../components/ContentReader'
import { useDialogs } from '../components/Dialogs'
import { HELP, HelpButton } from '../components/Help'

/**
 * Port de Views/Grammar/Index.cshtml — mapa adaptativo (técnica 4.2): conteúdos bloqueados mostram
 * os pré-requisitos; 20 s de leitura declaram o conhecimento, que libera os conteúdos seguintes.
 */
export function GrammarPage() {
  const dialogs = useDialogs()

  async function showRequisites(id: number) {
    try {
      const r = await api<GrammarRequisites>(`/grammar/${id}/requisites`)
      dialogs.alert(
        <>
          <p>{r.message}</p>
          {r.requisites.length > 0 && <ul>{r.requisites.map((t) => <li key={t}>{t}</li>)}</ul>}
        </>,
      )
    } catch (e) {
      dialogs.error(errorMessage(e, 'Falha ao carregar os pré-requisitos.'))
    }
  }

  return (
    <ContentReader
      basePath="/grammar"
      title="Estudo de Gramática"
      help={<HelpButton id={HELP.grammar} />}
      emptyText="Selecione um dos conteúdos listados para começar."
      onLocked={(item) => showRequisites(item.id)}
      showExercises={(item) => item.hasExercises}
      exercisesLink={(id) => `/exercicios?grammarId=${id}`}
      editLink={(id) => `/professor/gramatica?id=${id}`}
    />
  )
}
