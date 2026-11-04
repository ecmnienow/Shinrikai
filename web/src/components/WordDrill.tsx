import { useEffect, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { DrillSession, Subject, VocabularyItem, VocabularyResult } from '../api/types'
import { useDialogs } from './Dialogs'
import { shuffle } from './common'

/**
 * Tela de estudo em lista: Views/Vocabulary/Index.cshtml e Views/Transcription/Index.cshtml.
 * Assunto → lista embaralhada em páginas de 10 → Anterior / Próximo / Corrigir / Repetir /
 * Refazer erradas. A correção e o contador adaptativo ficam no servidor.
 */
export function WordDrill({ basePath, extraControls, sessionParams, promptLang, answerLang }: {
  /** "/vocabulary" ou "/transcription" */
  basePath: string
  /** Controles extras (ex.: direção da tradução) — mudam a sessão via sessionParams. */
  extraControls?: ReactNode
  sessionParams?: Record<string, unknown>
  checkParams?: Record<string, unknown>
  promptLang: string
  answerLang: string
}) {
  const dialogs = useDialogs()
  const [subjects, setSubjects] = useState<Subject[]>([])
  const [subjectId, setSubjectId] = useState<number | null>(null)
  const [mistakes, setMistakes] = useState(false)

  const [session, setSession] = useState<DrillSession | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [page, setPage] = useState<VocabularyItem[]>([])
  const [answers, setAnswers] = useState<Record<number, string>>({})
  const [results, setResults] = useState<Record<number, VocabularyResult> | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const log = (action: string, option?: string) =>
    api(`${basePath}/interactions`, { method: 'POST', json: { action, option } }).catch(() => {})

  useEffect(() => {
    log('Pagina inicial')
    api<Subject[]>(`${basePath}/subjects`).then(setSubjects).catch((e) => setMessage(e.message))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [basePath])

  const paramsKey = JSON.stringify(sessionParams ?? {})

  // LoadSubject: recarrega quando assunto, modo ou parâmetros (direção) mudam.
  useEffect(() => {
    if (subjectId === null) return
    let cancelled = false
    setBusy(true)
    setMessage(null)
    api<DrillSession>(`${basePath}/sessions`, { method: 'POST', json: { subjectId, mistakes, ...sessionParams } })
      .then((s) => {
        if (cancelled) return
        setSession(s)
        showPage(s, 0)
        if (s.totalItems === 0)
          setMessage(mistakes ? 'Nenhuma palavra pendente neste assunto. 🎉' : 'Este assunto não tem palavras para estudar.')
      })
      .catch((e) => !cancelled && setMessage(e.message))
      .finally(() => !cancelled && setBusy(false))
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [basePath, subjectId, mistakes, paramsKey])

  function showPage(s: DrillSession, index: number) {
    setPageIndex(index)
    setPage(s.pages[index] ?? [])
    setAnswers({})
    setResults(null)
  }

  function go(delta: number) {
    if (!session) return
    const next = pageIndex + delta
    if (next < 0 || next >= session.pages.length) return
    showPage(session, next)
    log(delta > 0 ? 'Proxima pagina' : 'Pagina anterior', String(next + 1))
  }

  function repeat() {
    setPage((p) => shuffle(p))
    setAnswers({})
    setResults(null)
    log('Repetir lista')
  }

  async function check() {
    if (!session || page.length === 0) return
    setBusy(true)
    try {
      const res = await api<VocabularyResult[]>(`${basePath}/check`, {
        method: 'POST',
        json: {
          subject: session.subject,
          ...sessionParams,
          answers: page.map((i) => ({ wordId: i.wordId, prompt: i.prompt, answer: answers[i.wordId] ?? '' })),
        },
      })
      setResults(Object.fromEntries(res.map((r) => [r.wordId, r])))
      if (res.every((r) => !r.wrong)) dialogs.info('Parabéns, todas as respostas estão corretas.')
    } catch (e) {
      dialogs.error(e instanceof Error ? e.message : 'Falha ao corrigir.')
    } finally {
      setBusy(false)
    }
  }

  const totalPages = session?.pages.length ?? 0
  const hasErrors = results !== null && Object.values(results).some((r) => r.wrong)

  return (
    <>
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
        {extraControls}
      </div>

      {page.length > 0 && (
        <form className="card stack" onSubmit={(e) => { e.preventDefault(); check() }}>
          {page.map((item) => {
            const r = results?.[item.wordId]
            return (
              <div key={item.wordId} className={`word-row ${r ? (r.wrong ? 'wrong' : 'right') : ''}`}>
                <span className="prompt" lang={promptLang}>{item.prompt}</span>
                <input
                  lang={answerLang}
                  value={answers[item.wordId] ?? ''}
                  readOnly={results !== null}
                  aria-label={`Resposta para ${item.prompt}`}
                  onChange={(e) => setAnswers((a) => ({ ...a, [item.wordId]: e.target.value }))}
                />
                {r?.wrong && <span className="expected" lang={answerLang}>{r.expected.join(', ')}</span>}
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
            <button type="button" onClick={() => setMistakes((m) => !m)}>{mistakes ? 'Modo normal' : 'Refazer erradas'}</button>
          </div>
        </form>
      )}

      {page.length === 0 && subjectId !== null && session && (
        <div className="row">
          <button type="button" onClick={() => setMistakes((m) => !m)}>{mistakes ? 'Modo normal' : 'Refazer erradas'}</button>
        </div>
      )}

      {message && <p className="notice">{message}</p>}
    </>
  )
}
