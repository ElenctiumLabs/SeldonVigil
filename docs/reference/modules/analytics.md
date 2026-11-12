# Analytics Module

Notification and analytics data management. Provides the data feed for the activity feed on the dashboard.

Notifications are stored in PostgreSQL (`app_notifications`) through `NotificationRepository` and read via the API. The list supports an unread filter and a mark-as-read operation.
