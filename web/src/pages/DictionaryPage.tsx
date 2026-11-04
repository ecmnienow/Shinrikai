import { useEffect, useState, type FormEvent } from 'react'
import { api, errorMessage, fetchBlobUrl, fire } from '../api/client'
import type { SearchResult, SearchType, Word } from '../api/types'
import { EditLink, PageTitle, playAudio } from '../components/common'
import { useDialogs } from '../components/Dialogs'

const TYPES: { value: SearchType; label: string }[] = [
  { value: 'General', label: 'Geral' },
  { value: 'Japanese', label: 'Japonês' },
  { value: 'Portuguese', label: 'Português' },
]

function slots(word: Word, kind: 'image' | 'audio') {
  const flags = kind === 'image' ? [word.hasImage1, word.hasImage2, word.hasImage3] : [word.hasAudio1, word.hasAudio2, word.hasAudio3]
  return flags.map((has, i) => (has ? i + 1 : 0)).filter(Boolean)
}

/** Realça o termo buscado dentro do termo encontrado (Search.cshtml). */
function Highlight({ text, term }: { text: string; term: string }) {
  const parts = text.split(term)
  return (
    <>
      {parts.map((p, i) => (
        <span key={i}>
          {p}
          {i < parts.length - 1 && <mark>{term}</mark>}
        </span>
      ))}
    </>
  )
}

/** Port de Views/Dictionary/Index.cshtml + Search.cshtml. */
export function DictionaryPage() {
  const dialogs = useDialogs()
  const [term, setTerm] = useState('')
  const [type, setType] = useState<SearchType>('General')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [invalid, setInvalid] = useState(false)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    fire('/dictionary/interactions')
  }, [])

  async function search(e?: FormEvent) {
    e?.preventDefault()
    if (!term.trim()) {
      setInvalid(true)
      return
    }
    setInvalid(false)
    setBusy(true)
    try {
      setResult(await api<SearchResult>(`/dictionary/search?term=${encodeURIComponent(term)}&type=${type}`))
    } catch (err) {
      dialogs.error(errorMessage(err, 'Falha na busca.'))
    } finally {
      setBusy(false)
    }
  }

  async function showImages(word: Word) {
    for (const slot of slots(word, 'image')) {
      const url = await fetchBlobUrl(`/vocabulary/words/${word.id}/image/${slot}`)
      if (url) {
        dialogs.showImage(url, word.kana)
        return
      }
    }
  }

  async function play(word: Word) {
    const slot = slots(word, 'audio')[0]
    if (!slot) return
    const url = await fetchBlobUrl(`/vocabulary/words/${word.id}/audio/${slot}`)
    if (url) playAudio(url)
  }

  return (
    <main>
      <PageTitle>Dicionário</PageTitle>
      <form className="card stack" onSubmit={search}>
        <label>
          Buscar
          <div className="row">
            <input
              className={`grow ${invalid ? 'is-wrong' : ''}`}
              value={term}
              onChange={(e) => setTerm(e.target.value)}
              placeholder="Pesquisa"
              lang="ja"
            />
            <button className="primary" disabled={busy}>🔍 Buscar</button>
          </div>
        </label>
        <div className="row" role="radiogroup" aria-label="Tipo de busca">
          {TYPES.map((t) => (
            <label key={t.value} className="inline">
              <input type="radio" name="searchType" checked={type === t.value} onChange={() => setType(t.value)} /> {t.label}
            </label>
          ))}
        </div>
      </form>

      {result && (
        <section className="card">
          {result.results.length === 0 ? (
            <h2>Nenhum resultado para “{result.term}”</h2>
          ) : (
            <>
              <h2>Resultados para “{result.term}”</h2>
              <ul className="results">
                {result.results.map((r, i) => {
                  const kanji = [r.word.kanjiWord1, r.word.kanjiWord2, r.word.kanjiWord3].filter((k, idx, arr) => k && arr.indexOf(k) === idx)
                  const translations = [r.word.word1, r.word.word2, r.word.word3].filter(Boolean).flatMap((w) => w!.split(','))
                  return (
                    <li key={`${r.word.id}-${i}`} className="result">
                      <div className="found" lang="ja"><Highlight text={r.foundTerm} term={result.term} /></div>
                      <div>
                        <span className="actions">
                          <EditLink className="" to={`/professor/vocabulario?id=${r.word.id}`} label="Editar palavra" />
                          {slots(r.word, 'image').length > 0 && (
                            <button type="button" className="icon" title="Ver imagem" aria-label="Ver imagem" onClick={() => showImages(r.word)}>🖼</button>
                          )}
                          {slots(r.word, 'audio').length > 0 && (
                            <button type="button" className="icon" title="Tocar áudio" aria-label="Tocar áudio" onClick={() => play(r.word)}>▶</button>
                          )}
                        </span>
                        <span lang="ja">{r.word.kana}{kanji.length > 0 && `  [ ${kanji.join(', ')} ]`}</span>
                        <div className="muted">{translations.join(', ')}</div>
                      </div>
                    </li>
                  )
                })}
              </ul>
            </>
          )}
        </section>
      )}
    </main>
  )
}
