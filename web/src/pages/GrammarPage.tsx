import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { ContentGroup, Grammar, GrammarRequisites } from '../api/types'
import { ContentReader } from '../components/ContentReader'
import { useDialogs } from '../components/Dialogs'
import { HELP, HelpButton } from '../components/Help'

/**
 * Port de Views/Grammar/Index.cshtml — mapa adaptativo (técnica 4.2): conteúdos bloqueados mostram
 * os pré-requisitos; 20 s de leitura declaram o conhecimento, que libera os conteúdos seguintes.
 */
export function GrammarPage() {
  const dialogs = useDialogs()
  const [groups, setGroups] = useState<ContentGroup[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<ContentGroup[]>('/grammar').then(setGroups).catch((e) => setError(e.message))
  }, [])

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
      dialogs.error(e instanceof Error ? e.message : 'Falha ao carregar os pré-requisitos.')
    }
  }

  return (
    <>
      {error && <p className="notice error">{error}</p>}
      <ContentReader
        title="Estudo de Gramática"
        help={<HelpButton id={HELP.grammar} />}
        groups={groups}
        emptyText="Selecione um dos conteúdos listados para começar."
        loadItem={async (id) => {
          const g = await api<Grammar>(`/grammar/${id}`)
          return { title: g.title, body: g.body }
        }}
        onLocked={(item) => showRequisites(item.id)}
        onReadLongEnough={(id) => api(`/grammar/${id}/knowledge`, { method: 'POST' }).catch(() => {})}
        showExercises={(item) => item.hasExercises}
        exercisesLink={(id) => `/exercicios?grammarId=${id}`}
        editLink={(id) => `/professor/gramatica?id=${id}`}
      />
    </>
  )
}
