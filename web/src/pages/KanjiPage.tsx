import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { LEVELS, type KanjiItem, type KanjiResult, type KanjiSession } from '../api/types'
import { PageTitle, shuffle } from '../components/common'
import { useDialogs } from '../components/Dialogs'
import { HELP, HelpButton } from '../components/Help'

/**
 * Port de Views/Kanjis/Index.cshtml (Leitura de Kanjis): nível N5…N1 (cumulativo), páginas de 10,
 * uma caixa por leitura (até 3). O kanji só conta como acerto com todas as leituras certas.
 */
export function KanjiPage() {
  const dialogs = useDialogs()
  const [level, setLevel] = useState('N5')
  const [mistakes, setMistakes] = useState(false)
  const [session, setSession] = useState<KanjiSession | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [page, setPage] = useState<KanjiItem[]>([])
  const [answers, setAnswers] = useState<Record<number, string[]>>({})
  const [results, setResults] = useState<Record<number, KanjiResult> | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const log = (action: string, option?: string) =>
    api('/kanjis/interactions', { method: 'POST', json: { action, option } }).catch(() => {})

  useEffect(() => {
    log('Pagina inicial')
  }, [])

  // O original já carregava o N5 ao abrir a tela.
  useEffect(() => {
    let cancelled = false
    setBusy(true)
    setMessage(null)
    api<KanjiSession>('/kanjis/sessions', { method: 'POST', json: { level, mistakes } })
      .then((s) => {
        if (cancelled) return
        setSession(s)
        showPage(s, 0)
        if (s.totalItems === 0)
          setMessage(mistakes ? 'Nenhum kanji pendente neste nível. 🎉' : 'Nenhum kanji cadastrado para este nível.')
      })
      .catch((e) => !cancelled && setMessage(e.message))
      .finally(() => !cancelled && setBusy(false))
    return () => {
      cancelled = true
    }
  }, [level, mistakes])

  function showPage(s: KanjiSession, index: number) {
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

  function setAnswer(kanjiId: number, index: number, value: string) {
    setAnswers((a) => {
      const current = [...(a[kanjiId] ?? [])]
      current[index] = value
      return { ...a, [kanjiId]: current }
    })
  }

  async function check() {
    if (page.length === 0) return
    setBusy(true)
    try {
      const res = await api<KanjiResult[]>('/kanjis/check', {
        method: 'POST',
        json: {
          answers: page.map((k) => ({
            kanjiId: k.kanjiId,
            kanji: k.kanji,
            answers: Array.from({ length: k.inputs }, (_, i) => answers[k.kanjiId]?.[i] ?? ''),
          })),
        },
      })
      setResults(Object.fromEntries(res.map((r) => [r.kanjiId, r])))
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
    <main className="narrow">
      <PageTitle help={<HelpButton id={HELP.kanji} />}>Leitura de Kanjis</PageTitle>

      <div className="card row" role="radiogroup" aria-label="Nível">
        {LEVELS.map((l) => (
          <label key={l} className="inline">
            <input type="radio" name="kanjiLevel" checked={level === l} onChange={() => setLevel(l)} /> {l}
          </label>
        ))}
      </div>

      {page.length > 0 && (
        <form className="card stack" onSubmit={(e) => { e.preventDefault(); check() }}>
          {page.map((k) => {
            const r = results?.[k.kanjiId]
            return (
              <div key={k.kanjiId} className="kanji-row">
                <span className="kanji" lang="ja">{k.kanji}</span>
                <div className="kanji-inputs" style={{ gridTemplateColumns: `repeat(${k.inputs}, 1fr)` }}>
                  {Array.from({ length: k.inputs }, (_, i) => (
                    <input
                      key={i}
                      lang="ja"
                      className={r ? (r.wrongInputs[i] ? 'is-wrong' : 'is-right') : ''}
                      value={answers[k.kanjiId]?.[i] ?? ''}
                      readOnly={results !== null}
                      aria-label={`Leitura ${i + 1} de ${k.kanji}`}
                      onChange={(e) => setAnswer(k.kanjiId, i, e.target.value)}
                    />
                  ))}
                </div>
                {r?.wrong && <span className="expected" lang="ja">{r.expected.join(', ')}</span>}
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

      {page.length === 0 && session && (
        <div className="row">
          <button type="button" onClick={() => setMistakes((m) => !m)}>{mistakes ? 'Modo normal' : 'Refazer erradas'}</button>
        </div>
      )}

      {message && <p className="notice">{message}</p>}
    </main>
  )
}
