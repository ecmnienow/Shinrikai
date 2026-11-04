import { useEffect, useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { Word } from '../api/types'
import { Modal } from '../components/Dialogs'

/**
 * Escolhe a palavra do vocabulário para um link de stretchtext. Busca pelo kanji/tradução
 * (TranscriptionController.SearchKanjiWord do original) e pelo kana exato; também aceita o ID
 * manualmente. Resolve com { wordId, reading } — reading = kana da palavra (vira o <rt>).
 */
export function VocabularyPicker({ initialTerm, onDone }: {
  initialTerm: string
  onDone: (picked: { wordId: number; reading: string } | null) => void
}) {
  const [term, setTerm] = useState(initialTerm)
  const [results, setResults] = useState<Word[] | null>(null)
  const [manualId, setManualId] = useState('')
  const [manualReading, setManualReading] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function search(e?: FormEvent) {
    e?.preventDefault()
    if (!term.trim()) return
    setError(null)
    try {
      const byKanji = await api<Word[]>(`/transcription/kanji-words?term=${encodeURIComponent(term.trim())}`)
      let byKana: Word[] = []
      try {
        byKana = [await api<Word>(`/vocabulary/words?kana=${encodeURIComponent(term.trim())}`)]
      } catch (err) {
        if (!(err instanceof ApiError && err.status === 404)) throw err
      }
      const all = [...byKana, ...byKanji].filter((w, i, arr) => arr.findIndex((x) => x.id === w.id) === i)
      setResults(all)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na busca.')
    }
  }

  useEffect(() => {
    search()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <Modal title="Ligar ao vocabulário" onClose={() => onDone(null)} wide footer={<button type="button" onClick={() => onDone(null)}>Cancelar</button>}>
      <form className="row" onSubmit={search}>
        <input className="grow" lang="ja" value={term} onChange={(e) => setTerm(e.target.value)} aria-label="Buscar palavra" />
        <button className="primary">Buscar</button>
      </form>
      {error && <p className="error">{error}</p>}
      {results && results.length === 0 && <p className="muted">Nenhuma palavra encontrada.</p>}
      {results && results.length > 0 && (
        <ul className="picker-list">
          {results.map((w) => (
            <li key={w.id}>
              <button type="button" onClick={() => onDone({ wordId: w.id, reading: w.kana })}>
                <strong lang="ja">{[w.kanjiWord1, w.kanjiWord2, w.kanjiWord3].filter(Boolean).join('、') || w.kana}</strong>
                <span lang="ja"> {w.kana}</span>
                <span className="muted"> — {[w.word1, w.word2, w.word3].filter(Boolean).join('; ')} (#{w.id})</span>
              </button>
            </li>
          ))}
        </ul>
      )}
      <details>
        <summary>Informar o ID manualmente</summary>
        <div className="row">
          <input inputMode="numeric" placeholder="VCB_ID" value={manualId} onChange={(e) => setManualId(e.target.value)} aria-label="ID da palavra" />
          <input lang="ja" placeholder="leitura (kana)" value={manualReading} onChange={(e) => setManualReading(e.target.value)} aria-label="Leitura" />
          <button
            type="button"
            disabled={!(Number(manualId) > 0) || !manualReading.trim()}
            onClick={() => onDone({ wordId: Number(manualId), reading: manualReading.trim() })}
          >
            Usar
          </button>
        </div>
      </details>
    </Modal>
  )
}
