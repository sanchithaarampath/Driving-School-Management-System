with open('src/app/services/auth.ts', 'r', encoding='utf-8') as f:
    content = f.read()
content = content.replace('http://localhost:5000/api/auth', 'http://localhost:5062/api/auth')
with open('src/app/services/auth.ts', 'w', encoding='utf-8') as f:
    f.write(content)
print('URL updated to port 5062!')
