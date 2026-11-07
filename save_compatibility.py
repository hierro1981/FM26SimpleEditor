"""Edition-specific capabilities. A matching schema alone never authorises writes."""
FM27_PAIRED_BUILD = '27.0.0+2493426'


def edition(info):
    return getattr(info, 'game', None) or ('FM27' if str(getattr(info, 'build', '')).startswith('27.') else 'FM26')


def paired_fm27(info):
    return edition(info) == 'FM27' and info.build == FM27_PAIRED_BUILD and info.section_schemas.get('game_db') == 4000


def capabilities(info):
    game = edition(info)
    return dict(game=game, build=info.build, experimental=game == 'FM27',
                canSave=game == 'FM26' or paired_fm27(info))


def require_writable(info):
    if not capabilities(info)['canSave']:
        raise ValueError('FM27: questa build è disponibile solo in lettura; la scrittura non è stata verificata.')
