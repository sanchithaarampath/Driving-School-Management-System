content = '''import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
'''

with open('src/main.ts', 'w', encoding='utf-8') as f:
    f.write(content)
print('main.ts fixed!')
