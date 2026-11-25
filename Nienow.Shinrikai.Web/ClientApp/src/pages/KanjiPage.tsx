import { useState } from 'react'
import { LEVELS, type KanjiItem, type KanjiResult } from '../api/types'
import { PageTitle } from '../components/common'
import { DrillForm, useDrill } from '../components/Drill'
import { HELP, HelpButton } from '../components/Help'

function Expected({ result }: { result: KanjiResult }) {
  const others = result.expected.filter((e) => !result.required.includes(e))
  if (result.required.length === 0) return <span className="expected" lang="ja">{result.expected.join(', ')}</span>
  return (
    <span className="expected" lang="ja">
      <strong>{result.required.join(', ')}</strong>
      {others.length > 0 && <span className="muted"> (também valem: {others.join(', ')})</span>}
    </span>
  )
}

/**
 * Port de Views/Kanjis/Index.cshtml (Leitura de Kanjis): nível N5…N1 (cumulativo), páginas de 10,
 * uma caixa por leitura exigida no nível (até 3). O kanji só conta como acerto com todas as leituras certas.
 */
export function KanjiPage() {
  // O original já carregava o N5 ao abrir a tela.
  const [level, setLevel] = useState('N5')

  const drill = useDrill<KanjiItem, string[], KanjiResult>({
    basePath: '/kanjis',
    params: { level },
    emptyMessage: (mistakes) => (mistakes ? 'Nenhum kanji pendente neste nível. 🎉' : 'Nenhum kanji cadastrado para este nível.'),
    idOf: (k) => k.kanjiId,
    resultIdOf: (r) => r.kanjiId,
    checkBody: (page, answers) => ({
      level,
      answers: page.map((k) => ({
        kanjiId: k.kanjiId,
        kanji: k.kanji,
        answers: Array.from({ length: k.inputs }, (_, i) => answers[k.kanjiId]?.[i] ?? ''),
      })),
    }),
  })

  function setAnswer(kanjiId: number, index: number, value: string) {
    drill.setAnswers((a) => {
      const current = [...(a[kanjiId] ?? [])]
      current[index] = value
      return { ...a, [kanjiId]: current }
    })
  }

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

      <DrillForm drill={drill}>
        {drill.page.map((k) => {
          const r = drill.resultOf(k)
          return (
            <div key={k.kanjiId} className="kanji-row">
              <span className="kanji" lang="ja">{k.kanji}</span>
              <div className="kanji-inputs" style={{ gridTemplateColumns: `repeat(${k.inputs}, 1fr)` }}>
                {Array.from({ length: k.inputs }, (_, i) => (
                  <input
                    key={i}
                    lang="ja"
                    className={r ? (r.wrongInputs[i] ? 'is-wrong' : 'is-right') : ''}
                    value={drill.answers[k.kanjiId]?.[i] ?? ''}
                    readOnly={drill.results !== null}
                    aria-label={`Leitura ${i + 1} de ${k.kanji}`}
                    onChange={(e) => setAnswer(k.kanjiId, i, e.target.value)}
                  />
                ))}
              </div>
              {r?.wrong && <Expected result={r} />}
            </div>
          )
        })}
      </DrillForm>
    </main>
  )
}
