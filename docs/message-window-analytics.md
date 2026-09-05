# Message-window analytics scenario

## Business scenario

A retailer records one `amount` for every message/event. The operations team wants
to identify how often a sequence of exactly `m` consecutive events reaches a
target total `d`. For example, with amounts `[20, 30, 50, 10, 40]`, `m = 2`, and
`d = 50`, the matching windows are `[20, 30]` and `[10, 40]`; the result is `2`.

Messages are ordered by `CreatedUtc` and then `Id`, so a "consecutive" window has
a deterministic meaning even when two messages have the same timestamp. The
analysis is always retailer-scoped by the existing authorization and EF query
filter.

## REST API

Create messages with a numeric `amount` attribute:

```http
POST /api/messages
Authorization: Bearer <retailer JWT>
Content-Type: application/json

{ "text": "Order event", "amount": 20 }
```

Request the aggregate:

```http
GET /api/messages/analytics/windows?m=2&d=50
Authorization: Bearer <retailer JWT>
```

Example response:

```json
{
  "windowLength": 2,
  "targetAmount": 50,
  "messageCount": 5,
  "matchingWindowCount": 2
}
```

The endpoint returns `400 Bad Request` when `m` is not positive and `422
Unprocessable Entity` when any message in the retailer's analysis set does not
have a numeric `attributes.amount`. This preserves the meaning of consecutive
messages: silently skipping an invalid message would join two events that were
not adjacent.

`MessageWindowAnalytics.CountWindowsWithSum` uses a sliding window. It calculates
the first window once, then subtracts the outgoing amount and adds the incoming
amount for each subsequent window. Its time complexity is `O(n)` and its extra
space is `O(1)` apart from the API's materialized message list.
