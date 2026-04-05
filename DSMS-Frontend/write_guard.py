import os

content = '''import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  
  if (token) {
    return true;
  }
  
  router.navigate(['/login']);
  return false;
};
'''

os.makedirs("src/app/guards", exist_ok=True)
with open("src/app/guards/auth.ts", "w", encoding="utf-8") as f:
    f.write(content)
print("auth.ts guard created successfully!")
