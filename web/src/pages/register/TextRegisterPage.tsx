import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../../api/client'
import type { Jlpt, Response, TextContent } from '../../api/types'
import { LevelRadios, PageTitle } from '../../components/common'
import { useDialogs } from '../../components/Dialogs'
import { RichEditor } from '../../editor/RichEditor'
import { VocabularyPicker } from '../../editor/VocabularyPicker'

type Pick = { wordId: number; reading: string } | null

/**
 * Port de Views/Text/Register.cshtml. O CKEditor original não tinha ferramenta para furigana nem
 * para ligar palavras ao vocabulário (era notação manual 漢字「かんじ」 e HTML cru); aqui são os
 * botões 字 Furigana e 🔗 Vocabulário. Textos antigos com a notação são convertidos ao abrir.
 */
export function TextRegisterPage() {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const id = Number(params.get('id') ?? 0)

  const [loaded, setLoaded] = useState(id === 0)
  const [title, setTitle] = useState('')
  const [level, setLevel] = useState<Jlpt>('None')
  const [initialBody, setInitialBody] = useState('')
  const [body, setBody] = useState('')
  const [busy, setBusy] = useState(false)
  const [picker, setPicker] = useState<{ term: string; resolve: (p: Pick) => void } | null>(null)

  useEffect(() => {
    if (id === 0) return
    api<TextContent>(`/texts/${id}/edit`)
      .then((t) => {
        setTitle(t.title)
        setLevel(t.level)
        setInitialBody(t.body)
        setBody(t.body)
        setLoaded(true)
      })
      .catch((e) => dialogs.error(e.message))
  }, [id, dialogs])

  async function register() {
    if (!title.trim()) {
      dialogs.alert('É obrigatório que informe um título para o texto a ser cadastrado.')
      return
    }
    if (!body.trim()) {
      dialogs.alert('O texto a ser cadastrado não possui um corpo.')
      return
    }
    setBusy(true)
    try {
      const response = await api<Response>('/texts', { method: 'POST', json: { id, title, level, body } })
      await dialogs.info(response.message ?? 'Texto cadastrado com sucesso!')
      if (id === 0) navigate(0)
      else navigate('/textos')
    } catch (e) {
      dialogs.error(e instanceof Error ? e.message : id === 0 ? 'Não foi possível cadastrar o novo texto!' : 'Não foi possível alterar o texto!')
    } finally {
      setBusy(false)
    }
  }

  function addExercise() {
    if (id === 0) dialogs.alert('Antes de adicionar um exercício ao texto, este deve já estar cadastrado.')
    else navigate(`/professor/exercicios?textId=${id}`)
  }

  if (!loaded) return <main><p className="notice">Carregando…</p></main>

  return (
    <main>
      <PageTitle>Cadastro de Textos</PageTitle>
      <div className="card stack">
        <label>Título<input value={title} onChange={(e) => setTitle(e.target.value)} /></label>
        <fieldset>
          <legend>Nível aconselhado</legend>
          <LevelRadios name="textLevel" value={level} onChange={setLevel} />
        </fieldset>
        <RichEditor
          initialHtml={initialBody}
          onChange={setBody}
          japaneseTools
          askText={(l, i) => dialogs.prompt(l, i)}
          pickVocabularyWord={(selection) => new Promise<Pick>((resolve) => setPicker({ term: selection, resolve }))}
        />
        <div className="row">
          <button type="button" className="primary" onClick={register} disabled={busy}>{id === 0 ? 'Cadastrar' : 'Alterar'}</button>
          <button type="button" onClick={() => navigate('/')}>Cancelar</button>
          <span className="spacer" />
          <button type="button" className="primary" onClick={addExercise}>Adicionar exercício</button>
        </div>
      </div>

      {picker && (
        <VocabularyPicker
          initialTerm={picker.term}
          onDone={(p) => {
            picker.resolve(p)
            setPicker(null)
          }}
        />
      )}
    </main>
  )
}
