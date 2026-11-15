# Security policy

PulseGrid 0.1.0 is a pre-release Evidence MVP. Security updates target the development branch; no stable support window exists yet.

Report vulnerabilities privately through this repository's GitHub Security Advisories when enabled. Do not post credentials or private execution evidence in public issues. If private reporting is unavailable, request a private disclosure channel without publishing exploit details.

Capture excludes raw reports, test parameters, failure text, stack traces and output. It does not collect environment variables, source files or credentials, and does not execute tests or upload evidence. Retained stable identifiers and explicitly supplied revision/branch values still require review before sharing.

Imported archives are untrusted. Schema, entry paths, lengths and SHA-256 hashes are checked before publishing output. Hashes are not authentication; attackers can construct valid bundles or recompute digests. Unsigned bundles cannot prove runner identity or execution correctness.

Use restricted local directory permissions and disk quotas, review retained data and delete bundles under your own retention policy. Test results outside the bundle are the producer's responsibility. Reproduction sandboxing, signatures, automatic retention policies, remote credentials and remote authorization belong to later milestones.
