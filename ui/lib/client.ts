import createClient from "openapi-fetch";
import type { paths } from "./generated/schema";

// Typed analytics client generated from the .NET server's OpenAPI document.
// baseUrl is relative: requests hit /api/analytics/* on the dashboard's own origin and
// are forwarded to the .NET server by the Next.js proxy route
// (app/api/analytics/[endpoint]/route.ts), which keeps the backend URL server-side.
export const apiClient = createClient<paths>({ baseUrl: "" });
