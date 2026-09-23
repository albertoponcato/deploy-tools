# Copilot Instructions

## Commit Messages

All commit messages **must** follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification. Copilot must always suggest commit messages in this format.

### Format

```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

- The **header** (first line) is mandatory.
- The **body** is optional; use it to explain *why*, not *what*.
- The **footer** is optional; use it for breaking changes or issue references.
- The header must not exceed **72 characters**.
- Use the **imperative, present tense** in the description: "add" not "added" or "adds".
- Do **not** capitalize the first letter of the description.
- Do **not** end the description with a period.

---

### Allowed Types

| Type       | When to use                                                                 |
|------------|-----------------------------------------------------------------------------|
| `feat`     | A new feature                                                               |
| `fix`      | A bug fix                                                                   |
| `docs`     | Documentation changes only                                                  |
| `style`    | Formatting, missing semicolons, etc. — no logic change                      |
| `refactor` | Code change that neither fixes a bug nor adds a feature                     |
| `perf`     | A code change that improves performance                                     |
| `test`     | Adding or correcting tests                                                  |
| `build`    | Changes to the build system or external dependencies (e.g. NuGet, npm)     |
| `ci`       | Changes to CI/CD configuration or scripts                                   |
| `chore`    | Maintenance tasks that don't modify src or test files                       |
| `revert`   | Reverts a previous commit                                                   |

---

### Scopes

Scopes are **optional** but strongly encouraged to provide context. Use lowercase, short, noun-based names that reflect the area of the codebase being changed.

**Suggested scopes for this project:**

- `routing` — URL routing, content finders, redirects
- `content` — Umbraco content models or document types
- `media` — media handling
- `auth` — authentication and authorization
- `api` — controller or API surface changes
- `ui` — front-end or Razor Pages views
- `config` — configuration files or settings
- `db` — database migrations or schema changes
- `deps` — dependency or package updates
- `seo` — SEO-related changes (meta, sitemaps, redirects)

Custom scopes are allowed as long as they are **consistent** and **descriptive**.

---

### Breaking Changes

Breaking changes must be indicated in **one of two ways** (or both):

1. Append `!` after the type/scope in the header:
   ```
   feat(api)!: remove legacy product endpoint
   ```
2. Add a `BREAKING CHANGE:` footer with a description:
   ```
   BREAKING CHANGE: the `/api/v1/products` endpoint has been removed. Use `/api/v2/products` instead.
   ```

Both methods can be combined for clarity.

---

### Examples

#### ✅ Valid commit messages

```
feat(routing): add custom 404 redirect content finder
```
```
fix(content): resolve null reference in product page model
```
```
refactor(ui): extract header partial into shared layout
```
```
docs: update README with local setup instructions
```
```
build(deps): upgrade Umbraco.Cms to 13.4.0
```
```
feat(api)!: replace XML serialization with System.Text.Json

BREAKING CHANGE: all API responses are now camelCase. Clients must be updated accordingly.
```
```
fix(seo): correct canonical URL generation for category pages

Closes #142
```

#### ❌ Invalid commit messages

```
fixed bug                         # missing type, vague description
```
```
WIP                               # not descriptive, no type
```
```
feat: Added new feature.          # past tense, ends with period, capitalized
```
```
update stuff                      # no type, no scope, vague
```
```
FEAT(Routing): Add Redirect       # type must be lowercase, description capitalized
```
```
feat(routing): add redirect content finder for 404 pages in the website  # header too long (>72 chars)
```

---

### Multi-line Body Guidelines

Use the body when the change needs explanation beyond the summary line:

```
fix(routing): prevent redirect loop on missing culture segment

The NotFoundRedirectContentFinder was matching URLs without a culture
prefix and redirecting to the same URL, causing an infinite loop.
Added a guard to exit early when no valid redirect target is found.
```

- Separate the body from the header with a **blank line**.
- Wrap lines at **72 characters**.
- Focus on the motivation and context, not the implementation details.

---

### Footer Guidelines

Footers are key-value pairs separated by `: ` or ` #`.

```
Reviewed-by: Jane Doe
Refs: #98, #101
Closes: #110
BREAKING CHANGE: see migration guide in docs/migration.md
```

Multiple footers are allowed, each on its own line.

---

### Copilot Behavior

- **Always** suggest commit messages in the Conventional Commits format.
- **Infer the type** from the nature of the changes (new file → `feat`, bug fix → `fix`, etc.).
- **Infer the scope** from the folder or module being changed (e.g., changes in `Routing/` → `routing`).
- **Never** suggest vague messages like `update`, `fix bug`, or `changes`.
- When a change is potentially breaking, **always** include the `!` marker and a `BREAKING CHANGE:` footer.
- Prefer **short, focused commits** over large multi-purpose ones; suggest splitting when appropriate.
