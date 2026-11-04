import { useEffect, useState, type ReactNode } from 'react'
import type { Jlpt } from '../api/types'

export function PageTitle({ children, help }: { children: ReactNode; help?: ReactNode }) {
  return (
    <div className="page-title">
      <h1>{children}</h1>
      {help}
    </div>
  )
}

/** Radios "Nenhum / N5 … N1" usados nos cadastros ("Nível aconselhado"). */
export function LevelRadios({ value, onChange, name, includeNone = true }: {
  value: Jlpt
  onChange: (level: Jlpt) => void
  name: string
  includeNone?: boolean
}) {
  const options: Jlpt[] = includeNone ? ['None', 'N5', 'N4', 'N3', 'N2', 'N1'] : ['N5', 'N4', 'N3', 'N2', 'N1']
  return (
    <div className="row" role="radiogroup">
      {options.map((l) => (
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

/** Toca um áudio (PlayAUdio do dicionário). */
export function playAudio(url: string) {
  const audio = new Audio(url)
  audio.play().catch(() => {})
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

/** Busca autenticada de um blob (imagem/áudio) → object URL. A API exige o token JWT. */
export function useBlobUrl(path: string | null) {
  const [url, setUrl] = useState<string | null>(null)
  useEffect(() => {
    if (!path) return
    let revoked = false
    let objectUrl: string | null = null
    fetchBlobUrl(path).then((u) => {
      if (revoked) {
        if (u) URL.revokeObjectURL(u)
        return
      }
      objectUrl = u
      setUrl(u)
    })
    return () => {
      revoked = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [path])
  return url
}

export async function fetchBlobUrl(path: string): Promise<string | null> {
  const { getToken } = await import('../api/client')
  const token = getToken()
  const res = await fetch(`/api${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
  if (!res.ok) return null
  return URL.createObjectURL(await res.blob())
}
