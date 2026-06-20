# Stage 5: Single Company Docs Library

## Summary

Index one configured local directory recursively as a single versionless library:

- Library ID: `docs:company-docs`
- Kind: `docs`
- Display name: `Company Docs`
- All matching files belong to this library.
- Git cloning or synchronization is out of scope.

## Key Changes

- Add optional `DevContextMcp:Documentation` configuration with `RootPath` and a required extension allowlist such as `.md`, `.txt`.
- Skip hidden directories, symbolic links, reparse points, and unsupported files.
- Chunk documents using the existing chunker while preserving normalized relative paths and content hashes.
- Replace the library snapshot atomically; remove deleted files and preserve the previous snapshot if indexing fails.
- Extend library persistence and contracts to support a `docs` kind with nullable environment and version.
- Keep an internal `current` snapshot record for storage, but never expose it as a version.

## MCP Behavior

- `resolve_library` can return `docs:company-docs` from its name or indexed content.
- `query_docs` searches all files in the library and returns `docs://company-docs/{relativePath}` citations.
- Package-only query parameters are ignored for this library with a `parameter_not_applicable` warning.
- `list_versions` returns an empty successful result with a `version_not_applicable` warning.
- `get_symbol` returns `not_found` with `symbol_lookup_not_supported`.
- Add an MCP resource for reading the complete indexed file referenced by a citation.

## Test Plan

- Validate missing roots, empty or malformed extension lists, and relative path resolution.
- Verify recursive indexing, extension filtering, path normalization, and traversal protection.
- Resolve the single library using terms found in different files.
- Query evidence across multiple files while retaining file-specific citations.
- Verify unchanged, modified, added, and deleted file behavior.
- Confirm failed indexing preserves the previous company-docs snapshot.
- Run all existing NuGet tests to ensure its versioned behavior remains unchanged.

## Assumptions

- The documentation root is already available locally.
- Text files must be readable as UTF-8.
- The documentation feature is disabled when its configuration section is absent.
- Every configured document belongs to `docs:company-docs`; there are no categories represented as separate libraries.
