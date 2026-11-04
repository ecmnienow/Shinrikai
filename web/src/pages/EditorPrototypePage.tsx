import { useState, type MouseEvent } from 'react'
import { TextEditor } from '../editor/TextEditor'

const SAMPLE =
  '<p>私は毎日日本語「にほんご」を勉強「べんきょう」します。</p>' +
  '<p>家に<span class="stretchText" data-id="1"><ruby><rb>猫</rb><rt>ねこ</rt></ruby></span>がいます。</p>'

/**
 * Protótipo isolado do editor (fase 5 do plano). Mostra o HTML gerado e uma prévia que
 * imita a leitura do aluno: furigana de palavras do vocabulário começa escondida e
 * aparece ao clicar (stretchtext) — no sistema real isso dispara RegisterLackOfKnowledge.
 */
export function EditorPrototypePage() {
  const [html, setHtml] = useState('')
  const [studentView, setStudentView] = useState(true)

  function reveal(e: MouseEvent<HTMLDivElement>) {
    const span = (e.target as HTMLElement).closest('.stretchText')
    span?.classList.add('revealed')
  }

  return (
    <main>
      <h1>Editor de textos — protótipo</h1>
      <p className="muted">
        Selecione um trecho e use <b>字 Furigana</b> ou <b>🔗 Vocabulário</b>. Textos antigos com a notação
        漢字「かんじ」 são convertidos ao abrir.
      </p>

      <TextEditor initialHtml={SAMPLE} onChange={setHtml} />

      <div className="grid2">
        <section>
          <div className="row">
            <h2>Prévia</h2>
            <span className="spacer" />
            <label className="inline">
              <input type="checkbox" checked={studentView} onChange={(e) => setStudentView(e.target.checked)} /> Visão do aluno
            </label>
          </div>
          <div
            key={html + studentView}
            className={`card reading ${studentView ? 'student' : ''}`}
            lang="ja"
            onClick={reveal}
            dangerouslySetInnerHTML={{ __html: html }}
          />
        </section>
        <section>
          <h2>HTML gerado</h2>
          <pre className="card code">{html}</pre>
        </section>
      </div>
    </main>
  )
}
