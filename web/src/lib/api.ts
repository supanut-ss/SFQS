/** Thin fetch wrapper for the Freito API — same-origin in production (technical-plan.md §1),
 * proxied through Vite in dev (see vite.config.ts). Throws ApiError with the server's
 * `errors` array when present so callers can show field-level validation messages. */
export class ApiError extends Error {
  status: number
  errors: string[]

  constructor(status: number, errors: string[]) {
    super(errors.join(', ') || `Request failed with status ${status}`)
    this.status = status
    this.errors = errors
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })

  if (!res.ok) {
    let errors: string[] = []
    try {
      const body = await res.json()
      errors = Array.isArray(body?.errors) ? body.errors : body?.title ? [body.title] : []
    } catch {
      // non-JSON error body — fall through with an empty errors list
    }
    throw new ApiError(res.status, errors)
  }

  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body: body ? JSON.stringify(body) : undefined }),
}
