import json

with open('angular.json', 'r', encoding='utf-8') as f:
    config = json.load(f)

build_options = config['projects']['DSMS-Frontend']['architect']['build']['options']

# Add polyfills
build_options['polyfills'] = ['zone.js']

# Remove SSR related options
build_options.pop('server', None)
build_options.pop('outputMode', None)
build_options.pop('ssr', None)

with open('angular.json', 'w', encoding='utf-8') as f:
    json.dump(config, f, indent=2)

print('angular.json updated!')
