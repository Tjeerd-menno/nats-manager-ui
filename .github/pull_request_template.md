## Summary

-

## Validation

-

## Workflow Security Impact (required when changing `.github/workflows/**` or `.github/dependabot.yml`)

- [ ] All non-generated workflow actions are SHA pinned
- [ ] `actions/checkout` steps use `persist-credentials: false` unless push is required
- [ ] No PAT fallback patterns (for example `secretA || secrets.GITHUB_TOKEN`)
- [ ] No mutable container image tags (must use digest pinning)
- [ ] Generated lock workflows were updated only through source + compile flow
- [ ] Branch protection / required checks behavior remains enforced
- [ ] Any accepted risk is documented with owner and follow-up issue
