# Box Search Agent Playbook

This repository is for a Core Keeper mod built with C# (Unity ecosystem).

## Mission

Build a fast, reliable item-search mod that helps players locate items across storage boxes instantly.

## Tech Direction

- Language: C#
- Mod ecosystem: BepInEx + Harmony
- Primary target: clear gameplay value with low UI friction

## Non-Negotiable Rules

1. Every `public` class must have XML documentation comments.
2. Every `public` method must have XML documentation comments.
3. Documentation must explain intent (why), not only behavior (what).
4. Public APIs without XML docs are not allowed to be committed.

## XML Doc Standard

Use this style for public types and members:

```csharp
/// <summary>
/// Searches known storage containers for items matching a query.
/// </summary>
public sealed class StorageSearchService
{
    /// <summary>
    /// Returns all matches for the provided item name query.
    /// </summary>
    /// <param name="query">User-entered text used to match item names.</param>
    /// <returns>A read-only list of matching item locations.</returns>
    public IReadOnlyList<SearchResult> Search(string query)
    {
        throw new NotImplementedException();
    }
}
```

## Engineering Style

- Favor clear naming over clever naming.
- Keep methods focused and small.
- Prefer immutable data where practical.
- Preserve compatibility when changing public interfaces.
- Add tests for behavior changes whenever test infrastructure exists.

## Collaboration Protocol

- Make small, reviewable commits.
- Write commit messages that explain why.
- Keep README and docs aligned with real behavior.
- If assumptions are made, document them in code or docs.

## Definition of Done

A task is done only when:

- Implementation is complete.
- Public APIs include XML documentation comments.
- Documentation is updated if behavior changed.
- Build and test steps pass (when available).

Ship clean. Ship searchable. Ship with docs.
