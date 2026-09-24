/** Thin fetch wrapper for the Freito API — same-origin in production (technical-plan.md §1),
 * proxied through Vite in dev (see vite.config.ts). Throws ApiError with the server's
 * `errors` array (or its plain-string/ProblemDetails body) when present so callers can show
 * field-level validation messages. */
export class ApiError extends Error {
  status: number
  errors: string[]

  constructor(status: number, message: string, errors: string[] = []) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init?.headers } : init?.headers,
  })

  if (!res.ok) {
    let body: unknown = null
    try {
      body = await res.json()
    } catch {
      // non-JSON error body — body stays null, message falls back below
    }

    if (typeof body === 'string') throw new ApiError(res.status, body, [body])
    if (body && typeof body === 'object') {
      const obj = body as { errors?: unknown; title?: string; detail?: string }
      const errors = Array.isArray(obj.errors) ? (obj.errors as string[]) : []
      const message = errors[0] ?? obj.detail ?? obj.title ?? `Request failed with status ${res.status}`
      throw new ApiError(res.status, message, errors)
    }

    throw new ApiError(res.status, `Request failed with status ${res.status}`)
  }

  if (res.status === 204) return undefined as T
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
