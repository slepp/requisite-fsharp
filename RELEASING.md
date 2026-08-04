# Releasing

1. Update `VersionPrefix` in `Directory.Build.props` and add the matching
   changelog entry. `AssemblyVersion` and `FileVersion` derive from it.
   Starting with the second release, also set `PackageValidationBaselineVersion`
   in the library project to the previous stable package version.
2. Run the locked restore, formatting, Release build, both test targets,
   package, package inspection, and example commands from the README.
3. Commit and push those changes, then create and push an exact `v<version>`
   tag, for example `v0.1.0`.
4. Configure a NuGet trusted-publishing policy for repository
   `slepp/requisite-fsharp`, workflow `release.yml`, and environment `nuget`.
   Add the policy creator's NuGet username as the environment secret
   `NUGET_USER`.
5. The release workflow verifies that the tag and package version match,
   rebuilds from lock files, tests, packs, attests the artifacts, exchanges
   GitHub OIDC for a short-lived NuGet key, and publishes the package.

For an existing tag that completed before trusted publishing was configured,
run **Release** manually with the released version. It checks out the exact
tag. Never publish from an untagged branch or by editing the workflow's version
check.
