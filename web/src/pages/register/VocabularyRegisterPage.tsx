import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, errorMessage, isNotFound, post } from '../../api/client'
import type { Response, Subject, Word } from '../../api/types'
import { FileField, PageTitle } from '../../components/common'
import { useDialogs } from '../../components/Dialogs'

interface Slot {
  kanji: string
  subject: string
  translation: string
  image: File | null
  audio: File | null
  hasImage: boolean
  hasAudio: boolean
}

const emptySlot = (): Slot => ({ kanji: '', subject: '', translation: '', image: null, audio: null, hasImage: false, hasAudio: false })

/**
 * Port de Views/Vocabulary/Register.cshtml: kana (com busca), romaji e até 3 "formas"
 * (kanji, assunto, tradução, imagem e áudio). O assunto é escolhido da lista (autocomplete).
 */
export function VocabularyRegisterPage() {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [wordId, setWordId] = useState(Number(params.get('id') ?? 0))
  const [subjects, setSubjects] = useState<Subject[]>([])
  const [kana, setKana] = useState('')
  const [romaji, setRomaji] = useState('')
  const [slots, setSlots] = useState<Slot[]>([emptySlot(), emptySlot(), emptySlot()])
  const [invalid, setInvalid] = useState(false)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api<Subject[]>('/vocabulary/subjects').then(setSubjects).catch((e) => dialogs.error(e.message))
  }, [dialogs])

  useEffect(() => {
    const id = Number(params.get('id') ?? 0)
    if (id > 0) api<Word>(`/vocabulary/words/${id}`).then(fill).catch((e) => dialogs.error(e.message))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params])

  function fill(w: Word) {
    setWordId(w.id)
    setKana(w.kana ?? '')
    setRomaji(w.romaji ?? '')
    setSlots([
      { kanji: w.kanjiWord1 ?? '', subject: w.subject1 ?? '', translation: w.word1 ?? '', image: null, audio: null, hasImage: w.hasImage1, hasAudio: w.hasAudio1 },
      { kanji: w.kanjiWord2 ?? '', subject: w.subject2 ?? '', translation: w.word2 ?? '', image: null, audio: null, hasImage: w.hasImage2, hasAudio: w.hasAudio2 },
      { kanji: w.kanjiWord3 ?? '', subject: w.subject3 ?? '', translation: w.word3 ?? '', image: null, audio: null, hasImage: w.hasImage3, hasAudio: w.hasAudio3 },
    ])
  }

  async function searchWord() {
    if (!kana.trim()) {
      setInvalid(true)
      return
    }
    setInvalid(false)
    try {
      fill(await api<Word>(`/vocabulary/words?kana=${encodeURIComponent(kana.trim())}`))
    } catch (e) {
      if (isNotFound(e)) dialogs.alert('Palavra não encontrada!')
      else dialogs.error(errorMessage(e, 'Falha na busca.'))
    }
  }

  const setSlot = (index: number, patch: Partial<Slot>) =>
    setSlots((s) => s.map((slot, i) => (i === index ? { ...slot, ...patch } : slot)))

  const subjectId = (name: string) => subjects.find((s) => s.name === name.trim())?.id ?? 0

  async function register() {
    for (const [i, slot] of slots.entries()) {
      if (slot.subject.trim() && subjectId(slot.subject) === 0) {
        dialogs.alert(`O assunto "${slot.subject}" da forma ${i + 1} não existe. Escolha um da lista.`)
        return
      }
    }

    const form = new FormData()
    form.append('ID', String(wordId))
    form.append('Kana', kana)
    form.append('Romaji', romaji)
    slots.forEach((slot, i) => {
      const n = i + 1
      form.append(`KanjiWord${n}`, slot.kanji)
      form.append(`SubjectID${n}`, String(subjectId(slot.subject)))
      form.append(`Word${n}`, slot.translation)
      if (slot.image) form.append(`Image${n}`, slot.image)
      if (slot.audio) form.append(`Audio${n}`, slot.audio)
    })

    setBusy(true)
    try {
      const response = await post<Response>('/vocabulary/words', form)
      await dialogs.info(response.message ?? (wordId === 0 ? 'Palavra cadastrada com sucesso!' : 'Palavra alterada com sucesso!'))
      if (wordId === 0) {
        setKana('')
        setRomaji('')
        setSlots([emptySlot(), emptySlot(), emptySlot()])
      } else {
        navigate(-1)
      }
    } catch (e) {
      dialogs.error(errorMessage(e, wordId === 0 ? 'Não foi possível cadastrar a nova palavra!' : 'Não foi possível alterar a palavra!'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <main>
      <PageTitle>Cadastro de Vocabulário</PageTitle>
      <datalist id="subjects">
        {subjects.map((s) => <option key={s.id} value={s.name} />)}
      </datalist>

      <div className="card stack">
        <label>
          Kana
          <div className="row">
            <input className={`grow ${invalid ? 'is-wrong' : ''}`} lang="ja" value={kana} onChange={(e) => setKana(e.target.value)} />
            <button type="button" onClick={searchWord}>🔍 Procurar</button>
          </div>
        </label>
        <label>Romaji<input value={romaji} onChange={(e) => setRomaji(e.target.value)} /></label>
      </div>

      {slots.map((slot, i) => (
        <fieldset key={i} className="card stack">
          <legend>Forma {i + 1}{i === 0 ? '' : ' (opcional)'}</legend>
          <label>Kanji<input lang="ja" value={slot.kanji} onChange={(e) => setSlot(i, { kanji: e.target.value })} /></label>
          <label>Assunto<input list="subjects" value={slot.subject} onChange={(e) => setSlot(i, { subject: e.target.value })} /></label>
          <label>
            Tradução <span className="muted">(separe as alternativas por vírgula)</span>
            <input value={slot.translation} onChange={(e) => setSlot(i, { translation: e.target.value })} />
          </label>
          <div className="grid2">
            <FileField label="Imagem" accept="image/*" file={slot.image} existing={slot.hasImage} existingLabel="[Imagem]" onChange={(f) => setSlot(i, { image: f })} />
            <FileField label="Áudio" accept="audio/*" file={slot.audio} existing={slot.hasAudio} existingLabel="[Audio]" onChange={(f) => setSlot(i, { audio: f })} />
          </div>
        </fieldset>
      ))}

      <div className="row">
        <button type="button" className="primary" onClick={register} disabled={busy}>{wordId === 0 ? 'Cadastrar' : 'Alterar'}</button>
        <button type="button" onClick={() => navigate('/')}>Cancelar</button>
      </div>
    </main>
  )
}
