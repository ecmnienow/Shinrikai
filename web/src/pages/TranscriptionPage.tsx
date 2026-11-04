import { PageTitle } from '../components/common'
import { HELP, HelpButton } from '../components/Help'
import { WordDrill } from '../components/WordDrill'

/** Port de Views/Transcription/Index.cshtml: o aluno vê a palavra em kanji e escreve em hiragana. */
export function TranscriptionPage() {
  return (
    <main className="narrow">
      <PageTitle help={<HelpButton id={HELP.transcription} />}>Transcrição para Hiragana</PageTitle>
      <WordDrill basePath="/transcription" promptLang="ja" answerLang="ja" />
    </main>
  )
}
