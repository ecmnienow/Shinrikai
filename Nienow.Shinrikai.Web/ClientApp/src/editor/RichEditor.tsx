import Color from '@tiptap/extension-color'
import Highlight from '@tiptap/extension-highlight'
import Image from '@tiptap/extension-image'
import Link from '@tiptap/extension-link'
import Subscript from '@tiptap/extension-subscript'
import Superscript from '@tiptap/extension-superscript'
import Table from '@tiptap/extension-table'
import TableCell from '@tiptap/extension-table-cell'
import TableHeader from '@tiptap/extension-table-header'
import TableRow from '@tiptap/extension-table-row'
import TextAlign from '@tiptap/extension-text-align'
import TextStyle from '@tiptap/extension-text-style'
import Underline from '@tiptap/extension-underline'
import { EditorContent, useEditor, type Editor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { useEffect, useMemo, useRef, useState } from 'react'
import { convertLegacyFurigana, Furigana, VocabularyWord } from './extensions'
import { checkFidelity } from './fidelity'

export interface RichEditorHandle {
  getHTML: () => string
}

interface Props {
  /** HTML inicial (de um cadastro existente). */
  initialHtml: string
  onChange: (html: string) => void
  /** Botões 字 Furigana e 🔗 Vocabulário (cadastro de textos). */
  japaneseTools?: boolean
  /** Resolve a palavra do vocabulário para o link (abre o diálogo de busca). */
  pickVocabularyWord?: (selection: string) => Promise<{ wordId: number; reading: string } | null>
  askText: (label: string, initial?: string) => Promise<string | null>
}

const EXTENSIONS = [
  StarterKit,
  Underline,
  Subscript,
  Superscript,
  TextStyle,
  Color,
  Highlight.configure({ multicolor: true }),
  Link.configure({ openOnClick: false, autolink: true }),
  Image.configure({ inline: true, allowBase64: true }),
  Table.configure({ resizable: false }),
  TableRow,
  TableHeader,
  TableCell,
  TextAlign.configure({ types: ['heading', 'paragraph'] }),
  Furigana,
  VocabularyWord,
]

/**
 * Editor dos cadastros de Gramática e Texto (substitui o CKEditor 4).
 * - Modo visual (TipTap) com a formatação do CKEditor padrão + furigana/vocabulário.
 * - Modo HTML (o "Código-fonte" do CKEditor): edita o HTML cru, sem normalização.
 * - Ao abrir um conteúdo existente, se o modo visual fosse descartar algo, abre direto em HTML
 *   e avisa — o conteúdo salvo é exatamente o que estava no banco, a menos que seja editado.
 */
export function RichEditor({ initialHtml, onChange, japaneseTools, pickVocabularyWord, askText }: Props) {
  const source = useMemo(() => convertLegacyFurigana(initialHtml), [initialHtml])
  const [mode, setMode] = useState<'visual' | 'html'>('visual')
  const [html, setHtml] = useState(source)
  const [lost, setLost] = useState<string[]>([])
  const [, force] = useState(0)
  const checked = useRef(false)

  const editor = useEditor({
    extensions: EXTENSIONS,
    content: source,
    onUpdate: ({ editor }) => {
      const out = editor.getHTML()
      setHtml(out)
      onChange(out)
    },
    onSelectionUpdate: () => force((n) => n + 1),
  })

  // Verificação de fidelidade do conteúdo carregado.
  useEffect(() => {
    if (!editor || checked.current) return
    checked.current = true
    const report = checkFidelity(source, editor.getHTML())
    if (!report.lossless) {
      setLost(report.lost)
      setMode('html')
      setHtml(source)
      onChange(source)
    } else {
      onChange(source.trim() ? editor.getHTML() : '')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editor, source])

  if (!editor) return null

  function toVisual() {
    const report = checkFidelity(html, (() => {
      const probe = editor!.getHTML()
      editor!.commands.setContent(html, false)
      const normalized = editor!.getHTML()
      editor!.commands.setContent(probe, false)
      return normalized
    })())
    if (!report.lossless && !window.confirm(`O editor visual vai descartar: ${report.lost.join(', ')}.\nContinuar mesmo assim?`)) return
    editor!.commands.setContent(html, true)
    setLost([])
    setMode('visual')
  }

  function toHtml() {
    setHtml(editor!.getHTML())
    setMode('html')
  }

  return (
    <div className="editor">
      <div className="toolbar" role="toolbar" aria-label="Formatação">
        {mode === 'visual' ? (
          <VisualToolbar editor={editor} japaneseTools={japaneseTools} pickVocabularyWord={pickVocabularyWord} askText={askText} />
        ) : (
          <span className="muted">Modo HTML (código-fonte)</span>
        )}
        <span className="spacer" />
        {mode === 'visual'
          ? <button type="button" onClick={toHtml} title="Editar o HTML (código-fonte)">&lt;/&gt; HTML</button>
          : <button type="button" onClick={toVisual}>Editor visual</button>}
      </div>
      {lost.length > 0 && mode === 'html' && (
        <p className="notice warn">
          Este conteúdo tem formatação que o editor visual não suporta ({lost.join(', ')}). Ele foi aberto em
          modo HTML para não perder nada.
        </p>
      )}
      {mode === 'visual' ? (
        <EditorContent editor={editor} className="editor-content" lang="ja" />
      ) : (
        <textarea
          className="html-source"
          spellCheck={false}
          value={html}
          aria-label="Código-fonte HTML"
          onChange={(e) => {
            setHtml(e.target.value)
            onChange(e.target.value)
          }}
        />
      )}
    </div>
  )
}

function VisualToolbar({ editor, japaneseTools, pickVocabularyWord, askText }: {
  editor: Editor
  japaneseTools?: boolean
  pickVocabularyWord?: Props['pickVocabularyWord']
  askText: Props['askText']
}) {
  const chain = () => editor.chain().focus()
  const hasSelection = !editor.state.selection.empty
  const btn = (label: string, title: string, active: boolean, run: () => void, disabled = false) => (
    <button type="button" title={title} aria-label={title} aria-pressed={active} onClick={run} disabled={disabled}>{label}</button>
  )

  async function link() {
    const previous = editor.getAttributes('link').href as string | undefined
    const href = await askText('Endereço do link (vazio para remover):', previous ?? 'https://')
    if (href === null) return
    if (!href.trim()) chain().unsetLink().run()
    else chain().extendMarkRange('link').setLink({ href: href.trim() }).run()
  }

  async function image() {
    const src = await askText('Endereço da imagem:', 'https://')
    if (src?.trim()) chain().setImage({ src: src.trim() }).run()
  }

  async function color() {
    const value = await askText('Cor do texto (ex.: #b3261e; vazio para remover):', (editor.getAttributes('textStyle').color as string) ?? '')
    if (value === null) return
    if (!value.trim()) chain().unsetColor().run()
    else chain().setColor(value.trim()).run()
  }

  async function furigana() {
    const reading = await askText('Leitura (furigana) em kana:')
    if (reading?.trim()) chain().setFurigana(reading).run()
  }

  async function vocabulary() {
    if (!pickVocabularyWord) return
    const { from, to } = editor.state.selection
    const picked = await pickVocabularyWord(editor.state.doc.textBetween(from, to, ''))
    if (picked) editor.chain().focus().setTextSelection({ from, to }).setVocabularyWord(picked).run()
  }

  return (
    <>
      {btn('B', 'Negrito', editor.isActive('bold'), () => chain().toggleBold().run())}
      {btn('I', 'Itálico', editor.isActive('italic'), () => chain().toggleItalic().run())}
      {btn('U', 'Sublinhado', editor.isActive('underline'), () => chain().toggleUnderline().run())}
      {btn('S', 'Tachado', editor.isActive('strike'), () => chain().toggleStrike().run())}
      {btn('x₂', 'Subscrito', editor.isActive('subscript'), () => chain().toggleSubscript().run())}
      {btn('x²', 'Sobrescrito', editor.isActive('superscript'), () => chain().toggleSuperscript().run())}
      {btn('A', 'Cor do texto', !!editor.getAttributes('textStyle').color, color)}
      {btn('▮', 'Marca-texto', editor.isActive('highlight'), () => chain().toggleHighlight().run())}
      {btn('⌫', 'Remover formatação', false, () => chain().unsetAllMarks().clearNodes().run())}
      <span className="sep" />
      <select
        aria-label="Estilo do parágrafo"
        value={[1, 2, 3, 4].find((l) => editor.isActive('heading', { level: l })) ?? 0}
        onChange={(e) => {
          const level = Number(e.target.value)
          if (level === 0) chain().setParagraph().run()
          else chain().toggleHeading({ level: level as 1 | 2 | 3 | 4 }).run()
        }}
      >
        <option value={0}>Normal</option>
        <option value={1}>Título 1</option>
        <option value={2}>Título 2</option>
        <option value={3}>Título 3</option>
        <option value={4}>Título 4</option>
      </select>
      {btn('• ', 'Lista', editor.isActive('bulletList'), () => chain().toggleBulletList().run())}
      {btn('1.', 'Lista numerada', editor.isActive('orderedList'), () => chain().toggleOrderedList().run())}
      {btn('❝', 'Citação', editor.isActive('blockquote'), () => chain().toggleBlockquote().run())}
      {btn('⇤', 'Alinhar à esquerda', editor.isActive({ textAlign: 'left' }), () => chain().setTextAlign('left').run())}
      {btn('⇔', 'Centralizar', editor.isActive({ textAlign: 'center' }), () => chain().setTextAlign('center').run())}
      {btn('⇥', 'Alinhar à direita', editor.isActive({ textAlign: 'right' }), () => chain().setTextAlign('right').run())}
      {btn('☰', 'Justificar', editor.isActive({ textAlign: 'justify' }), () => chain().setTextAlign('justify').run())}
      <span className="sep" />
      {btn('🔗', 'Link', editor.isActive('link'), link)}
      {btn('🖼', 'Imagem', false, image)}
      {btn('▦', 'Inserir tabela', false, () => chain().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run())}
      {editor.isActive('table') && (
        <>
          {btn('+col', 'Adicionar coluna', false, () => chain().addColumnAfter().run())}
          {btn('+lin', 'Adicionar linha', false, () => chain().addRowAfter().run())}
          {btn('−col', 'Remover coluna', false, () => chain().deleteColumn().run())}
          {btn('−lin', 'Remover linha', false, () => chain().deleteRow().run())}
          {btn('✕▦', 'Remover tabela', false, () => chain().deleteTable().run())}
        </>
      )}
      {btn('―', 'Linha horizontal', false, () => chain().setHorizontalRule().run())}
      {btn('↶', 'Desfazer', false, () => chain().undo().run(), !editor.can().undo())}
      {btn('↷', 'Refazer', false, () => chain().redo().run(), !editor.can().redo())}
      {japaneseTools && (
        <>
          <span className="sep" />
          {btn('字 Furigana', 'Inserir furigana na seleção', false, furigana, !hasSelection)}
          {btn('🔗 Vocabulário', 'Ligar a seleção a uma palavra do vocabulário', false, vocabulary, !hasSelection)}
        </>
      )}
    </>
  )
}
