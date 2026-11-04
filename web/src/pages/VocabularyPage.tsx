import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { Subject, VocabularyItem, VocabularyResult, VocabularySession } from '../api/types'

/**
 * Port de Views/Vocabulary/Index.cshtml.
 * A paginação (Anterior / Próximo / Repetir) agora é feita no cliente; a correção e o
 * contador adaptativo (variação de fragmento) continuam no servidor.
 */
export function VocabularyPage() {
  const [subjects, setSubjects] = useState<Subject[]>([])
  const [subjectId, setSubjectId] = useState<number | null>(null)
  const [toJapanese, setToJapanese] = useState(false)
  const [mistakes, setMistakes] = useState(false)

  const [session, setSession] = useState<VocabularySession | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [page, setPage] = useState<VocabularyItem[]>([])
  const [answers, setAnswers] = useState<Record<number, string>>({})
  const [results, setResults] = useState<Record<number, VocabularyResult> | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api<Subject[]>('/vocabulary/subjects').then(setSubjects).catch((e) => setMessage(e.message))
  }, [])

  // Recarrega a lista sempre que assunto, direção ou modo mudam (LoadSubject do original).
  useEffect(() => {
    if (subjectId === null) return
    let cancelled = false
    setBusy(true)
    setMessage(null)
    api<VocabularySession>('/vocabulary/sessions', { method: 'POST', json: { subjectId, toJapanese, mistakes } })
      .then((s) => {
        if (cancelled) return
        setSession(s)
        showPage(s, 0)
        if (s.totalItems === 0)
          setMessage(mistakes ? 'Nenhuma palavra pendente neste assunto. 🎉' : 'Este assunto não tem vocabulário.')
      })
      .catch((e) => !cancelled && setMessage(e.message))
      .finally(() => !cancelled && setBusy(false))
    return () => {
      cancelled = true
    }
  }, [subjectId, toJapanese, mistakes])

  function showPage(s: VocabularySession, index: number) {
    setPageIndex(index)
    setPage(s.pages[index] ?? [])
    setAnswers({})
    setResults(null)
  }

  function log(action: string, option?: string) {
    api('/vocabulary/interactions', { method: 'POST', json: { action, option } }).catch(() => {})
  }

  function go(delta: number) {
    if (!session) return
    const next = pageIndex + delta
    if (next < 0 || next >= session.pages.length) return
    showPage(session, next)
    log(delta > 0 ? 'Proxima pagina' : 'Pagina anterior', String(next + 1))
  }

  function repeat() {
    const shuffled = [...page]
    for (let i = shuffled.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1))
      ;[shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]]
    }
    setPage(shuffled)
    setAnswers({})
    setResults(null)
    log('Repetir lista')
  }

  async function check() {
    if (!session || page.length === 0) return
    setBusy(true)
    try {
      const res = await api<VocabularyResult[]>('/vocabulary/check', {
        method: 'POST',
        json: {
          subject: session.subject,
          toJapanese: session.toJapanese,
          answers: page.map((i) => ({ wordId: i.wordId, prompt: i.prompt, answer: answers[i.wordId] ?? '' })),
        },
      })
      setResults(Object.fromEntries(res.map((r) => [r.wordId, r])))
      setMessage(res.every((r) => !r.wrong) ? 'Parabéns, todas as respostas estão corretas.' : null)
    } catch (e) {
      setMessage(e instanceof Error ? e.message : 'Falha ao corrigir.')
    } finally {
      setBusy(false)
    }
  }

  const totalPages = session?.pages.length ?? 0
  const hasErrors = results !== null && Object.values(results).some((r) => r.wrong)

  return (
    <main className="narrow">
      <h1>Estudo de Vocabulário</h1>

      <div className="card stack">
        <label>
          Assunto
          <select value={subjectId ?? ''} onChange={(e) => setSubjectId(e.target.value ? Number(e.target.value) : null)}>
            <option value="">Selecione</option>
            {subjects.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </label>
        <div className="row">
          <label className="inline">
            <input type="radio" checked={!toJapanese} onChange={() => setToJapanese(false)} /> Japonês para português
          </label>
          <label className="inline">
            <input type="radio" checked={toJapanese} onChange={() => setToJapanese(true)} /> Português para japonês
          </label>
        </div>
      </div>

      {page.length > 0 && (
        <form className="card stack" onSubmit={(e) => { e.preventDefault(); check() }}>
          {page.map((item) => {
            const r = results?.[item.wordId]
            return (
              <div key={item.wordId} className={`word-row ${r ? (r.wrong ? 'wrong' : 'right') : ''}`}>
                <span className="prompt" lang={session?.toJapanese ? 'pt-BR' : 'ja'}>{item.prompt}</span>
                <input
                  lang={session?.toJapanese ? 'ja' : 'pt-BR'}
                  value={answers[item.wordId] ?? ''}
                  readOnly={results !== null}
                  onChange={(e) => setAnswers((a) => ({ ...a, [item.wordId]: e.target.value }))}
                />
                {r?.wrong && <span className="expected">{r.expected.join(', ')}</span>}
              </div>
            )
          })}

          <nav className="row center">
            <button type="button" onClick={() => go(-1)} disabled={pageIndex === 0}>Anterior</button>
            <span className="muted">{pageIndex + 1} / {totalPages}</span>
            <button type="button" onClick={() => go(1)} disabled={pageIndex >= totalPages - 1}>Próximo</button>
          </nav>

          <div className="row">
            <button className="primary" disabled={busy || results !== null || hasErrors}>Corrigir</button>
            <button type="button" className="primary" onClick={repeat} disabled={busy}>Repetir</button>
            <span className="spacer" />
            <button type="button" onClick={() => setMistakes((m) => !m)} disabled={subjectId === null}>
              {mistakes ? 'Modo normal' : 'Refazer erradas'}
            </button>
          </div>
        </form>
      )}

      {page.length === 0 && subjectId !== null && session && (
        <div className="row">
          <button type="button" onClick={() => setMistakes((m) => !m)}>{mistakes ? 'Modo normal' : 'Refazer erradas'}</button>
        </div>
      )}

      {message && <p className="notice">{message}</p>}
    </main>
  )
}
