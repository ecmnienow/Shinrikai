import { useEffect, useRef, useState, type MouseEvent, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { ContentGroup, ContentListItem } from '../api/types'
import { useAuth } from '../auth/AuthContext'

/** Tempo de leitura para declarar conhecimento (setTimeout de 20000 ms do original). */
export const READING_TIME_MS = 20_000

/**
 * Estrutura comum de Views/Grammar/Index.cshtml e Views/Text/Index.cshtml: menu por nível à
 * esquerda, conteúdo à direita, Anterior/Próximo (pulando os bloqueados), "Exercícios" e o
 * "contador de tempo" que declara o conhecimento após 20 s no mesmo conteúdo.
 */
export function ContentReader({ title, help, groups, emptyText, loadItem, onLocked, onReadLongEnough, exercisesLink, showExercises, editLink, onContentClick }: {
  title: string
  help: ReactNode
  groups: ContentGroup[]
  emptyText: string
  loadItem: (id: number) => Promise<{ title: string; body: string }>
  onLocked: (item: ContentListItem) => void
  onReadLongEnough: (id: number) => void
  exercisesLink: (id: number) => string
  /** Gramática: só se houver exercícios; Texto: sempre (como no original). */
  showExercises: (item: ContentListItem) => boolean
  editLink: (id: number) => string
  onContentClick?: (e: MouseEvent<HTMLDivElement>) => void
}) {
  const { user } = useAuth()
  const isEditor = user?.role === 'Administrator' || user?.role === 'Teacher'
  const [open, setOpen] = useState<Record<string, boolean>>({})
  const [active, setActive] = useState<ContentListItem | null>(null)
  const [content, setContent] = useState<{ title: string; body: string } | null>(null)
  const [error, setError] = useState<string | null>(null)
  const timer = useRef<number | undefined>(undefined)

  useEffect(() => () => window.clearTimeout(timer.current), [])

  const flat = groups.flatMap((g) => g.items)

  async function select(item: ContentListItem) {
    if (!item.hasEnoughLevel) {
      onLocked(item)
      return
    }
    setActive(item)
    setOpen((o) => ({ ...o, [item.level]: true }))
    setError(null)
    try {
      setContent(await loadItem(item.id))
      // TriggerTimeOut: reinicia a cada troca de conteúdo.
      window.clearTimeout(timer.current)
      timer.current = window.setTimeout(() => onReadLongEnough(item.id), READING_TIME_MS)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Falha ao carregar.')
    }
  }

  function move(delta: number) {
    if (!active) return
    let i = flat.findIndex((x) => x.id === active.id) + delta
    while (i >= 0 && i < flat.length) {
      if (flat[i].hasEnoughLevel) {
        select(flat[i])
        return
      }
      i += delta
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
                    {isEditor && (
                      <Link className="icon edit" to={editLink(item.id)} title="Editar" aria-label={`Editar ${item.title}`}>✎</Link>
                    )}
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
        <div className="page-title">
          <h1>{content?.title ?? title}</h1>
          {help}
        </div>
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
