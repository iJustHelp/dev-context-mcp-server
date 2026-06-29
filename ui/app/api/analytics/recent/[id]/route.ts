// Server-side proxy for recent-call detail requests.

const BASE_URL =
  process.env.MCP_ANALYTICS_API_BASE_URL ?? "http://127.0.0.1:2222";

export const dynamic = "force-dynamic";

export async function GET(
  request: Request,
  { params }: { params: { id: string } },
) {
  const { id } = params;
  if (!id) {
    return Response.json({ error: "id is required." }, { status: 400 });
  }

  const search = new URL(request.url).search;
  const target = `${BASE_URL}/api/analytics/recent/${encodeURIComponent(id)}${search}`;

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
