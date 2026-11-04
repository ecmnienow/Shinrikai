import { useState } from 'react'
import { api, errorMessage } from '../../api/client'
import type { Grammar, GrammarSummary } from '../../api/types'
import { ContentForm, useContentForm } from '../../components/ContentForm'
import { Modal, useDialogs } from '../../components/Dialogs'
import { RichEditor } from '../../editor/RichEditor'

/**
 * Port de Views/Grammar/Register.cshtml: título, nível aconselhado, corpo (editor rico) e, ao
 * cadastrar, a janela "Definir pré-requisitos" com os conteúdos do mesmo nível.
 */
export function GrammarRegisterPage() {
  const dialogs = useDialogs()
  const [requisites, setRequisites] = useState<number[]>([])
  const [candidates, setCandidates] = useState<GrammarSummary[] | null>(null)
  const content = useContentForm<Grammar>({
    basePath: '/grammar', listRoute: '/gramatica', noun: 'conteúdo',
    exerciseParam: 'grammarId', exerciseTarget: 'ao conteúdo gramatical',
    onLoad: (g) => setRequisites(g.requisites),
  })

  /** StartRegister: abre a escolha de pré-requisitos (conteúdos do nível escolhido). */
  async function startRegister() {
    if (!content.validate()) return
    try {
      const list = await api<GrammarSummary[]>(`/grammar/all?jlpt=${content.form.level}`)
      setCandidates(list.filter((g) => g.id !== content.id))
    } catch (e) {
      dialogs.error(errorMessage(e, 'Falha ao listar conteúdos.'))
    }
  }

  function register() {
    setCandidates(null)
    content.save({ requisites })
  }

  return (
    <main>
      <ContentForm heading="Cadastro de Gramática" content={content} name="grammarLevel" onRegister={startRegister}>
        <RichEditor initialHtml={content.initialBody ?? ''} onChange={(body) => content.set({ body })} askText={(l, i) => dialogs.prompt(l, i)} />
      </ContentForm>

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
