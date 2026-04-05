import os

content = '''import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet></router-outlet>',
  styles: []
})
export class App {
  title = 'DSMS-Frontend';
}
'''

with open("src/app/app.ts", "w", encoding="utf-8") as f:
    f.write(content)
print("app.ts updated!")
