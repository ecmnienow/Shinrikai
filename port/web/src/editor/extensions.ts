import { Node } from '@tiptap/core'

/**
 * Extensões TipTap que substituem as convenções manuais do CKEditor 4 original.
 *
 * Por que *nodes* e não *marks*: um mark só anota um trecho de texto, mas <ruby> tem
 * dois textos diferentes (a base 漢字 e a leitura かんじ em <rt>). No ProseMirror isso é
 * modelado como um nó inline atômico com atributos { base, reading } — o HTML gerado é
 * exatamente o que o backend espera.
 */

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    furigana: {
      /** Transforma a seleção em <ruby><rb>seleção</rb><rt>reading</rt></ruby>. */
      setFurigana: (reading: string) => ReturnType
    }
    vocabularyWord: {
      /** Transforma a seleção em palavra ligada ao vocabulário (stretchtext). */
      setVocabularyWord: (attrs: { wordId: number; reading: string }) => ReturnType
    }
  }
}

function rubyParts(el: HTMLElement) {
  const ruby = el.tagName === 'RUBY' ? el : el.querySelector('ruby')
  if (!ruby) return { base: el.textContent ?? '', reading: '' }
  const rt = ruby.querySelector('rt')
  const rb = ruby.querySelector('rb')
  // <ruby>漢字<rt>かんじ</rt></ruby> (sem <rb>) também é HTML válido.
  const base = rb?.textContent ?? Array.from(ruby.childNodes)
    .filter((n) => !(n instanceof HTMLElement && (n.tagName === 'RT' || n.tagName === 'RP')))
    .map((n) => n.textContent)
    .join('')
  return { base, reading: rt?.textContent ?? '' }
}

/** Furigana: substitui a notação manual 漢字「かんじ」 (TextManager.GetText). */
export const Furigana = Node.create({
  name: 'furigana',
  group: 'inline',
  inline: true,
  atom: true,
  selectable: true,

  addAttributes() {
    return {
      base: { default: '', rendered: false },
      reading: { default: '', rendered: false },
    }
  },

  parseHTML() {
    return [{ tag: 'ruby', getAttrs: (el) => rubyParts(el as HTMLElement) }]
  },

  renderHTML({ node }) {
    return ['ruby', ['rb', node.attrs.base], ['rt', node.attrs.reading]]
  },

  renderText({ node }) {
    return node.attrs.base
  },

  addCommands() {
    return {
      setFurigana:
        (reading) =>
        ({ state, chain }) => {
          const { from, to, empty } = state.selection
          const base = state.doc.textBetween(from, to, '')
          if (empty || !base.trim() || !reading.trim()) return false
          return chain()
            .insertContentAt({ from, to }, { type: this.name, attrs: { base, reading: reading.trim() } })
            .run()
        },
    }
  },
})

/**
 * Palavra ligada ao vocabulário. Gera exatamente o HTML que TextController.GetText procura:
 *   <span class="stretchText" data-id="N"><ruby><rb>猫</rb><rt>ねこ</rt></ruby></span>
 * (antes, isso tinha de ser digitado à mão no modo "código-fonte" do CKEditor).
 */
export const VocabularyWord = Node.create({
  name: 'vocabularyWord',
  group: 'inline',
  inline: true,
  atom: true,
  selectable: true,
  priority: 1000, // antes de Furigana: o <ruby> interno pertence a este nó

  addAttributes() {
    return {
      wordId: { default: null, rendered: false },
      base: { default: '', rendered: false },
      reading: { default: '', rendered: false },
    }
  },

  parseHTML() {
    return [
      {
        tag: 'span.stretchText',
        getAttrs: (el) => {
          const span = el as HTMLElement
          const id = Number(span.dataset.id)
          if (!Number.isInteger(id) || id <= 0) return false
          return { wordId: id, ...rubyParts(span) }
        },
      },
    ]
  },

  renderHTML({ node }) {
    return [
      'span',
      { class: 'stretchText', 'data-id': String(node.attrs.wordId) },
      ['ruby', ['rb', node.attrs.base], ['rt', node.attrs.reading]],
    ]
  },

  renderText({ node }) {
    return node.attrs.base
  },

  addCommands() {
    return {
      setVocabularyWord:
        ({ wordId, reading }) =>
        ({ state, chain }) => {
          const { from, to, empty } = state.selection
          // Se a seleção já é um furigana, reaproveita a base dele.
          const selected = state.doc.nodeAt(from)
          const base =
            selected && selected.type.name === 'furigana' && to - from === selected.nodeSize
              ? (selected.attrs.base as string)
              : state.doc.textBetween(from, to, '')
          if (empty || !base.trim() || !Number.isInteger(wordId) || wordId <= 0) return false
          return chain()
            .insertContentAt({ from, to }, { type: this.name, attrs: { wordId, base, reading } })
            .run()
        },
    }
  },
})

/** Mesma regex do original — converte textos antigos (漢字「かんじ」) ao abrir no editor. */
const LEGACY_NOTATION = /([一-龠]+「[ぁ-ゔァ-ヴー々〆〤]+」)/g

export function convertLegacyFurigana(html: string): string {
  return html.replace(LEGACY_NOTATION, (value) => {
    const open = value.indexOf('「')
    return `<ruby><rb>${value.slice(0, open)}</rb><rt>${value.slice(open + 1, -1)}</rt></ruby>`
  })
}
