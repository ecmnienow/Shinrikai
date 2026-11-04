import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../../api/client'
import type { Grammar, GrammarSummary, Jlpt, Response } from '../../api/types'
import { LevelRadios, PageTitle } from '../../components/common'
import { Modal, useDialogs } from '../../components/Dialogs'
import { RichEditor } from '../../editor/RichEditor'

/**
 * Port de Views/Grammar/Register.cshtml: título, nível aconselhado, corpo (editor rico) e, ao
 * cadastrar, a janela "Definir pré-requisitos" com os conteúdos do mesmo nível.
 */
export function GrammarRegisterPage() {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const id = Number(params.get('id') ?? 0)

  const [loaded, setLoaded] = useState(id === 0)
  const [title, setTitle] = useState('')
  const [level, setLevel] = useState<Jlpt>('None')
  const [initialBody, setInitialBody] = useState('')
  const [body, setBody] = useState('')
  const [requisites, setRequisites] = useState<number[]>([])
  const [candidates, setCandidates] = useState<GrammarSummary[] | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (id === 0) return
    api<Grammar>(`/grammar/${id}/edit`)
      .then((g) => {
        setTitle(g.title)
        setLevel(g.level)
        setInitialBody(g.body)
        setBody(g.body)
        setRequisites(g.requisites)
        setLoaded(true)
      })
      .catch((e) => dialogs.error(e.message))
  }, [id, dialogs])

  function validate() {
    if (!title.trim()) {
      dialogs.alert('É obrigatório que informe um título para o conteúdo a ser cadastrado.')
      return false
    }
    if (!body.trim()) {
      dialogs.alert('O conteúdo a ser cadastrado não possui um corpo.')
      return false
    }
    return true
  }

  /** StartRegister: abre a escolha de pré-requisitos (conteúdos do nível escolhido). */
  async function startRegister() {
    if (!validate()) return
    try {
      const list = await api<GrammarSummary[]>(`/grammar/all?jlpt=${level}`)
      setCandidates(list.filter((g) => g.id !== id))
    } catch (e) {
      dialogs.error(e instanceof Error ? e.message : 'Falha ao listar conteúdos.')
    }
  }

  async function register() {
    setCandidates(null)
    setBusy(true)
    try {
      const response = await api<Response>('/grammar', { method: 'POST', json: { id, title, level, body, requisites } })
      await dialogs.info(response.message ?? 'Conteúdo cadastrado com sucesso!')
      if (id === 0) navigate(0)
      else navigate('/gramatica')
    } catch (e) {
      dialogs.alert(e instanceof Error ? e.message : 'Não foi possível cadastrar o novo conteúdo!')
    } finally {
      setBusy(false)
    }
  }

  function addExercise() {
    if (id === 0) dialogs.alert('Antes de adicionar um exercício ao conteúdo gramatical, este deve já estar cadastrado.')
    else navigate(`/professor/exercicios?grammarId=${id}`)
  }

  if (!loaded) return <main><p className="notice">Carregando…</p></main>

  return (
    <main>
      <PageTitle>Cadastro de Gramática</PageTitle>
      <div className="card stack">
        <label>Título<input value={title} onChange={(e) => setTitle(e.target.value)} /></label>
        <fieldset>
          <legend>Nível aconselhado</legend>
          <LevelRadios name="grammarLevel" value={level} onChange={setLevel} />
        </fieldset>
        <RichEditor initialHtml={initialBody} onChange={setBody} askText={(l, i) => dialogs.prompt(l, i)} />
        <div className="row">
          <button type="button" className="primary" onClick={startRegister} disabled={busy}>{id === 0 ? 'Cadastrar' : 'Alterar'}</button>
          <button type="button" onClick={() => navigate('/')}>Cancelar</button>
          <span className="spacer" />
          <button type="button" className="primary" onClick={addExercise}>Adicionar exercício</button>
        </div>
      </div>

      {candidates && (
        <Modal
          title="Definir pré-requisitos"
          onClose={() => setCandidates(null)}
          footer={
            <>
              <button type="button" className="primary" onClick={register}>OK</button>
              <button type="button" onClick={() => setCandidates(null)}>Cancelar</button>
            </>
          }
        >
          {candidates.length === 0 ? (
            <p className="muted">Nenhum outro conteúdo neste nível.</p>
          ) : (
            <ul className="picker-list">
              {candidates.map((g) => (
                <li key={g.id}>
                  <label className="inline">
                    <input
                      type="checkbox"
                      checked={requisites.includes(g.id)}
                      onChange={(e) => setRequisites((r) => (e.target.checked ? [...r, g.id] : r.filter((x) => x !== g.id)))}
                    />
                    {g.title}
                  </label>
                </li>
              ))}
            </ul>
          )}
        </Modal>
      )}
    </main>
  )
}
