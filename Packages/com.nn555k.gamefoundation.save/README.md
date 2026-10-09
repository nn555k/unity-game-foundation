# Game Foundation Save

Versioned local persistence with checksums, sequential migrations, backup fallback and primary-file recovery.

Projects define their own save DTO and migration chain. Default stores support files, PlayerPrefs and memory; cloud persistence remains a project adapter.

Import `Versioned Save` from Package Manager for a compiling version-two data model and version-one migration example.
