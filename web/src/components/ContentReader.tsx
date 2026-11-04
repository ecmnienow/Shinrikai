import { useEffect, useRef, useState, type MouseEvent, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { api, errorMessage, fire } from '../api/client'
import type { ContentGroup, ContentListItem } from '../api/types'
import { EditLink, PageTitle } from './common'

/** Tempo de leitura para declarar conhecimento (setTimeout de 20000 ms do original). */
export const READING_TIME_MS = 20_000

/**
 * Estrutura comum de Views/Grammar/Index.cshtml e Views/Text/Index.cshtml: menu por nível à
 * esquerda, conteúdo à direita, Anterior/Próximo (pulando os bloqueados), "Exercícios" e o
 * "contador de tempo" que declara o conhecimento após 20 s no mesmo conteúdo.
 */
export function ContentReader({ basePath, title, help, emptyText, onLocked, exercisesLink, showExercises, editLink, onContentClick }: {
  /** "/grammar" ou "/texts": lista em GET {basePath}, conteúdo em GET {basePath}/{id}, conhecimento em POST {basePath}/{id}/knowledge. */
  basePath: string
  title: string
  help: ReactNode
  emptyText: string
  onLocked?: (item: ContentListItem) => void
  exercisesLink: (id: number) => string
  /** Gramática: só se houver exercícios; Texto: sempre (como no original). */
  showExercises: (item: ContentListItem) => boolean
  editLink: (id: number) => string
  onContentClick?: (e: MouseEvent<HTMLDivElement>) => void
}) {
  const [groups, setGroups] = useState<ContentGroup[]>([])
  const [open, setOpen] = useState<Record<string, boolean>>({})
  const [active, setActive] = useState<ContentListItem | null>(null)
  const [content, setContent] = useState<{ title: string; body: string } | null>(null)
  const [error, setError] = useState<string | null>(null)
  const timer = useRef<number | undefined>(undefined)

  useEffect(() => {
    api<ContentGroup[]>(basePath).then(setGroups).catch((e) => setError(e.message))
  }, [basePath])

  useEffect(() => () => window.clearTimeout(timer.current), [])

  const flat = groups.flatMap((g) => g.items)

  async function select(item: ContentListItem) {
    if (!item.hasEnoughLevel) {
      onLocked?.(item)
      return
    }
    setActive(item)
    setOpen((o) => ({ ...o, [item.level]: true }))
    setError(null)
    try {
      setContent(await api<{ title: string; body: string }>(`${basePath}/${item.id}`))
      // TriggerTimeOut: reinicia a cada troca de conteúdo.
      window.clearTimeout(timer.current)
      timer.current = window.setTimeout(() => fire(`${basePath}/${item.id}/knowledge`), READING_TIME_MS)
    } catch (e) {
      setError(errorMessage(e, 'Falha ao carregar.'))
    }
  }

  function move(delta: number) {
    if (!active) return
    for (let i = flat.findIndex((x) => x.id === active.id) + delta; i >= 0 && i < flat.length; i += delta) {
      if (flat[i].hasEnoughLevel) {
        select(flat[i])
        return
      }
    }
  }

  return (
    <main className="reader">
      <aside className="reader-menu" aria-label="Conteúdos">
        {groups.map((g) => (
          <div key={g.level} className="level-group">
            <button type="button" className="level-toggle" aria-expanded={!!open[g.level]} onClick={() => setOpen((o) => ({ ...o, [g.level]: !o[g.level] }))}>
              {g.level}
            </button>
            {open[g.level] && (
              <ul>
                {g.items.map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      className={`content-item ${active?.id === item.id ? 'active' : ''} ${item.hasEnoughLevel ? '' : 'locked'}`}
                      onClick={() => select(item)}
                      title={item.hasEnoughLevel ? undefined : 'Bloqueado'}
                    >
                      {!item.hasEnoughLevel && <span aria-hidden="true">🔒 </span>}
                      {item.title}
                    </button>
                    <EditLink to={editLink(item.id)} label={`Editar ${item.title}`} />
                  </li>
                ))}
              </ul>
            )}
          </div>
        ))}
        {active && showExercises(active) && (
          <Link className="button primary block" to={exercisesLink(active.id)}>Exercícios</Link>
        )}
      </aside>

      <section className="reader-content">
        <PageTitle help={help}>{content?.title ?? title}</PageTitle>
        {error && <p className="error">{error}</p>}
        {content ? (
          <div className="card reading" lang="ja" onClick={onContentClick} dangerouslySetInnerHTML={{ __html: content.body }} />
        ) : (
          <p className="muted">{groups.length === 0 ? 'Nenhum conteúdo cadastrado.' : emptyText}</p>
        )}
        <nav className="row center">
          <button type="button" onClick={() => move(-1)} disabled={!active}>Anterior</button>
          <button type="button" onClick={() => move(1)} disabled={!active}>Próximo</button>
        </nav>
      </section>
    </main>
  )
}
