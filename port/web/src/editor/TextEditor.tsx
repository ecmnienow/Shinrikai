import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { api, ApiError } from '../api/client'
import type { Word } from '../api/types'
import { convertLegacyFurigana, Furigana, VocabularyWord } from './extensions'

interface Props {
  initialHtml?: string
  onChange?: (html: string) => void
}

/** Busca a palavra pelo kana (GET /api/vocabulary/words?kana=). Sem banco, aceita o ID manual. */
async function resolveWord(kana: string): Promise<{ wordId: number; reading: string } | null> {
  try {
    const word = await api<Word>(`/vocabulary/words?kana=${encodeURIComponent(kana)}`)
    return { wordId: word.id, reading: word.kana }
  } catch (e) {
    const reason = e instanceof ApiError && e.status === 404 ? 'não encontrada no vocabulário' : 'não pôde ser buscada'
    const manual = window.prompt(`A palavra "${kana}" ${reason}.\nInforme o ID (VCB_ID) manualmente:`)
    const id = Number(manual)
    return Number.isInteger(id) && id > 0 ? { wordId: id, reading: kana } : null
  }
}

export function TextEditor({ initialHtml = '', onChange }: Props) {
  const editor = useEditor({
    extensions: [StarterKit, Furigana, VocabularyWord],
    content: convertLegacyFurigana(initialHtml),
    onUpdate: ({ editor }) => onChange?.(editor.getHTML()),
    onCreate: ({ editor }) => onChange?.(editor.getHTML()),
  })

  if (!editor) return null

  const hasSelection = !editor.state.selection.empty

  function addFurigana() {
    const reading = window.prompt('Leitura (furigana) em kana:')
    if (reading) editor!.chain().focus().setFurigana(reading).run()
  }

  async function addVocabularyLink() {
    const kana = window.prompt('Kana da palavra no vocabulário (ex.: ねこ):')
    if (!kana) return
    const resolved = await resolveWord(kana.trim())
    if (resolved) editor!.chain().focus().setVocabularyWord(resolved).run()
  }

  return (
    <div className="editor">
      <div className="toolbar" role="toolbar" aria-label="Formatação">
        <button type="button" onClick={() => editor.chain().focus().toggleBold().run()} aria-pressed={editor.isActive('bold')}>
          <b>B</b>
        </button>
        <button type="button" onClick={() => editor.chain().focus().toggleItalic().run()} aria-pressed={editor.isActive('italic')}>
          <i>I</i>
        </button>
        <span className="sep" />
        <button type="button" onClick={addFurigana} disabled={!hasSelection} title="Inserir furigana na seleção">
          字 Furigana
        </button>
        <button type="button" onClick={addVocabularyLink} disabled={!hasSelection} title="Ligar a seleção a uma palavra do vocabulário">
          🔗 Vocabulário
        </button>
      </div>
      <EditorContent editor={editor} className="editor-content" lang="ja" />
    </div>
  )
}
