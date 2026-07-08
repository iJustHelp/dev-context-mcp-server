const BASE_URL =
  process.env.MCP_ANALYTICS_API_BASE_URL ?? "http://127.0.0.1:2222";

export const dynamic = "force-dynamic";

export async function GET() {
  const target = `${BASE_URL}/api/context/last-run`;

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
      { error: `Unable to reach the context API at ${BASE_URL}.` },
      { status: 502 },
    );
  }
}
