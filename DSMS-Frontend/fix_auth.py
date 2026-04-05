with open('src/app/services/auth.ts', 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace(
    "this.http.post(\/login,",
    "this.http.post(\/login,"
)
content = content.replace(
    "this.http.post(\/change-password,",
    "this.http.post(\/change-password,"
)

with open('src/app/services/auth.ts', 'w', encoding='utf-8') as f:
    f.write(content)
print('Fixed!')
