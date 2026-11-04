import { useEffect, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { Subject, VocabularyItem, VocabularyResult } from '../api/types'
import { DrillForm, useDrill } from './Drill'

/**
 * Tela de estudo em lista: Views/Vocabulary/Index.cshtml e Views/Transcription/Index.cshtml.
 * Assunto → lista embaralhada em páginas de 10 (ver {@link useDrill}).
 */
export function WordDrill({ basePath, extraControls, sessionParams, promptLang, answerLang }: {
  /** "/vocabulary" ou "/transcription" */
  basePath: string
  /** Controles extras (ex.: direção da tradução) — mudam a sessão via sessionParams. */
  extraControls?: ReactNode
  sessionParams?: Record<string, unknown>
  promptLang: string
  answerLang: string
}) {
  const [subjects, setSubjects] = useState<Subject[]>([])
  const [subject, setSubject] = useState<Subject | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<Subject[]>(`${basePath}/subjects`).then(setSubjects).catch((e) => setError(e.message))
  }, [basePath])

  const drill = useDrill<VocabularyItem, string, VocabularyResult>({
    basePath,
    params: subject && { subjectId: subject.id, ...sessionParams },
    emptyMessage: (mistakes) => (mistakes ? 'Nenhuma palavra pendente neste assunto. 🎉' : 'Este assunto não tem palavras para estudar.'),
    idOf: (i) => i.wordId,
    resultIdOf: (r) => r.wordId,
    checkBody: (page, answers) => ({
      subject: subject?.name,
      ...sessionParams,
      answers: page.map((i) => ({ wordId: i.wordId, prompt: i.prompt, answer: answers[i.wordId] ?? '' })),
    }),
  })

  return (
    <>
      <div className="card stack">
        <label>
          Assunto
          <select value={subject?.id ?? ''} onChange={(e) => setSubject(subjects.find((s) => s.id === Number(e.target.value)) ?? null)}>
            <option value="">Selecione</option>
            {subjects.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </label>
        {extraControls}
      </div>

      <DrillForm drill={drill}>
        {drill.page.map((item) => {
          const r = drill.resultOf(item)
          return (
            <div key={item.wordId} className={`word-row ${r ? (r.wrong ? 'wrong' : 'right') : ''}`}>
              <span className="prompt" lang={promptLang}>{item.prompt}</span>
              <input
                lang={answerLang}
                value={drill.answers[item.wordId] ?? ''}
                readOnly={drill.results !== null}
                aria-label={`Resposta para ${item.prompt}`}
                onChange={(e) => drill.setAnswers((a) => ({ ...a, [item.wordId]: e.target.value }))}
              />
              {r?.wrong && <span className="expected" lang={answerLang}>{r.expected.join(', ')}</span>}
            </div>
          )
        })}
      </DrillForm>

      {error && <p className="notice">{error}</p>}
    </>
  )
}
