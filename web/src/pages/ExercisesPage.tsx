import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, errorMessage, post } from '../api/client'
import type { ExerciseQuestion, Response } from '../api/types'
import { EditLink, PageTitle, shuffle } from '../components/common'
import { useDialogs } from '../components/Dialogs'
import { HELP, HelpButton } from '../components/Help'

type Status = 'pending' | 'success' | 'fail'

/**
 * Port de Views/Exercises/Index.cshtml — ordenação adaptativa (técnica 4.4): a lista vem filtrada
 * pelo nível e embaralhada; os exercícios são feitos em sequência (o seguinte só libera depois de
 * responder o atual) e, ao final, mostra o total de acertos.
 */
export function ExercisesPage() {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const grammarId = Number(params.get('grammarId') ?? 0)
  const textId = Number(params.get('textId') ?? 0)

  const [ids, setIds] = useState<number[] | null>(null)
  const [status, setStatus] = useState<Record<number, Status>>({})
  const [unlocked, setUnlocked] = useState(1)
  const [current, setCurrent] = useState<number | null>(null)
  const [question, setQuestion] = useState<ExerciseQuestion | null>(null)
  const [answer, setAnswer] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const q = new URLSearchParams()
    if (grammarId) q.set('grammarId', String(grammarId))
    if (textId) q.set('textId', String(textId))
    api<number[]>(`/exercises?${q}`)
      .then((list) => {
        setIds(list)
        setStatus({})
        setUnlocked(1)
        if (list.length > 0) load(list[0])
      })
      .catch((e) => setError(e.message))
  }, [grammarId, textId])

  async function load(id: number) {
    setCurrent(id)
    setAnswer(null)
    try {
      const q = await api<ExerciseQuestion>(`/exercises/${id}`)
      setQuestion({ ...q, options: shuffle(q.options) })
    } catch (e) {
      setError(errorMessage(e, 'Falha ao carregar o exercício.'))
    }
  }

  function select(id: number, index: number) {
    if (status[id] && status[id] !== 'pending') {
      dialogs.alert('Você já fez este exercício.')
      return
    }
    if (index >= unlocked) {
      dialogs.alert('Primeiro deve-se fazer os exercícios antecessores.')
      return
    }
    load(id)
  }

  async function check() {
    if (current === null || !ids) return
    if (answer === null) {
      dialogs.alert('Nenhuma resposta selecionada.')
      return
    }
    try {
      const result = await post<Response>(`/exercises/${current}/check`, { answer })
      if (result.ok) await dialogs.success('Resposta correta!')
      else await dialogs.info(<>Resposta errada!<br />{result.message}</>)

      const nextStatus = { ...status, [current]: result.ok ? 'success' : 'fail' } as Record<number, Status>
      setStatus(nextStatus)

      const index = ids.indexOf(current)
      if (index + 1 < ids.length) {
        setUnlocked(Math.max(unlocked, index + 2))
        load(ids[index + 1])
      } else {
        setCurrent(null)
        setQuestion(null)
        const right = Object.values(nextStatus).filter((s) => s === 'success').length
        await dialogs.info(`Todos os exercícios respondidos!\nVocê acertou ${right} de ${ids.length}.`)
        if (grammarId) navigate('/gramatica')
        else if (textId) navigate('/textos')
        else navigate(0)
      }
    } catch (e) {
      dialogs.error(errorMessage(e, 'Falha ao corrigir.'))
    }
  }

  function move(delta: number) {
    if (current === null || !ids) return
    const index = ids.indexOf(current) + delta
    if (index >= 0 && index < ids.length && index < unlocked && (status[ids[index]] ?? 'pending') === 'pending') load(ids[index])
  }

  return (
    <main className="reader">
      <aside className="reader-menu" aria-label="Exercícios">
        <ul>
          {ids?.map((id, i) => {
            const s = status[id] ?? 'pending'
            return (
              <li key={id}>
                <button
                  type="button"
                  className={`content-item ${current === id ? 'active' : ''} ${s} ${i >= unlocked ? 'locked' : ''}`}
                  onClick={() => select(id, i)}
                >
                  Exercício {i + 1} {s === 'success' && '✔'} {s === 'fail' && '✘'}
                </button>
                <EditLink to={`/professor/exercicios?id=${id}`} label={`Editar exercício ${i + 1}`} />
              </li>
            )
          })}
        </ul>
      </aside>

      <section className="reader-content">
        <PageTitle help={<HelpButton id={HELP.exercises} />}>Resolução de Exercícios</PageTitle>
        {error && <p className="error">{error}</p>}
        {ids && ids.length === 0 && <p className="notice">Nenhum exercício disponível para o seu nível.</p>}

        {question && (
          <form className="card stack" onSubmit={(e) => { e.preventDefault(); check() }}>
            <div className="question" lang="ja">{question.question}</div>
            <div className="stack" role="radiogroup">
              {question.options.map((o) => (
                <label key={o.value} className="inline option">
                  <input type="radio" name="answer" checked={answer === o.value} onChange={() => setAnswer(o.value)} />
                  <span lang="ja">{o.text}</span>
                </label>
              ))}
            </div>
            <div className="row">
              <button type="button" onClick={() => move(-1)}>Anterior</button>
              <button type="button" onClick={() => move(1)}>Próximo</button>
              <span className="spacer" />
              <button className="primary">Corrigir</button>
            </div>
          </form>
        )}
      </section>
    </main>
  )
}
