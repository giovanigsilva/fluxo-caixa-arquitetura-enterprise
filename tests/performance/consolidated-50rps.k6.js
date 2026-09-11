import http from "k6/http"
import { check } from "k6"

export const options = {
  scenarios: {
    consolidated_read_50rps: {
      executor: "constant-arrival-rate",
      rate: 50,
      timeUnit: "1s",
      duration: "10m",
      preAllocatedVUs: 50,
      maxVUs: 120
    }
  },
  thresholds: {
    http_req_failed: ["rate<=0.05"],
    http_req_duration: ["p(95)<=200", "p(99)<=400"]
  }
}

const baseUrl = __ENV.BASE_URL || "http://127.0.0.1:6210"

export default function () {
  const response = http.get(`${baseUrl}/api/consolidated/daily`, {
    headers: {
      "X-Tenant-Id": "org-alpha",
      "X-User-Id": "user-admin-alpha"
    }
  })

  check(response, {
    "status 200": r => r.status === 200,
    "read model returned": r => r.body.includes("read-model")
  })
}
