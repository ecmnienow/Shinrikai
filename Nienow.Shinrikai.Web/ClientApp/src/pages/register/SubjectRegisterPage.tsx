import { useCallback, useEffect, useState } from 'react'
import { api, errorMessage, post } from '../../api/client'
import type { Response, Subject } from '../../api/types'
import { PageTitle } from '../../components/common'
import { useDialogs } from '../../components/Dialogs'

/** Cadastro de assuntos do vocabulário: lista, inclusão, alteração e exclusão. */
export function SubjectRegisterPage() {
  const dialogs = useDialogs()
  const [subjects, setSubjects] = useState<Subject[] | null>(null)
  const [editingId, setEditingId] = useState(0)
  const [name, setName] = useState('')
  const [invalid, setInvalid] = useState(false)
  const [busy, setBusy] = useState(false)

  /** Busca a lista no servidor (na abertura da tela e depois de cada alteração). */
  const load = useCallback(async () => {
    try {
      setSubjects(await api<Subject[]>('/subjects'))
    } catch (e) {
      dialogs.error(errorMessage(e, 'Não foi possível carregar os assuntos.'))
      setSubjects([])
    }
  }, [dialogs])

  useEffect(() => {
    load()
  }, [load])

  function reset() {
    setEditingId(0)
    setName('')
    setInvalid(false)
  }

  function edit(subject: Subject) {
    setEditingId(subject.id)
    setName(subject.name)
    setInvalid(false)
  }

  async function save() {
    if (!name.trim()) {
      setInvalid(true)
      return
    }
    setBusy(true)
    try {
      const response = editingId === 0
        ? await post<Response>('/subjects', { name })
        : await api<Response>(`/subjects/${editingId}`, { method: 'PUT', json: { name } })
      await dialogs.success(response.message ?? 'Assunto salvo com sucesso!')
      reset()
      await load()
    } catch (e) {
      dialogs.error(errorMessage(e, 'Não foi possível salvar o assunto.'))
    } finally {
      setBusy(false)
    }
  }

  async function remove(subject: Subject) {
    if (!(await dialogs.confirm(`Excluir o assunto "${subject.name}"?`, 'Excluir assunto'))) return
    try {
      const response = await api<Response>(`/subjects/${subject.id}`, { method: 'DELETE' })
      await dialogs.success(response.message ?? 'Assunto excluído com sucesso!')
      if (subject.id === editingId) reset()
      await load()
    } catch (e) {
      dialogs.error(errorMessage(e, 'Não foi possível excluir o assunto.'))
    }
  }

  return (
    <main className="narrow">
      <PageTitle>Cadastro de Assuntos</PageTitle>

      <form className="card stack" onSubmit={(e) => { e.preventDefault(); save() }}>
        <label>
          Nome do assunto
          <input className={invalid ? 'is-wrong' : ''} value={name} maxLength={150} onChange={(e) => setName(e.target.value)} />
        </label>
        <div className="row">
          <button type="submit" className="primary" disabled={busy}>{editingId === 0 ? 'Cadastrar' : 'Alterar'}</button>
          {editingId !== 0 && <button type="button" onClick={reset}>Cancelar edição</button>}
        </div>
      </form>

      {subjects === null ? (
        <p className="notice">Carregando…</p>
      ) : subjects.length === 0 ? (
        <p className="notice">Nenhum assunto cadastrado.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr><th>Nome</th><th aria-label="Ações" /></tr>
          </thead>
          <tbody>
            {subjects.map((s) => (
              <tr key={s.id} className={s.id === editingId ? 'selected' : ''}>
                <td>{s.name}</td>
                <td className="actions">
                  <button type="button" className="icon" title="Editar" aria-label={`Editar ${s.name}`} onClick={() => edit(s)}>✎</button>
                  <button type="button" className="icon" title="Excluir" aria-label={`Excluir ${s.name}`} onClick={() => remove(s)}>🗑</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}
