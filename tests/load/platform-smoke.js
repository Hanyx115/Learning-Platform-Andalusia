import http from 'k6/http'
import { check, group, sleep } from 'k6'

export const options = {
  vus: Number(__ENV.K6_VUS || 5),
  duration: __ENV.K6_DURATION || '30s',
  thresholds: {
    'http_req_failed{phase:load}': ['rate<0.01'],
    'http_req_duration{phase:load}': ['p(95)<750', 'p(99)<1500'],
    checks: ['rate>0.99'],
  },
}

const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/$/, '')

export function setup() {
  const warmupRequests = [
    `${baseUrl}/health/ready`,
    `${baseUrl}/api/v1/courses?page=1&pageSize=10`,
  ]

  for (const url of warmupRequests) {
    const response = http.get(url, { tags: { phase: 'warmup' } })
    if (response.status !== 200) {
      throw new Error(`Warm-up request failed for ${url} with status ${response.status}.`)
    }
  }
}

export default function platformSmoke() {
  group('health', () => {
    const live = http.get(`${baseUrl}/health/live`, {
      tags: { endpoint: 'health-live', phase: 'load' },
    })
    check(live, { 'liveness is healthy': (response) => response.status === 200 })

    const ready = http.get(`${baseUrl}/health/ready`, {
      tags: { endpoint: 'health-ready', phase: 'load' },
    })
    check(ready, { 'readiness is healthy': (response) => response.status === 200 })
  })

  group('public catalog', () => {
    const courses = http.get(`${baseUrl}/api/v1/courses?page=1&pageSize=10`, {
      tags: { endpoint: 'courses', phase: 'load' },
    })
    check(courses, {
      'catalog responds successfully': (response) => response.status === 200,
      'catalog response is JSON': (response) =>
        (response.headers['Content-Type'] || '').includes('application/json'),
    })
  })

  sleep(0.5)
}
