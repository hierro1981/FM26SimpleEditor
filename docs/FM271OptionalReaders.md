# FM27 optional-reader checks

FM Simple Editor 16.3.1 uses fmsave 0.12.6. The optional-reader allowlist adds FM27 27.1.0+2505117 while retaining schema checks: game_db 4000, and tactics_man 26 for tactics.

Before enabling these readers, the following checks were exercised on local saves, which are not included in this repository:

- Tactics: manager identity, first-team ownership, declared block counts, eleven slots, complete phase-slot permutation and recognised position/role/duty mappings in both phases.
- Attribute history: repeated marker-2 records, valid counts and ranges for all 47 attributes, and rejection of ambiguous sequences. Exact per-sample dates and historical CA/PA remain undecoded.
- Additional staff: unique identities, exclusion of already employed profiles, bounded person records and valid ability/attribute ranges. Recruitment integration was also exercised. No detected club does not establish unemployment or willingness to join.

The recovered-staff reader now passes ambiguous player-header boundaries to fmsave's seven-argument closing-header helper. A regression test uses the installed helper signature and verifies that ambiguous boundaries are preserved.

Unknown optional-reader layouts remain rejected. Saving remains enabled independently of this allowlist, with the existing integrity checks.
