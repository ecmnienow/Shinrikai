import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, errorMessage, isNotFound, post } from '../../api/client'
import type { Jlpt, Kanji, Response } from '../../api/types'
import { FileField, LevelRadios, PageTitle } from '../../components/common'
import { useDialogs } from '../../components/Dialogs'

/** Port de Views/Kanjis/Register.cshtml: kanji (com busca), kana, romaji, nível, informações e imagem. */
export function KanjiRegisterPage() {
  const dialogs = useDialogs()
  const navigate = useNavigate()
  const [kanjiId, setKanjiId] = useState(0)
  const [letter, setLetter] = useState('')
  const [kana, setKana] = useState('')
  const [romaji, setRomaji] = useState('')
  const [level, setLevel] = useState<Jlpt>('None')
  const [info, setInfo] = useState('')
  const [image, setImage] = useState<File | null>(null)
  const [hasImage, setHasImage] = useState(false)
  const [invalid, setInvalid] = useState<Set<string>>(new Set())
  const [busy, setBusy] = useState(false)

  function reset() {
    setKanjiId(0)
    setLetter('')
    setKana('')
    setRomaji('')
    setLevel('None')
    setInfo('')
    setImage(null)
    setHasImage(false)
    setInvalid(new Set())
  }

  async function search() {
    if (!letter.trim()) {
      setInvalid(new Set(['letter']))
      return
    }
    try {
      const k = await api<Kanji>(`/kanjis?letter=${encodeURIComponent(letter.trim())}`)
      setKanjiId(k.id)
      setKana(k.kana ?? '')
      setRomaji(k.romaji ?? '')
      setInfo(k.info ?? '')
      setLevel((k.jlpt as Jlpt | null) ?? 'None')
      setHasImage(k.hasImage)
      setInvalid(new Set())
    } catch (e) {
      if (isNotFound(e)) dialogs.alert('Kanji não encontrado!')
      else dialogs.error(errorMessage(e, 'Falha na busca.'))
    }
  }

  async function register() {
    const bad = new Set<string>()
    if ([...letter.trim()].length !== 1) bad.add('letter')
    if (!kana.trim()) bad.add('kana')
    if (!romaji.trim()) bad.add('romaji')
    if (!info.trim()) bad.add('info')
    setInvalid(bad)
    if (bad.size > 0) return

    const form = new FormData()
    form.append('ID', String(kanjiId))
    form.append('KanjiLetter', letter.trim())
    form.append('Kana', kana)
    form.append('Romaji', romaji)
    form.append('JLPT', level === 'None' ? 'none' : level)
    form.append('Info', info)
    if (image) form.append('Image', image)

    setBusy(true)
    try {
      const response = await post<Response>('/kanjis', form)
      await dialogs.info(response.message ?? 'Kanji cadastrado com sucesso!')
      reset()
    } catch (e) {
      dialogs.error(errorMessage(e, kanjiId === 0 ? 'Não foi possível cadastrar o novo kanji!' : 'Não foi possível alterar o kanji!'))
    } finally {
      setBusy(false)
    }
  }

  const cls = (key: string) => (invalid.has(key) ? 'is-wrong' : '')

  return (
    <main className="narrow">
      <PageTitle>Cadastro de Kanjis</PageTitle>
      <div className="card stack">
        <label>
          Kanji
          <div className="row">
            <input className={`kanji-input ${cls('letter')}`} lang="ja" value={letter} disabled={kanjiId !== 0} onChange={(e) => setLetter(e.target.value)} />
            <button type="button" onClick={search} disabled={kanjiId !== 0}>🔍 Procurar</button>
            {kanjiId !== 0 && <button type="button" onClick={reset}>Novo</button>}
          </div>
        </label>
        <label>Kana <span className="muted">(leituras separadas por vírgula)</span><input className={cls('kana')} lang="ja" value={kana} onChange={(e) => setKana(e.target.value)} /></label>
        <label>Romaji<input className={cls('romaji')} value={romaji} onChange={(e) => setRomaji(e.target.value)} /></label>
        <fieldset>
          <legend>Nível aconselhado</legend>
          <LevelRadios name="kanjiLevel" value={level} onChange={setLevel} />
        </fieldset>
        <label>Informações<textarea className={cls('info')} rows={3} value={info} onChange={(e) => setInfo(e.target.value)} /></label>
        <FileField label="Imagem" accept="image/*" file={image} existing={hasImage} existingLabel="[Imagem]" onChange={setImage} />
        <div className="row">
          <button type="button" className="primary" onClick={register} disabled={busy}>{kanjiId === 0 ? 'Cadastrar' : 'Alterar'}</button>
          <button type="button" onClick={() => navigate('/')}>Cancelar</button>
        </div>
      </div>
    </main>
  )
}
