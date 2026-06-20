// Server-side proxy: forwards dashboard analytics requests to the .NET MCP server.
// The browser only ever talks to this route, never directly to the .NET host.

const ALLOWED_ENDPOINTS = new Set(["summary", "timeseries", "tools", "recent"]);

const BASE_URL =
  process.env.MCP_ANALYTICS_API_BASE_URL ?? "http://127.0.0.1:2222";

export const dynamic = "force-dynamic";

export async function GET(
  request: Request,
  { params }: { params: { endpoint: string } },
) {
  const { endpoint } = params;
  if (!ALLOWED_ENDPOINTS.has(endpoint)) {
    return Response.json(
      { error: `Unknown analytics endpoint '${endpoint}'.` },
      { status: 404 },
    );
  }

  const search = new URL(request.url).search;
  const target = `${BASE_URL}/api/analytics/${endpoint}${search}`;

  try {
    const upstream = await fetch(target, {
      cache: "no-store",
      headers: { accept: "application/json" },
    });
    const body = await upstream.text();
    return new Response(body, {
      status: upstream.status,
      headers: {
        "content-type":
          upstream.headers.get("content-type") ?? "application/json",
      },
    });
  } catch {
    return Response.json(
      { error: `Unable to reach the analytics API at ${BASE_URL}.` },
      { status: 502 },
    );
  }
}
