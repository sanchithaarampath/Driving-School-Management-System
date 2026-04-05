import os

content = """@import 'bootstrap/dist/css/bootstrap.min.css';
@import 'bootstrap-icons/font/bootstrap-icons.css';

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

:root {
  --primary: #e63946;
  --dark-bg: #0d1117;
  --dark-card: #161b22;
  --dark-sidebar: #010409;
  --dark-border: #30363d;
  --text-primary: #e6edf3;
  --text-secondary: #8b949e;
  --success: #238636;
  --warning: #d29922;
}

body {
  background-color: var(--dark-bg);
  color: var(--text-primary);
  font-family: 'Segoe UI', sans-serif;
}

.card-dark {
  background-color: var(--dark-card);
  border: 1px solid var(--dark-border);
  border-radius: 10px;
  padding: 1.5rem;
}

.btn-primary {
  background-color: var(--primary) !important;
  border-color: var(--primary) !important;
}

.btn-primary:hover {
  background-color: #c1121f !important;
}

.sidebar {
  background-color: var(--dark-sidebar);
  min-height: 100vh;
  border-right: 1px solid var(--dark-border);
}

.table-dark {
  --bs-table-bg: var(--dark-card);
  --bs-table-border-color: var(--dark-border);
}

input, select, textarea {
  background-color: var(--dark-card) !important;
  border-color: var(--dark-border) !important;
  color: var(--text-primary) !important;
}

input::placeholder {
  color: var(--text-secondary) !important;
}

.badge-success { background-color: var(--success); }
.badge-warning { background-color: var(--warning); }
.text-muted { color: var(--text-secondary) !important; }
"""

with open("src/styles.scss", "w", encoding="utf-8") as f:
    f.write(content)
print("styles.scss updated successfully!")
