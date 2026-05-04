const DEFAULT_API_URL = "http://localhost:5073"

type ApiRouteContext = {
  params: Promise<{
    path: string[]
  }>
}

function getBackendUrl(path: string[], requestUrl: string): URL {
  const backendBaseUrl = process.env.API_URL ?? DEFAULT_API_URL
  const incomingUrl = new URL(requestUrl)
  const backendUrl = new URL(`/api/${path.join("/")}`, backendBaseUrl)

  backendUrl.search = incomingUrl.search

  return backendUrl
}

async function proxyRequest(
  request: Request,
  context: ApiRouteContext
): Promise<Response> {
  const { path } = await context.params
  const backendUrl = getBackendUrl(path, request.url)
  const proxyRequest = new Request(backendUrl, request)

  return fetch(proxyRequest)
}

export const GET = proxyRequest
export const POST = proxyRequest
export const PUT = proxyRequest
export const PATCH = proxyRequest
export const DELETE = proxyRequest
export const HEAD = proxyRequest
export const OPTIONS = proxyRequest
