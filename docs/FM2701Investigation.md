# FM27 27.0.1 investigation (work in progress)

- The supplied save reports build `27.0.1+2494687`, `game_db=4000`, `tactics_man=26`.
- With fmsave 0.12.2, 102 Python tests passed. In the supplied save, 74,983 players, the managed squad, 77 club staff rows and three tactics of eleven positions each were read.
- On the earlier paired FM27 save, staff recruitment including recovered profiles and three tactics were read. An FM26 save yielded 75,687 players and 84 managed-club staff. These are coverage checks, not proof every field matches the game.
- Writing and tactics are explicitly enabled for this exact build and matching schemas at the user's request. A modified copy was reopened and compared section by section; loading it in the game has not yet been confirmed. FM27 history and recovered staff remain restricted to the earlier paired build. Future builds remain guarded.
- First-run language selection currently offers Italian and English only. French and German resources, dynamic text and UI translation remain outstanding. Do not merge or release this branch as finished.
- Upstream documents a 27.0.1 training/mentoring change and treats non-registered FM27 builds as unverified: https://rhiever.github.io/fmsave/changelog.html
