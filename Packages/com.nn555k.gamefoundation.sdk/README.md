# Game Foundation SDK

Ordered SDK adapter initialization with consent gates, hard timeouts, isolated failures, lifecycle forwarding and diagnostics.

Vendor libraries and event schemas remain in project-owned `ISdkAdapter` implementations. Purchasing and IAP are intentionally excluded.

Import `SDK Adapter Template` from Package Manager for a compiling project-owned bridge example. Replace only the bridge implementation; keep vendor keys in project configuration or the platform secret system.
