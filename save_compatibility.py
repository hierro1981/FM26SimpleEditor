"""Build metadata and optional-reader gates; saving is never blocked by build version."""
FM27_PAIRED_BUILD = '27.0.0+2493426'
FM27_ADDITIONAL_BUILDS = {'27.0.1+2494687'}


def edition(info):
    return getattr(info, 'game', None) or ('FM27' if str(getattr(info, 'build', '')).startswith('27.') else 'FM26')


def paired_fm27(info):
    return edition(info) == 'FM27' and getattr(info, 'build', '') == FM27_PAIRED_BUILD and getattr(info, 'section_schemas', {}).get('game_db') == 4000


def writable_fm27(info):
    return paired_fm27(info) or (edition(info) == 'FM27' and getattr(info, 'build', '') in FM27_ADDITIONAL_BUILDS
                               and getattr(info, 'section_schemas', {}).get('game_db') == 4000)


def capabilities(info):
    game = edition(info)
    return dict(game=game, build=getattr(info, 'build', ''), experimental=game == 'FM27' and not paired_fm27(info),
                canSave=True)


def require_writable(info):
    """Unknown builds do not block saving; all data and archive checks still apply."""
    return None
