import { useState } from 'react'
import { PageTitle } from '../components/common'
import { HELP, HelpButton } from '../components/Help'
import { WordDrill } from '../components/WordDrill'

/** Port de Views/Vocabulary/Index.cshtml (Estudo de Vocabulário). */
export function VocabularyPage() {
  const [toJapanese, setToJapanese] = useState(false)

  return (
    <main className="narrow">
      <PageTitle help={<HelpButton id={HELP.vocabulary} />}>Estudo de Vocabulário</PageTitle>
      <WordDrill
        basePath="/vocabulary"
        sessionParams={{ toJapanese }}
        promptLang={toJapanese ? 'pt-BR' : 'ja'}
        answerLang={toJapanese ? 'ja' : 'pt-BR'}
        extraControls={
          <div className="row" role="radiogroup" aria-label="Direção">
            <label className="inline">
              <input type="radio" checked={!toJapanese} onChange={() => setToJapanese(false)} /> Japonês para português
            </label>
            <label className="inline">
              <input type="radio" checked={toJapanese} onChange={() => setToJapanese(true)} /> Português para japonês
            </label>
          </div>
        }
      />
    </main>
  )
}
