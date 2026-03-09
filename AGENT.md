# Box Search Agent Playbook

This document defines how coding agents work in this repository.

## Mission

Build a fast, reliable Core Keeper mod that lets players find items inside storage boxes without guesswork.

## Non-Negotiable Rules

1. Every `public` class must have JavaDoc.
2. Every `public` method must have JavaDoc.
3. JavaDoc must explain intent, not only behavior.
4. Public APIs without JavaDoc are not allowed to be committed.

## JavaDoc Standard

Use this format for public types and methods:

```java
/**
 * Short purpose statement.
 */
public final class ExampleClass {

    /**
     * Explains what this method does and why it exists.
     *
     * @param value input meaning
     * @return result meaning
     */
    public String run(String value) {
        return value;
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
- Keep README and docs aligned with actual behavior.
- If assumptions are made, document them in code or docs.

## Definition of Done

A task is done only when:

- Implementation is complete
- Public APIs include JavaDoc
- Documentation is updated if behavior changed
- Build/test steps pass (when available)

Ship clean. Ship searchable. Ship with docs.
