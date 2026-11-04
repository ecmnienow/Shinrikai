import { useState } from 'react'
import { ContentForm, useContentForm } from '../../components/ContentForm'
import { useDialogs } from '../../components/Dialogs'
import { RichEditor } from '../../editor/RichEditor'
import { VocabularyPicker } from '../../editor/VocabularyPicker'

type Pick = { wordId: number; reading: string } | null

/**
 * Port de Views/Text/Register.cshtml. O CKEditor original não tinha ferramenta para furigana nem
 * para ligar palavras ao vocabulário (era notação manual 漢字「かんじ」 e HTML cru); aqui são os
 * botões 字 Furigana e 🔗 Vocabulário. Textos antigos com a notação são convertidos ao abrir.
 */
export function TextRegisterPage() {
  const dialogs = useDialogs()
  const content = useContentForm({
    basePath: '/texts', listRoute: '/textos', noun: 'texto', exerciseParam: 'textId', exerciseTarget: 'ao texto',
  })
  const [picker, setPicker] = useState<{ term: string; resolve: (p: Pick) => void } | null>(null)

  return (
    <main>
      <ContentForm heading="Cadastro de Textos" content={content} name="textLevel" onRegister={() => content.validate() && content.save()}>
        <RichEditor
          initialHtml={content.initialBody ?? ''}
          onChange={(body) => content.set({ body })}
          japaneseTools
          askText={(l, i) => dialogs.prompt(l, i)}
          pickVocabularyWord={(selection) => new Promise<Pick>((resolve) => setPicker({ term: selection, resolve }))}
        />
      </ContentForm>

      {picker && (
        <VocabularyPicker
          initialTerm={picker.term}
          onDone={(p) => {
            picker.resolve(p)
            setPicker(null)
          }}
        />
      )}
    </main>
  )
}
