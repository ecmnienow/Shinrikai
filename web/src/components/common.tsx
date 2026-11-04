import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { LEVELS, type Jlpt } from '../api/types'
import { useAuth } from '../auth/AuthContext'

export function PageTitle({ children, help }: { children: ReactNode; help?: ReactNode }) {
  return (
    <div className="page-title">
      <h1>{children}</h1>
      {help}
    </div>
  )
}

/** Radios "Nenhum / N5 … N1" usados nos cadastros ("Nível aconselhado"). */
export function LevelRadios({ value, onChange, name }: { value: Jlpt; onChange: (level: Jlpt) => void; name: string }) {
  return (
    <div className="row" role="radiogroup">
      {(['None', ...LEVELS] as Jlpt[]).map((l) => (
        <label key={l} className="inline">
          <input type="radio" name={name} checked={value === l} onChange={() => onChange(l)} /> {l === 'None' ? 'Nenhum' : l}
        </label>
      ))}
    </div>
  )
}

/** Campo de arquivo com o nome escolhido ou "[Imagem]"/"[Audio]" quando já existe no banco. */
export function FileField({ label, accept, file, existing, existingLabel, onChange }: {
  label: string
  accept: string
  file: File | null
  existing: boolean
  existingLabel: string
  onChange: (f: File | null) => void
}) {
  return (
    <label>
      {label}
      <div className="row">
        <input type="file" accept={accept} onChange={(e) => onChange(e.target.files?.[0] ?? null)} />
        <span className="muted">{file ? file.name : existing ? existingLabel : ''}</span>
      </div>
    </label>
  )
}

/** Atalho "✎" para o cadastro, visível só para professores e administradores. */
export function EditLink({ to, label, className = 'icon edit' }: { to: string; label: string; className?: string }) {
  const { isEditor } = useAuth()
  return isEditor ? <Link className={className} to={to} title="Editar" aria-label={label}>✎</Link> : null
}

/** Toca um áudio (PlayAUdio do dicionário). */
export function playAudio(url: string) {
  new Audio(url).play().catch(() => {})
}

/** Embaralha (ShuffleElements do site.js). */
export function shuffle<T>(items: T[]): T[] {
  const copy = [...items]
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[copy[i], copy[j]] = [copy[j], copy[i]]
  }
  return copy
}
