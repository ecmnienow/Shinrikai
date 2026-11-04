import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, errorMessage, post } from '../api/client'
import type { Jlpt, Response } from '../api/types'
import { LevelRadios, PageTitle } from './common'
import { useDialogs } from './Dialogs'

interface Content {
  title: string
  level: Jlpt
  body: string
}

/**
 * Estado comum dos cadastros de gramática e de texto (Views/Grammar|Text/Register.cshtml):
 * ?id= carrega o conteúdo para alteração; título, nível aconselhado e corpo (editor rico).
 */
export function useContentForm<T extends Content>({ basePath, listRoute, noun, exerciseParam, exerciseTarget, onLoad }: {
  /** "/grammar" ou "/texts" */
  basePath: string
  /** Tela de leitura para onde volta depois de alterar. */
  listRoute: string
  /** "conteúdo" / "texto", nas mensagens de validação. */
  noun: string
  exerciseParam: 'grammarId' | 'textId'
  /** "ao conteúdo gramatical" / "ao texto". */
  exerciseTarget: string
  onLoad?: (content: T) => void
}) {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const id = Number(params.get('id') ?? 0)

  const [form, setForm] = useState<Content>({ title: '', level: 'None', body: '' })
  /** Corpo inicial do editor; null enquanto carrega. */
  const [initialBody, setInitialBody] = useState<string | null>(id === 0 ? '' : null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (id === 0) return
    api<T>(`${basePath}/${id}/edit`)
      .then((c) => {
        setForm({ title: c.title, level: c.level, body: c.body })
        setInitialBody(c.body)
        onLoad?.(c)
      })
      .catch((e) => dialogs.error(e.message))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, basePath, dialogs])

  function validate() {
    if (!form.title.trim()) dialogs.alert(`É obrigatório que informe um título para o ${noun} a ser cadastrado.`)
    else if (!form.body.trim()) dialogs.alert(`O ${noun} a ser cadastrado não possui um corpo.`)
    else return true
    return false
  }

  async function save(extra: object = {}) {
    setBusy(true)
    try {
      const response = await post<Response>(basePath, { id, ...form, ...extra })
      await dialogs.info(response.message ?? 'Cadastrado com sucesso!')
      if (id === 0) navigate(0)
      else navigate(listRoute)
    } catch (e) {
      dialogs.alert(errorMessage(e, `Não foi possível salvar o ${noun}!`))
    } finally {
      setBusy(false)
    }
  }

  function addExercise() {
    if (id === 0) dialogs.alert(`Antes de adicionar um exercício ${exerciseTarget}, este deve já estar cadastrado.`)
    else navigate(`/professor/exercicios?${exerciseParam}=${id}`)
  }

  return {
    id, form, initialBody, busy, validate, save, addExercise,
    set: (patch: Partial<Content>) => setForm((f) => ({ ...f, ...patch })),
  }
}

/** Cartão do cadastro: título, nível aconselhado, o editor (children) e os botões. */
export function ContentForm({ heading, content, name, onRegister, children }: {
  heading: string
  content: ReturnType<typeof useContentForm>
  name: string
  onRegister: () => void
  children: ReactNode
}) {
  const navigate = useNavigate()
  const { id, form, set, busy } = content
  if (content.initialBody === null) return <p className="notice">Carregando…</p>

  return (
    <>
      <PageTitle>{heading}</PageTitle>
      <div className="card stack">
        <label>Título<input value={form.title} onChange={(e) => set({ title: e.target.value })} /></label>
        <fieldset>
          <legend>Nível aconselhado</legend>
          <LevelRadios name={name} value={form.level} onChange={(level) => set({ level })} />
        </fieldset>
        {children}
        <div className="row">
          <button type="button" className="primary" onClick={onRegister} disabled={busy}>{id === 0 ? 'Cadastrar' : 'Alterar'}</button>
          <button type="button" onClick={() => navigate('/')}>Cancelar</button>
          <span className="spacer" />
          <button type="button" className="primary" onClick={content.addExercise}>Adicionar exercício</button>
        </div>
      </div>
    </>
  )
}
