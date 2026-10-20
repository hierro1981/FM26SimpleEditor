import dataclasses
import json
from pathlib import Path
import unittest
from fmsave.models.players import Attributes


class TacticalCatalogueTests(unittest.TestCase):
    def test_catalogue_coverage_and_data_provenance(self):
        data=json.loads((Path(__file__).resolve().parents[1]/'Data/tactical_roles.json').read_text())
        roles=data['roles']
        self.assertEqual((len(roles),sum(r['phase']=='ip' for r in roles),sum(r['phase']=='op' for r in roles)),(79,40,39))
        self.assertFalse(data['officialFormulaAvailable'])
        self.assertEqual(data['scoreSource'],'editor-heuristic')
        self.assertEqual(len({r['id'] for r in roles}),len(roles))
        available={f.name for f in dataclasses.fields(Attributes)}
        positions={'GK','DC','DL','DR','WBL','WBR','DM','MC','ML','MR','AMC','AML','AMR','STC'}
        for role in roles:
            self.assertTrue(role['english'] and role['italian'])
            self.assertTrue(role['positions'] and set(role['positions'])<=positions)
            self.assertTrue(role['focus'] and set(role['focus']+role['support'])<=available,role['id'])
            self.assertEqual(len(role['focus']),len(set(role['focus'])))
            self.assertEqual(len(role['support']),len(set(role['support'])))
        for position in positions:
            self.assertTrue(any(position in r['positions'] and r['phase']=='ip' for r in roles),position)
            self.assertTrue(any(position in r['positions'] and r['phase']=='op' for r in roles),position)
        for name in ['Deep-Lying Forward','Centre Forward','Target Forward','False Nine','Poacher']:
            self.assertTrue(any(r['english']==name and 'STC' in r['positions'] and r['phase']=='ip' for r in roles))


if __name__=='__main__': unittest.main()
