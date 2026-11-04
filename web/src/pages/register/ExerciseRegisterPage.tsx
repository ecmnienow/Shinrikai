import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, errorMessage, post } from '../../api/client'
import type { Exercise, Jlpt, Response } from '../../api/types'
import { LevelRadios, PageTitle } from '../../components/common'
import { useDialogs } from '../../components/Dialogs'

const LETTERS = ['a', 'b', 'c', 'd']

/**
 * Port de Views/Exercises/Register.cshtml: nível, questão e 4 opções (a–d) com a correta marcada.
 * Aberto a partir da gramática/texto já vem ligado a ele e com o nível dele.
 */
export function ExerciseRegisterPage() {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [exercise, setExercise] = useState<Exercise | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const q = new URLSearchParams()
    for (const key of ['id', 'grammarId', 'textId']) q.set(key, params.get(key) ?? '0')
    api<Exercise>(`/exercises/edit?${q}`).then(setExercise).catch((e) => dialogs.error(e.message))
  }, [params, dialogs])

  if (!exercise) return <main><p className="notice">Carregando…</p></main>

  const options = [exercise.option1, exercise.option2, exercise.option3, exercise.option4]
  const set = (patch: Partial<Exercise>) => setExercise({ ...exercise, ...patch })
  const setOption = (i: number, value: string) => set({ [`option${i + 1}`]: value } as Partial<Exercise>)

  async function register() {
    if (!exercise) return
    if (!exercise.question.trim()) {
      dialogs.alert('É obrigatório que informe uma questão para o exercício a ser cadastrado.')
      return
    }
    if (options.some((o) => !o.trim())) {
      dialogs.alert('Todas as opções de respostas devem ser preenchidas.')
      return
    }
    if (exercise.rightAnswer < 1 || exercise.rightAnswer > 4) {
      dialogs.alert('Nenhuma resposta foi marcada como correta.')
      return
    }
    setBusy(true)
    try {
      const response = await post<Response>('/exercises', exercise)
      await dialogs.info(response.message ?? 'Exercício cadastrado com sucesso!')
      navigate(-1)
    } catch (e) {
      dialogs.alert(errorMessage(e, 'Não foi possível cadastrar o novo exercício!'))
    } finally {
      setBusy(false)
    }
  }

  const context = exercise.grammarId ? `Gramática #${exercise.grammarId}` : exercise.textId ? `Texto #${exercise.textId}` : 'Exercício geral'

  return (
    <main className="narrow">
      <PageTitle>Cadastro de Exercícios</PageTitle>
      <div className="card stack">
        <p className="muted">{context}</p>
        <fieldset>
          <legend>Nível aconselhado</legend>
          <LevelRadios name="exerciseLevel" value={exercise.level} onChange={(level: Jlpt) => set({ level })} />
        </fieldset>
        <label>Questão<textarea lang="ja" rows={3} value={exercise.question} onChange={(e) => set({ question: e.target.value })} /></label>
        <fieldset className="stack">
          <legend>Possíveis respostas <span className="muted">(marque a correta)</span></legend>
          {options.map((text, i) => (
            <div key={i} className="row">
              <label className="inline">
                {LETTERS[i]})
                <input type="radio" name="right" checked={exercise.rightAnswer === i + 1} onChange={() => set({ rightAnswer: i + 1 })} aria-label={`Opção ${LETTERS[i]} é a correta`} />
              </label>
              <input className="grow" lang="ja" value={text} onChange={(e) => setOption(i, e.target.value)} aria-label={`Opção ${LETTERS[i]}`} />
            </div>
          ))}
        </fieldset>
        <div className="row">
          <button type="button" className="primary" onClick={register} disabled={busy}>{exercise.id ? 'Alterar' : 'Cadastrar'}</button>
          <button type="button" onClick={() => navigate(-1)}>Cancelar</button>
        </div>
      </div>
    </main>
  )
}
