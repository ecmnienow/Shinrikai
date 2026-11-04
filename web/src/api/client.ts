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

export async function api<T>(path: string, init: RequestInit & { json?: unknown } = {}): Promise<T> {
  const headers = new Headers(init.headers)
  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let body = init.body
  if (init.json !== undefined) {
    headers.set('Content-Type', 'application/json')
    body = JSON.stringify(init.json)
  }

  const res = await fetch(`/api${path}`, { ...init, headers, body })

  if (res.status === 401 && token) {
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
