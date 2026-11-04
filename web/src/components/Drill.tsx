import { useEffect, useState, type ReactNode } from 'react'
import { errorMessage, fire, post } from '../api/client'
import { useDialogs } from './Dialogs'
import { shuffle } from './common'

/**
 * Mecânica comum das telas de estudo em lista (vocabulário, transcrição e kanjis): sessão gerada
 * no servidor em páginas de 10 → Anterior / Próximo / Corrigir / Repetir / Refazer erradas.
 * A correção e o contador adaptativo ficam no servidor.
 */
export function useDrill<Item, Answer, Result extends { wrong: boolean }>({ basePath, params, emptyMessage, idOf, resultIdOf, checkBody }: {
  /** "/vocabulary", "/transcription" ou "/kanjis" */
  basePath: string
  /** Parâmetros da sessão; null enquanto falta escolher (ex.: assunto). */
  params: Record<string, unknown> | null
  emptyMessage: (mistakes: boolean) => string
  idOf: (item: Item) => number
  resultIdOf: (result: Result) => number
  checkBody: (page: Item[], answers: Record<number, Answer>) => unknown
}) {
  const dialogs = useDialogs()
  const [mistakes, setMistakes] = useState(false)
  const [pages, setPages] = useState<Item[][] | null>(null)
  const [pageIndex, setPageIndex] = useState(0)
  const [page, setPage] = useState<Item[]>([])
  const [answers, setAnswers] = useState<Record<number, Answer>>({})
  const [results, setResults] = useState<Record<number, Result> | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const log = (action: string, option?: string) => fire(`${basePath}/interactions`, { action, option })

  useEffect(() => {
    log('Pagina inicial')
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [basePath])

  // Recarrega quando os parâmetros (assunto, nível, direção) ou o modo mudam.
  const paramsKey = JSON.stringify(params)
  useEffect(() => {
    if (params === null) return
    let cancelled = false
    setBusy(true)
    setMessage(null)
    post<{ pages: Item[][] }>(`${basePath}/sessions`, { ...params, mistakes })
      .then((s) => {
        if (cancelled) return
        setPages(s.pages)
        showPage(s.pages, 0)
        if (s.pages.length === 0) setMessage(emptyMessage(mistakes))
      })
      .catch((e) => !cancelled && setMessage(e.message))
      .finally(() => !cancelled && setBusy(false))
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [basePath, mistakes, paramsKey])

  function reset(items: Item[]) {
    setPage(items)
    setAnswers({})
    setResults(null)
  }

  function showPage(all: Item[][], index: number) {
    setPageIndex(index)
    reset(all[index] ?? [])
  }

  function go(delta: number) {
    const next = pageIndex + delta
    if (!pages || next < 0 || next >= pages.length) return
    showPage(pages, next)
    log(delta > 0 ? 'Proxima pagina' : 'Pagina anterior', String(next + 1))
  }

  function repeat() {
    reset(shuffle(page))
    log('Repetir lista')
  }

  async function check() {
    if (page.length === 0) return
    setBusy(true)
    try {
      const res = await post<Result[]>(`${basePath}/check`, checkBody(page, answers))
      setResults(Object.fromEntries(res.map((r) => [resultIdOf(r), r])))
      if (res.every((r) => !r.wrong)) dialogs.info('Parabéns, todas as respostas estão corretas.')
    } catch (e) {
      dialogs.error(errorMessage(e, 'Falha ao corrigir.'))
    } finally {
      setBusy(false)
    }
  }

  return {
    page, pageIndex, totalPages: pages?.length ?? 0, hasSession: pages !== null,
    answers, setAnswers, results, resultOf: (item: Item) => results?.[idOf(item)],
    message, busy, mistakes, toggleMistakes: () => setMistakes((m) => !m), go, repeat, check,
  }
}

type Drill = Pick<ReturnType<typeof useDrill>,
  'page' | 'pageIndex' | 'totalPages' | 'hasSession' | 'results' | 'message' | 'busy' | 'mistakes' | 'toggleMistakes' | 'go' | 'repeat' | 'check'>

/** Formulário da página atual com a navegação e os botões do original. */
export function DrillForm({ drill, children }: { drill: Drill; children: ReactNode }) {
  const { page, pageIndex, totalPages, results, busy } = drill
  const hasErrors = results !== null && Object.values(results).some((r) => r.wrong)
  const modeButton = (
    <button type="button" onClick={drill.toggleMistakes}>{drill.mistakes ? 'Modo normal' : 'Refazer erradas'}</button>
  )

  return (
    <>
      {page.length > 0 && (
        <form className="card stack" onSubmit={(e) => { e.preventDefault(); drill.check() }}>
          {children}

          <nav className="row center">
            <button type="button" onClick={() => drill.go(-1)} disabled={pageIndex === 0}>Anterior</button>
            <span className="muted">{pageIndex + 1} / {totalPages}</span>
            <button type="button" onClick={() => drill.go(1)} disabled={pageIndex >= totalPages - 1}>Próximo</button>
          </nav>

          <div className="row">
            <button className="primary" disabled={busy || results !== null || hasErrors}>Corrigir</button>
            <button type="button" className="primary" onClick={drill.repeat} disabled={busy}>Repetir</button>
            <span className="spacer" />
            {modeButton}
          </div>
        </form>
      )}

      {page.length === 0 && drill.hasSession && <div className="row">{modeButton}</div>}

      {drill.message && <p className="notice">{drill.message}</p>}
    </>
  )
}
