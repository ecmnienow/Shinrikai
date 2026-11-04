/**
 * Verificação de fidelidade: o editor visual (ProseMirror) normaliza o HTML e descarta o que o
 * esquema não conhece (ex.: <font>, <span style="font-size">, atributos desconhecidos). Para que
 * nada se perca, comparamos um "censo" do HTML original com o que o editor produziria. Se algo
 * sumiria, o cadastro abre no modo HTML (código-fonte), que preserva o conteúdo byte a byte.
 */

const IGNORED_TAGS = new Set(['TBODY', 'COLGROUP', 'COL'])

interface Census {
  tags: Map<string, number>
  attributes: Map<string, number>
  text: string
}

function census(html: string): Census {
  const doc = new DOMParser().parseFromString(`<body>${html}</body>`, 'text/html')
  const tags = new Map<string, number>()
  const attributes = new Map<string, number>()
  doc.body.querySelectorAll('*').forEach((el) => {
    if (IGNORED_TAGS.has(el.tagName)) return
    tags.set(el.tagName, (tags.get(el.tagName) ?? 0) + 1)
    for (const attr of Array.from(el.attributes)) {
      const key = `${el.tagName}[${attr.name}]`
      attributes.set(key, (attributes.get(key) ?? 0) + 1)
    }
  })
  const text = (doc.body.textContent ?? '').replace(/\s+/g, '')
  return { tags, attributes, text }
}

/** Equivalências que o editor introduz sem perder informação. */
const EQUIVALENT: Record<string, string> = { B: 'STRONG', I: 'EM', STRIKE: 'S', DEL: 'S' }

export interface FidelityReport {
  lossless: boolean
  lost: string[]
}

export function checkFidelity(original: string, normalized: string): FidelityReport {
  if (!original.trim()) return { lossless: true, lost: [] }

  const a = census(original)
  const b = census(normalized)
  const lost: string[] = []

  if (a.text !== b.text) lost.push('texto')

  const normalizedTags = new Map(b.tags)
  for (const [tag, count] of a.tags) {
    const target = EQUIVALENT[tag] ?? tag
    const available = normalizedTags.get(target) ?? 0
    if (available < count) lost.push(`<${tag.toLowerCase()}>`)
    normalizedTags.set(target, available - count)
  }

  for (const [attr, count] of a.attributes) {
    const [tag, rest] = attr.split('[')
    const target = `${EQUIVALENT[tag] ?? tag}[${rest}`
    if ((b.attributes.get(target) ?? 0) < count) lost.push(attr.toLowerCase())
  }

  return { lossless: lost.length === 0, lost: [...new Set(lost)] }
}
