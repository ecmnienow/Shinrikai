// Cliente HTTP fino para a API .NET. O token JWT fica em memória + sessionStorage.

const TOKEN_KEY = 'shinrikai.token'

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
  }
}

export function getToken(): string | null {
  try {
    return sessionStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function setToken(token: string | null) {
  try {
    if (token) sessionStorage.setItem(TOKEN_KEY, token)
    else sessionStorage.removeItem(TOKEN_KEY)
  } catch {
    /* storage indisponível: segue só em memória */
  }
}

let onUnauthorized: (() => void) | null = null
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

function authFetch(path: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers)
  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  return fetch(`/api${path}`, { ...init, headers })
}

export async function api<T>(path: string, init: RequestInit & { json?: unknown } = {}): Promise<T> {
  const { json, ...rest } = init
  if (json !== undefined) {
    const headers = new Headers(rest.headers)
    headers.set('Content-Type', 'application/json')
    Object.assign(rest, { headers, body: JSON.stringify(json) })
  }

  const res = await authFetch(path, rest)

  if (res.status === 401 && getToken()) {
    setToken(null)
    onUnauthorized?.()
  }

  if (!res.ok) {
    let message = res.statusText
    try {
      const data = await res.json()
      // Response { ok, message } das regras de negócio, ou ProblemDetails { title, detail } de erros.
      message = data.message ?? [data.title, data.detail].filter(Boolean).join('\n') ?? message
    } catch {
      /* corpo vazio */
    }
    throw new ApiError(res.status, message)
  }

  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

/** POST com corpo JSON (ou FormData). */
export const post = <T = void>(path: string, body?: unknown) =>
  api<T>(path, body instanceof FormData ? { method: 'POST', body } : { method: 'POST', json: body })

/** POST cujo resultado não importa (registro de interação, conhecimento declarado): falhas são ignoradas. */
export const fire = (path: string, body?: unknown) => {
  post(path, body).catch(() => {})
}

export const errorMessage = (e: unknown, fallback: string) => (e instanceof Error ? e.message : fallback)

export const isNotFound = (e: unknown) => e instanceof ApiError && e.status === 404

/** Busca autenticada de um blob (imagem/áudio) → object URL. A API exige o token JWT. */
export async function fetchBlobUrl(path: string): Promise<string | null> {
  const res = await authFetch(path)
  return res.ok ? URL.createObjectURL(await res.blob()) : null
}
