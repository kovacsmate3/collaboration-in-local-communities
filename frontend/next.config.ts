import type { NextConfig } from "next"
import { dirname } from "node:path"
import { fileURLToPath } from "node:url"

const projectRoot = dirname(fileURLToPath(import.meta.url))

const nextConfig: NextConfig = {
  output: "standalone",
  turbopack: {
    root: projectRoot,
  },
  async rewrites() {
    const backendUrl = process.env.API_URL ?? "http://localhost:5073"
    return [
      {
        source: "/hubs/:path*",
        destination: `${backendUrl}/hubs/:path*`,
      },
    ]
  },
  async redirects() {
    return [
      // The seeker/helper feeds were merged into a single canonical /feed.
      // Keep /helper bookmarks working with a permanent (308) redirect.
      {
        source: "/helper",
        destination: "/feed",
        permanent: true,
      },
      {
        source: "/helper/:path*",
        destination: "/feed",
        permanent: true,
      },
    ]
  },
}

export default nextConfig
